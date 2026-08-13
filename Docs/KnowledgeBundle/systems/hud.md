---
type: Game System
title: HUD — o ponto único de montagem
description: As seis views do HUD de gameplay, o HUDController como injetor, e por que "cada cena montava o HUD do seu jeito" era o bug real.
tags: [ui, hud, editor-tools]
---

# HUD

## As seis views

`HUDController` (`Assets/Scripts/UI/HUDController.cs`) é o ponto de injeção: o `GameManager`
cria/acha as POCOs e Bridges de Damião no bootstrap e as repassa via seis métodos `Injetar*`,
cada um ligando uma view por `Bind()`.

| View | Fonte | Evento |
|---|---|---|
| `ResilienciaBar` | `ResilienciaMental` (Core) | `OnChanged` |
| `VitalidadeBar` | `Vitalidade` (Core) | `OnChanged` |
| `VigorBar` | `GerenciadorDeVigor` (Player) | `OnVigorChanged` + `OnExaustaoChanged` |
| `BarraDeAcoes` | `MaoFisicaBridge` (Player) | `OnArmaTrocada` |
| `BarraDeItens` | `InventoryManager.Instance` (exceção — ver abaixo) | `Main.OnSlotChanged` |
| `BarraDeArtefatos` | `ArtefatosBridge` (Player) | `OnArtefatosMudaram` |

Contrato (`Assets/Scripts/UI/CLAUDE.md`): view só lê estado e reage a evento C#, nunca faz
polling fora de animação visual (Lerp de fill), nunca localiza por `Find`/`FindObjectOfType`.

### `BarraAnimada<TFonte>`: a base das três barras de recurso

`ResilienciaBar`, `VitalidadeBar` e `VigorBar` herdam de `BarraAnimada<TFonte>`
(`Assets/Scripts/UI/BarraAnimada.cs`), que concentra o que era idêntico: os campos de
`fillImage`/`backgroundImage`/`velocidadeLerp`, o Lerp do fill no `Update`, e o ciclo
`Bind`/`Unbind` com `OnDisable → Unbind` (nunca deixa handler pendurado). A extração tirou
**243 linhas das três e devolveu 50** — a base tem ~130.

O que **não** subiu para a base, de propósito, é a **política de cor**. Foi ela que desmentiu a
alegação de "~80% de código duplicado" de um roadmap externo: o núcleo realmente comum eram
~40–50%, e o miolo restante muda de gatilho em cada barra — flags de transição no payload
(`ResilienciaChangedArgs.EntrouEmPanico`…), limiar local comparado ao percentual (Vitalidade),
booleano de evento dedicado (`OnExaustaoChanged`). Cada subclasse implementa quatro pontos:
`Inscrever`/`Desinscrever` no(s) evento(s), `PercentualAtual` (para sincronizar o fill no `Bind`)
e `AtualizarCor`.

Regra que a base impõe: `AtualizarCor` lê o estado **ao vivo** da `Fonte`, nunca um booleano
cacheado do payload. Assim o mesmo método serve tanto ao `Bind` (antes do primeiro evento)
quanto aos handlers — e some o estado duplicado que cada barra guardava por conta própria.

**A `BarraDeItens` é a exceção deliberada:** lê `InventoryManager.Instance` direto e captura
teclado (`Keyboard.current`, Input System novo) no próprio `Update`, em vez de receber a fonte
por injeção — o `GameManager.cs` documenta essa escolha explicitamente. Diverge do padrão, mas
não é o problema que este documento resolve.

## O bug real não era código faltando — era wiring

Até 2026-08-13, cada peça do HUD vinha de uma ferramenta de Editor com **lista de cenas
própria**: `MontarBarraDeItens`, `MontarPainelDeInventario`, e uma `MontarBarraDeArtefatos` que
só existia dentro de `MontarArenaDeTestes`. `BuildHUDCompleto` montava só três das seis views.
Resultado: **nenhuma cena tinha HUD completo.**

| View | Arena | Deserto | Santuário | Playtest |
|---|---|---|---|---|
| `BarraDeAcoes` (arma empunhada) | ✅ | ❌ | ❌ | ✅ |
| `BarraDeArtefatos` (F1–F4) | ✅ | ❌ | ❌ | ❌ |
| `VigorBar` | ❌ | ❌ | ❌ | ❌ |

Em duas fases reais o jogador não via qual arma empunhava nem os artefatos. A `VigorBar` nunca
foi instanciada em cena ou prefab nenhum — e `HUDController.InjetarVigor` ligava em `null` **sem
avisar**, o único `Injetar*` sem `Debug.LogError` no caso nulo. Nada no console apontava a causa.

