---
type: Lore Reference
title: Rainha Cassilda, Os Fragmentos de Yhtill e O Byakhee
description: Design completo de Cassilda (5 Fragmentos de Yhtill), diálogos em caixa visual novel, e o boss Byakhee — ver §I.5 para como o implementado diverge (3 fragmentos + recital).
tags: [lore, npc, quest, cassilda, byakhee, fragmentos-yhtill, dialogos, recital]
timestamp: 2026-07-30T11:00:00-03:00
---

# Rainha Cassilda, Os Fragmentos de Yhtill e O Byakhee

---

## I. Rainha Cassilda — Perfil Narrativo

**Nome completo:** Cassilda, Rainha de Yhtill, Guardiã da Segunda Máscara
**Localização:** Santuário de Yhtill — ponto fixo no setor norte do Deserto de Hali, próximo ao Lago de Hali.

**Aparência:** Mulher de meia-idade com traços de nobreza fossilizada. Cabelos prateados presos num arranjo de contas de ônix. Veste trajes anacrônicos de Yhtill (tecido amarelo-pálido bordado em preto). Os olhos têm um brilho suave, não de loucura — mas de *resignação profunda*. Ela não está corrompida pelo Sinal Amarelo; está *presa* em Carcosa assim como Damião.

**Tom de voz:** Solene, melancólico, poético. Fala em orações longas e imagens. Nunca usa termos genéricos de RPG. Não tem pressa.

**Canção de Cassilda:** Ela recita fragmentos da canção original de *O Rei em Amarelo* (Robert W. Chambers) ao longo dos diálogos. Em pt-BR adaptado:
> *"Sombras longas nas luas de Carcosa / Luas gêmeas que os sóis desconhecem / O rei parte, a canção permanece / Nas tarjas negras que o vento tece."*

**Lore da personagem:** Cassilda é a rainha da peça maldita *O Rei em Amarelo*. Em Carcosa, ela existe em estado de vigília permanente — não dorme, não morre, aguarda. Ela perdeu os nobres de sua corte que foram explorar o Deserto e as Dungeons em busca de uma saída. Esses nobres deixaram fragmentos de seus diários espalhados pelo caminho. Cassilda sabe que estão lá; apenas não pode ir buscá-los (ela não pode deixar o Santuário — a geometria de Carcosa a prende ao ponto).

---

## I.5. Divergência do implementado (2026-08-02)

> Regra de conflito do `CLAUDE.md` §3.1: o **código é a verdade para como a quest funciona
> hoje**; o resto deste capítulo (5 fragmentos, diálogos 1–5) é a **verdade de design** — o
> roteiro completo, que volta a valer quando o Templo da Serpente existir. Detalhes de
> implementação atual em [systems/quest_cassilda.md](../systems/quest_cassilda.md).

O que está **realmente em jogo** difere deste capítulo em três pontos, todos decisão do
Vini em 2026-08-02:

1. **3 fragmentos, não 5.** As estrofes 1 e 2 da Canção de Cassilda foram redistribuídas
   nos diários de Seraphel, Morthis e Vaine (não há fragmento próprio do Aldaron ainda —
   ele segue desaparecido, citado mas não documentado).
2. **Um recital fecha a quest**, não a simples entrega. Depois dos 3 fragmentos, Cassilda
   pede as estrofes 3 e 4 — ela **esqueceu** as palavras depois de eras cantando, mas
   **reconhece** a resposta certa. Cada estrofe é uma escolha entre 3 versões (uma certa,
   duas erradas por *tom*, não por detalhe decorável). Errar não tem custo mecânico.
3. **A "Canção de Cassilda adaptada"** citada no perfil dela (§I, abaixo) e o poema de
   Chambers usado no recital **não são a mesma coisa** — a adaptada é o que ela cantarola
   sozinha, gasta, antes da quest; o poema de Chambers é o que Damião precisa completar.
   Ela vira, narrativamente, a prova audível de que a rainha perdeu a canção.

## II. A Quest: Os Fragmentos de Yhtill

**Nome da quest (in-game):** "A Canção Incompleta"
**Tipo:** Coleção e entrega (5 fragmentos)
**Iniciada:** Automaticamente ao entrar no Santuário de Yhtill pela primeira vez.
**Recompensa:** Patuá das Luas Gêmeas

