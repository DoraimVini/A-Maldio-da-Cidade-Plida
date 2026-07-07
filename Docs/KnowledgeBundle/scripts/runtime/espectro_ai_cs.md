---
type: C# Script
title: EspectroAI.cs
description: Bridge que traduz a EspectroFSM em visual e movimento
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldição%20da%20Cidade%20Pálida/Assets/Scripts/Enemies/EspectroAI.cs
tags: [runtime, enemies, spectral]
timestamp: 2026-07-07T17:00:00Z
---

# EspectroAI

**Namespace:** `FavelaAmarela.Runtime.Enemies`
**Tipo:** `public sealed class` (herda de `MonoBehaviour`)

Adapter que injeta a [EspectroFSM](../core/espectro_fsm_cs.md) em `Awake()`. Diferente da `CultistaAI` (que decide sozinha, reagindo a som), esta classe é **dirigida externamente** — não tem percepção própria.

## API Pública
- `Manifestar()`: Latente → Manifestando (materializa visualmente).
- `IniciarCerco(Vector2 alvo)`: Manifestando → Cercando (passa a avançar até `alvo` no `FixedUpdate`, parando a `distanciaParada` dele).

## Visual
Cor/alpha do `SpriteRenderer` muda por estado: invisível em Latente, tonalidade amarelo-espectral semi-transparente ao manifestar (mesmo padrão de "cor por estado" da `CultistaAI`, sem interpolação gradual).

## Dependências e Relacionamentos
- [EspectroFSM (POCO)](../core/espectro_fsm_cs.md)
- Instanciada e dirigida por [CercoZ4Cutscene](cerco_z4_cutscene_cs.md).
