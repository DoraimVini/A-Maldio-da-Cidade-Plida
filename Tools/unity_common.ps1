<#
.SYNOPSIS
    Peças compartilhadas pelos wrappers de Unity em batch mode.

.NOTES
    ⚠️ ESTES ARQUIVOS PRECISAM DE BOM UTF-8. NÃO REMOVA.

    O PowerShell 5.1 lê arquivo UTF-8 SEM BOM como Windows-1252. Aí o travessão "—"
    (bytes E2 80 94) vira "â€”", cujo último byte decodifica para a aspa tipográfica U+201D —
    e o PowerShell ACEITA aspa tipográfica como delimitador de string.

    Resultado: cada travessão dentro de um Write-Host abre uma string fantasma, e o erro
    aparece dezenas de linhas depois, como "a cadeia de caracteres não tem o terminador",
    apontando para uma linha inocente. Aconteceu em 2026-08-27 ao escrever estes wrappers.
    O script original sobrevivia sem BOM só porque só usava travessão em comentário.

.DESCRIPTION
    Existe para que run_qa_tests.ps1 e run_editor_tool.ps1 não mantenham duas cópias das
    mesmas três coisas — achar o Editor, ler o log e detectar erro de compilação. Duas cópias
    divergiriam, que é o modo de falha dominante deste repositório.
#>

# --------------------------------------------------------------------------------------
# Localiza o Unity Editor instalado pelo Hub.
# --------------------------------------------------------------------------------------
function Find-UnityEditor {
    $hub = "C:\Program Files\Unity\Hub\Editor"
    if (-not (Test-Path $hub)) { return $null }

    $versoes = Get-ChildItem -Path $hub -Directory | Sort-Object Name -Descending
    foreach ($v in $versoes) {
        $candidato = Join-Path $v.FullName "Editor\Unity.exe"
        if (Test-Path $candidato) { return $candidato }
    }
    return $null
}

# --------------------------------------------------------------------------------------
# O Editor está aberto?
#
# Batch mode colide com o lock de uma instância do Editor. Sem esta checagem, a colisão
# aparece como "arquivo de resultados não encontrado" — indistinguível de erro de
# compilação, que é um problema completamente diferente e leva a investigar o lugar errado.
# --------------------------------------------------------------------------------------
function Test-UnityAberta {
    $p = Get-Process Unity -ErrorAction SilentlyContinue
    return ($null -ne $p)
}

# --------------------------------------------------------------------------------------
# Lê o log da Unity, detectando a codificação.
#
# ISTO EXISTE POR CAUSA DE UM ERRO REAL (2026-08-27): o log saiu em UTF-16LE, um
# `grep "error CS"` não achou nada, e a conclusão foi "compilou". NÃO tinha compilado. Só
# apareceu porque o XML de resultados não existia. É a pior classe de falha deste projeto:
# o sinal existe, ninguém consegue lê-lo, e o silêncio se parece com sucesso.
# --------------------------------------------------------------------------------------
function Read-UnityLog {
    param([string]$Path)

    if (-not (Test-Path $Path)) { return @() }

    # FileShare.ReadWrite, e não ReadAllBytes: quando a Unity ABORTA (erro de compilação),
    # ela sai sem soltar o handle do log, e ReadAllBytes bate em violação de compartilhamento.
    # O resultado seria uma exceção no lugar do diagnóstico -- justo no caso em que o
    # diagnóstico mais importa. Descoberto ao testar este wrapper contra um erro proposital.
    $bytes = $null
    $fs = $null
    try {
        $fs = [System.IO.File]::Open($Path,
                                     [System.IO.FileMode]::Open,
                                     [System.IO.FileAccess]::Read,
                                     [System.IO.FileShare]::ReadWrite)
        $ms = New-Object System.IO.MemoryStream
        $fs.CopyTo($ms)
        $bytes = $ms.ToArray()
        $ms.Dispose()
    }
    catch {
        Write-Host "  (não consegui ler o log em $Path : $($_.Exception.Message))"
        return @()
    }
    finally {
        if ($null -ne $fs) { $fs.Dispose() }
    }

    if ($null -eq $bytes -or $bytes.Length -eq 0) { return @() }

    # BOM UTF-16 LE
    if ($bytes.Length -ge 2 -and $bytes[0] -eq 0xFF -and $bytes[1] -eq 0xFE) {
        $texto = [System.Text.Encoding]::Unicode.GetString($bytes, 2, $bytes.Length - 2)
        return $texto -split "`r?`n"
    }

    # BOM UTF-8
    if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
        $texto = [System.Text.Encoding]::UTF8.GetString($bytes, 3, $bytes.Length - 3)
        return $texto -split "`r?`n"
    }

    # Sem BOM: UTF-16 sem marca aparece como bytes nulos alternados. Uma amostra basta —
    # texto UTF-8 normal não tem nulo nenhum.
    $amostra = [Math]::Min(400, $bytes.Length)
    $nulos = 0
    for ($i = 0; $i -lt $amostra; $i++) { if ($bytes[$i] -eq 0) { $nulos++ } }

    if ($nulos -gt ($amostra / 4)) {
        $texto = [System.Text.Encoding]::Unicode.GetString($bytes)
        return $texto -split "`r?`n"
    }

    $texto = [System.Text.Encoding]::UTF8.GetString($bytes)
    return $texto -split "`r?`n"
}

# --------------------------------------------------------------------------------------
# Erros de compilação, sem repetição.
#
# O Roslyn emite o mesmo erro várias vezes (uma por assembly afetado). Repetir tudo esconde
# quantos problemas DISTINTOS existem.
# --------------------------------------------------------------------------------------
function Get-ErrosDeCompilacao {
    param([string[]]$Linhas)

    $erros = @()
    foreach ($l in $Linhas) {
        if ($l -match 'error CS\d+') { $erros += $l.Trim() }
    }
    return ($erros | Select-Object -Unique)
}

# --------------------------------------------------------------------------------------
# Sinais de que a colisão com o Editor aconteceu do lado do Unity.
# --------------------------------------------------------------------------------------
function Test-ColisaoDeInstancia {
    param([string[]]$Linhas)

    foreach ($l in $Linhas) {
        if ($l -match 'another Unity instance|Multiple Unity instances|lockfile') { return $true }
    }
    return $false
}
