---
type: C# Script
title: ResilienciaBar.cs
description: Barra de HUD que reflete a Resiliência Mental de Damião, sem polling
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/UI/ResilienciaBar.cs
tags: [runtime, ui, combat]
timestamp: 2026-07-09T00:00:00Z
---

# ResilienciaBar

**Namespace:** `FavelaAmarela.Runtime.UI`
**Tipo:** `public sealed class` (herda de `MonoBehaviour`)

Barra de HUD que reflete a [Resiliência Mental](../../systems/resiliencia_mental.md) de Damião. Contrato de arquitetura explícito no próprio arquivo: não faz polling (reage só a `OnChanged`), não contém regra de negócio, e é "burra" — recebe a POCO via `Bind()` e não sabe de onde ela veio.

## API Pública
- `Bind(ResilienciaMental fonte)`: conecta a barra à POCO; idempotente (chama `Unbind()` internamente antes de trocar a fonte); sincroniza o visual imediatamente com o estado atual sem esperar o primeiro evento.
- `Unbind()`: desconecta do evento; seguro chamar mesmo sem bind ativo. Também chamado em `OnDisable()` para nunca deixar handler pendurado.

## Comportamento
- `HandleResilienciaChanged(ResilienciaChangedArgs args)`: atualiza `_fillAlvo` e reage a `EntrouEmPanico`/`SaiuDoPanico`/`EntrouEmColapso` — as transições já vêm prontas no payload, sem recalcular nada aqui.
- `Update()`: única lógica de polling permitida — interpola visualmente o `fillImage.fillAmount` até `_fillAlvo` via `Mathf.MoveTowards` (animação, não regra de negócio).
- Cores por estado (`corNormal`, `corPanico`, `corColapso`) e `colapsoAnimator.SetTrigger` para o efeito de entrada em Colapso.

## Testado por
`ResilienciaBarPlayTests.cs` (PlayMode — exceção ao padrão EditMode, ver [test_patterns.md](../../tests/test_patterns.md)), já que depende de componentes de UI reais da Unity.
