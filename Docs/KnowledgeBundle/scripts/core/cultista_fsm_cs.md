---
type: C# Script
title: CultistaFSM.cs
description: Máquina de estados pura para o Cultista
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Core/Enemies/CultistaFSM.cs
tags: [core, enemies, fsm, ai]
timestamp: 2026-07-07T11:00:00Z
---

# CultistaFSM

**Namespace:** `FavelaAmarela.Core.Enemies`  
**Tipo:** `public class`

A Máquina de Estados que implementa as regras da [IA do Cultista](../../systems/cultista_ai.md).

## API Pública

### Propriedades
- `CurrentState`: Enum `CultistaState` (Errante, Alerta, Caca)
- `UltimaOrigemConhecida`: `Vector2?` — onde o som foi ouvido
- `TimeInState`: Tempo no estado atual
- `TimeSinceLastStimulus`: Tempo desde o último som ouvido

### Métodos Principais
- `ReceberEstimuloSonoro(origem, distancia, raioEfetivo)`: Injeta estímulos no sistema
- `Tick(float dt)`: Deve ser chamado a cada frame pelo adapter para computar timeouts (8s de alerta, 10s de caça).

### Eventos
- `OnStateChanged(anterior, novo)`: Notifica transições de estado.
