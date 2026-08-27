<#
.SYNOPSIS
    Roda uma ferramenta de Editor em batch mode e PROVA que ela rodou.
    Imprime "TOOL OK" ou "TOOL FAILED".

.DESCRIPTION
    O Corolário 4 do COMMANDMENT diz: "o exit code de uma ferramenta e o log dela não são
    evidência". E `-executeMethod` devolve exatamente isso — um exit code. Um método com nome
    errado, um método que retornou cedo por um `if` de guarda, um método que achou a pasta
    vazia: os três saem com código 0 e nada distinguindo.

    Este wrapper exige um MARCADOR — o prefixo que a ferramenta usa nos próprios
    `Debug.Log` (ex.: "[BasesDeArma]"). Se o log não contiver nenhuma linha com ele, o
    wrapper FALHA, porque a ferramenta não deixou rastro de ter feito nada.

    Não substitui verificar o disco. Substitui o passo anterior: saber se vale a pena olhar.

.PARAMETER Metodo
    Nome totalmente qualificado. Ex.: FavelaAmarela.EditorTools.MontarBasesDeArma.Executar

.PARAMETER Marcador
    O prefixo dos Debug.Log da ferramenta. Ex.: "[BasesDeArma]"

.PARAMETER SemMarcador
    Roda sem exigir rastro. Use só para ferramenta que legitimamente não loga nada —
    e prefira acrescentar um log à ferramenta.

.EXAMPLE
    .\Tools\run_editor_tool.ps1 `
        -Metodo FavelaAmarela.EditorTools.MontarBasesDeArma.Executar `
        -Marcador "[BasesDeArma]"
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$Metodo,

    [string]$Marcador = "",

    [switch]$SemMarcador
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

. (Join-Path $PSScriptRoot "unity_common.ps1")

$ProjectPath = Split-Path -Parent $PSScriptRoot

if ((-not $SemMarcador) -and [string]::IsNullOrWhiteSpace($Marcador)) {
    Write-Host ""
    Write-Host "  FALTA O MARCADOR" -ForegroundColor Yellow
    Write-Host "  Sem ele não há como distinguir 'a ferramenta rodou e não achou nada' de"
    Write-Host "  'o método nem existe'. Passe -Marcador ""[SeuPrefixo]"", ou -SemMarcador"
    Write-Host "  se a ferramenta realmente não loga."
    Write-Host ""
    Write-Host "TOOL FAILED"
    exit 1
}

# ── 1. O Editor está aberto? ───────────────────────────────────────────────────────────
if (Test-UnityAberta) {
    Write-Host ""
    Write-Host "  EDITOR ABERTO" -ForegroundColor Yellow
    Write-Host "  Batch mode exige instância única — feche a Unity e rode de novo."
    Write-Host "  (Nada foi executado, então nada mudou no projeto.)"
    Write-Host ""
    Write-Host "TOOL FAILED"
    exit 1
}

$unityEditor = Find-UnityEditor
if (-not $unityEditor) {
    Write-Host "  ERRO: Unity Editor não encontrado." -ForegroundColor Red
    Write-Host "TOOL FAILED"
    exit 1
}

# ── 2. Roda ────────────────────────────────────────────────────────────────────────────
$logDir = Join-Path $ProjectPath "TestResults"
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir -Force | Out-Null }

$nomeCurto = ($Metodo -split '\.')[-2..-1] -join '.'
$logFile = Join-Path $logDir "unity_tool.log"
if (Test-Path $logFile) { Remove-Item $logFile -Force }

Write-Host "  $nomeCurto..." -NoNewline
$inicio = Get-Date

Set-Location $ProjectPath

