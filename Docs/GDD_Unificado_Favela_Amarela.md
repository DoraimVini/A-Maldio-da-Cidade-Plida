# A MALDIÇÃO DA CIDADE PÁLIDA (FAVELA AMARELA)

## Documento de Design de Jogo Mestre Unificado (GDD Versão 3.0 — Devlog & Canon Atualizados)

---

### INFORMAÇÕES DE PRODUÇÃO E UNIVERSO TRANSMÍDIA

- **Título do Jogo:** A Maldição da Cidade Pálida (Favela Amarela)
- **Universo Transmídia:** _Favela Amarela_ (Curta-Metragem, Animação _"O Peregrino Amarelo"_, HQ e Longa-Metragem)
- **Criador / Lead Designer:** Vinícius (Vini), Tiago Magalhães Rosa (Tiago Tuchu/Gutux) & Nicolas Lobato (Nico)
- **Gênero:** RPG de Terror Cósmico Isométrico 2D / Survival Furtivo Narrativo
- **Motor:** Unity 6.4 (2D Isometric 32 PPU)
- **Target de Produção:** Vertical Slice para Edital (Fase 1: O Deserto de Hali + Fase 2: O Castelo de Carcosa)
- **Data da Última Revisão:** 30/07/2026 (Sincronizado com Devlog e Código)

---

## 1. VISÃO GERAL NARRATIVA E PILARES DE DESIGN

### 1.1 Premissa Narrativa Canônica (Favela Amarela)

O protagonista **Damião** é um jovem morador da **Favela do Rato Baleado** (Rio de Janeiro), estudante de Direito. Para financiar os estudos na faculdade, Damião entra para o movimento do tráfico local com a ajuda de seu amigo **Juninho**.

Durante sua vivência no morro, Damião é tocado pelo sonho de **Hastur** e descobre que uma ONG atuante na favela (liderada por Natasha) é, na verdade, uma fachada para uma seita secular de adoração ao Rei em Amarelo. Ao tentar interromper o ritual da seita em uma igreja para proteger seus amigos e **Martha**, Damião é capturado e sacrificado — seu ventre é cortado com a lâmina ritual que marca o símbolo de Hastur.

Damião morre na Terra e acorda abruptamente no **Deserto de Hali**, nas areias de **Carcosa**. Ele não compreende onde está: o que parece uma travessia pós-morte revela-se uma peregrinação através de memórias arrancadas (a diáspora africana, o quilombo de Malik Nazinga, a favela, a seita, sua própria morte e o futuro de Martha). Carcosa não é apenas uma dimensão alienígena: é uma **máquina cósmica de captura de memória e identidade**.

### 1.2 A Criatura Amarela vs. O Rei em Amarelo

- **O Rei em Amarelo:** O Avatar supremo de Hastur que governa Carcosa de seu trono no palácio.
- **A Criatura Amarela (O Observador Amarelo):** Não é Hastur nem o Rei em Amarelo. É a **forma atemporal de Damião** após sua transformação final na matéria amarela de Carcosa. Ao final de sua peregrinação, Damião é envolvido pela pele/casulo amarelo da dimensão, tornando-se o Observador que atravessa tempestades de memória e observa a si mesmo no passado (como a figura misteriosa nos pesadelos da boca de fumo do curta-metragem).

### 1.3 O Tom do Jogo (RPG Narrativo, Não-Roguelike)

O foco é a **tensão constante, a sobrevivência psicológica, a memória ancestral e a vulnerabilidade**.

- O jogo é um **RPG progressivo e narrativo**, conectado diretamente ao universo transmidiático. Não existe "tela de vitória" ao fim de uma dungeon ou fase — o encerramento é sempre uma **Transição Geográfica Diegética**.
- O combate é punitivo e técnico. A furtividade, a gestão de ruído, a navegação pela luz e o uso estratégico de armas rituais e consumíveis são a chave de sobrevivência.

### 1.4 Os 4 Pilares de Gameplay

1. **Dupla Barra de Sobrevivência (Resiliência Mental + Vitalidade Corpórea):** A saúde é dividida em sanidade (RM) e vida física (Vitalidade). Damião pode sofrer Derrota Corpórea ou Colapso Mental.
2. **Furtividade por Som, Faro e Luz:** Cultistas cegos caçam pelo som; Sseth Farejadores caçam pelo cheiro. A luz ampara a mente, mas expõe Damião visualmente.
3. **Mão Física & Sorteio de Armas Rituais (RNG):** Damião começa desarmado (golpe de mão vazia com Dano 0). Encontra no Baú da Tumba uma de três armas rituais únicas com habilidades próprias. _(Nota: O Salto Dimensional foi completamente removido do jogo)._
4. **Companheirismo com Yug-Neth:** Damião encontra e protege um filhote de **Mi-Go** (cativo na arena de Abdul), que atua como guia, funcionário de Carcosa e chave dos Portões.

