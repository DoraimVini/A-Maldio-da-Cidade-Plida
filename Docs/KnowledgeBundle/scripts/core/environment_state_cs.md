---
type: C# Script
title: EnvironmentState.cs
description: Estado ambiental compartilhado (intensidade de tempestade) observado por múltiplos adapters
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Core/Environment/EnvironmentState.cs
tags: [core, environment, state]
timestamp: 2026-07-09T00:00:00Z
---

# EnvironmentState

**Namespace:** `FavelaAmarela.Core.Environment`
**Tipo:** `public class`

POCO que guarda a intensidade atual da [Tempestade de Areia](../../systems/environment.md) (`StormIntensity`, 0..1) e notifica quem observa quando ela muda de verdade — nunca em polling.

## API Pública

### Propriedades (readonly)
- `StormIntensity`: intensidade atual da tempestade, clampada em `[0, 1]`. Valor inicial `0.3f`.

### Métodos de Mutação
- `SetStormIntensity(float valor)`: faz clamp em `[0, 1]` e só dispara `OnStormIntensityChanged` se o valor realmente mudou (evita ruído em quem observa).

### Eventos
- `OnStormIntensityChanged(float)`: disparado com o novo valor de `StormIntensity` sempre que `SetStormIntensity` muda o valor de fato. Observado por [TempestadeAmbiente](../runtime/tempestade_ambiente_cs.md) e [TempestadeVisualOverlay](../runtime/tempestade_visual_overlay_cs.md).

## Quem alimenta este estado
Quem chama `SetStormIntensity` é o adapter Runtime — tipicamente o resultado de [TempestadeOscilador.Tick](tempestade_oscilador_cs.md), aplicado a cada frame por `TempestadeAmbiente`.
