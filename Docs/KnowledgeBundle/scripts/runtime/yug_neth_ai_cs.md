---
type: C# Script
title: YugNethAI.cs
description: Adaptador do companheiro Yug-Neth (cativo → livre)
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Enemies/YugNethAI.cs
tags: [runtime, enemies, companion]
timestamp: 2026-07-30T00:00:00Z
---

# YugNethAI

**Namespace:** `FavelaAmarela.Runtime.Enemies`
**Tipo:** `public sealed class` (`MonoBehaviour`)

O filhote Mi-Go acorrentado por Abdul — **companion obrigatório** para abrir os Portões de
Carcosa. Frágil e passivo: não ataca. Ver [Yug-Neth](../../systems/companheiro_mi_go.md).

**Dois momentos de vida:**
- **Cativo** (padrão): vaga de um lado para o outro perto de onde foi preso (`PatrolRoute` ping-pong, reaproveitando a peça do `CultistaAI.Errante`). Não segue ninguém e **não é alvo de nada durante a luta** — ainda está sob controle de Abdul.
- **Livre** (após `Bind(Transform)`): segue quem o libertou via [SeguidorDeAlvo](../core/seguidor_de_alvo_cs.md).

`OnYugNethAbatido` → o `GameManager` encerra a run com `TipoDeDerrota.EscoltaPerdida`: sem ele
não há progressão, e **não existe resgate** (mesma lógica da escolta da Ashley em RE4).
