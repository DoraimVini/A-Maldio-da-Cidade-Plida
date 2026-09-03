---
type: GDD
title: Documento de Design Mestre (GDD)
description: Documento de Design de Jogo oficial e unificado para A Maldição da Cidade Pálida (Favela Amarela).
tags: [design, gdd, carcosa, core-vision]
timestamp: 2026-07-30T11:00:00-03:00
---

# A Maldição da Cidade Pálida (Favela Amarela)
## Documento de Design Mestre (GDD) — Versão 1.3 (Vertical Slice)

* **Data:** 30/07/2026
* **Autor(es):** Vinícius (Vini) — Lead Designer & Developer / Claude & Antigravity (Assistentes)
* **Status:** Aprovado para Implementação (Vertical Slice / Edital)
* **Confidencialidade:** Interna (Estúdio)

### Changelog
- **v1.4 (2026-09-02):** A família de arma do meio deixa de ser **Cravo** e passa a ser **Maça** (morning star), e a habilidade *Fincar o Aklo* vira ***Calar o Aklo***. Decisão do Vini, ao perguntar de onde tinha vindo a ideia do cravo. Motivo: o nome era um **fóssil** — nasceu na v1.0, quando a arma era um espigão de "mitigação pesada", e sobreviveu à troca da habilidade para interrupção de conjuração. Com maça, as três famílias viram **corte / impacto / perfuração** (Alfanje / Maça / Lâmina fina); antes, Cravo e Lâmina fina eram as duas lâminas diagonais e não se distinguiam no inventário. A geometria não mudou (Alcance 1,2 · Raio 0,6 · Janela 0,1). **A ideia do Cravo fica guardada para a expansão de itens** — ver `systems/armas_da_tumba.md`.
- **v1.3 (2026-07-30):** Varredura de consistência do documento. Removidas todas as menções remanescentes ao **Salto Dimensional** como mecânica ativa (§1.4 curva de sentimento, §3.1 gameplay loop, §5.4 Zona 5, §7.3 SFX, §14 apêndice) — a habilidade foi integralmente removida do jogo. Corrigido nome da arma **Cravo de Aklo** (estava "Cravo de Ferro"). Documentados no §4.3 os elementos de HUD já implementados (Vitalidade, Barra de Ações, Prompt de Interação, Painel de Escolha) e a nova **camada de interação por botão E** (§4.3.1). Adicionado §3.4.1 com a estrutura da luta do Abdul (fases, Escudo Mágico, Pedras de Poder, janela de exaustão). Marcada como pendente de redesenho a Z6 do Templo da Serpente, cujo design dependia do Salto.
- **v1.2 (2026-07-30):** Companion Yug-Neth (Mi-Go filhote) adicionado ao jogo. Encontro com Alhazred redesenhado como cena de escolha binária (Caminho A: sem luta, sem Necronomicon; Caminho B: luta, dropa Necronomicon). Sistema de Resiliência do Companheiro (RC) e mecânica de proteção adicionados. Yug-Neth é a chave dos Portões das Ruínas. Sprite de Yug-Neth gerada. Docs: `lore/migo_companion.md` (novo), `lore/abdul_alhazred.md` (atualizado).
- **v1.1 (2026-07-30):** Vertical Slice fechado para edital. Quest de Cassilda expandida (5 Fragmentos de Yhtill + diálogos completos). Templo da Serpente redesenhado com 12 zonas, Boss 1 Nagaraja e Boss 2 Avatar de Set. Boss Byakhee com mecânicas detalhadas. Novo doc: `lore/povo_serpente.md`. Boss 1 do Templo renomeado de "Naga" genérico para **Nagaraja, o Sacerdote do Sinal Escamado**.

---

## 1. VISÃO GERAL DO PROJETO

### 1.1 Resumo Executivo (Elevator Pitch)
**A Maldição da Cidade Pálida** é um jogo de furtividade e horror cósmico em 2D isométrico onde o som é o seu maior inimigo. O jogador controla Damião, um tradutor indefeso que acorda em Carcosa — dimensão do Rei em Amarelo — e deve navegar pelo Deserto de Hali, explorar dungeons ancestrais e enfrentar cultistas cegos para encontrar um caminho de volta antes que sua mente colapse completamente sob a estática cósmica de Aldebaran. O foco do jogo é a tensão constante, a sobrevivência psicológica e a vulnerabilidade do protagonista frente ao inevitável colapso mental.

### 1.2 High Concept
É como *Dishonored* encontra *Silent Hill* em perspectiva isométrica (estilo *Diablo I/II* clássico) com a visceralidade de *Death Trash* e o gerenciamento de equipamentos de *Source of Madness*.

