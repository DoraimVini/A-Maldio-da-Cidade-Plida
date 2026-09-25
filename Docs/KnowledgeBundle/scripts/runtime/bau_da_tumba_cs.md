---
type: C# Script
title: BauDaTumba.cs
description: Baú que sorteia uma das três armas da Tumba (RNG)
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/GameLoop/BauDaTumba.cs
tags: [runtime, gameloop, interacao, combate]
timestamp: 2026-07-30T00:00:00Z
---

# BauDaTumba

**Namespace:** `FavelaAmarela.Runtime.GameLoop`
**Tipo:** `public sealed class` (`MonoBehaviour`, implementa `IInteragivel`)

Ao ser aberto (**botão E**, "Abrir o baú"), **sorteia** uma das três armas seladas (Cravo de
Aklo, Estilete de Irem, Alfanje de Alhazred) e a equipa na Mão Física. **Não é escolha — é RNG**,
e é o que faz a build variar entre partidas.

Abre por interação deliberada, não por encostar: é um baú, o jogador decide abrir.
Prioridade 10. `PodeInteragir` vira false depois de aberto (um baú entrega uma arma só).

A regra do sorteio vive no Core (`SorteioDeArmaDaTumba`); aqui só há o gatilho e o feedback.
