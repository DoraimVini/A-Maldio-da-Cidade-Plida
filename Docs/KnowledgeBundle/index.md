---
type: Index
title: Favela Amarela Knowledge Bundle
description: Base de conhecimento oficial para o projeto "A Maldição da Cidade Pálida" — o cérebro de um Dev Sênior Unity 6.4
timestamp: 2026-07-07T11:00:00Z
---

# Knowledge Bundle: A Maldição da Cidade Pálida (Favela Amarela)

Este bundle OKF contém a base de conhecimento completa do projeto, organizada para ser lida tanto por desenvolvedores humanos quanto por agentes de IA. Ele funciona como a memória persistente de um **Engenheiro de Gameplay Sênior especializado em Unity 6.4**.

> 🤖 **[Contexto Completo para Outros Agentes](contexto_para_outros_agentes.md)** — ponto de entrada único para qualquer IA (Antigravity, outra sessão) que não acompanhou a sessão em andamento: estado do git, o que foi construído, pendências ativas e armadilhas já pagas. Leia isto primeiro se você está chegando agora.
>
> 📌 **[Roadmap do Vertical Slice](roadmap_vertical_slice.md)** — estado real de cada item da lista de produção (pronto / parcial / não-começado) e os riscos de escopo. Auditado em 2026-07-31.

## Navegação do Catálogo

- [Documento de Design Mestre (GDD)](GDD_Mestre.md) — Visão geral, pilares de design e HUD diegético do jogo
- ⚠️ [**Divergências do GDD**](divergencias_do_gdd.md) — onde a implementação se afastou do GDD, e **por quê**. Leia junto com o GDD: ele guarda a intenção de design, este registro guarda a diferença. Ex.: Yug-Neth **deixou de ser** a chave dos Portões; a Resiliência Mental **deixou de ser** o único recurso
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
