---
type: C# Script
title: IAnomalyPower.cs
description: Interface para qualquer habilidade sobrenatural (Anomalia/Salto Dimensional) plugável
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Core/Abilities/IAnomalyPower.cs
tags: [core, abilities, interface]
timestamp: 2026-07-09T00:00:00Z
---

# IAnomalyPower

**Namespace:** `FavelaAmarela.Core.Abilities`
**Tipo:** `public interface` (+ `public struct PowerResult`)

Contrato base para qualquer habilidade sobrenatural (Anomalia) usada na Mão Anômala de Damião — o mecanismo de composição-sobre-herança citado no CLAUDE.md §4 para poderes plugáveis.

> ⚠️ **Sem implementações hoje (2026-07-30).** A única classe que implementava esta interface era o `DimensionalLeap`, e o Salto Dimensional foi **integralmente removido do jogo**. A interface foi **mantida por decisão explícita do Vini**: continua sendo o contrato para poderes anômalos futuros (e o padrão que a árvore de habilidades deve seguir). Não apagar por parecer código morto.

## API Pública

### `IAnomalyPower`
- `PowerName` (`string`, readonly): nome diegético da habilidade.
- `CanActivate(float currentResilience, float timeSinceLastUse)`: valida se a habilidade pode ser ativada agora (custo de Resiliência Mental + cooldown).
- `Execute(float currentResilience)`: executa o poder e retorna um `PowerResult`.

### `PowerResult` (struct)
- `Success` (`bool`)
- `DurationSeconds` (`float`)
- `CooldownSeconds` (`float`)
- `ResilienceCost` (`float`)

## Diferença para `IArma`
`IAnomalyPower` é para poderes que custam Resiliência Mental e distorcem Carcosa (Mão Anômala). Armas físicas mundanas (Mão Física) usam o contrato irmão [IArma](iarma_cs.md), sem custo de recurso.
