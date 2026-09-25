---
type: C# Script
title: TempestadeOscilador.cs
description: Calcula a intensidade de tempestade oscilando suavemente entre um mínimo e um máximo
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Core/Environment/TempestadeOscilador.cs
tags: [core, environment, state]
timestamp: 2026-07-09T00:00:00Z
---

# TempestadeOscilador

**Namespace:** `FavelaAmarela.Core.Environment`
**Tipo:** `public sealed class`

POCO que calcula a intensidade da [Tempestade de Areia](../../systems/environment.md) como uma onda senoidal entre um mínimo e um máximo — simulando rajadas de vento — em vez de um valor estático fixo por zona. Única dependência de Unity é `Mathf`.

## API Pública

### Construtor
- `TempestadeOscilador(float minimo = 0.2f, float maximo = 0.6f, float velocidadeCiclo = 0.3f)`

### Métodos
- `DefinirFaixa(float novoMinimo, float novoMaximo)`: redefine a faixa de oscilação (ex.: ao entrar numa [TempestadeZonaTrigger](../runtime/tempestade_zona_trigger_cs.md) com tempestade mais forte). Aceita min/max em qualquer ordem; faz clamp em `[0, 1]`.
- `Tick(float dt)`: avança o tempo interno acumulado e retorna a intensidade atual (`0..1`), interpolando (`Mathf.Lerp`) entre `minimo` e `maximo` conforme uma onda `Mathf.Sin`.

## Uso típico
`TempestadeAmbiente` (Runtime) chama `Tick(Time.deltaTime)` a cada frame e passa o resultado para `EnvironmentState.SetStormIntensity(...)`.
