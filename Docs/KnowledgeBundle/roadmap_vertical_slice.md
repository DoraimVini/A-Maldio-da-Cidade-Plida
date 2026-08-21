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
| ~~**Áudio: ausente**~~ **Áudio: existe e está ligado** ⚠️ **corrigido em 2026-08-20** | A linha anterior dizia "zero `AudioSource`/`AudioClip` no gameplay, zero arquivos de som" e concluía que *"hoje se joga um stealth sonoro sem ouvir nada"*. **Estava errada.** Existe uma camada de áudio **sintetizado** (`SinteseDeSom` gera as formas de onda em runtime, por isso zero arquivos `.wav` é o esperado, não um sintoma): `MixerDeAudio` está em **5 cenas**, e `AudioDeStealth` e `AudioDeResiliencia` também — as cinco de gameplay. Conferido por GUID no YAML. | **O pilar sonoro toca.** Sobram dois buracos reais, bem menores que o alegado: **(a)** `AudioDeCombate` está só no `Cultista.prefab` — Byakhee, Rei em Amarelo, Abdul, Espectro e Esqueleto **lutam em silêncio**; **(b)** `SomDoJogo` não está em lugar nenhum. |
| **Persistência incompleta** | `InventoryManager.GetSaveData()` e `ProgressionManager.GetSaveData()` **nunca são chamados** — nada os liga ao `GerenciadorDeSave`. | Mochila, equipamento, nível de Exposição, Ecos e slots de Artefato **se perdem** ao recarregar. O `RefugioDeLuz` grava em disco, mas grava um save incompleto. |
| **Fluxo de jogo** | Zero arquivos para menu, pause ou tela de morte. | Não dá para começar, pausar nem perder. Hoje só se entra pela cena aberta no Editor, e o Colapso não tem desfecho de tela. |
| **Animação** | Nenhum `Animator` no gameplay (só cinemática e `ResilienciaBar`). | Tudo estático. O Abdul tem spritesheet fatiado em 28 frames usando **um frame só**. ⚠️ **Re-medido em 2026-08-19 — os dois números acima estavam errados e a conclusão muda; ver a linha abaixo.** |
| **Animação — remedição (2026-08-20)** | **Feita.** Os seis personagens animam. A linha anterior desta célula ("7 clipes `.anim` no projeto inteiro, todos do Abdul, todos desligados; nenhum dos 3 chefes tem `Animator`; todo personagem desenha um quadro parado") descrevia o estado de **antes** do trabalho de 19/08 e ficou obsoleta no mesmo dia. Conferido no YAML dos prefabs em 2026-08-20. | **Dois caminhos convivem, e isso é deliberado.** Byakhee, Cultista, Espectro e Damião usam `Animador*` — MonoBehaviours que leem a FSM e escrevem `SpriteRenderer.sprite` direto, **sem `AnimatorController`**. Os arrays estão populados: 27, 17, 9 e 39 sprites respectivamente. Rei em Amarelo e Abdul usam `Animator` de verdade (o do Rei tem 5 estados, 5 clipes e estado default, acionado por `animator.Play(...)` — por isso 0 parâmetros, e está certo assim). **Um guarda proíbe** Animator no Byakhee: quando os dois sistemas escreveram o mesmo `SpriteRenderer`, o `OnDanoSofrido` ficou duplamente assinado. Ver `AnimacaoDoByakheeTests`. |

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
| 2 | **Sistema de Consumíveis** | ✅ **Fechado** (2026-08-12) | `InventoryManager` + `ItemDatabase` + `ItemDef`, `EquipmentInventory` com 7 slots, `BarraDeItens` na HUD (teclas 1–8) e `ConsumirItem` funcionando. **3 consumíveis autorados** (Água da Cacimba → corpo, Erva de Ancoragem → mente, Raiz de Yhtill → os dois) e **9 instâncias espalhadas no Deserto** via `Tools/FavelaAmarela/Montar consumíveis do Deserto`. Modelo: **finitos, não farmáveis**, com o anti-*soft-lock* no `RefugioDeLuz` (que agora cura 40% da Vitalidade além da RM cheia) em vez de moeda ou recarga. ⚠️ A anotação anterior dizia `grep "Tipo: 3" devolve zero` — era **factualmente errada**, os 3 já existiam. Ver [inventario_e_consumiveis.md](systems/inventario_e_consumiveis.md). |
| 3 | **Companheiro (RC)** | ✅ **Fechado** (2026-08-20) | Seguir Damião ✅; incapacitação + reanimação num Refúgio ✅ (2026-07-31); **barra no HUD ✅** (`CompanheiroBar`, na família `BarraAnimada<TFonte>`). A barra é ligada **por evento** (`CompanionManager.OnCompanheiroRegistrado`), não no bootstrap: Yug-Neth só vira companheiro quando libertado no meio do jogo, e perguntar no arranque devolveria `null`. Nasce **desativada** — uma barra vazia desde o menu anunciaria um recurso que o jogador ainda não tem. Guarda: `BarraDoCompanheiroTests`. |

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
| 5 | **Tempestade de Memória** | ✅ **Funcional no Deserto** (2026-08-01) | Driver + véu visual instalados na cena; o `GameLoopBootstrap` os liga no bootstrap (era o `GameManager` — **classe removida na 33ª rodada, 2026-08-18**; referência corrigida em 2026-08-19). **Intensidade→detecção já funcionava**: a percepção do Cultista é 100% sonora e a tempestade abafa o ruído do Damião (`PlayerStealthState.AplicarAbafamentoTempestade`) — stealth invertido conforme decidido. **Intensidade→velocidade foi descartada** (decisão do Vini: a tempestade atrapalha só os inimigos). Falta variar a faixa por setor (`TempestadeZonaTrigger`), o que depende do item 4. |
| 6 | **População de inimigos** | ✅ **Em cena** (verificado 2026-08-12) | **11 Cultistas** no Deserto de Hali, sob o grupo `Inimigos_Deserto`, cada um com chave de persistência própria (12 chaves distintas contando a Coisa — sem elas o abatido ressuscita ao recarregar). `PovoarODeserto` espalha por setor com **densidade proporcional à tempestade** — ela abafa o ruído do Damião, então setor de tempestade forte aguenta mais companhia sem ficar injusto. Setor de chegada fica vazio de propósito. ⚠️ **Não rode a ferramenta de novo nesta cena:** ela é idempotente na estrutura (semente fixa `20260811`, mesma distribuição), mas `ObjetoPersistente.GarantirChave()` sorteia um GUID novo por inimigo recriado — as 12 chaves mudam e **todo abate registrado no save é perdido**, fazendo os mortos ressuscitarem. Verificado em 2026-08-12. Para tornar a ferramenta reexecutável, a chave teria de ser derivada de algo estável (setor + índice) em vez de aleatória. |
| 7 | **A Coisa do Cemitério** | ✅ **Em cena** (verificado 2026-08-12) | `CoisaDoCemiterioAI` + FSM implementados (caça por faro, insta-kill). **1 instância** no Deserto Central, com chave de persistência própria. Uma só de propósito: ela caça por faro, então a tempestade que protege contra Cultistas **não serve de nada** contra ela — duas fariam do Deserto um corredor de morte. |

