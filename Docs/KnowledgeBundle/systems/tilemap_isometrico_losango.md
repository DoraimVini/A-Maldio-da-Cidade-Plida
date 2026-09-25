---
type: Game System
title: Chão em Tilemap Isométrico de Losango 2:1
description: A receita real (confirmada na cena, não no código de uma ferramenta desatualizada) para construir o chão de uma área em Tilemap isométrico — Grid, colisão de borda e a matemática do tamanho do losango. Usada no Deserto, na Tumba e no Santuário.
tags: [tilemap, isometrico, grid, chao, colisao, level-design]
---

# Chão em Tilemap Isométrico de Losango 2:1

Como desenhar o chão de uma cena em Tilemap isométrico verdadeiro (visual em losango,
não um retângulo topdown) — a receita que o Deserto de Hali, a Tumba de Alhazred e o
Santuário de Yhtill usam hoje. Escrito para ser replicável em qualquer ferramenta que
edite a cena da Unity (Claude Code, Antigravity, ou na mão pelo Editor).

> **Por que este documento existe:** em 2026-08-02 o piso do Santuário era um
> `SpriteRenderer` retangular liso — sem textura, sem pista visual de ângulo nenhuma —
> e por isso "parecia topdown" mesmo a câmera estando correta. A causa não era bug de
> câmera nem de grid do Editor: era o chão nunca ter sido construído como Tilemap
> isométrico, só como um retângulo de cor. Ver `log.md`, entrada "Templo não está com
> grid certo" (o "Templo" ali era engano de nome — o problema era o Santuário).

## 1. O essencial: só o CHÃO vira Tilemap

**Paredes e casas altas NUNCA viram tile — continuam sprites individuais com Y-sort.**
Um Tilemap não consegue Y-sortar uma parede alta contra um ator que passa na frente dela
(teria que fatiar a parede em pedaços por linha, o que não existe hoje); tiles são bons
para chão plano, ruins para qualquer coisa com altura que precise ocluir/ser ocluída
dinamicamente. Ver `favela-isometric-standards` §3 e §6 (oclusão dither).

Isso significa: ao converter uma sala para Tilemap isométrico, só o piso muda. Paredes
seguem como estão — objetos com `BoxCollider2D` (barreira física) e, se tiverem arte,
`SpriteRenderer` com `sortingOrder` calculado por Y como qualquer outra geometria estática.

## 2. O Grid

Um `GameObject` com componente `Grid`:

```
cellSize:   (1, 0.5, 1)
cellLayout: Isometric      // enum GridLayout.CellLayout.Isometric = valor 2
cellSwizzle: XYZ           // padrão
```

`cellSize.y = cellSize.x / 2` é o que dá a proporção 2:1 do losango (duas vezes mais
largo que alto) — é essa proporção que faz a projeção parecer isométrica em vez de
topdown. Mudar essa razão muda o "ângulo" aparente da vista.

**Achado em 2026-08-02:** existe uma ferramenta antiga no projeto
(`BuildDesertTilemap.cs`) cujo código cria um Grid **retangular** (`cellSize (1,1,0)`).
O Grid que está de fato salvo nas cenas do Deserto e da Tumba é o isométrico acima —
alguém corrigiu na cena depois, e o código da ferramenta nunca foi atualizado para
bater. **A cena é a fonte da verdade, não esse arquivo.** Se for reescrever essa
ferramenta, corrija-a para casar com o Grid real.

## 3. O chão (Tilemap de visual)

Filho do Grid: `GameObject` com `Tilemap` + `TilemapRenderer`.

```
TilemapRenderer.sortingOrder = -1000   // sempre atrás de todo o resto da cena
```

Pinta tiles com `tilemap.SetTile(new Vector3Int(x, y, 0), tile)`, varrendo um
retângulo em **espaço de grid** (coordenadas de célula, não de mundo):

```csharp
for (int gx = -M; gx < M; gx++)
    for (int gy = -M; gy < M; gy++)
        tilemap.SetTile(new Vector3Int(gx, gy, 0), tile);
```

Um bloco **quadrado em espaço de grid** (mesmo N de células em X e Y) sai como um
**losango em espaço de mundo** — é a transformação isométrica fazendo o trabalho. Não
tente desenhar um "retângulo" pintando um losango maior; um bloco quadrado em grid já
produz o losango certo sozinho.

### A matemática do tamanho (onde é fácil errar)

Com `cellSize (1, 0.5)` e um bloco de **N células por eixo** (grid de `-M` a `M-1`,
`N = 2M`):

| Em mundo | Fórmula | Com N=28 (M=14) |
|---|---|---|
| Largura total do losango | `N × cellSize.x` = `N` | 28 |
| Altura total do losango | `N × cellSize.y` = `N / 2` | 14 |
| Meia-largura (do centro até a ponta leste/oeste) | `N / 2` | 14 |
| Meia-altura (do centro até a ponta norte/sul) | `N / 4` | 7 |

