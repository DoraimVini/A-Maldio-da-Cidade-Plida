---
type: C# Script
title: DanoFlutuante.cs
description: Número de dano flutuante em world space (diagnóstico visual)
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Combat/DanoFlutuante.cs
tags: [runtime, combat, feedback]
timestamp: 2026-07-30T00:00:00Z
---

# DanoFlutuante

**Namespace:** `FavelaAmarela.Runtime.Combat`
**Tipo:** `public sealed class` (`MonoBehaviour`, criado só por código)

Número que sobe e desvanece na posição do alvo. **Natureza provisória:** é um
**diagnóstico visual** pedido enquanto não há animações de golpe/impacto — o jeito mais direto
de confirmar que dano, mitigação e cadência funcionam. Substituível por VFX diegético depois,
sem afetar o Core.

Cores distintas por alvo (Cultista amarelo-pálido, Damião vermelho, Abdul azulado).

> **Gotcha Unity 6:** usa `TextMesh` legado de propósito (world space sem Canvas nem assets
> importados). A fonte built-in é **`LegacyRuntime.ttf`** — o nome antigo (`Arial.ttf`) faz
> `Resources.GetBuiltinResource` **lançar** `ArgumentException`, não retornar null. Blindado
> com try/catch para o diagnóstico nunca derrubar o combate. Travado por `FonteBuiltinTests`.
