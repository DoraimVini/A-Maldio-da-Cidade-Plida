---
type: Game System
title: Boss Rei em Amarelo — O Rito no Trono de Aldebaran
description: O confronto final. Sem barra de vida — duas metades, ritual de relíquias sem pressão e selamento em ciclos de reação pura, onde errar mata na hora.
tags: [boss, rei-em-amarelo, combate, castelo, final]
---

# Boss Rei em Amarelo

> **Status:** Core e Runtime implementados em 2026-08-11. **Falta prefab, arte, o Trono de
> Aldebaran em cena de verdade, e uma fonte jogável para a Coroa de Ossos.** Item 12 da lista
> do edital. Design em
> [level_design_castelo_carcosa.md](level_design_castelo_carcosa.md) §Z5.

## Não é uma luta — é um rito que se sobrevive

> **Não há barra de vida.** `ReiEmAmareloAI` não tem `EnemyBase`, `Vitalidade` nem
> `IDanificavel` — de propósito. O par de referência não é `CultistaAI`, é mais perto de
> `ColapsoTrigger`/`CoisaDoCemiterioAI`: algo que mata instantaneamente, e cuja "vitória" é um
> evento, não uma barra chegando a zero.

Duas metades, naturezas opostas:

1. **Ritual das relíquias** — sem pressão, sem relógio. O jogador ativa cada relíquia exigida
   num `PontoFocalDeReliquia` da arena (interação deliberada, botão E).
2. **Selamento** — o Rei se desvela em ciclos. Cada um é um teste de reação puro: dar as
   costas a tempo sobrevive o ciclo; não dar, `Colapso` instantâneo.

## O ritual das relíquias

`PontoFocalDeReliquia` implementa `IInteragivel` (mesmo contrato do `BauDaTumba`). Ao
interagir, checa `ArtefatosBridge.Inventario.Contem(artefatoId)` — só ativa se o jogador
**já tiver a relíquia equipada**. O ponto nunca entrega nada, só confirma.

Todas as relíquias ativas dispara `Selando` automaticamente (`ReiEmAmareloFSM.AtivarReliquia`).

## A lista de relíquias exigidas é dado, não constante

O design pede 4 (Anel do Sinal Amarelo, Coroa de Ossos, Patuá das Luas Gêmeas, Necronomicon),
mas **a Coroa de Ossos não tem fonte jogável** — seria drop do Nagaraja, no Templo da Serpente,
que não tem cena. `ReiEmAmareloFSM` recebe a lista de ids exigidos no construtor (como
`TabelaDeDrop` recebe entradas), em vez de hardcodar os 4. `ReiEmAmareloAI` hoje exige só 3
(`necronomicon`, `patua_luas_gemeas`, `anel_sinal_amarelo`) — trocar para os 4 reais é uma
alteração de um array serializado no Inspector, não de código, assim que a Coroa tiver fonte.

## A Mecânica da Máscara Pálida

Ao se desvelar (`Desvelado`), o jogador tem **1,5 s** (único número que o design doc realmente
especifica) para estar de costas para o Rei. `DetectorDeCostas.EstaDeCostas` decide isso por
produto escalar entre a direção do olhar (`PlayerMovement.LookDirection`) e o vetor
jogador→Rei: `-1` seria alinhamento perfeito (impossível de acertar por input), `0` aceitaria
só um perfil de lado. O padrão, `-0,5`, aceita ~60° de desvio da direção oposta ao Rei —
folgado o bastante para não parecer injusto, apertado o bastante para "de lado" não contar
como "de costas".

**De costas salva assim que acontece**, não precisa se manter até o fim da janela — é reflexo
pontual, coerente com "1,5 s para reagir" em vez de "1,5 s parado".

Sobreviver todos os ciclos de desvelar (default 3, com 6 s de calmaria entre eles) sela o Rei —
vitória. Falhar um único ciclo é derrota instantânea, via
`GameManager.Instance.Resiliencia.ForcarColapso()` (mesmo mecanismo do `ColapsoTrigger`).

