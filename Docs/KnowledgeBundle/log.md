---
type: Changelog
title: Log de Atualizações do Knowledge Bundle
description: Histórico cronológico de mudanças na base de conhecimento
---

# Log de Atualizações

## 2026-07-28 — Fundação code-first do mapa do Deserto de Hali (v0)

Fundação em código do overworld da Fase 1. QA: compilação limpa, testes EditMode 8/8 verdes. (A Fatia 1 "Vitória→TransicaoDeFase" está registrada mais abaixo, por outra rodada.)

> **Nota de alinhamento:** este planner **v0** foi escrito antes de `systems/level_design_deserto_hali.md` (autorado em paralelo). Ainda **não** implementa a topologia de lá — 5 setores, Lago de Hali central impassável, ~22×16, posições de POI por bússola. Alinhamento a esse doc é a próxima fatia antes de gerar a cena definitiva.

### Core
- **`DesertOverworldTypes.cs` (novo):** `DesertOverworldConfig` (DTO), `DesertOverworldLayout`, `PointOfInterestSpec` + enum `PointOfInterestKind` (PlayerSpawn, EntradaTumbaAlhazred, EntradaTemploSerpente, SantuarioYhtill, PortoesDasRuinas). Reaproveita `WallSpec`/`FloorSpec` do blockout de salas.
- **`DesertOverworldPlanner.cs` (novo):** POCO irmão do `LevelBlockoutPlanner` — monta o overworld (chão + perímetro + pontos de interesse posicionados). Agnóstico de Unity/PPU.

### Runtime
- **`PortalDeCena.cs` (novo):** porta de trigger que carrega uma cena por nome via `SceneManager.LoadScene` — carregamento mínimo e pontual (não é a infra completa de multi-cena). Usada na entrada da Tumba para levar ao S-Path.

### Editor
- **`BuildDesertOverworld.cs` (novo):** menu `Tools/FavelaAmarela/Build Desert Overworld` — gera do zero `Assets/Scenes/Deserto_Hali.unity` jogável (câmera-seguidora, GameManager, prefab do Damião no spawn, chão-tilemap de areia, limites e marcadores dos pontos de interesse; a entrada da Tumba vira `PortalDeCena`→Playtest_RuinasPalidas). Registra as duas cenas em Build Settings.

### Testes
- **`DesertOverworldPlannerTests.cs` (novo):** 8 testes EditMode — estrutura (1 chão + 4 limites), 5 pontos de interesse sem categoria duplicada e dentro do chão, portais com/sem cena destino, robustez do perímetro.

## 2026-07-28 — Correções narrativas e estruturais do GDD (documentação)

### Decisões de design registradas

- **Origem canônica de Damião:** Damião é o personagem do curta-metragem *Favela Amarela* (Richard Abelha). No final do curta, ele morre na Terra. O jogo começa imediatamente após — ele acorda em Carcosa, cercado por Espectros de Hali, no Deserto das Cinzas. A sinopse anterior ("viaja até a favela para decifrar inscrições") foi removida — essa é a premissa do curta, não do jogo.
- **Universo multimídia:** O jogo faz parte de um universo multimídia em desenvolvimento concomitante: curta (existe), animação, HQ e longa-metragem. O jogo é a fatia dedicada ao encontro com o Rei em Amarelo. Fragmentos de lore na Tumba de Alhazred conectam diretamente com o trabalho audiovisual.
- **Progressão da Fase 1 corrigida:** Exploração livre do overworld como estrutura principal. Ordem dos eventos: (1) Tumba de Alhazred → Boss Abdul Alhazred → Drop Necronomicon; (2) Santuário de Yhtill → Quest Cassilda → Drop Patuá; (3) Templo da Serpente (opcional) → 2 bosses → Drop set lendário; (4) Portões das Ruínas → Miniboss Byakhee → Transição para Fase 2.
- **Templo da Serpente — 2 bosses:** A Dungeon 2 tem dois guardiões (nomes TBD), cada um dropando uma peça de um set de equipamento lendário. O set completo desbloqueia uma sidequest exclusiva dentro do Castelo de Carcosa (Fase 2 do demo, Fase 6 do jogo completo).
- **Patuá reposicionado:** O Patuá (Patuá das Luas Gêmeas) é a recompensa da conclusão do puzzle/quest narrativo da Rainha Cassilda no Santuário — não é um drop de combate. O `PatuaPickup.cs` da Zona 5 que destrava o Salto Dimensional é um item diferente (rename para "Fragmento de Hali do Salto" pendente na Fatia 3 do roadmap).
- **Cassilda adicionada como personagem principal:** Cassilda entra formalmente em §2.4 do GDD como NPC de quest. Abdul Alhazred também entra formalmente como personagem.
- **Castelo de Carcosa — escopo:** Demo/Vertical Slice: Castelo = Fase 2, chefe final = Rei em Amarelo. Jogo completo: Castelo = Fase 6 (última fase), mesmo confronto.

