---
type: Game System
title: Inventário e Consumíveis
description: O inventário enxuto de Damião, as definições de item e como o efeito de um consumível vira mudança no mundo.
tags: [inventario, itens, consumiveis, ancoragem, estabilizacao]
---

# Inventário e Consumíveis

> **Destravado em 2026-07-31** (antes era "previsto, sem data"). O item 2 do escopo do
> edital — Sistema de Consumíveis — depende dele. Construído em 2026-08-01.

## Forma: enxuto, de propósito

Restrição do `CLAUDE.md` §1: **sem grind de itens**. Poucas posições (8 por padrão), sem
peso, sem categorias, sem ordenação automática.

Poucas posições não é limitação técnica — é **tensão de design**: escolher o que deixar
para trás é parte do horror de sobrevivência. Um inventário grande transformaria decisão em
contabilidade.

## Vocabulário

Segue a skill `favela-lore-enforcer`. Nada de "cura X de HP":

| Efeito | Termo diegético | O que restaura |
|---|---|---|
| `Ancorar` | **Ancoragem** | Resiliência Mental (a sanidade) |
| `Estabilizar` | **Estabilização** | Vitalidade corpórea (a carne) |
| `EstancarFeridas` | — | Interrompe uma Ferida de Aklo em curso |
| `Nenhum` | — | Item de lore, chave ou relíquia (não é gasto ao "usar") |

## As peças

| Peça | Camada | Papel |
|---|---|---|
| `DefinicaoDeItem` | Core | **O que um item é** — id, nome, descrição, pilha, efeito, potência. Imutável. |
| `PilhaDeItens` | Core | Uma posição: o item + quantos. `readonly struct`. |
| `Inventario` | Core | As posições, empilhamento, guardar/retirar/usar. **21 testes.** |
| `EfeitoDeUso` | Core | O que sai de um uso, para o Runtime aplicar. |
| `ItemConfig` | Runtime | Autoria em asset (um por tipo de item) → cospe a `DefinicaoDeItem`. |
| `InventarioBridge` | Runtime | Dono do inventário e **único ponto** onde o efeito vira mudança no mundo. |

### Por que o Core não aplica o efeito

O `Inventario` sabe contar, empilhar e gastar — mas **não sabe o que é Vitalidade nem
Resiliência**. Ele devolve um `EfeitoDeUso` e o `InventarioBridge` decide onde aplicar. É o
que mantém o inventário inteiro testável sem a Unity rodando.

## Regras que evitam bug silencioso de progresso

Cada uma existe por um motivo, e todas têm teste:

1. **Completa pilhas antes de ocupar posição nova.** Sem isso o inventário enche por
   fragmentação com o jogador achando que tem espaço.
2. **`Remover` nunca retira parcialmente.** Se não há o suficiente, não retira nada — evita
   consumir metade de um custo que não podia ser pago.
3. **Item sem efeito não é gasto ao ser "usado".** Uma relíquia não pode sumir porque o
   jogador clicou nela.
4. **`Usar` só consome se o efeito tiver onde agir.** Usar uma Ancoragem sem Resiliência
   injetada gastaria o item à toa — o pior tipo de bug de inventário, porque o jogador perde
   recurso e não vê nada acontecer.
5. **Índice fora da faixa devolve pilha vazia**, não exceção.

## Ligação

`GameManager.InjetarDependencias` injeta a `ResilienciaMental` no `InventarioBridge` — é o
que dá alvo aos itens de Ancoragem. A Vitalidade vem do `VitalidadeBridge` do próprio
GameObject (`RequireComponent`).

## Pendente

- **Nenhum `ItemConfig` foi autorado ainda** — o sistema funciona, mas não existe nenhum
  item de verdade no jogo. Os nomes usados nos testes (Cinza de Âncora, Emplastro de Sal)
  são de exemplo, não decisão de design.
- **Nenhuma UI de inventário.** A `BarraDeAcoes` cobre arma + habilidade; falta a tela/faixa
  que mostra as posições e permite usar.
- **`InventarioBridge` não está em cena** em lugar nenhum.
- **`EstancarFeridas` não tem alvo:** só inimigos sangram hoje; Damião não tem uma Ferida de
  Aklo própria. O efeito existe no enum mas é sempre recusado.
- **Não persiste.** O inventário não entra no save ainda — ver
  [architecture/persistencia.md](../architecture/persistencia.md).

## Nota de arquivo

Todos os tipos de Core estão em `DefinicaoDeItem.cs`, não em um arquivo por tipo. Não é
preferência: o AssetDatabase da Unity parou de indexar `.cs` novos naquela pasta e ignorava
os arquivos em silêncio (um erro de sintaxe proposital não gerou erro nenhum). Separar
depois de um restart do Editor — é só mover os blocos, nada no código muda.
