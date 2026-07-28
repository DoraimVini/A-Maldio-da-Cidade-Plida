---
type: C# Script
title: ScreenFader.cs
description: Funde a tela toda pra uma cor sólida e de volta, usado para mascarar teletransportes/eventos roteirizados
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/UI/ScreenFader.cs
tags: [runtime, ui]
timestamp: 2026-07-09T00:00:00Z
---

# ScreenFader

**Namespace:** `FavelaAmarela.Runtime.UI`
**Tipo:** `public sealed class` (herda de `MonoBehaviour`)

Funde a tela toda pra uma cor sólida (normalmente preto) e de volta — usado para mascarar teletransportes/eventos roteirizados sem precisar de um sistema de cutscene completo (ex.: a [queda Z4→Z5](../../systems/queda_z4_z5.md)). Sem regra de negócio: só interpola o alpha de uma `Image` full-stretch.

## API Pública
- `FadeTo(float alvo, float duracao)` (`IEnumerator`): interpola o alpha até `alvo` (0..1) ao longo de `duracao` segundos, via `Mathf.Lerp`. Use com `yield return`.
