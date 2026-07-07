---
type: C# Script
title: SoundBroadcastService.cs
description: Event bus para o sistema de propagação sonora
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Core/Stealth/SoundBroadcastService.cs
tags: [core, stealth, sound, event-bus]
timestamp: 2026-07-07T11:00:00Z
---

# SoundBroadcastService

**Namespace:** `FavelaAmarela.Core.Stealth`  
**Tipo:** `public sealed class`

Um Event Bus simples que implementa o [Sistema de Propagação Sonora](../../systems/sound_propagation.md). 

## API Pública

- `Emitir(SomEmitido som)`: Injeta um som no sistema.
- `event Action<SomEmitido> OnSomEmitido`: Evento observado por inimigos.

## Struct `SomEmitido`
Payload `readonly struct` contendo a `Origem` (Vector2) e o `RaioEfetivo` do som.
