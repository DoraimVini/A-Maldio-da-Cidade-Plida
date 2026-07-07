---
type: Unity Gotcha
title: APIs Renomeadas na Unity 6
description: APIs que foram renomeadas ou alteradas na Unity 6 (6000.x) vs versões anteriores.
tags: [unity6, api, breaking-changes, migration]
timestamp: 2026-07-07T11:00:00Z
---

# APIs Renomeadas na Unity 6

A Unity 6 (6000.x) renomeou diversas APIs. Agentes de IA treinados em código de Unity 5/2019-2022 frequentemente usam os nomes antigos. **Sempre consulte a Script Reference da versão 6000.4 antes de usar uma API.**

Referência oficial: https://docs.unity3d.com/6000.4/Documentation/ScriptReference/

## Tabela de Renomeações Críticas

| API Antiga | API Nova (Unity 6) | Contexto |
|-----------|-------------------|----------|
| `Rigidbody2D.velocity` | `Rigidbody2D.linearVelocity` | Já refletido em `PlayerMovement.cs` |
| `Rigidbody2D.angularVelocity` (float) | `Rigidbody2D.angularVelocityZ` | Rotação 2D |
| `Object.FindObjectOfType<T>()` | `Object.FindAnyObjectByType<T>()` | Busca mais performática |
| `Object.FindObjectsOfType<T>()` | `Object.FindObjectsByType<T>(sortMode)` | Requer argumento de sort |

## Regra para Agentes

Ao gerar código Unity, **NUNCA** assuma nomes de API baseado em versões anteriores. Se estiver em dúvida, consulte:
- Script Reference: https://docs.unity3d.com/6000.4/Documentation/ScriptReference/
- Manual: https://docs.unity3d.com/6000.4/Documentation/Manual/
