---
type: C# Script
title: ResilienciaMental.cs
description: Classe central de gerenciamento de sanidade do protagonista
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Core/Combat/ResilienciaMental.cs
tags: [core, combat, state]
timestamp: 2026-07-07T11:00:00Z
---

# ResilienciaMental

**Namespace:** `FavelaAmarela.Core.Combat`  
**Tipo:** `public sealed class`

POCO que gerencia o estado da [Resiliência Mental](../../systems/resiliencia_mental.md) de Damião.

## API Pública

### Propriedades (readonly)
- `Max`: Teto da resiliência
- `Atual`: Valor corrente
- `ThresholdPanico`: Limiar absoluto de pânico
- `IsPanico`: `true` se `0 < Atual <= ThresholdPanico`
- `IsColapso`: `true` se `Atual <= 0`

### Métodos de Mutação
- `SofrerTrauma(float valor)`: Reduz a resiliência
- `Ancorar(float valor)`: Aumenta a resiliência
- `EstabilizarCompletamente()`: Restaura ao máximo
- `ForcarColapso()`: Zera a resiliência instantaneamente

### Eventos
- `OnChanged(ResilienciaChangedArgs)`: Disparado apenas quando ocorre uma alteração real (o clamp não absorveu tudo).

## O Payload `ResilienciaChangedArgs`

Struct `readonly` para evitar alocação. Traz o `ValorAnterior`, `ValorAtual`, e booleanos vitais:
- `EntrouEmPanico` / `SaiuDoPanico` / `EntrouEmColapso`
- Esses booleanos só são `true` no frame exato da transição de limiar, ideais para disparar sons e animações one-shot.
