---
type: Game System
title: Auditoria de Colisores 2D
description: Forma, tamanho, offset e material de todo Collider2D em prefabs e cenas do Build Settings, comparado ao corpo desenhado. Gerado por Tools/FavelaAmarela/Auditar Física 2D.
date: 2026-09-04
---

# Auditoria de Colisores 2D

> **Gerado por ferramenta.** Rode `Tools/FavelaAmarela/Auditar Física 2D` para atualizar — edições à mão neste arquivo são perdidas.

141 colisor(es) em 6 cena(s) do Build Settings e nos prefabs do projeto.

## Como ler a coluna "queixa"

Comparar todo colisor com o sprite inteiro **marcaria 100% do elenco**, porque os dois maiores desvios são de propósito:

- a **hurtbox** nasce em `0,72 × 0,86` da silhueta (`Hurtbox.GarantirPara`) — sozinho isso já daria −28% e −14%;
- a **pegada de movimento** é uma área de *chão* de `0,60 × 0,30`, na proporção 2:1 da célula isométrica, num corpo que o sprite desenha com ~2,5 de altura — daria −40% e −88%.

Comparar **por papel** também não bastou enquanto o papel era grosseiro: a primeira versão chamava de pegada todo colisor sólido e acusou 57 de 141 — entre eles as paredes do Santuário, o Lago de Hali e os limites do Deserto, que não têm pegada a respeitar. O que vale hoje:

| papel | o que é | o que se confere |
|---|---|---|
| `Hurtbox` | camada 13/14, ou componente `Hurtbox` | tamanho contra a silhueta × `0,72 / 0,86`, e o centro (limiares **20%** e **0,2** unidade) |
| `Pegada` | colisor sólido de um **ator** — corpo não-estático *mais* sprite | a **proporção de chão** (2:1 ±0,5) e a **linha do pé** (±0,2). *Não* o tamanho absoluto |
| `Cenario` | colisor sólido sem corpo ou sem sprite: parede, tilemap, limite de mapa, portão | nada |
| `Gatilho` | trigger que não é hurtbox: zona, portal, coletável | nada |

> **Por que a `Pegada` não confere tamanho.** `ColisoresDoElencoTests` já guarda os quatro humanos (Damião, Cultista, Abdul, EspectroHali) pelo caminho do prefab, com a pegada calibrada de `0,60 × 0,30`. Repetir a regra aqui, com uma identificação pior, criaria a segunda fonte da verdade que o doc daquele arquivo chama de modo de falha mais repetido do projeto. A proporção e a linha do pé, essas, **ninguém mais confere** — e valem para qualquer espécie.

## Fora do esperado

