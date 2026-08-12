---
type: C# Script
title: AbdulAlhazredAI.cs
description: Adaptador do boss Abdul — FSM, escudo, Pedras, conversa ramificada e drop
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Enemies/AbdulAlhazredAI.cs
tags: [runtime, enemies, boss, interacao]
timestamp: 2026-07-30T00:00:00Z
---

# AbdulAlhazredAI

**Namespace:** `FavelaAmarela.Runtime.Enemies`
**Tipo:** `public sealed class` (`MonoBehaviour`, implementa `IDanificavel` e `IInteragivel`)

Liga a [AbdulFSM](../core/abdul_fsm_cs.md) à cena. Nenhuma regra de luta aqui — mesma divisão
de `CultistaFSM`/`CultistaAI`. Design em [Luta contra Abdul](../../systems/boss_abdul.md).

**Como `IInteragivel`:** enquanto em `Transe`, oferece "Falar com o vulto". Cada aperto avança
uma fala; a última abre a **escolha ramificada** (Lutar / Concordar). Prioridade 100.

**Pedras de Poder por fase:** assina `OnStateChanged` — instancia as Pedras ao entrar na Fase 1
(fazendo `Bind(this)` em cada uma) e destrói as restantes ao sair dela. Elas **não ficam
pré-plantadas na cripta**.

**Traição da trégua:** atacá-lo depois de "Concordar" reabre a luta (o golpe que trai não causa
dano — só desperta).

**Liberta Yug-Neth nos dois caminhos** (`LibertarYugNeth`, idempotente); só o Necronomicon é
exclusivo da vitória em combate.
