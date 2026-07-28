---
type: C# Script
title: PatuaPickup.cs
description: Pickup do patuá na Zona 5 — destrava o Salto Dimensional permanentemente
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/GameLoop/PatuaPickup.cs
tags: [runtime, gameloop, progression]
timestamp: 2026-07-09T00:00:00Z
---

# PatuaPickup

**Namespace:** `FavelaAmarela.Runtime.GameLoop`
**Tipo:** `public sealed class` (herda de `MonoBehaviour`, `[RequireComponent(Collider2D)]`)

Trigger de coleta única (flag `_coletado`): ao tocar o `Player`, busca o componente [AnomalyPowerBridge](anomaly_power_bridge_cs.md) do colisor e chama `DesbloquearSalto()` — é aqui que o Salto Dimensional deixa de estar bloqueado (ver [progressão do Salto](../../lore/world_rules.md)). Mostra uma dica opcional via `TutorialHintUI` e desativa o próprio `GameObject` ao coletar.
