---
type: C# Script
title: QuedaZ4Z5Trigger.cs
description: Orquestra a sequência de cerco + colapso do chão da Zona 4 para a Zona 5
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldição%20da%20Cidade%20Pálida/Assets/Scripts/GameLoop/QuedaZ4Z5Trigger.cs
tags: [runtime, game-loop, level-transition, cutscene]
timestamp: 2026-07-07T17:00:00Z
---

# QuedaZ4Z5Trigger

**Namespace:** `FavelaAmarela.Runtime.GameLoop`
**Tipo:** `public class` (herda de `MonoBehaviour`)

> ⚠️ **Componente sem instância na cena atual (2026-07-30).** A queda Z4→Z5 foi removida da Tumba de Alhazred, que virou uma dungeon única e fechada. O script continua no projeto (pode ser reaproveitado noutra fase), mas não está mais instanciado em `Tumba_De_Alhazred.unity`.

Trigger (`Collider2D` + `CompareTag("Player")`) que dispara a sequência completa da queda Z4 → Z5, uma única vez (`_disparado`). Mirror do padrão de `ColapsoTrigger`, mas com uma corrotina de efeitos em vez de uma ação imediata.

## Sequência (`SequenciaDeQueda`)

1. Trava `PlayerMovement` e zera a velocidade.
2. Se `cerco` estiver atribuído: toca `CercoZ4Cutscene.Tocar(rb.position)` e espera `pausaTensaoDuration` (tensão antes do tremor).
3. `IsometricCameraController.Shake(shakeDuration, shakeMagnitude)`.
4. `ScreenFader.FadeTo(1, fadeOutDuration)` (fade pra preto).
5. Teleporta `rb.position` para `destino` (marcador na Zona 5).
6. Aguarda `blackHoldDuration`.
7. `ScreenFader.FadeTo(0, fadeInDuration)` (fade de volta).
8. Reativa `PlayerMovement`.

O campo `cerco` é opcional — se nulo, a queda acontece sem a cutscene de cerco (só o colapso).

## Dependências e Relacionamentos
- [CercoZ4Cutscene](cerco_z4_cutscene_cs.md) (opcional, cerco).
- `ScreenFader`, `IsometricCameraController` (efeitos, sem regra de negócio própria).
- `FavelaAmarela.Level.Runtime.LevelBlockoutGenerator` — gera a barreira anômala que impede o retorno da Zona 5 para a Zona 4.
