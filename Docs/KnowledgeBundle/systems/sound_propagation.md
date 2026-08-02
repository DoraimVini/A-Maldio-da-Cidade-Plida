---
type: Game System
title: Propagação Sonora
description: Sistema de broadcast de sons que alimenta a detecção dos Cultistas.
tags: [stealth, sound, broadcast, enemies]
timestamp: 2026-07-07T11:00:00Z
---

# Propagação Sonora

O som é a **principal mecânica de detecção** do jogo. O jogador emite sons ao se mover, e esses sons se propagam como "ondas" que os Cultistas podem ouvir.

## Modelo de Propagação

Cada som é modelado como `SomEmitido`:
- **Origem** (`Vector2`): posição de onde o som foi emitido
- **RaioEfetivo** (`float`): distância máxima que o som alcança

Um Cultista só reage ao som se a distância entre ele e a origem for **menor ou igual** ao raio efetivo.

## Fluxo de Dados

```
Jogador (PlayerMovement/PlayerStealthState)
    │ emite
    ▼
SoundBroadcastService.Emitir(SomEmitido)
    │ dispara evento OnSomEmitido
    ▼
CultistaAI (adapter) → CultistaFSM.ReceberEstimuloSonoro()
```

## Regras de Design

- Sons **mais altos** (correr, derrubar objetos) têm raio efetivo maior
- Sons **silenciosos** (andar agachado) têm raio efetivo menor ou zero
- A [Esquiva](esquiva.md) emite um pulso de som ao ser usada — é um movimento brusco, e por isso não é uma saída silenciosa mesmo em modo furtivo
- `SomEmitido` é uma `readonly struct` para evitar alocação em hot paths