---

## 2. REGRAS DE GAMEPLAY E SISTEMAS CENTRAIS

### 2.1 Dupla Barra: Resiliência Mental (RM) & Vitalidade Corpórea (Vit)

- **Resiliência Mental (RM - 100 max):** Mede a estabilidade psíquica. Drena no escuro (1 RM/s), dentro de tempestades de memória ou na presença de Espectros Roxos. Chegar a 0 RM dispara **Colapso Mental (Game Over)**.
- **Vitalidade Corpórea (Vit - 100 max):** Mede a integridade física de Damião. Sofrer golpes físicos de cultistas ou lâminas drena a Vitalidade. Chegar a 0 Vit dispara **Morte Corpórea (Game Over por Derrota Física)**.

### 2.2 Iluminação e Ancoragem

- **Postes de Luz Amarela:** Pontos fixos de Refúgio. Estar sob a luz ilumina o setor, cancela a drenagem de RM e ativa a **Ancoragem** (+10 RM/s até 100%).
- **Paradoxo da Luz:** A luz protege a mente, mas torna Damião 100% visível para criaturas com visão (como Nagas e o Byakhee).

### 2.3 Furtividade por Som, Faro e Tempestade

- **Agachar (Crouch):** Reduz a velocidade de movimento em 40%, reduz a hitbox e diminui em 80% o ruído gerado.
- **Detecção por Faro (Sseth Farejador):** Inimigos com faro detectam Damião por proximidade em área, independente de som ou visão. Podem ser neutralizados pelo _Frasco de Incenso_.
- **Tempestade de Memória como Stealth Passivo:** O vento forte no Overworld do Deserto abafa 100% o som dos passos, permitindo avançar rápido perto de cultistas cegos.

### 2.4 Ficha de Atributos e Sistema de Combate Físico

Todas as unidades do jogo (Damião, Cultistas, Bosses) possuem uma **Ficha de Atributos (ScriptableObject)** com 5 atributos fundamentais:

- **Vitalidade Máxima (Vit):** Saúde física.
- **Ataque (Atq):** Potencial de dano bruto desferido.
- **Defesa (Def):** Mitigação de dano físico recebido.
- **Conjuração (Conj):** Potencial de dano mágico/anômalo.
- **Resistência Anômala (ResAnom):** Defesa mágica contra dreno psíquico e frio.

#### Fórmula Simétrica de Mitigação por Defesa (Subtrativa com Piso):

$$\text{Dano Real} = \max(\text{Dano Bruto} \times 0.15,\, \text{Dano Bruto} - \text{Defesa})$$
Essa fórmula impede que defesas altas anulem 100% do dano, garantindo que todo golpe cause ao menos 15% de dano residual. Damião possui números de dano flutuantes (_DanoFlutuante_) em world space para feedback visual imediato.

---

## 3. O SISTEMA DE ARMAS E O BAÚ RNG DA TUMBA

### 3.1 Gating Inicial: Mão Vazia

Damião começa a jornada desarmado. O golpe desarmado (`MaoVazia`) possui **Dano 0**, mas permite desferir empurrões ruidosos para atrair atenção de cultistas para armadilhas ou fugir.

### 3.2 O Baú da Tumba (Zona 6b — Câmara do Baú)

Logo após a entrada da Tumba de Alhazred, Damião encontra a **Câmara do Baú (Zona 6b)**. Abrir o Baú da Tumba realiza um sorteio uniforme (RNG) de uma entre três armas rituais com habilidades próprias equilibradas para combate de chefes:

> ⚠️ **Esta tabela é o desenho ORIGINAL e está superada.** Mantida como registro. O estado
> vigente vive em `Docs/KnowledgeBundle/systems/armas_da_tumba.md`. Duas mudanças desde então:
> o **Cravo de Ferro** virou **Cravo de Aklo** (a habilidade deixou de ser mitigação e passou a
> interromper conjuração) e, em **2026-09-02**, a família inteira virou **Maça**, com a
> habilidade ***Calar o Aklo***. A ideia do Cravo ficou guardada para a expansão de itens.

| Arma Ritual             | Dano Bruto | Defesa | Efeito / Habilidade Especial                                               |
| :---------------------- | :--------: | :----: | :------------------------------------------------------------------------- |
| **Cravo de Ferro**      |     40     |   30   | Arma pesada com alto poder de mitigação física.                            |
| **Estilete de Irem**    |     25     |   15   | Aplica **Sangramento Contínuo** de 15 de dano por segundo no alvo.         |
| **Alfanje de Alhazred** |     60     |   40   | Habilidade **"Golpe do Deserto"**: atordoamento (Stun) garantido por 2.5s. |

