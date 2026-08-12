---
type: C# Script
title: SeletorDeInteracao.cs
description: Regra pura de qual objeto o jogador interage ao apertar o botão
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Core/Interaction/SeletorDeInteracao.cs
tags: [core, interacao]
timestamp: 2026-07-30T00:00:00Z
---

# SeletorDeInteracao

**Namespace:** `FavelaAmarela.Core.Interaction`
**Tipo:** `public sealed class` (POCO puro)

Responde "**qual** alvo vence?" — a pergunta "quem está por perto?" é da Unity
(`OverlapCircle` no `DetectorDeInteracao`). Ver [Interação com o Mundo](../../systems/interacao.md).

**Ordem de decisão:** descarta indisponíveis e fora de alcance → maior `Prioridade` vence →
empatou, o mais perto → empatou de novo, **menor `Id`**.

Esse último critério não é detalhe: sem ele, dois objetos à mesma distância fariam o prompt
**piscar entre os dois** conforme a ordem que o `Physics2D` devolvesse os colisores a cada
frame. Há teste travando isso.

Recebe `array + contagem` (em vez de coleção) porque o detector chama a cada frame com
buffer pré-alocado — Regra de Ouro 1, zero lixo. 10 testes EditMode.
