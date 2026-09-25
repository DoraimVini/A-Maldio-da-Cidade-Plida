---
type: Lore Reference
title: Regras do Mundo
description: Regras narrativas e lógicas do universo de Carcosa que afetam o gameplay.
tags: [lore, world-building, narrative, carcosa]
timestamp: 2026-07-30T00:00:00Z
---

# Regras do Mundo de Carcosa

## Premissa

Damião está preso nas Ruínas Pálidas, uma manifestação da Cidade Pálida (Carcosa) do mito de Hastur. O mundo é hostil à sanidade humana — simplesmente estar lá drena a mente.

## Regras que Afetam Gameplay

1. **A realidade é maleável** — Carcosa é onírica: a geometria não obedece, e "rachar" a realidade é possível. Esta continua sendo a premissa cosmológica do mundo, mas **hoje não há nenhuma habilidade de jogador ligada a ela** — o Salto Dimensional foi integralmente removido do jogo (2026-07-30). A maleabilidade sustenta o *design do mundo* (Portões que só Yug-Neth abre, barreiras anômalas, o Templo da Serpente), não uma ferramenta na mão de Damião. Ver [Habilidades Anômalas](../systems/abilities.md).

2. **O som atrai** — Os [Cultistas Amarelos](../systems/cultista_ai.md) caçam por som, não por visão (na implementação atual). O mundo amplifica sons em [zonas de alta anomalia](../systems/environment.md).

3. **Luz é refúgio** — Postes de luz e fontes luminosas funcionam como pontos de recuperação de RM. Na escuridão, o dreno passivo é mais intenso.

4. **Há duas formas de perder** — A [Resiliência Mental](../systems/resiliencia_mental.md) a zero é o **Colapso**: Damião não "morre" no sentido tradicional, se perde na loucura de Carcosa (game over narrativo). A [Vitalidade](../systems/vitalidade.md) a zero é a morte **corpórea** — o corpo cede a golpes, garras e lâminas. Uma terceira causa existe depois da Tumba: perder [Yug-Neth](../systems/companheiro_mi_go.md), o companheiro obrigatório, encerra a run na hora.

5. **Stealth é o núcleo tonal; o combate é um pilar sistêmico real** — A furtividade e o horror cósmico definem o *tom* do jogo, e evitar conflito quase sempre é a escolha mais sábia. Mas o combate aberto **existe, é implementado e é profundo**: toda unidade tem [ficha de atributos](../systems/ficha_de_atributos.md) (Vitalidade/Ataque/Defesa/Conjuração/Resistência Anômala), há fórmula de mitigação por defesa, três armas com ataque básico + habilidade própria, e um boss em fases ([Abdul Alhazred](../systems/boss_abdul.md)). O que **não** existe é farming/loot genérico de ARPG — ver a seção de escopo no `CLAUDE.md` da raiz.

## Implicações para Design de Mecânicas

- Toda nova mecânica deve respeitar o tom de **horror cósmico + stealth**
- Habilidades sobrenaturais sempre têm um custo (RM)
- O jogador deve sentir tensão constante, não empoderamento — vencer uma luta deve custar caro, não virar power fantasy
- Itens são raros e preciosos, não farmáveis