& "$unityEditor" -batchmode -nographics -projectPath "." `
    -executeMethod $Metodo -quit -logFile "$logFile" | Out-Null

$exitCode = $LASTEXITCODE
$duracao = [Math]::Round(((Get-Date) - $inicio).TotalSeconds)
Write-Host " ($duracao s)"

$linhas = Read-UnityLog -Path $logFile

# ── 3. Classifica ──────────────────────────────────────────────────────────────────────
if (Test-ColisaoDeInstancia -Linhas $linhas) {
    Write-Host ""
    Write-Host "  EDITOR ABERTO (a Unity acusou a colisão)" -ForegroundColor Yellow
    Write-Host "TOOL FAILED"
    exit 1
}

$erros = Get-ErrosDeCompilacao -Linhas $linhas
if ($erros.Count -gt 0) {
    Write-Host ""
    Write-Host "  ERRO DE COMPILAÇÃO — $($erros.Count) distinto(s)" -ForegroundColor Red
    Write-Host "  A ferramenta NÃO rodou: sem compilar, o método nem existe." -ForegroundColor Red
    Write-Host ""
    foreach ($e in $erros | Select-Object -First 15) { Write-Host "    $e" }
    Write-Host ""
    Write-Host "TOOL FAILED"
    exit 1
}

# Método inexistente ou com nome errado: a Unity diz isso no log, e o exit code sozinho não.
foreach ($l in $linhas) {
    if ($l -match 'executeMethod.*(could not|not found|does not exist)' -or
        $l -match 'Failed to (find|execute) method') {
        Write-Host ""
        Write-Host "  MÉTODO NÃO ENCONTRADO" -ForegroundColor Red
        Write-Host "    $($l.Trim())"
        Write-Host "  Confira o nome totalmente qualificado: $Metodo"
        Write-Host ""
        Write-Host "TOOL FAILED"
        exit 1
    }
}

# ── 4. A ferramenta deixou rastro? ─────────────────────────────────────────────────────
if ($SemMarcador) {
    Write-Host ""
    Write-Host "  Unity saiu com código $exitCode. SEM MARCADOR: nada foi verificado além disso." -ForegroundColor Yellow
    Write-Host "  Confira o resultado no disco antes de acreditar." -ForegroundColor Yellow
    Write-Host ""
    if ($exitCode -eq 0) { Write-Host "TOOL OK"; exit 0 }
    Write-Host "TOOL FAILED"
    exit 1
}

# Um Debug.Log multi-linha só tem o marcador na PRIMEIRA linha; o corpo vem indentado
# logo abaixo, e a Unity anexa uma stack trace depois. Pegar só a linha do marcador
# mostraria "[BasesDeArma] Concluído:" e engoliria exatamente o que a ferramenta fez.
$rastro = @()
$dentro = $false

foreach ($l in $linhas) {
    if ($l.Contains($Marcador)) {
        $rastro += $l.TrimEnd()
        $dentro = $true
        continue
    }

    if (-not $dentro) { continue }

    # A stack trace da Unity fecha a mensagem.
    if ($l -match '^UnityEngine\.|^UnityEditor\.|^\s*at\s' ) { $dentro = $false; continue }

    # Linha em branco também: o corpo da mensagem é contíguo.
    if ([string]::IsNullOrWhiteSpace($l)) { $dentro = $false; continue }

    # Continuação indentada do corpo.
    if ($l -match '^\s') { $rastro += $l.TrimEnd(); continue }

    $dentro = $false
}

if ($rastro.Count -eq 0) {
    Write-Host ""
    Write-Host "  A FERRAMENTA NÃO DEIXOU RASTRO" -ForegroundColor Red
    Write-Host "  Unity saiu com código $exitCode e o log não tem uma linha com '$Marcador'."
    Write-Host ""
    Write-Host "  As três causas prováveis, em ordem:"
    Write-Host '    1. O método retornou cedo por uma guarda (pasta vazia, asset ausente).'
    Write-Host "    2. O marcador está errado — confira o prefixo dos Debug.Log da ferramenta."
    Write-Host "    3. O método rodou e não loga nada — nesse caso, ACRESCENTE um log a ele."
    Write-Host ""
    Write-Host "  Log completo: $logFile"
    Write-Host "TOOL FAILED"
    exit 1
}

# ── 5. Verde: mostra o que a ferramenta disse ──────────────────────────────────────────
Write-Host ""
foreach ($l in $rastro) {
    # A stack trace que a Unity anexa a cada Debug.Log não interessa aqui.
    if ($l -match 'UnityEngine\.|UnityEditor\.|^\s+at ') { continue }
    Write-Host "  $l"
}
Write-Host ""

if ($exitCode -ne 0) {
    Write-Host "  A ferramenta logou, mas a Unity saiu com código $exitCode." -ForegroundColor Yellow
    Write-Host "TOOL FAILED"
    exit 1
}

Write-Host "TOOL OK"
exit 0
