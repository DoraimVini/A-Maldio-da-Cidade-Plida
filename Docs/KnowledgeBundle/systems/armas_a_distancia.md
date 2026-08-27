---
type: Game System
title: Armas à Distância — registro de pendência (pós-VS)
description: Arcos, bestas e armas de fogo. Registro da intenção e das perguntas em aberto; nenhum design fechado e nada implementado.
tags: [armas, distancia, fisica, projetil, pos-vs, pendencia]
---

# Armas à Distância

> **Status: REGISTRO, não design.** Pedido do Vini em 2026-08-27, durante o trabalho de
> itemização em `develop_items`, com a instrução explícita de que *"isso é só registro para essa
> branch, mesmo"*. **Não começar sem pedido explícito.** Nada aqui está decidido e nada está
> implementado.

## O que foi pedido

Entender a **física de ataques à distância** para depois do Vertical Slice:

- **Arcos e bestas** — projétil com tempo de voo.
- **Armas de fogo** — escopeta, pistola, rifle.

## Por que isto não é uma extensão trivial do que existe

O combate hoje é **inteiramente corpo a corpo e instantâneo**. `MaoFisicaBridge.ResolverGolpe`
faz um `Physics2D.OverlapCircle` na frente do Damião e resolve o dano no mesmo quadro. Não
existe projétil de jogador em lugar nenhum.

O que existe de mais próximo é inimigo: `ConeDeGelo` (do Abdul) é um `Rigidbody2D` com
`linearVelocity` constante — um projétil de verdade, mas escrito para um caso único, sem
abstração reaproveitável.

## Perguntas em aberto (a responder quando o trabalho começar)

1. **Projétil viaja ou é hitscan?** Arco e besta pedem tempo de voo (o jogador lidera o alvo).
   Arma de fogo, num isométrico 2D, geralmente é hitscan — e aí a "física" vira traçado de raio,
   não corpo.
2. **Escopeta é um cone de vários projéteis ou um único teste de área?** Muda tudo: contagem de
   pellets é o que dá a queda de dano por distância que faz escopeta ser escopeta.
3. **Como isso conversa com o stealth?** A percepção neste jogo é **100% sonora**
   (`SoundBroadcastService`). Um tiro é o evento mais alto que o jogo teria — arma de fogo
   pode ser a decisão de "abro mão da furtividade agora". Isso é design de pilar, não de arma.
4. **Munição existe?** Se sim, é o primeiro recurso consumível não-curativo do jogo e mexe no
   inventário. O `CLAUDE.md` §1 proíbe propor mecânica de ARPG nova por conta própria — esta
   precisa de decisão do Vini.
5. **Arma de fogo cabe em Carcosa?** É pergunta de ficção antes de ser de sistema. O jogo é
   horror cósmico com ambientação de Ruínas Pálidas; pólvora tem implicações de época e de tom
   que o lore ainda não tratou.

## O que já vai facilitar quando chegar a hora

- **`Hitbox`** (`Assets/Scripts/Combat/Hitbox.cs`) já resolve golpe com **janela ativa** por
  `FixedUpdate`, com de-duplicação por alvo. Um projétil é uma `Hitbox` que se move.
- **A física de impacto da Fase 1** desta branch (repulsão modulada por
  `resistenciaAImpulso`, hit-stop) é agnóstica de origem do golpe — vale para flecha e bala do
  mesmo jeito que vale para lâmina.
- **O sistema de arma a dado** (`BaseDeArma` + `HabilidadeDef`) é onde "arco" e "rifle" entram
  como **famílias**, sem classe C# nova por arma.

## Relacionados

- [armas_da_tumba.md](armas_da_tumba.md) — as 3 armas corpo a corpo autoradas.
- [habilidades_de_item.md](habilidades_de_item.md) — o modelo de habilidade a dado.
- [loot_e_drop.md](loot_e_drop.md) — o modelo de composição de item vigente.
