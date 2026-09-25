---
type: C# Script
title: Vitalidade.cs
description: Recurso de vida corpórea (a carne) — distinto da Resiliência Mental
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Core/Combat/Vitalidade.cs
tags: [core, combat, recurso]
timestamp: 2026-07-30T00:00:00Z
---

# Vitalidade

**Namespace:** `FavelaAmarela.Core.Combat`
**Tipo:** `public sealed class` (POCO puro)

Vida **física** de um ator (Cultista, Damião, Aparição Primordial). Zerá-la é ser
**abatido** (morte corpórea) — distinto do **Colapso**, que é a [Resiliência Mental](resiliencia_mental_cs.md)
a zero. Dois vetores de derrota separados. Regras de design em [Vitalidade](../../systems/vitalidade.md).

## API Pública
- `Max` / `Atual` / `Percentual` — estado somente-leitura
- `EstaAbatido` — `Atual <= 0`
- `Ferir(valor)` — dano físico, clampado a zero
- `Curar(valor)` — cura física, clampada ao máximo
- `Restaurar(valor)` — reconstrução de estado a partir de save (não é dano/cura diegético)
- `OnChanged(VitalidadeChangedArgs)` — `readonly struct` (sem alocação em hot path). A flag `AcabouDeAbater` é **true uma única vez**, no evento em que cruza para zero — é o gatilho de morte.

Espelha deliberadamente o contrato da `ResilienciaMental`: estado só-leitura, mutação por
métodos explícitos, evento como única superfície de saída.