### 1.3 Pilares de Design (Pillars)
1. **Impotência e Sobrevivência Tensa:** O combate direto é um último recurso arriscado. A sobrevivência depende de evasão, do planejamento de rotas e do silêncio.
2. **Geometria Instável (Glitch Diegético):** O próprio cenário reage às anomalias e ao estado mental do jogador. A distorção visual e os glitches são mecânicos e narrativos.
3. **Escuridão e Luz (Postes de Luz):** A escuridão das vielas corrói ativamente a mente do jogador. A luz amarela quente dos postes funciona como um porto seguro de cura e respiro.
4. **Composição POCO (Arquitetura Limpa):** A engenharia do jogo separa a lógica pura (POCO, testável) dos adaptadores visuais da Unity, garantindo um código desacoplado e robusto.

### 1.4 Objetivos de Experiência (Player Experience Goals)
* **Tensão e Paranoia:** O jogador deve sentir que cada passo ruidoso pode atrair a morte e que a própria realidade está colapsando ao seu redor.
* **Curva de Sentimento:**
  * *Primeiros 5 minutos:* Desorientação, impotência e pavor ao fugir de ameaças no escuro absoluto.
  * *Após 1 hora:* Adaptação e cálculo — lendo o layout das vielas, dosando ruído e escolhendo quando evitar e quando encarar um Cultista.
  * *Ao final:* Resignação frente ao horror inevitável de Carcosa.

### 1.5 Público-Alvo
* **Perfil:** Jogadores mid-core/hardcore de jogos indie de terror atmosférico, stealth e horror cósmico (*Signalis*, *Silent Hill*, *Deus Ex*, *Death Trash*).
* **Plataforma Preferida:** PC (Steam), utilizando preferencialmente teclado/mouse ou controle com foco tátil.

### 1.6 Plataformas e Requisitos Gerais
* **Plataforma Alvo:** PC (Windows).
* **Requisitos Gerais:** Jogo otimizado para 60 FPS estáveis. Física 2D no plano XY.
* **Multiplayer:** Não aplicável (Foco 100% Singleplayer).

### 1.7 Diferenciais Competitivos (Unique Selling Points)
* **HUD Diegético e Sem Poluição:** A saúde do jogador é sua própria percepção de sanidade (Resiliência Mental). O colapso mental altera a renderização da tela, os sons e a precisão do controle.
* **O Som como Vetor de Caça:** A mecânica de propagação sonora substitui a visão clássica dos inimigos nas fases iniciais, exigindo que o jogador gerencie o atrito físico e o barulho dos seus poderes.
* **Slots de Ação Estilo "Source of Madness":** Equipamento minimalista em 2 slots (Mão Física + Mão Anômala) em vez de inventário tradicional complexo de RPGs.

### 1.8 Escopo e Metas Comerciais
* **Universo Multimídia:** *A Maldição da Cidade Pálida* faz parte de um **universo multimídia em desenvolvimento concomitante**, iniciado com o curta-metragem *Favela Amarela* (Richard Abelha): curta (existê), animação, HQ e longa-metragem estão em desenvolvimento paralelo ao jogo. O jogo é a fatia deste universo dedicada ao encontro com o Rei em Amarelo. Os fragmentos de lore encontrados dentro das dungeons (especialmente a Tumba de Alhazred) conectam diretamente com o trabalho audiovisual.
* **Estrutura Macro (6 Fases):** O jogo completo é composto por **6 Fases** até sua conclusão narrativa. Este GDD detalha em profundidade a **Fase 1 — O Deserto de Hali**: um overworld aberto (32 PPU) contendo duas dungeons (Tumba de Alhazred + Templo da Serpente), o Santuário de Yhtill e os Portões das Ruínas. **As antigas "Ruínas Pálidas" (S-Path, 9 Zonas) viraram a Dungeon 1 (Tumba de Alhazred) dentro desta Fase 1** (2026-07-28). As Fases 2 a 6 do jogo completo serão especificadas em documentos próprios conforme o design avançar. Este é um RPG, não um roguelike: **não existe "tela de Vitória"** ao fim de uma fase ou dungeon — o encerramento é uma **transição** para o próximo trecho. A única vitória do jogo é o desfecho da história, ao fim da Fase 6.
  > **Nota de escopo:** a numeração "Fase 1/Fase 2" usada no `gdd_expansao_deserto_demo.md` (Vertical Slice/demo) é local àquele documento e não corresponde 1:1 a esta contagem de 6 fases do jogo completo — as duas ainda não foram reconciliadas.
