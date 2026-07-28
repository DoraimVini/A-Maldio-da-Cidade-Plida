---
type: Index
title: Favela Amarela Knowledge Bundle
description: Base de conhecimento oficial para o projeto "A Maldição da Cidade Pálida" — o cérebro de um Dev Sênior Unity 6.4
timestamp: 2026-07-07T11:00:00Z
---

# Knowledge Bundle: A Maldição da Cidade Pálida (Favela Amarela)

Este bundle OKF contém a base de conhecimento completa do projeto, organizada para ser lida tanto por desenvolvedores humanos quanto por agentes de IA. Ele funciona como a memória persistente de um **Engenheiro de Gameplay Sênior especializado em Unity 6.4**.

## Navegação do Catálogo

- [Documento de Design Mestre (GDD)](GDD_Mestre.md) — Visão geral, pilares de design e HUD diegético do jogo
- [Arquitetura](architecture/index.md) — Decisões arquiteturais, padrões e convenções estruturais
- [Sistemas de Jogo](systems/index.md) — Regras de game design, mecânicas e fórmulas
- [Scripts](scripts/index.md) — Documentação de implementação dos scripts C#
- [Unity 6.4 Gotchas](unity64_gotchas/index.md) — APIs renomeadas, armadilhas de performance e padrões técnicos
- [Testes e QA](tests/index.md) — Padrões de testes e pipeline de qualidade
- [Lore e Vocabulário](lore/index.md) — Terminologia diegética e regras do universo narrativo

## Como Usar Este Bundle

**Para Humanos:** Navegue pela estrutura de diretórios ou use os links acima para encontrar a documentação relevante.

**Para Agentes de IA:** Comece por este `index.md`. Use o campo `type` do frontmatter YAML para filtrar documentos. Siga os links relativos para aprofundar. Consulte a seção 3.1 do `CLAUDE.md` raiz para regras obrigatórias.

> **Regra de Ouro:** Em caso de conflito entre este OKF e o código-fonte, o código é verdade para *como* funciona; o OKF é verdade para *como deveria* funcionar. Sinalize divergências.
