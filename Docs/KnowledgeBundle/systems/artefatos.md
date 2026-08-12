---
type: Game System
title: Artefatos — Inventário de 4 Slots, Passivas e a Barra F1–F4
description: As relíquias com passiva e habilidade próprias. Quatro slots, uma habilidade por Artefato, e a regra de que só vale o que está equipado.
tags: [artefatos, reliquias, habilidades, inventario, hud]
---

# Artefatos

> **Status:** Implementado em 2026-08-11. Motor, UI e os 4 Artefatos autorados. Falta
> **wiring de cena** (ver o fim deste documento) e **persistência dos slots no save**.

Um **Artefato** é uma relíquia que carrega **uma passiva e uma habilidade ativa própria**. Não
é arma nem armadura: não disputa espaço de corpo, não tem `EquipmentSlot`. Vive num inventário
separado, de **quatro slots**.

## A regra central: só vale o que está equipado

> **Coletar não basta. Um Artefato só concede passiva e habilidade enquanto ocupa um dos
> quatro slots.**

Isso **substitui** a regra anterior do [loot_e_drop.md](loot_e_drop.md) ("sempre ativo assim que
coletado"), por decisão do Vini em 2026-08-11. O motivo é escala: o desenho prevê **mais
Artefatos do que quatro**. Com ativação automática, o quinto Artefato quebraria o sistema; com
quatro slots, ele vira **escolha** — carregar um custa deixar outro para trás.

Consequência de UI: a barra de Artefatos tem exatamente quatro posições (F1–F4), uma por slot,
e é leitura direta do inventário.

## Os quatro Artefatos

| Artefato | Fonte | Passiva | Habilidade (ativa) |
|---|---|---|---|
| **Necronomicon** | Abdul Alhazred (Tumba) | `TraumaAnomalia +15` | **Recitar o Aklo** — revela entidades num raio de 10, por 6 s. Custa 12 de RM, recarrega em 25 s |
| **Patuá das Luas Gêmeas** | Quest da Cassilda | `RegenRM +1.5` | **Canção de Cassilda** — Ancoragem imediata de 25 de RM. Sem custo, recarrega em 45 s |
| **Anel do Sinal Amarelo** | Byakhee (Portões das Ruínas) | `Furtividade +0.3`, `RMMaxima +20` | **Resguardo do Sinal** — passos silenciosos por 8 s. Custa 8 de RM, recarrega em 30 s |
| **Coroa de Ossos** | Nagaraja (Templo da Serpente) | `DefesaAnomalia +5` | **Sibilo de Yig** — aplaca serpentinos num raio de 7, por 4 s. Custa 10 de RM, recarrega em 35 s |

**Anel do Sinal Amarelo = "Anel do Byakhee"** — mesmo item, nome único (confirmado 2026-08-10 e
2026-08-11). **Coroa de Ossos ≠ Elmo de Set**: coexistem, um é Artefato e o outro é armadura —
ver a nota de correção em [lore/reliquias_cosmicas.md](../lore/reliquias_cosmicas.md).

### Por que o custo de RM não pode colapsar Damião
`ArtefatoAtivo.PodeAtivar` exige RM **estritamente maior** que o custo. Gastar a última lasca de
Resiliência e entrar em Colapso por causa da própria habilidade seria punição sem aviso — o
jogador não tem como prever o resultado antes de apertar.

## Arquitetura

Divisão POCO/Unity do `CLAUDE.md` §2. **Não** estende a `MaoFisicaBridge`: aquela guarda um
único relógio de habilidade, e aqui são quatro recargas independentes.

| Peça | Camada | Papel |
|---|---|---|
| `IContextoDeArtefato` | Core | O que um efeito **pode** fazer no mundo. O Core declara o vocabulário; o Runtime executa |
| `IEfeitoDeArtefato` | Core | Efeito atômico. `Aplicar(ctx)` |
| `ArtefatoAtivo` | Core | A habilidade: nome, custo de RM, cooldown, duração e a lista de efeitos |
| `ResultadoDeArtefato` | Core (`readonly struct`) | O que o adaptador precisa cobrar |
| `InventarioDeArtefatos` | Core | Os 4 slots. Guarda **ids**, recusa duplicata |
| `ArtefatoDef` | Data (SO) | Dado autorado: passivas + bloco da ativa. `CriarAtivo()` monta o POCO |
| `ArtefatosBridge` | Runtime | Dona do inventário, dos 4 cooldowns e do contexto concreto |
| `MarcadorDeRevelacao` | Runtime | O sinal que paira sobre quem foi revelado |
| `BarraDeArtefatos` | UI | Os 4 slots F1–F4, com recarga |

### Por que o contexto é uma interface do Core
Um efeito precisa mexer no mundo (revelar inimigo, calar passo), mas o Core não pode tocar
`UnityEngine`. Invertendo — o Core **declara** `IContextoDeArtefato` e o Runtime implementa —
os efeitos ficam testáveis com um contexto falso que só anota o que foi pedido. É a mesma
disciplina que já separa `Core/Enemies` de `Runtime/Enemies`.

### Como cada efeito se costura ao que já existia
Nenhum dos quatro exigiu sistema novo, fora a revelação:

- **Ancoragem** → `ResilienciaMental.Ancorar` (já existia).
- **Silêncio** → `PlayerMovement.SilenciarPassos`, espelhando o `MascararOdor` que já existia
  para o faro do Sseth. Gateia só o broadcast contínuo do caminhar; **a Esquiva continua fazendo
  barulho de propósito**, senão Resguardo + Esquiva viraria um apagão sonoro completo.
- **Aplacamento** → `EnemyStateMachine.Atordoar` (já existia).
- **Revelação** → peça nova. O `MarcadorDeRevelacao` é um **objeto filho próprio**, com o
  próprio `SpriteRenderer` em ordem 30000, e não uma alteração no renderer do inimigo: assim
  atravessa parede sem brigar com o `DynamicYSort`, que reescreve o `sortingOrder` do inimigo
  todo `LateUpdate`.

## Passivas: a 4ª fonte do GerenciadorEfeitosPassivos
`GetBonus(StatType)` agora soma **quatro** fontes: equipamento, itens `Chave` na mochila,
**Artefatos equipados** e Ecos da Memória. A bridge é ligada por `GameManager` via
`GerenciadorEfeitosPassivos.Bind(artefatos)`, e a barra redesenha pelo evento `OnArtefatosMudaram`.

> **O Necronomicon deixou de ser `ItemType.Chave`.** Se voltar a ser, a passiva seria contada
> **duas vezes** (uma pelo laço da mochila, outra pelo laço dos artefatos). Há um teste
> guardando isso. A `PortaDeAklo` **não quebra** com a migração: ela checa posse por `Id`
> (`PossuiItemNaMochila`), não por tipo.

## Input
Quatro ações novas (`HabilidadeArtefato1..4`, teclas **F1–F4**) no `InputSystem_Actions`,
consumidas em `PlayerMovement.Update()` pelo padrão polled do projeto (`FindAction` em `Awake`
+ `WasPressedThisFrame`). Dígitos 1–8 já são da `BarraDeItens` e Q é a habilidade da arma.

**Invocar um Artefato não trava a FSM** — diferente de golpear, não é ação exclusiva. Mas, por
entrar depois dos guardas de `Update`, herda de graça o bloqueio por diálogo (`MovimentoBloqueado`).

## Wiring de cena

**Automatizado (2026-08-11):** rodar `Tools/FavelaAmarela/Ligar sistemas novos` anexa a
`ArtefatosBridge` e a ponte de persistência dos slots ao Damião, nas 3 cenas. Idempotente.

**Ainda manual:**
1. Montar a `BarraDeArtefatos` no HUD (4 slots) e ligá-la ao campo `barraDeArtefatos` do
   `HUDController` — o `MontarHUDController.cs` ainda não a linka automaticamente.
2. Ligar os `ColetavelDeItem` dos Artefatos para chamar
   `ArtefatosBridge.EquiparNoPrimeiroSlotLivre(id)` ao recolher — **enquanto isso não for
   feito, coletar um Artefato não o coloca em nenhum slot**, e ele não faz nada.

## Relacionados
- [Habilidades de Item](habilidades_de_item.md) — `IEfeitoDeArtefato` é a primeira aplicação real daquele desenho
- [Loot e Drop](loot_e_drop.md) — de onde os Artefatos caem
- [Relíquias de Hali](reliquias_de_hali.md) — as passivas originais
- [Ficha de Atributos](ficha_de_atributos.md) — o que os modificadores afetam
