---
type: Index
title: Scripts (Implementação)
description: Documentação técnica dos scripts C# do projeto, separados em Core e Runtime
---

# Scripts (Implementação)

Esta seção documenta a implementação técnica dos scripts. Aqui detalhamos *como* o código funciona (contratos, dependências, APIs públicas). Para entender as regras lógicas e o "porquê", consulte [Sistemas de Jogo](../systems/index.md).

## Organização por Camada

Seguindo o [Padrão POCO + Adapter](../architecture/poco_adapter_pattern.md), os scripts estão divididos em duas grandes famílias:

- [Scripts Core (POCOs)](core/index.md) — Lógica de domínio pura (sem Unity)
- [Scripts Runtime (Adapters)](runtime/index.md) — MonoBehaviours que integram a lógica com a engine
