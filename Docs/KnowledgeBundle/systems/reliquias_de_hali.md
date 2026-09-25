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
> **Atualizado em 2026-08-11: virou Artefato.** Deixou de ser `ItemType.Chave` — agora é
> `ItemType.Artefato` e vive num dos 4 slots do inventário de Artefatos, com passiva **e**
> habilidade ativa. Ver [artefatos.md](artefatos.md).

- **Tipo:** Artefato (`ItemType.Artefato`)
- **Slot:** Nenhum de corpo — ocupa um dos 4 slots de Artefato
- **Passiva (só enquanto equipado):** `TraumaAnomalia +15`
- **Habilidade ativa:** *Recitar o Aklo* — revela entidades através da parede (raio 10, 6 s).
  Custa 12 de RM, recarrega em 25 s.
- **Nota:** o `DrenoRM` passivo previsto aqui **não foi implementado**. A pressão de sanidade
  agora vem do **custo por uso** da habilidade, que é mais legível para o jogador do que um
  dreno silencioso — e não pune quem só carrega o tomo sem usá-lo. Reverter para dreno contínuo
  é decisão de design em aberto.

### 2. Anel do Sinal Amarelo
- **Tipo:** Amuleto (`ItemType.Amuleto`)
- **Slot:** Anel (`EquipmentSlot.Anel`)
- **Modificadores:**
  - `Furtividade`: Bônus de Stealth (+0.3). Dificulta a detecção pelas sombras e sentinelas.
  - `RMMaxima`: Aumento de Sanidade Total (+20).

### 3. Elmo de Set
> **Criado como asset em 2026-08-11**, junto com Peitoral e Grevas — ver
> [lore/reliquias_cosmicas.md](../lore/reliquias_cosmicas.md) §3. Ele **não** é a Coroa de Ossos
> renomeada (nota antiga corrigida): são itens distintos, e a Coroa é Artefato.

- **Tipo:** Armadura (`ItemType.Armadura`)
- **Slot:** Elmo (`EquipmentSlot.Elmo`)
- **Modificadores:**
  - `DefesaFisica`: Resistência estrutural (+5).
  - `VitMaxima`: Aumento de Vitalidade corpórea (+15).

### 4. Patuá das Luas Gêmeas
> **Atualizado em 2026-08-11: virou Artefato.** Saiu do slot Amuleto (que ficou sem nenhum item
> usando) e passou a ocupar um slot de Artefato, com passiva **e** ativa.

- **Tipo:** Artefato (`ItemType.Artefato`)
- **Slot:** Nenhum de corpo — ocupa um dos 4 slots de Artefato
- **Passiva (só enquanto equipado):** `RegenRM +1.5`. Abate o dreno constante da
  tempestade/escuridão.
- **Habilidade ativa:** *Canção de Cassilda* — Ancoragem imediata de 25 de RM. Sem custo,
  recarrega em 45 s.

## Arquitetura de Status

Atributos fixos (como `DefesaFisica`, `VitMaxima`, `RMMaxima`) são consultados e consolidados pela `VitalidadeBridge` ou `StatusDoJogador` quando o evento `OnEquipmentChanged` é disparado.

As **taxas e matemáticas de tempo** (como `RegenRM` e `DrenoRM`), bem como as verificações passivas na mochila (o caso único do Necronomicon), são geridas pelo `GerenciadorEfeitosPassivos` no `Update` do GameLoop, desonerando a lógica POCO da `ResilienciaMental`.

*Ref: [Inventário e Consumíveis](./inventario_e_consumiveis.md)*
