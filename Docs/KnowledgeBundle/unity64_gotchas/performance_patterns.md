---
type: Unity Gotcha
title: Padrões de Performance
description: Regras de alocação zero em hot paths, caching e event args.
tags: [performance, gc, allocation, hot-path]
timestamp: 2026-07-07T11:00:00Z
---

# Padrões de Performance

## Regra #1: Zero Alocação em Hot Paths

Em `Update()`, `FixedUpdate()` e `LateUpdate()`:
- **Proibido:** `new` (classes), `GetComponent<T>()`, `FindObjectOfType`, LINQ, `string` concatenation
- **Permitido:** `readonly struct` (stack-allocated), cached references, value types

## Regra #2: Cache em Awake/Start

```csharp
// CORRETO — cache no Awake
private Rigidbody2D _rb;
private void Awake() => _rb = GetComponent<Rigidbody2D>();

// ERRADO — GetComponent no Update
private void Update() => GetComponent<Rigidbody2D>().linearVelocity = ...;
```

## Regra #3: readonly struct para Event Args

Eventos disparados frequentemente (combate, movimentação) devem usar `readonly struct` para evitar alocação de heap:

```csharp
// CORRETO — stack-allocated
public readonly struct SomEmitido { ... }
public readonly struct ResilienciaChangedArgs { ... }

// ERRADO — alocação de heap toda vez
public class SomEmitidoArgs { ... }
```

## Regra #4: Preferir `sealed`

Classes `sealed` permitem devirtualização pelo JIT, melhorando performance de chamadas de método:

```csharp
public sealed class ResilienciaMental { ... }  // CORRETO
public class ResilienciaMental { ... }          // Evitar (a menos que herança seja necessária)
```
