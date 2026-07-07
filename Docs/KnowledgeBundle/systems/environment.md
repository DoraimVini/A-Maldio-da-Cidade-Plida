---
type: Game System
title: Estado do Ambiente
description: Sistema que modela o estado corrente do mundo de Carcosa e suas zonas de anomalia.
tags: [environment, world, anomaly, atmosphere]
timestamp: 2026-07-07T11:00:00Z
---

# Estado do Ambiente

O mundo de Carcosa é dividido em zonas com diferentes níveis de anomalia. O `EnvironmentState` (Core) modela o estado corrente do ambiente que afeta outros sistemas.

## Conceito

Diferentes áreas do mapa possuem diferentes intensidades de influência de Hastur/Carcosa. Zonas de alta anomalia:
- Drenam [Resiliência Mental](resiliencia_mental.md) passivamente
- Alteram comportamento dos [Cultistas](cultista_ai.md)
- Modificam a atmosfera visual e sonora (via adapters)

## Integração

- Alimenta o dreno passivo de RM (ver "Fatores de Dreno" em [Resiliência Mental](resiliencia_mental.md))
- Pode alterar raio efetivo de [Propagação Sonora](sound_propagation.md) (sons viajam mais longe em zonas anômalas)
- Adapters visuais (shaders, iluminação) observam mudanças de estado