---

## Prioridade 3 — Encerramento da Fase 1

| # | Item | Estado |
|---|---|---|
| 8 | **Quest do Santuário de Yhtill** (Cassilda + fragmentos) | ✅ **Jogável de ponta a ponta** (2026-08-02) — `CancaoIncompleta` + `RecitalDaCancao` (Core, 13 + 9 testes), `CassildaNPC` e `FragmentoDeYhtill` implementados; Cassilda agora é prefab (`Cassilda.prefab`) com todo o conteúdo textual, instanciada e ligada em cena; os 3 fragmentos carregam as 2 primeiras estrofes da Canção de Cassilda; entregar tudo abre um **recital sem punição** das 2 estrofes finais antes do Patuá; o primeiro encontro ganhou a ramificação A/B/C do roteiro do lore (cosmética, via `PainelDeEscolha`). Progresso atravessa as duas cenas via save; o recital e a escolha do primeiro encontro não são persistidos (decisão). **Falta:** só arte — sprites placeholder em Cassilda, fragmentos e piso. Ver [quest_cassilda.md](systems/quest_cassilda.md). |
| 9 | **Boss Byakhee** | ✅ **Core, Runtime e prefab prontos** (2026-08-12) — `ByakheeFSM` (3 fases, 10 testes), `ByakheeAI`, `Ficha_Byakhee` e `Drop_Byakhee` com o Anel do Sinal Amarelo garantido — **a tabela existia e não estava ligada a nada** até 2026-08-19: o `DropAoAbater` só vivia no `Cultista.prefab`, então o Byakhee morria sem largar o Anel e o rito do Rei era impossível de fechar em jogo. Corrigido por `LigarDropDoByakhee` + guarda `ReliquiasDoRitoTests`. `Byakhee.prefab` com spritesheet animado real (26 frames, 6 animações nomeadas) e o `AnimadorDoByakhee` ligado e populado. **Arena em cena desde 2026-08-20** (`Portoes_Das_Ruinas.unity`, alcançável pelo marco do Deserto; luta por gatilho, Portões destrancados no abate e abertos por interação). **Falta:** só a cena de abertura (o grito antes da forma). O `AnimadorDoByakhee` **já está no prefab e populado** com os 27 sprites — a menção anterior a "ligar o `Animator`" estava obsoleta, e pior, apontava para o caminho errado: há um guarda que **proíbe** `Animator` neste prefab. (O papel de Yug-Neth como "chave dimensional" foi **descartado** em 2026-08-20 — no lugar, o fim da luta libera um **Poste de Luz**, que já reanima o companheiro, ancora a RM, cura e grava a partida.) Ver [systems/boss_byakhee.md](systems/boss_byakhee.md). |
| 10 | **Transição de Fase** | ✅ **Fechado** (2026-08-20) — os Portões das Ruínas são a transição. Abater o Byakhee **destranca**; quem **abre** é o jogador, interagindo no portão (`PortaoDosPortoes`, um `IInteragivel`). Arte: Kenney "Dungeon Pack" 2.3, CC0. |