**Erro cometido na primeira tentativa do Santuário:** usei M=7 (N=14) achando que dava
meia-altura 7 — na verdade dava meia-altura **3,5** (N/4 = 14/4). O marco da saída da
cena estava em y=-4,8, fora do losango inteiro (a ponta é um único ponto, sem área — o
jogador ficaria preso fora do chão). Corrigido para M=14 (N=28, meia-altura 7), com
margem sobre o marco mais distante.

**Regra prática:** antes de pintar, liste todo marco/trigger/NPC já fixado na cena e
pegue o maior `|y|` entre eles. A meia-altura do losango (`N/4`) precisa superar esse
valor com folga — perto da ponta, a largura disponível encolhe para quase zero.

## 4. A colisão (Tilemap de borda, não a forma inteira)

**Não** colide o chão inteiro — só a **borda**. Um segundo `Tilemap` (nome `Colisao`,
sem `TilemapRenderer` — invisível de propósito) com `TilemapCollider2D`, na layer
`Obstacle`.

Algoritmo (o mesmo em `BuildIsoCollisionFromFloor.cs` e `BuildSantuarioIsoFloor.cs`):

1. Colete todas as células que o chão tem pintadas (`floorTilemap.HasTile(cell)`).
2. Para cada célula de chão, olhe as 8 vizinhas (`dx,dy` de -1 a 1, exceto 0,0). Toda
   vizinha que **não** é chão vira uma célula de "parede".
3. Pinte um tile de colisão invisível (`Tile.ColliderType.Grid` — o colisor segue a
   forma de losango da própria célula, sem precisar de sprite) em cada célula de
   parede, no tilemap `Colisao`.

Isso traça o perímetro externo do losango **e** qualquer buraco interno, automaticamente,
a partir do desenho do chão — não precisa desenhar parede nenhuma à mão. O tile de
colisão é um asset reutilizável do projeto inteiro:
`Assets/FavelaAmarela/Art/Tiles/colisao_invisivel.asset` — não recrie, reaproveite.

Sem `Rigidbody2D`: colisão estática não precisa. (Um `CompositeCollider2D` mesclaria as
bordas numa única forma, mas exige `Rigidbody2D` e não está em uso ainda — fica como
possível polimento futuro, não bloqueia nada hoje.)

## 5. Arte placeholder (quando ainda não existe tile de verdade)

Sem sprite de piso desenhado, gere um losango de cor sólida por código — mesma
proporção 2:1 da célula (`32×16px` a 32 PPU): dentro de um retângulo `w×h`, um pixel
`(x,y)` pertence ao losango se

```
|x - w/2| / (w/2) + |y - h/2| / (h/2) <= 1
```

(equação padrão de losango/diamante), preenchido com a cor da paleta da área e alpha 0
fora dele. Configure o import como qualquer sprite pixel art do projeto: PPU 32, Point,
sem compressão, `alphaIsTransparency: true` (ver skill `favela-pixelart-standards`).

## 6. Checklist para uma sala nova

1. Confirme que **não** existe Tilemap ainda para essa cena (`Grid` na hierarquia).
2. Crie o `Grid` isométrico (cellSize 1×0.5, layout Isometric).
3. Liste os marcos/triggers/NPCs já fixados na cena e ache o maior `|y|` entre eles.
4. Escolha N (células por eixo) tal que `N/4` supere esse `|y|` com folga.
5. Pinte o chão (bloco quadrado `N×N` em espaço de grid).
6. Gere a colisão de borda a partir do chão pintado.
7. **Desative** (não delete) qualquer piso/parede placeholder antigo — reversível.
8. Salve a cena e rode os testes EditMode (nenhum deles deveria mudar — isto é
   Runtime/Editor puro, sem POCO novo).

## 7. Nunca confundir com o blockout antigo (aposentado)

Existe uma geração de nível **completamente diferente e mais antiga** —
`LevelBlockoutPlanner` + `LevelBlockoutGenerator`, que produz posições `Vector2` cruas
em espaço topdown (paredes/chãos como `SpriteRenderer`/`BoxCollider2D` retos, sem
Tilemap nenhum). **Nunca rode "Regenerate Blockout"** — essa ferramenta reconstrói a
geometria antiga, incompatível em espaço de coordenadas com o chão isométrico atual, e
o jogador fica barrado nos lugares errados (colisão de um espaço, chão visual de
outro). Sala nova = pintar tile seguindo esta receita, nunca mexer no planner.

## Onde isso já foi aplicado

| Cena | Ferramenta | Nome do Grid |
|---|---|---|
| `Deserto_Hali` | `BuildDesertTilemap` (código desatualizado — cena é a verdade) + `BuildIsoCollisionFromFloor` | `DesertFloorGrid` |
| `Playtest_RuinasPalidas` (Tumba) | mesmas ferramentas, reexecutadas nessa cena | `DesertFloorGrid` (nome reaproveitado, não específico do deserto) |
| `Santuario_Yhtill` | `BuildSantuarioIsoFloor` (2026-08-02) | `SantuarioFloorGrid` |