### Documentação atualizada

- `GDD_Mestre.md`: §1.1 (Elevator Pitch), §1.8 (Escopo/Universo multimídia), §2.1 (Sinopse — reescrita completa), §2.4 (Personagens — Damião, Vance, Rei em Amarelo, Cassilda, Alhazred), §2.7 (Progressão Narrativa — reescrita com estrutura correta).
- `lore/templo_da_serpente.md`: Atualizado de stub para design parcial com 2 bosses, set lendário e mecânica de descoberta por drop.
- `lore/reliquias_cosmicas.md`: Coroa agora é "Peça A do set lendário do Templo"; Patuá agora tem nota de reposicionamento (quest de Cassilda) e distinção do `PatuaPickup.cs` existente.

### Pendências restantes desta rodada

- Nomes dos 2 guardiões do Templo da Serpente (TBD). *(Resolvido: Naga e Avatar de Set)*
- Nome do segundo drop do set lendário (Peça B — além da Coroa). *(Resolvido: O Set de Set possui 4 peças - Elmo, Peitoral, Grevas, Arma)*
- Conteúdo da sidequest do Castelo que o set desbloqueia. *(Resolvido: Luta contra Avatar de Nyarlathotep)*
- Papel narrativo completo de Professor Alistair Vance (guia ou traidor). *(Resolvido: Removido, confusão com Sub Mask Net_Dead)*
- O que é o objeto narrativo da Garganta? *(Resolvido: 1ª página do diário de um nobre de Yhtill, quest de Cassilda)*
- Rename do `PatuaPickup.cs` para "Fragmento de Hali do Salto" (Fatia 3 do roadmap de código).

---

## 2026-07-28 — Level Design do Overworld: Deserto de Hali (documentação)

### Decisão de design

Criado o documento de Level Design do overworld da Fase 1 (`systems/level_design_deserto_hali.md`), definindo o conceito do espaço, mapa topológico, zonas de tempestade de areia, pontos de interesse, filosofia de inimigos errantes e referências visuais. Nenhum código alterado nesta rodada.

### Decisões de design registradas

- **Mapa topológico:** 5 setores geográficos separados pelo Lago de Hali (barreira natural impassável). Setor sul: Entrada + Tumba de Alhazred. Setor norte: Santuário de Yhtill + Portões das Ruínas. Setor leste (oculto): Templo da Serpente.
- **Tempestade relocada:** A tempestade de areia sai da Dungeon 1 (Tumba de Alhazred) e passa a pertencer ao Overworld do Deserto de Hali, com zonas de intensidade variável por setor. A Dungeon 1 ficará com StormIntensity = 0 permanente. Os triggers `Z1_Spawn`, `Z2_Rajadas`, `Z3Z4_Forte` em `Playtest_RuinasPalidas.unity` são candidatos a remoção em fatia futura de código.
- **Tempestade como stealth invertido:** No overworld, a tempestade forte abafa completamente os passos de Damião (stealth passivo) e cega os Byakhee — mas também drena RM na zona máxima (leste) e oculta o caminho do Templo da Serpente.
- **Inimigos no overworld:** Cada inimigo tem função de design específica (sem spawns genéricos). Cultistas errantes em pares no deserto central, Byakhee como sentinelas aéreos, Coisa do Cemitério como foreshadowing de terror, Sementes de Hastur na borda do Lago, Cultista do Templo como guia opcional. Zona de Entrada e Santuário ficam livres de inimigos (zonas de respiro).
- **3 formas de descoberta do Templo da Serpente:** Necronomicon reage ao ambiente, marco visual na tempestade, Cultista especial com mapa fragmentado. Quantas implementar: decisão pendente.
- **Dungeon 1 como atalho geográfico:** Damião entra na Tumba pelo sul e emerge por uma saída diferente mais ao norte — a dungeon encurta o caminho além de ser narrativa. Coordenada exata de emergência: TBD.