## Por que ciclos e intervalo não foram calibrados por simulação (diferente do Byakhee)

O Byakhee é uma corrida de DPS — dá para simular um "jogo perfeito" e tirar números reais de
RM gasta. O selamento do Rei é mecânica de **reação pura**: não existe "jogo perfeito"
significativo para um teste de reflexo de 1,5 s, só existe "o jogador reagiu ou não". Os
defaults (3 ciclos, 6 s de intervalo) não estão no design doc — são ponto de partida para
calibrar **ao vivo**, na `Cena_ArenaDeTestes`, e não por simulação externa.

## Arquitetura

| Peça | Camada |
|---|---|
| `ReiEmAmareloFSM`, `ReiEmAmareloState` | Core (POCO, 13 testes) |
| `DetectorDeCostas` | Core (POCO, geometria pura, 7 testes) |
| `ReiEmAmareloAI` | Runtime — sem `EnemyBase`/`Vitalidade`; liga o `Tick`, computa "de costas", aplica Colapso, expõe vitória |
| `PontoFocalDeReliquia` | Runtime — `IInteragivel`, checa posse real da relíquia antes de ativar |

## Infraestrutura de teste (2026-08-11)

Sem a Coroa de Ossos jogável, não havia como testar o rito de ponta a ponta dentro do fluxo
real do jogo. Duas ferramentas resolvem isso **sem fabricar uma fonte de drop que não
existe**:

- **`CarcosaDebuggerWindow`** (`Tools/FavelaAmarela/Carcosa Debugger`, primeira `EditorWindow`
  do projeto, Play-Mode-only): concede os 4 artefatos (inclui a Coroa), concede+equipa as 3
  armas da Tumba, invoca Byakhee e Rei em Amarelo sob demanda (o corpo é montado em runtime —
  nenhum dos dois tem prefab ainda), e mostra o estado ao vivo da FSM de qualquer um dos dois
  chefes presentes na cena.
- **`Cena_ArenaDeTestes`** (`Tools/FavelaAmarela/Montar Arena de Testes`): chão neutro,
  `GameManager`, Damião, câmera isométrica e HUD completo. Cena de dev, **deliberadamente fora
  do Build Settings** — nunca vai para um build de jogador.

## Prefab e sprite emprestado (2026-08-12)

`ReiEmAmarelo.prefab` existe (`Tools/FavelaAmarela/Montar Prefab do Rei em Amarelo`), com
`ReiEmAmarelo_Placeholder.png` — um frame isolado (recorte por canal alfa, sem redesenho) do
spritesheet "Necromancer" já presente na Inbox desde a rodada anterior. Não é a arte final
(cores erradas, sem a Máscara Pálida), mas é o mesmo arquétipo visual — figura encapuzada e
sinistra com cajado — e tira o Rei do quadrado colorido. O `CarcosaDebuggerWindow` agora
instancia este prefab ao invocar o Rei, com o corpo construído em runtime como fallback só se
o prefab for removido.

## Pendente
- **Arte final** — cores certas (amarelo/pálido), a Máscara Pálida.
- **O Trono de Aldebaran em cena de verdade** — o Castelo (item 11) ainda não existe.
- **Fonte jogável para a Coroa de Ossos** — depende do Templo da Serpente/Nagaraja terem cena.
- Trocar a lista de relíquias exigidas de `ReiEmAmareloAI` de 3 para as 4 reais assim que a
  Coroa tiver fonte.
- Calibrar ciclos/intervalo do selamento na Arena de Testes com playtest real.

## Relacionados
- [Boss Byakhee](boss_byakhee.md) — o outro chefe do VS; contraste DPS-simulável vs. reação pura
- [Artefatos](artefatos.md) — os 4 slots e a regra "só vale o que está equipado"
- [Interação com o Mundo (botão E)](interacao.md) — o contrato `IInteragivel` que `PontoFocalDeReliquia` implementa
- [Resiliência Mental](resiliencia_mental.md) — o recurso que o Colapso zera
