---
type: C# Script
title: GameLoopStateMachine.cs
description: Máquina de estados do Game Loop
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Core/GameLoop/GameLoopStateMachine.cs
tags: [core, game-loop, fsm]
timestamp: 2026-07-07T11:00:00Z
---

# GameLoopStateMachine

**Namespace:** `FavelaAmarela.Core.GameLoop`  
**Tipo:** `public sealed class`

Implementa o [Game Loop](../../systems/game_loop.md).

## API Pública

- `CurrentState`: Enum `GameState`
- `TryTransition(GameState alvo)`: Tenta transicionar. Retorna `true` se bem-sucedido e `false` se a transição for inválida (conforme as regras da FSM).
- `event Action<GameState, GameState> OnStateChanged`: Dispara apenas se `TryTransition` for bem sucedido.
