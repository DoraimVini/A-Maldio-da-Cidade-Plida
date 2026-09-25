---
type: Architecture
title: Tranca de Arena — nenhum chefe pode ser abandonado
description: Padrão genérico que fecha as saídas de uma arena durante a luta de chefe. Pensado para ser reaproveitado por Byakhee e Rei em Amarelo, não só pelo Abdul.
tags: [boss, arena, arquitetura, padrao]
---

# Tranca de Arena

> **Regra de design do Vini (2026-07-31):** *"Nenhuma luta de boss dá para fugir antes do
> final."* Vale para o jogo inteiro — Abdul, Byakhee, Rei em Amarelo e o que vier.

## Por que um componente genérico

A primeira tentativa foi específica: o portal da arena checava uma chave de save toda vez que
o jogador encostava nele. Funcionava para o Abdul, mas era um remendo — e o Vini apontou que
**esse padrão vai se repetir em toda luta de chefe do jogo**, então merecia uma peça de
verdade em vez de um `if` copiado três vezes.

`TrancaDeArena` é essa peça. Ela **não sabe**:
- qual chefe a controla,
- que existe um sistema de save,
- que a saída é um `PortalDeCena` (poderia ser uma porta, um gatilho de transição, o que for).

Ela recebe uma lista de `Collider2D` e liga/desliga. Um chefe novo reaproveita tudo só
ligando campos no Inspector — **sem escrever código**.

## Como funciona

| Momento | Quem chama | O quê |
|---|---|---|
| A luta começa de verdade | `AbdulAlhazredAI.HandleEstadoMudou` (entrada na Fase 1) | `Trancar()` |
| A luta é resolvida | `AbdulAlhazredAI.HandleDerrotado` | `Destrancar()` |
| Cena recarrega com a luta já resolvida | `AbdulAlhazredAI.AplicarEstadoSalvo` | `Destrancar()` |

**A trava é dirigida por evento, não por polling.** A FSM do chefe diz quando trancar e
quando destrancar; nada fica checando estado a cada frame nem consultando o save no meio do
`OnTriggerEnter2D`.

### Detalhes que importam

- **Desliga o `Collider2D`, não o GameObject.** A saída pode carregar visual de porta, luz e
  som de ambiente que devem continuar existindo enquanto a passagem está fechada.
- **A conversa não tranca nada.** Só a entrada na Fase 1 fecha a arena — e é o mesmo caminho
  usado pela traição da trégua (`IniciarLuta()`), então trair também tranca. Poupar Abdul sem
  nunca lutar nunca fecha nada, porque a FSM não sai de `Transe`.
- **Uma arena nunca pode nascer trancada.** Por isso `AplicarEstadoSalvo` também destranca:
  se a luta já tinha sido resolvida antes desta carga de cena, a saída precisa estar aberta
  desde o primeiro frame.
- **O campo é opcional.** Sem `TrancaDeArena` atribuída, a luta funciona normalmente — só dá
  para fugir. Isso mantém cenas de teste e playtests isolados funcionando sem montagem extra.

## Montagem

Ferramenta: `Tools/FavelaAmarela/Montar Tranca de Arena do Abdul`.

Ela identifica o portal da arena pela **distância até o Abdul**, não pelo nome. O objeto se
chama `Saida_TumbaAlhazred (1)` — sufixo automático que a Unity gera ao duplicar, exatamente
o tipo de identificador frágil que uma renomeação quebraria em silêncio (mesma armadilha que
`ObjetoPersistente` documenta para chaves de save).

> **Contexto da Tumba:** há dois portais de saída — um na entrada da dungeon e outro dentro
> da arena, colocado à mão pelo Vini para não obrigar o jogador a refazer o caminho todo
> depois da luta. Só o da arena é trancado.

## Reaproveitar em chefes futuros

1. Pôr um `TrancaDeArena` na cena e listar os colisores das saídas daquela arena.
2. No adaptador do chefe, chamar `Trancar()` quando a FSM entra no estado de combate e
   `Destrancar()` quando ela chega ao desfecho.
3. Se o chefe tiver restauração de save, destrancar também ali.

Nada além disso. Ver [boss_abdul.md](../systems/boss_abdul.md) para o caso concreto.
