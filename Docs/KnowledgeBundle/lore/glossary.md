---
type: Lore Reference
title: Glossário Diegético
description: Tradução de termos genéricos de game dev para o vocabulário oficial do universo.
tags: [lore, terminology, diegetic, naming]
timestamp: 2026-07-07T11:00:00Z
---

# Glossário Diegético

**Regra:** Nunca use termos genéricos de RPG em texto visível ao jogador, nomes de habilidade, ScriptableObjects, ou nomes de variáveis/métodos públicos.

## Tabela de Tradução

| Termo Genérico | Termo Diegético | Usado em |
|---------------|-----------------|----------|
| HP / Health | **Resiliência Mental** | `ResilienciaMental.cs` |
| Take Damage | **Sofrer Trauma** | `SofrerTrauma()` |
| Heal | **Ancorar** | `Ancorar()` |
| Full Heal | **Estabilizar Completamente** | `EstabilizarCompletamente()` |
| Death / Game Over | **Colapso** | `IsColapso`, `ForcarColapso()` |
| Low HP State | **Pânico** | `IsPanico` |
| Dash | **Salto Dimensional** | `DimensionalLeap.cs` |
| Dodge | **Esquiva** | `Esquiva.cs` |
| Enemy | **Cultista Amarelo** | `CultistaFSM.cs` |
| Patrol | **Errante** | `CultistaState.Errante` |
| Alert | **Alerta** | `CultistaState.Alerta` |
| Chase | **Caça** | `CultistaState.Caca` |
| Sound Detection | **Estímulo Sonoro** | `ReceberEstimuloSonoro()` |
| Mana / Energy | _(não existe)_ | O jogo não tem sistema de mana |
| Level Up | _(não existe)_ | O jogo não tem progressão de nível |
| Inventory | _(não existe)_ | Evite propor inventário complexo |

## Contexto Narrativo

O protagonista **Damião** está preso nas **Ruínas Pálidas** (Ruins of Hali) dentro da **Cidade Pálida** (Carcosa), sob influência de **Hastur** (o Rei de Amarelo). A terminologia reflete o horror cósmico lovecraftiano: a "saúde" é a capacidade mental de resistir à loucura, não pontos de vida físicos.
