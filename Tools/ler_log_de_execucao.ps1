<#
.SYNOPSIS
    Le o log de execucao do jogo e resume o que ELE reclamou. Rode depois de jogar.

.DESCRIPTION
    Em 2026-09-02 o Vini perguntou por que eu nao conseguia consertar a UI. Uma das quatro
    causas: existe log de runtime nesta maquina desde sempre, e eu nunca abri um. O jogo
    diagnostica os proprios defeitos -- "[Navegacao] Nenhum Grid nesta cena", "[Detector]
    o buffer encheu e alvos podem estar sendo descartados em silencio" -- e ninguem lia.

    Le tres fontes, na ordem em que forem existindo:
      Editor.log   -- a sessao no Editor (o Play do Unity)
      Player.log   -- a build rodada
      Player-prev.log

    Agrupa por MARCADOR (o prefixo [Assim] que o projeto usa nos Debug.Log), conta as
    repeticoes, e mostra erros e excecoes por inteiro.

.PARAMETER Fonte
    Editor (padrao), Player, ou Ambos.

.PARAMETER Linhas
    Quantos marcadores distintos listar. Padrao 25.

.EXAMPLE
    .\Tools\ler_log_de_execucao.ps1
    .\Tools\ler_log_de_execucao.ps1 -Fonte Ambos
#>

param(
    [ValidateSet("Editor", "Player", "Ambos")]
    [string]$Fonte = "Editor",

    [int]$Linhas = 25
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$ProjectPath = Split-Path -Parent $PSScriptRoot

# A identidade da build decide o caminho do Player.log. Lida do ProjectSettings para nao
# virar mais uma copia de string que diverge no primeiro renomeio.
$settings = Join-Path $ProjectPath "ProjectSettings\ProjectSettings.asset"
$empresa = "Favela Amarela"
$produto = "Caminho para Carcosa"

if (Test-Path $settings) {
    $txt = Get-Content -Raw -Encoding UTF8 $settings
    $mE = [regex]::Match($txt, 'companyName:\s*(.+)')
    $mP = [regex]::Match($txt, 'productName:\s*(.+)')
    if ($mE.Success) { $empresa = $mE.Groups[1].Value.Trim() }
    if ($mP.Success) { $produto = $mP.Groups[1].Value.Trim() }
}

$pastaDoPlayer = Join-Path $env:USERPROFILE "AppData\LocalLow\$empresa\$produto"

$candidatos = @()
if ($Fonte -in @("Editor", "Ambos")) {
    $candidatos += [pscustomobject]@{
        Nome = "Editor.log"
        Caminho = Join-Path $env:LOCALAPPDATA "Unity\Editor\Editor.log"
    }
}
if ($Fonte -in @("Player", "Ambos")) {
    $candidatos += [pscustomobject]@{ Nome = "Player.log";      Caminho = Join-Path $pastaDoPlayer "Player.log" }
    $candidatos += [pscustomobject]@{ Nome = "Player-prev.log"; Caminho = Join-Path $pastaDoPlayer "Player-prev.log" }
}

$achouAlgum = $false

foreach ($c in $candidatos) {
    if (-not (Test-Path $c.Caminho)) {
        Write-Host "  $($c.Nome): nao existe" -ForegroundColor DarkGray
        continue
    }

    $achouAlgum = $true
    $info = Get-Item $c.Caminho
    $idade = [Math]::Round(((Get-Date) - $info.LastWriteTime).TotalMinutes)

    Write-Host ""
    Write-Host "  == $($c.Nome) ==" -ForegroundColor Cyan
    Write-Host "  $([Math]::Round($info.Length/1KB)) KB, escrito ha $idade min ($($info.LastWriteTime))"

    $conteudo = Get-Content -Encoding UTF8 $c.Caminho

    # 1. Marcadores do proprio jogo, agrupados. Numeros viram N para "20 esqueletos
    #    invocados" nao virar 20 linhas distintas.
    $marcados = $conteudo | Where-Object { $_ -match '^\[[A-Za-z][A-Za-z0-9_]*\]' }

    if ($marcados.Count -eq 0) {
        Write-Host "  (nenhuma linha com marcador [Assim] -- o jogo nao rodou nesta sessao)" -ForegroundColor DarkGray
    }
    else {
        $grupos = $marcados |
            ForEach-Object { ($_ -replace '[0-9]+([.,][0-9]+)?', 'N').Trim() } |
            Group-Object |
            Sort-Object Count -Descending |
            Select-Object -First $Linhas

        Write-Host ""
        Write-Host "  Marcadores ($($marcados.Count) linhas, $($grupos.Count) distintos):"
        foreach ($g in $grupos) {
            $texto = $g.Name
            if ($texto.Length -gt 110) { $texto = $texto.Substring(0, 107) + "..." }
            Write-Host ("    {0,4}x  {1}" -f $g.Count, $texto)
        }
    }

    # 2. Erros e excecoes, por inteiro -- estes nunca sao resumidos.
    $graves = $conteudo | Where-Object {
        $_ -match 'Exception|NullReference|error CS[0-9]|Assertion failed'
    } | Select-Object -Unique -First 15

    if ($graves.Count -gt 0) {
        Write-Host ""
        Write-Host "  ERROS / EXCECOES:" -ForegroundColor Red
        foreach ($g in $graves) { Write-Host "    $($g.Trim())" }
    }
    else {
        Write-Host ""
        Write-Host "  Sem excecoes." -ForegroundColor Green
        Write-Host "  (o que NAO quer dizer sem defeito: layout quebrado, tecla disputada e" -ForegroundColor DarkGray
        Write-Host "   clique que nao chega nao levantam excecao nenhuma)" -ForegroundColor DarkGray
    }
}

if (-not $achouAlgum) {
    Write-Host ""
    Write-Host "  Nenhum log encontrado." -ForegroundColor Yellow
    Write-Host "  Editor.log aparece depois de abrir a Unity; Player.log, depois de rodar a build."
}

Write-Host ""
