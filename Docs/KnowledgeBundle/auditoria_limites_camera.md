---
type: Audit Report
title: Limites da câmera por cena
---

# Limites da câmera por cena

Gerado por `Tools/Camera/Auditar limites da câmera`. **Não escreve nada nas cenas.**

## 1. O que o `ResolverLimites` encontra hoje

Ele procura colisores cujo nome comece com `Limite_`.

| cena | câmera | `Limite_*` | trava hoje? |
|---|---|---|---|
| Cena_Menu | sim | 0 | **não** |
| Deserto_Hali | sim | 4 | sim |
| Tumba_De_Alhazred | sim | 0 | **não** |
| Santuario_Yhtill | sim | 0 | **não** |
| Castelo_Carcosa | sim | 0 | **não** |
| Portoes_Das_Ruinas | sim | 0 | **não** |

**5 de 6 cenas não travam a câmera.**

## 2. As fontes candidatas, medidas

| cena | paredes nomeadas (`Limite_`/`Parede_`/`Muro_`) | tilemaps (pintado) |
|---|---|---|
| Cena_Menu | 0× — | 0× — |
| Deserto_Hali | 4× (-44.0, -32.0) a (44.0, 32.0) · 88.0 × 64.0 | 1× (-114.0, -57.0) a (114.0, 57.0) · 228.0 × 114.0 |
| Tumba_De_Alhazred | 0× — | 2× (-6.5, -22.1) a (51.0, 6.7) · 57.5 × 28.8 |
| Santuario_Yhtill | 4× (-8.0, -5.5) a (8.0, 5.5) · 16.0 × 11.0 | 3× (-15.0, -7.5) a (15.0, 7.5) · 30.0 × 15.0 |
| Castelo_Carcosa | 0× — | 2× (-107.0, -36.0) a (107.0, 71.0) · 214.0 × 107.0 |
| Portoes_Das_Ruinas | 0× — | 2× (-34.0, -17.0) a (34.0, 17.0) · 68.0 × 34.0 |

## 3. Tilemap: extensão declarada vs. pintada

`localBounds` sai de `m_Size`, a área **alocada**. `CompressBounds()` encolhe para o que está de fato pintado. Divergência aqui significa que usar `localBounds` cru daria um limite maior que o nível real.

| cena | declarado | pintado | diferença |
|---|---|---|---|
| Cena_Menu | — | — | — |
| Deserto_Hali | (-114.0, -57.0) a (114.0, 57.0) · 228.0 × 114.0 | (-114.0, -57.0) a (114.0, 57.0) · 228.0 × 114.0 | 0.0 × 0.0 |
| Tumba_De_Alhazred | (-6.5, -22.1) a (51.0, 6.7) · 57.5 × 28.8 | (-6.5, -22.1) a (51.0, 6.7) · 57.5 × 28.8 | 0.0 × 0.0 |
| Santuario_Yhtill | (-15.0, -7.5) a (15.0, 7.5) · 30.0 × 15.0 | (-15.0, -7.5) a (15.0, 7.5) · 30.0 × 15.0 | 0.0 × 0.0 |
| Castelo_Carcosa | (-107.0, -36.0) a (107.0, 71.0) · 214.0 × 107.0 | (-107.0, -36.0) a (107.0, 71.0) · 214.0 × 107.0 | 0.0 × 0.0 |
| Portoes_Das_Ruinas | (-34.0, -17.0) a (34.0, 17.0) · 68.0 × 34.0 | (-34.0, -17.0) a (34.0, 17.0) · 68.0 × 34.0 | 0.0 × 0.0 |

## 4. Parede vs. tilemap: qual diz a verdade?

Se o tilemap for **maior** que as paredes, travar por ele deixa a câmera passar da área jogável. Se for **menor**, travar pelas paredes deixa a câmera mostrar chão não pintado.

| cena | tilemap − parede (largura) | (altura) | leitura |
|---|---|---|---|
| Cena_Menu | — | — | sem parede nomeada: o tilemap é a **única** fonte |
| Deserto_Hali | 140.0 | 50.0 | tilemap **maior** — travar por ele passaria da área jogável |
| Tumba_De_Alhazred | — | — | sem parede nomeada: o tilemap é a **única** fonte |
| Santuario_Yhtill | 14.0 | 4.0 | tilemap **maior** — travar por ele passaria da área jogável |
| Castelo_Carcosa | — | — | sem parede nomeada: o tilemap é a **única** fonte |
| Portoes_Das_Ruinas | — | — | sem parede nomeada: o tilemap é a **única** fonte |

