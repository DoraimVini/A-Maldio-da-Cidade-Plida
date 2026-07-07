---
type: C# Script
title: PatrolRoute.cs
description: Lógica de gerenciamento de waypoints de patrulha
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Core/Enemies/PatrolRoute.cs
tags: [core, enemies, movement]
timestamp: 2026-07-07T11:00:00Z
---

# PatrolRoute

**Namespace:** `FavelaAmarela.Core.Enemies`  
**Tipo:** `public sealed class`

Implementa as lógicas de movimentação do [Sistema de Patrulha](../../systems/patrulha.md).

## Construtor
```csharp
public PatrolRoute(Vector2[] waypoints, bool loop = true)
```

## API Pública
- `AlvoAtual`: `Vector2` — Retorna o waypoint atual
- `AtualizarChegada(Vector2 posicaoAtual, float raioDeChegada)`: Deve ser chamado pelo adapter a cada frame. Se o NPC chegou no waypoint, avança o índice (respeitando o modo `loop` ou `ping-pong`) e retorna `true`.
