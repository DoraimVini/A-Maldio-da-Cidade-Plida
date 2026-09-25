---
type: C# Script
title: BarraDeAcoes.cs
description: Slots de arma e habilidade no HUD, com indicador de recarga
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/UI/BarraDeAcoes.cs
tags: [runtime, ui, hud, combate]
timestamp: 2026-07-30T00:00:00Z
---

# BarraDeAcoes

**Namespace:** `FavelaAmarela.Runtime.UI`
**Tipo:** `public sealed class` (`MonoBehaviour`)

Mostra a arma equipada na Mão Física e a habilidade dela, com **indicador de recarga**
(cooldown) por slot. Fica vazia enquanto Damião está desarmado — o que também comunica ao jogador
que ele ainda não achou o baú.

Observa o `MaoFisicaBridge`; os nomes exibidos são os diegéticos da arma
("Cravo de Aklo" / "Fincar o Aklo"), conforme a skill `favela-lore-enforcer`.
