---
type: Game System
title: Patrulha
description: Sistema de rotas de patrulha dos Cultistas com suporte a loop e ping-pong.
tags: [enemies, ai, movement, patrol]
timestamp: 2026-07-07T11:00:00Z
---

# Sistema de Patrulha

Define como os Cultistas se movimentam pelo mapa no estado **Errante**.

## Modos de Rota

| Modo | Comportamento |
|------|---------------|
| **Loop** (`loop = true`) | Ao chegar no último waypoint, volta para o primeiro. Rota circular. |
| **Ping-Pong** (`loop = false`) | Ao chegar no fim, inverte a direção e percorre a rota de volta. |

## Regras

- Uma rota é definida por um array de `Vector2` (waypoints)
- O Cultista se move em direção ao `AlvoAtual` (waypoint corrente)
- Quando a distância ao alvo é menor que o `raioDeChegada`, avança para o próximo
- Rotas com um único waypoint: o Cultista fica parado (guarda)

## Integração

- Consumido pela [IA do Cultista](cultista_ai.md) durante o estado Errante
- O adapter `CultistaAI` (Runtime) lê `AlvoAtual` para definir a direção de movimento via `Rigidbody2D`
