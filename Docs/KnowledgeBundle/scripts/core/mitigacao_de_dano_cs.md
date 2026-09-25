---
type: C# Script
title: MitigacaoDeDano.cs
description: Fórmula pura de mitigação de dano por defesa (subtrativa com piso)
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Core/Combat/MitigacaoDeDano.cs
tags: [core, combat, formula]
timestamp: 2026-07-30T00:00:00Z
---

# MitigacaoDeDano

**Namespace:** `FavelaAmarela.Core.Combat`
**Tipo:** `public static class` (função pura)

A conta do combate, isolada num único lugar testável — nada de `max()` espalhado pelo Runtime.

```
danoFinal = max(danoBruto × 0,15 , danoBruto − defesa)
```

**Subtrativa com piso:** a defesa subtrai um valor plano ("a armadura absorve X"), mas o piso
de 15% garante que **nenhuma pilha de defesa deixe alguém invulnerável** — sempre passa um mínimo.

**Simétrica:** a mesma função resolve o golpe do Cultista no Damião e o golpe da arma do
Damião no Cultista. Coberta por `MitigacaoDeDanoTests`.