### Documentação atualizada

- `systems/level_design_deserto_hali.md`: criado (novo).
- `systems/environment.md`: adicionadas notas de decisão de relocação da tempestade para o overworld nos pontos §Tempestade de Areia e §Zoneamento.
- `lore/deserto_e_dungeons.md`: seção §1 expandida com Lago de Hali, variação por setor e link para o novo doc.

### Pendências sinalizadas (sem código)

- Sequência de abertura (como Damião chega à Garganta de Pedra Pálida) — GDD §2.7.
- Condicional do Santuário (Cassilda exige Necronomicon? Cria dependência implícita de D1).
- Ponto exato de emergência da Tumba de volta ao overworld.
- Quantidade de formas de descoberta do Templo da Serpente.
- A Coisa do Cemitério no overworld: antes ou depois da Dungeon 1?

---

## 2026-07-28 — Fatia 1 do roadmap: "Vitória" → "TransicaoDeFase" (código)

Primeira fatia de código da reestruturação. Remove o estado terminal de "Vitória" da FSM do game loop, substituindo-o por uma transição de fim de fase/dungeon (RPG multi-fase, não roguelike — decisão registrada em `escopo-6-fases-sem-vitoria`). Também travadas duas decisões de conteúdo do Vini: o pickup existente vira "Fragmento de Hali do Salto" (rename de código pendente da Fatia 3) e o miniboss da Zona 9 é o Abdul Alhazred (o "Vulto" genérico não será implementado).

### Core
- `GameLoopStateMachine.cs`: `GameState.Vitoria` → `GameState.TransicaoDeFase`; grafo de transições e XML docs atualizados. Gameplay→TransicaoDeFase e TransicaoDeFase→Menu continuam válidas (mesmas arestas do antigo Vitoria).

### Runtime
- `GameManager.cs`: `TriggerVitoria()` → `TriggerTransicaoDeFase()`; campo serializado `telaVitoria` → `telaTransicaoDeFase`; lógica de timescale/telas ajustada.
- `VitoriaTrigger.cs` removido; criado `TransicaoDeFaseTrigger.cs` (mesmo padrão de trigger, agora reaproveitável em qualquer ponto de saída, ex. Portões das Ruínas). Verificado antes: o trigger antigo não estava anexado a nenhum GameObject e `telaVitoria` era referência nula nas duas cenas — rename sem perda de dado.
- Cenas `Playtest_RuinasPalidas.unity` e `cena_1.unity`: campo serializado do GameManager atualizado (era `{fileID: 0}` nas duas).

### Testes
- `GameLoopStateMachineTests.cs`: +3 testes (Gameplay→TransicaoDeFase válida, TransicaoDeFase→Menu válida, TransicaoDeFase→Gameplay rejeitada).
- QA: Runtime e Tests.EditMode compilam sem erros/avisos; **suíte EditMode 174/174 verde**.

### Documentação
- `systems/game_loop.md`, `architecture/dependency_map.md`, `scripts/runtime/game_manager_cs.md`, `scripts/runtime/index.md` atualizados; `scripts/runtime/vitoria_trigger_cs.md` → `transicao_de_fase_trigger_cs.md`.
- `lore/abdul_alhazred.md` e `systems/abilities.md`: pendências de conteúdo Vulto/Alhazred e patuá marcadas como resolvidas.

