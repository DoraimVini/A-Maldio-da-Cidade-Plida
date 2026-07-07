---
type: Unity Gotcha
title: Física 2D — Best Practices
description: Padrões de uso correto de Rigidbody2D no contexto isométrico do projeto.
tags: [physics, rigidbody2d, fixedupdate, isometric]
timestamp: 2026-07-07T11:00:00Z
---

# Física 2D — Best Practices

## Configuração Padrão do Projeto

| Propriedade | Valor | Motivo |
|-------------|-------|--------|
| `gravityScale` | **0** | Isométrico 2D — não existe gravidade "para baixo" |
| `CollisionDetectionMode2D` | **Continuous** | Para atores que se movem (evita tunneling) |
| Movimento | `rb.linearVelocity = ...` em **FixedUpdate** | Não usar `MovePosition` nem `transform.position` |

## Padrão de Movimentação (convenção do projeto)

```csharp
// Em FixedUpdate — CORRETO
rb.linearVelocity = direction * speed;

// ERRADO — não usar
rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);
transform.position += direction * speed * Time.deltaTime;
```

## Por que `linearVelocity` e não `MovePosition`?

- `linearVelocity` integra naturalmente com o sistema de colisão 2D
- `MovePosition` pode causar problemas com colliders compostos no isométrico
- O projeto já usa essa convenção em `PlayerMovement.cs` — manter consistência