Esta não foi a primeira vez nesta sessão que "existe no C# e não está ligado em lugar nenhum"
apareceu: a ficha que não carregava do disco, o `ArtefatoDef.Item` que ninguém lia, o
`ByakheeAI.IniciarLuta()` que ninguém chamava, a `ArtefatosBridge` ausente da Arena, o
`TilemapCollider2D` sem geometria por causa de um tile com `colliderType None`. É o modo de
falha dominante do projeto — e por isso a resposta aqui não foi só consertar o wiring, foi
também travar um guarda contra ele voltar.

## `BuildHUDCompleto`: o ponto único de montagem

`Assets/FavelaAmarela/Editor/BuildHUDCompleto.cs` monta as **seis** views e liga os seis campos
do `HUDController`, mais a barra de itens e o painel de inventário (que se autoligam via
`MontarBarraDeItens.MontarNaCenaAberta()`/`MontarPainelDeInventario.MontarNaCenaAberta()`).
Idempotente: acha o `HUDController` existente (inclusive dentro de uma instância do prefab
`HUD_ResilienciaBar`, que só liga duas das seis views) e só completa o que falta.

Dois pontos de entrada:
- `Tools/FavelaAmarela/Build HUD Completo (cena aberta)` — a cena que estiver aberta.
- `Tools/FavelaAmarela/Build HUD Completo em todas as cenas de jogo` — percorre Deserto,
  Playtest e Santuário em sequência.

`MontarArenaDeTestes` não monta mais HUD por conta própria — chama só `BuildHUDCompleto.Build()`.
Não existe mais "HUD da Arena" separado do HUD do jogo.

## O guarda: `HudCompletoTests`

`Assets/Tests/EditMode/HudCompletoTests.cs` trava duas propriedades, lendo o YAML de cenas e
prefabs em vez de abrir a cena no Editor (mesma técnica de `FichaAtributosAssetsTests`):

1. **Toda cena de HUD tem os seis campos do `HUDController` ligados** (nenhum `fileID: 0`).
2. **Nenhum script de `Assets/Scripts/UI/` fica órfão** — 0 ocorrências em qualquer cena/prefab
   — sem estar numa lista explícita de exceções com motivo documentado (`ScreenFader`, hoje:
   dormente, porque os dois consumidores dele também não estão instanciados em cena alguma).

### A complicação que valeu a pena entender

Um `HUDController` que vem de uma instância de prefab **sem nenhum campo sobrescrito** não tem
bloco de componente duplicado na cena — a Unity só serializa o que diverge do prefab. Um campo
sobrescrito vira uma entrada em `PrefabInstance.m_Modification.m_Modifications`, referenciando o
componente pelo `fileID` **dentro do prefab**, não pelo guid do script. Um grep simples pelo guid
do `HUDController` não encontra nada em três das quatro cenas do VS — não porque o componente não
exista, mas porque esse é o formato real de override da Unity.

O guarda resolve os dois casos: acha o componente direto na cena (`Caso A`) ou, se não achar,
localiza o prefab que o contém, lê o baseline de campos do `.prefab`, e aplica por cima qualquer
override da cena (`Caso B`). **O valor efetivo é: o valor do prefab, substituído pelo override
da cena se houver um.**

### Validado por mutação, não só por ficar verde

Zerar de propósito o override de `vigorBar` em `Deserto_Hali.unity` fez o teste falhar,
citando exatamente a cena e o campo — depois revertido. Um guarda que só fica verde não prova
nada; é preciso ver que ele acende quando o bug volta.

## Prioridades revisadas de um roadmap anterior

Um documento de design propôs Barra de Chefe e ícones de status ailment como 🔴 bloqueantes.
Verificado contra o código:

- **Barra de Chefe:** só faz sentido para o **Byakhee** — o Rei em Amarelo não tem `Vitalidade`
  nem `IDanificavel` por design (ver [boss_rei_em_amarelo.md](boss_rei_em_amarelo.md)), então uma
  barra de vida para ele é impossível, não bloqueante.
- **Status ailments:** Sangramento é 100% jogador→inimigo (só o Estilete o produz). Congelamento
  é 100% Abdul→jogador (`ConeDeGelo.cs`). Nenhum chefe do VS (Byakhee, Rei) aplica qualquer um dos
  dois no jogador — um indicador no HUD do jogador só faria sentido para a luta do Abdul.
- **"RM regenera +10/s sob o Poste de Luz":** falso. `RefugioDeLuz` cura 100 de Resiliência de
  uma vez + 40% da Vitalidade, com 30s de cooldown — evento único, não regeneração contínua.
- **"As 3 barras de recurso compartilham ~80% do código":** o núcleo comum
  (`ResilienciaBar`/`VitalidadeBar`/`VigorBar`) é ~35–40 linhas = 40–50%. A política de cor — o
  miolo — é estruturalmente diferente nas três.

Nenhum desses itens entrou nesta rodada; ficam como escopo futuro, corrigidos de prioridade.
