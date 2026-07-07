---
type: Test Pattern
title: Padrões de Teste POCO (NUnit EditMode)
description: Como criar testes automatizados que instanciam POCOs diretamente, sem cena nem MonoBehaviour.
tags: [testing, nunit, editmode, poco, qa]
timestamp: 2026-07-07T11:00:00Z
---

# Padrões de Teste POCO

## Princípio

Toda lógica em `Core/` é testável **sem a Unity rodando**. Os testes vivem em `Assets/Tests/EditMode/` e instanciam os POCOs diretamente via `new`.

## Testes Existentes

| Teste | POCO Testado |
|-------|-------------|
| `ResilienciaMentalTests.cs` | `ResilienciaMental` |
| `CultistaFSMTests.cs` | `CultistaFSM` |
| `PatrolRouteTests.cs` | `PatrolRoute` |
| `SoundBroadcastServiceTests.cs` | `SoundBroadcastService` |
| `GameLoopStateMachineTests.cs` | `GameLoopStateMachine` |
| `DimensionalLeapTests.cs` | `DimensionalLeap` |
| `EsquivaTests.cs` | `Esquiva` |
| `PlayerStealthStateTests.cs` | `PlayerStealthState` |
| `EnvironmentStateTests.cs` | `EnvironmentState` |

## Padrão de Escrita (Arrange-Act-Assert)

```csharp
[Test]
public void SofrerTrauma_ReduzResiliencia()
{
    // Arrange
    var rm = new ResilienciaMental(100f, 25f);

    // Act
    rm.SofrerTrauma(30f);

    // Assert
    Assert.AreEqual(70f, rm.Atual);
}
```

## Regras

1. **Sem cena, sem MonoBehaviour** — instancie o POCO diretamente
2. **Um teste por comportamento** — não teste múltiplos cenários no mesmo método
3. **Teste transições de estado** — para FSMs, verifique o estado antes e depois
4. **Teste eventos** — assine o evento, execute a ação, verifique que foi disparado
5. **Toda lógica nova em Core/ DEVE ter teste correspondente**

## Pipeline

> Para detalhes do pipeline de QA (compilar → testar → commit), consulte a skill `favela-qa-pipeline` do Claude Code.