---

## Prioridade 4 — O Castelo de Carcosa (**última fase do jogo**)

> O jogo completo tem **6 fases**; o Castelo é a última. Ele não vem "logo depois" da Fase 1
> na campanha final — o VS salta as 4 fases do meio de propósito, para mostrar abertura e
> desfecho. Isso é escopo, não continuidade narrativa: a transição Fase 1 → Castelo dentro
> do VS é um **corte de apresentação**, não o fluxo real do jogo.

| # | Item | Estado |
|---|---|---|
| 11 | **Blockout do Castelo** | ✅ **Greybox jogável** (2026-08-19) — `Castelo_Carcosa.unity` existe, está no Build Settings e é alcançável pelos **Portões das Ruínas** — o atalho direto do Santuário, que existia só porque o Castelo era cena solta, foi **removido em 2026-08-20**: ele pulava o Byakhee, a única fonte do Anel do Sinal Amarelo, e levava ao Rei sem o que é preciso para vencê-lo. Quatro zonas do caminho crítico montadas por `MontarCasteloCarcosa`: **Z1 Portões** (chegada + Refúgio), **Z2 Salão do Banquete** (6 nobres fossilizados como cobertura + 2 `CortesaoPalido` patrulhando), **Z3 Biblioteca** (3 Espelhos de Aldebaran com `PressaoPsiquicaZone` + 2 `EcoDeCarcosa`), **Z5 Trono** (Rei + 3 pontos focais). **Z4 Observatório fica de fora seguindo o design** — é dungeon opcional, aberta só com o Set Lendário 4/4. **O achado que destravou:** `PressaoPsiquicaZone`, `CortesaoPalido`, `EcoDeCarcosa`, `PontoFocalDeReliquia` e `DetectorDeCostas` já estavam **todos escritos e em cena nenhuma** — o Castelo era ligação, não sistema novo. **Falta:** vestir com arte (ver nota abaixo) e playtest. Guarda: `CasteloDeCarcosaTests` (4 testes). |

> **Nota de arte do Castelo (2026-08-19).** `Carcosa_Tiles.png` foi conferido e **não é interior
> de palácio**, apesar do nome: são tiles isométricos de **deserto** (dunas douradas, rocha negra,
> Sol Negro). A paleta preto-e-ouro serve ao "mármore negro com adornos de ouro manchado" do
> design, mas o arquivo está a **PPU 100 e não fatiado** — fora do padrão do projeto. Também
> avaliei e descartei o `Aquanoctis_IsoSliceBasicDungeonAssets`: o README revela que é para
> **Sprite Stacking** (os 16 "quadros" são fatias horizontais de um voxel, não animação), técnica
> que o projeto não usa. O greybox usa retângulos coloridos nessa paleta.

