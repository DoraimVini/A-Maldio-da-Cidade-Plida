<#
.SYNOPSIS
    Roda os testes EditMode do projeto "Caminho para Carcosa".
    Imprime "TESTS PASSED" ou "TESTS FAILED" para consumo da skill favela-qa-pipeline.

.DESCRIPTION
    Classifica o resultado em EXATAMENTE UM de quatro estados, e diz qual:

      1. EDITOR ABERTO    — batch mode colide com o lock; feche a Unity.
      2. ERRO DE COMPILAÇÃO — com as linhas de erro, já sem repetição.
      3. TESTES FALHARAM  — com os nomes e as mensagens.
      4. VERDE            — com as contagens.

    Antes de 2026-08-27 os três primeiros caíam todos em "arquivo de resultados não
    encontrado", e a mensagem chutava entre duas causas. Três problemas que exigem ações
    opostas compartilhavam um sinal só.

    Também deixou de despejar as ~20 mil linhas de boot da Unity no console: elas vão para
    um arquivo de log, e só o que importa é impresso.

.PARAMETER TestPlatform
    EditMode (padrão) ou PlayMode.

.PARAMETER ComGraficos
    Tira o -nographics. Necessário quando os testes de layout acusarem que não conseguiram
    medir texto (sem gráficos a Unity pode não carregar métricas de fonte).

.PARAMETER Detalhado
    Imprime também as últimas linhas do log da Unity. Útil quando o resultado não se
    encaixa em nenhum dos quatro estados.
#>

