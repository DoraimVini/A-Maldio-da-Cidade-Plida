---
type: C# Script
title: TempestadeZonaTrigger.cs
description: Redefine a faixa de oscilação da tempestade sempre que o jogador entra numa zona
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/GameLoop/TempestadeZonaTrigger.cs
tags: [runtime, gameloop, environment]
timestamp: 2026-07-09T00:00:00Z
---

# TempestadeZonaTrigger

**Namespace:** `FavelaAmarela.Runtime.GameLoop`
**Tipo:** `public class` (herda de `MonoBehaviour`, `[RequireComponent(Collider2D)]`)

Diferente dos outros triggers de progressão (`ColapsoTrigger`, `PatuaPickup`, `TutorialHintTrigger`), **dispara toda vez** que o jogador entra na zona — não é um evento único, é "agora você está numa zona com esse clima". Chama `TempestadeAmbiente.DefinirFaixa(minimo, maximo)` com os valores serializados no Inspector.

## Robustez
`Awake()` valida a referência `tempestadeAmbiente` via `Debug.LogError` se não atribuída.
