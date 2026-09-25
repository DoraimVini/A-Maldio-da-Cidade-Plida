---
type: C# Script
title: BarraEnferrujada.cs
description: Arma física mundana com chance de atordoamento por golpe, mesma usada pelos Cultistas Amarelos
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Core/Abilities/BarraEnferrujada.cs
tags: [core, abilities, combat]
timestamp: 2026-07-09T00:00:00Z
---

# BarraEnferrujada

**Namespace:** `FavelaAmarela.Core.Abilities`
**Tipo:** `public sealed class` (implementa [IArma](iarma_cs.md))

POCO da família "Barra Enferrujada" — a mesma arma mundana que os Cultistas Amarelos usam contra Damião. Golpe pesado e simples: cada acerto tem uma *chance* de atordoar o alvo (nunca uma garantia). Não implementa `IAnomalyPower` pois não distorce Carcosa nem custa Resiliência Mental.

## API Pública

### Construtor
- `BarraEnferrujada(float duration = 0.3f, float cooldown = 0.6f, float probabilidadeAtordoar = 0.35f, float duracaoAtordoamento = 2f, Func<double> amostraAleatoria = null)`

### Métodos
- `CanActivate(float timeSinceLastUse)`: `true` se `timeSinceLastUse >= cooldown`.
- `Execute()`: sorteia atordoamento via `amostraAleatoria()` contra `probabilidadeAtordoar` e retorna `ArmaResult`.

## RNG testável
`amostraAleatoria` é um `Func<double>` injetável em `[0, 1)` — permite testes determinísticos (`() => 0.0` força atordoar, `() => 0.99` força não atordoar) sem depender de `System.Random`/`UnityEngine.Random` real. Se omitido, usa uma instância `Random` padrão compartilhada.
