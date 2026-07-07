---
type: C# Script
title: CercoZ4Cutscene.cs
description: Diretor da cutscene de cerco (Cultistas + Espectros) antes da queda Z4→Z5
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldição%20da%20Cidade%20Pálida/Assets/Scripts/GameLoop/CercoZ4Cutscene.cs
tags: [runtime, game-loop, cutscene, enemies]
timestamp: 2026-07-07T17:00:00Z
---

# CercoZ4Cutscene

**Namespace:** `FavelaAmarela.Runtime.GameLoop`
**Tipo:** `public sealed class` (herda de `MonoBehaviour`)

Instancia Cultistas e Espectros a partir de prefab, um pouco afastados de suas posições finais (`slotsCultista`/`slotsEspectro`, offsets relativos ao centro), e os aproxima de Damião. Só cuida da encenação visual deste momento — não é a IA normal desses inimigos.

## Comportamento por tipo de ator

- **Cultista:** o componente `CultistaAI` é desativado na instância (senão ele tentaria patrulhar); o próprio `CercoZ4Cutscene` interpola a posição do `Rigidbody2D` até o slot, via `Vector2.Lerp` ao longo de `duracaoAproximacao`.
- **Espectro:** dirigido via [EspectroAI](espectro_ai_cs.md) — `Manifestar()` logo ao instanciar, depois `IniciarCerco(alvo)` após `tempoManifestacao`. O próprio `EspectroAI` cuida do próprio movimento.

## API Pública
- `IEnumerator Tocar(Vector2 centro)`: toca a cutscene inteira ao redor de `centro` (posição de Damião no instante do gatilho).

## Dependências e Relacionamentos
- Chamado por [QuedaZ4Z5Trigger](queda_z4z5_trigger_cs.md), como o primeiro passo da sequência de queda.
- [EspectroAI](espectro_ai_cs.md), `CultistaAI` (componente desativado durante a cutscene).