> **Divergência OKF × código no rito final.** O documento de level design fala de **4 relíquias**
> (Anel, Coroa, Patuá, Necronomicon), mas `ReiEmAmareloAI.idsDasReliquiasExigidas` exige **3** — a
> Coroa de Ossos está de fora porque não tem fonte jogável. A ferramenta lê os ids **do Rei**, não
> de uma lista própria, então criou 3 pontos focais; o guarda segue o código, como manda o
> `CLAUDE.md` §3.1 regra 4. Decidir se a Coroa entra no rito é chamada de design.
| 12 | **Boss Rei em Amarelo** | ✅ **Jogável de ponta a ponta** (2026-08-20) — **`OnVitoria` tinha zero assinantes até aqui**: o evento existia com o comentário "quem monta a cena decide o que fazer com isso" e ninguém decidia, então selar o Rei só repintava o sprite. Ligado à `SequenciaDeSelamento` (espelha a `SequenciaDeColapso`; a linha do desfecho é **provisória** e fica serializada, para trocar no Inspector). Guarda: `CasteloDeCarcosaTests.VencerORei_TemConsequencia`. Core, Runtime e prefab prontos desde 2026-08-12 — — `ReiEmAmareloFSM` (ritual de relíquias + selamento em ciclos, 13 testes), `DetectorDeCostas` (geometria da Máscara Pálida, 7 testes), `ReiEmAmareloAI` e `PontoFocalDeReliquia`. `ReiEmAmarelo.prefab` com sprite emprestado (recorte do spritesheet "Necromancer" da Inbox — arquétipo certo, cores erradas). A Coroa de Ossos ainda não tem fonte jogável — contornado com o `CarcosaDebuggerWindow` (concede/invoca sob demanda, agora instanciando os prefabs reais dos dois chefes) e a `Cena_ArenaDeTestes` (cena de dev, fora do Build Settings). **Falta:** arte final (cores, Máscara Pálida), o Trono de Aldebaran em cena de verdade, e uma fonte jogável para a Coroa de Ossos. Ver [systems/boss_rei_em_amarelo.md](systems/boss_rei_em_amarelo.md). |

---

## Caminho crítico para a build (definido em 2026-08-19)

O Vini pediu o caminho mais curto até **uma build entregável**, para então focar inteiramente no
Templo do Povo-Serpente. Contado sobre o estado medido, sobram **cinco** itens — a cinemática
(13) saiu por decisão dele, e o Templo (14) é o destino, não o caminho.

| ordem | item | o que falta | tamanho |
|---|---|---|---|
| ~~1~~ | 3 — Companheiro | ✅ **feito 2026-08-20** — `CompanheiroBar` ligada por evento | — |
| ~~2~~ | 9 — Byakhee | ✅ **feito 2026-08-20** — `Portoes_Das_Ruinas.unity` | — |
| ~~3~~ | 10 — Transição de Fase | ✅ **feito 2026-08-20** — o portão interagível | — |
| ~~4~~ | 12 — Rei em Amarelo | ✅ Trono em cena 2026-08-19. A Coroa de Ossos **não é exigida pelo rito** (o Rei pede 3 relíquias, não 4) — ela só faz falta para o Set Lendário 4/4, que abre a Z4 opcional, fora do VS | — |
| ~~5~~ | 11 — Blockout do Castelo | ✅ **feito 2026-08-19** — `Castelo_Carcosa.unity` | — |

**A lista fechou.** As seis cenas do caminho estão no Build Settings e ligadas ponta a ponta
(auditado em 2026-08-20 pelo casamento `chegarEm` × `PontoDeChegada`, que achou uma ligação
muda e virou o guarda `NavegacaoEntreCenasTests`):

```
Cena_Menu → Deserto_Hali ⇄ Playtest_RuinasPalidas
                         ⇄ Santuario_Yhtill
                         → Portoes_Das_Ruinas → Castelo_Carcosa
```

O que separa isto de uma build entregue é **playtest**, não construção: nada do caminho crítico
foi jogado de ponta a ponta por um humano ainda.

> ✅ **O Build Settings tem as 6 cenas do caminho** (`Cena_Menu`, `Deserto_Hali`,
> `Playtest_RuinasPalidas`, `Santuario_Yhtill`, `Portoes_Das_Ruinas`,
> `Castelo_Carcosa`). O aviso anterior — "o jogo compila sem o desfecho" — deixou de
> valer em 2026-08-19/20. `Cena_ArenaDeTestes` segue fora, de propósito: é cena de dev.

---

## Prioridade 5 — Polimento

| # | Item | Estado |
|---|---|---|
| 13 | **Cinemática de abertura** | ⏸️ **ADIADA — decisão do Vini, 2026-08-19.** Não há artista para produzi-la e não há ferramenta definida para fazê-la. **Sai do caminho crítico da build**: não bloqueia a entrega do VS. O design continua em `systems/cinematica_abertura_deserto.md` e o esqueleto de código em `Assets/Scripts/Cinematics/AberturaDesertoCinematica.cs`. Reavaliar só depois que a build estiver entregue. |
| 14 | **Dungeon 2 (Templo da Serpente)** | ⚠️ **Começada** — corrigido em 2026-08-19; a linha anterior dizia "❌ Não começada" e estava errada. Existem três arquivos: `Assets/FavelaAmarela/Editor/GeradorCenaTemploSerpente.cs`, `Assets/FavelaAmarela/Editor/TemploSerpenteSceneBuilder.cs` e `Assets/Scripts/Dungeons/TemploSerpenteSetup.cs`. **Não verifiquei ainda** se produzem cena jogável nem se há cena salva — só que o código existe. Design completo em `lore/templo_da_serpente.md`. |

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
