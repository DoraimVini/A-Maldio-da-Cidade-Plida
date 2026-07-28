---
type: C# Script
title: TempestadeVisualOverlay.cs
description: Ajusta o alpha de um véu semitransparente na tela conforme a intensidade da tempestade
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/UI/TempestadeVisualOverlay.cs
tags: [runtime, ui, environment]
timestamp: 2026-07-09T00:00:00Z
---

# TempestadeVisualOverlay

**Namespace:** `FavelaAmarela.Runtime.UI`
**Tipo:** `public sealed class` (herda de `MonoBehaviour`)

Ajusta o alpha de um véu semitransparente na tela conforme [EnvironmentState.StormIntensity](../core/environment_state_cs.md) — reduz visibilidade por cautela, sem mexer em velocidade de movimento. Observa o evento `OnStormIntensityChanged`, nunca faz polling a cada frame (regra 8 do CLAUDE.md raiz).

## API Pública
- `Bind(EnvironmentState environment)`: desinscreve de uma fonte anterior (se houver), inscreve no novo `EnvironmentState` e sincroniza o visual imediatamente com `HandleStormIntensityChanged(_environment.StormIntensity)` — não espera o primeiro evento.

## Comportamento
- `HandleStormIntensityChanged(float intensidade)`: seta `veu.color.a = intensidade * alphaMaximo`.
- `OnDestroy()`: desinscreve do evento — evita handler pendurado após a cena descarregar.
