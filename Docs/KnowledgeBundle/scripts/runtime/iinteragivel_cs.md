---
type: C# Script
title: IInteragivel.cs
description: Contrato de qualquer objeto do mundo usável pelo botão E
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Interaction/IInteragivel.cs
tags: [runtime, interacao, contrato]
timestamp: 2026-07-30T00:00:00Z
---

# IInteragivel

**Namespace:** `FavelaAmarela.Runtime.Interaction`
**Tipo:** `public interface`

Baú, colecionável, porta, NPC. Vive no **Runtime** (e não no Core) de propósito: as
implementações são `MonoBehaviour` presos a objetos de cena — a regra de *qual* alvo vence é
que é pura, e mora no Core.

- `RotuloDeInteracao` — texto visível ao jogador, no infinitivo ("Abrir o baú"). Passa pela skill `favela-lore-enforcer`
- `PodeInteragir` — vira false quando a ação esgota (baú aberto), sumindo do prompt
- `PrioridadeDeInteracao` — desempate; itens de progressão usam 10, o boss usa 100
- `PosicaoDeInteracao`, `Interagir(quemInterage)`

Implementado hoje por: `BauDaTumba`, `PatuaPickup`, `AbdulAlhazredAI`.