### Mapa de Localização dos 5 Fragmentos

| # | Nome do Fragmento | Localização | Zona |
| :- | :--- | :--- | :--- |
| 1 | **Diário de Lady Seraphel** | Deserto Aberto — Garganta de Pedra Pálida | Overworld (início) |
| 2 | **Anotações de Lord Morthis** | Tumba de Alhazred — Z2 (Vila das Casas) | Dungeon 1, Z2 |
| 3 | **Carta de Lady Vaine** | Tumba de Alhazred — Z6b (Câmara do Baú) | Dungeon 1, Z6b |
| 4 | **Crônica de Lord Aldaron (I)** | Templo da Serpente — Z2 (Corredor dos Glifos) | Dungeon 2, Z2 |
| 5 | **Crônica de Lord Aldaron (II)** | Templo da Serpente — Z4 (Salão dos Nagas) | Dungeon 2, Z4 |

> **Nota de design:** Os fragmentos 4 e 5 estão na Dungeon 2 (Templo da Serpente, semi-obrigatória). O jogador pode completar a quest com apenas os 3 primeiros e retornar depois. Cassilda aceita entrega parcial mas só dá o Patuá com os 5.

---

### Conteúdo Narrativo de Cada Fragmento

#### Fragmento 1 — Diário de Lady Seraphel
*Encontrado no Overworld, numa pedra rachada logo na Garganta de Pedra Pálida (área inicial).*

> **[Diário de Lady Seraphel — 3ª Lua de Carcosa]**
>
> *Acordei no deserto sem lembrar de como cheguei aqui. Cassilda disse que exploraríamos o Deserto de Hali em busca de passagem para fora. Somos quatro: eu, Morthis, Vaine e Aldaron. A rainha ficou no Santuário — diz que não pode partir.*
>
> *O deserto cheira a cinzas e a algo que não consigo nomear. Escrevo isto para que alguém saiba que partimos.*
> — *Lady Seraphel, nobreza de Yhtill*

---

#### Fragmento 2 — Anotações de Lord Morthis
*Encontrado na Tumba de Alhazred, Zona 2 (Vila das Casas), escondido sob uma soleira de porta.*

> **[Anotações de Lord Morthis — sem data]**
>
> *Perdemos Seraphel na entrada. Não estava morta — simplesmente não estava mais. Carcosa faz isso. A geometria do lugar engole quem não presta atenção.*
>
> *Entramos numa tumba que cheira a giz e a estática. Os seres daqui são cegos e caçam pelo som. Aprendi a andar devagar. Vaine está bem. Aldaron não fala mais muito.*
>
> *Se alguém encontrar isto: vá devagar. O silêncio é a única moeda que tem valor aqui.*
> — *Lord Morthis de Yhtill*

---

#### Fragmento 3 — Carta de Lady Vaine
*Encontrado na Z6b (Câmara do Baú) da Tumba de Alhazred.*

> **[Carta de Lady Vaine — para Cassilda, que talvez nunca leia]**
>
> *Rainha, encontramos as câmaras mais profundas da Tumba. Há algo aqui — uma presença que sussurra em Aklo. Aldaron diz que é o autor do tomo proibido. Eu prefiro não saber o nome.*
>
> *Morthis não acordou esta manhã. Sua forma simplesmente desapareceu enquanto dormia, como Seraphel. Estou sozinha com Aldaron agora.*
>
> *Ele quer ir mais fundo. Achar o Templo das Escamas. Não consigo dissuadi-lo.*
>
> *Se você conseguir o Patuá antes de mim, rainha — guarde. Talvez alguém precisará mais do que eu.*
> — *Lady Vaine de Yhtill*

---

#### Fragmento 4 — Crônica de Lord Aldaron (I)
*Encontrado no Templo da Serpente, Z2 (Corredor dos Glifos), atrás de um painel falso.*

> **[Crônica de Lord Aldaron — Entrada no Templo das Escamas]**
>
> *...o corredor não tem fim visível. As gravuras me dizem que estas bestas eram reis antes de sermos primatas. Carrego o terceiro anel de Yhtill e rezo para que Cassilda nos encontre antes que a tempestade...*
>
> *Vaine ficou na entrada do Templo. Disse que esperaria. Não a culpo.*
>
> *As serpentes aqui têm olhos de pedra e movem-se com propósito ancestral. Não são loucas como os cultistas — são piores. São corretas.*
> — *Lord Aldaron de Yhtill*

