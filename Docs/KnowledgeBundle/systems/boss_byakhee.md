---
type: Game System
title: Boss Byakhee — O Cadeado dos Portões
description: A luta que fecha a Fase 1. Imune no ar, vulnerável só no pouso; a dificuldade cresce encurtando a janela, não subindo o dano.
tags: [boss, byakhee, combate, fase1, portoes]
---

# Boss Byakhee

> **Status:** jogável de ponta a ponta desde 2026-08-20 — Core, Runtime, prefab, espólio
> ligado e arena em cena. **Falta animação e arte.** Item 9 da lista do edital. Design narrativo em
> [lore/cassilda_e_byakhee.md](../lore/cassilda_e_byakhee.md) §IV.

## A inversão que define a luta

> **O Byakhee é imune no ar. A única janela de dano é o pouso.**

O jogador **não escolhe quando atacar** — ele espera, esquiva e aproveita a abertura. Toda a
FSM existe para sustentar isso, e é o que separa esta luta de um saco de pancadas voador.

Consequência de balanceamento: **a dificuldade cresce encurtando a janela, não aumentando o
dano.** Há um teste travando essa propriedade — se alguém "balancear" trocando janela menor
por golpe mais forte, `JanelaDeDano_EncurtaDaFase1ParaAFase2` cai.

## As três fases

| Fase | Vida | Padrão | Janela |
|---|---|---|---|
| 1 | 100–60% | rasante → mergulho → pouso | **2,0 s** |
| 2 | 60–30% | + grito direcionado (telegrafado 1 s, 20 de Trauma) | **1,5 s** |
| 3 | 30–0% | circunda sem pousar | **3,0 s** ao forçar o pouso |
| Frenesi | <10% | grito longo, 5 RM/s, só sai por golpe | — |

## O grito infrassônico é o relógio

Dreno **passivo** de 2 RM/s enquanto o Byakhee viver, sem precisar acertar ninguém. Quem
demora demais **colapsa sem levar um golpe**. É o que impede a estratégia de esperar
eternamente pela janela perfeita — a paciência tem preço.

No frenesi o dreno sobe para 5 RM/s: o último recurso da criatura é uma corrida contra a
sanidade, não contra a vida.

## A dependência que o design tem e o jogo não

A fase 3 prevê **cortar a asa com a Lâmina do Sinal** para forçar o pouso. **Essa arma não
existe** — as da Tumba são Maça de Aklo, Estilete de Irem e Alfanje de Alhazred.

`ByakheeFSM.CortarAsa()` está implementado e exposto em `ByakheeAI.TentarCortarAsa()` para
quando ela existir. O caminho real hoje é o **pouso espontâneo a cada 30 s**, que o próprio
design documenta como alternativa. Sem essa válvula, a fase 3 seria um impasse para todo
jogador — ninguém tem a arma.

## Ficha e espólio

| | |
|---|---|
| Vitalidade | 500 |
| Ataque (garras) | 26 |
| Defesa | 8 |
| Conjuração / Resistência Anômala | 20 / 12 |
| Drop | **Anel do Sinal Amarelo** (garantido) |

A Vitalidade alta é deliberada: com janelas de 1,5–3 s, uma vida baixa faria a luta acabar em
dois pousos e o padrão nunca se revelaria. O drop é **garantido**, não sorteado — é
progressão roteirizada, e `Garantido: 1` fura o gate de nível de propósito (ver
[loot_e_drop.md](loot_e_drop.md)).

## Balanceamento (calibrado por simulação, 2026-08-11)

> **Pedido do Vini:** equilíbrio levemente puxado para o difícil — vencível, sem ser fácil.

A primeira estimativa desta luta foi feita **de cabeça** e errou por completo: concluiu que a
luta era matematicamente impossível (RM insuficiente) quando na verdade era vencível com folga.
A causa raiz não era número nenhum — era um **bug de mecânica**: cair para a fase 3 durante um
pouso apenas *estendia* aquela janela em vez de fazer o Byakhee decolar, então a fase 3 (a
identidade da luta — "circunda sem pousar") nunca acontecia de verdade.

**Dois problemas corrigidos, um de mecânica e um de dado:**
1. `ByakheeFSM.AtualizarFracaoDeVida` agora força a decolagem para `Circundando` no instante em
   que a fase 3 começa, mesmo em pleno pouso.
2. `intervaloPousoEspontaneo` (o pouso automático da fase 3 sem a Lâmina do Sinal): 30 s → 15 s.
   `Vitalidade`: 420 → 500 (compensa a fase 3 ficar mais rápida).

**Números reais** (`ByakheeRelatorioDeBalanceamento`, jogador simulado com taxa de acerto nas
janelas de dano):

| Arma | 100% de acerto | 85% | 70% |
|---|---|---|---|
| Maça de Aklo | vence, 42% RM restante | vence, 42% | vence, 26% |
| Estilete de Irem | vence, **25%** | vence, **14%** (raspando) | **colapsa** |
| Alfanje de Alhazred | vence, 29% | vence, 29% | **colapsa** |

