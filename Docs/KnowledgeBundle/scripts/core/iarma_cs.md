---
type: C# Script
title: IArma.cs
description: Contrato para armas físicas mundanas equipadas na Mão Física de Damião
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Core/Abilities/IArma.cs
tags: [core, abilities, combat, interface]
timestamp: 2026-07-09T00:00:00Z
---

# IArma

**Namespace:** `FavelaAmarela.Core.Abilities`
**Tipo:** `public interface` (+ `public readonly struct ArmaResult`)

Contrato para armas físicas mundanas equipadas na Mão Física de Damião — sem custo de Resiliência Mental, diferente de [IAnomalyPower](ianomaly_power_cs.md) (Mão Anômala). Cada família de arma implementa este contrato e define seu próprio "verbo de combate" (ex.: [BarraEnferrujada](barra_enferrujada_cs.md) atordoa por chance).

## API Pública

### `IArma`
- `NomeDaArma` (`string`, readonly): nome diegético da arma.
- `CanActivate(float timeSinceLastUse)`: só valida cooldown — arma física não tem custo de recurso.
- `Execute()`: executa o golpe e retorna um `ArmaResult`.

### `ArmaResult` (readonly struct)
- `Success` (`bool`)
- `DurationSeconds` (`float`)
- `CooldownSeconds` (`float`)
- `Atordoou` (`bool`): se este golpe específico atordoou o alvo (nem toda arma usa isso).
- `DuracaoAtordoamento` (`float`): duração do atordoamento, se `Atordoou` for `true`.

## Consumidor Runtime
Ligado à Mão Física via `MaoFisicaBridge` (Runtime) — ver [scripts/runtime](../runtime/mao_fisica_bridge_cs.md).