* **Tipo de Lançamento:** Premium (Steam).
* **Duração Estimada:** Fase 1 (Deserto de Hali, incluindo a Tumba de Alhazred), ~2 a 3 horas de gameplay; duração total do jogo completo (6 fases) a definir.

---

## 2. NARRATIVA E MUNDO

### 2.1 Sinopse da História
**Damião** é o personagem central do universo *Favela Amarela* (criado por Richard Abelha), um tradutor obcecado por escrituras antigas. No curta-metragem original, Damião morre na Terra. O jogo começa no momento seguinte: **ele acorda em Carcosa** — a dimensão do Rei em Amarelo — cercado por Espectros de Hali, no meio do Deserto das Cinzas.

Sem saber como chegou ali, sem armas e com a mente já fragilizada pela morte, Damião precisa explorar o Deserto de Hali para entender onde está. Ao adentrar a **Tumba de Alhazred** (Dungeon 1), ele encontra fragmentos do lore de Carcosa — inscrições e registros que conectam diretamente com os eventos do universo audiovisual (*curta*, *animação*, *HQ*, *longa*). Vencer o miniboss **Abdul Alhazred** lhe rende o *Necronomicon*, artefato que começa a abrir o caminho para entender as regras desta dimensão.

Explorando mais o deserto, ele pode encontrar o **Templo da Serpente** (Dungeon 2, opcional), enfrentar seus dois guardiões e obter peças de um conjunto de equipamento lendário que desbloqueiam uma sidequest dentro do **Castelo de Carcosa** (Fase 2). A **Rainha Cassilda**, no Santuário de Yhtill, oferece um puzzle narrativo cuja conclusão recompensa Damião com o *Patuá* (item de proteção mental). Para sair do Deserto, ele deve enfrentar o **Byakhee** nos Portões das Ruínas — guardião alado que barra a entrada no Castelo. No jogo completo, a jornada se estende pelas **6 Fases** até o confronto com o próprio **Rei em Amarelo** na fase final.

### 2.2 Temas e Tom
* **Temas:** A inevitabilidade da loucura, a insignificância humana perante o cosmos e a busca autodestrutiva pelo conhecimento proibido.
* **Tom:** Melancólico, claustrofóbico, geométrico e opressor.

### 2.3 Lore e Contexto do Mundo
Carcosa é uma dimensão onde a física obedece a uma lógica onírica e matemática. Sua arquitetura remete a uma favela corrompida por monumentos de pedra pálida e tentáculos de Hastur. O ambiente flutua na frequência natural de 7.83 Hz.

### 2.4 Personagens Principais
* **Damião (Protagonista):** Jovem morador da Favela do Rato Baleado (Rio de Janeiro), estudante de Direito. Para pagar a faculdade, entrou para o movimento do tráfico local com a ajuda de seu amigo Juninho. Tocados pelo sonho de Hastur, descobrem a seita por trás de uma ONG local (liderada por Natasha). Ao tentar interromper o ritual da seita na igreja para salvar seus amigos e Martha, Damião é capturado e sacrificado — seu ventre é cortado com o símbolo de Hastur. Ao morrer na Terra, acorda nas areias de Carcosa. Sua jornada em Carcosa é uma peregrinação de resgate de memórias e resistência contra a cidade-máquina.
* **O Rei em Amarelo (Antagonista):** Entidade vestida de farrapos amarelos e portando a Máscara Pálida. Sua observação direta destrói a mente humana. É o antagonista final do jogo completo (Fase 6) e também o chefe final do Vertical Slice/demo (Fase 2: Castelo de Carcosa).
* **Rainha Cassilda:** Figura matriarcal de Yhtill, presente no Santuário de Yhtill como NPC de quest principal. Propõe a quest "A Canção Incompleta": coletar os 5 Fragmentos de Yhtill (diários/cartas dos nobres perdidos) espalhados pelo Deserto e pelas Dungeons. Recompensa: Patuá das Luas Gêmeas. Diálogos completos em `lore/cassilda_e_byakhee.md`.
* **Abdul Alhazred:** Autor lendário do *Necronomicon*. Miniboss da Tumba de Alhazred (Dungeon 1, Z9 — Tumba de Abdul). Sua essência persiste nas profundezas da dungeon que carrega seu nome. Design completo da luta em `lore/abdul_alhazred.md`.
* **Nagaraja, o Sacerdote do Sinal Escamado:** Boss 1 do Templo da Serpente (Dungeon 2, Z10). Serpent Person de 2,5m, metade humanoide / metade serpente. Único de sua raça consciente do aprisionamento em Carcosa. Design completo em `lore/templo_da_serpente.md`.
* **O Avatar de Set:** Boss 2 do Templo da Serpente (Dungeon 2, Z11). Fragmento da consciência divina de Set aprisionado por Hastur no Deserto de Hali. Ser colossal de 4m, não-senciente no sentido humano, pura força primordial. Dropa o Elmo de Set (garantido). Design em `lore/templo_da_serpente.md`.
* **O Byakhee:** Miniboss dos Portões das Ruínas (fim da Fase 1). Guardião alado biológico de Carcosa. Luta aérea com grito infrassônico que drena RM passivamente. Dropa o Anel do Sinal Amarelo. Design em `lore/cassilda_e_byakhee.md`.
* **Yug-Neth (Mi-Go Filhote — Companion):** Mi-Go filhote aprisionado por Abdul Alhazred na Zona 9 da Tumba. Libertado por Damião no encontro com Alhazred (em ambos os caminhos). Comunica-se por bioluminescência (pontos dourados no corpo). **É a chave dimensional dos Portões das Ruínas** — o jogador não sabe disso até a cena de chegada aos Portões. Não ataca. Design e mecânicas completas em `lore/migo_companion.md`.

