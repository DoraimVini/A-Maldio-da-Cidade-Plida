---
type: GDD
title: Documento de Design Mestre (GDD)
description: Documento de Design de Jogo oficial e unificado para A Maldição da Cidade Pálida (Favela Amarela).
tags: [design, gdd, carcosa, core-vision]
timestamp: 2026-07-10T15:00:00Z
---

# A Maldição da Cidade Pálida (Favela Amarela)
## Documento de Design Mestre (GDD) — Versão 1.0

* **Data:** 10/07/2026
* **Autor(es):** Vinícius (Vini) — Lead Designer & Developer / Claude & Antigravity (Assistentes)
* **Status:** Aprovado para Implementação
* **Confidencialidade:** Interna (Estúdio)

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
  * *Após 1 hora:* Adaptação e cálculo, usando o Salto Dimensional com cautela para resolver o layout das vielas.
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
* **Damião (Protagonista):** Personagem central do universo *Favela Amarela* (Richard Abelha). Historiador e linguista, físico frágil, obcecado por escrituras antigas. Morre na Terra durante os eventos do curta-metragem e acorda em Carcosa no início do jogo. Possui uma sensibilidade anômala única que o permite interagir com a estática de Carcosa — o que o torna simultaneamente um alvo e um agente capaz de escapar.
* **O Rei em Amarelo (Antagonista):** Entidade vestida de farrapos amarelos e portando a Máscara Pálida. Sua observação direta destrói a mente humana. É o antagonista final do jogo completo (Fase 6) e também o chefe final do Vertical Slice/demo (Fase 2: Castelo de Carcosa).
* **Rainha Cassilda:** Figura matriarcal de Yhtill, presente no Santuário de Yhtill como NPC de quest. Entrega o Patuá como conclusão do seu puzzle narrativo. Não é uma inimiga.
* **Abdul Alhazred:** Autor lendário do *Necronomicon*. Miniboss da Tumba de Alhazred (Dungeon 1). Sua essência persiste nas profundezas da dungeon que carrega seu nome.

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
       [Esconder-se / Evasão] ──▶ [Usar Salto Dimensional (Custa RM)]
                │                                    │
                └─────────────────┬──────────────────┘
                                  ▼
                    [Recuperar RM sob Postes de Luz]
```

### 3.2 Mecânicas Core
* **Movimentação Isométrica:** Movimento em 8 direções no plano XY com velocidade adaptada ao estado mental (Focado ganha +10% de velocidade).
* **Furtividade baseada em Som e Luz:**
  * **Agachar:** Reduz a velocidade, reduz a hitbox física e diminui o atrito/ruído emitido.
  * **Sombra física:** Ficar em áreas não iluminadas oculta o jogador, mas drena a RM.
* **Esquiva Física:** Um desvio rápido que não consome RM, mas tem tempo de recarga (2.5s).
* **Salto Dimensional (Ghost Dash):** Teleporte de curta distância que atravessa paredes e obstáculos, custando RM e emitindo estática sonora.

### 3.3 Sistema de Combate (Detalhado)
* **Combate Físico (Mão Física):** Damião pode desferir golpes usando a **Barra Enferrujada** (35% de chance de atordoar por golpe). O atordoamento (Stun) interrompe o cultista e dá tempo para Damião fugir e voltar ao stealth.
* **Inexistência de HP:** Não há barra de vida. Ataques inimigos drenam diretamente a *Resiliência Mental* (10 a 25 RM). Se a RM chegar a zero, ocorre o **Colapso** (Game Over).

### 3.4 Progressão e RPG (Slots de Equipamento)
* **Equipamento Minimalista:** Sem árvores de talentos complexas. A progressão ocorre pela troca de módulos nos 2 slots principais:
  * **Mão Física:** Barra Enferrujada (Stun) ou Lâmina do Sinal (bônus por trás).
  * **Mão Anômala:** Salto Dimensional (Dash) ou Talismã do Vento Negro (Empurrão).
* **Gating de Entrada:** Damião começa o jogo desarmado e sem poderes. Ambos são desbloqueados simultaneamente no início da Zona 5.

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
* Dois pequenos ícones no canto inferior esquerdo indicando o módulo ativo em cada mão.

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
> **Lista parcial (Zonas 1-5).** O blockout real já tem Zonas 6-9 (`Zona6_CriptaDosPrimeiros`, `Zona7_FendaDosSussurros`, `Zona8_Ossario`, `Zona9_TronoDoVulto` — arena do miniboss final da dungeon) implementadas em `Assets/Scenes/Playtest_RuinasPalidas.unity`, ainda não documentadas aqui. Sinalizado em 2026-07-28, não preenchido nesta rodada (é levantamento de conteúdo, não reconciliação de estrutura).

* **Zona 1 (Rua de Entrada):** Trecho de subida com 2 cultistas errantes. Introdução aos postes de luz.
* **Zona 2 (Vila das Casas):** Área ampla com 3 casas modulares que o jogador pode adentrar para contornar patrulhas.
* **Zona 3 (Beco do Vento):** Vielas estreitas com vento forte que desacelera o movimento mas abafa 100% o barulho dos passos.
* **Zona 4 (Praça do Cerco):** Ponto de arena fechada. O jogador é cercado por cultistas e o chão desaba sob seus pés.
* **Zona 5 (Subterrâneo - Ruínas de Hali):** Zona de terror puro. Sem luz. O jogador encontra a Barra Enferrujada e o Salto para conseguir escapar.

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
* Som de estática elétrica do Salto Dimensional.

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
* **Dash:** Salto Dimensional.

### B – Referências
* *The King in Yellow* (Livro de Robert W. Chambers).
* *Dishonored* (Furtividade vertical e habilidades de travessia).
* *Death Trash* (Estilo visual visceral e tom maduro).
