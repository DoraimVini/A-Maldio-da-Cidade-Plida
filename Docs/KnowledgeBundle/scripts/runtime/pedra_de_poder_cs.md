---
type: C# Script
title: PedraDePoder.cs
description: Cenário destrutível que derruba o Escudo Mágico do Abdul (Fase 1)
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Enemies/PedraDePoder.cs
tags: [runtime, enemies, boss]
timestamp: 2026-07-30T00:00:00Z
---

# PedraDePoder

**Namespace:** `FavelaAmarela.Runtime.Enemies`
**Tipo:** `public sealed class` (`MonoBehaviour`, implementa `IDanificavel`)

Sustenta o Escudo Mágico na Fase 1 — quebrá-la abre a **única janela de dano** daquela fase,
o que transforma a Fase 1 numa luta de arena (procurar e quebrar) em vez de bater no escudo.

Não é Aparição Primordial: é cenário destrutível, leva crítico furtivo normalmente.

`Bind(AbdulAlhazredAI)` injeta o boss cujo escudo ela sustenta — chamado por quem a instancia
em runtime, já que as Pedras **nascem na Fase 1** e não ficam plantadas na cena.
