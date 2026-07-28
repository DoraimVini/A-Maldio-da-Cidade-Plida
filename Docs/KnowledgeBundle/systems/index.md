---
type: Index
title: Sistemas de Jogo
description: Regras de game design, mecânicas e fórmulas de gameplay
---

# Sistemas de Jogo

Aqui estão as **regras de negócio** — o que deve acontecer e as fórmulas matemáticas/lógicas. Para detalhes de implementação (como o código funciona), veja [Scripts](../scripts/index.md).

## Sistemas Core

- [Sistema de Combate Pálido](combate.md) — Combate Tático (Priming/Defesas) como alternativa ao Stealth
- [Resiliência Mental](resiliencia_mental.md) — Sistema central de sanidade (HP diegético)
- [IA do Cultista](cultista_ai.md) — FSM de comportamento dos inimigos
- [Espectro](espectro.md) — Manifestação espectral roteirizada (cutscenes)
- [Coisa do Cemitério](coisa_do_cemiterio.md) — Stealth-brute que caça por faro, imune a combate, insta-kill no toque
- [Patrulha](patrulha.md) — Rotas e lógica de movimentação dos Cultistas
- [Propagação Sonora](sound_propagation.md) — Como o som funciona como mecânica de stealth
- [Stealth](stealth.md) — Mecânica de furtividade geral
- [Habilidades Anômalas](abilities.md) — Sistema de poderes sobrenaturais
- [Salto Dimensional](dimensional_leap.md) — Ghost Dash (habilidade anômala)
- [Esquiva](esquiva.md) — Dodge físico (não-anômalo)
- [Game Loop](game_loop.md) — Máquina de estados do ciclo do jogo
- [Queda Z4 → Z5](queda_z4_z5.md) — Cerco e colapso do chão, transição de zona só de ida
- [Estado do Ambiente](environment.md) — Estados do mundo de Carcosa
- [Level Design - Ruínas Pálidas](level_design.md) — Diretrizes de level design do nível inicial e métricas
- [Persistência (Save)](persistencia.md) — Esqueleto de salvamento: SaveData JSON, SaveSystem, ResilienciaMental.Restaurar
- [Renderização Isométrica](renderizacao_isometrica.md) — Profundidade por Y-sort dinâmico + oclusão dither (silhueta atrás de paredes altas)
