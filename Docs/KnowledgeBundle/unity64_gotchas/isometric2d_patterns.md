---
type: Unity Gotcha
title: Isométrico 2D
description: Padrões de implementação isométrica 2D sem câmera inclinada.
tags: [isometric, camera, y-sorting, ppu]
timestamp: 2026-07-07T11:00:00Z
---

# Isométrico 2D — Padrões do Projeto

A "sensação" isométrica vem do **Y-sorting** e do **remapeamento de input**, não de uma câmera 3D inclinada.

## Constantes Fixas

| Propriedade | Valor | Motivo |
|-------------|-------|--------|
| Câmera rotação | `Quaternion.identity` | **Nunca** tiltar a câmera fisicamente |
| PPU (Pixels Per Unit) | **16** | Pixel art consistency |
| Y-sorting | `sortingOrder = -worldCenter.y` | Custom Axis sorting |
| gravityScale | **0** | Sem gravidade no isométrico |

## Remapeamento de Input

O input é remapeado em `PlayerMovement.ConvertToIsometric()` para que "cima" no controle mova o personagem na diagonal isométrica.

## Regra

Qualquer alteração em física, câmera, prefab de sala ou Rigidbody de inimigo/player **DEVE** respeitar essas constantes.

> **Para detalhes completos**, consulte a skill `favela-isometric-standards` do Claude Code.
