---
type: C# Script
title: EsquivaBridge.cs
description: Bridge MonoBehaviour que conecta o POCO Esquiva à Unity
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldição%20da%20Cidade%20Pálida/Assets/Scripts/Player/EsquivaBridge.cs
tags: [runtime, player, abilities, dodge]
timestamp: 2026-07-07T16:00:00Z
---

# EsquivaBridge

**Namespace:** `FavelaAmarela.Player`
**Tipo:** `public class` (herda de `MonoBehaviour`)

Adapter que instancia o POCO [Esquiva](../core/esquiva_cs.md) em `Awake()` e expõe `TryActivateEsquiva(Vector2 direction)`, chamado pelo `PlayerMovement` quando a action `Esquiva` (tecla Espaço) é pressionada.

## Responsabilidades
- Guarda o instante do último uso (`lastUseTime`) e delega a checagem de cooldown ao POCO (`esquiva.CanActivate`).
- Dispara o evento `OnEsquivaActivada(direction, duration, speedMultiplier)`, consumido por `PlayerMovement.HandleEsquivaActivada` para aplicar a velocidade e emitir som.
- Usa `Invoke(nameof(EndEsquiva), duration)` para encerrar o estado — mesma limitação do `AnomalyPowerBridge`: o `Invoke` não é cancelado automaticamente se o GameObject for desativado no meio da esquiva.
- Diferente do Salto Dimensional, não troca a layer do jogador — a Esquiva colide com paredes normalmente.

## Dependências e Relacionamentos
- [Esquiva (POCO)](../core/esquiva_cs.md)
- [PlayerMovement](player_movement_cs.md) — único consumidor do evento `OnEsquivaActivada`.