### 2.5 Facções, Criaturas e Raças
* **Irmandade do Sinal Amarelo:** Cultistas que costuraram os próprios olhos e caçam Damião usando apenas a audição.
* **Byakhee (Arautos do Vento Negro):** Sentinelas alados que patrulham de cima e ativam alarmes visuais.
* **A Coisa do Cemitério:** Um ser cadavérico inchado com pele de textura úmida. Imune ao combate físico, ele caça pelo faro e mata Damião ao menor toque.
* **Habitantes de Carcosa (Corte de Yhtill):** Fantasmas estáticos que choram nas esquinas, drenando a RM de quem se aproxima.

### 2.6 Roteiro e Diálogos (Escopo Geral)
* **Diálogos:** Poucos, focados em gravações de áudio (Logs), anotações em terminais e sussurros da Cidade Pálida.
* **Localização:** Textos e legendas em Português do Brasil (idioma nativo) e Inglês.

### 2.7 Progressão Narrativa
O jogo começa com Damião acordando no **Deserto de Hali** — imediatamente após os eventos do curta-metragem *Favela Amarela*. Através da exploração livre do overworld e do drop de itens (incluindo o Mapa Fragmentado que revela o Templo da Serpente), o jogador vai progredindo:

1. **Tumba de Alhazred (Dungeon 1)** — S-Path de 9 Zonas. O ponto de virada interno ocorre na transição Z4→Z5 (queda física, desbloqueio de Arma + Salto). Clímax: derrota de Abdul Alhazred, drop do Necronomicon. A dungeon contém fragmentos de lore que conectam o jogo ao universo audiovisual.
2. **Santuário de Yhtill** — Quest narrativa com a Rainha Cassilda. Conclusão recompensa o Patuá.
3. **Templo da Serpente (Dungeon 2, opcional)** — Dois guardiões: **Naga** (Guardião 1) e **Avatar de Set** (Guardião 2, Deus Egípcio Primordial). O Avatar de Set dropa o **Elmo do Set** sempre garantido, mais uma segunda peça do Set Lendário de forma aleatória. O Naga também dropa uma peça RNG. O set lendário completo é composto por 4 peças: **Elmo, Peço, Pernas e Arma**.
4. **Portões das Ruínas** — Miniboss Byakhee. Drop: Anel do Sinal Amarelo. Abertura dos Portões → transição para a Fase 2 (Castelo de Carcosa).
5. **Castelo de Carcosa (Fase 2)** — Dentro do Castelo, Damião precisa encontrar as peças restantes do Set Lendário ainda não coletadas. Com o set completo, ele tem chance de enfrentar e matar o **Avatar de Nyarlathotep** (sidequest secreta). A quest principal da Fase 2 culmina no confronto com o **Rei em Amarelo**.

---

## 3. MECÂNICAS DE JOGO (GAMEPLAY)

### 3.1 Gameplay Loop
```
[Explorar no Escuro / Dreno de RM] ──▶ [Detectar Cultistas por Som/IA]
                │                                    │
                ▼                                    ▼
       [Esconder-se / Evasão] ──▶ [Esquivar ou Encarar (Combate)]
                │                                    │
                └─────────────────┬──────────────────┘
                                  ▼
                    [Recuperar RM sob Postes de Luz]
```

### 3.2 Mecânicas Core
* **Movimentação Isométrica:** Movimento em 8 direções no plano XY com velocidade adaptada ao estado mental (Focado ganha +10% de velocidade).
* **Furtividade baseada em Som, Faro e Luz:**
  * **Agachar:** Reduz a velocidade, reduz a hitbox física e diminui em 80% o ruído emitido.
  * **Detecção por Faro (Sseth Farejador):** Inimigos farejadores detectam por proximidade em área. Podem ser neutralizados com o *Frasco de Incenso*.
  * **Tempestade de Memória:** O vento forte no Overworld do Deserto abafa 100% o som dos passos.
