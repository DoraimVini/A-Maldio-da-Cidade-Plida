---
type: C# Script
title: EspectroFSM.cs
description: Máquina de estados pura da manifestação roteirizada do Espectro
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldição%20da%20Cidade%20Pálida/Assets/Scripts/Core/Enemies/EspectroFSM.cs
tags: [core, enemies, fsm, spectral]
timestamp: 2026-07-07T17:00:00Z
---

# EspectroFSM

**Namespace:** `FavelaAmarela.Core.Enemies`
**Tipo:** `public sealed class`

Implementa a lógica de estados do [Espectro](../../systems/espectro.md): `Latente → Manifestando → Cercando`, só para frente. Diferente da `CultistaFSM` (reativa a estímulos sonoros, com timers e retrocesso), esta FSM é puramente sequencial e roteirizada — sem `Tick`, sem regra de tempo.

## API Pública

- `CurrentState`: estado atual.
- `TryTransition(EspectroState alvo)`: tenta avançar; retorna `false` sem efeito colateral se a transição não for permitida (fora de ordem ou retrocesso).
- `OnStateChanged`: evento disparado só em transições válidas.

## Dependências e Relacionamentos
- Consumida por [EspectroAI](../runtime/espectro_ai_cs.md).