param(
    [string]$TestPlatform = "EditMode",
    [switch]$Detalhado,
    [switch]$ComGraficos
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

. (Join-Path $PSScriptRoot "unity_common.ps1")

# Raiz do projeto = pasta acima de Tools/. Derivado do próprio caminho do script para
# sobreviver a mudanças de pasta.
$ProjectPath = Split-Path -Parent $PSScriptRoot

# ── 1. O Editor está aberto? ───────────────────────────────────────────────────────────
if (Test-UnityAberta) {
    Write-Host ""
    Write-Host "  EDITOR ABERTO" -ForegroundColor Yellow
    Write-Host "  Batch mode exige instância única — feche a Unity e rode de novo."
    Write-Host "  (Nada foi executado, então nada mudou no projeto.)"
    Write-Host ""
    Write-Host "TESTS FAILED"
    exit 1
}

# ── 2. Acha o Editor ───────────────────────────────────────────────────────────────────
$unityEditor = Find-UnityEditor
if (-not $unityEditor) {
    Write-Host "  ERRO: Unity Editor não encontrado em C:\Program Files\Unity\Hub\Editor" -ForegroundColor Red
    Write-Host "TESTS FAILED"
    exit 1
}

# ── 3. Prepara os caminhos ─────────────────────────────────────────────────────────────
$resultsDir = Join-Path $ProjectPath "TestResults"
if (-not (Test-Path $resultsDir)) {
    New-Item -ItemType Directory -Path $resultsDir -Force | Out-Null
}

$resultsFile = Join-Path $resultsDir "${TestPlatform}Results.xml"
$logFile = Join-Path $resultsDir "unity_${TestPlatform}.log"

# O XML antigo TEM de sair antes: sem isso, uma rodada que nem chegou a compilar deixaria
# o resultado da rodada anterior no lugar, e ele seria lido como se fosse desta.
if (Test-Path $resultsFile) { Remove-Item $resultsFile -Force }
if (Test-Path $logFile) { Remove-Item $logFile -Force }

Write-Host "  Rodando $TestPlatform..." -NoNewline
$inicio = Get-Date

Set-Location $ProjectPath

# -logFile manda a saída para arquivo em vez do console. Redirecionar com `*>` do
# PowerShell grava em UTF-16 e foi exatamente isso que tornou o log ilegível em 2026-08-27.
# -nographics e o padrao e economiza muito tempo. Mas SEM graficos a Unity pode nao
# carregar metricas de fonte, e ai todo 'preferredWidth' devolve 0 -- o que faria os testes
# de layout passarem verdes sem medir nada. O LayoutDaUiTests.AsMedidasFuncionam detecta
# isso e manda rodar com -ComGraficos.
$graficos = if ($ComGraficos) { @() } else { @("-nographics") }

& "$unityEditor" -runTests -batchmode @graficos -projectPath "." `
    -testPlatform $TestPlatform -testResults "$resultsFile" -logFile "$logFile" | Out-Null

$exitCode = $LASTEXITCODE
$duracao = [Math]::Round(((Get-Date) - $inicio).TotalSeconds)
Write-Host " ($duracao s)"

$linhas = Read-UnityLog -Path $logFile

# ── 4. Classifica ──────────────────────────────────────────────────────────────────────

# 4a. Colisão de instância detectada pelo próprio Unity (o Editor abriu no meio).
if (Test-ColisaoDeInstancia -Linhas $linhas) {
    Write-Host ""
    Write-Host "  EDITOR ABERTO (a Unity acusou a colisão)" -ForegroundColor Yellow
    Write-Host "  Feche a Unity e rode de novo."
    Write-Host ""
    Write-Host "TESTS FAILED"
    exit 1
}

# 4b. Erro de compilação. Vem ANTES da leitura do XML porque, sem compilar, o XML nem
#     existe — e "arquivo não encontrado" não é o diagnóstico útil.
$erros = Get-ErrosDeCompilacao -Linhas $linhas
if ($erros.Count -gt 0) {
    Write-Host ""
    Write-Host "  ERRO DE COMPILAÇÃO — $($erros.Count) distinto(s)" -ForegroundColor Red
    Write-Host ""
    foreach ($e in $erros | Select-Object -First 15) {
        Write-Host "    $e"
    }
    if ($erros.Count -gt 15) {
        Write-Host "    ... e mais $($erros.Count - 15). Log completo: $logFile"
    }
    Write-Host ""
    Write-Host "TESTS FAILED"
    exit 1
}

# 4c. Sem XML e sem erro de compilação: algo fora dos casos conhecidos.
if (-not (Test-Path $resultsFile)) {
    Write-Host ""
    Write-Host "  SEM RESULTADOS, E SEM ERRO DE COMPILAÇÃO" -ForegroundColor Red
    Write-Host "  Unity saiu com código $exitCode. Isto não se encaixa em nenhum caso conhecido."
    Write-Host "  Últimas linhas do log:"
    Write-Host ""
    foreach ($l in ($linhas | Where-Object { $_.Trim() -ne "" } | Select-Object -Last 15)) {
        Write-Host "    $l"
    }
    Write-Host ""
    Write-Host "  Log completo: $logFile"
    Write-Host "TESTS FAILED"
    exit 1
}

# 4d. Tem XML: lê o resultado de verdade.
[xml]$results = Get-Content $resultsFile -Encoding UTF8
$run = $results.'test-run'

$falharam = [int]$run.failed
$erros_ = [int]$run.errors

if ($falharam -eq 0 -and $erros_ -eq 0) {
    Write-Host ""
    Write-Host "  VERDE — $($run.total) testes, $($run.passed) passando, $($run.skipped) aposentados" -ForegroundColor Green
    Write-Host ""
    Write-Host "TESTS PASSED"
    exit 0
}

Write-Host ""
Write-Host "  TESTES FALHARAM — $falharam de $($run.total)" -ForegroundColor Red
Write-Host ""

foreach ($t in $results.SelectNodes("//test-case[@result='Failed']")) {
    Write-Host "    $($t.fullname)" -ForegroundColor Red

    # .InnerText, e nao .message direto: no formato NUnit3 a mensagem vem dentro de um
    # CDATA, entao a propriedade devolve um XmlElement -- e o Write-Host imprimia
    # "System.Xml.XmlElement" no lugar do diagnostico. Aconteceu em 2026-08-27, e uma
    # ferramenta de QA que esconde o motivo da falha e pior que nao ter ferramenta.
    $texto = $null
    if ($t.failure -and $t.failure.message) { $texto = $t.failure.message.InnerText }
    if ([string]::IsNullOrWhiteSpace($texto) -and $t.failure) { $texto = $t.failure.InnerText }

    if (-not [string]::IsNullOrWhiteSpace($texto)) {
        # So as tres primeiras linhas: as assercoes deste projeto explicam a consequencia em
        # jogo, e o paragrafo inteiro afogaria a lista quando ha varias falhas.
        $msg = ($texto -split "`r?`n" | Where-Object { $_.Trim() -ne "" } | Select-Object -First 3)
        foreach ($m in $msg) { Write-Host "      $($m.Trim())" }
    }
    Write-Host ""
}

if ($Detalhado) {
    Write-Host "  Log da Unity: $logFile"
}

Write-Host "TESTS FAILED"
exit 1
