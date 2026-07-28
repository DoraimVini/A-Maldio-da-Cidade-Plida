---
type: Level Design
title: Level Design — Overworld: O Deserto de Hali
description: Documento de design do overworld da Fase 1, incluindo mapa topológico, zonas de tempestade de areia, pontos de interesse, inimigos errantes e referências visuais.
tags: [level-design, overworld, deserto-de-hali, fase1, tempestade, pontos-de-interesse]
timestamp: 2026-07-28T12:00:00-03:00
status: Em revisão
---

# Level Design — Overworld: O Deserto de Hali
**Fase 1 — A Maldição da Cidade Pálida**

---

## 1. CONCEITO DO ESPAÇO

### 1.1 Identidade

O Deserto de Hali não é um deserto de areia dourada e calor. É um **deserto de cinzas cósmicas** — o resíduo sólido de sóis gêmeos que nunca terminam de morrer. O chão é mole, silencioso sob os pés, e o ar tem gosto de ferro. A paisagem não é vasta e aberta como no deserto real: ela é **claustrofóbica à distância**. Dunas altas criam paredes naturais. A visibilidade é sempre cortada por uma curva, por uma duna, pela própria tempestade.

Hali não é um lugar para ser explorado com alegria. É um lugar para ser **atravessado com cuidado**.

### 1.2 Função no Jogo

O Deserto de Hali é o **overworld conectivo da Fase 1**. Não é um mapa de hub com teleportes — é um espaço com lógica de mundo coerente que o jogador precisa aprender a ler:

- A **Tumba de Alhazred** (Dungeon 1) fica na borda ocidental, visível logo na chegada — é o objetivo principal obrigatório.
- O **Santuário de Yhtill** fica numa elevação ao noroeste, acessível por desvio natural da rota principal.
- O **Templo da Serpente** (Dungeon 2, opcional) está oculto no leste, escondido pela zona de tempestade mais intensa.
- Os **Portões das Ruínas** ficam ao norte, além do Lago de Hali — checkpoint final da Fase 1.

### 1.3 Tamanho e Escala

> **Escopo:** Grande o suficiente para ter senso de descoberta, mas não ao ponto de tornar a navegação um ônus. Nenhuma área deve demorar mais de **2 minutos de caminhada cautelosa** até o próximo ponto de interesse.

- **Dimensão aproximada:** ~22 × 16 unidades de jogo (tiles de 32 PPU)
- **Estrutura de setores:** O mapa é dividido em **5 setores** por elementos geográficos naturais (dunas, Lago de Hali, formações de pedra pálida).
- **Câmera:** Jogador enxerga ~6 tiles ao redor em cada direção. A tempestade reduz esse alcance dinamicamente.

---

## 2. MAPA TOPOLÓGICO

```
                          ┌─────────────────┐
                          │  PORTÕES DAS    │  ← Fim da Fase 1
                          │  RUÍNAS [BOSS]  │    (Byakhee)
                          └────────┬────────┘
                                   │
             ┌─────────────────────┤
             │                     │
   ┌─────────┴──────────┐    ┌─────┴────────────────────────┐
   │  SANTUÁRIO DE      │    │     LAGO DE HALI             │
   │  YHTILL [QUEST]    │    │  (impassável — barreira      │
   │  ↑ elevação rochosa│    │   natural de neblina negra)  │
   └────────────────────┘    └─────┬────────────────────────┘
                                   │
          ┌────────────────────────┤              ┌──────────────────┐
          │                        │              │  TEMPLO DA       │
   ┌──────┴─────────┐              │        ···►  │  SERPENTE [OPC.] │
   │  TUMBA DE      │      [DESERTO CENTRAL]      │  (oculto pela    │
   │  ALHAZRED [D1] │      tempestade moderada    │  tempestade      │
   │  Dungeon 1     │                             │  máxima no leste)│
   └──────┬─────────┘                             └──────────────────┘
          │
   ┌──────┴─────────┐
   │    ENTRADA     │  ← Damião chega aqui (sequência de abertura TBD)
   │  (garganta de  │
   │   pedra pálida)│
   └────────────────┘

LEGENDA:
──── Rota principal obrigatória
···· Rota de exploração (opcional)
[BOSS] Miniboss
[D1]   Dungeon obrigatória
[OPC.] Dungeon opcional
[QUEST] Ponto de missão secundária
```

