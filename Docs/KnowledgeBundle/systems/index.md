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
- [Vitalidade Corpórea](vitalidade.md) — Vida física (a "carne"), distinta da sanidade; zerá-la abate o ator
- [Companheiro Mi-Go e Escolha Ramificada com Abdul](companheiro_mi_go.md) — Filhote Mi-Go obrigatório para os Portões de Carcosa; conversa lutar × concordar; morte do companheiro encerra a run
- [Interação com o Mundo (botão E)](interacao.md) — Camada de interação deliberada: prompt + seleção de alvo, substitui o disparo por toque
- [Ficha de Atributos e Matemática do Combate](ficha_de_atributos.md) — Os 5 atributos de toda unidade, fórmula de mitigação e balanceamento
- [As Três Armas da Tumba](armas_da_tumba.md) — Cravo/Estilete/Alfanje: básico + habilidade, e o sangramento por acúmulo
- [Luta contra Abdul Alhazred](boss_abdul.md) — Boss em fases: Escudo Mágico, Pedras de Poder, Cones de Gelo e a janela de exaustão
- [IA do Cultista](cultista_ai.md) — FSM de comportamento dos inimigos
- [Espectro](espectro.md) — Manifestação espectral roteirizada (cutscenes)
- [Coisa do Cemitério](coisa_do_cemiterio.md) — Stealth-brute que caça por faro, imune a combate, insta-kill no toque
- [Patrulha](patrulha.md) — Rotas e lógica de movimentação dos Cultistas
- [Propagação Sonora](sound_propagation.md) — Como o som funciona como mecânica de stealth
- [Stealth](stealth.md) — Mecânica de furtividade geral
- [Habilidades Anômalas](abilities.md) — Sistema de poderes sobrenaturais
- [Esquiva](esquiva.md) — Dodge físico (não-anômalo)
- [Game Loop](game_loop.md) — Máquina de estados do ciclo do jogo
- [Estado do Ambiente](environment.md) — Estados do mundo de Carcosa
- [Level Design - Ruínas Pálidas](level_design.md) — Diretrizes de level design do nível inicial e métricas
- [Chão em Tilemap Isométrico de Losango 2:1](tilemap_isometrico_losango.md) — Receita real de Grid+Tilemap isométrico (confirmada na cena), colisão de borda automática e a matemática do tamanho do losango
- [Persistência (Save)](../architecture/persistencia.md) — Chaves de persistência (GUID imutável), Save Manager central, JSON e degradação graciosa
- [Renderização Isométrica](renderizacao_isometrica.md) — Profundidade por Y-sort dinâmico + oclusão dither (silhueta atrás de paredes altas)
