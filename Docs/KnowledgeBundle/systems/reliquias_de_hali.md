---
type: Game System
title: Relíquias de Hali
description: Documentação de design e mecânicas das 4 relíquias do Pilar 3.
tags: [reliquias, equipamentos, lore, passivas]
---

# As Relíquias de Hali

As relíquias são itens únicos da lore, forjados ou encontrados ao longo de Favela Amarela, que oferecem bônus narrativos e mecânicos. Elas se integram com o `InventoryManager` e modificam os atributos (via `ModificadorFixo`) e a matemática contínua (via `GerenciadorEfeitosPassivos`).

## As Quatro Relíquias (Pilar 3)

### 1. O Necronomicon (O Al-Azif)
- **Tipo:** Chave (`ItemType.Chave`)
- **Slot:** Nenhum (Item-chave mantido na `Mochila`)
- **Modificadores:**
  - `TraumaAnomalia`: Bônus de Dano (+10). Concedido ao `CombateManager` quando o jogador ataca.
  - `DrenoRM`: Custo passivo (+1). Drena constantemente a sanidade enquanto o item estiver na mochila, forçando o jogador a agir rápido ou se desfazer/guardar o livro.

### 2. Anel do Sinal Amarelo
- **Tipo:** Amuleto (`ItemType.Amuleto`)
- **Slot:** Anel (`EquipmentSlot.Anel`)
- **Modificadores:**
  - `Furtividade`: Bônus de Stealth (+0.3). Dificulta a detecção pelas sombras e sentinelas.
  - `RMMaxima`: Aumento de Sanidade Total (+20).

### 3. Elmo de Set
- **Tipo:** Armadura (`ItemType.Armadura`)
- **Slot:** Elmo (`EquipmentSlot.Elmo`)
- **Modificadores:**
  - `DefesaFisica`: Resistência estrutural (+5).
  - `VitMaxima`: Aumento de HP corpóreo (+15).

### 4. Patuá das Luas Gêmeas
- **Tipo:** Amuleto (`ItemType.Amuleto`)
- **Slot:** Amuleto (`EquipmentSlot.Amuleto`)
- **Modificadores:**
  - `RegenRM`: Regeneração passiva de sanidade (+1.5). Abate o dano constante da tempestade/escuridão.

## Arquitetura de Status

Atributos fixos (como `DefesaFisica`, `VitMaxima`, `RMMaxima`) são consultados e consolidados pela `VitalidadeBridge` ou `StatusDoJogador` quando o evento `OnEquipmentChanged` é disparado.

As **taxas e matemáticas de tempo** (como `RegenRM` e `DrenoRM`), bem como as verificações passivas na mochila (o caso único do Necronomicon), são geridas pelo `GerenciadorEfeitosPassivos` no `Update` do GameLoop, desonerando a lógica POCO da `ResilienciaMental`.

*Ref: [Inventário e Consumíveis](./inventario_e_consumiveis.md)*