| origem | objeto | tipo | papel | queixa |
|---|---|---|---|---|
| Castelo_Carcosa | `Castelo_Root/Z2_SalaoDoBanquete/Cortesao_Palido_0` | Box | Pegada | proporção 1:1 — chão isométrico é 2:1 (±0,5) |
| Castelo_Carcosa | `Castelo_Root/Z2_SalaoDoBanquete/Cortesao_Palido_1` | Box | Pegada | proporção 1:1 — chão isométrico é 2:1 (±0,5) |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0` | Box | Pegada | escala não uniforme 0,63 × 0,804 (esticado 1,28× em Y) |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0` | Box | Pegada | escala não uniforme 0,63 × 0,804 (esticado 1,28× em Y) |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0` | Box | Pegada | escala não uniforme 0,63 × 0,804 (esticado 1,28× em Y) |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0` | Box | Pegada | escala não uniforme 0,63 × 0,804 (esticado 1,28× em Y) |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0 (1)` | Box | Pegada | escala não uniforme 0,63 × 0,804 (esticado 1,28× em Y) |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0 (1)` | Box | Pegada | escala não uniforme 0,63 × 0,804 (esticado 1,28× em Y) |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0 (1)/Hurtbox` | Box | Hurtbox | escala não uniforme 0,63 × 0,804 (esticado 1,28× em Y); largura -43% do esperado (0,63 contra 1,11) |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0 (1)/Hurtbox` | Box | Hurtbox | escala não uniforme 0,63 × 0,804 (esticado 1,28× em Y); largura -43% do esperado (0,63 contra 1,11) |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0 (2)` | Box | Pegada | escala não uniforme 0,63 × 0,804 (esticado 1,28× em Y) |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0 (2)` | Box | Pegada | escala não uniforme 0,63 × 0,804 (esticado 1,28× em Y) |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0 (2)/Hurtbox` | Box | Hurtbox | escala não uniforme 0,63 × 0,804 (esticado 1,28× em Y); largura -43% do esperado (0,63 contra 1,11) |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0 (2)/Hurtbox` | Box | Hurtbox | escala não uniforme 0,63 × 0,804 (esticado 1,28× em Y); largura -43% do esperado (0,63 contra 1,11) |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0 (3)` | Box | Pegada | escala não uniforme 0,63 × 0,804 (esticado 1,28× em Y) |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0 (3)` | Box | Pegada | escala não uniforme 0,63 × 0,804 (esticado 1,28× em Y) |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0 (3)/Hurtbox` | Box | Hurtbox | escala não uniforme 0,63 × 0,804 (esticado 1,28× em Y); largura -43% do esperado (0,63 contra 1,11) |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0 (3)/Hurtbox` | Box | Hurtbox | escala não uniforme 0,63 × 0,804 (esticado 1,28× em Y); largura -43% do esperado (0,63 contra 1,11) |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0/Hurtbox` | Box | Hurtbox | escala não uniforme 0,63 × 0,804 (esticado 1,28× em Y); largura -43% do esperado (0,63 contra 1,11) |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0/Hurtbox` | Box | Hurtbox | escala não uniforme 0,63 × 0,804 (esticado 1,28× em Y); largura -43% do esperado (0,63 contra 1,11) |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0/Hurtbox` | Box | Hurtbox | escala não uniforme 0,63 × 0,804 (esticado 1,28× em Y); largura -43% do esperado (0,63 contra 1,11) |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0/Hurtbox` | Box | Hurtbox | escala não uniforme 0,63 × 0,804 (esticado 1,28× em Y); largura -43% do esperado (0,63 contra 1,11) |
| Playtest_RuinasPalidas | `Inimigos_Playtest/Cultista` | Box | Pegada | escala não uniforme 0,592 × 0,751 (esticado 1,27× em Y) |
| Playtest_RuinasPalidas | `Inimigos_Playtest/Cultista` | Box | Pegada | escala não uniforme 0,583 × 0,758 (esticado 1,3× em Y) |
| Playtest_RuinasPalidas | `Inimigos_Playtest/Cultista/Hurtbox` | Box | Hurtbox | escala não uniforme 0,592 × 0,751 (esticado 1,27× em Y) |
| Playtest_RuinasPalidas | `Inimigos_Playtest/Cultista/Hurtbox` | Box | Hurtbox | escala não uniforme 0,583 × 0,758 (esticado 1,3× em Y) |
| Playtest_RuinasPalidas | `TumbaDeAbdul_Conteudo/Abdul_Alhazred/Hurtbox` | Box | Hurtbox | escala não uniforme 1,162 × 2,671 (esticado 2,3× em Y); largura -70% do esperado (0,56 contra 1,89); altura -62% do esperado (2,52 contra 6,6); centro a 2,61 do centro desenhado (limiar 0,2) |
| Playtest_RuinasPalidas | `TumbaDeAbdul_Conteudo/YugNeth` | Box | Pegada | escala não uniforme 0,901 × 1,133 (esticado 1,26× em Y); proporção 0,41:1 — chão isométrico é 2:1 (±0,5); a +0,78 do pé — pegada é área de chão |
| Portoes_Das_Ruinas | `Portoes_Root/Byakhee` | Capsule | Pegada | escala não uniforme 1,021 × 0,938 (esticado 0,92× em Y); proporção 0,73:1 — chão isométrico é 2:1 (±0,5); a +2,4 do pé — pegada é área de chão |
| Portoes_Das_Ruinas | `Portoes_Root/Byakhee` | Circle | Gatilho | escala não uniforme 1,021 × 0,938 (esticado 0,92× em Y) |
| Portoes_Das_Ruinas | `Portoes_Root/Byakhee/Hurtbox` | Box | Hurtbox | escala não uniforme 1,021 × 0,938 (esticado 0,92× em Y) |
| Santuario_Yhtill | `Cassilda` | Circle | Gatilho | escala não uniforme 1,478 × 1,925 (esticado 1,3× em Y) |

## Todos os colisores

Tamanho e centro em **unidades de mundo** (já multiplicados pela escala); offset é local, como aparece no Inspector.

