<#
.SYNOPSIS
    Roda os testes EditMode do projeto "A Maldição da Cidade Pálida".
    Imprime "TESTS PASSED" ou "TESTS FAILED" para consumo da skill favela-qa-pipeline.

.DESCRIPTION
    Localiza o Unity Editor instalado pelo Unity Hub, roda os testes em batch mode e
    interpreta o XML de resultados. Falha se o Editor estiver aberto (batch mode exige
    instância única) — feche o Unity antes de rodar.
#>

param(
    [string]$TestPlatform = "EditMode"
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# Raiz do projeto = pasta acima de Tools/. Derivado do próprio caminho do script para
# sobreviver a mudanças de pasta — o caminho fixo anterior apontava para a pasta que o
# Google Drive apagou em 2026-08-10.
$ProjectPath = Split-Path -Parent $PSScriptRoot

# --- Localiza o Unity Editor ---
$unityHubEditorPath = "C:\Program Files\Unity\Hub\Editor"
$unityEditor = $null

if (Test-Path $unityHubEditorPath) {
    $versions = Get-ChildItem -Path $unityHubEditorPath -Directory | Sort-Object Name -Descending
    foreach ($v in $versions) {
        $candidate = Join-Path $v.FullName "Editor\Unity.exe"
        if (Test-Path $candidate) {
            $unityEditor = $candidate
            Write-Host "[QA] Unity Editor encontrado: $unityEditor"
            break
        }
    }
}

if (-not $unityEditor) {
    Write-Host "[QA] ERRO: Unity Editor não encontrado. Confira a instalação do Unity Hub."
    Write-Host "TESTS FAILED"
    exit 1
}

# --- Prepara o caminho dos resultados ---
$resultsDir = Join-Path $ProjectPath "TestResults"
if (-not (Test-Path $resultsDir)) {
    New-Item -ItemType Directory -Path $resultsDir -Force | Out-Null
}
$resultsFile = Join-Path $resultsDir "EditModeResults.xml"

if (Test-Path $resultsFile) {
    Remove-Item $resultsFile -Force
}

Write-Host "[QA] Rodando testes $TestPlatform..."
Write-Host "[QA] Projeto:    $ProjectPath"
Write-Host "[QA] Resultados: $resultsFile"

Set-Location $ProjectPath
& "$unityEditor" -runTests -batchmode -nographics -projectPath "." -testPlatform $TestPlatform -testResults "$resultsFile" | ForEach-Object { Write-Host $_ }

$exitCode = $LASTEXITCODE
Write-Host "[QA] Unity saiu com código: $exitCode"

# --- Interpreta os resultados ---
if (Test-Path $resultsFile) {
    [xml]$results = Get-Content $resultsFile -Encoding UTF8
    $testRun = $results.'test-run'

    Write-Host ""
    Write-Host "============================================"
    Write-Host "  RESULTADO DOS TESTES"
    Write-Host "============================================"
    Write-Host "  Total:     $($testRun.total)"
    Write-Host "  Passaram:  $($testRun.passed)"
    Write-Host "  Falharam:  $($testRun.failed)"
    Write-Host "  Erros:     $($testRun.errors)"
    Write-Host "  Pulados:   $($testRun.skipped)"
    Write-Host "============================================"
    Write-Host ""

    if ([int]$testRun.failed -eq 0 -and [int]$testRun.errors -eq 0) {
        Write-Host "TESTS PASSED"
        exit 0
    }

    foreach ($test in $results.SelectNodes("//test-case[@result='Failed']")) {
        Write-Host "[FAIL] $($test.fullname)"
        if ($test.failure.message) { Write-Host "       $($test.failure.message)" }
    }
    Write-Host "TESTS FAILED"
    exit 1
}

Write-Host "[QA] ERRO: arquivo de resultados não encontrado em: $resultsFile"
Write-Host "[QA] O Unity provavelmente teve erro de compilação (ou o Editor está aberto)."
Write-Host "TESTS FAILED"
exit 1