---

#### Fragmento 5 — Crônica de Lord Aldaron (II)
*Encontrado no Templo da Serpente, Z4 (Salão dos Nagas), no pedestal de uma estátua de Set.*

> **[Crônica de Lord Aldaron — Última Entrada]**
>
> *Esta é a última anotação que consigo fazer. Sinto que o Avatar me observa.*
>
> *Cassilda disse que as luas testemunhariam nosso retorno — mas as luas de Carcosa não piscam para nós. Que ela encontre estas páginas. Que ela saiba que viemos até o fim.*
>
> *O Sacerdote de Escamas me ofereceu uma escolha que não compreendi. Agora compreendo. Escolhi errado.*
>
> *Não procurem Aldaron de Yhtill mais.*
> — *[última linha escrita em Aklo, indecifrável mesmo com o Necronomicon]*

---

## III. Diálogos de Cassilda (Visual Novel Style)

*Formato: caixa de texto na parte inferior da tela, retrato do personagem à esquerda. Música de fundo: drone suave + cordas.*

---

### DIÁLOGO 1 — Primeiro Encontro (Automático ao entrar no Santuário)

> **[CASSILDA]** *(voz melancólica, olhando para o horizonte)*
> "Você cheira a Hali, forasteiro. E a algo mais — a morte recente. Bem-vindo ao Santuário de Yhtill. Que reste ainda um santuário para ser chamado assim."

> **[DAMIÃO]** *(escolha do jogador)*
> A) "Onde estou?"
> B) "Você está presa aqui?"
> C) *[Silêncio — apenas observa]*

*(Resposta A)*
> **[CASSILDA]**
> "No coração de Carcosa, onde os sóis gêmeos esqueceram como se pôr. Este é o Santuário de Yhtill — o que resta da corte do Rei em Amarelo antes que ele deixasse de ser apenas um personagem de peça e passasse a ser um fato."

*(Resposta B)*
> **[CASSILDA]**
> "Presa. Sim. Essa palavra serve. A geometria de Carcosa tem predileção por ironias: a rainha que fundou este santuário não pode sair dele. Mas meus nobres partiram. Foram buscar fragmentos de uma saída que não existe. Ou talvez existe — simplesmente não voltaram para me contar."

*(Resposta C)*
> **[CASSILDA]** *(levemente divertida)*
> "Silêncio. Isso é raro aqui. A maioria dos que chegam aqui ou gritam ou choram. Sente-se, forasteiro. Ou não. A geometria de Carcosa não se importa com sua postura."

---

### DIÁLOGO 2 — Abertura da Quest

*(Após a saudação, Cassilda propõe a quest)*

> **[CASSILDA]**
> "Tenho um pedido — não uma ordem, não tenho mais autoridade para ordens. Um pedido. Meus nobres partiram há... tempo não mensurável, aqui. Eles escreviam diários, anotações, cartas. Fragmentos de nossas vidas antes de Carcosa. Estão espalhados pelo deserto e pelas ruínas que eles exploraram."

> **[CASSILDA]**
> "Não peço que os traga de volta. Isso está além do possível de qualquer tradutor. Mas seus escritos... traga-os. Cinco fragmentos. Para que eu saiba o que aconteceu com cada um deles. Que eu possa cantar a canção de cada nome deles direito."

> **[DAMIÃO]** *(escolha)*
> A) "Farei isso."
> B) "Por que eu faria isso por você?"

*(Resposta B)*
> **[CASSILDA]**
> "Porque você precisa de algo que eu tenho, e sabe disso — mesmo que ainda não saiba o que é. O Patuá das Luas Gêmeas. Uma bênção tecida na noite de Carcosa que desacelera o apagamento que o escuro causa em mentes humanas. Você vai precisar, forasteiro. O Deserto não é gentil com os não-preparados."
> *(pausa)*
> "E porque, no fundo, você é o tipo de pessoa que faz estas coisas. Eu reconheço o tipo."