---

## 3. ZONAS DE TEMPESTADE DE AREIA

> **Decisão de design (2026-07-28):** A tempestade de areia é **removida da Dungeon 1 (Tumba de Alhazred)** e **relocada para o Overworld do Deserto de Hali**. Subterrâneos não têm tempestade de areia. A dungeon passa a ter StormIntensity = 0 por padrão.
>
> **Impacto de código (fatia futura):** Os triggers `TempestadeTrigger_Z1_Spawn`, `TempestadeTrigger_Z2_Rajadas`, `TempestadeTrigger_Z3Z4_Forte` na cena `Playtest_RuinasPalidas.unity` devem ser removidos ou desativados. O `TempestadeTrigger_Z5_Nula` permanece correto (subterrâneo = 0 de qualquer forma).

| Setor | Intensidade | Visibilidade | Impacto Mecânico |
| :--- | :---: | :---: | :--- |
| **Entrada** (sul) | Calmaria | 100% | Área de orientação. Sem penalidade. |
| **Tumba de Alhazred** (oeste) | Moderada | ~70% | Passos abafados (−30% som emitido). Byakhee voa baixo, mais perigoso. |
| **Deserto Central** | Forte | ~45% | Passos completamente silenciosos. Cultistas errantes com rota aleatória. Byakhee cego. |
| **Santuário de Yhtill** (noroeste) | Calma sobrenatural | 90% | Sem vento. RM **não drena** (luz espiritual de Cassilda). |
| **Leste / Templo da Serpente** | Tempestade máxima | ~15% | Navegação às cegas. Marco visual necessário. Drena 2 RM/s. |
| **Portões das Ruínas** (norte) | Calmaria ominosa | 95% | Sem vento. Luta com o Byakhee exige visibilidade total. |

### A Tempestade como Stealth Invertido

Na Dungeon 1, o stealth é **silêncio ativo** — o jogador gerencia o som que emite.

No Overworld, a tempestade cria **stealth passivo** na região central: o vento abafa tudo. Isso cria um espaço de alívio no meio do percurso, mas exige que o jogador aprenda a **ler a tempestade** como aliada e inimiga ao mesmo tempo.

---

## 4. PONTOS DE INTERESSE

### 4.1 ENTRADA — A Garganta de Pedra Pálida

