---
type: C# Script
title: VitalidadeBar.cs
description: Barra de Vitalidade Corpórea do Damião no HUD
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/UI/VitalidadeBar.cs
tags: [runtime, ui, hud]
timestamp: 2026-07-30T00:00:00Z
---

# VitalidadeBar

**Namespace:** `FavelaAmarela.Runtime.UI`
**Tipo:** `public sealed class` (`MonoBehaviour`)

Espelha a [Vitalidade](../core/vitalidade_cs.md) do Damião no HUD, observando `OnChanged`
(sem polling). É a barra **corpórea** — distinta da [ResilienciaBar](resiliencia_bar_cs.md),
que é a sanidade. Duas barras, dois vetores de derrota.
