---
type: Game System
title: Level Design - Ruínas Pálidas (Nível Inicial)
description: Diretrizes de level design do primeiro nível (Ruínas Pálidas) do projeto, incluindo o fluxo das zonas e métricas de construção básicas
tags: [level-design, metricas, s-path, ruinas-palidas]
timestamp: 2026-07-10T15:00:00Z
---

# Level Design: Ruínas Pálidas (Nível Inicial)

Este documento define as diretrizes de construção de nível para as **Ruínas Pálidas** — hoje a **Tumba de Alhazred, Dungeon 1 da Fase 1 (Deserto de Hali)**, ver `GDD_Mestre.md` §1.8 e `lore/deserto_e_dungeons.md`. Ele está aberto para futuras expansões conforme o mapa e novos setores forem criados.

---

## 1. Métricas Físicas de Construção

Para manter o gameplay isométrico justo e consistente nas 8 direções de movimento:
*   **PPU (Pixels Per Unit):** 32 (2026-07-28: padrão único do projeto; ver `.claude/skills/favela-isometric-standards/SKILL.md`). Métricas antigas a 16 PPU/sprites 64x64 valem para a arte legada, reimportada quando tocada.
*   **Largura de Vielas/Corredores:** Mínimo de $3.5$ unidades (permite desviar apertado), ideal de $4.5$ a $5.0$ unidades.
*   **Espessura de Parede:** Padrão de $0.5$ unidades (`BoxCollider2D` sólido).
*   **Física:** Gravidade zero (`gravityScale = 0`) em todos os rigidbodies 2D. O Y-Sorting dinâmico lida com a sobreposição de sprites no eixo vertical (Y) do plano de jogo.

---

## 2. Zoneamento do Nível Inicial (O Layout "S-Path")

O nível inicial é dividido em 5 zonas que conectam a entrada à primeira área subterrânea:

### Zona 1: Rua de Entrada (Leste)
*   **Foco:** Tutorial básico de som e refúgios.
*   **Desafio:** Uma rua linear com 2 cultistas errantes.
*   **Estrutura:** Contém postes de luz amarela que ensinam o jogador a parar e regenerar a Resiliência Mental (RM).
*   **Tempestade (2026-07-10):** Moderada (0.2–0.6) desde o spawn — ver [Estado do Ambiente](environment.md).

### Zona 2: Vila das Casas (Sul)
*   **Foco:** Furtividade de cobertura em interiores.
*   **Desafio:** Cultistas patrulham a rua principal em rotas sobrepostas.
*   **Estrutura:** 3 casas com portas abertas e paredes sólidas. O jogador deve adentrar nas casas para quebrar a linha de visão e som das patrulhas.
*   **Tempestade (2026-07-10):** Base calma (0.1–0.3), mas com rajadas fortes aleatórias (0.6–0.9) via `TempestadeRajadaAleatoria` — ver [Estado do Ambiente](environment.md).
*   **Coisa do Cemitério (2026-07-10):** Posicionada na transição para a Zona 3 (fim da Z2 → meio da Z3) — ver [Coisa do Cemitério](coisa_do_cemiterio.md).

### Zona 3: Beco do Vento (Oeste)
*   **Foco:** Pacing e efeitos de clima local.
*   **Desafio:** Vielas estreitas com uivos de vento forte.
*   **Estrutura:** O som de Damião correndo é abafado (silêncio), mas se ele permanecer parado fora de abrigos, a tempestade de vento causa lentidão de movimento e aumenta o dreno de RM (lentidão/dreno ainda não implementados — hoje a tempestade só afeta véu visual + abafamento sonoro).
*   **Tempestade (2026-07-10):** Forte e estável (0.6–0.9), mesmo trigger que cobre a Zona 4 (zonas fisicamente contíguas).

### Zona 4: Praça do Cerco (Sul)
*   **Foco:** Clímax de tensão e transição física.
*   **Desafio:** Uma arena retangular aberta onde o jogador é encurralado por cultistas após pegar uma pista.
*   **Transição:** O chão desaba em um tremor de anomalia, forçando a queda inevitável de Damião (sentido único) para a Zona 5.
*   **Tempestade (2026-07-10):** Forte e estável (0.6–0.9), mesmo trigger da Zona 3.

### Zona 5: Subterrâneo - Ruínas de Hali (Ponto de Virada da Dungeon)
*   **Foco:** Conquista de poder (Retaliação).
*   **Desafio:** Área totalmente escura, sem postes de luz iniciais e cultistas no caminho.
*   **Estrutura:** O jogador acorda vulnerável, mas encontra no centro os dois itens que alteram a jogabilidade: a **Barra Enferrujada** (desbloqueando a Mão Física) e o **Salto Dimensional** (desbloqueando a Mão Anômala).
*   **Tempestade (2026-07-10):** Nula (0–0) — área fechada, sem céu, acessível só pela queda forçada da Zona 4.

---

## 3. Diretrizes de Expansão (Roadmap de Level Design)
Conforme novos níveis e mapas forem desenhados:
- Toda nova zona deve manter um equilíbrio rígido de **Postes de Luz (Pontos de Refúgio)** a cada ~45 segundos de caminhada tensa.
- Rotas alternativas de stealth devem estar sempre presentes para evitar combate direto.
- Puzzles ambientais futuros (ex: portais de anomalia) devem usar o **Salto Dimensional** como chave física de travessia.
