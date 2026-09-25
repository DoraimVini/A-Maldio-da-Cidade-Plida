---
type: Game System
title: Renderização Isométrica (Profundidade e Oclusão)
description: Como o "fake iso" cria profundidade — Y-sorting dinâmico + oclusão por dither (silhueta atrás de paredes altas).
tags: [rendering, isometric, y-sort, occlusion, shader, depth]
timestamp: 2026-07-17T18:00:00Z
---

# Renderização Isométrica (Profundidade e Oclusão)

O jogo **não** usa isométrico 3D nem Tilemap isométrico (losango). É um "fake iso": câmera ortográfica com `Quaternion.identity`, mundo no plano XY, e a sensação de profundidade vem de **Y-sorting** + do remapeamento de input (`PlayerMovement.ConvertToIsometric`). Ver skill `favela-isometric-standards`.

## Y-sorting: estático + dinâmico

- **Geometria estática** (paredes, chão): o `LevelBlockoutGenerator` seta `sortingOrder = -worldCenter.y * 10` uma vez na geração (chão fixo em 0).
- **Atores que se movem** (Damião, inimigos): o componente **`DynamicYSort`** (`Runtime.Rendering`) atualiza `sortingOrder = -y * 10` em `LateUpdate`, usando o **mesmo fator (10)** do gerador para sortar de forma consistente contra a geometria. Sem ele, atores móveis renderizam numa ordem fixa e a profundidade não funciona — foi o pré-requisito que faltava para a oclusão existir.
  - `offsetPes`: permite sortar pela BASE (pés) do sprite em vez do centro, importante quando a arte tiver pivot central.
  - Só escreve quando o valor arredondado muda (sem alocação, regra §1).

## Oclusão por dither (silhueta atrás de paredes altas)

Quando Damião passa **atrás** de uma parede/casa alta (que o Y-sort desenha por cima dele), a parede abre **buracos em padrão dither** e a silhueta do boneco aparece por eles — em vez de ele sumir por completo.

- **Shader** `FavelaAmarela/SpriteDitherOcclusion` (Built-in RP): recorta pixels via `clip(limiar_Bayer4x4 - _DitherAmount)` em screen-space. `_DitherAmount` 0 = opaco, 0.5 ≈ xadrez, 1 = quase todo furado.
- **Componente** `OcclusaoDitherFade` (`Runtime.Rendering`): vai na parede alta; detecta o jogador atrás (trigger + comparação de Y: oclui quando `jogador.y > parede.y`) e faz o fade do `_DitherAmount` via `MaterialPropertyBlock` (sem instanciar material). Event-driven.
- Os buracos revelam o que foi desenhado **antes** (atrás) — o jogador. Se parede e chão têm a mesma cor, a silhueta só "aparece" onde o boneco contrasta; com arte real de cores distintas, lê com clareza.

## Transparency Sort Mode (divergência a reconciliar)

A skill `favela-isometric-standards` manda `Transparency Sort Mode = Custom Axis (0,1,0)`, mas o `GraphicsSettings` do projeto está em **Default (0)** com eixo (0,0,1). Na prática não quebra porque o projeto ordena por `sortingOrder` explícito (não pelo sort axis) — o axis só desempata sprites de mesma ordem. Reconciliar ao mexer no pipeline (ex.: switch pra URP).

## Planejado

- **Tilemap só para o CHÃO** do deserto (célula iso 1×0.5) — para pintar o cenário rápido. Paredes/casas altas continuam **sprites Y-sorted** (não tiles), senão o Y-sort de telhados altos contra um ator que se move quebra (z-fighting). Ver decisão em `log.md` (2026-07-17).

## Status

- ✅ `DynamicYSort`, `SpriteDitherOcclusion.shader`, `OcclusaoDitherFade` — implementados, oclusão provada em Play (grey-box).
- ⏳ Tilemap de chão, arte real (paredes/casas altas, Damião, deserto).
