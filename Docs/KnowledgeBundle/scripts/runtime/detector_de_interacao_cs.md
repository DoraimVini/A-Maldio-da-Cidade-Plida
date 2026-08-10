---
type: C# Script
title: DetectorDeInteracao.cs
description: Componente do jogador que acha o alvo interagível e dispara o botão E
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Interaction/DetectorDeInteracao.cs
tags: [runtime, interacao, player]
timestamp: 2026-07-30T00:00:00Z
---

# DetectorDeInteracao

**Namespace:** `FavelaAmarela.Runtime.Interaction`
**Tipo:** `public sealed class` (`MonoBehaviour`)

Descobre o que está ao alcance (`OverlapCircle` com buffers pré-alocados), pergunta ao POCO
[SeletorDeInteracao](../core/seletor_de_interacao_cs.md) qual vence, e chama `Interagir()` quando
a ação `Interact` (**E** / botão Norte) é pressionada.

**Revalida `PodeInteragir` no instante do aperto** — o alvo pode ter mudado de estado entre a
mira e o clique.

`OnAlvoMudou(IInteragivel)` alimenta o [PromptDeInteracao](prompt_de_interacao_cs.md) sem polling.
