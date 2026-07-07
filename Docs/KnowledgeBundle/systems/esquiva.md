---
type: Game System
title: Esquiva
description: Dodge físico rápido sem custo de Resiliência Mental.
tags: [abilities, movement, dodge, physical]
timestamp: 2026-07-07T11:00:00Z
---

# Esquiva (Dodge)

A **Esquiva** é uma habilidade puramente física — Damião se joga para o lado rapidamente. Não consome Resiliência Mental porque não envolve distorção dimensional.

## Parâmetros (valores padrão)

| Parâmetro | Valor | Descrição |
|-----------|-------|-----------|
| `duration` | **0.15s** | Duração do impulso |
| `cooldown` | **0.8s** | Tempo entre usos |
| `speedMultiplier` | **2.5x** | Multiplicador de velocidade durante a esquiva |

## Condições de Ativação

- Apenas verifica cooldown (`timeSinceLastUse >= cooldown`)
- **Sem custo de recurso** — pode ser usada mesmo com RM zero

## Resultado: EsquivaResult

| Campo | Tipo | Valor |
|-------|------|-------|
| `Success` | `bool` | Sempre `true` se ativada |
| `DurationSeconds` | `float` | 0.15s |
| `SpeedMultiplier` | `float` | 2.5x |

## Por que NÃO implementa IAnomalyPower?

A interface `IAnomalyPower` exige um custo de resiliência. A Esquiva é um ato físico comum, não sobrenatural. Por isso tem sua própria classe `sealed`, seu próprio resultado `EsquivaResult` (readonly struct), e não participa do sistema de [Habilidades Anômalas](abilities.md).
