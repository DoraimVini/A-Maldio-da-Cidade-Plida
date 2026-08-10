---
type: C# Script
title: CultistaFSM.cs
description: Máquina de estados pura para o Cultista (5 estados, incluindo corpo-a-corpo)
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Core/Enemies/CultistaFSM.cs
tags: [core, enemies, fsm, ai, combate]
timestamp: 2026-07-30T00:00:00Z
---

# CultistaFSM

**Namespace:** `FavelaAmarela.Core.Enemies`  
**Tipo:** `public class`

A Máquina de Estados que implementa as regras da [IA do Cultista](../../systems/cultista_ai.md).

## API Pública

### Propriedades
- `CurrentState`: Enum `CultistaState` — **5 estados**: `Errante`, `Alerta`, `Caca`, `Atacar`, `Atordoado`
- `UltimaOrigemConhecida`: `Vector2?` — onde o som foi ouvido
- `AlvoAoAlcance`: `bool` — se o Damião está ao alcance de golpe (alimentado pelo adaptador)
- `TimeInState`: Tempo no estado atual
- `TimeSinceLastStimulus`: Tempo desde o último som ouvido

### Métodos Principais
- `ReceberEstimuloSonoro(origem, distancia, raioEfetivo)`: Injeta estímulos sonoros no sistema
- `AtualizarAlcanceDoAlvo(bool)`: Informa se o alvo está no corpo-a-corpo — é o gatilho de `Caca → Atacar`. **Proximidade física, não visão** (coerente com a percepção só-sonora do Cultista)
- `AtordoarPor(float duracaoSegundos)`: Interrompe qualquer estado (ex.: habilidade do Alfanje de Alhazred)
- `Tick(float dt)`: Deve ser chamado a cada frame pelo adapter para computar timeouts (8s de alerta, 10s de caça) e a cadência de golpes

### Eventos
- `OnStateChanged(anterior, novo)`: Notifica transições de estado
- `OnGolpeDesferido()`: Um golpe corpo-a-corpo saiu (cadência default 1,2 s). **A FSM não calcula dano** — o Runtime traduz isso em dano usando o `Ataque` da [ficha](../../systems/ficha_de_atributos.md) do Cultista, mitigado pela `Defesa` do alvo

### Detalhes do estado `Atacar`
- Ao entrar, o timer de cadência **reinicia do zero**: o primeiro golpe só sai após uma cadência completa (janela de telegrafo para o jogador recuar). Sair e reentrar no alcance **não** acumula progresso — não dá para "carregar" um golpe instantâneo.
- Enquanto o alvo está ao alcance, os timeouts sonoros não tiram o Cultista de `Atacar`.
