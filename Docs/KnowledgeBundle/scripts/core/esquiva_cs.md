---
type: C# Script
title: Esquiva.cs
description: Implementação da mecânica de Esquiva física
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Core/Abilities/Esquiva.cs
tags: [core, abilities, dodge]
timestamp: 2026-07-07T11:00:00Z
---

# Esquiva

**Namespace:** `FavelaAmarela.Core.Abilities`  
**Tipo:** `public sealed class`

Implementa a lógica matemática da [Esquiva](../../systems/esquiva.md). **Não implementa** `IAnomalyPower` pois não possui custo de RM.

## API Pública

- `CanActivate(float timeSinceLastUse)`: Valida cooldown.
- `Execute()`: Retorna `EsquivaResult` (struct readonly).
