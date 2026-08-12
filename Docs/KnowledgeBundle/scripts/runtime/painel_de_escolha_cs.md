---
type: C# Script
title: PainelDeEscolha.cs
description: UI de escolha de diálogo (navegável por setas + E)
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/UI/PainelDeEscolha.cs
tags: [runtime, ui, dialogo]
timestamp: 2026-07-30T00:00:00Z
---

# PainelDeEscolha

**Namespace:** `FavelaAmarela.Runtime.UI`
**Tipo:** `public sealed class` (`MonoBehaviour`)

Apresenta as opções de um diálogo ramificado e devolve a escolha por callback. Usa o POCO
[NavegadorDeOpcoes](../core/navegador_de_opcoes_cs.md) para a regra de cursor.

Trava o movimento do jogador enquanto está aberto (`PlayerMovement.MovimentoBloqueado`) — não dá
para sair andando no meio de uma decisão.

Primeiro caso de uso: a bifurcação **Lutar / Concordar** com Abdul.
