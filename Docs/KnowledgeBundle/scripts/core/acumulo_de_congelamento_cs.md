---
type: C# Script
title: AcumuloDeCongelamento.cs
description: Stacks de frio dos Cones de Gelo do Abdul (3 stacks congelam)
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Core/Combat/AcumuloDeCongelamento.cs
tags: [core, combat, boss, status]
timestamp: 2026-07-30T00:00:00Z
---

# AcumuloDeCongelamento

**Namespace:** `FavelaAmarela.Core.Combat`
**Tipo:** `public sealed class` (POCO puro)

Regra dos **Cones de Gelo** da Fase 2 do [Abdul](../../systems/boss_abdul.md): cada cone que
acerta Damião aplica um acúmulo de frio; ao chegar a **3**, ele congela (atordoado brevemente)
e o acúmulo zera.

**Acúmulos expiram com o tempo** — a mecânica é "não leve três seguidos", não uma punição
inevitável por levar três ao longo da luta inteira. 10 testes EditMode.

> ⚠️ **Ainda não ligado ao Damião:** o POCO existe e está testado, mas falta o componente no
> jogador, o prefab do Cone de Gelo, e um estado de "congelado" no `PlayerState` (decisão de
> design pendente).
