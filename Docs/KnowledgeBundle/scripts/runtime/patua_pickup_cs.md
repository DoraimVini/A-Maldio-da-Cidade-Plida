---
type: C# Script
title: PatuaPickup.cs
description: Colecionável do patuá na Zona 5 — coletado por interação (botão E); efeito pendente de design
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/GameLoop/PatuaPickup.cs
tags: [runtime, gameloop, progression, interacao]
timestamp: 2026-07-30T00:00:00Z
---

# PatuaPickup

**Namespace:** `FavelaAmarela.Runtime.GameLoop`
**Tipo:** `public sealed class` (herda de `MonoBehaviour`, implementa `IInteragivel`, `[RequireComponent(Collider2D)]`)

Colecionável do patuá na Zona 5. Coletado por **interação deliberada** (botão **E**), não
por encostar — colecionável é escolha do jogador, e o prompt *"Recolher o patuá"* também
sinaliza que ali há algo importante. Ver [Interação com o Mundo](../../systems/interacao.md).

> ⚠️ **Efeito pendente de design (2026-07-30).** O patuá foi revisto e **não destrava mais o
> Salto Dimensional** — essa habilidade foi integralmente removida do jogo. O item continua
> na cena e coletável, mas seu novo propósito **ainda não foi definido pelo Vini**. Há um
> `TODO(design)` em `Interagir()` marcando exatamente onde o efeito novo entra. Não inventar
> um efeito por conta própria.

## Implementação de `IInteragivel`

| Membro | Valor |
|---|---|
| `RotuloDeInteracao` | `"Recolher o patuá"` |
| `PodeInteragir` | `!_coletado` — some do prompt depois de coletado |
| `PrioridadeDeInteracao` | `10` (item de progressão: ganha de cenário ao lado) |
| `Interagir(quemInterage)` | Marca coletado, mostra a mensagem no `TutorialHintUI` e desativa o próprio `GameObject` |
