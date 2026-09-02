<#
.SYNOPSIS
    Reimpõe o padrão de tipografia de diálogo NO DISCO. Roda sempre por último.

.DESCRIPTION
    O problema, medido cinco vezes na branch develop_temple: toda ferramenta de Editor que
    abre o HUD_Gameplay.prefab (ou uma cena com caixa de diálogo) em batch mode reescreve
    m_MaxSize de 44 para 60 ao salvar. A Unity serializa o valor que está no cache de
    artefatos da Library, não o que está no arquivo — e o cache guarda o default do
    componente, não a nossa calibragem.

    Escrever pela Unity NÃO resolve: o PadronizarTextoDoHud faz exatamente isso, lê de volta
    44 em memória, e o disco continua 60. O único conserto que gruda é editar o arquivo
    depois de a Unity ter fechado.

    Por isso este passo é PowerShell puro, sem abrir a Unity, e é chamado no fim do
    run_editor_tool.ps1. Ele é idempotente: rodar num projeto já certo não muda nada e não
    imprime nada.

.PARAMETER Silencioso
    Não imprime quando não há nada a corrigir. É como o run_editor_tool.ps1 o chama.

.NOTES
    A REGRA VEM DO CÓDIGO, não daqui: PadraoDeTextoDeDialogo.TamanhoMinimo/TamanhoMaximo são
    lidos do fonte C#. Duplicar os números neste script criaria a terceira cópia da mesma
    constante, e a divergência silenciosa é o defeito que este projeto mais cataloga.

    E O ALVO É DERIVADO, não por nome: segue a referência do campo `texto` de quem escreve
    diálogo (TutorialHintUI e PainelDeEscolha), igual ao TipografiaDeDialogoTests. Os objetos
    se chamam todos "Texto"; casar por nome pegaria o texto errado.
#>