_(Nota de Design: O Salto Dimensional/Ghost Dash foi integralmente removido do jogo. Não há poderes de teleporte atravessando paredes. Damião depende de esquiva física e das 3 armas rituais)._

---

## 4. ÁRVORE DE ITENS CONSUMÍVEIS E RELÍQUIAS

### 4.1 Consumíveis Rápidos (Hotbar de Ações)

Damião pode carregar até 3 unidades de cada consumível rápido na hotbar do HUD:

- **Chá Calmante:** Restaura 40 pontos de Resiliência Mental (tempo de consumo: 1s em lentidão).
- **Frasco de Incenso:** Emite uma nuvem perfumada que neutraliza o faro de Sseth Farejadores por 15s.
- **Sino de Estática:** Projetil arremessável que emite ruído estridente de 15m no ponto de impacto, atraindo cultistas.
- **Frasco de Veneno de Yig:** Arremessável que aplica debuff de defesa em bosses (-10 Defesa por 10s).
- **Fragmento de Yuggoth:** Restaura 25 de Resiliência do Companheiro (RC) para Yug-Neth.

### 4.2 Relíquias e Colecionáveis Ancestrais

- **📖 O Necronomicon:** Obtido exclusivamente ao derrotar Abdul Alhazred (Caminho B). Permite traduzir inscrições em Aklo e entender diálogos arcanos.
- **🧿 Patuá de Malik Nazinga:** Encontrado na capela das Ruínas Submersas. Conecta a linha temporal ancestral (Damião → Malik → Seu Kalunga → Martha).
- **🧿 Patuá das Luas Gêmeas:** Recompensa da Quest da Rainha Cassilda. Reduz o dreno de RM no escuro em 40%.
- **💍 Anel do Sinal Amarelo:** Drop do Boss Byakhee. Reduz a distância de detecção de cultistas em 30%.

### 4.3 Set Lendário de Set (4 Peças)

Conjunto de equipamento sacrófago pré-humano obtido no Templo da Serpente e no Castelo de Carcosa:

- **👑 Elmo de Set:** Drop garantido do Avatar de Set (+30% de resistência a dano divino).
- **🛡️ Peitoral de Set:** Drop na Cripta das Larvas (Z7) ou no Castelo.
- **🦵 Grevas de Set:** Drop no Templo ou no Castelo.
- **⚔️ Arma de Set:** Drop no Templo ou no Castelo.

* **Recompensa do Conjunto Completo (4/4):** Desbloqueia o acesso à **Sidequest Secreta do Avatar de Nyarlathotep** na Fase 2.

---

## 5. O COMPANHEIRO: YUG-NETH (MI-GO FILHOTE)

### 5.1 Identidade e Estado Cativo

Yug-Neth é um filhote de Mi-Go aprisionado por Abdul Alhazred na Zona 9. Antes do diálogo, ele já existe na arena em **estado cativo** (patrulha vaivém em rota de ping-pong ao lado de Abdul).

### 5.2 O Encontro Interativo com Abdul Alhazred

Damião interage com o botão de Ação (**E** / Botão Norte) no vulto flutuante de Abdul. O prompt "Falar com o vulto" avança diálogos até a decisão binária:

- **Caminho A (Concordar com Alhazred):** Alhazred libera Yug-Neth e desaparece com o _Necronomicon_. Sem luta. Damião obtém o companheiro, mas perde a capacidade de traduzir Aklo.
- **Caminho B (Recusar / Lutar):** Boss fight contra Alhazred. Yug-Neth permanece cativo e seguro na arena. Vitória garante **O Necronomicon** + libertação de Yug-Neth.

### 5.3 Função nos Portões de Carcosa & Mecânica de Proteção

- Yug-Neth é o guia e chave dimensional para os Portões de Carcosa.
- **Mecânica de Proteção (Resiliência do Companheiro — RC):** Yug-Neth possui barra de RC no HUD após ser libertado. Durante batalhas (Byakhee, Nagas), se a RC de Yug-Neth chegar a 0 por ataques inimigos → **Escolta Perdida = Game Over por Colapso do Companheiro**. Damião deve bloquear projéteis fisicamente.

---

## 6. ESTRUTURA MACRO DO VERTICAL SLICE (DUAS FASES)

```
[FASE 1: O DESERTO DE HALI] ──────────────────────▶ [FASE 2: O CASTELO DE CARCOSA]
• Entrada (Garganta de Pedra Pálida)                • Interior do Palácio Real de Yhtill
• Ruínas Submersas & Patuá de Malik                 • Galerias da Burocracia Industrial do Horror
• Dungeon 1 (Tumba de Alhazred — Baú RNG Z6b)       • Filas de Almas Esvaziadas & Espectros Roxos
• Santuário de Yhtill (Quest Cassilda + 5 Frags)    • Sidequest: Avatar de Nyarlathotep (Set 4/4)
• Dungeon 2 (Templo da Serpente — Opcional)         • CHEFE FINAL: O REI EM AMARELO
• Portões das Ruínas (Boss Byakhee + Yug-Neth)
```

