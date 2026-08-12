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
| `EquipmentInventory` | Core | Herda de `BaseInventory`. Controla os 7 slots do corpo. Valida encaixes e as **regras de empunhadura**. |
| `InventoryManager` | Runtime (Singleton) | Dono do inventário global (`Mochila` e `Equipamentos`). Ponto central de save/load e eventos (`OnItemConsumed`). |

## Anatomia: os 7 slots do corpo (2026-08-12)

A ordem do array `anatomia` no `InventoryManager` **define os índices** e é autorada no
Inspector:

| Índice | Slot |
|---|---|
| 0 | `Arma` (mão principal) |
| 1 | `Elmo` |
| 2 | `Peitoral` |
| 3 | `Grevas` |
| 4 | `Amuleto` |
| 5 | `Anel` |
| 6 | `MaoSecundaria` |

> **A Arma tem de continuar no índice 0.** A `MaoFisicaBridge` escuta especificamente esse
> índice (`VerificarSlotDeArma`) para reconstruir o POCO da arma pela `WeaponFactory`.
> Reordenar a anatomia sem ajustar essa bridge desarma Damião silenciosamente.

### Empunhadura: uma mão ou duas

`ItemDef.Empunhadura` (`UmaMao` / `DuasMaos`) é a escolha tática central do combate: arma
leve **+ foco/escudo** na secundária, ou uma lâmina colossal que toma as duas mãos e não
deixa espaço para defesa.

O `EquipmentInventory` recusa nos dois sentidos — nada entra na secundária com uma arma de
duas mãos empunhada, e uma arma de duas mãos não entra com a secundária ocupada.

**A recusa é estrita, não desalojamento automático, e isso é deliberado:** liberar a
off-hand exige devolver aquele item à mochila, e a mochila pode estar cheia. O POCO não tem
como saber disso. Quem orquestra os dois contêineres é o `InventoryManager.Equipar`, que
esvazia a secundária para a mochila **com rollback** — se não houver espaço, o item volta
para a mão de onde saiu e a troca inteira é cancelada, em vez de deixar o jogador sem o
escudo *e* sem o espadão.

> **`EquipmentSlot` e `ItemType` são serializados por índice.** Valores novos entram
> **sempre no fim** do enum. Inserir no meio remapearia silenciosamente todo item já
> autorado — um elmo viraria grevas sem erro no console.

### Migração de save (anatomia 6 → 7)

`InventorySaveData.saveVersion` versiona o formato (**0** = save anterior ao campo, anatomia
de 6 slots; **1** = 7 slots com Mão Secundária).

`InventoryManager.RestaurarEquipamento` **nunca** recria o contêiner com `new`, nem quando a
capacidade do save diverge da atual. O ramo que fazia isso existia desde sempre mas era
inalcançável enquanto a anatomia nunca mudava; ao entrar a Mão Secundária, **todo save
antigo passaria por ele** e cairia no bug de 2026-08-11 ("perde a arma no deserto"), em que
recriar a instância deixava órfãos `MaoFisicaBridge`, `GerenciadorEfeitosPassivos`,
`BarraDeItens` e `PainelDeInventario` — sem nenhum erro no console.

Item que não couber (anatomia encolheu, ou o tipo não bate com o índice salvo) tenta o slot
do tipo dele e, em último caso, vai para a mochila — nunca some. Coberto por
`MaoSecundariaTests`.

### Separação de Responsabilidades (Desacoplamento)

Diferente do sistema antigo, onde a UI e o loot dependiam de um Bridge rígido:
- **UI:** A `BarraDeItens` e o `HUDController` assinam eventos do `InventoryManager` (ex: `OnSlotChanged`). Não há dependência cíclica.
- **Consumo:** A UI chama `InventoryManager.ConsumirItem(indice)`. O `InventoryManager` consome a quantidade e dispara o evento `OnItemConsumed`.
- **Efeito no mundo:** Quem assina `OnItemConsumed` (ex: `VitalidadeBridge`) valida se o item possui efeito (como `Ancoragem`) e aplica no jogador.

## Modelo de consumíveis: "a luz é a válvula" (2026-08-12)

**Consumíveis são finitos e encontrados no mapa.** Não caem de inimigo comum, não recarregam,
não têm moeda. Só três existem, e cobrem os dois canais de dano:

| Item | Cura | Empilha | Quantos no Deserto |
|---|---|---|---|
| **Água da Cacimba** | Vitalidade +30 | 5 | 4 |
| **Erva de Ancoragem** | Resiliência Mental +25 | 5 | 3 |
| **Raiz de Yhtill** | Vitalidade +15 **e** RM +12 | 3 | 2 |

Os dois diais de escassez são **puro dado**: o `EmpilhamentoMaximo` de cada `ItemDef` e a
quantidade de instâncias que a ferramenta espalha. Nenhum código para rebalancear.

### O anti-soft-lock é o Refúgio, não recarga

O `RefugioDeLuz` é o **único ponto de save do jogo**, então o jogador passa por ele por design.
Ele devolve Resiliência cheia e, desde 2026-08-12, **40% da Vitalidade máxima** — sob o mesmo
`intervaloDeAncoragem` que já existia, para não dar dois motivos de ficar entrando e saindo da luz.