* **Esquiva Física:** Um desvio rápido que não consome RM, mas tem tempo de recarga (2.5s). *(Nota: O Salto Dimensional foi integralmente removido do jogo).*
* **Companheiro Yug-Neth (Mi-Go Filhote):** Após ser libertado na Tumba de Alhazred, Yug-Neth segue Damião pelo jogo. Não combate, mas oferece efeitos passivos (bioluminescência ilumina 1 unidade de raio; confunde Nagas; revela posição do Byakhee). **Mecânica de Proteção:** Yug-Neth possui uma barra de **Resiliência do Companheiro (RC)**. Se a RC chegar a zero por ataques inimigos → **Colapso do Companheiro = Game Over**. Damião deve se posicionar entre inimigos e Yug-Neth para bloquear projéteis.

### 3.3 Sistema de Combate Físico & Dupla Barra
* **Dupla Barra de Sobrevivência:** Damião possui **Resiliência Mental (RM - 100 max)** para estabilidade psíquica e **Vitalidade Corpórea (Vit - 100 max)** para integridade física. Ataques de armas tiram Vitalidade Corpórea; terror/escuridão tira RM.
* **Mão Física (As 3 Armas Rituais do Baú RNG — Zona 6b):** Damião começa desarmado (golpe de mão vazia com Dano 0). Na Câmara do Baú (Zona 6b), obtém por sorteio RNG 1 de 3 armas rituais:
  1. **Maça de Aklo:** Dano 40, Defesa 30 (mitigação pesada). Habilidade *interrompe a canalização* anômala do boss.
  2. **Estilete de Irem:** Dano 25, Defesa 15 + Sangramento (15/s).
  3. **Alfanje de Alhazred:** Dano 60, Defesa 40 + Habilidade *Golpe do Deserto* (Stun 2.5s).
* **Ficha de Atributos e Mitigação:** Todas as unidades possuem Ficha de Atributos com 5 atributos (Vitalidade, Ataque, Defesa, Conjuração, Resistência Anômala) e mitigação por fórmula subtrativa com piso ($\max(\text{bruto} \times 0.15, \text{bruto} - \text{defesa})$).

### 3.4 Consumíveis Rápidos & Relíquias (Árvore de Itens)
* **Hotbar de Consumíveis Rápidos:** Chá Calmante (+40 RM), Frasco de Incenso (corta faro), Sino de Estática (distração por som 15m), Frasco de Veneno de Yig (-10 Defesa no boss), Fragmento de Yuggoth (+25 RC do companheiro).
* **Relíquias Ancestrais:** O Necronomicon (tradução Aklo), Patuá de Malik Nazinga (linha de conexão ancestral), Patuá das Luas Gêmeas (-40% dreno de RM no escuro), Anel do Sinal Amarelo (-30% detecção de cultistas).
* **Set Lendário de Set (4 Peças):** Elmo, Peitoral, Grevas e Arma de Set. O conjunto completo desbloqueia o confronto contra o Avatar de Nyarlathotep no Castelo.

### 3.4.1 Luta de Aparição Primordial — Abdul Alhazred (Zona 9)
O boss da Dungeon 1 é uma **luta em fases governada pelo Escudo Mágico** — dano só entra
quando o escudo está baixo, então "bater até cair" não funciona. *(Implementado; detalhes em
[`systems/boss_abdul.md`](systems/boss_abdul.md).)*

* **Fase 1 (100% → 35%):** escudo impenetrável sustentado por **Pedras de Poder**, que se
  manifestam na arena quando ele desperta (não ficam plantadas na cripta antes disso).
  Quebrar uma derruba o escudo por alguns segundos — a única janela de dano. Ele invoca
  esqueletos em cadência para pressionar o jogador enquanto ele procura as Pedras.
* **Fase 2 (< 35%):** o escudo vira permanente e as Pedras somem. Ele passa a alternar
  **Cones de Gelo** (3 stacks congelam Damião) e invocações, gastando "mana"; após 3 magias
  a mana esgota, o escudo cai e abre a **janela de exaustão** — o momento do golpe de misericórdia.
* **Imune a crítico de furtividade:** Aparições Primordiais não caem por stealth. A
  furtividade serve para *chegar* até a luta, não para resolvê-la.