*(Aceitar quest)*
> **[CASSILDA]**
> "A primeira página está perto. Na Garganta de Pedra Pálida, onde o vento é alto e a pedra racha. Lady Seraphel sempre escrevia em lugares onde o vento não poderia apagar a tinta. Procure nas fendas."

---

### DIÁLOGO 3 — Entrega Parcial (1-4 Fragmentos)

*(Ao retornar com 1 a 4 fragmentos — Cassilda aceita ver mas aguarda os demais)*

> **[CASSILDA]** *(ao receber o fragmento de Seraphel)*
> "Seraphel... Ela sempre escrevia rápido, como se tivesse medo de esquecer. Está bem, forasteiro. Ela foi, como todos nós eventualmente vamos — só que ela foi antes. Obrigada por trazer a letra dela de volta para mim."
> *(pausa)*
> "Ainda faltam ${N_RESTANTES}. Quando você os tiver, volte."

*(Ao receber o fragmento de Morthis)*
> **[CASSILDA]**
> "Morthis. Ele era prático acima de qualquer coisa. 'Andar devagar.' Sim. Era o único conselho que ele tinha. Funciona, de certa forma."
> "Continua, forasteiro."

*(Ao receber o fragmento de Vaine)*
> **[CASSILDA]** *(voz levemente alterada — mais sombria)*
> "Vaine. Ela não era obrigada a ir. Era a mais jovem da corte. Eu deveria tê-la impedido."
> *(olha para o horizonte)*
> "Não a culpe, se a encontrar. Ela não escolheu mal. Ela simplesmente... escolheu seguir."

---

### DIÁLOGO 4 — Entrega dos Fragmentos de Aldaron (4 e 5)

*(Cassilda percebe que os fragmentos vêm do Templo da Serpente)*

> **[CASSILDA]** *(ao receber o fragmento 4)*
> "O Corredor dos Glifos do Templo das Escamas. Aldaron era o único entre nós que lia Aklo. Devia ser ele a traduzir o que as serpentes escreveram nas pedras."
> *(uma pausa longa)*
> "Ele entrou no Templo sabendo o que encontraria. Você sobreviveu ao que ele não sobreviveu. Isso não diz nada sobre seu valor — diz apenas que você teve sorte diferente."

*(Ao receber o fragmento 5 — o último)*
> **[CASSILDA]**
> "A última entrada. 'Escolhi errado.' Aldaron nunca errava — ele calculava. Mas o Sacerdote de Escamas fazia perguntas que não tinham respostas corretas. Apenas consequências."
> *(fecha os olhos por um momento)*
> "Cinco fragmentos. Cinco nomes. Posso cantá-los agora. Você fez algo que nenhuma geometria de Carcosa poderia fazer — devolveu a mim o direito de chorar os meus nobres com nome."

---

### DIÁLOGO 5 — Conclusão e Entrega do Patuá

*(Cassilda recita a Canção de Cassilda adaptada, depois entrega o Patuá)*

> **[CASSILDA]** *(de olhos fechados, recitando)*
> *"Nas luas que não piscam, Seraphel se foi rápida.*
> *Morthis pisou devagar até não mais poder.*
> *Vaine enviou uma carta que o vento não respondeu.*
> *Aldaron leu o que não devia e escolheu o que não havia.*
> *Que as quatro sombras descansem na areia de Hali.*
> *Que a rainha cante seus nomes até o fim do que resta chamar de tempo."*

> **[CASSILDA]** *(abre os olhos, estende o Patuá)*
> "Tome. O Patuá das Luas Gêmeas. Feito com fios das vestes de Yhtill e bênçãos das duas luas que iluminaram nossa corte quando ainda havia corte."
> "Ele desacelera o que o escuro faz com a sua mente. O escuro de Carcosa corrói a Resiliência Mental — com o Patuá, o processo é mais lento. Use as pausas que ele lhe dá."

> **[CASSILDA]** *(enquanto Damião guarda o Patuá)*
> "Uma última coisa, forasteiro. Os Portões das Ruínas — ao norte. Há algo lá que não deixa ninguém passar. Não é uma pessoa. É um instinto com asas."
> "Boa sorte."

---

## IV. O Byakhee — Design Completo do Boss Fight

