---
type: C# Script
title: FichaDeAtributos.cs
description: Os 5 atributos base de toda unidade do jogo
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Core/Combat/FichaDeAtributos.cs
tags: [core, combat, atributos]
timestamp: 2026-07-30T00:00:00Z
---

# FichaDeAtributos

**Namespace:** `FavelaAmarela.Core.Combat`
**Tipo:** `public sealed class` (POCO imutável)

**Toda unidade tem uma ficha** — Cultista, Damião, Abdul, Yug-Neth. Imutável: valida na
construção e nunca muda depois. Regras e balanceamento em [Ficha de Atributos](../../systems/ficha_de_atributos.md).

| Atributo | Canal | Papel |
|---|---|---|
| `VitalidadeMax` | — | Teto da [Vitalidade](vitalidade_cs.md) |
| `Ataque` | Físico | Dano bruto do golpe corpo-a-corpo |
| `Defesa` | Físico | Mitiga dano físico recebido |
| `Conjuracao` | Anômalo | Dano bruto das magias (0 se não conjura) |
| `ResistenciaAnomala` | Anômalo | Mitiga dano de conjuração |

O Core **não conhece `ScriptableObject`**: o asset de autoria (`FichaAtributosConfig`, Runtime)
nasce por cima deste POCO, nunca o contrário.