* **Escolha antes da luta:** conversar com ele abre uma bifurcação — *lutar* (dropa o
  Necronomicon) ou *concordar* (poupa Abdul, sem Necronomicon). Yug-Neth é libertado nos
  dois caminhos. Atacá-lo depois de poupado **reabre a luta** (traição da trégua).

### 3.5 Economia Interna e Recursos
* **Resiliência Mental (RM):** O único recurso ativo (0.0 a 100.0).
* **Itens Raros:** Consumíveis raros de uso único (ex.: *Chá Calmante* que recupera 40 RM).

### 3.6 Sistemas Secundários
* **Propagação Sonora:** Ruídos emitidos por Damião (passos rápidos, quedas, estática de poderes) propagam ondas circulares físicas na cena que alertam os cultistas se tocarem em seu raio auditivo.

### 3.7 Física e Simulação
* **Física Isométrica 2D:** Todo o jogo roda em física 2D (`Rigidbody2D` com `gravityScale = 0`), simulando altura e profundidade através de remapeamento matemático de eixos e Y-Sorting dinâmico por eixo customizado.

### 3.8 Modos de Jogo
* **Singleplayer:** Campanha principal única.

---

## 4. INTERFACE E EXPERIÊNCIA DO USUÁRIO (UI/UX)

### 4.1 Filosofia de Interface
* **Imersão Absoluta (HUD Diegética):** Minimizar overlays digitais. A tela pisca e perde cor para sinalizar o status mental do jogador.
* **Estilo Monocromático de Terminal:** Menus de pausa, logs e mensagens usam fontes monospace que emulam terminais antigos.

### 4.2 Fluxo de Telas
```
Menu Inicial ──▶ Gameplay (Zonas 1-4) ──▶ Queda (Z4 → Z5) ──▶ Gameplay (Zonas 5-9, Tumba de Alhazred)
   ▲                 │                                             │
   │            RM a 0 (Colapso)                    Drop do Necronomicon ──▶ resto do Deserto de Hali
   └─────────────────┘
```
O encerramento da Zona 5 (fuga do subterrâneo) **não é fim de fase** — é o meio da Tumba de Alhazred (Dungeon 1), que continua até a Zona 9 e o miniboss final da dungeon (2026-07-28: corrigido — antes este diagrama descrevia a Zona 5 como "fim da Fase 1", o que deixou de valer quando a Ruínas Pálidas virou dungeon dentro do Deserto). O fim de fase de verdade é a derrota do Byakhee nos Portões das Ruínas. O único fim de jogo mid-campanha é o **Colapso** (RM a 0), que retorna ao Menu Inicial.

### 4.3 HUD (In-Game)
* Um medidor circular de **Resiliência Mental** muito discreto no canto da tela (sinalizado como batimento de sinal).
* **Barra de Vitalidade Corpórea** (`VitalidadeBar`) — a integridade física de Damião, separada da RM. *(Implementado.)*
* **Barra de Ações** (`BarraDeAcoes`) — slots mostrando a arma equipada e a habilidade dela, com indicador de recarga. Fica vazia enquanto Damião está desarmado. *(Implementado.)*
* **Prompt de Interação** (`PromptDeInteracao`) — aparece quando há algo usável ao alcance, no formato `E — {ação}` (ex.: "E — Abrir o baú"). Some quando não há alvo. *(Implementado.)*
* **Painel de Escolha** (`PainelDeEscolha`) — caixa de diálogo com opções navegáveis (setas + E), usada na conversa com Alhazred. *(Implementado.)*
* Dois pequenos ícones no canto inferior esquerdo indicando o módulo ativo em cada mão.

### 4.3.1 Interação com o Mundo (botão E)
Objetos do mundo são usados por **interação deliberada**, não por encostar: Damião chega
perto, o prompt aparece e **o jogador decide** apertar **E** (ou o botão Norte do gamepad).
Vale para colecionáveis (baú da Tumba, patuá) e para conversar com NPCs (Alhazred).
Gatilhos de *travessia* (transição de cena, zonas de tempestade, dicas de tutorial)
continuam automáticos de propósito — são eventos de passagem, não objetos que se "usa".
Detalhes em [`systems/interacao.md`](systems/interacao.md). *(Implementado.)*

### 4.4 Menus e Painéis
* **Diário de Tradução (OKF Logs):** Aba onde Damião lê documentos traduzidos e anota pistas para entender a Cidade Pálida.

---

## 5. CONTEÚDO E LEVEL DESIGN

### 5.1 Estrutura de Progressão do Jogo (S-Path)
As **"Ruínas Pálidas"** — hoje a **Tumba de Alhazred, Dungeon 1 da Fase 1 (Deserto de Hali)**, ver §1.8 — são construídas em formato de S. O blockout real (`LevelBlockoutPlanner.cs`) já tem **9 Zonas**; as 5 primeiras (documentadas abaixo) cobrem o trecho de stealth urbano até a queda para o subterrâneo:

```
[Zona 1: Rua Entrada] ──▶ [Zona 2: Vila das Casas]
                                  │
                                  ▼
[Zona 4: Praça Cerco] ◀── [Zona 3: Beco do Vento]
         │ (Queda/Colapso)
         ▼
[Zona 5: Ruínas Subterrâneas]
```

### 5.2 Métricas de Construção (Level Design Metrics)
* **PPU (Pixels Per Unit):** 32 (2026-07-28: padrão único do projeto; ver `.claude/skills/favela-isometric-standards/SKILL.md`). Arte legada importada a 16 PPU é reimportada a 32 quando for tocada, não em massa.
* **Corredores:** Largura padrão de 3.5 a 4.0 unidades para permitir movimentação livre e desvios de cultistas.

### 5.3 Catálogo de Assets
* **Ambientação:** Ruínas de favela com alvenaria deteriorada, pedras cinzentas e vegetação murcha bioluminescente.
* **Linguagem Visual:** Postes de luz amarela atuam como faróis direcionando o caminho seguro do jogador.

### 5.4 Lista de Fases / Regiões

#### Dungeon 1 — Tumba de Alhazred (Zonas 1-9, S-Path)
> Implementado em `Assets/Scenes/Playtest_RuinasPalidas.unity`. Zonas 1-5 documentadas abaixo; Z6-9 implementadas mas pendentes de documentação textual.

* **Zona 1 (Rua de Entrada):** Trecho de subida com 2 Cultistas Errantes. Introdução aos postes de luz. **[Fragmento de Yhtill nº1 não está aqui — está no Overworld: Garganta de Pedra Pálida]**
* **Zona 2 (Vila das Casas):** Área ampla com 3 casas modulares. **Fragmento de Yhtill nº2 (Lord Morthis)** escondido sob soleira.
* **Zona 3 (Beco do Vento):** Vielas estreitas com vento que abafa passos.
* **Zona 4 (Praça do Cerco):** Arena fechada, chão desaba → queda para Zona 5.
* **Zona 5 (Subterrâneo - Ruínas de Hali):** Terror puro, sem luz. Contém o **patuá** — item cujo efeito foi revisto e está **pendente de definição** (não destrava mais o Salto Dimensional, removido do jogo).
* **Zona 6b (Câmara do Baú):** Sala lateral a Leste da Z6, baú sorteia uma das três armas. **Fragmento de Yhtill nº3 (Lady Vaine)** encontrado aqui.
* **Zona 9 (Tumba de Abdul):** Encontro com Alhazred — **Cena de Escolha** (concordar = sem luta + sem Necronomicon; recusar = Boss Fight + Necronomicon + mecânica de proteção de Yug-Neth). Yug-Neth libertado em ambos os caminhos e se torna companion.

#### Deserto Aberto (Overworld)
* **Garganta de Pedra Pálida (área inicial):** **Fragmento de Yhtill nº1 (Lady Seraphel)** em fenda de pedra.
* **Santuário de Yhtill:** Rainha Cassilda — Quest "A Canção Incompleta" (5 Fragmentos). Diálogos em visual novel style. Recompensa: Patuá das Luas Gêmeas.
* **Portões das Ruínas:** Boss Byakhee. Drop: Anel do Sinal Amarelo. Abertura dos Portões = transição para Fase 2.

#### Dungeon 2 — Templo da Serpente (Zonas 1-12, Semi-obrigatória)
> Novo conteúdo para o Vertical Slice (2026-07-30). Design completo em `lore/templo_da_serpente.md`.

