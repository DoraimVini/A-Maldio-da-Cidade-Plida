---
type: Game System
title: Inventário e Consumíveis (Refatorado)
description: O inventário unificado de Damião, equipamentos, consumíveis e a fundação para relíquias.
tags: [inventario, itens, consumiveis, equipamentos, reliquias]
---

# Inventário e Consumíveis

> **Atualizado em 2026-08-03**
> O sistema antigo (`InventarioBridge`, `DefinicaoDeItem`, `ItemConfig`) foi totalmente expurgado em favor de uma arquitetura centralizada e orientada a dados.

## Forma: Enxuto, modular e orientado a eventos

Restrição do `CLAUDE.md` §1: **sem grind de itens**. A premissa de escassez e survival horror se mantém. O inventário é deliberadamente limitado, forçando decisões difíceis (ex: Escolher entre uma relíquia importante ou um item de cura).

## As novas peças arquiteturais

| Peça | Camada | Papel |
|---|---|---|
| `ItemDef` | Data (ScriptableObject) | **O que um item é** — ID, nome, descrição, tipo (`Amuleto`, `Arma`, etc.), modificadores. |
| `ItemInstance` | Core/Runtime | Instância de um item no inventário (referencia o `ItemDef` e guarda quantidade atual). |
| `BaseInventory` | Core | Lógica pura de contêiner. Limites, empilhamento, adição e remoção. Testável sem Unity. |
| `MainInventory` | Core | Herda de `BaseInventory`. É a Mochila do jogador (itens gerais e consumíveis). |
| `EquipmentInventory` | Core | Herda de `BaseInventory`. Controla os slots restritos (Arma, Elmo, Amuleto, Anel). Valida encaixes. |
| `InventoryManager` | Runtime (Singleton) | Dono do inventário global (`Mochila` e `Equipamentos`). Ponto central de save/load e eventos (`OnItemConsumed`). |

### Separação de Responsabilidades (Desacoplamento)

Diferente do sistema antigo, onde a UI e o loot dependiam de um Bridge rígido:
- **UI:** A `BarraDeItens` e o `HUDController` assinam eventos do `InventoryManager` (ex: `OnSlotChanged`). Não há dependência cíclica.
- **Consumo:** A UI chama `InventoryManager.ConsumirItem(indice)`. O `InventoryManager` consome a quantidade e dispara o evento `OnItemConsumed`.
- **Efeito no mundo:** Quem assina `OnItemConsumed` (ex: `VitalidadeBridge`) valida se o item possui efeito (como `Ancoragem`) e aplica no jogador.

## Regras que evitam bugs de progressão

1. **Testes isolados (POCO):** O `BaseInventory` e o `MainInventory` são exaustivamente testados sem Unity (NUnit EditMode), cobrindo falhas de empilhamento e limites estritos.
2. **Cópia segura:** Inserir itens ou recuperar via `GetSlot` retorna clones protegidos (`ItemInstance.Clone()`), impedindo manipulação indevida do estado original da pilha.
3. **Gerenciamento de Identidade (GUID):** O `ItemDef` armazena um ID persistente, permitindo que instâncias sejam serializadas para os saves de progresso.

## Conexões Atuais

- **Loot:** Baús (`BauDaTumba`) e drops (`ColetavelDeItem`) agora usam `ItemDef` e injetam diretamente no `InventoryManager.Instance.Mochila`.
- **Save/Load:** O `InventoryManager` possui `GerarSaveData()` e `CarregarSaveData()` compatíveis com a arquitetura do `GameManager`.

## Pendentes (Próximos Passos)

- **GerenciadorEfeitosPassivos:** Para resolver o processamento contínuo de relíquias e equipamentos (como Dreno de RM no escuro), o sistema demandará um intermediário (`GerenciadorEfeitosPassivos.cs`) para ler atributos do `EquipmentInventory` e injetar matemática contínua no `ResilienciaMental`.
- **Drop em Inimigos:** `ColetavelDeItem` precisa ser configurado nos chefes (ex: Byakhee).
