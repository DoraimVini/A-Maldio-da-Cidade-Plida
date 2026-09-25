---
type: C# Script
title: PlayerMovement.cs
description: Bridge de movimentação, stealth e disparo das ações do jogador (Esquiva, Ataque, Habilidade da Arma)
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldição%20da%20Cidade%20Pálida/Assets/Scripts/Player/PlayerMovement.cs
tags: [runtime, player, physics, input, stealth, combate]
timestamp: 2026-07-30T00:00:00Z
---

# PlayerMovement

**Namespace:** `FavelaAmarela.Player`
**Tipo:** duas classes no mesmo arquivo — `PlayerStealthState` (POCO puro) e `PlayerMovement` (`MonoBehaviour`, Bridge)

## PlayerStealthState (POCO)
Guarda o modo de movimentação atual (`Sneaking`/`Walking`/`Running`) e calcula velocidade + raio de ruído emitido, incluindo o abafamento por tempestade (`AplicarAbafamentoTempestade`, reaproveitado pelo pulso sonoro da Esquiva). Ver [Propagação Sonora](../../systems/sound_propagation.md) e [Stealth](../../systems/stealth.md).

## PlayerMovement (Bridge)
Adapter que:
- Lê as actions `Move`/`Crouch`/`Sprint`/`Esquiva`/`Attack`/`HabilidadeArma` do Input System via `PlayerInput.actions.FindAction`, com fallback seguro (`Debug.LogWarning`) se alguma não existir.
- Aplica velocidade via `rb.linearVelocity` em `FixedUpdate` (nunca `MovePosition`), convertendo input WASD para direção isométrica via `ConvertToIsometric`.
- **Cria e distribui a `PlayerStateMachine`** (POCO, `Core.Player`): injeta a mesma instância no `EsquivaBridge` e no `MaoFisicaBridge` via `BindStateMachine()`. É essa FSM que garante **exclusão mútua** entre Esquiva, Ataque e Habilidade — nenhuma ação começa enquanto outra está em curso, e o input de movimento fica travado durante elas.
- Dispara as ações a partir do input: `EsquivaBridge.TryActivateEsquiva`, `MaoFisicaBridge.TryAtacar` (ataque básico) e `MaoFisicaBridge.TryUsarHabilidade` (habilidade da arma, cooldown próprio).
- No estado `Atacando`, zera a velocidade — o golpe trava Damião no lugar.
- Emite som periodicamente durante deslocamento normal (a cada 0.15s) e emite um pulso de som pontual na Esquiva — mesmo em modo Furtivo, de propósito (ver [Esquiva](../../systems/esquiva.md)).
- Expõe `MovimentoBloqueado`, usado pelo `PainelDeEscolha` para travar Damião durante um diálogo com escolha.
- Desenha um gizmo do raio de ruído em `OnDrawGizmos` para depuração visual.

## Dependências e Relacionamentos
- [Esquiva](esquiva_bridge_cs.md) via `EsquivaBridge`.
- [MaoFisicaBridge](mao_fisica_bridge_cs.md) — armas da Tumba (ataque básico + habilidade).
- `PlayerStateMachine` / `PlayerState` (`Core.Player`) — a FSM de ações exclusivas.
- [SoundBroadcastService](../core/sound_broadcast_cs.md) e `EnvironmentState`, injetados via `Bind()`.
- Afeta e é afetado pelo sistema de [Resiliência Mental](../../systems/resiliencia_mental.md) (thresholds de RM alteram velocidade).

> **Nota histórica (2026-07-30):** este bridge também tratava o **Salto Dimensional** — a
> troca de layer para intangibilidade, o vetor de dash e o `HandleFsmStateChanged` que
> restaurava a layer ao fim do Salto. Tudo isso foi removido junto com a habilidade.

## Nota de arquitetura
`PlayerStealthState` é um POCO (sem `MonoBehaviour`), mas vive no namespace `FavelaAmarela.Player` em vez de `FavelaAmarela.Core.*` — diverge da convenção estrita da seção 5 do `CLAUDE.md` raiz. Não é um bug funcional, apenas uma inconsistência de organização a considerar numa limpeza futura.
