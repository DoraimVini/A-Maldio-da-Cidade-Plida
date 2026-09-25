---
type: C# Script
title: CultistaAI.cs
description: Adapter Runtime que dirige visual, movimento e física do CultistaFSM
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Enemies/CultistaAI.cs
tags: [runtime, enemies, ai]
timestamp: 2026-07-09T00:00:00Z
---

# CultistaAI

**Namespace:** `FavelaAmarela.Runtime.Enemies`
**Tipo:** `public class` (herda de `MonoBehaviour`, `[RequireComponent(SpriteRenderer, Rigidbody2D)]`)

Adapter que instancia e dirige o [CultistaFSM](../core/cultista_fsm_cs.md), traduzindo cada estado (`Errante`/`Alerta`/`Caca`) em movimento via `Rigidbody2D.linearVelocity` (nunca `MovePosition`) e cor do `SpriteRenderer`.

## Responsabilidades
- `Awake()`: zera `gravityScale`, seta `CollisionDetectionMode2D.Continuous`, monta a `PatrolRoute` a partir dos `waypoints` do Inspector (com fallback pra posição atual se algum waypoint estiver nulo).
- `FixedUpdate()`: chama `_fsm.Tick(Time.fixedDeltaTime)` e move o `Rigidbody2D` conforme o estado atual.
- `OnEnable`/`OnDisable`: se inscreve/desinscreve em `GameManager.Instance.SoundBroadcaster.OnSomEmitido` — evento, não polling.
- `ReceberGolpeFisico(ArmaResult resultado)`: chamado por [MaoFisicaBridge](mao_fisica_bridge_cs.md); só atordoa a FSM (`_fsm.AtordoarPor(...)`) se `resultado.Atordoou` for `true` — a chance em si é decidida pela arma, não aqui.

## Fallback de robustez
Waypoint nulo no Inspector gera `Debug.LogError` e usa a posição atual como fallback, em vez de estourar `NullReferenceException` (regra 7 do CLAUDE.md raiz).
