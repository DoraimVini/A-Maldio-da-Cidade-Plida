---
type: Lore Reference
title: O Templo da Serpente — Design Completo
description: Dungeon 2 do Deserto de Hali. Semi-obrigatória (opcional, mas com Set Lendário que impacta o final). 10-12 zonas, dois bosses, lore dos Serpent People.
tags: [lore, dungeon, optional, desert, templo-da-serpente, nagaraja, avatar-de-set, serpent-people]
timestamp: 2026-07-30T11:00:00-03:00
---

# O Templo da Serpente — Design Completo (Vertical Slice)

> **Status:** Design fechado para o Vertical Slice do edital (2026-07-30).
> **Tipo:** Dungeon semi-obrigatória. Opcional para progressão, mas o **Set Lendário** obtido aqui impacta o desfecho (Sidequest de Nyarlathotep no Castelo de Carcosa).
> **Lore completo da raça:** Ver [povo_serpente.md](povo_serpente.md).

---

## I. Introdução e Descoberta

**Localização:** Extremo leste do Deserto de Hali, dentro da zona de tempestade máxima (~15% de visibilidade).

**Descoberta:** O **Mapa Fragmentado** — item raro droppado por qualquer inimigo errante do deserto — revela a localização do Templo com um símbolo de serpente enrolada. Sem o mapa, a entrada é invisível sob a areia.

**Visual da Entrada:** Uma espiral de pedras serpentinas semienterradas, alinhadas com as luas gêmeas de Carcosa. Gravuras em Aklo Serpentino na pedra de entrada alertam: *"Hali-nasseth"* (o lago nos aprisiona). A tempestade se intensifica ao cruzar o limiar.

**Tom Narrativo:** Diferente da Tumba de Alhazred (terror urbano, claustrofóbico), o Templo é **grandioso e alienígena** — arquitetura ciclópea de pedra negra, afrescos de batalhas pré-humanas, estátuas de Set com olhos de ônix que parecem seguir Damião. O Povo Serpente não é louco como os cultistas: é *antigo e deliberado*.

---

## II. Estrutura em 12 Zonas

```
[ENTRADA] ──▶ [Z1: Átrio das Escamas] ──▶ [Z2: Corredor dos Glifos]
                                                    │
                                          [Z3: Câmara dos Sseth]
                                                    │
                              ┌─────────────────────┘
                              ▼
              [Z4: Salão dos Nagas] ──▶ [Z5: O Poço de Yig]
                                                │
                                      [Z6: Câmara do Veneno]
                                                │
              ┌─────────────────────────────────┘
              ▼
[Z7: Cripta das Larvas] ──▶ [Z8: Galeria das Visões de Set]
                                       │
                             [Z9: Santuário do Olho]
                                       │
                     ┌─────────────────┘
                     ▼
         [Z10: Câmara do Trono do Sacerdote]
                     │
         ══════════════════════════════════
         ★ BOSS 1: NAGARAJA (Sacerdote) ★
         ══════════════════════════════════
                     │
         [Z11: A Fenda de Set — Zona de Transição]
                     │
         ══════════════════════════════════════
         ★ BOSS 2: O AVATAR DE SET (Divino) ★
         ══════════════════════════════════════
                     │
         [Z12: Saída — O Deserto Coberto de Escamas]
```

---

## III. Descrição de Cada Zona

### Z1 — Átrio das Escamas
**Tamanho:** Grande (5x5 unidades)
**Atmosfera:** Luz filtrada por cristais de ônix. Chão coberto por escamas secas que crocham sob os pés — emitem som. O jogador aprende imediatamente: **qualquer passo aqui gera barulho dobrado**.
**Inimigos:** 2 Sseth Comuns em patrulha lenta.
**Segredo:** Uma passagem dissimulada (atrás de uma estátua de Set) leva a um baú com o **Chá Calmante** (recupera 40 de Resiliência Mental).
**Mecânica introduzida:** Escamas no chão como armadilha de som — Damião precisa agachar e se mover lentamente para não acordar os Sseth.

