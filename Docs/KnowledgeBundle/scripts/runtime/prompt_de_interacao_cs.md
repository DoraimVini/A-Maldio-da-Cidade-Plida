---
type: C# Script
title: PromptDeInteracao.cs
description: UI que mostra o convite de interação quando há algo ao alcance
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/UI/PromptDeInteracao.cs
tags: [runtime, ui, interacao]
timestamp: 2026-07-30T00:00:00Z
---

# PromptDeInteracao

**Namespace:** `FavelaAmarela.Runtime.UI`
**Tipo:** `public sealed class` (`MonoBehaviour`)

Observa `DetectorDeInteracao.OnAlvoMudou` — sem polling (Regra de Ouro 8). Sem alvo, o painel
é desativado, então não custa nada quando não está em uso. Formato do texto: `E — {rótulo}`.

> ⚠️ **Cuidado de montagem:** o campo `Raiz` **não pode** ser o próprio GameObject deste
> componente — desativá-lo derrubaria o componente junto, o `OnDisable` desinscreveria do evento
> e o prompt **nunca mais voltaria**. O código detecta e cai para o objeto do Label.