* **Z1 (Átrio das Escamas):** Chão de escamas que amplifica som; introdução ao Sseth Farejador.
* **Z2 (Corredor dos Glifos):** Lore dos Serpent People em afrescos. **Fragmento de Yhtill nº4 (Lord Aldaron I)** atrás de painel falso.
* **Z3 (Câmara dos Sseth):** 4 Sseth Comuns + 1 Farejador. Fogueira ritual de luz maligna.
* **Z4 (Salão dos Nagas):** 2 Nagas Guerreiros (elite). Estátuas de Set como cobertura. **Fragmento de Yhtill nº5 (Lord Aldaron II)** no pedestal.
* **Z5 (O Poço de Yig):** Larvas de Yig. Mecânica de distração com objetos no poço.
* **Z6 (Câmara do Veneno):** Névoa tóxica + 1 Espectro Escamado. ⚠️ *Design pendente de redesenho: dependia do Salto Dimensional como ferramenta de travessia, removido do jogo.*
* **Z7 (Cripta das Larvas):** Ovos de Yig sensíveis a vibração. Stealth absoluto. Drop RNG: Peitoral de Set.
* **Z8 (Galeria das Visões de Set):** Espelhos de obsidiana + 2 Sseth. Reflexos de Set.
* **Z9 (Santuário do Olho):** Cristal pulsante de Set. Ponto de Ancoragem (único do Templo).
* **Z10 (Câmara do Trono):** ★ **BOSS 1: Nagaraja, o Sacerdote do Sinal Escamado** ★ Drop: 1 peça RNG do Set Lendário.
* **Z11 (A Fenda de Set):** Zona de transição. ★ **BOSS 2: O Avatar de Set** ★ Drop: Elmo de Set (garantido) + 1 peça RNG.
* **Z12 (Saída — O Deserto Coberto de Escamas):** Retorno ao Overworld. Cena narrativa do Diário de Damião.

---

## 6. ARTE

### 6.1 Direção de Arte
* **Estilo Visual:** Pixel art 2D de alta resolução (64x64px por tile) com shaders de iluminação dinâmica (mapa de normais) e efeito scanline de monitor CRT.
* **Paleta de Cores:** Cinza pálido, verde ácido, ferrugem profunda e preto abissal, contrastados pela luz quente amarela dos refúgios.

### 6.2 Efeitos Visuais (VFX)
* **Shaders de Glitch:** Estática e distorção na tela que se intensificam quando Damião está no escuro ou perto de anomalias.
* **Partículas de Vento:** Poeira e cinzas soprando lateralmente nas zonas externas.

---

## 7. ÁUDIO

### 7.1 Direção Sonora
* **Identidade:** Minimalista, industrial e ambiental. Predomínio de sons analógicos de fita magnética e sintetizadores distorcidos.

### 7.2 Música Adaptativa
* **Estados Musicais:**
  * *Exploração:* Drone silencioso e tenso.
  * *Alerta:* Batidas sutis de percussão industrial.
  * *Caça:* Música agressiva, distorcida e opressora.

### 7.3 Efeitos Sonoros (SFX)
* Ruído físico de passos variando conforme a velocidade (agachado = silêncio, corrida = barulho alto).
* Impacto de arma na carne e no gesso da máscara (Mão Física); estilhaço das Pedras de Poder.

---

## 8. TECNOLOGIA E ENGENHARIA

### 8.1 Engine e Ferramentas
* **Engine:** Unity 6000.4.4f1.
* **Controle de Versão:** Git.

### 8.2 Arquitetura de Software (POCO vs MonoBehaviour)
* **Regra de Ouro:** Toda a lógica matemática, FSM de inimigos, contadores de sanidade e mecânicas de stealth rodam em C# puro (POCO) no namespace `FavelaAmarela.Core.*`, sem herdar de MonoBehaviour.
* **Adaptação:** Os scripts da Unity em `FavelaAmarela.Runtime.*` servem apenas para instanciar, ler input e aplicar feedback visual/físico aos POCOs correspondentes.

### 8.3 Salvamento e Persistência
* Salvamento de progresso realizado via serialização JSON de classes POCO nos postes de luz (Refúgios).

### 8.4 Performance e Otimização
* Limitação de alocação de memória (zero Garbage Collection) no loop principal de física e updates.

---

## 11. TESTES E GARANTIA DE QUALIDADE (QA)

### 11.1 Estratégia de Testes
* **Testes EditMode (NUnit):** Toda a lógica do Core (como a resiliência mental e a máquina de estados do cultista) possui testes automáticos rodando em CLI (`dotnet test`), testando as lógicas de negócio sem abrir a Unity.

### 11.2 Critérios de Aceitação
* Uma feature só é considerada concluída se passar em 100% dos testes automáticos de qualidade (EditMode) e compilar sem avisos.

---

## 14. APÊNDICES

### A – Glossário Diegético
* **HP:** Resiliência Mental (RM).
* **Dano de HP:** Trauma.
* **Game Over:** Colapso Mental.
* **Vitória / "You Win":** Não se aplica (RPG, não roguelike). Fim de fase é **Transição**; a vitória é o desfecho da história, ao fim da Fase 6.
* **Posto de Cura:** Poste de Luz / Refúgio.
* **Inimigo:** Cultista Amarelo.
* **Dash:** Esquiva (movimento físico, sem custo de RM).

### B – Referências
* *The King in Yellow* (Livro de Robert W. Chambers).
* *Dishonored* (Furtividade vertical e habilidades de travessia).
* *Death Trash* (Estilo visual visceral e tom maduro).