---

### Z2 — Corredor dos Glifos
**Tamanho:** Longo e estreito (2x8 unidades)
**Atmosfera:** Afrescos imensos nas paredes narram a guerra pré-humana entre Serpentes e os ancestrais de Hastur. O Necronomicon (se obtido) permite traduzir alguns glifos — revelando fragmentos de lore.
**Inimigos:** Nenhum. Zona de exploração e lore.
**Fragmento de Yhtill nº 4:** Escondido atrás de um painel falso. Damião encontra uma página arrancada de um diário da nobreza de Yhtill — evidência de que nobres de Cassilda chegaram aqui antes.
**Texto do Fragmento (em jogo):**
> *"...o corredor não tem fim visível. As gravuras me dizem que estas bestas eram reis antes de sermos primatas. Carrego o terceiro anel de Yhtill e rezo para que Cassilda nos encontre antes que a tempestade..."*
> — *[assinatura ilegível, Nobreza de Yhtill, data: círculo de luas gêmeas desconhecido]*

---

### Z3 — Câmara dos Sseth
**Tamanho:** Médio (4x4 unidades), múltiplas saídas
**Atmosfera:** Central de patrulha. Quatro Sseth Comuns distribuídos. Fogueira ritual de chamas verdes no centro — ilumina, mas drena RM (luz maligna ≠ poste de luz).
**Inimigos:** 4 Sseth Comuns. Um deles **Farejador** (detecta Damião por cheiro, não por som).
**Mecânica nova — Farejador:** O Sseth Farejador tem um cone de cheiro (invisible no mapa, mas indicado por partículas verde-podre). Damião pode neutralizá-lo usando um **Frasco de Incenso** (item consumível encontrado na Z1 secreta).

---

### Z4 — Salão dos Nagas
**Tamanho:** Grande (6x4 unidades), teto alto
**Atmosfera:** Dois Nagas Guerreiros patrulham devagar com lanças. Estátuas ciclópeas de Set (~3 unidades de altura) criam ângulos cegos perfeitos para stealth.
**Inimigos:** 2 Nagas Guerreiros (elites). Não detectam por som, mas têm **visão de cone ampla**.
**Mecânica de cobertura:** Estátuas de Set bloqueiam visão dos Nagas. O jogador aprende a usar os ângulos mortos entre as estátuas.
**Fragmento de Yhtill nº 5:** No pedestal de uma estátua. A quinta e última página da quest de Cassilda.
**Texto do Fragmento:**
> *"Esta é a última anotação que consigo fazer. Sinto que o Avatar me observa. Cassilda disse que as luas testemunhariam nosso retorno — mas as luas de Carcosa não piscam para nós. Que ela encontre estas páginas. Que ela saiba que viemos até o fim."*
> — *[Lorde Aldaron de Yhtill]*

---

### Z5 — O Poço de Yig
**Tamanho:** Pequeno e circular (3x3 unidades)
**Atmosfera:** Poço central de pedra negra com serpentes vivas enroladas nas bordas — as **Larvas de Yig**. Não atacam, mas grudam em Damião se ele encostar, causando lentidão severa e dreno de RM.
**Mecânica do Poço:** Jogar objetos no poço gera barulho que atrai as Larvas para longe por 10 segundos. Necessário para atravessar.
**Item:** Frasco de Veneno de Yig (consumível, pode ser usado em combate para aplicar debuff nos bosses).

---

### Z6 — Câmara do Veneno
**Tamanho:** Médio (4x3 unidades)
**Atmosfera:** Sala de preparação ritual. Recipientes com veneno de serpente fermentado. Gases amarelo-esverdeados no ar causam dreno passivo de RM.
**Mecânica:** Damião deve se mover rapidamente (paradoxo: correr gera som, mas ficar na névoa drena RM). Solução: usar o Salto Dimensional para atravessar a sala em uma única translação.
**Inimigos:** 1 Espectro Escamado (invisível na névoa, detectável pelo som de escamas).

