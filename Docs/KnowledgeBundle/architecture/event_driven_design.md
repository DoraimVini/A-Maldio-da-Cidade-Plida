---
type: Architecture Decision
title: Design Orientado a Eventos
description: Camadas de UI/áudio/câmera observam eventos C#, nunca fazem polling.
tags: [architecture, events, decoupling, performance]
timestamp: 2026-07-07T11:00:00Z
---

# Design Orientado a Eventos

## Princípio

Toda comunicação entre camadas usa `event Action` / `event Action<T>`. Nenhuma camada de UI, áudio ou câmera faz polling de estado a cada frame.

## Padrão

```csharp
// No POCO (Core) — dispara evento
public event Action<ResilienciaChangedArgs> OnChanged;

// No Adapter (UI) — observa
private void OnEnable()  => _rm.OnChanged += HandleChanged;
private void OnDisable() => _rm.OnChanged -= HandleChanged;
```

## Regras de Performance

- Para hot paths (combate, frames de gameplay), use `readonly struct` nos argumentos do evento para **evitar alocação de heap**
- Exemplos existentes: `SomEmitido`, `ResilienciaChangedArgs`
- Nunca use classes como event args em loops de gameplay

## Eventos Existentes no Projeto

| Classe | Evento | Args |
|--------|--------|------|
| `ResilienciaMental` | `OnChanged` | `ResilienciaChangedArgs` (readonly struct) |
| `CultistaFSM` | `OnStateChanged` | `(CultistaState anterior, CultistaState novo)` |
| `GameLoopStateMachine` | `OnStateChanged` | `(GameState anterior, GameState atual)` |
| `SoundBroadcastService` | `OnSomEmitido` | `SomEmitido` (readonly struct) |