## 2026-07-28 — Reestruturação Fase 1 (Deserto de Hali) — documentação

### Decisão de design
A área jogável já construída (`Assets/Scenes/Playtest_RuinasPalidas.unity`, Zonas 1-9) passa a ser a **Dungeon 1 (Tumba de Alhazred)** dentro de uma nova **Fase 1: "O Deserto de Hali"** — overworld aberto (32 PPU) com duas dungeons, o Santuário de Yhtill (quest de Cassilda) e os Portões das Ruínas (checkpoint de saída). O que era "Fase 3: Castelo de Carcosa" no GDD de expansão renumerou para "Fase 2". Nenhum código foi alterado nesta rodada — só reconciliação de documentação (OKF) antes de qualquer implementação, conforme CLAUDE.md §3.1.

### Lore (`Docs/KnowledgeBundle/lore/`)
- `reliquias_cosmicas.md`: "Portões de Carcosa" → "Portões das Ruínas"; Coroa renomeada de "Coroa da Serpente" (Valusia) para **"Coroa de Ossos do Rei em Amarelo"**, obtenção mantida (drop do Chefe Guardião do Templo da Serpente), efeito marcado como "a definir" (roteiro em fechamento, sem especular habilidades de item).
- `deserto_e_dungeons.md`: seção 3 antiga ("Dungeon 2: Portões de Carcosa", que colocava o Byakhee como guardião de dungeon) dividida em "Dungeon 2: O Templo da Serpente (Opcional)" + "Portões das Ruínas (Fim da Fase 1)"; nota de que a Tumba de Alhazred = área já implementada.
- Novo stub `templo_da_serpente.md`: rascunho da Dungeon 2, localização/inimigos/nome do Guardião ainda TBD.
- `abdul_alhazred.md`: nota de localização + pendência sinalizada (Zona9_TronoDoVulto/"Vulto" sem relação escrita com Alhazred ainda).
- `cassilda_e_byakhee.md`: mesma renomeação de "Portões de Carcosa".
- `index.md`: linkados os 4 arquivos de lore que existiam mas não estavam no índice, + o novo stub.

### GDD (`Docs/KnowledgeBundle/`)
- `gdd_expansao_deserto_demo.md` (v3.0→v4.0): "3 Fases"→"2 Fases"; Fase 2 antiga (Ruínas Pálidas) fundida no bullet da Tumba de Alhazred; Fase 3→Fase 2; tabela de relíquias com efeitos marcados "a definir".
- `GDD_Mestre.md`: §1.8 (Escopo), §2.7 (Progressão Narrativa), §4.2 (Fluxo de Telas/diagrama) e §5.1/§5.4 (Level Design) atualizados para refletir que Ruínas Pálidas não é mais "Fase 1" e sim a Dungeon 1; §5.2 e §5.4 marcam PPU 32 e lista de zonas como parcial (1-5; 6-9 existem no blockout, não documentadas ainda).

### Systems e Skills
- `systems/abilities.md`: nota de colisão de nome entre o patuá existente (`PatuaPickup.cs`, destrava Salto) e a nova relíquia "Patuá das Luas Gêmeas".
- `systems/level_design.md`: PPU 16→32; nota de que é a Dungeon 1 agora.
- `.claude/skills/favela-isometric-standards/SKILL.md` e espelho em `.agents/`: PPU padrão do projeto 16→32.

### Memória (Claude Code)
- `escopo-6-fases-sem-vitoria.md`: marcado como superado no escopo da demo; ambição de 6 fases mantida como horizonte separado, não reconciliado.
- Nova memória `fase1-deserto-hali-multi-cena-32ppu.md`: registra as decisões desta rodada e o roadmap de código futuro (fatia por fatia — infra multi-cena, `SaveData` estendido, pickup genérico de relíquia, repropriação da cena, overworld do deserto, Templo da Serpente, Santuário de Yhtill, Portões/Byakhee).
- Notas de esclarecimento em `roadmap-camera-combate-urp.md` e `ia-inimigos-percepcao-graduada-fase2.md` (usam "Fase 2" no sentido antigo).