---

### Z7 — Cripta das Larvas
**Tamanho:** Grande e labiríntico (5x5 unidades)
**Atmosfera:** Câmaras de incubação. Ovos de Larvas de Yig em toda parte. Não eclodidos, mas sensíveis a vibração — barulho alto os quebra e libera ninhadas.
**Mecânica de stealth extremo:** Qualquer som acima de um limiar quebra um ovo. Cada ovo quebrado libera 3 Larvas. Damião deve cruzar em silêncio absoluto (agachado, passo a passo).
**Recompensa:** Sala de tesouro no centro com **Peitoral de Set** (peça RNG do Set Lendário — 50% de chance de dropar aqui; caso não drope, aparece no Avatar de Set).

---

### Z8 — Galeria das Visões de Set
**Tamanho:** Longo (2x10 unidades)
**Atmosfera:** Corredor com espelhos de obsidiana negra. As reflexões mostram Set — não Damião. Cada espelho quebrado (por som/impacto) dispara uma visão psicodélica que causa 15 de Trauma direto.
**Mecânica:** Os espelhos são obstáculos que o jogador não pode tocar ou quebrar. Exige movimentos precisos num corredor estreito enquanto 2 Sseth patrulham.
**Narrativa:** Ao final do corredor, um espelho maior mostra o Avatar de Set olhando diretamente para fora do espelho — presságio do boss final.

---

### Z9 — Santuário do Olho
**Tamanho:** Médio-circular (4x4 unidades), altar central
**Atmosfera:** Câmara sagrada com o **Olho de Set**: um cristal negro gigante que pulsa com luz vermelha. Não é hostil, mas Damião não pode tocar — causaria Trauma severo.
**Inimigos:** 3 Sseth em adoração estacionária (em transe — não respondem a som, mas respondem a toque).
**Checkpoint narrativo:** Damião pode ler inscrições ao redor do Olho (com o Necronomicon) que revelam a natureza do Avatar de Set — é um fragmento da consciência divina de Set, aprisionado em Carcosa por Hastur.
**Preparação para o boss:** Damião encontra um **Poste de Luz improvisado** (tocha de Yhtill, criada com fragmentos dos Portões de Yhtill) — único ponto de Ancoragem dentro do Templo. Máximo de Resiliência Mental antes do Boss 1.

---

### Z10 — Câmara do Trono do Sacerdote
**Tamanho:** Grande (6x6 unidades), trono no centro
**Atmosfera:** Salão de audiências. Nagaraja, o Sacerdote, senta no trono — um ser de 2,5 metros com coroa de espinhos ósseos e manto de pele de Byakhee. Seu corpo é metade humano (braços, tronco) e metade serpente (a partir da cintura). Ele já sabe que Damião está ali.

**Cena de abertura (diálogo pré-boss em caixa de texto):**
> **NAGARAJA** *(em Aklo Serpentino, traduzido pelo Necronomicon se Damião o tiver)*:
> *"Ssseth-kaa... o tradutor. Alhazred nos mencionou em seu tomo podre. Você carrega o cheiro de Hali, viajante. Set-ur-haal... e você anda pelo nosso Templo como se a senha fosse o medo."*
>
> *(sem o Necronomicon, Damião ouve apenas sibilos — e o subtítulo diz apenas: [Aklo Serpentino incompreensível])*
>
> **DAMIÃO** *(pensamento interno, exibido na caixa de texto)*:
> *"Não entendo as palavras. Mas entendo os dentes."*

---

## IV. Boss 1 — Nagaraja, o Sacerdote do Sinal Escamado

**Localização:** Z10 — Câmara do Trono
**Lore:** Nagaraja é o único Serpent Person consciente de que está aprisionado em Carcosa. Séculos de encarceramento dimensional o enlouqueceram parcialmente — ele agora acredita que matar Damião libertará Set e lhe dará passagem de volta para Valúsia. É um fanático com lógica impecável e premissa falsa.

