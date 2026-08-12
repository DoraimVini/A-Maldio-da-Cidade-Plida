---
type: Roadmap
title: Roadmap do Vertical Slice — Estado Real vs. Lista de Produção
description: Auditoria do que está pronto, parcial e não-começado, contra a lista priorizada do edital. Inclui riscos de escopo.
tags: [roadmap, escopo, edital, vertical-slice]
timestamp: 2026-07-31T00:00:00Z
---

# Roadmap do Vertical Slice — Estado Real

Auditoria feita em **2026-07-31** contra a lista priorizada de produção (12 itens + 2 de
polimento). Cada item foi verificado **no código e nas cenas**, não por memória.

> 🔄 **Re-auditado em 2026-08-11.** Várias linhas abaixo tinham envelhecido. As correções
> estão marcadas inline. **A mudança mais importante é a seção nova
> [Buracos sistêmicos](#buracos-sistêmicos-fora-dos-14-itens)**: quatro sistemas que não
> aparecem em nenhum dos 14 itens da lista, mas sem os quais o VS não se sustenta como demo —
> e um deles (áudio) torna o pilar central do jogo imperceptível para quem joga.

---

## Buracos sistêmicos (fora dos 14 itens)

Verificado no código em 2026-08-11. Nada disto está na lista do edital, e todos são baratos
perto dos bosses:

| Buraco | Evidência | Por que importa |
|---|---|---|
| **Áudio: ausente** | Zero `AudioSource`/`AudioClip` no gameplay (só em `AberturaDesertoCinematica`). Zero arquivos de som no projeto. | O pilar do jogo é **furtividade sonora** — o Cultista caça por som, a tempestade abafa o ruído, a Esquiva faz barulho de propósito. **Hoje se joga um stealth sonoro sem ouvir nada:** o jogador não percebe que fez barulho nem que foi ouvido. |
| **Persistência incompleta** | `InventoryManager.GetSaveData()` e `ProgressionManager.GetSaveData()` **nunca são chamados** — nada os liga ao `GerenciadorDeSave`. | Mochila, equipamento, nível de Exposição, Ecos e slots de Artefato **se perdem** ao recarregar. O `RefugioDeLuz` grava em disco, mas grava um save incompleto. |
| **Fluxo de jogo** | Zero arquivos para menu, pause ou tela de morte. | Não dá para começar, pausar nem perder. Hoje só se entra pela cena aberta no Editor, e o Colapso não tem desfecho de tela. |
| **Animação** | Nenhum `Animator` no gameplay (só cinemática e `ResilienciaBar`). | Tudo estático. O Abdul tem spritesheet fatiado em 28 frames usando **um frame só**. |

> ✅ **Escopo decidido em 2026-07-31:** o Vertical Slice são os **14 itens desta lista** —
> a **Fase 1 completa** e a **última fase do jogo** (Castelo de Carcosa + Rei em Amarelo),
> não só a Tumba. É um recorte **início + desfecho**, pulando as 4 fases do meio: mostra a
> abertura do jogo e o confronto final, que é o que faz um edital enxergar o jogo inteiro.
> O `CLAUDE.md` §1.1 foi reescrito para refletir isso.

---

## Prioridade 1 — Core de Sobrevivência

| # | Item | Estado | Observação |
|---|---|---|---|
| 1 | **Status Ailments** | ✅ **Pronto** | Sangramento por acúmulo (10 → estouro percentual) e Congelamento (3 acúmulos → trava o jogador). Ver [armas_da_tumba.md](systems/armas_da_tumba.md). |
| 2 | **Sistema de Consumíveis** | ⚠️ **Infra pronta, zero itens** (re-auditado 2026-08-11) | O inventário amadureceu muito desde 08/01: `InventoryManager` + `ItemDatabase` + `ItemDef`, `EquipmentInventory` com 6 slots, `BarraDeItens` na HUD (teclas 1–8) e `ConsumirItem` funcionando. **O que falta é só conteúdo: `grep "Tipo: 3"` nos `ItemDef` devolve zero — nenhum consumível existe.** É o item mais barato de fechar da lista. Ver [inventario_e_consumiveis.md](systems/inventario_e_consumiveis.md). |
| 3 | **Companheiro (RC)** | ⚠️ **Parcial** | Seguir Damião ✅; incapacitação + reanimação num Refúgio ✅ (implementado 2026-07-31). **Falta:** barra no HUD. Ver nota abaixo. |

### ⚠️ Conflito no item 1 — *Lentidão* vs. *Congelamento*
A lista pede **"Lentidão (Congelamento do Boss)"**. O implementado é **trava total**
(`PlayerState.Congelado`, ~1,5 s), não redução de velocidade. Foi assim que o design do
Abdul foi escrito ("3 stacks = Stun"). **Se a intenção for lentidão, é mudança de design a
confirmar** — não é bug.

### Nota sobre o item 3 — atualizado em 2026-07-31
1. **Barra "RC":** mantido como `Vitalidade` comum (decisão de 2026-07-30). **Falta só a
   barra no HUD**, não o recurso.
2. **Morte do companheiro:** revisado em 2026-07-31 — deixou de ser fim de run permanente
   (estilo Ashley/RE4) e virou **incapacitação recuperável**: ele cai no lugar, bloqueia os
   Portões de Carcosa, e é reanimado num `RefugioDeLuz` (novo, versão mínima). Ver
   [companheiro_mi_go.md](systems/companheiro_mi_go.md).
3. **"Escudo humano contra projéteis" durante a luta do Abdul continua descartado** — ele
   não é alvo de nada lá (ainda sob controle de Abdul). Se a intenção for proteção durante
   *alguma* luta, o candidato natural é o **Byakhee** (onde ele já estará livre), não a Tumba.

---

## Prioridade 2 — O Deserto de Hali

| # | Item | Estado | Observação |
|---|---|---|---|
| 4 | **Blockout geográfico** | ✅ **Setores definidos** (2026-08-01) | Os marcos já estavam nas posições certas da topologia (Tumba a oeste, Santuário a noroeste, Templo a leste, Portões ao norte, chegada ao sul). O que faltava eram os **setores como entidades de jogo**: 6 volumes de `TempestadeZonaTrigger` ladrilhando o mapa sem sobrepor, com as faixas de intensidade da tabela §3 do design. Ferramenta: `Tools/FavelaAmarela/Montar setores de tempestade do Deserto`. **Terreno não foi regerado** — só acrescentei volumes. |
| 5 | **Tempestade de Memória** | ✅ **Funcional no Deserto** (2026-08-01) | Driver + véu visual instalados na cena; o `GameManager` os liga no bootstrap. **Intensidade→detecção já funcionava**: a percepção do Cultista é 100% sonora e a tempestade abafa o ruído do Damião (`PlayerStealthState.AplicarAbafamentoTempestade`) — stealth invertido conforme decidido. **Intensidade→velocidade foi descartada** (decisão do Vini: a tempestade atrapalha só os inimigos). Falta variar a faixa por setor (`TempestadeZonaTrigger`), o que depende do item 4. |
| 6 | **População de inimigos** | ⚠️ **Ferramenta pronta, falta rodar** (2026-08-11) | O Deserto estava com **zero** inimigos (contagem por GUID do prefab). `PovoarODeserto` espalha Cultistas por setor com **densidade proporcional à tempestade** — ela abafa o ruído do Damião, então setor de tempestade forte aguenta mais companhia sem ficar injusto. Setor de chegada fica vazio de propósito. Cada instância ganha chave de persistência própria, senão o abatido ressuscita ao recarregar. **Falta rodar o menu no Editor.** |
| 7 | **A Coisa do Cemitério** | ⚠️ **Ferramenta pronta, falta rodar** (2026-08-11) | `CoisaDoCemiterioAI` + FSM implementados (caça por faro, insta-kill) e o prefab existe, mas havia **0 instâncias em qualquer cena**. A mesma ferramenta coloca **uma só**, no Deserto Central: ela caça por faro, então a tempestade que protege contra Cultistas **não serve de nada** contra ela — duas fariam do Deserto um corredor de morte. **Falta rodar o menu no Editor.** |

---

## Prioridade 3 — Encerramento da Fase 1

| # | Item | Estado |
|---|---|---|
| 8 | **Quest do Santuário de Yhtill** (Cassilda + fragmentos) | ✅ **Jogável de ponta a ponta** (2026-08-02) — `CancaoIncompleta` + `RecitalDaCancao` (Core, 13 + 9 testes), `CassildaNPC` e `FragmentoDeYhtill` implementados; Cassilda agora é prefab (`Cassilda.prefab`) com todo o conteúdo textual, instanciada e ligada em cena; os 3 fragmentos carregam as 2 primeiras estrofes da Canção de Cassilda; entregar tudo abre um **recital sem punição** das 2 estrofes finais antes do Patuá; o primeiro encontro ganhou a ramificação A/B/C do roteiro do lore (cosmética, via `PainelDeEscolha`). Progresso atravessa as duas cenas via save; o recital e a escolha do primeiro encontro não são persistidos (decisão). **Falta:** só arte — sprites placeholder em Cassilda, fragmentos e piso. Ver [quest_cassilda.md](systems/quest_cassilda.md). |
| 9 | **Boss Byakhee** | ⚠️ **Core, Runtime e prefab prontos** (2026-08-12) — `ByakheeFSM` (3 fases, 10 testes), `ByakheeAI`, `Ficha_Byakhee` e `Drop_Byakhee` com o Anel do Sinal Amarelo garantido. `Byakhee.prefab` com spritesheet animado real (26 frames, 6 animações nomeadas), ainda sem `Animator` (só o frame de idle é usado). **Falta:** ligar o `Animator`, a arena dos Portões em cena e a cena de abertura. Ver [systems/boss_byakhee.md](systems/boss_byakhee.md). |
| 10 | **Transição de Fase** | ⚠️ **Parcial** — `PortalDeCena` e `TransicaoDeFaseTrigger` existem; os Portões em si não. |

---

## Prioridade 4 — O Castelo de Carcosa (**última fase do jogo**)

> O jogo completo tem **6 fases**; o Castelo é a última. Ele não vem "logo depois" da Fase 1
> na campanha final — o VS salta as 4 fases do meio de propósito, para mostrar abertura e
> desfecho. Isso é escopo, não continuidade narrativa: a transição Fase 1 → Castelo dentro
> do VS é um **corte de apresentação**, não o fluxo real do jogo.

| # | Item | Estado |
|---|---|---|
| 11 | **Blockout do Castelo** | ❌ **Não começado** — cena inexistente. |
| 12 | **Boss Rei em Amarelo** | ⚠️ **Core, Runtime e prefab prontos** (2026-08-12) — `ReiEmAmareloFSM` (ritual de relíquias + selamento em ciclos, 13 testes), `DetectorDeCostas` (geometria da Máscara Pálida, 7 testes), `ReiEmAmareloAI` e `PontoFocalDeReliquia`. `ReiEmAmarelo.prefab` com sprite emprestado (recorte do spritesheet "Necromancer" da Inbox — arquétipo certo, cores erradas). A Coroa de Ossos ainda não tem fonte jogável — contornado com o `CarcosaDebuggerWindow` (concede/invoca sob demanda, agora instanciando os prefabs reais dos dois chefes) e a `Cena_ArenaDeTestes` (cena de dev, fora do Build Settings). **Falta:** arte final (cores, Máscara Pálida), o Trono de Aldebaran em cena de verdade, e uma fonte jogável para a Coroa de Ossos. Ver [systems/boss_rei_em_amarelo.md](systems/boss_rei_em_amarelo.md). |

---

## Prioridade 5 — Polimento

| # | Item | Estado |
|---|---|---|
| 13 | **Cinemática de abertura** | ❌ Não implementada (design existe em `systems/cinematica_abertura_deserto.md`). |
| 14 | **Dungeon 2 (Templo da Serpente)** | ❌ Não começada (design completo em `lore/templo_da_serpente.md`). |

---

## O que está pronto e **não aparece na lista**

A **Tumba de Alhazred (Dungeon 1)** — o trabalho das últimas sessões — não é citada em
nenhum dos 14 itens, mas está essencialmente **jogável de ponta a ponta**:

- Combate completo: ficha de 5 atributos, mitigação por defesa, 3 armas com habilidade própria, golpe desarmado.
- HUD: Resiliência, Vitalidade, Barra de Ações, prompt de interação, painel de escolha.
- Interação por botão **E** (baú, patuá, NPC).
- Baú com sorteio RNG das 3 armas.
- **Boss Abdul completo**: 2 fases, Escudo Mágico, Pedras de Poder que nascem por fase, Cones de Gelo, esqueletos invocados, janela de exaustão, drop do Necronomicon.
- Conversa ramificada (lutar × concordar) + traição da trégua.
- Yug-Neth cativo → libertado → segue Damião; se cair, incapacita e é reanimado num Refúgio.

**Pendência real da Tumba:** **arte**. Verificado — Pedra de Poder, Esqueleto, Cone de Gelo
e Necronomicon usam o **sprite built-in do Unity** (retângulos coloridos). O Abdul tem
spritesheet real fatiado em 28 frames, mas **sem Animator** (usa um frame estático).

### Acrescentado depois desta auditoria (2026-08-10 / 08-11)

Nada disto está nos 14 itens, mas passou a existir:

- **Motor de loot** (`Core.Loot`): `SorteioDeDrop` testável, `TabelaDeDrop` por arquétipo,
  `DropAoAbater`, e tiers liberados por `ProgressionManager.NivelAtual`. O `BauDaTumba` migrou
  para tabela. Ver [systems/loot_e_drop.md](systems/loot_e_drop.md).
- **Artefatos** (`Core.Artefatos`): inventário de 4 slots, passiva + habilidade por Artefato,
  barra F1–F4, e os 4 autorados (Necronomicon, Patuá, Anel do Sinal Amarelo, Coroa de Ossos).
  Ver [systems/artefatos.md](systems/artefatos.md). **Falta wiring de cena.**
- **Catálogo de armaduras**: 3 peças Inerte + **3 peças do Set Lendário** (Elmo, Peitoral,
  Grevas — a Arma de Set segue sem forma decidida).
- **Progressão já existia e ninguém tinha registrado**: o `ProgressionManager` (nível de
  Exposição, curva até 12, árvore de Ecos) está implementado desde antes, apesar de o
  `CLAUDE.md` dizer que era "previsto, sem data". Corrigido em 2026-08-11.

---

## Consequências da decisão de escopo (2026-07-31)

O VS são os 14 itens desta lista: **Fase 1 completa + a última fase do jogo** (Castelo de
Carcosa), pulando as 4 fases do meio. A lógica do recorte é de **pitch**, não de
continuidade: dungeons de abertura mostram o loop de jogo (stealth, combate, armas,
companheiro) e o Rei em Amarelo mostra onde tudo isso desemboca. Junto disso, o Vini
**destravou o inventário** (antes "previsto, sem data") porque o item 2 depende dele.

**O volume que isso implica:** 2 bosses novos do zero (Byakhee, Rei em Amarelo), 2 cenas
novas (Deserto povoado, Castelo), um sistema de quest/NPC, inventário + consumíveis, e arte
para tudo isso — além da arte que a Tumba já devia.

### Recomendação de ordem (dentro do escopo escolhido)

1. **Item 5 (tempestade) antes do 4 (blockout completo).** A infra já existe e é a mecânica
   que dá identidade ao Deserto (stealth invertido). Ligar num blockout parcial já produz
   algo jogável; um blockout perfeito sem tempestade, não.
2. **Itens 6 e 7 (povoar + Coisa do Cemitério) logo depois do 5.** São baratos — o código
   de ambos já existe e está testado, é trabalho de posicionamento em cena — e transformam
   o Deserto de cenário vazio em área jogável.
3. **Inventário (pré-requisito do item 2) o quanto antes.** Outras coisas vão querer se
   pendurar nele (Necronomicon, patuá, futuros consumíveis); quanto mais tarde entrar, mais
   retrabalho nesses pontos.
4. **Arte pode correr em paralelo com o código.** É o gargalo mais provável do prazo e a
   única frente que não depende de nenhuma decisão de design pendente — a Tumba inteira já
   está mecanicamente pronta e travada só nisso.
5. **A Prioridade 4 (Castelo + Rei em Amarelo) NÃO é candidata a corte** — corrigido em
   2026-07-31. Cheguei a recomendá-la como primeira coisa a cortar se o prazo apertasse,
   antes de saber que o Castelo é a **última fase do jogo**. Com o recorte sendo
   deliberadamente "abertura + desfecho", cortar o Rei em Amarelo remove **metade da tese
   do VS** e deixa uma demo que não mostra para onde o jogo vai.

   Se for preciso cortar, os candidatos passam a ser os itens que **não** sustentam esse
   arco: item 14 (Dungeon 2 — Templo da Serpente, explicitamente "polimento"), item 8
   (quest do Santuário, a única peça de conteúdo secundário) e a profundidade do item 4
   (um Deserto menor ainda cumpre o papel de mostrar o overworld).