| origem | objeto | tipo | papel | tam. (L×A) | raio | offset | centro | trigger | composição | material | sprite (L×A) |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Abdul_Alhazred | `Abdul_Alhazred` | Box | Cenario | 0,6×0,3 | — | 0, 0 | 39,41, -16,2 | não | None | — | — |
| Abdul_Alhazred | `Abdul_Alhazred/Hurtbox` | Box | Hurtbox | 1,4×2 | — | 0, 1,03 | 39,41, -15,17 | sim | None | — | — |
| Byakhee | `Byakhee` | Capsule | Cenario | 2×3 | — | 0, 2,56 | 0, 2,56 | não | None | — | — |
| Byakhee | `Byakhee/Hurtbox` | Box | Hurtbox | 3,69×4,41 | — | 0, 2,56 | 0, 2,56 | sim | None | — | — |
| Cassilda | `Cassilda` | Circle | Gatilho | 2,4×2,4 | 1,2 | 0, 0 | 0, 0 | sim | None | — | — |
| Castelo_Carcosa | `Castelo_Root/Castelo_Grid/Colisao` | TilemapCollider2D | Cenario | 0×0 | — | 0, 0 | 0, 0 | não | Merge | — | — |
| Castelo_Carcosa | `Castelo_Root/Castelo_Grid/Colisao` | Composite | Cenario | 35×107 | — | 0, 0 | 0, 0 | não | None | — | — |
| Castelo_Carcosa | `Castelo_Root/Z1_PortoesInternos` | Box | Gatilho | 20×10 | — | 0, 0 | 0, -30 | sim | None | — | — |
| Castelo_Carcosa | `Castelo_Root/Z1_PortoesInternos/Refugio_DosPortoes` | Circle | Gatilho | 4×4 | 2 | 0, 0 | -5, -30 | sim | None | — | 0,16×0,16 |
| Castelo_Carcosa | `Castelo_Root/Z2_SalaoDoBanquete` | Box | Gatilho | 30×15 | — | 0, 0 | 0, 0 | sim | None | — | — |
| Castelo_Carcosa | `Castelo_Root/Z2_SalaoDoBanquete/Cortesao_Palido_0` | Box | Pegada | 0,6×0,6 | — | 0, 0 | -9, 0 | não | None | — | 0,91×1,72 |
| Castelo_Carcosa | `Castelo_Root/Z2_SalaoDoBanquete/Cortesao_Palido_1` | Box | Pegada | 0,6×0,6 | — | 0, 0 | 0, 6 | não | None | — | 0,91×1,72 |
| Castelo_Carcosa | `Castelo_Root/Z2_SalaoDoBanquete/Nobre_Fossilizado_0` | Box | Cenario | 1,2×1 | — | 0, -0,5 | -10, 1,5 | não | None | — | 0,16×0,16 |
| Castelo_Carcosa | `Castelo_Root/Z2_SalaoDoBanquete/Nobre_Fossilizado_1` | Box | Cenario | 1,2×1 | — | 0, -0,5 | -3, 4,5 | não | None | — | 0,16×0,16 |
| Castelo_Carcosa | `Castelo_Root/Z2_SalaoDoBanquete/Nobre_Fossilizado_2` | Box | Cenario | 1,2×1 | — | 0, -0,5 | 3, 4,5 | não | None | — | 0,16×0,16 |
| Castelo_Carcosa | `Castelo_Root/Z2_SalaoDoBanquete/Nobre_Fossilizado_3` | Box | Cenario | 1,2×1 | — | 0, -0,5 | 10, 1,5 | não | None | — | 0,16×0,16 |
| Castelo_Carcosa | `Castelo_Root/Z2_SalaoDoBanquete/Nobre_Fossilizado_4` | Box | Cenario | 1,2×1 | — | 0, -0,5 | -6, -4,5 | não | None | — | 0,16×0,16 |
| Castelo_Carcosa | `Castelo_Root/Z2_SalaoDoBanquete/Nobre_Fossilizado_5` | Box | Cenario | 1,2×1 | — | 0, -0,5 | 6, -4,5 | não | None | — | 0,16×0,16 |
| Castelo_Carcosa | `Castelo_Root/Z3_BibliotecaEsquecida` | Box | Gatilho | 30×15 | — | 0, 0 | 0, 30 | sim | None | — | — |
| Castelo_Carcosa | `Castelo_Root/Z3_BibliotecaEsquecida/Pressao_Psiquica_0` | Circle | Gatilho | 12×12 | 6 | 0, 0 | -10, 32 | sim | None | — | — |
| Castelo_Carcosa | `Castelo_Root/Z3_BibliotecaEsquecida/Pressao_Psiquica_1` | Circle | Gatilho | 12×12 | 6 | 0, 0 | 10, 32 | sim | None | — | — |
| Castelo_Carcosa | `Castelo_Root/Z3_BibliotecaEsquecida/Pressao_Psiquica_2` | Circle | Gatilho | 12×12 | 6 | 0, 0 | 0, 23 | sim | None | — | — |
| Castelo_Carcosa | `Castelo_Root/Z5_TronoDeAldebaran` | Box | Gatilho | 30×15 | — | 0, 0 | 0, 62 | sim | None | — | — |
| Castelo_Carcosa | `Castelo_Root/Z5_TronoDeAldebaran/Ponto_Focal_anel_sinal_amarelo` | Circle | Gatilho | 2,4×2,4 | 1,2 | 0, 0 | 0, 56 | sim | None | — | 0,16×0,16 |
| Castelo_Carcosa | `Castelo_Root/Z5_TronoDeAldebaran/Ponto_Focal_necronomicon` | Circle | Gatilho | 2,4×2,4 | 1,2 | 0, 0 | -10, 61 | sim | None | — | 0,16×0,16 |
| Castelo_Carcosa | `Castelo_Root/Z5_TronoDeAldebaran/Ponto_Focal_patua_luas_gemeas` | Circle | Gatilho | 2,4×2,4 | 1,2 | 0, 0 | 10, 61 | sim | None | — | 0,16×0,16 |
| Castelo_Carcosa | `Castelo_Root/Z5_TronoDeAldebaran/ReiEmAmarelo` | Circle | Cenario | 1×1 | 0,5 | 0, 0,88 | 0, 67,88 | não | None | — | 2,75×4,03 |
| Castelo_Carcosa | `Player_Damiao` | Box | Pegada | 0,6×0,3 | — | 0, 0 | 0, -33 | não | None | — | 0,99×2,3 |
| Castelo_Carcosa | `Player_Damiao/Hurtbox` | Capsule | Hurtbox | 0,7×1,9 | — | 0, 1,25 | 0, -31,95 | sim | None | — | 0,99×2,3 |
| CoisaDoCemiterio | `CoisaDoCemiterio` | Box | Gatilho | 2,5×3 | — | 0, 0 | 0, 0 | sim | None | — | — |
| ConeDeGelo | `ConeDeGelo` | Box | Gatilho | 0,6×0,3 | — | 0, 0 | 0, 0 | sim | None | — | — |
| Cultista | `Cultista` | Box | Cenario | 0,6×0,3 | — | 0, 0 | 0, 0 | não | None | — | — |
| Cultista | `Cultista/Hurtbox` | Box | Hurtbox | 1,51×1,98 | — | 0, 1,28 | 0, 1,1 | sim | None | — | — |
| Deserto_Hali | `Consumiveis_Deserto/Coletavel_consumivel_agua_cacimba_0` | Circle | Gatilho | 0,6×0,6 | 0,3 | 0, 0 | 7,29, -7,66 | sim | None | — | 0,1×0,1 |
| Deserto_Hali | `Consumiveis_Deserto/Coletavel_consumivel_agua_cacimba_1` | Circle | Gatilho | 0,6×0,6 | 0,3 | 0, 0 | 24,29, 7,06 | sim | None | — | 0,1×0,1 |
| Deserto_Hali | `Consumiveis_Deserto/Coletavel_consumivel_agua_cacimba_2` | Circle | Gatilho | 0,6×0,6 | 0,3 | 0, 0 | 9,43, 22,4 | sim | None | — | 0,1×0,1 |
| Deserto_Hali | `Consumiveis_Deserto/Coletavel_consumivel_agua_cacimba_3` | Circle | Gatilho | 0,6×0,6 | 0,3 | 0, 0 | -30,89, 20,44 | sim | None | — | 0,1×0,1 |
| Deserto_Hali | `Consumiveis_Deserto/Coletavel_consumivel_erva_ancoragem_0` | Circle | Gatilho | 0,6×0,6 | 0,3 | 0, 0 | -25,42, 0,76 | sim | None | — | 0,1×0,1 |
| Deserto_Hali | `Consumiveis_Deserto/Coletavel_consumivel_erva_ancoragem_1` | Circle | Gatilho | 0,6×0,6 | 0,3 | 0, 0 | -9,73, -11,96 | sim | None | — | 0,1×0,1 |
| Deserto_Hali | `Consumiveis_Deserto/Coletavel_consumivel_erva_ancoragem_2` | Circle | Gatilho | 0,6×0,6 | 0,3 | 0, 0 | 28,22, 7,75 | sim | None | — | 0,1×0,1 |
| Deserto_Hali | `Consumiveis_Deserto/Coletavel_consumivel_raiz_yhtill_0` | Circle | Gatilho | 0,6×0,6 | 0,3 | 0, 0 | -8,19, 23,1 | sim | None | — | 0,1×0,1 |
| Deserto_Hali | `Consumiveis_Deserto/Coletavel_consumivel_raiz_yhtill_1` | Circle | Gatilho | 0,6×0,6 | 0,3 | 0, 0 | -22,88, 20,33 | sim | None | — | 0,1×0,1 |
| Deserto_Hali | `Deserto_Root/Coletavel_CartaDasAreias` | Box | Gatilho | 1×1 | — | 0, 0 | -31,5, 12,3 | sim | None | — | — |
| Deserto_Hali | `Deserto_Root/Entrada_TumbaAlhazred` | Box | Gatilho | 2×2 | — | 0, 0 | -31,4, -13,98 | sim | None | — | 8×5,75 |
| Deserto_Hali | `Deserto_Root/Lago_De_Hali` | Polygon | Cenario | 15,85×9,62 | — | 0, 0 | 0,88, -13,99 | não | None | — | 20,5×17,9 |
| Deserto_Hali | `Deserto_Root/Limite_Leste` | Box | Cenario | 1×64 | — | 0, 0 | 43, 0 | não | None | — | 1×64 |
| Deserto_Hali | `Deserto_Root/Limite_Norte` | Box | Cenario | 88×1 | — | 0, 0 | 0, 31 | não | None | — | 88×1 |
| Deserto_Hali | `Deserto_Root/Limite_Oeste` | Box | Cenario | 1×64 | — | 0, 0 | -43, 0 | não | None | — | 1×64 |
| Deserto_Hali | `Deserto_Root/Limite_Sul` | Box | Cenario | 88×1 | — | 0, 0 | 0, -31 | não | None | — | 88×1 |
| Deserto_Hali | `Deserto_Root/Portoes_DasRuinas/Entrada_DosPortoes` | Box | Gatilho | 6×4 | — | 0, 0 | 1,4, 23,02 | sim | None | — | 8×8,25 |
| Deserto_Hali | `Deserto_Root/Santuario_Yhtill` | Box | Gatilho | 6×4 | — | 0, 1,69 | -34, 17,18 | sim | None | — | 8×6,75 |
| Deserto_Hali | `Deserto_Root/Veu_DaTempestade_Templo` | Box | Gatilho | 14×66 | — | 0, 0 | 38, 0 | sim | None | — | — |
| Deserto_Hali | `Fragmento_0` | Circle | Gatilho | 1,6×1,6 | 0,8 | 0, 0 | -20, -24 | sim | None | — | 0,2×0,2 |
| Deserto_Hali | `Inimigos_Deserto/CoisaDoCemiterio` | Box | Gatilho | 2,5×3 | — | 0, 0 | 1,63, 11,26 | sim | None | — | 2,5×3 |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_LesteTemploSerpente_0` | Box | Pegada | 0,6×0,3 | — | 0, 0 | 33,28, 19,86 | não | None | — | 2,09×2,31 |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_LesteTemploSerpente_0/Hurtbox` | Box | Hurtbox | 1,51×1,98 | — | 0, 1,28 | 33,28, 20,96 | sim | None | — | 2,09×2,31 |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0` | Box | Pegada | 0,44×0,28 | — | 0, 0 | -7,78, -3,56 | não | None | — | 1,54×2,16 |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0` | Box | Pegada | 0,44×0,28 | — | 0, 0 | 12,78, 24,68 | não | None | — | 1,54×2,16 |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0` | Box | Pegada | 0,44×0,28 | — | 0, 0 | 3,84, -8,56 | não | None | — | 1,54×2,16 |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0` | Box | Pegada | 0,44×0,28 | — | 0, 0 | -34,93, 7,3 | não | None | — | 1,54×2,16 |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0 (1)` | Box | Pegada | 0,44×0,28 | — | 0, 0 | 19, 4,38 | não | None | — | 1,54×2,16 |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0 (1)` | Box | Pegada | 0,44×0,28 | — | 0, 0 | -31,9, -2,84 | não | None | — | 1,54×2,16 |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0 (1)/Hurtbox` | Box | Hurtbox | 0,63×1,86 | — | 0,12, 1,28 | 19,08, 5,41 | sim | None | — | 1,54×2,16 |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0 (1)/Hurtbox` | Box | Hurtbox | 0,63×1,86 | — | 0,12, 1,28 | -31,82, -1,81 | sim | None | — | 1,54×2,16 |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0 (2)` | Box | Pegada | 0,44×0,28 | — | 0, 0 | 36,14, 0,86 | não | None | — | 1,54×2,16 |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0 (2)` | Box | Pegada | 0,44×0,28 | — | 0, 0 | 23,46, -1,22 | não | None | — | 1,54×2,16 |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0 (2)/Hurtbox` | Box | Hurtbox | 0,63×1,86 | — | 0,12, 1,28 | 36,22, 1,89 | sim | None | — | 1,54×2,16 |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0 (2)/Hurtbox` | Box | Hurtbox | 0,63×1,86 | — | 0,12, 1,28 | 23,54, -0,19 | sim | None | — | 1,54×2,16 |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0 (3)` | Box | Pegada | 0,44×0,28 | — | 0, 0 | 30,12, -16,78 | não | None | — | 1,54×2,16 |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0 (3)` | Box | Pegada | 0,44×0,28 | — | 0, 0 | -26,36, 26,22 | não | None | — | 1,54×2,16 |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0 (3)/Hurtbox` | Box | Hurtbox | 0,63×1,86 | — | 0,12, 1,28 | 30,2, -15,75 | sim | None | — | 1,54×2,16 |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0 (3)/Hurtbox` | Box | Hurtbox | 0,63×1,86 | — | 0,12, 1,28 | -26,28, 27,25 | sim | None | — | 1,54×2,16 |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0/Hurtbox` | Box | Hurtbox | 0,63×1,86 | — | 0,12, 1,28 | -7,7, -2,53 | sim | None | — | 1,54×2,16 |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0/Hurtbox` | Box | Hurtbox | 0,63×1,86 | — | 0,12, 1,28 | 12,86, 25,71 | sim | None | — | 1,54×2,16 |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0/Hurtbox` | Box | Hurtbox | 0,63×1,86 | — | 0,12, 1,28 | 3,92, -7,53 | sim | None | — | 1,54×2,16 |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0/Hurtbox` | Box | Hurtbox | 0,63×1,86 | — | 0,12, 1,28 | -34,85, 8,33 | sim | None | — | 1,54×2,16 |
| Deserto_Hali | `Player_Damiao` | Box | Pegada | 0,57×0,29 | — | 0, 0 | -12, -13,12 | não | None | — | 0,95×2,2 |
| Deserto_Hali | `Player_Damiao/Hurtbox` | Capsule | Hurtbox | 0,67×1,81 | — | 0, 1,25 | -12, -12,12 | sim | None | — | 0,95×2,2 |
| Deserto_Hali | `Refugios/Refugio_Entrada` | Circle | Gatilho | 3,6×3,6 | 1,8 | 0, 0 | -24, -22 | sim | None | — | — |
| Deserto_Hali | `Refugios/Refugio_PortoesDasRuinas` | Circle | Gatilho | 3,6×3,6 | 1,8 | 0, 0 | -4, 26 | sim | None | — | — |
| Deserto_Hali | `Refugios/Refugio_SantuarioDeYhtill` | Circle | Gatilho | 3,6×3,6 | 1,8 | 0, 0 | -26, 18 | sim | None | — | — |
| Deserto_Hali | `Setores_Tempestade/Setor_DesertoCentral` | Box | Gatilho | 18×18 | — | 0, 0 | 0, 2 | sim | None | — | — |
| Deserto_Hali | `Setores_Tempestade/Setor_Entrada` | Box | Gatilho | 43×7,5 | — | 0, 0 | 0, -23,5 | sim | None | — | — |
| Deserto_Hali | `Setores_Tempestade/Setor_LesteTemploSerpente` | Box | Gatilho | 12,5×23,5 | — | 0, 0 | 30,5, 7,5 | sim | None | — | — |
| Deserto_Hali | `Setores_Tempestade/Setor_PortoesDasRuinas` | Box | Gatilho | 18×5,5 | — | 0, 0 | 0, 25,5 | sim | None | — | — |
| Deserto_Hali | `Setores_Tempestade/Setor_SantuarioDeYhtill` | Box | Gatilho | 12,5×9,5 | — | 0, 0 | -30,5, 21,5 | sim | None | — | — |
| Deserto_Hali | `Setores_Tempestade/Setor_TumbaDeAlhazred` | Box | Gatilho | 12,5×14 | — | 0, 0 | -30,5, -2 | sim | None | — | — |
| EspectroHali | `EspectroHali` | Box | Cenario | 0,6×0,3 | — | 0, 0 | 0, 0 | não | None | — | — |
| EsqueletoInvocado | `EsqueletoInvocado` | Box | Cenario | 0,6×0,3 | — | 0, 0 | 0, 0 | não | None | — | — |
| EsqueletoInvocado | `EsqueletoInvocado/Hurtbox` | Box | Hurtbox | 1,3×1,95 | — | 0, 1 | 0, 1 | sim | None | — | — |
| Necronomicon | `Necronomicon` | Box | Gatilho | 0,84×1,05 | — | 0, 0,25 | 0, 0,1 | sim | None | — | — |
| Patua_DasLuasGemeas | `Patua_DasLuasGemeas` | Circle | Gatilho | 1,2×1,2 | 0,6 | 0, 0 | 0, 0 | sim | None | — | — |
| Patua_Pickup | `Patua_Pickup` | Box | Gatilho | 1,4×1,4 | — | 0, 0,5 | 0, 0,5 | sim | None | — | — |
| PedraDePoder | `PedraDePoder` | Box | Cenario | 1×1,35 | — | 0, 0,75 | 0, 0,68 | não | None | — | — |
| PedraDePoder | `PedraDePoder/Hurtbox` | Box | Hurtbox | 0,65×1,16 | — | 0, 0,75 | 0, 0,68 | sim | None | — | — |
| Player_Damiao | `Player_Damiao` | Box | Cenario | 0,6×0,3 | — | 0, 0 | 0, 0 | não | None | — | — |
| Player_Damiao | `Player_Damiao/Hurtbox` | Capsule | Hurtbox | 0,7×1,9 | — | 0, 1,25 | 0, 1,05 | sim | None | — | — |
| Playtest_RuinasPalidas | `DesertFloorGrid/Colisao` | TilemapCollider2D | Cenario | 0×0 | — | 0, 0 | 0,54, 0,68 | não | Merge | — | — |
| Playtest_RuinasPalidas | `DesertFloorGrid/Colisao` | Composite | Cenario | 53,5×27,25 | — | 0, 0 | 0,54, 0,68 | não | None | — | — |
| Playtest_RuinasPalidas | `Fragmento_1` | Circle | Gatilho | 1,6×1,6 | 0,8 | 0, 0 | 12, 4 | sim | None | — | 0,2×0,2 |
| Playtest_RuinasPalidas | `Fragmento_2` | Circle | Gatilho | 1,6×1,6 | 0,8 | 0, 0 | 30, -12 | sim | None | — | 0,2×0,2 |
| Playtest_RuinasPalidas | `Inimigos_Playtest/Cultista` | Box | Pegada | 0,41×0,26 | — | 0, 0 | 7,32, 2,42 | não | None | — | 1,44×2,02 |
| Playtest_RuinasPalidas | `Inimigos_Playtest/Cultista` | Box | Pegada | 0,41×0,26 | — | 0, 0 | 11,4, 1,57 | não | None | — | 1,42×2,04 |
| Playtest_RuinasPalidas | `Inimigos_Playtest/Cultista/Hurtbox` | Box | Hurtbox | 1,04×1,73 | — | 0, 1,28 | 7,32, 3,39 | sim | None | — | 1,44×2,02 |
| Playtest_RuinasPalidas | `Inimigos_Playtest/Cultista/Hurtbox` | Box | Hurtbox | 1,02×1,75 | — | 0, 1,28 | 11,4, 2,55 | sim | None | — | 1,42×2,04 |
| Playtest_RuinasPalidas | `Patua_Pickup` | Box | Gatilho | 1,4×1,4 | — | 0, 0,5 | 0,3, -36,63 | sim | None | — | 0,5×0,5 |
| Playtest_RuinasPalidas | `Player_Damiao` | Box | Pegada | 0,57×0,29 | — | 0, 0 | 1,59, 1,27 | não | None | — | 0,95×2,2 |
| Playtest_RuinasPalidas | `Player_Damiao/Hurtbox` | Capsule | Hurtbox | 0,67×1,81 | — | 0, 1,25 | 1,59, 2,27 | sim | None | — | 0,95×2,2 |
| Playtest_RuinasPalidas | `Saida_TumbaAlhazred` | Box | Gatilho | 1,6×1,6 | — | 0, 0 | -3,39, -1,17 | sim | None | — | — |
| Playtest_RuinasPalidas | `Saida_TumbaAlhazred (1)` | Box | Gatilho | 1,6×1,6 | — | 0, 0 | 41,43, -17,6 | sim | None | — | — |
| Playtest_RuinasPalidas | `TumbaDeAbdul_Conteudo/Abdul_Alhazred` | Box | Cenario | 0,3×2,54 | — | 0,01, 0,47 | 37,05, -14,85 | não | None | — | 2,62×7,68 |
| Playtest_RuinasPalidas | `TumbaDeAbdul_Conteudo/Abdul_Alhazred/Hurtbox` | Box | Hurtbox | 0,56×2,52 | — | -0,02, 0,46 | 37,02, -14,88 | sim | None | — | 2,62×7,68 |
| Playtest_RuinasPalidas | `TumbaDeAbdul_Conteudo/Bau_DaTumba` | Box | Gatilho | 6,24×2,01 | — | 0, 0 | 4,9, 2,75 | sim | None | — | 0,62×0,2 |
| Playtest_RuinasPalidas | `TumbaDeAbdul_Conteudo/YugNeth` | Box | Pegada | 0,61×1,52 | — | 0,19, 0,69 | 44,23, -15,47 | não | None | — | 1,13×1,77 |
| Portoes_Das_Ruinas | `Player_Damiao` | Box | Pegada | 0,6×0,3 | — | 0, 0 | 0, -10 | não | None | — | 0,99×2,3 |
| Portoes_Das_Ruinas | `Player_Damiao/Hurtbox` | Capsule | Hurtbox | 0,7×1,9 | — | 0, 1,25 | 0, -8,95 | sim | None | — | 0,99×2,3 |
| Portoes_Das_Ruinas | `Portoes_Root/Byakhee` | Capsule | Pegada | 2,04×2,82 | — | 0, 2,56 | 0, 4,66 | não | None | — | 5,23×4,81 |
| Portoes_Das_Ruinas | `Portoes_Root/Byakhee` | Circle | Gatilho | 1,52×1,52 | 0,76 | -0,04, 0,97 | -0,05, 3,17 | sim | None | — | 5,23×4,81 |
| Portoes_Das_Ruinas | `Portoes_Root/Byakhee` | Box | Gatilho | 2,83×3,21 | — | -0,26, 1,92 | -0,27, 4,06 | sim | None | — | 5,23×4,81 |
| Portoes_Das_Ruinas | `Portoes_Root/Byakhee/Hurtbox` | Box | Hurtbox | 3,77×4,14 | — | 0, 2,56 | 0, 4,66 | sim | None | — | 5,23×4,81 |
| Portoes_Das_Ruinas | `Portoes_Root/Gatilho_DaArena` | Box | Gatilho | 38×1,5 | — | 0, 0 | 0, -7 | sim | None | — | — |
| Portoes_Das_Ruinas | `Portoes_Root/Os_Portoes` | Box | Cenario | 18×1 | — | 0, 0,5 | 0, 11,5 | não | None | — | — |
| Portoes_Das_Ruinas | `Portoes_Root/Passagem_ParaOCastelo` | Box | Gatilho | 16,81×1,74 | — | 0,01, -0,02 | -0,78, 11,81 | sim | None | — | 0,16×0,16 |
| Portoes_Das_Ruinas | `Portoes_Root/Refugio_DosPortoes` | Circle | Gatilho | 3,6×3,6 | 1,8 | 0, 0 | -4, 8 | sim | None | — | 0,16×0,16 |
| Portoes_Das_Ruinas | `Portoes_Root/Volta_AoDeserto` | Box | Gatilho | 10×2 | — | 0, 0 | 0, -13 | sim | None | — | — |
| Portoes_Das_Ruinas | `PortoesFloorGrid/Colisao` | TilemapCollider2D | Cenario | 0×0 | — | 0, 0 | 0, 0 | não | Merge | — | — |
| Portoes_Das_Ruinas | `PortoesFloorGrid/Colisao` | Composite | Cenario | 68×34 | — | 0, 0 | 0, 0 | não | None | — | — |
| ReiEmAmarelo | `ReiEmAmarelo` | Circle | Cenario | 1×1 | 0,5 | 0, 0,88 | 0, 0,88 | não | None | — | — |
| Santuario_Yhtill | `Bau_DeYhtill` | Box | Gatilho | 1×1 | — | 0, 0 | 2,32, 2,36 | sim | None | — | 1×1 |
| Santuario_Yhtill | `Cassilda` | Circle | Gatilho | 4,62×4,62 | 2,31 | 0, 0 | 0,12, 2,96 | sim | None | — | 1,39×3,07 |
| Santuario_Yhtill | `Player_Damiao` | Box | Pegada | 0,6×0,3 | — | 0, 0 | 0, -3,5 | não | None | — | 0,99×2,3 |
| Santuario_Yhtill | `Player_Damiao/Hurtbox` | Capsule | Hurtbox | 0,7×1,9 | — | 0, 1,25 | 0, -2,45 | sim | None | — | 0,99×2,3 |
| Santuario_Yhtill | `Refugio_Santuario` | Circle | Gatilho | 3,6×3,6 | 1,8 | 0, 0 | -4,5, -1 | sim | None | — | — |
| Santuario_Yhtill | `Saida_Santuario` | Box | Gatilho | 6,36×1,2 | — | 0,03, 0 | 0,03, -4,8 | sim | None | — | — |
| Santuario_Yhtill | `Santuario_Root/Parede_Leste` | Box | Cenario | 0,5×11 | — | 0, 0 | 8, 0 | não | None | — | — |
| Santuario_Yhtill | `Santuario_Root/Parede_Norte` | Box | Cenario | 16×0,5 | — | 0, 0 | 0, 5,5 | não | None | — | — |
| Santuario_Yhtill | `Santuario_Root/Parede_Oeste` | Box | Cenario | 0,5×11 | — | 0, 0 | -8, 0 | não | None | — | — |
| Santuario_Yhtill | `Santuario_Root/Parede_Sul` | Box | Cenario | 16×0,5 | — | 0, 0 | 0, -5,5 | não | None | — | — |
| Santuario_Yhtill | `SantuarioFloorGrid/Colisao` | TilemapCollider2D | Cenario | 0×0 | — | 0, 0 | 0, 0 | não | Merge | — | — |
| Santuario_Yhtill | `SantuarioFloorGrid/Colisao` | Composite | Cenario | 30×15 | — | 0, 0 | 0, 0 | não | None | — | — |
| YugNeth | `YugNeth` | Box | Cenario | 0,6×0,6 | — | 0, 0 | 0, 0 | não | None | — | — |

> **Polygon, Edge e Composite** aparecem com tamanho medido por `bounds`, que a doc da 6000.4 diz ficar **vazio com o colisor desligado ou o objeto inativo** — nesses casos a linha traz `0×0`, que aqui significa *não medido*, não *vazio*.
