---
type: Game System
title: Stealth
description: Mecânica geral de furtividade do jogador.
tags: [stealth, player, core-loop]
timestamp: 2026-07-07T11:00:00Z
---

# Sistema de Stealth

O stealth é o pilar central da gameplay. "A Maldição da Cidade Pálida" é um jogo de **stealth/horror**, não um ARPG. O jogador deve evitar combate direto.

## Pilares do Stealth

1. **Detecção Sonora** — O jogador emite sons que propagam via [Propagação Sonora](sound_propagation.md). Ações mais ruidosas aumentam o risco.
2. **Estados do Jogador** — Gerenciados por `PlayerStealthState` no adapter. Define se o jogador está agachado, caminhando ou correndo.
3. **Cone de Visão** — (a implementar) Cultistas terão detecção visual além da sonora.

## Interações com Outros Sistemas

- [Resiliência Mental](resiliencia_mental.md): Estar em estado de **Pânico** pode tornar o jogador mais barulhento
- [IA do Cultista](cultista_ai.md): Consome os dados de som para decidir perseguição
- [Esquiva](esquiva.md): Permite escapar de situações de detecção sem custo de sanidade
- [Salto Dimensional](dimensional_leap.md): Permite atravessar paredes mas custa [Resiliência Mental](resiliencia_mental.md) e pode emitir som
