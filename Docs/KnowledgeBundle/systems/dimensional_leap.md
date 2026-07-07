---
type: Game System
title: Salto Dimensional
description: Ghost Dash — habilidade anômala que permite travessia instantânea curta, com custo de Resiliência Mental.
tags: [abilities, anomaly, movement, dimensional]
timestamp: 2026-07-07T11:00:00Z
---

# Salto Dimensional (Ghost Dash)

O **Salto Dimensional** é a habilidade anômala principal de Damião. Permite um dash curto que potencialmente atravessa obstáculos, representando uma breve distorção dimensional causada pela influência de Carcosa.

## Parâmetros (valores padrão)

| Parâmetro | Valor | Descrição |
|-----------|-------|-----------|
| `duration` | **0.2s** | Duração do dash |
| `cooldown` | **1.0s** | Tempo entre usos |
| `resilienceCost` | **10 RM** | Custo em Resiliência Mental |

## Condições de Ativação

1. O cooldown deve ter passado (`timeSinceLastUse >= cooldown`)
2. O jogador deve ter RM suficiente (`currentResilience >= resilienceCost`)
3. Se a RM for insuficiente, `Execute()` retorna `Success = false` e custo zero

## Integração com Outros Sistemas

- Consome [Resiliência Mental](resiliencia_mental.md) via `SofrerTrauma(resilienceCost)` no adapter
- Pode emitir som ao entrar/sair via [Propagação Sonora](sound_propagation.md), alertando [Cultistas](cultista_ai.md)
- Segue o contrato [IAnomalyPower](abilities.md)

## Diferença da Esquiva

O Salto Dimensional é **sobrenatural** (custa RM, atravessa obstáculos). A [Esquiva](esquiva.md) é **física** (não custa RM, apenas um dodge lateral rápido).