**Visual:** 2,5m de altura. Tronco superior humanoide com escamas douradas; cintura para baixo é serpente pura. Coroa de espinhos ósseos de Byakhee. Segura um **Cajado do Olho de Set** (ônix com cristal pulsante no topo).

### Atributos
- **Resiliência Mental Causada por Ataque:** 20 RM por golpe (veneno do cajado)
- **Vulnerabilidade:** Exposto quando conjura magia (2 segundos sem escudo)
- **Imunidade:** Imune a ataques físicos diretos enquanto o **Escudo Escamado** estiver ativo

### Estrutura da Luta (2 Fases)

**Fase 1 (100% → 40% de vida)**
- Nagaraja se move lentamente pelo salão, rastejando. Usa o cajado para conjurar **Ondas de Veneno** (cones de névoa verde que causam Trauma por segundo de contato).
- Escudo Escamado: Uma camada de escamas sobrepostas forma um escudo que deflecte o ataque de Damião.
- **Mecânica de quebra do escudo:** Damião deve atrair Nagaraja para pisar nas **Pedras Rituais** espalhadas na arena (marcadas com glifos dourados). Ao pisar, Nagaraja absorve a pedra e o escudo enfraquece. Ao pisar em 3 pedras, o escudo quebra por 4 segundos — janela de dano.
- **Summon:** Em 60% de vida, Nagaraja invoca 2 Sseth Comuns para flanquear Damião.

**Fase 2 (Menos de 40% de vida) — Transmutação**
- Nagaraja abandona o cajado e luta com o corpo. Seus braços se transformam em serpentes adicionais — ele ataca com quatro "cabeças" (dois braços-serpente + a cabeça + a cauda).
- O escudo muda de mecânica: agora só é vulnerável quando Nagaraja está em animação de **ataque de cauda** (3 segundos de cooldown).
- **Fúria Final:** Abaixo de 20% de vida, Nagaraja recita o nome de Set em Aklo Serpentino, causando distorção na tela e dreno passivo de 5 RM/segundo enquanto o recita. Damião deve interrompê-lo com um golpe.

### Morte e Drop
- Ao morrer, Nagaraja diz: *"Set-ur-haal... Você não entende o que libertou..."*
- Drop: **1 peça aleatória do Set Lendário** (Peitoral, Grevas ou Arma — exceto Elmo, que é do Avatar de Set).

---

### Z11 — A Fenda de Set (Zona de Transição)

**Atmosfera:** Após matar Nagaraja, uma fenda racha o chão do Trono, revelando uma escada descendente. Abaixo: uma sala menor, geométrica, com paredes de areia negra comprimida e um círculo de luz vermelha no centro.

**Narrativa:** Damião pode interagir com o círculo. O **Necronomicon** (se tiver) diz: *"Este é o umbigo de Set. Não um portal — uma impressão digital de divindade. Quem se ajoelha aqui convoca o Avatar."*

Ao interagir, o chão treme. O Avatar de Set materializa do círculo de luz.

**Cena (texto):**
> *O círculo de luz vermelha pulsa. Do chão, um braço emerge — não de serpente, não de humano. De algo que existia antes de ambos. O ar cheira a pedra quente e a presença de algo enorme que está sendo comprimido em algo menor para caber aqui.*
> *O Avatar de Set ergueu a cabeça. Olhos de âmbar. Ele não fala.*

---

## V. Boss 2 — O Avatar de Set

**Localização:** Z11 — A Fenda de Set
**Lore:** O Avatar de Set não é Set — é um **fragmento da consciência divina** de Set aprisionado por Hastur no Deserto de Hali. Set usou Nagaraja como prisão intelectual e o Avatar como prisão de poder. Com Nagaraja morto, o Avatar está parcialmente liberto — mas ainda preso à dimensão. Ele não é senciente no sentido humano; é uma força da natureza com intenção primordial.

