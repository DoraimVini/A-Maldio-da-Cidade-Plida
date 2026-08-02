---
type: Game System
title: Interação com o Mundo (botão E)
description: Camada de interação deliberada — o jogador aperta um botão para usar objetos, em vez de disparar por encostar.
tags: [interaction, input, collectibles, dialogue]
---

# Interação com o Mundo

Objetos do mundo são usados por **interação deliberada**: o Damião chega perto, um prompt
aparece ("E — Abrir o baú") e **o jogador decide** apertar. Decisão do Vini (2026-07-30),
motivada pelos colecionáveis e pelas futuras caixas de diálogo — encostar e disparar tira a
agência do jogador e atrapalha diálogo.

> **Mudança de padrão.** Antes, todo objeto usava `OnTriggerEnter2D` + `CompareTag("Player")`
> e disparava sozinho ao toque. A ação `Interact` **já existia** no asset de Input (tecla
> **E** / botão Norte do gamepad) e a tag `Interactable` também — mas **nenhum código lia
> nenhuma das duas**. Esta camada finalmente as usa.

## Controle

| Ação | Teclado | Gamepad |
|---|---|---|
| `Interact` | **E** | Botão Norte (Y / △) |

## Arquitetura

Divisão de responsabilidade entre "quem está perto?" (Unity) e "qual vale?" (regra pura):

- **`SeletorDeInteracao`** (`Core.Interaction`, POCO) — decide **qual** alvo vence entre os
  candidatos. Ordem: descarta indisponíveis e fora de alcance → maior `Prioridade` vence →
  empatou, o mais perto → empatou, o menor `Id`. Esse último critério existe para o
  resultado ser **estável entre frames**, e não depender da ordem em que o `Physics2D`
  devolveu os colisores (o prompt ficaria piscando entre dois alvos). Recebe array +
  contagem em vez de coleção porque o detector chama a cada frame com buffer pré-alocado.
- **`IInteragivel`** (`Runtime.Interaction`) — contrato de qualquer objeto usável:
  `RotuloDeInteracao`, `PodeInteragir`, `PrioridadeDeInteracao`, `PosicaoDeInteracao`,
  `Interagir(quemInterage)`. Fica no Runtime porque as implementações são `MonoBehaviour`.
- **`DetectorDeInteracao`** (`Runtime.Interaction`, no Damião) — `OverlapCircle` a cada
  frame com buffers pré-alocados, monta os candidatos, pergunta ao POCO qual vence, e
  chama `Interagir` quando a ação `Interact` é pressionada. Revalida `PodeInteragir` no
  instante do aperto (o alvo pode ter mudado de estado entre a mira e o clique). Expõe
  `OnAlvoMudou` para a UI.
- **`PromptDeInteracao`** (`Runtime.UI`) — escreve "E — {rótulo}" e liga/desliga o painel,
  reagindo ao evento (sem polling).

## Quem já usa

| Objeto | Rótulo | Prioridade |
|---|---|---|
| `BauDaTumba` | "Abrir o baú" | 10 |
| `PatuaPickup` | "Recolher o patuá" | 10 |

Prioridade 10 nos dois porque são itens de progressão: devem ganhar de qualquer cenário
interagível que esteja ao lado.

## Regras de autoria

- O **rótulo é texto visível ao jogador** — passa pela skill `favela-lore-enforcer`. Use
  infinitivo com o verbo da ação ("Abrir o baú"), não "Interagir" genérico.
- `PodeInteragir` deve virar `false` quando a ação esgota (baú aberto, patuá coletado):
  o alvo some do prompt em vez de oferecer uma ação que não faz nada.
- **Nem tudo vira interagível.** Gatilhos de área — transição de cena, queda Z4→Z5, zonas de
  tempestade, dicas de tutorial, colapso — continuam automáticos por `OnTriggerEnter2D`, e
  devem continuar: são eventos de travessia, não objetos que o jogador "usa".
- Cuidado no `PromptDeInteracao`: o campo `Raiz` **não pode** ser o próprio GameObject do
  componente — desativá-lo derrubaria o componente, o `OnDisable` desinscreveria do evento
  e o prompt nunca mais voltaria. O código detecta e cai para o objeto do Label.

## Pendências

- **Montagem na cena:** falta criar o painel do prompt no Canvas do HUD e adicionar o
  `DetectorDeInteracao` ao prefab do Damião. Sem isso o botão E não faz nada ainda.
- **Diálogo:** não existe sistema de diálogo — hoje o feedback reusa o `TutorialHintUI`
  (`Mostrar(mensagem, duração)`). Um `GatilhoDeDialogo : IInteragivel` encaixa direto nesta
  camada quando o sistema existir.
