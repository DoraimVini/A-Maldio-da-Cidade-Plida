---
type: Architecture Decision
title: Padrão POCO + Adapter
description: Toda lógica de domínio vive em classes C# puras; MonoBehaviours são apenas adaptadores.
tags: [architecture, core-pattern, poco, testability]
timestamp: 2026-07-07T11:00:00Z
---

# Padrão POCO + Adapter (Regra de Ouro Arquitetural)

## Princípio

Toda lógica de domínio (dano, transição de estado, detecção sonora, cooldowns, fórmulas) vive em **classes C# puras (POCO)** no namespace `FavelaAmarela.Core.*`, sem herdar de `MonoBehaviour`.

Os `MonoBehaviour` em `FavelaAmarela.Runtime.*` e nas pastas `Player/`, `Enemies/`, `GameLoop/`, `Camera/`, `UI/` são **adaptadores**: leem input, instanciam/injetam os POCOs (via `.Bind()`), e sincronizam estado com o mundo visual. Nunca o contrário.

## Por que essa decisão?

1. **Testabilidade** — POCOs são testáveis via NUnit sem a Unity rodando (`new ResilienciaMental(100f, 25f)`)
2. **Portabilidade** — Lógica de domínio não depende do ciclo de vida da Unity
3. **Clareza** — Separação estrita entre "o que acontece" (Core) e "como mostra" (Runtime)

## Exemplos Canônicos

| POCO (Core) | Adapter (Runtime) |
|-------------|-------------------|
| `CultistaFSM` | `CultistaAI` |
| `ResilienciaMental` | `PlayerMovement` / `PlayerStealthState` / `ResilienciaBar` |
| `SoundBroadcastService` | (wired via `GameManager.Bind()`) |
| `GameLoopStateMachine` | `GameManager` |
| `Vitalidade` / `FichaDeAtributos` | `VitalidadeBridge` (+ `FichaAtributosConfig`) |
| `AbdulFSM` | `AbdulAlhazredAI` |
| `SeletorDeInteracao` | `DetectorDeInteracao` |
| `NavegadorDeOpcoes` | `PainelDeEscolha` |
| `SeguidorDeAlvo` | `YugNethAI` |
| `PlayerStateMachine` | `PlayerMovement` (injeta nos bridges) |

## Restrições no Core

- Zero dependência de `UnityEngine` além de `Vector2`, `Vector3` e `Mathf` (estritamente para cálculo)
- Nada de `MonoBehaviour`, `ScriptableObject`, `GameObject`, `Transform`
- Classe `sealed` por padrão
- Estado exposto via propriedades somente-leitura; mutação só via métodos explícitos

## Regras para Agentes

Ao criar uma nova mecânica:
1. Implemente a lógica em `Assets/Scripts/Core/<Domínio>/`
2. Crie o adapter em `Assets/Scripts/<Camada>/` (Player, Enemies, UI, etc.)
3. O adapter instancia o POCO em `Awake()` ou recebe via `.Bind()`
4. Crie um teste NUnit em `Assets/Tests/EditMode/` instanciando o POCO diretamente
