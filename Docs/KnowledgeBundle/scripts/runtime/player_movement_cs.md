---
type: C# Script
title: PlayerMovement.cs
description: Bridge de movimentação, stealth e habilidades temporárias (Salto/Esquiva) do jogador
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldição%20da%20Cidade%20Pálida/Assets/Scripts/Player/PlayerMovement.cs
tags: [runtime, player, physics, input, stealth]
timestamp: 2026-07-07T16:00:00Z
---

# PlayerMovement

**Namespace:** `FavelaAmarela.Player`
**Tipo:** duas classes no mesmo arquivo — `PlayerStealthState` (POCO puro) e `PlayerMovement` (`MonoBehaviour`, Bridge)

## PlayerStealthState (POCO)
Guarda o modo de movimentação atual (`Sneaking`/`Walking`/`Running`) e calcula velocidade + raio de ruído emitido, incluindo o abafamento por tempestade (`AplicarAbafamentoTempestade`, reaproveitado pelo pulso sonoro da Esquiva). Ver [Propagação Sonora](../../systems/sound_propagation.md) e [Stealth](../../systems/stealth.md).

## PlayerMovement (Bridge)
Adapter que:
- Lê as actions `Move`/`Crouch`/`Sprint`/`SaltoDimensional`/`Esquiva` do Input System via `PlayerInput.actions.FindAction`, com fallback seguro (`Debug.LogWarning`) se alguma não existir.
- Aplica velocidade via `rb.linearVelocity` em `FixedUpdate` (nunca `MovePosition`), convertendo input WASD para direção isométrica via `ConvertToIsometric`.
- Escuta `AnomalyPowerBridge.OnDimensionalLeapActivated` e `EsquivaBridge.OnEsquivaActivada` para travar o movimento normal e aplicar a velocidade do Salto/Esquiva por uma duração fixa (via `Invoke`).
- Emite som periodicamente durante deslocamento normal (a cada 0.15s) e emite um pulso de som pontual na Esquiva — mesmo em modo Furtivo, de propósito (ver [Esquiva](../../systems/esquiva.md)).
- Desenha um gizmo do raio de ruído em `OnDrawGizmos` para depuração visual.

## Dependências e Relacionamentos
- [DimensionalLeap](../core/dimensional_leap_cs.md) via `AnomalyPowerBridge`.
- [Esquiva](esquiva_bridge_cs.md) via `EsquivaBridge`.
- [SoundBroadcastService](../core/sound_broadcast_cs.md) e `EnvironmentState`, injetados via `Bind()`.
- Afeta e é afetado pelo sistema de [Resiliência Mental](../../systems/resiliencia_mental.md) (thresholds de RM alteram velocidade).

## Nota de arquitetura
`PlayerStealthState` é um POCO (sem `MonoBehaviour`), mas vive no namespace `FavelaAmarela.Player` em vez de `FavelaAmarela.Core.*` — diverge da convenção estrita da seção 5 do `CLAUDE.md` raiz. Não é um bug funcional, apenas uma inconsistência de organização a considerar numa limpeza futura.