param(
    [switch]$Silencioso
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$ProjectPath = Split-Path -Parent $PSScriptRoot
$Assets = Join-Path $ProjectPath "Assets"

# ── A regra, lida do C# ────────────────────────────────────────────────────────────────
$padraoCs = Join-Path $Assets "Scripts\UI\PadraoDeTextoDeDialogo.cs"
if (-not (Test-Path $padraoCs)) {
    Write-Host "  TIPOGRAFIA: PadraoDeTextoDeDialogo.cs não existe — nada a impor." -ForegroundColor Yellow
    exit 0
}

$fonte = Get-Content -Raw -Encoding UTF8 $padraoCs
$mMin = [regex]::Match($fonte, 'TamanhoMinimo\s*=\s*(\d+)')
$mMax = [regex]::Match($fonte, 'TamanhoMaximo\s*=\s*(\d+)')

if (-not $mMin.Success -or -not $mMax.Success) {
    Write-Host "  TIPOGRAFIA: não achei TamanhoMinimo/TamanhoMaximo no C# — nada foi tocado." -ForegroundColor Yellow
    exit 0
}

$minimo = [int]$mMin.Groups[1].Value
$maximo = [int]$mMax.Groups[1].Value

# ── Quem escreve diálogo ───────────────────────────────────────────────────────────────
function Get-Guid-Do([string]$caminhoDoScript) {
    $meta = "$caminhoDoScript.meta"
    if (-not (Test-Path $meta)) { return $null }
    $m = [regex]::Match((Get-Content -Raw -Encoding UTF8 $meta), 'guid:\s*([0-9a-f]{32})')
    if ($m.Success) { return $m.Groups[1].Value }
    return $null
}

$guidDica    = Get-Guid-Do (Join-Path $Assets "Scripts\UI\TutorialHintUI.cs")
$guidEscolha = Get-Guid-Do (Join-Path $Assets "Scripts\UI\PainelDeEscolha.cs")

if (-not $guidDica -and -not $guidEscolha) {
    Write-Host "  TIPOGRAFIA: nem TutorialHintUI nem PainelDeEscolha têm .meta — nada a impor." -ForegroundColor Yellow
    exit 0
}

# ── Varre cenas e prefabs ──────────────────────────────────────────────────────────────
$arquivos = @()
$arquivos += Get-ChildItem -Path $Assets -Filter *.unity  -Recurse -File
$arquivos += Get-ChildItem -Path $Assets -Filter *.prefab -Recurse -File

# ── A varredura ────────────────────────────────────────────────────────────────────────
#
# Opera no texto inteiro, documento a documento, SEM remontar o arquivo. Remontar exigiria
# reconstruir a linha de cabecalho de cada documento, e a CLASSE do documento se perde num
# Split que so captura a ancora. Reescrever com a classe errada corrompe a cena inteira.
$corrigidos = @()

foreach ($arq in $arquivos) {
    $yaml = Get-Content -Raw -Encoding UTF8 $arq.FullName

    $temDica    = $guidDica    -and $yaml.Contains($guidDica)
    $temEscolha = $guidEscolha -and $yaml.Contains($guidEscolha)
    if (-not $temDica -and -not $temEscolha) { continue }

    # Cada documento, com o cabeçalho INTACTO.
    $docs = [regex]::Matches($yaml, '(?m)^--- !u!\d+ &(\d+)(?: stripped)?\r?\n(?:(?!^--- ).*\r?\n?)*')

    $alvos = New-Object System.Collections.Generic.HashSet[string]
    foreach ($d in $docs) {
        $corpo = $d.Value
        $ehDono = ($temDica -and $corpo.Contains($guidDica)) -or
                  ($temEscolha -and $corpo.Contains($guidEscolha))
        if (-not $ehDono) { continue }
        $t = [regex]::Match($corpo, '(?m)^\s*texto:\s*\{fileID:\s*(-?\d+)\}')
        if ($t.Success -and $t.Groups[1].Value -ne "0") { [void]$alvos.Add($t.Groups[1].Value) }
    }

    if ($alvos.Count -eq 0) { continue }

    $novo = $yaml
    $mudou = $false

    # De trás para a frente: substituir por offset invalida os offsets seguintes.
    for ($k = $docs.Count - 1; $k -ge 0; $k--) {
        $d = $docs[$k]
        if (-not $alvos.Contains($d.Groups[1].Value)) { continue }
        if (-not $d.Value.Contains("UnityEngine.UI.Text")) { continue }

        $corpo = $d.Value
        $original = $corpo

        $mMinAtual = [regex]::Match($corpo, '(?m)^(\s*)m_MinSize:\s*(\d+)\s*$')
        if ($mMinAtual.Success -and [int]$mMinAtual.Groups[2].Value -ne $minimo) {
            $corpo = [regex]::Replace($corpo, '(?m)^(\s*)m_MinSize:\s*\d+\s*$', "`${1}m_MinSize: $minimo")
            $corrigidos += "$($arq.Name): m_MinSize $($mMinAtual.Groups[2].Value) -> $minimo"
        }

        $mMaxAtual = [regex]::Match($corpo, '(?m)^(\s*)m_MaxSize:\s*(\d+)\s*$')
        if ($mMaxAtual.Success -and [int]$mMaxAtual.Groups[2].Value -ne $maximo) {
            $corpo = [regex]::Replace($corpo, '(?m)^(\s*)m_MaxSize:\s*\d+\s*$', "`${1}m_MaxSize: $maximo")
            $corrigidos += "$($arq.Name): m_MaxSize $($mMaxAtual.Groups[2].Value) -> $maximo"
        }

        if ($corpo -ne $original) {
            $novo = $novo.Remove($d.Index, $d.Length).Insert($d.Index, $corpo)
            $mudou = $true
        }
    }

    if ($mudou) {
        # Sem BOM: é como a Unity grava, e um BOM a mais vira diff em todo o arquivo.
        $semBom = New-Object System.Text.UTF8Encoding($false)
        [System.IO.File]::WriteAllText($arq.FullName, $novo, $semBom)
    }
}

if ($corrigidos.Count -eq 0) {
    if (-not $Silencioso) { Write-Host "  TIPOGRAFIA: já no padrão ($minimo-$maximo)." }
    exit 0
}

Write-Host ""
Write-Host "  TIPOGRAFIA REIMPOSTA — $($corrigidos.Count) campo(s)" -ForegroundColor Yellow
foreach ($c in $corrigidos) { Write-Host "    $c" }
Write-Host "  (a Unity reverte isto ao salvar em batch mode; este passo roda depois dela)"
Write-Host ""
exit 0
