---
type: Audit
title: Auditoria do Tilemap
description: Tiles em uso, import das sprites, duplicatas, regras sem vizinhança e o que cada regra de fato desenha.
tags: [tilemap, tiles, isometrico, auditoria]
---

# Auditoria do Tilemap

> Gerado por `Tools/FavelaAmarela/Auditoria: tilemap e tiles`. **Não edite à mão** — rode a ferramenta.

## 1. Tiles em uso

| cena | tilemap | tile | células |
|---|---|---|---|
| Castelo_Carcosa | Colisao | `arena_colisao` | 1708 |
| Castelo_Carcosa | Piso_Castelo | `arena_piso_01` | 1502 |
| Castelo_Carcosa | Piso_Castelo | `arena_piso_02` | 1363 |
| Castelo_Carcosa | Piso_Castelo | `arena_piso_03` | 1359 |
| Deserto_Hali | DesertFloor | `RuleTile_Areia` | 25252 |
| Portoes_Das_Ruinas | Colisao | `arena_colisao` | 528 |
| Portoes_Das_Ruinas | PortoesFloor | `arena_piso_01` | 1434 |
| Portoes_Das_Ruinas | PortoesFloor | `arena_piso_03` | 1348 |
| Portoes_Das_Ruinas | PortoesFloor | `arena_piso_02` | 1314 |
| Santuario_Yhtill | Colisao | `colisao_invisivel` | 116 |
| Santuario_Yhtill | SantuarioFloor | `santuario_piso_01` | 295 |
| Santuario_Yhtill | SantuarioFloor | `santuario_piso_03` | 246 |
| Santuario_Yhtill | SantuarioFloor | `santuario_piso_02` | 243 |
| Tumba_De_Alhazred | Colisao | `colisao_invisivel` | 270 |
| Tumba_De_Alhazred | DesertFloor | `RuleTile_Areia` | 1111 |

## 2. Import das sprites de tile

| sprite | PPU | pivô | malha | filtro | compressão |
|---|---|---|---|---|---|
| `arena_piso_01` | 32 | Center | FullRect | Point | Uncompressed |
| `arena_piso_02` | 32 | Center | FullRect | Point | Uncompressed |
| `arena_piso_03` | 32 | Center | FullRect | Point | Uncompressed |
| `sand_01` | 32 | Center | FullRect | Point | Uncompressed |
| `sand_02` | 32 | Center | FullRect | Point | Uncompressed |
| `sand_03` | 32 | Center | FullRect | Point | Uncompressed |
| `sand_crack` | 32 | Center | FullRect | Point | Uncompressed |
| `sand_pebbles` | 32 | Center | FullRect | Point | Uncompressed |
| `santuario_piso_01` | 32 | Center | FullRect | Point | Uncompressed |
| `santuario_piso_02` | 32 | Center | FullRect | Point | Uncompressed |
| `santuario_piso_03` | 32 | Center | FullRect | Point | Uncompressed |

**Nenhuma divergência de import.**

## 3. Tile assets duplicados

> Dois Tile assets com a **mesma sprite** podem ter colisor ou cor diferentes, e aí dois pedaços de chão idênticos se comportam diferente sem que nada na tela explique.

**Nenhuma duplicata**: 6 Tile asset(s) simples sobre o mesmo número de sprites distintas, um para um.

## 4. Rule Tiles e condições de vizinhança

> Uma regra **sem** condição de vizinhança casa com tudo. Isso é legítimo quando o Rule Tile serve só para variar a sprite — mas significa que **não há borda**: nenhuma célula vai desenhar transição, e o `m_DefaultSprite` nunca é alcançado por falta de regra.

| rule tile | regras | com vizinhança | sprites por regra |
|---|---|---|---|
| `RuleTile_Areia` | 1 | 0 | 5 |

### Sem borda

- `RuleTile_Areia`: 1 regra(s), **nenhuma** com condição de vizinhança — ele varia a sprite e não desenha borda nenhuma

Não é defeito hoje: **não existe transição de terreno neste projeto** — cada cena usa uma família de chão só. Vira defeito no dia em que duas superfícies diferentes se encontrarem.

## 5. O que cada tile de fato desenha

> Medido célula a célula com `Tilemap.GetSprite`, que pergunta ao próprio tile. Para um Tile simples é sempre a mesma sprite; para um Rule Tile em modo `Random`, é a escolha do ruído Perlin — e é a única forma honesta de saber a proporção, porque o `Mathf.PerlinNoise` da Unity não é reproduzível fora da engine.

### Deserto_Hali / DesertFloor — `RuleTile_Areia` (25252 células)

| sprite | células | % |
|---|---|---|
| `sand_03` | 12232 | 48.4% |
| `sand_01` | 4884 | 19.3% |
| `sand_02` | 4074 | 16.1% |
| `sand_crack` | 2056 | 8.1% |
| `sand_pebbles` | 2006 | 7.9% |

### Tumba_De_Alhazred / DesertFloor — `RuleTile_Areia` (1111 células)

| sprite | células | % |
|---|---|---|
| `sand_03` | 543 | 48.9% |
| `sand_01` | 212 | 19.1% |
| `sand_02` | 165 | 14.9% |
| `sand_pebbles` | 100 | 9.0% |
| `sand_crack` | 91 | 8.2% |

