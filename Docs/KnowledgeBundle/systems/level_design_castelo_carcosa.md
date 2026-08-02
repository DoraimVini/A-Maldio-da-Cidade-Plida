---
type: Level Design
title: Level Design — Fase 2: O Castelo de Carcosa
description: Documento de design da Fase 2, incluindo o mapa topológico, zonas do castelo, sidequest de Nyarlathotep, o boss Rei em Amarelo e a mecânica de Pressão Psíquica.
tags: [level-design, dungeon, castelo-de-carcosa, fase2, pressao-psiquica, rei-em-amarelo]
timestamp: 2026-08-02T12:00:00-03:00
status: Planejado
---

# Level Design — Fase 2: O Castelo de Carcosa
**A Maldição da Cidade Pálida — Fim do Vertical Slice**

---

## 1. CONCEITO DO ESPAÇO

### 1.1 Identidade
Ao contrário do deserto aberto, o Castelo de Carcosa é a manifestação física do delírio. Mámore negro, adornos de ouro manchado, tecidos nobres apodrecendo. O palácio está isolado do tempo e do espaço — as janelas não mostram o deserto lá fora, mas o vazio cósmico pontuado por Aldebaran, as luas gêmeas e o Sol Negro de Carcosa. A própria nobreza do lugar (os governantes de Yhtill) está fossilizada pelo terror cósmico, estátuas calcificadas em poses de adoração doentia.

### 1.2 Função no Jogo
O Castelo de Carcosa é a Fase Final do Vertical Slice (Fase 6 do jogo completo).
- É um ambiente **claustrofóbico**, centrado na mecânica de furtividade visual (evitar contato visual com horrores).
- É o clímax narrativo, resolvendo a história do companion Yug-Neth.
- É a conclusão das duas grandes buscas de Damião: a principal (O Rei em Amarelo) e a opcional (O Avatar de Nyarlathotep, via Set Lendário completo).

---

## 2. MAPA TOPOLÓGICO

```
                                  ┌───────────────────────┐
                                  │      O TRONO DE       │ ← BOSS FINAL
                                  │       ALDEBARAN       │ (Rei em Amarelo)
                                  └───────────┬───────────┘
                                              │ (Requer as 4 Relíquias para o ritual)
                                              │
                      ┌───────────────────────┴───────────────────────┐
                      │                                               │
             ┌────────┴────────┐                             ┌────────┴────────┐
             │ Z3: A BIBLIOTECA│                             │ Z4: OBSERVATÓRIO│ ← SIDEQUEST
             │ ESQUECIDA       │                             │ SECRETO         │ (Nyarlathotep)
             │ (Puzzle / Lore) │                             │ (Requer Set 4/4)│
             └────────┬────────┘                             └────────┬────────┘
                      │                                               │
                      └───────────────────────┬───────────────────────┘
                                              │
                                  ┌───────────┴───────────┐
                                  │ Z2: O SALÃO DO BANQUETE│ ← Hub Central
                                  │ FOSSILIZADO           │ (Nobreza petrificada)
                                  └───────────┬───────────┘
                                              │
                                  ┌───────────┴───────────┐
                                  │ Z1: OS PORTÕES        │ ← Ponto de Chegada
                                  │ INTERNOS (Entrada)    │
                                  └───────────────────────┘
```

---

## 3. ZONAS E PONTOS DE INTERESSE

### Z1: Os Portões Internos (Entrada)
- **Visual:** O interior gigantesco das portas maciças derrotadas após o Byakhee.
- **Narrativa (O Destino de Yug-Neth):** Ao entrar, as portas se trancam magicamente. A pressão psíquica afeta o Migo fortemente. Ele foge para o interior do castelo, enlouquecido. 
- **Mecânica:** Área segura. Último Refúgio oficial livre de tensão.

### Z2: O Salão do Banquete Fossilizado
- **Visual:** Mesas quilométricas, lustres caídos. Dezenas de nobres transformados em pedra branca.
- **Mecânica Central (Stealth Visual):** Inimigos (*Cortesãos Pálidos*) patrulham ativamente. O jogador deve se esconder atrás dos corpos de pedra. 
- **Conteúdo Oculto:** A terceira peça do Set Lendário está no corpo do lorde sentado à cabeceira da mesa secundária.

### Z3: A Biblioteca Esquecida
- **Visual:** Um ambiente verticalizado (dentro das limitações isométricas), espelhos imensos refletindo realidades impossíveis.
- **Mecânica (Pressão Psíquica):** O jogador drena RM continuamente se ficar virado de frente para os **Espelhos de Aldebaran**. Ele deve navegar a sala andando de costas ou de lado para certos objetos.
- **Clímax da Área:** Miniboss contra **Yug-Neth Corrompido**. Sem salvação. Após derrotá-lo, obtém-se o acesso à quarta e última peça do Set Lendário.

### Z4: O Observatório Secreto (Dungeon Opcional)
- **Acesso:** Porta selada por constelações antigas. Só abre se Damião possuir as 4 peças do Set Lendário (2 do Deserto, 2 do Castelo).
- **Conteúdo:** O encontro com o Avatar de Nyarlathotep. Arena fragmentada. Boss fight pautado em quebra de ritmo e ilusões sonoras.

### Z5: O Trono de Aldebaran (A Arena do Rei)
- **Visual:** Varanda sem bordas para o espaço. O Rei flutua ou aguarda no centro, as luas gêmeas refletidas na pedra negra do chão.
- **O Combate Final:**
  - Não há barra de vida (Vitalidade).
  - O jogador precisa se mover por pontos focais na arena para ativar as **4 Relíquias** (Anel, Coroa, Patuá, Necronomicon), iniciando o rito de selamento.
  - **Mecânica da Máscara Pálida:** Quando o Rei sinaliza o desvelar do rosto, a UI emite o sinal de Pressão Psíquica Extrema. Damião tem **1.5s** para dar as costas ao Rei. Ficar de frente resulta em Colapso Psíquico (Instant Game Over).

---

## 4. INIMIGOS E MECÂNICAS AMBIENTAIS

### 4.1 Pressão Psíquica (Mecânica Ambiental)
- Substitui a Tempestade de Areia da Fase 1.
- Áreas de Pressão exigem que Damião não faça contato visual direto (através de LookDirection) com certas entidades ou objetos (Espelhos, Máscara do Rei).
- Penalidade: Dreno rápido de Resiliência Mental (RM).

### 4.2 Inimigos do Castelo
| Inimigo | Comportamento | Função de Design |
| :--- | :--- | :--- |
| **Cortesão Pálido** | Patrulha agressiva. Ataques físicos pesados e gritos de dano anômalo. | "Guarda" padrão que força furtividade tática. |
| **Eco de Carcosa** | Entidade invisível. Surge e drena RM se o jogador ficar imóvel na sombra por muito tempo. | Ferramenta anti-camping, forçando progressão. |
| **Yug-Neth (Miniboss)** | Pulos erráticos, golpes de garra e uso de magias cósmicas residuais. Luta passional. | Peso emocional e liberação da peça final do Set. |
