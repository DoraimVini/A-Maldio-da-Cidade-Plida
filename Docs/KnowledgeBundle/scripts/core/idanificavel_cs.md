---
type: C# Script
title: IDanificavel.cs
description: Contrato de qualquer entidade que pode receber um golpe de arma
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Core/Combat/IDanificavel.cs
tags: [core, combat, contrato]
timestamp: 2026-07-30T00:00:00Z
---

# IDanificavel

**Namespace:** `FavelaAmarela.Core.Combat`
**Tipo:** `public interface`

Desacopla o resolvedor de golpe (`MaoFisicaBridge`) de tipos concretos. Antes ele só
reconhecia o `CultistaAI`, o que tornava todo o resto imune "de graça".

- `EhAparicaoPrimordial` — Aparições Primordiais (bosses) são **imunes a crítico de furtividade**: a furtividade serve para chegar até a luta, não para resolvê-la.
- `ReceberGolpe(ArmaResult)` — aplica dano + efeitos do golpe.

A **Coisa do Cemitério é imortal justamente por não implementar esta interface** — não é um
caso especial no código, é ausência de contrato.
