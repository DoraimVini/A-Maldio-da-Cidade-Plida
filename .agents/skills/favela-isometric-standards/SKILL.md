---
name: favela-isometric-standards
description: Enforces the strict 2D isometric grid rules for Favela Amarela rendering and physics. Use when creating or modifying level layout, prefabs, physics colliders or camera settings.
---

# Favela Amarela - Isometric Standards

## Core Mandates

1. **Gravity is Void**:
   - `Rigidbody2D.gravityScale` MUST ALWAYS be `0.0`. The isometric perspective handles depth artificially. If gravity is applied, the game's line of sight and movement systems will completely break.

1b. **Rotation is Void, too** (acrescentado 2026-08-21):
   - Todo `Rigidbody2D` **`Dynamic`** DEVE ter `constraints |= RigidbodyConstraints2D.FreezeRotation`.
   - **Por quê:** corpo dinâmico que leva impulso de colisão fora do centro ganha velocidade angular. Com `gravityScale 0` e `angularDamping` padrão (0,05), nada zera isso depressa — o `transform` roda e, como o `SpriteRenderer` está no mesmo `GameObject`, **o sprite gira**. Num isométrico cuja profundidade é fingida por `sortingOrder`, personagem rodando destrói a ilusão inteira; e gira o colisor junto, mudando a pegada a cada quadro.
   - **Isto já aconteceu:** em 2026-08-21 o Vini relatou "uma coisa muito estranha nos mobs e até no boss" e apontou a causa. Quatro corpos estavam sem a trava: `Byakhee.prefab`, `Cortesao_Palido_0`, `Cortesao_Palido_1` e o Damião da `cena_1`. **A regra não existia neste documento** — a lacuna estava no padrão, não só nos prefabs.
   - `Kinematic` e `Static` ficam de fora: não recebem impulso.
   - Guarda: `FisicaDosAtoresTests`. Correção: `Tools/FavelaAmarela/Física: padronizar os atores`.

1c. **Detecção de colisão contínua** (acrescentado 2026-08-21):
   - Ator que se move DEVE usar `CollisionDetectionMode2D.Continuous` (já exigido no `CLAUDE.md` §5, agora com guarda).
   - **Por quê:** `Discrete` deixa ator rápido atravessar parede fina entre dois `FixedUpdate`. É por isso que anel de colisão de mapa precisa ter **2 células de espessura** — uma só não segura o mergulho do Byakhee mesmo em `Continuous`.
   - No mesmo levantamento de 2026-08-21, **sete dos nove atores estavam em `Discrete`**, inclusive o Damião.

### Modelo de física do jogo (resumo, para não ser redescoberto)
- Corpos: `Rigidbody2D` **Dynamic**, `gravityScale 0`, `FreezeRotation`, `Continuous`.
- Movimento: `linearVelocity` atribuída em `FixedUpdate` (`PlayerMovement`) — **não** `MovePosition`, **não** `AddForce`. Unity 6 renomeou `velocity` → `linearVelocity`.
- A sensação isométrica vem do remapeamento de input (`PlayerMovement.ConvertToIsometric`), **não** de câmera inclinada nem de física 3D.
- Profundidade é `sortingOrder = -y*10` (`DynamicYSort`), **não** eixo Z nem física.
- Dano de inimigo é resolvido por `Vector2.Distance` + `IDanificavel`, **não** por sobreposição de colisor. O colisor do jogador governa só o que barra movimento.

