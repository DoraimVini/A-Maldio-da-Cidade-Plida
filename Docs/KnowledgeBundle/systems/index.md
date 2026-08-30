---
type: Index
title: Sistemas de Jogo
description: Regras de game design, mecânicas e fórmulas de gameplay
---

# Sistemas de Jogo

Aqui estão as **regras de negócio** — o que deve acontecer e as fórmulas matemáticas/lógicas. Para detalhes de implementação (como o código funciona), veja [Scripts](../scripts/index.md).

## Sistemas Core

- [Arte e Animação — estado medido](arte_e_animacao.md) — Nada no jogo anima; o defeito de alfa na folha do Abdul; avaliação dos pacotes de sprite e suas licenças
- [Sistema de Combate Pálido](combate.md) — Combate Tático (Priming/Defesas) como alternativa ao Stealth
- [Resiliência Mental](resiliencia_mental.md) — Sistema central de sanidade (HP diegético)
- [Vitalidade Corpórea](vitalidade.md) — Vida física (a "carne"), distinta da sanidade; zerá-la abate o ator
- [Companheiro Mi-Go e Escolha Ramificada com Abdul](companheiro_mi_go.md) — Filhote Mi-Go obrigatório para os Portões de Carcosa; conversa lutar × concordar; morte do companheiro encerra a run
- [Interação com o Mundo (botão E)](interacao.md) — Camada de interação deliberada: prompt + seleção de alvo, substitui o disparo por toque
- [Opções e Preferências do Jogador](opcoes_e_preferencias.md) — **[NOVO, 2026-08-29]** Volume, tela cheia e sincronização vertical. Até esta data o jogo não tinha controle de volume nenhum. Padrão é **VSync ligada**, por recomendação explícita da doc da Unity 6.4 contra `targetFrameRate`
- [Ficha de Atributos e Matemática do Combate](ficha_de_atributos.md) — Os 5 atributos de toda unidade, fórmula de mitigação e balanceamento
- [Atributos, Níveis e Build](atributos_e_build.md) — **[CONSOLIDADO 2026-08-14]** Documento de discussão: os dois vocabulários de atributo que coexistem, os números reais das 5 fichas e das 3 armas, a curva de 12 níveis, e as 6 perguntas abertas de design
- [As Três Armas da Tumba](armas_da_tumba.md) — Cravo/Estilete/Alfanje: básico + habilidade, e o sangramento por acúmulo
- [Loot e Drop](loot_e_drop.md) — **[BASE + AFIXOS ROLADOS, 2026-08-27]** A invariante "o sorteio nunca gera atributos" foi **revogada** (sem geração, arma de nível máximo = arma de nível 1). Agora: base autorada + grau + afixos rolados de pool autorado
- [Habilidades de Item](habilidades_de_item.md) — **[IMPLEMENTADO 2026-08-27]** Efeitos como dado: arma nova deixou de custar uma classe C#
- [Armas à Distância](armas_a_distancia.md) — **[REGISTRO, PÓS-VS]** Arcos, bestas e armas de fogo. Nada decidido, nada implementado; não começar sem pedido explícito
- [Artefatos](artefatos.md) — **[IMPLEMENTADO]** Os 4 slots, passiva + habilidade por Artefato, a barra F1–F4 e a regra de que só vale o que está equipado
- [Análise do Inventário](inventario_analise.md) — **[AUDITORIA 2026-08-14]** O que está de fato ligado, os 7 atributos que não fazem nada, o bug que apaga a ficha ao trocar equipamento, e a ordem de correção sugerida
- [Áudio](audio.md) — **[IMPLEMENTADO, sem clipes]** Mixer com pool, banco autorável e síntese de andaime. Torna audível o ruído que Damião emite — o pilar que era invisível
- [Boss Byakhee](boss_byakhee.md) — **[CORE+RUNTIME]** O cadeado dos Portões: imune no ar, vulnerável só no pouso; fecha a Fase 1
- [Boss Rei em Amarelo](boss_rei_em_amarelo.md) — **[CORE+RUNTIME]** O confronto final: sem barra de vida, ritual de relíquias + selamento por reação (Máscara Pálida). Ver também o Carcosa Debugger e a Arena de Testes
- [Persistência](persistencia.md) — O ciclo do save, o padrão Observer do `IPersistente`, e o bug em que o jogo gravava mas nunca lia de volta
- [HUD](hud.md) — As 6 views, o `HUDController` como injetor, e `BuildHUDCompleto` como ponto único de montagem — nenhuma cena tinha HUD completo antes de 2026-08-13
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
- [Construção de Dungeons (Templo da Serpente)](construcao_de_dungeons.md) — Hierarquia, Colliders e Template de Dungeons Isométricas
- [Chão em Tilemap Isométrico de Losango 2:1](tilemap_isometrico_losango.md) — Receita real de Grid+Tilemap isométrico (confirmada na cena), colisão de borda automática e a matemática do tamanho do losango
- [Persistência (Save)](../architecture/persistencia.md) — Chaves de persistência (GUID imutável), Save Manager central, JSON e degradação graciosa
- [Física 2D — espaços de coordenada, camadas e colisão](fisica_2d.md) — **[NOVO 2026-08-27]** O modelo de física num lugar só: os dois espaços de direção e a tabela dos 8 inputs, a taxonomia de camadas, quando usar matriz × excludeLayers × ContactFilter2D, a colisão de cenário por CompositeCollider2D, câmera pixel-perfect, e a classe de defeito que domina o projeto
- [Renderização Isométrica](renderizacao_isometrica.md) — Profundidade por Y-sort dinâmico + oclusão dither (silhueta atrás de paredes altas)
- [Sistema de Vigor (Estamina)](vigor_estamina.md) — Consumo tático na corrida e esquiva, penalidade por exaustão
- [Labirinto de Carcosa (Progressão)](progressao_labirinto_carcosa.md) — XP narrativo, Árvore de Símbolo Amarelo, Pontos de Eco e Santuários. **§5 registra a divergência: zero nós autorados e o manager nunca instanciado.** §6 mapeia a proposta de 2026-08-14 (Lucidez, Sinal, 3 eixos) ao que já existe
