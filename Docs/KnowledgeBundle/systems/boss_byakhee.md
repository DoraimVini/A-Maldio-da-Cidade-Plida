---
type: Game System
title: Boss Byakhee — O Cadeado dos Portões
description: A luta que fecha a Fase 1. Imune no ar, vulnerável só no pouso; a dificuldade cresce encurtando a janela, não subindo o dano.
tags: [boss, byakhee, combate, fase1, portoes]
---

# Boss Byakhee

> **Status:** Core e Runtime implementados em 2026-08-11. **Falta prefab, arte e a arena em
> cena.** Item 9 da lista do edital. Design narrativo em
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
existe** — as da Tumba são Cravo de Aklo, Estilete de Irem e Alfanje de Alhazred.

`ByakheeFSM.CortarAsa()` está implementado e exposto em `ByakheeAI.TentarCortarAsa()` para
quando ela existir. O caminho real hoje é o **pouso espontâneo a cada 30 s**, que o próprio
design documenta como alternativa. Sem essa válvula, a fase 3 seria um impasse para todo
jogador — ninguém tem a arma.

## Ficha e espólio

| | |
|---|---|
| Vitalidade | 420 |
| Ataque (garras) | 26 |
| Defesa | 8 |
| Conjuração / Resistência Anômala | 20 / 12 |
| Drop | **Anel do Sinal Amarelo** (garantido) |

A Vitalidade alta é deliberada: com janelas de 1,5–3 s, uma vida baixa faria a luta acabar em
dois pousos e o padrão nunca se revelaria. O drop é **garantido**, não sorteado — é
progressão roteirizada, e `Garantido: 1` fura o gate de nível de propósito (ver
[loot_e_drop.md](loot_e_drop.md)).

## Arquitetura

| Peça | Camada |
|---|---|
| `ByakheeFSM`, `ByakheeState` | Core (POCO, 10 testes) |
| `ByakheeAI` | Runtime — move o corpo, pinta o sprite, aplica dano e dreno |
| `Ficha_Byakhee`, `Drop_Byakhee` | Assets |

A imunidade em voo é aplicada pelo `ByakheeAI` ligando `EnemyBase.IgnorarDano` conforme a FSM.
A `EnemyBase` sozinha aceitaria qualquer golpe — a regra vive no POCO, o efeito no adaptador.

## Pendente
- **Prefab e arte.** Não há sprite do Byakhee; as cores de estado são leitura provisória.
- **Arena em cena:** os Portões das Ruínas não existem como local jogável.
- **Cena de abertura** (o grito antes da forma) e a abertura dos Portões ao morrer.
- **`DropAoAbater`** precisa ser anexado ao prefab com a `Drop_Byakhee`.

## Relacionados
- [Ficha de Atributos](ficha_de_atributos.md) — a fórmula de mitigação que rege o dano
- [Loot e Drop](loot_e_drop.md) — por que o drop é garantido e fura o gate
- [Resiliência Mental](resiliencia_mental.md) — o recurso que o grito consome