- **Tipo:** Ponto de chegada / Tutorial implícito
- **Visual:** Passagem estreita entre dois penhascos de pedra pálida. Luz de dois sóis gêmeos esmaecidos no horizonte cor de osso.
- **Mecânica:** Sem inimigos. Poste de luz rachado que ainda funciona = primeiro Refúgio e ensino da mecânica de RM.
- **Narrativa:** Inscrições em Aklo nas pedras (log #001). Na areia, a **1ª página de um diário perdido de um antigo nobre de Yhtill** (item de introdução para a quest de Cassilda).
- **Pendência:** Sequência de abertura (cinemática detalhada em `cinematica_abertura_deserto.md` e storyboard).

---

### 4.2 TUMBA DE ALHAZRED — Dungeon 1

- **Tipo:** Dungeon obrigatória — área implementada em `Playtest_RuinasPalidas.unity`
- **Conexão:** Visível da Entrada. Depressão no solo com escadaria de pedra.
- **Retorno ao Overworld:** Damião emerge por saída diferente da entrada, mais ao norte. A dungeon funciona também como **atalho geográfico** do overworld.
- **Pós-dungeon:** Necronomicon **reage ao ambiente** — páginas vibram na direção do leste (hint para Dungeon 2).
- **Pendência:** Ponto exato de emergência no overworld (coordenada) ainda a definir.

---

### 4.3 SANTUÁRIO DE YHTILL — Quest de Cassilda

- **Tipo:** Ponto de quest (obrigatória para obter o Patuá das Luas Gêmeas)
- **Localização:** Elevação acessível por rampa de pedra, fora da rota natural mas visível de longe por contraste de iluminação.
- **Atmosfera:** Única zona de calmaria sobrenatural. A tempestade **para** nas bordas da plataforma. RM não drena.
- **Cassilda:** Em corpo semi-translúcido. Fala com Damião independentemente de qualquer pré-requisito — **as quests do overworld são independentes entre si e podem ser acessadas em qualquer ordem de exploração** (decisão 2026-07-28).
- **Poste de Luz:** Refúgio e ponto de save.
- **Pendência:** Conteúdo da quest (objetivos, diálogos) — ver `lore/cassilda_e_byakhee.md`.

---

### 4.4 TEMPLO DA SERPENTE — Dungeon 2 (Opcional)

- **Tipo:** Dungeon opcional encontrada por exploração
- **Localização:** Extremo leste, zona de tempestade máxima. Requer atravessar ~4 tiles de visibilidade quase zero.
- **Entrada:** Espiral de pedras serpentinas semienterradas que forma uma porta quando alinhada com as luas gêmeas.
- **Forma de descoberta (decisão 2026-07-28):** **Drop de mapa** — qualquer personagem inimigo no overworld tem chance de dropar um Mapa Fragmentado do Templo da Serpente. O mapa é um item consumível que revela a localização no overworld com um marcador visual discreto (rastro de luz pálida no chão, na direção leste). Essa é a **única** forma de descoberta oficial — as outras formas (Necronomicon reage, marco visual) foram descartadas como mecânica separada, podendo ser usadas como dressing visual e ambiental sem função de gameplay.
- **Conteúdo interno:** TBD — ver `lore/templo_da_serpente.md`.

---

### 4.5 LAGO DE HALI — Barreira Natural

- **Tipo:** Elemento geográfico / barreira impassável / hazard de lore
- **Mecânica:** Impassável. Fronteira entre oeste (Tumba, Entrada) e norte (Portões, Santuário).
- **Perigo passivo:** Ficar na margem por mais de 10 segundos causa drenagem lenta de RM (−5 RM/s). Câmera distorce levemente.
- **Visual:** Superfície negra, perfeitamente imóvel. Nenhuma reflexão. Borda com brilho pálido-amarelado — como luz vindo de baixo.
- **Função narrativa:** Hastur dorme aqui. Presença como barreira reforça a cosmologia sem cutscene.

---

### 4.6 PORTÕES DAS RUÍNAS — Fim da Fase 1

- **Tipo:** Arena de Miniboss / checkpoint de saída de fase
- **Acesso:** Acessível após sair da Tumba. Não requer Dungeon 2 nem quest de Cassilda (mas relíquias podem ajudar na luta).
- **Preparação:** Área sem inimigos com Poste de Luz (último Refúgio da Fase 1) e penas espalhadas como prefiguração do Byakhee.
- **Arena do Byakhee:** Área aberta. Ataques aéreos rasantes, gritos de desorientação, tentativa de agarrar e voar com Damião (dano contínuo de RM).
- **Drop:** Anel do Sinal Amarelo.
- **Pós-Boss:** Portões se abrem. `TransicaoDeFaseTrigger` → Fase 2 (Castelo de Carcosa).

---

## 5. INIMIGOS NO OVERWORLD

### 5.1 Filosofia

**Cada inimigo no overworld tem função de design específica.** Sem presença genérica de dificuldade.

| Inimigo | Zona | Comportamento | Função de Design |
| :--- | :--- | :--- | :--- |
| **Cultistas errantes (pares)** | Deserto Central | Patrulha diagonal, rota levemente aleatória por influência da tempestade | Tensão no cruzamento. Fáceis de desviar individualmente, mas em pares forçam separar e contornar |
| **Byakhee (sentinelas aéreos)** | Zona moderada + Portões | Circulam em elipse. Visão cônica para baixo. Cegos na tempestade forte | Forçar o jogador a monitorar o céu além do chão |
| **Coisa do Cemitério (1x)** | Deserto Central (meio do mapa) | **Patrulha errante por toda a extensão do deserto central** — IA de rota aleatória lenta, guiada por faro. O encontro é imprevisível: o jogador pode cruzar o deserto inteiro sem vê-la, ou encontrá-la de frente a qualquer momento (2026-07-28) | Terror puro sem solução de combate. Tensão passiva permanente: o jogador nunca sabe onde ela está. Pode ser encontrada antes ou depois da Dungeon 1 — sem pré-requisito. |
| **Sementes de Hastur** | Lago de Hali (borda) | Flutuam na margem — explodem ao toque do jogador | Reforçar que o Lago é proibido |
| **Cultista do Templo (especial)** | Leste (tempestade máxima) | Caminha em direção ao Templo. Não ataca proativamente | Guia opcional. Derrota furtiva = mapa fragmentado |

### 5.2 O Que Não Colocar

- Grupos de 3+ cultistas em campo aberto.
- Inimigos fixos sem função de design.
- Qualquer inimigo na zona de Entrada ou no Santuário de Yhtill (zonas de respiro).

---

## 6. REFERÊNCIAS VISUAIS

### 6.1 Paleta de Cores

| Elemento | Cor | Código |
| :--- | :--- | :--- |
| Areia / Cinza cósmico | Bege acinzentado | `#C8B89A` |
| Rochas pálidas | Calcário frio | `#D4CFC8` |
| Céu | Amarelo-ocre escuro | `#7A6A3A` |
| Tempestade (partículas) | Âmbar translúcido | `#D4883A` |
| Lago de Hali | Preto absoluto | `#080808` |
| Borda do Lago (brilho) | Amarelo pálido | `#E8E0A0` |
| Postes de Luz (Refúgios) | Amarelo quente | `#F5C842` |

### 6.2 Referências de Jogos

| Referência | O que emprestar |
| :--- | :--- |
| **Hyper Light Drifter** | Navegação silenciosa em overworld vazio. Descoberta orgânica de pontos de interesse. |
| **Death Stranding** | Traversal como desafio. O terreno em si é o obstáculo. |
| **Blasphemous** | Pixel art com horror sacro. Luz e sombra dramáticas. |
| **Silent Hill 2** | Névoa/tempestade como mecânica diegética, não UI. |

---

## 7. DECISÕES FECHADAS (2026-07-28)

| # | Decisão | Resultado |
| :- | :--- | :--- |
| 1 | Coisa do Cemitério no overworld | **Patrulha errante no meio do mapa**, IA aleatória. Encontro imprevisível — pode ou não acontecer dependendo de onde está na hora que o jogador passa. Sem pré-requisito de dungeon. |
| 2 | Ordem das quests do overworld | **Completamente independentes entre si.** Santuário, Dungeon 2 e Portões podem ser acessados em qualquer ordem de exploração. |
| 3 | Ponto de emergência da Dungeon 1 | **Tanto faz** — a definir na hora da construção da cena, sem impacto de design. |
| 4 | Descoberta do Templo da Serpente | **Drop de mapa** de qualquer inimigo do overworld. Única mecânica oficial de descoberta. |
| 5 | Sequência de abertura | **Cinemática** — ver `systems/cinematica_abertura_deserto.md` (criado em 2026-07-28). |

## 8. PENDÊNCIAS RESTANTES

- **Remoção da tempestade da Dungeon 1** — execução em fatia futura de código (remover triggers Z1, Z2, Z3Z4 da cena `Playtest_RuinasPalidas`).
- **Conteúdo da quest de Cassilda** (objetivos, diálogos) — ver `lore/cassilda_e_byakhee.md`.
- **Conteúdo do Templo da Serpente** (layout, inimigos, chefe) — ver `lore/templo_da_serpente.md`.
- **Drop rate do Mapa Fragmentado** — a definir no balanceamento.