## 2026-07-27 — Placeholders jogáveis integrados (Damião, inimigos, patuá e arma)

### Arte / Placeholders (favela-pixelart-standards: PPU 16, Point, None)
- **Damião:** sprite idle `Damiao_Robe_Idle` (32×48, derivado por downscale do `Damiao_Concept_Robe`, com preenchimento de buracos e key de fundo por saturação) atribuído ao `Player_Damiao`; `Transform.scale = 0.5` (tile de chão = 16px = 1 unidade → personagem ~1.5 tile, corrigindo o "gigante" de 3 tiles); virou `Player_Damiao.prefab` preservando a ref `SequenciaDeColapso.damiaoSprite`.
- **Cultista:** idle 16×32 extraído do `Cultista_Spritesheet_16x32` (limpeza: chroma-key do xadrez cinza opaco queimado + despeckle de respingos). `Cultista_Idle.png` importado (pivot pés); `Cultista.prefab` → sprite novo + `DynamicYSort` (fator 10) + scale 0.5. Verificado na cena (bounds 1×2, AI/waypoints intactos).
- **Espectro:** idle 24×48 do `EspectroHali_Spritesheet_24x48` (remoção da faixa de rótulo queimada no topo + despeckle). `EspectroHali_Idle.png` importado; `EspectroHali.prefab` → sprite + `DynamicYSort` + scale 0.5 (spawna via Cerco Z4, alpha controlado pelo `EspectroAI`).
- **Barra Enferrujada:** sprite placeholder 16×16 desenhado (`Barra_Enferrujada.png`) — não existia visual da arma.
- **Patuá:** substituído o placeholder quadrado-amarelo chapado por amuleto/trouxinha pixel art 16×16 (`Patua.png` sobrescrito mantendo o guid → propaga a todos os usos).

### Pickups (Runtime.GameLoop — prefabs autorados por YAML)
- `Patua_Pickup.prefab` e `Arma_Pickup.prefab` criados: `SpriteRenderer` + `BoxCollider2D` (trigger) + `PatuaPickup`/`ArmaPickup` + `DynamicYSort`, layer 0 (igual aos triggers que já funcionam), scale 0.5. Posicionados na Zona 5. Patuá destrava o Salto Dimensional; arma destrava a Mão Física. **Validados em Play.**

### Gotcha registrado
- Sprite **single-mode** referenciado por YAML resolve no `fileID: 21300000` — não no `internalID` do `.meta` (que pegou o Patuá até corrigir). Ver memória `single-sprite-fileid-21300000`.

### Pendências (próximos slices)
- Colliders dos inimigos ficaram pequenos (0.16 pós-scale 0.5) — tunar; normalizar tamanhos nativos (voltar tudo a scale 1.0 reautorando px); avaliar mover pickups pra layer `Pickup`; **reorg de hierarquia** (raiz plana → containers de transform identidade) como slice dedicado.

## 2026-07-27 — Spritesheets e Fatiamento via Aseprite MCP (16x32 e 24x48)

