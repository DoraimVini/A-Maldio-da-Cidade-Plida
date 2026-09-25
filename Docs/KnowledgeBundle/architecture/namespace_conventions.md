---
type: Architecture Decision
title: Convenções de Namespace
description: Mapa de namespaces, Assembly Definitions e onde cada tipo de código vive.
tags: [architecture, namespaces, organization]
timestamp: 2026-07-07T11:00:00Z
---

# Convenções de Namespace

## Mapa de Namespaces

| Namespace | Pasta | Responsabilidade |
|-----------|-------|------------------|
| `FavelaAmarela.Core.Combat` | `Assets/Scripts/Core/Combat/` | Resiliência Mental (HP diegético) |
| `FavelaAmarela.Core.Enemies` | `Assets/Scripts/Core/Enemies/` | FSM e patrulha dos Cultistas |
| `FavelaAmarela.Core.Stealth` | `Assets/Scripts/Core/Stealth/` | Propagação sonora |
| `FavelaAmarela.Core.Abilities` | `Assets/Scripts/Core/Abilities/` | Esquiva, armas físicas (`IArma`/`IArmaComHabilidade`: Cravo de Aklo, Estilete de Irem, Alfanje de Alhazred, Mão Vazia) e o contrato `IAnomalyPower` (hoje sem implementações) |
| `FavelaAmarela.Core.GameLoop` | `Assets/Scripts/Core/GameLoop/` | Máquina de estados do loop do jogo |
| `FavelaAmarela.Core.Environment` | `Assets/Scripts/Core/Environment/` | Estado do ambiente |
| `FavelaAmarela.Runtime.*` | `Assets/Scripts/` (raiz Runtime) | Adaptadores MonoBehaviour |
| `FavelaAmarela.Player` | `Assets/Scripts/Player/` | Scripts do jogador |
| `FavelaAmarela.Tests.EditMode` | `Assets/Tests/EditMode/` | Testes NUnit (POCO puro) |

## Assembly Definitions

| Assembly | Arquivo | Depende de |
|----------|---------|------------|
| `FavelaAmarela.Runtime` | `Assets/Scripts/FavelaAmarela.Runtime.asmdef` | Core |
| `FavelaAmarela.Level` | `Assets/FavelaAmarela/Level/` | Runtime |
| `FavelaAmarela.Tests.EditMode` | `Assets/Tests/EditMode/` | Core, Runtime |
| `FavelaAmarela.Tests.PlayMode` | `Assets/Tests/PlayMode/` | Core, Runtime |

## Regras

- Novos domínios de lógica: `FavelaAmarela.Core.<NovoDomínio>`
- Novos adaptadores: `FavelaAmarela.Runtime.<NovoDomínio>` ou pasta dedicada em `Assets/Scripts/`
- `Core` **nunca** referencia `Runtime`; a dependência é unidirecional
