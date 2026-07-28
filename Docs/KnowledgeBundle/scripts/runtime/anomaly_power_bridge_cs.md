---
type: C# Script
title: AnomalyPowerBridge.cs
description: Adapter que conecta o Salto Dimensional (DimensionalLeap) à Unity
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Player/AnomalyPowerBridge.cs
tags: [runtime, player, abilities]
timestamp: 2026-07-09T00:00:00Z
---

# AnomalyPowerBridge

**Namespace:** `FavelaAmarela.Player`
**Tipo:** `public class` (herda de `MonoBehaviour`)

Adapter que conecta o POCO `DimensionalLeap` (Salto Dimensional) à Unity, seguindo o mesmo padrão de bridge de [MaoFisicaBridge](mao_fisica_bridge_cs.md). Espalha eventos para VFX/áudio/física reagirem, sem regra de negócio própria.

## Responsabilidades
- `Awake()`: instancia `DimensionalLeap` com os valores serializados (duração, cooldown, custo de Resiliência Mental).
- `Bind(ResilienciaMental resiliencia)`: injeta a POCO de Resiliência Mental de Damião — chamado pelo `GameManager` em `InjetarDependencias()`.
- `TryActivateLeap(Vector2 direction)`: valida desbloqueio + `CanActivate` (custo de RM + cooldown), executa o salto, aplica `SofrerTrauma(custo)` na Resiliência e dispara os eventos.

## API Pública
- `OnDimensionalLeapActivated(Vector2 direction, float duration, float speedMultiplier)` (evento)
- `OnResilienceConsumed(float custo)` (evento)
- `IsLeaping` (`bool`)
- `SaltoDesbloqueado` (`bool`)
- `DesbloquearSalto()`: chamado pelo pickup do patuá na Zona 5 — Damião nasce sem o Salto Dimensional (ver [progressao-salto-e-controles](../../lore/world_rules.md)).