---

## 7. BESTIÁRIO E COMPÊNDIO DE ENTIDADES

| Entidade                       | Tipo / Classificação                | Origem Transmídia       | Comportamento & Mecânica                                                                       |
| :----------------------------- | :---------------------------------- | :---------------------- | :--------------------------------------------------------------------------------------------- |
| **Cultista Amarelo**           | Inimigo Comum                       | Curta / Seita da ONG    | Cegos. Caçam por som. Ficha: Vit 100 / Atq 24 / Def 5.                                         |
| **Espectro de Hali**           | Inimigo Furtivo                     | Deserto / Ruínas        | Espectro jovem de sanidade corrompida. Invisível no escuro; drena RM.                          |
| **Espectro Roxo (Dementador)** | Predador Aéreo / Espectro Ancestral | Animação "O Peregrino"  | **Espectros de Hali ancientes e mais fortes**, deformados por consumirem memórias por séculos. |
| **Coisa do Cemitério**         | Mini-boss Errante                   | Curta / Deserto Central | Patrulha aleatória guiada por faro no deserto central. Invencível; exige fuga.                 |
| **Sseth Comum**                | Inimigo Serpentino                  | Templo da Serpente      | Serpentes bípedes. Variante **Farejadora** (detecta por cheiro).                               |
| **Naga Guerreiro**             | Inimigo de Elite                    | Templo da Serpente      | 2m de altura. Visão ampla. Podem ser confundidos por Yug-Neth.                                 |
| **Larvas de Yig**              | Armadilha Viva                      | Templo (Poço Z5)        | Grudam no jogador causando lentidão severa e dreno de RM.                                      |
| **Abdul Alhazred**             | Miniboss                            | Tumba Z9                | Flutua, dispara Cones de Gelo, protegido por Escudo Mágico. Vit 300 / Def 5.                   |
| **Nagaraja**                   | Boss 1 do Templo                    | Templo Z10              | Sacerdote de 2.5m. Transmuta braços em serpentes na Fase 2.                                    |
| **Avatar de Set**              | Boss 2 do Templo                    | Templo Z11              | Ser divino de 4m. Vulnerável apenas quando hieróglifo branco pulsa.                            |
| **Byakhee**                    | Boss dos Portões                    | Portões das Ruínas      | Criatura alada de 3.5m. Grito infrassônico drena RM passivamente.                              |
| **Yug-Neth**                   | Companheiro                         | Animação / Tumba Z9     | Filhote Mi-Go. Guiador/chave dos Portões. Requer proteção (RC).                                |
| **A Criatura Amarela**         | Entidade / O Observador             | Curta / Animação        | **A forma atemporal de Damião absorvido por Carcosa**. Damião observando seu próprio passado.  |
| **O Rei em Amarelo**           | Chefe Final                         | Curta / Animação / HQ   | Avatar supremo de Hastur. Exige virar de costas quando a Máscara cai.                          |

---

## 8. PADRÃO TÉCNICO E ESPECIFICAÇÕES DE ENGINE (UNITY 6.4)

- **Pixel Per Unit (PPU):** Exactly `32` para todas as texturas e sprites.
- **Geometria de Chão:** Tilemap com losangos de `32 × 16 px` (Proporção 2:1 Isométrica).
- **Grid Cell Size:** `(1.0, 0.5, 1.0)`.
- **Física:** Rigidbody2D com `gravityScale = 0`. Colisores poligonais ajustados ao pé dos atores.
- **Sorting de Profundidade:** Algoritmo dinâmico `Z = -y * 10` para resolver sobreposição de personagens e paredes.
- **Câmera:** Ortográfica plana sem rotação (`Quaternion.identity`), offset Z = -10, tamanho ortográfico 6.0.
- **Filtros de Textura:** `Point (no filter)`, compressão `None`, pivot `Bottom`.

---

## 9. GLOSSÁRIO DIEGÉTICO OBRIGATÓRIO

| Termo Proibido (RPG Genérico) | Termo Oficial Diegético             |
| :---------------------------- | :---------------------------------- |
| HP / Vida / Health            | **Resiliência Mental (RM)**         |
| Levar Dano                    | **Sofrer Trauma**                   |
| Curar / Heal                  | **Ancorar**                         |
| Cura Completa                 | **Estabilizar Completamente**       |
| Morte / Game Over             | **Colapso**                         |
| Low Health                    | **Estado de Pânico**                |
| Inimigo Genérico              | **Cultista Amarelo**                |
| Patrol                        | **Errante**                         |
| Companheiro HP                | **Resiliência do Companheiro (RC)** |