2. **Camera Projection**:
   - Camera must be Orthographic.
   - Rotation MUST stay `Quaternion.identity` (no tilt on any axis). This project does NOT use a physically-tilted 3D dimetric camera — `LevelBlockoutGenerator` places walls/floors as flat `SpriteRenderer`/`BoxCollider2D` on the XY plane and fakes depth purely via Y-sorting (`sortingOrder = -worldCenter.y * 10`). The "isometric feel" comes from `PlayerMovement.ConvertToIsometric` remapping input direction, not from camera rotation. Tilting the camera (e.g. 26.57° on X, the classic 3D-dimetric-diorama trick) breaks the Y-sort depth illusion and visually desyncs colliders from sprites — do not reintroduce it. (`PrefabMigrationTool.cs`'s "cenário A" / `Quaternion.identity` is the correct reference implementation; `PlaytestSceneSetup.cs` previously diverged from it and was fixed to match.)

3. **Tilemap & Cell Size**:
   - Level geometry is generated procedurally as plain XY `Vector2` positions (`LevelBlockoutPlanner` + `LevelBlockoutGenerator`), NOT tiles.

   - **Colisão de tilemap (2026-08-27):** todo `TilemapCollider2D` de parede fica na camada `Obstacle`, com `compositeOperation = Merge`, um `CompositeCollider2D` em `Outlines` e um `Rigidbody2D` **Static** no mesmo objeto. O `Rigidbody2D` que a Unity cria junto do Composite nasce **Dynamic com `gravityScale 1`** — parede Dynamic é parede que o Damião empurra. Ferramenta: `Tools/FavelaAmarela/Física: consolidar a colisão dos tilemaps`; guarda: `ColisaoDeCenarioTests`.

   - **Câmera (2026-08-27):** ortográfica, sem rotação, `z = -10`, com `PixelPerfectCamera` (PPU 32, referência 480×270 no padrão e 640×360 nas arenas, Pixel Snapping ligado, Upscale e Crop desligados). O `orthographicSize` **não** se escreve à mão: sai de `EscalaDePixel.TamanhoOrtografico` (4× → 4,21875; 3× → 5,625). Ferramenta: `Tools/FavelaAmarela/Padronizar Canvas e moldura do menu`; guarda: `CameraPixelPerfectTests`. **Planned (decisão 2026-07-17):** a Tilemap será usada SÓ para o CHÃO do deserto (pintar rápido), com `cellSize (1.0, 0.5, 1.0)`. Paredes/casas altas continuam **sprites Y-sorted individuais**, nunca tiles — um Tilemap não consegue Y-sortar telhados altos contra um ator que se move (z-fighting).

4. **Sprite Settings (Pixels Per Unit)**:
   - All Sprites must have a consistent Pixels Per Unit (PPU). The standard for this project is `32` (2026-07-28: promoted from 16 to 32 as the project's single standard, matching `gdd_expansao_deserto_demo.md` §4 and the new Deserto de Hali assets). Legacy art still imported at 16 PPU is re-imported at 32 when it is next touched, not migrated in bulk.
   - The Tilemap `cellSize (1.0, 0.5, 1.0)` from rule 3 is expressed in world units, not pixels — it does not change with this PPU update.

5. **Sorting (Y-Sorting)**:
   - Depth vem de `sortingOrder` explícito por sprite: `-y * 10`. Geometria estática recebe o valor uma vez (`LevelBlockoutGenerator`); **atores que se movem (Damião, inimigos) DEVEM ter o componente `DynamicYSort`** (`Runtime.Rendering`), que atualiza `sortingOrder = -y*10` em `LateUpdate` com o mesmo fator. Sem ele, atores móveis não são ocultados corretamente por paredes à frente.
   - **Transparency Sort Mode: `Custom Axis (0,1,0)`, e já está assim** — `GraphicsSettings.m_TransparencySortMode: 3` com eixo `(0,1,0)`, desde o commit `92410413`. Nenhuma câmera sobrescreve. Guardado por `OrdenacaoIsometricaTests`.
     - **Correção de 2026-08-27:** esta linha dizia *"o projeto hoje está em `Default`"* e estava errada — `Default` era o check-in inicial. A afirmação obsoleta virou premissa de uma fase inteira de plano de revisão de física ("decidir se troca para Custom Axis, medindo antes numa cena"), para uma troca que já tinha acontecido.
     - **Os dois mecanismos NÃO competem**, ao contrário do que aquele plano supunha. A doc de `Camera.transparencySortAxis`: *"This is used for sorting Renderer components when other, higher priority, criterias fail to distinguish the render order."* `sortingLayer` e `sortingOrder` mandam; o eixo é o **desempate** — e ele é necessário porque `sortingOrder` é `int`: `round(-y*10)` tem resolução de 0,1 unidade (3,2 px a PPU 32), então dois atores a menos de 3 px de distância vertical empatam. Sem o eixo, o desempate de câmera ortográfica é a distância em z, que é ~0 para todo sprite — e a ordem entre eles alterna de quadro em quadro.

6. **Oclusão (silhueta atrás de paredes altas)**:
   - Parede/casa alta que o Y-sort desenha por cima do jogador usa o shader `FavelaAmarela/SpriteDitherOcclusion` + o componente `OcclusaoDitherFade`: quando o jogador passa atrás (`jogador.y > parede.y`), a parede abre buracos dither e a silhueta aparece. Não usar alpha liso (não-pixel-art) nem SphereCast de câmera (ideia 3D). Ver `Docs/KnowledgeBundle/systems/renderizacao_isometrica.md`.

## Audit Enforcement
Any time you modify a Room prefab, the Camera setup, or Enemy/Player rigidbodies, YOU MUST verify these constraints. If a Rigidbody2D has a gravity scale other than `0.0`, you must immediately fix it.
