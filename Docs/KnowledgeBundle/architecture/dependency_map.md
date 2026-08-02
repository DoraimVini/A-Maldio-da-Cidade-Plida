---
type: Architecture Decision
title: Mapa de Dependências
description: Grafo de dependências entre os domínios Core e suas camadas de adaptação.
tags: [architecture, dependencies, graph]
timestamp: 2026-07-07T11:00:00Z
---

# Mapa de Dependências

## Grafo de Domínios (Core)

```
SoundBroadcastService ──emite SomEmitido──▶ CultistaFSM
         │                                      │
         │                                      │ usa
         │                                      ▼
         │                                  PatrolRoute
         │
         ▼
   (Observadores do som: CultistaAI adapter)

ResilienciaMental ◀──trauma── (dano anômalo / horror)
Vitalidade        ◀──dano físico── MitigacaoDeDano ◀── FichaDeAtributos
AbdulFSM          ──eventos──▶ (escudo, invocações, derrota)
         │
         │ OnChanged
         ▼
   ResilienciaBar (UI)
   DynamicMusicController (Áudio)
   CameraShake (Câmera)

GameLoopStateMachine ──controla──▶ Todo o fluxo (Menu → Gameplay → Pausado → Colapso → TransicaoDeFase)
         │
         │ observa
         ▼
   ResilienciaMental.IsColapso ──dispara──▶ GameState.Colapso

EnvironmentState ──OnStormIntensityChanged──▶ TempestadeAmbiente
         ▲                                          │
         │ Tick()                                   ▼
   TempestadeOscilador                        TempestadeVisualOverlay (UI)

TempestadeZonaTrigger ──DefinirFaixa()──▶ EnvironmentState

CoisaDoCemiterioFSM ──estímulo (Farejando→AlvoPreciso)──▶ ResilienciaMental.ForcarColapso()
```

## Regra de Dependência Unidirecional

```
   ┌──────────────────┐
   │    Core (POCO)    │  ← Sem dependência de Unity (exceto Vector2/Mathf)
   └────────▲─────────┘
            │ depende de
   ┌────────┴─────────┐
   │  Runtime/Player   │  ← MonoBehaviours, adaptadores
   │  /Enemies/UI/etc  │
   └────────▲─────────┘
            │ depende de
   ┌────────┴─────────┐
   │     Tests         │  ← NUnit EditMode (instancia POCOs diretamente)
   └──────────────────┘
```

## Contratos entre Domínios

| Produtor | Consumidor | Contrato |
|----------|------------|----------|
| `SoundBroadcastService` | `CultistaFSM` | `SomEmitido` → `ReceberEstimuloSonoro()` |
| `ResilienciaMental` | UI, Áudio, Câmera | `OnChanged` → `ResilienciaChangedArgs` |
| `CultistaFSM` | `CultistaAI` | `OnStateChanged` → `(old, new)` |
| `IAnomalyPower` | `PlayerMovement` | `CanActivate()` / `Execute()` → `PowerResult` |
| `GameLoopStateMachine` | `GameManager` | `OnStateChanged` → `(anterior, atual)` |
| `EnvironmentState` | `TempestadeAmbiente`, `TempestadeVisualOverlay` | `OnStormIntensityChanged` → `float` |
| `CoisaDoCemiterioFSM` | `ResilienciaMental` (via adapter) | estímulo de proximidade → `ForcarColapso()` (mesmo contrato reaproveitado de `ColapsoTrigger`) |
