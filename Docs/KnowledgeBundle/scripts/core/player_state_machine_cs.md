---
type: C# Script
title: PlayerStateMachine.cs
description: FSM de ações exclusivas do jogador (Esquiva, Ataque)
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Core/Player/PlayerStateMachine.cs
tags: [core, player, fsm]
timestamp: 2026-07-30T00:00:00Z
---

# PlayerStateMachine

**Namespace:** `FavelaAmarela.Core.Player`
**Tipo:** `public sealed class` (POCO puro) + enum `PlayerState`

Fonte única de verdade das **ações exclusivas** de Damião — substituiu as antigas
flags-espelho (`isEsquivando`/`isAtacando`) espalhadas pelos bridges.

**Estados** (`PlayerState`): `Livre`, `Esquivando`, `Atacando`.

- `EstaLivre` — se uma ação nova pode começar
- `TryEntrarAcao(estado, duracao)` — tenta ocupar a FSM; falha se já há ação em curso
- `Tick(dt)` — expira a ação e volta a `Livre`
- `OnStateChanged(anterior, novo)`

O `PlayerMovement` cria a instância e injeta nos bridges (`BindStateMachine`), garantindo que
todos consultem **a mesma** FSM.

> **Nota histórica:** havia um quarto estado, `Saltando`, do Salto Dimensional — removido
> junto com a habilidade em 2026-07-30.
