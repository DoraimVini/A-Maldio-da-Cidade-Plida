---
type: C# Script
title: ColapsoTrigger.cs
description: Trigger que força o colapso imediato da Resiliência Mental (ex. cair num abismo)
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/GameLoop/ColapsoTrigger.cs
tags: [runtime, gameloop]
timestamp: 2026-07-09T00:00:00Z
---

# ColapsoTrigger

**Namespace:** `FavelaAmarela.Runtime.GameLoop`
**Tipo:** `public class` (herda de `MonoBehaviour`, `[RequireComponent(Collider2D)]`)

Trigger simples: ao entrar em contato com o `Player`, chama `GameManager.Instance.Resiliencia.ForcarColapso()` — o mesmo contrato reaproveitado por [CoisaDoCemiterioFSM](../core/coisa_do_cemiterio_fsm_cs.md) para o insta-kill (ver [dependency_map.md](../../architecture/dependency_map.md)).
