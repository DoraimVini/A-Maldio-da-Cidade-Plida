---
type: C# Script
title: SeguidorDeAlvo.cs
description: Cálculo puro de movimento de companheiro (seguir com distância de conforto)
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Core/Companion/SeguidorDeAlvo.cs
tags: [core, companion]
timestamp: 2026-07-30T00:00:00Z
---

# SeguidorDeAlvo

**Namespace:** `FavelaAmarela.Core.Companion`
**Tipo:** `public sealed class` (POCO puro)

Movimento do companheiro [Yug-Neth](../../systems/companheiro_mi_go.md) depois de libertado:
fica parado dentro de uma **distância de conforto** e anda até o alvo quando fica para trás.

`CalcularVelocidade(posicaoPropria, posicaoDoAlvo)` devolve o vetor de velocidade — o
adaptador só aplica em `rb.linearVelocity`. Sem FSM de combate: o companheiro é passivo.
