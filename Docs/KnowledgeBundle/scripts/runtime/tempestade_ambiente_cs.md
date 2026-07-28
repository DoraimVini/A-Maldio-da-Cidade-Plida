---
type: C# Script
title: TempestadeAmbiente.cs
description: Adapter que tica o TempestadeOscilador continuamente e empurra o resultado pro EnvironmentState
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Environment/TempestadeAmbiente.cs
tags: [runtime, environment]
timestamp: 2026-07-09T00:00:00Z
---

# TempestadeAmbiente

**Namespace:** `FavelaAmarela.Runtime.Environment`
**Tipo:** `public class` (herda de `MonoBehaviour`)

Adapter que tica o [TempestadeOscilador](../core/tempestade_oscilador_cs.md) a cada frame e empurra o resultado para [EnvironmentState.SetStormIntensity](../core/environment_state_cs.md) — as rajadas de vento/areia que dão vida ao valor de [Tempestade de Areia](../../systems/environment.md).

## Responsabilidades
- `Awake()`: instancia `TempestadeOscilador` com a faixa inicial serializada (`minimoInicial`, `maximoInicial`, `velocidadeCiclo`).
- `Bind(EnvironmentState environment)`: injeta a POCO de ambiente — chamado pelo `GameManager` em `InjetarDependencias()`.
- `DefinirFaixa(float minimo, float maximo)`: redefine a faixa de oscilação — chamado por [TempestadeZonaTrigger](tempestade_zona_trigger_cs.md) ao mudar de zona.
- `Update()`: `_environment.SetStormIntensity(_oscilador.Tick(Time.deltaTime))`.
