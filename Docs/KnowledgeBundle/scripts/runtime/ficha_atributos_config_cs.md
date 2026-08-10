---
type: C# Script
title: FichaAtributosConfig.cs
description: ScriptableObject que autora a ficha de atributos de uma unidade
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Combat/FichaAtributosConfig.cs
tags: [runtime, combat, scriptableobject]
timestamp: 2026-07-30T00:00:00Z
---

# FichaAtributosConfig

**Namespace:** `FavelaAmarela.Runtime.Combat`
**Tipo:** `public sealed class` (`ScriptableObject`, `[CreateAssetMenu]`)

Autoria em asset da [FichaDeAtributos](../core/ficha_de_atributos_cs.md) — um asset por tipo de
unidade (`Ficha_Cultista`, `Ficha_Damiao`, `Ficha_Abdul`, `Ficha_YugNeth`), editável no Inspector.

`CriarFicha()` produz o POCO, com clamp defensivo para nunca estourar a validação por um asset
mal preenchido (Regra de Ouro 7).

**Balancear é editar asset, não código** — ver a tabela em [Ficha de Atributos](../../systems/ficha_de_atributos.md).
