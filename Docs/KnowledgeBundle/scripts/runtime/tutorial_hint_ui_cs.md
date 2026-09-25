---
type: C# Script
title: TutorialHintUI.cs
description: Mostra uma dica de texto na tela com fade in/out para momentos de tutorial
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/UI/TutorialHintUI.cs
tags: [runtime, ui]
timestamp: 2026-07-09T00:00:00Z
---

# TutorialHintUI

**Namespace:** `FavelaAmarela.Runtime.UI`
**Tipo:** `public sealed class` (herda de `MonoBehaviour`)

Mostra uma dica de texto na tela, com fade in/out, para momentos de tutorial (ex.: primeiro encontro com um Cultista na Zona 2). Sem regra de negócio: só anima o alpha de um `CanvasGroup` — mesmo espírito de [ScreenFader](screen_fader_cs.md), mas para texto de dica em vez de tela cheia.

## API Pública
- `Mostrar(string mensagem, float duracaoVisivel = 4f, float duracaoFade = 0.4f)`: para qualquer rotina em andamento, atualiza o texto e inicia a sequência de fade in → espera → fade out.

## Consumido por
[TutorialHintTrigger](tutorial_hint_trigger_cs.md) e [PatuaPickup](patua_pickup_cs.md).

## Robustez
`Awake()` valida `grupo` e `texto` via `Debug.LogError` se não atribuídos no Inspector.