Jogo perfeito sempre vence com folga real, nunca trivial. Jogo mediano (85%) vence na maioria
dos casos, com o Estilete no limite — coerente com ele já ser a arma de menor dano do baú
(`armas_da_tumba.md`), não um acidente de balance. Jogo ruim (70%) perde com duas das três
armas: a punição é real.

`LutaContraByakheeTests` trava a intenção (vencível com as 3 armas, gasto real de RM, fase 3
acontecendo de fato) como regressão. `ByakheeRelatorioDeBalanceamento` imprime a tabela acima a
cada rodada de QA — nunca falha, é instrumento de leitura, não teste de regra.

## Arquitetura

| Peça | Camada |
|---|---|
| `ByakheeFSM`, `ByakheeState` | Core (POCO, 10 testes) |
| `ByakheeAI` | Runtime — move o corpo, pinta o sprite, aplica dano e dreno |
| `Ficha_Byakhee`, `Drop_Byakhee` | Assets |
| `Byakhee.prefab`, `Byakhee_Spritesheet.png` | Assets (2026-08-12) |

A imunidade em voo é aplicada pelo `ByakheeAI` ligando `EnemyBase.IgnorarDano` conforme a FSM.
A `EnemyBase` sozinha aceitaria qualquer golpe — a regra vive no POCO, o efeito no adaptador.

## Prefab e sprite real (2026-08-12)

O Vini trouxe um spritesheet animado de verdade para a Inbox
(`byakhee_v2_animated.aseprite`, 26 frames de 140×140 numa fita única, com 6 tags: Idle,
Walk, Attack, Special, Hurt, Death). `SliceSpritesheetByakhee.cs`
(`Tools/FavelaAmarela/Slice Spritesheet do Byakhee`) fatia isso em `Byakhee_Spritesheet.png`
com nomes que seguem o vocabulário da `ByakheeFSM` — `byakhee_espreita_*`,
`byakhee_rasante_*`, `byakhee_garras_*`, `byakhee_grito_*`, `byakhee_dano_*`,
`byakhee_derrota_*` — em vez dos nomes genéricos das tags do Aseprite.

As tags do arquivo original tinham o campo "to" com bug (todas terminando no frame 25); os
"from" (0, 4, 10, 14, 20, 22) eram confiáveis e não-sobrepostos, e reconstruir os segmentos a
partir deles bateu exato com o total de 26 frames — não foi preciso adivinhar.

`MontarPrefabDoByakhee.cs` (`Tools/FavelaAmarela/Montar Prefab do Byakhee`) monta
`Byakhee.prefab` usando o frame `byakhee_espreita_0` como visual — **ainda sem `Animator`**,
mesma convenção do Abdul ("tudo estático" até animação entrar em escopo do VS). Números de
combate (dano, alcance, velocidades) continuam nos defaults calibrados por simulação, intactos.

## A arena (2026-08-20)

`Portoes_Das_Ruinas.unity`, montada por `MontarPortoesDasRuinas`. Alcançável pelo marco
`Portoes_DasRuinas` do Deserto — que era **pura decoração** até aqui, sem colisor nem portal.

A luta **começa por gatilho**, não no `Start`: o grito drena 2 RM/s passivamente, então começar
ao carregar a cena cobraria Resiliência antes de o jogador escolher entrar.

O portão é arte de verdade — Kenney "Dungeon Pack" 2.3 (CC0), o par
`stoneWallGateClosed_S`/`stoneWallGateOpen_S` ladeado por `stoneWallAged_S`. As peças são
levantadas por metade da própria altura porque o pivô das fatias é central: sem isso o pé
desenhado fica abaixo da linha do colisor e o jogador atravessa a base visível.

**Abater destranca; quem abre é o jogador.** `ArenaDosPortoes` chama `PortaoDosPortoes.Destrancar()`
no abate, e o portão — duas folhas de pedra que deslizam — é um `IInteragivel`: o jogador encosta
e aperta interagir. O portão abrindo sozinho roubaria o gesto e jogaria a transição de fase por
cima da animação de morte do chefe.

O chão reusa a receita da `MontarArenaDeTestes` (losango isométrico, anel de colisão de **duas**
células com tile de `colliderType Grid`). O gatilho de luta tem 38 de largura e os Portões 20 —
larguras calculadas, não estimadas: o losango afina com Y, e uma faixa estreita demais seria
contornada pela beirada.

## Pendente
- **Animação de verdade.** Os 26 frames existem e estão nomeados; falta o `Animator`/
  `AnimatorController` que os reproduza — hoje só o frame de idle é usado.
- **Cena de abertura** (o grito antes da forma).
- ~~Yug-Neth como chave dimensional~~ — **descartado em 2026-08-20** (decisão do Vini). Em
  vez de um bloqueio a mais, o fim da luta libera um **Poste de Luz**: o `RefugioDeLuz` já
  reanima o companheiro, ancora a RM, cura e grava a partida. Vencer o Byakhee é o que
  devolve o Yug-Neth de pé.
- **Arte:** as folhas dos Portões são caixas de placeholder.

## Relacionados
- [Ficha de Atributos](ficha_de_atributos.md) — a fórmula de mitigação que rege o dano
- [Loot e Drop](loot_e_drop.md) — por que o drop é garantido e fura o gate
- [Resiliência Mental](resiliencia_mental.md) — o recurso que o grito consome