**Visual:** 4 metros de altura dentro da arena comprimida. Forma humanoide colossalinfundida com Set — cabeça de falcão-serpente (nem o falcão de Hórus nem a serpente de Apophis, mas uma fusão grotesca), corpo coberto de hieróglifos dourados luminescentes, quatro braços com garras de obsidiana. Não usa armas — **é uma arma**.

**Arena:** Circular, pequena (4 unidades de raio). As paredes são de areia negra que o Avatar pode manipular.

### Atributos
- **Resiliência Mental Causada:** 30 RM por golpe (presença divina — causa Trauma por proximidade)
- **Aura Divina:** Damião perde 3 RM/segundo por estar na mesma sala (presença de divindade causa terror existencial)
- **Fase única:** Sem fases distintas, mas com 3 padrões de ataque que se intercalam aleatoriamente

### Mecânica Central — Hieróglifos de Vulnerabilidade
O Avatar é completamente imune a qualquer dano de Damião exceto durante a **Janela dos Hieróglifos**:
- A cada 20 segundos, um dos hieróglifos em seu corpo pulsa **branco** (em vez de dourado).
- Damião deve atingir o hieróglifo branco com a Barra Enferrujada ou a Lâmina do Sinal.
- Acertar o hieróglifo certo causa dano direto e atordoa o Avatar por 3 segundos.
- Errar (ou acertar um hieróglifo dourado) causa **Inversão Dimensional**: Damião é repelido para a parede, causando 15 de Trauma.

### Padrões de Ataque (intercalados)

| Padrão | Descrição | Defesa |
| :--- | :--- | :--- |
| **Tempestade de Set** | Invoca tempestade de areia que ocupa 50% da arena | Correr para o ângulo oposto + agachar |
| **Golpe dos Quatro Braços** | Sequência de 4 golpes físicos em leque | Salto Dimensional para atravessar o Avatar |
| **Olho de Set** | Raio de luz âmbar que atravessa a arena em linha reta | Desviar perpendicular à linha |

### Morte e Drop
- Ao morrer, o Avatar fragmenta-se em hieróglifos dourados que caem como poeira.
- **Drop garantido:** Elmo de Set ✅
- **Drop RNG:** 1 peça adicional do Set Lendário (50% de chance)
- **Narrativa de drop:** Uma voz sem corpo (Set, ausente): *"Fragmentos meus para fragmentos seus, tradutor. Use-os bem — ou não."*

---

## VI. Z12 — Saída: O Deserto Coberto de Escamas

**Atmosfera:** Escada de volta ao Deserto de Hali. A saída é marcada por escamas de Nagaraja que cobriram a areia — como se o Templo estivesse sangrando para fora.

**Evento narrativo pós-dungeon:** Damião escreve no Diário (exibido na tela durante transição):
> *"Saí do Templo. O chão ainda cheira a veneno velho. Tenho peças de um deus morto no bolso. Não sei se isso é uma bênção ou uma sentença — mas sinto que Set me vê de onde quer que ele esteja. E não parece feliz."*

---

## VII. Recompensas e Conexão com o Vertical Slice

| Recompensa | Origem | Impacto na Progressão |
| :--- | :--- | :--- |
| **Elmo de Set** | Avatar de Set (garantido) | +30% resistência a Trauma de fontes divinas |
| **1 peça RNG do Set** | Nagaraja OU Avatar (RNG) | Aproxima o jogador do Set Lendário completo |
| **Patuá das Luas Gêmeas** | Quest de Cassilda (entregue separadamente) | Desacelera dreno de RM no escuro |
| **Set Lendário 4/4** | Castelo de Carcosa completa | Acesso à Sidequest do Avatar de Nyarlathotep |

> **Nota técnica:** O `templo_da_serpente.md` anterior mencionava "Naga" como Boss 1. Esse nome foi promovido de genérico para nome próprio — o Boss 1 é **Nagaraja** (sacerdote), e os **Nagas Guerreiros** são o tipo de inimigo de elite da dungeon. Decaindo o antigo nome genérico.