### Arte & Automação Aseprite (aseprite-bridge MCP)
- **Fatiamento Automático Lua:** Executado script via MCP `aseprite-bridge` (`execute_lua_script`) para processar e fatiar individualmente os spritesheets em arquivos `.aseprite` nativos e exportar mapas de sprites com JSON metadata.
- **Cultista Amarelo (16x32 px):** Gerado [Cultista_Sliced.aseprite](file:///c:/Users/Vini/Desktop/projeto_amarelo_unity/Assets/Sprites/Cultistas/Cultista_Sliced.aseprite), [Cultista_Sliced_Sheet.png](file:///c:/Users/Vini/Desktop/projeto_amarelo_unity/Assets/Sprites/Cultistas/Cultista_Sliced_Sheet.png) e [Cultista_Sliced_Sheet.json](file:///c:/Users/Vini/Desktop/projeto_amarelo_unity/Assets/Sprites/Cultistas/Cultista_Sliced_Sheet.json) com as animações de *Idle*, *Walk*, *Attack* e *Death* organizadas por Tags de 4 frames.
- **Espectro de Hali (24x48 px):** Gerado [EspectroHali_Sliced.aseprite](file:///c:/Users/Vini/Desktop/projeto_amarelo_unity/Assets/FavelaAmarela/Art/Enemies/EspectroHali_Sliced.aseprite), [EspectroHali_Sliced_Sheet.png](file:///c:/Users/Vini/Desktop/projeto_amarelo_unity/Assets/FavelaAmarela/Art/Enemies/EspectroHali_Sliced_Sheet.png) e [EspectroHali_Sliced_Sheet.json](file:///c:/Users/Vini/Desktop/projeto_amarelo_unity/Assets/FavelaAmarela/Art/Enemies/EspectroHali_Sliced_Sheet.json) com as animações de *Idle*, *Move*, *Attack* e *Death* organizadas por Tags de 4 frames.
- **Importação Unity (favela-pixelart-standards):** Metafiles configuradas com `PPU = 16`, `Filter Mode = Point` e `Compression = None`.

## 2026-07-17 — Profundidade isométrica (oclusão) + correções de gameplay

### Renderização isométrica (novo sistema)
- `DynamicYSort` (Runtime/Rendering) — atualiza `sortingOrder = -y*10` nos atores que se movem em `LateUpdate` (o pré-requisito que faltava; sem ele a profundidade não funcionava)
- Shader `FavelaAmarela/SpriteDitherOcclusion` (Built-in RP) — recorte dither Bayer 4x4 por `_DitherAmount`
- `OcclusaoDitherFade` (Runtime/Rendering) — nas paredes altas; detecta o jogador atrás (trigger + Y) e faz o fade do dither via `MaterialPropertyBlock`. Silhueta atrás de paredes altas provada em Play (grey-box)
- Documentado em `systems/renderizacao_isometrica.md` (novo) + `systems/index.md`

### Correções de gameplay
- **Invulnerabilidade de cutscene:** `GameManager.JogadorInvulneravel` — Coisa do Cemitério e `ColapsoTrigger` não matam Damião durante a queda Z4→Z5 (antes ela o insta-matava encurralado)
- **Tempestade na Z5:** `QuedaZ4Z5Trigger` zera a faixa explicitamente no teleporte (o teleporte adormece o rigidbody e o `Z5_Nula` não disparava); colliders `Z3Z4_Forte`/`Z5_Nula` realinhados na barreira (-30.25)
- **Cerco:** `CercoZ4Cutscene.LimparAtores()` destrói os inimigos instanciados após a queda (não persistem/perseguem na Z5)
- **Espectro:** `Rigidbody2D` Kinematic no `EspectroAI` — fantasma atravessa paredes (excludeLayers não bastava por causa do `ForceReceiveLayers` das paredes)
- Docs atualizados: `coisa_do_cemiterio.md`, `environment.md`, `espectro.md`, `queda_z4_z5.md`

## 2026-07-16 — Fase 1 Slice 4: esqueleto de Save
- Criado o POCO `SaveData` (`Core.Persistence`) — DTO `[Serializable]` de progresso (versão, resiliência, unlocks de Salto/Arma, posição); campos serializados camelCase
- Adicionado `ResilienciaMental.Restaurar(float)` (Core) — define o valor salvo (clampado, dispara `OnChanged`), reusando o `Alterar` privado; 6 testes novos
- Criado o adapter Runtime `SaveSystem` (`Runtime.GameLoop`) — IO JSON em `Application.persistentDataPath` (slot único `save.json`), defensivo, nunca `PlayerPrefs`
- Novo `SaveDataTests` (round-trip JSON via `JsonUtility`)
- Documentado em `systems/persistencia.md` (novo) + `systems/index.md`
- Fora de escopo (slice futuro): gatilho de save, orquestração no `GameManager`, múltiplos slots
- QA: testes EditMode rodados no Test Runner pelo Vini — todos verdes (152 anteriores + 8 novos)

## 2026-07-10 — Cena da Tempestade de Areia + Prefab da Coisa do Cemitério
- Criado o POCO `AgendadorDeRajada` (Core/Environment) — decide quando uma rajada forte acontece, RNG injetável (padrão de `BarraEnferrujada`); 6 testes NUnit
- Criado o adapter Runtime `TempestadeRajadaAleatoria` — variante do `TempestadeZonaTrigger` que tica o `AgendadorDeRajada` só enquanto o jogador está no trigger e alterna a faixa da tempestade entre calmaria e rajada
- Colocados 4 GameObjects de trigger na cena `Playtest_RuinasPalidas`: `TempestadeTrigger_Z1_Spawn` (moderada), `TempestadeTrigger_Z2_Rajadas` (calma + rajadas aleatórias), `TempestadeTrigger_Z3Z4_Forte` (forte, cobre as duas zonas contíguas), `TempestadeTrigger_Z5_Nula` (nula, subterrâneo)
- Criado o prefab `Assets/FavelaAmarela/Art/Enemies/CoisaDoCemiterio.prefab` (espelhando `Cultista.prefab`, com `Collider2D.isTrigger = true` — diferença crítica pro toque→Colapso) e posicionado na transição Zona2→Zona3
- Corrigida divergência no OKF: `coisa_do_cemiterio.md` ainda listava `CoisaDoCemiterioAI` como não implementada (já existia desde 2026-07-08)
- Adicionado o utilitário de Editor `WireStormTriggers.cs` (`Tools/FavelaAmarela/Wire Storm Triggers`) — mesmo padrão do `WireConfigAssets.cs`, contornando a limitação do MCP `update_component` pra referências de objeto de cena
- Recompile limpo + 152/152 testes EditMode verdes (baseline 146 + 6 novos)

## 2026-07-10 — Slice 3: assets de Config dos bridges do Player
- Criados os 4 assets `.asset` em `Assets/FavelaAmarela/Config/` a partir dos ScriptableObjects de config (`LocomocaoConfig`, `EsquivaConfig`, `SaltoDimensionalConfig`, `BarraEnferrujadaConfig`), com os valores default dos `[SerializeField]`
- Atribuídos aos bridges do `Player_Damiao` na cena `Playtest_RuinasPalidas` (`PlayerMovement.locomocaoConfig`, `EsquivaBridge.config`, `AnomalyPowerBridge.config`, `MaoFisicaBridge.config`)
- Adicionado o utilitário de Editor `WireConfigAssets.cs` (`Tools/FavelaAmarela/Wire Config Assets`) — atribui via `SerializedObject`, contornando a limitação do MCP `update_component` (não resolve referência de asset para campo tipado de `ScriptableObject`)
- Recompile limpo + 146/146 testes EditMode verdes

## 2026-07-08 — Tempestade de Areia + Coisa do Cemitério
- Estendido `EnvironmentState` com o evento `OnStormIntensityChanged` e o POCO `TempestadeOscilador` (oscilação senoidal entre intensidade mínima e máxima)
- Adicionados os adapters Runtime `TempestadeAmbiente`, `TempestadeZonaTrigger` e `TempestadeVisualOverlay` para sincronizar a tempestade com a cena e a UI
- Criado o novo sistema "Coisa do Cemitério" (bestiário #5): `CoisaDoCemiterioFSM`/`CoisaDoCemiterioState` (Core), estados `Farejando`/`AlvoPreciso`, insta-kill via `ResilienciaMental.ForcarColapso()`
- Documentado em `systems/environment.md` (seção Tempestade de Areia) e no novo `systems/coisa_do_cemiterio.md`

## 2026-07-07 — Criação Inicial
- Criado bundle OKF completo com 6 seções
- Documentados todos os 7 domínios Core: Combat, Enemies, Stealth, Abilities, GameLoop, Environment, AI
- Incluída camada de Architecture Decisions, Unity 6.4 Gotchas, Testes e Lore
- Integrado ao `CLAUDE.md` raiz via seção 3.1