**Localização:** Portões das Ruínas — extremo norte do Deserto de Hali, entrada do Castelo de Carcosa.

**Papel narrativo:** O Byakhee não é um ser individual com agenda própria — é uma **força biológica de Carcosa** que o Rei em Amarelo usa como selo. Os Portões das Ruínas separam o Deserto do Castelo; o Byakhee é o cadeado vivo desse limiar.

**Visual:** 3,5 metros de envergadura. Corpo híbrido: torso de gralha enorme, asas membranosas de morcego, pernas de inseto-fêmur com garras de obsidiana. Rosto que mistura bico de rapinante com mandíbulas de formiga. Olhos compostos amarelos que refletem as duas luas de Carcosa. Emite um grito infrassônico que Damião ouve no peito, não nos ouvidos.

---

### Cena de Abertura (Antes do Boss Fight)

*Damião se aproxima dos Portões. As portas estão fechadas — pedra maciça de 4 metros. Acima, no topo do arco, algo se move.*

> *O grito vem antes da forma. Um som que não é som — é pressão no crânio, é a frequência errada de Carcosa amplificada. O Byakhee desce dos Portões com asas que bloqueiam os dois sóis.*
> *Não há diálogo. Há apenas o grito e a sombra.*

---

### Atributos e Mecânicas do Boss Fight

**Arena:** Espaço aberto em frente aos Portões (6x6 unidades). Sem obstáculos iniciais, mas o Byakhee cria obstáculos dinâmicos com ataques.

**Grito Infrassônico:** Passivo — Damião perde 2 RM/segundo enquanto o Byakhee estiver vivo, sem necessidade de ser atingido. Representa o terror cósmico da presença da criatura.

**Vulnerabilidade:** O Byakhee é vulnerável apenas durante os 2 segundos de **pouso na arena**. No ar, é imune.

### Padrões de Ataque (3 fases)

**Fase 1 (100% → 60%)**
| Ataque | Descrição | Defesa |
| :--- | :--- | :--- |
| **Rasante Simples** | Voa em linha reta de ponta a ponta da arena | Desviar lateral |
| **Mergulho de Garras** | Desce verticalmente sobre Damião | Esquiva (cooldown 2.5s) |
| **Pouso Agressivo** | Pousa, ataca 2x com garras, volta a voar | Usar os 2s de pouso para atacar |

**Fase 2 (60% → 30%)**
- O Byakhee ganha velocidade. O Rasante agora é em zigue-zague (imprevisível).
- **Novo ataque — Grito Direcionado:** Emite um cone de pressão sonora que causa 20 de Trauma. O cone é telegrafado por 1 segundo (o Byakhee aponta o bico para Damião antes de emitir).
- O pouso dura apenas 1.5 segundo nesta fase.

**Fase 3 (30% → 0%)**
- O Byakhee começa a **circundar a arena voando** sem pousar. Para forçar o pouso: Damião deve usar a **Lâmina do Sinal** (se equipada) para cortar uma das asas durante um rasante. A asa não cai, mas o Byakhee pousa de dor por 3 segundos — janela de dano ampliada.
- **Sem a Lâmina do Sinal:** O pouso ocorre espontaneamente a cada 30 segundos nesta fase (menor frequência).
- **Frenesi final (abaixo de 10%):** O Byakhee emite um grito longo que drena 5 RM/segundo. Damião deve interrompê-lo com um golpe (mesma mecânica de Nagaraja).

---

### Morte e Drop

*Ao morrer, o Byakhee cai pesadamente no chão, as asas se dobram ao contrário. O grito para — o silêncio é físico.*

> *Os Portões das Ruínas tremem. Depois, devagar, começam a se abrir.*
> *Do outro lado: o horizonte do Castelo de Carcosa.*
> *Damião respira. O Patuá pulsa no bolso.*

**Drop:** **Anel do Sinal Amarelo** — gravação sacra que reduz a taxa de detecção de sentinelas e desacelera o dreno de RM causado por entidades com o Sinal.

**Cena final da Fase 1:**
> *Damião avança para os Portões abertos. Lá atrás, o Deserto de Hali fica menor. Lá na frente, as torres do Castelo de Carcosa crescem.*
> *Ele não sente que venceu. Sente que a próxima parte começou.*
