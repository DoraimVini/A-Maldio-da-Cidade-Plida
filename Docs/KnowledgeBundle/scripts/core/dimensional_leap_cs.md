---
type: C# Script
title: DimensionalLeap.cs
description: Implementação do Salto Dimensional
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Core/Abilities/DimensionalLeap.cs
tags: [core, abilities, leap]
timestamp: 2026-07-07T11:00:00Z
---

# DimensionalLeap

**Namespace:** `FavelaAmarela.Core.Abilities`  
**Tipo:** `public class`

Implementa `IAnomalyPower`. Define a lógica matemática do [Salto Dimensional](../../systems/dimensional_leap.md).

## Contrato IAnomalyPower

- `CanActivate()`: Checa cooldown e custo de Resiliência Mental.
- `Execute()`: Retorna um `PowerResult`. Se falhar por falta de recursos, retorna `Success = false`.
