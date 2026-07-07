---
type: Game System
title: Habilidades Anômalas
description: Sistema de poderes sobrenaturais baseado na interface IAnomalyPower.
tags: [abilities, powers, anomaly, design-pattern]
timestamp: 2026-07-07T11:00:00Z
---

# Sistema de Habilidades Anômalas

O protagonista Damião possui acesso a poderes sobrenaturais (Anomalias) causados pela influência de Carcosa. Cada habilidade anômala **custa Resiliência Mental** — distorcer a realidade cobra um preço na sanidade.

## Contrato: IAnomalyPower

Toda habilidade anômala implementa a interface `IAnomalyPower`:

| Membro | Retorno | Descrição |
|--------|---------|-----------|
| `PowerName` | `string` | Nome diegético da habilidade |
| `CanActivate(currentResilience, timeSinceLastUse)` | `bool` | Verifica cooldown e custo de RM |
| `Execute(currentResilience)` | `PowerResult` | Executa e retorna resultado |

## PowerResult

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `Success` | `bool` | Se a habilidade foi executada |
| `DurationSeconds` | `float` | Duração do efeito |
| `CooldownSeconds` | `float` | Tempo até poder usar de novo |
| `ResilienceCost` | `float` | Quanto de RM foi consumido |

## Habilidades Implementadas

- [Salto Dimensional](dimensional_leap.md) — Ghost Dash (implementa `IAnomalyPower`)
- [Esquiva](esquiva.md) — Dodge físico (**NÃO** implementa `IAnomalyPower` — sem custo de RM)

## Regra de Design

Habilidades que distorcem a realidade de Carcosa **DEVEM** implementar `IAnomalyPower` e ter custo de Resiliência Mental. Habilidades puramente físicas (como a Esquiva) **NÃO** implementam essa interface.
