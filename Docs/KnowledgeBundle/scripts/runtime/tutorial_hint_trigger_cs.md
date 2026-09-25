---
type: C# Script
title: TutorialHintTrigger.cs
description: Trigger que dispara uma dica de tutorial uma única vez quando Damião entra na área
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/GameLoop/TutorialHintTrigger.cs
tags: [runtime, gameloop, ui]
timestamp: 2026-07-09T00:00:00Z
---

# TutorialHintTrigger

**Namespace:** `FavelaAmarela.Runtime.GameLoop`
**Tipo:** `public sealed class` (herda de `MonoBehaviour`, `[RequireComponent(Collider2D)]`)

Mesmo padrão de trigger de [ColapsoTrigger](colapso_trigger_cs.md)/`QuedaZ4Z5Trigger`: `Collider2D` + `CompareTag("Player")`, mas dispara **uma única vez** (flag `_disparado`) e chama `TutorialHintUI.Mostrar(mensagem, duracaoVisivel)` em vez de mexer em GameState.

## Robustez
`Awake()` valida `hintUI` via `Debug.LogError` se não atribuída no Inspector, seguindo a regra 7 do CLAUDE.md raiz (nunca deixar `NullReferenceException` estourar).
