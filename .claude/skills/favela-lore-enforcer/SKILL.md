---
name: favela-lore-enforcer
description: Translates generic game development jargon into the localized diegetic terminology for Favela Amarela. Use when writing documentation, tooltips, or naming abilities.
---

# Favela Amarela - Lore Enforcer

## Objective
To eliminate generic RPG/game jargons and replace them with terms that fit the "Cosmic Horror / Brasilis / King in Yellow" aesthetics of the game.

## Glossary & Terminology

| ❌ Generic Term | ✅ Favela Amarela Term | Context |
|---|---|---|
| HP / Health Points | **Lucidez** ou **Resiliência Mental** | The player's sanity/health. Dropping to zero means falling to the Yellow Curse. |
| Magic / Spell | **Salto Dimensional** / **Anomalia** | Supernatural actions tied to the distortion of Carcosa. |
| Enemy / Monster | **Cultista Amarelo** / **Entidade de Carcosa** / **Eco** | The corrupted inhabitants or manifestations of the Yellow King. |
| Boss | **Vulto** / **Aparição Primordial** | Major enemies. |
| Level Up / Upgrade | **Aprofundamento** / **Revelação** | Gaining power means understanding more of the cosmic horror. |
| Mana / Energy | **Ectoplasma** / **Trauma** | Resource used for abilities. |
| Healing | **Ancoragem** / **Estabilização** | Recovering Lucidez. |

## Usage
When generating `ScriptableObject` descriptions, ability names, or any text visible in-game, MUST strictly follow this glossary. E.g., instead of `Heals 20 HP`, write `Restaura 20 de Lucidez`. Emphasize the psychological and dimensional horror.
