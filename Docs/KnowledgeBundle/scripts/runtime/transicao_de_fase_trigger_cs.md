---
type: C# Script
title: TransicaoDeFaseTrigger.cs
description: Trigger de fim de fase/dungeon que dispara a transição TransicaoDeFase do GameLoopStateMachine
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/GameLoop/TransicaoDeFaseTrigger.cs
tags: [runtime, gameloop]
timestamp: 2026-07-28T00:00:00Z
---

# TransicaoDeFaseTrigger

**Namespace:** `FavelaAmarela.Runtime.GameLoop`
**Tipo:** `public class` (herda de `MonoBehaviour`, `[RequireComponent(Collider2D)]`)

Trigger de saída de fase/dungeon: ao entrar em contato com o `Player`, chama `GameManager.Instance.TriggerTransicaoDeFase()`, que transiciona a [GameLoopStateMachine](../core/game_loop_sm_cs.md) para `GameState.TransicaoDeFase`. Reaproveitável em qualquer ponto de saída (ex.: Portões das Ruínas, ao fim da Fase 1).

> **Renomeado em 2026-07-28** (antes `VitoriaTrigger` → `GameState.Vitoria`). O jogo é um RPG multi-fase, não roguelike: o fim de uma fase é uma **transição**, não uma tela de "Vitória". Ver `systems/game_loop.md`.