A tensão fica **espacial e momento a momento** ("consigo chegar no próximo poste?"), e o custo é
pago em travessia por patrulhas — o verbo do próprio jogo. A cura de Vitalidade é **parcial de
propósito**: o jogador chega no Refúgio ferido e ainda precisa decidir se gasta um consumível.

> A fração de Vitalidade é **fração**, não valor absoluto como a de Resiliência. O teto de RM é
> fixo no `GameManager`, mas `Vitalidade.Max` é dinâmico (`SetValorMaximo` reage aos bônus de
> `StatType.VitMaxima` das armaduras) — um absoluto curaria metade da barra no começo do jogo e
> uma lasca dela depois de algumas peças.

### Por que o modelo de moeda + Santuários foi descartado

Uma proposta ("Âncoras de Carcosa") queria recarga ritual em **Santuários de Carcosa** paga com
**Fragmentos de Carcosa** (~8 no jogo). Descartada por cinco motivos verificados no repositório —
registrados aqui para a proposta não voltar:

1. **A "lacuna de cura de Vitalidade" não existia.** A Água da Cacimba já curava corpo, e
   `VitalidadeBridge.AplicarEfeitoConsumivel` já tratava os dois canais. Um *Emplastro de Sal*
   duplicaria a Água; um *Chá Calmante* duplicaria a Erva.
2. **A "profecia dos Fragmentos" é leitura equivocada.** `FragmentoDeYhtill` não é item: é
   `MonoBehaviour`/`IInteragivel` de cena, chave `Quest.Cassilda.Fragmento{indice}`, carregando
   uma estrofe. **Não existe `ItemDef` de Fragmento.** Eles alimentam
   [quest_cassilda.md](quest_cassilda.md), já jogável de ponta a ponta — virar moeda colidiria
   com uma quest concluída.
3. **"Fragmento de Carcosa" e "Santuário de Carcosa" tinham zero ocorrências** em código e em
   `Docs/`. Não eram elementos semeados: eram novos. O que existe é o Refúgio de Luz.
4. **A justificativa mirava a luta errada.** O argumento era "chegar no Rei em Amarelo sem
   recursos", mas o Rei **não tem `Vitalidade` nem `IDanificavel`** — é ritual de relíquias e
   reação, sem desgaste para curar. Ver [boss_rei_em_amarelo.md](boss_rei_em_amarelo.md).
5. **RM já não podia soft-lockar**, porque o Refúgio devolvia 100. O único canal em risco era a
   Vitalidade — a lacuna real, e pequena.

Somado a isso, uma conversão moeda→carga em santuário é um laço de moeda + vendor, que o
`CLAUDE.md` §1 exige confirmar antes de propor. E oito usos num jogo inteiro produzem uma decisão
a cada poucas horas, com hoarding como estratégia dominante — o problema clássico do elixir.

### Ferramenta e chave de save

`Tools/FavelaAmarela/Montar consumíveis do Deserto` espalha os 9, é idempotente e usa **chave
derivada** (`Item.Deserto.<id>.<índice>`), com os setores percorridos em ordem alfabética.

> ⚠️ **Nunca use `ObjetoPersistente.GarantirChave()` aqui.** É o que o `PovoarODeserto` faz, e
> por isso ele **não pode ser reexecutado**: a chave é GUID aleatório, então reconstruir troca
> todas e o save perde o registro de tudo que o jogador já pegou ou matou. Verificado em
> 2026-08-12. `ConsumiveisNoMundoTests` trava essa propriedade com regex.

## Regras que evitam bugs de progressão

1. **Testes isolados (POCO):** O `BaseInventory` e o `MainInventory` são exaustivamente testados sem Unity (NUnit EditMode), cobrindo falhas de empilhamento e limites estritos.
2. **Cópia segura:** Inserir itens ou recuperar via `GetSlot` retorna clones protegidos (`ItemInstance.Clone()`), impedindo manipulação indevida do estado original da pilha.
3. **Gerenciamento de Identidade (GUID):** O `ItemDef` armazena um ID persistente, permitindo que instâncias sejam serializadas para os saves de progresso.

## Conexões Atuais

- **Loot:** Baús (`BauDaTumba`) e drops (`ColetavelDeItem`) agora usam `ItemDef` e injetam diretamente no `InventoryManager.Instance.Mochila`.
- **Save/Load:** O `InventoryManager` possui `GerarSaveData()` e `CarregarSaveData()` compatíveis com a arquitetura do `GameManager`.

## Pendentes (Próximos Passos)

- **GerenciadorEfeitosPassivos:** Para resolver o processamento contínuo de relíquias e equipamentos (como Dreno de RM no escuro), o sistema demandará um intermediário (`GerenciadorEfeitosPassivos.cs`) para ler atributos do `EquipmentInventory` e injetar matemática contínua no `ResilienciaMental`.
- **Drop em Inimigos:** ~~configurar `ColetavelDeItem` chefe por chefe~~ — **substituído em 2026-08-10** por um sistema de tabela de drop para todo inimigo e baú. Design em [loot_e_drop.md](loot_e_drop.md); implementação agendada para depois do Vertical Slice.
