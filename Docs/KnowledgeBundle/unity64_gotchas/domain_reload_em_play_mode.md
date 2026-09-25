---
type: Unity Gotcha
title: Domain Reload em Play Mode — a cascata de NullReference que parece bug de código
description: Editar script com o Play rodando zera todo POCO criado em Awake. O sintoma são dezenas de NREs em Update/FixedUpdate que não têm nada a ver com o código.
tags: [unity, play-mode, domain-reload, awake, poco, debugging]
---

# Domain Reload em Play Mode

## O sintoma

Dezenas de `NullReferenceException` ao mesmo tempo, em scripts sem relação entre si, sempre
em `Update`/`FixedUpdate`:

```
NullReferenceException  YugNethAI.FixedUpdate ()          → _seguidor
NullReferenceException  PlayerMovement.FixedUpdate ()     → _fsm
NullReferenceException  CoisaDoCemiterioAI.FixedUpdate () → _fsm
NullReferenceException  CongelamentoBridge.Update ()      → _acumulo
NullReferenceException  DetectorDeInteracao.AtualizarAlvo () → buffers
```

Junto disso, sintomas de gameplay que parecem bugs independentes:
- Damião **não morre** (a `Vitalidade`/`ResilienciaMental` do `GameManager` foram zeradas)
- A **arma não aparece equipada** ao trocar de cena (estado da `MaoFisicaBridge` perdido)
- O **menu não responde** (a `StateMachine` do `GameManager` virou `null`)

## A causa

**Editar um script enquanto o Play mode está rodando.** A Unity recompila e faz *domain
reload* no meio da execução. No log do Editor:

```
Reloading assemblies after finishing script compilation.
Reloading assemblies after forced synchronous recompile.
Begin MonoManager ReloadAssembly
```

No reload, os `MonoBehaviour` **sobrevivem** (a Unity serializa e restaura), mas:

1. **Campos privados que são POCO puro viram `null`** — objetos C# comuns não são
   serializáveis pela Unity.
2. **`Awake()` NÃO roda de novo** — o objeto já existe, então não há nova inicialização.

Resultado: todo POCO criado em `Awake` fica nulo para sempre, e `Update`/`FixedUpdate`
continuam rodando contra o vazio.

> **Este projeto é especialmente vulnerável** porque a arquitetura é POCO + adaptador
> (`CLAUDE.md` §2): *por design*, quase todo `MonoBehaviour` guarda um POCO criado em `Awake`
> (`_fsm`, `_acumulo`, `_seguidor`, `Resiliencia`, `StateMachine`...). Quanto mais fiel à
> arquitetura, maior a cascata.

## A correção

**Não é código. É procedimento:**

1. **Pare o Play mode** antes de editar qualquer script.
2. Deixe a Unity terminar de compilar.
3. Só então dê Play de novo.

## Como reconhecer que é isto, e não bug de verdade

Antes de caçar o bug, cheque estas três marcas:

- **Muitos scripts sem relação** falhando no mesmo frame.
- O campo nulo é sempre **POCO criado em `Awake`**, nunca referência do Inspector (essas
  sobrevivem, porque são serializadas).
- O log do Editor tem `Reloading assemblies` **logo antes** da primeira exceção.

Confirmando os três, **não perca tempo lendo o código**: pare o Play, recompile, rode de novo.

## Relacionado
- `CLAUDE.md` §2 — a divisão POCO/Unity que torna o projeto sensível a isto
