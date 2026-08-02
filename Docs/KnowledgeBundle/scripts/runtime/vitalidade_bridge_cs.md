---
type: C# Script
title: VitalidadeBridge.cs
description: Adaptador de vitalidade de um ator — ponto único da mitigação por defesa
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Combat/VitalidadeBridge.cs
tags: [runtime, combat]
timestamp: 2026-07-30T00:00:00Z
---

# VitalidadeBridge

**Namespace:** `FavelaAmarela.Runtime.Combat`
**Tipo:** `public sealed class` (`MonoBehaviour`, implementa `IDanificavel`)

Instancia a [Vitalidade](../core/vitalidade_cs.md) a partir da `FichaAtributosConfig` e é o
**único ponto** onde o dano físico recebido passa pela [mitigação por Defesa](../core/mitigacao_de_dano_cs.md).

- `ReceberDanoFisico(danoBruto)` — entrada do golpe corpo-a-corpo do Cultista
- `ReceberGolpe(ArmaResult)` — entrada de golpe de arma (`IDanificavel`)
- `IgnorarDano` — usado pelo `GameManager` em sequências roteirizadas (Damião não pode morrer no meio de uma cutscene)
- Eventos: `OnDanoSofrido(float)`, `OnAbatido`

Usado pelo Damião e pelo Yug-Neth. Spawna [números de dano flutuantes](dano_flutuante_cs.md)
quando configurado.
