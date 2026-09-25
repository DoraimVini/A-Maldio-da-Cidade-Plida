---
type: C# Script
title: GameManager.cs
description: Composition root — instancia todos os POCOs de domínio e injeta nos adapters via Bind()
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/GameLoop/GameManager.cs
tags: [runtime, gameloop, composition-root]
timestamp: 2026-07-09T00:00:00Z
---

# GameManager

**Namespace:** `FavelaAmarela.Runtime.GameLoop`
**Tipo:** `public class` (herda de `MonoBehaviour`, singleton `Instance`, `[DefaultExecutionOrder(-100)]`)

A raiz de composição/injeção de dependências do projeto, citada no CLAUDE.md §2 ("via métodos `.Bind()`, ver `GameManager.cs`"). Em `Awake()` instancia todos os POCOs de domínio e os injeta nos adapters correspondentes. `DefaultExecutionOrder(-100)` garante que `Instance` esteja pronto antes do `Awake`/`OnEnable` de qualquer outro script da cena (ex.: [CultistaAI](cultista_ai_cs.md) se inscreve em `GameManager.Instance.SoundBroadcaster` no próprio `OnEnable`).

## POCOs instanciados (Core)
- `StateMachine` ([GameLoopStateMachine](../core/game_loop_sm_cs.md))
- `Resiliencia` ([ResilienciaMental](../core/resiliencia_mental_cs.md), via `ComThresholdFracional`)
- `SoundBroadcaster` ([SoundBroadcastService](../core/sound_broadcast_cs.md))
- `Environment` ([EnvironmentState](../core/environment_state_cs.md))

## `InjetarDependencias()`
Busca (via `FindAnyObjectByType`) e faz `Bind()`/injeção nos adapters: `HUDController`, `PlayerMovement`, [TempestadeAmbiente](tempestade_ambiente_cs.md), [TempestadeVisualOverlay](tempestade_visual_overlay_cs.md), e injeta o `SoundBroadcaster` em todos os inimigos da cena.

Além do bootstrap, expõe **registros pontuais em runtime** — coisas que não existem no `Awake`: `RegistrarYugNeth(YugNethAI)`, chamado quando o companheiro é libertado por Abdul, que assina `OnYugNethAbatido` para encerrar a run com `TipoDeDerrota.EscoltaPerdida`.

## Reações a eventos Core
- `StateMachine.OnStateChanged` → controla `Time.timeScale` e ativa/desativa telas (pause, transição de fase, gameplay root).
- `Resiliencia.OnChanged` → se `args.EntrouEmColapso`, transiciona a state machine para `GameState.Colapso`.

## API Pública
- `TriggerTransicaoDeFase()`: chamado por [TransicaoDeFaseTrigger](transicao_de_fase_trigger_cs.md) (renomeado de `TriggerVitoria` em 2026-07-28).
- `OnDestroy()`: desinscreve os handlers e limpa `Instance` — evita handler pendurado após a cena descarregar.
