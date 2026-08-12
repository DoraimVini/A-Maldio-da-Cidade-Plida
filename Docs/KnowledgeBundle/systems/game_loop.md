---
type: Game System
title: Game Loop
description: Máquina de estados do ciclo principal do jogo com transições validadas.
tags: [game-loop, state-machine, core]
timestamp: 2026-07-07T11:00:00Z
---

# Game Loop (Máquina de Estados)

O ciclo principal do jogo é controlado por uma FSM com **5 estados** e **transições validadas** — nem toda transição é permitida.

## Estados

| Estado | Descrição |
|--------|-----------|
| **Menu** | Tela inicial. Estado padrão ao iniciar o jogo. |
| **Gameplay** | Jogo em andamento. Todos os sistemas de stealth, IA e RM ativos. |
| **Pausado** | Jogo pausado. Tempo congelado. |
| **Colapso** | Fim de jogo diegético — [Resiliência Mental](resiliencia_mental.md) chegou a zero; retorna ao Menu. |
| **TransicaoDeFase** | Encerramento de uma fase/dungeon (ex.: derrota do miniboss num portão de saída). **Não é "Vitória"** — o jogo é um RPG multi-fase, não roguelike; congela o gameplay para a transição visual antes do próximo trecho. (2026-07-28: renomeado de `Vitoria`.) |

## Transições Válidas

```
Menu            ──▶ Gameplay
Gameplay        ──▶ Pausado | Colapso | TransicaoDeFase
Pausado         ──▶ Gameplay | Menu
Colapso         ──▶ Menu
TransicaoDeFase ──▶ Menu
```

Qualquer outra transição é **rejeitada silenciosamente** (`TryTransition()` retorna `false`).

## Integração

- `ResilienciaMental.IsColapso == true` → deve disparar `TryTransition(GameState.Colapso)`
- O adapter `GameManager` observa `OnStateChanged` para pausar/despausar `Time.timeScale`
