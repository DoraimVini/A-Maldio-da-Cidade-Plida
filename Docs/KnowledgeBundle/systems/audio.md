---
type: Game System
title: Áudio — Dar Voz ao Pilar Sonoro
description: Mixer com pool, banco de sons autorável e síntese procedural de andaime. Torna audível o ruído que Damião emite, que era a mecânica central invisível do jogo.
tags: [audio, stealth, som, hud, feedback]
---

# Áudio

> **Status:** Implementado em 2026-08-11. **Nenhum clipe autorado ainda** — tudo soa por
> síntese procedural até a arte de áudio existir. Falta **wiring de cena**.

## Por que isto era urgente

Até 2026-08-11 o projeto tinha **zero `AudioSource` no gameplay**. Isso não era polimento
faltando: o pilar do jogo é **furtividade sonora** — o Cultista caça por som
(`SoundBroadcastService`), a tempestade abafa o ruído do Damião, a Esquiva faz barulho de
propósito. Sem retorno sonoro, **o jogador jogava um stealth sonoro sem ouvir nada**: andava,
era caçado, e não tinha como entender por quê. A mecânica central era invisível.

## A peça que resolve isso: `AudioDeStealth`

Observa `SoundBroadcastService.OnSomEmitido` e toca o passo **com volume proporcional ao raio
do som emitido**. É a tradução direta da mecânica em percepção:

| Situação | Raio do som | O que o jogador ouve |
|---|---|---|
| Agachado | pequeno | quase nada |
| Andando | médio | passo audível |
| Correndo / Esquiva | grande | passo alto |
| Tempestade forte | abafado | passo abafado |

Assim o jogador **aprende a mecânica jogando**, sem tutorial: ele ouve o próprio rastro
sonoro encolher ao agachar.

## Arquitetura

| Peça | Papel |
|---|---|
| `SomDoJogo` | Enum fechado dos sons do jogo. Pequeno de propósito. |
| `BancoDeSons` (SO) | Catálogo autorado: som → clipes + volume + variação de tom. **Som novo é asset, não código.** |
| `MixerDeAudio` | Ponto único de reprodução. Pool fixo de `AudioSource` criado no `Awake`. |
| `SinteseDeSom` | Andaime: gera clipes por síntese quando o banco não tem nenhum. |
| `AudioDeStealth` | O pilar: ruído emitido → som audível, escalado pelo raio. |
| `AudioDeResiliencia` | Só as **transições** de Pânico e Colapso. |
| `AudioDeCombate` | Por entidade: levar dano e ser abatida. |

### Por que um pool, e não `PlayClipAtPoint`
`AudioClip.PlayClipAtPoint` instancia um `GameObject` por som e o destrói depois — em jogo de
stealth, com passo a cada 0,15 s, isso é lixo constante em hot path (Regra de Ouro 1). O pool
é criado uma vez; tocar um som nunca aloca. Vozes esgotadas entram em rodízio: um som novo
vale mais que um som velho terminando.

### Por que síntese procedural
Não existe **nenhum** arquivo de áudio no projeto. Esperar a arte de som para ligar o sistema
deixaria o pilar invisível por mais tempo indeterminado. A `SinteseDeSom` gera ruído filtrado
(passos, impactos) e tons com varredura (Pânico sobe, Colapso desaba), cacheados uma vez.

**Não é substituto de som autorado — é andaime.** Assim que um clipe real entrar no
`BancoDeSons`, ele ganha a preferência automaticamente, sem tocar em código.

### Onde cada peça mora, e por quê
`AudioDeStealth` e `AudioDeResiliencia` vão **no Damião**: são sobre ele — o ruído que ele faz
e o estado da mente dele. O `AudioDeStealth` rodaria em qualquer objeto (a posição do som vem
da `Origem` do evento, não do `transform` do componente), mas deixá-lo solto na hierarquia
esconderia essa relação de quem abrisse a cena depois. O `MixerDeAudio` fica num objeto
próprio, porque é serviço, não característica de ninguém.

> **Premissa do `AudioDeStealth`:** todo `SomEmitido` é ruído de Damião. Vale hoje porque **só
> o `PlayerMovement` chama `Emitir`** — os inimigos apenas escutam (`EnemyPerception`,
> `CoisaDoCemiterioAI`). Se um inimigo passar a emitir pelo mesmo serviço, este componente
> tocaria "passo de Damião" para o passo dele; nesse dia, o `SomEmitido` precisa passar a
> dizer **quem** emitiu.

### Por que `AudioDeCombate` é por entidade
O som precisa sair **do lugar onde o golpe aconteceu**. Num jogo em que se caça por som,
áudio sem posição mente para o jogador. Por isso o `MixerDeAudio` usa `spatialBlend = 1`
(3D) com rolloff linear.

## Wiring de cena

**Automatizado (2026-08-11):** rodar `Tools/FavelaAmarela/Ligar sistemas novos` cria o
`MixerDeAudio` e o `AudioDeStealth` nas 3 cenas, anexa o `AudioDeResiliencia` ao Damião e o
`AudioDeCombate` aos prefabs de inimigo. Idempotente.

O `GameManager` liga as fontes no bootstrap e **avisa no console** se o `AudioDeStealth`
estiver ausente. Um asset `BancoDeSons` é opcional — sem ele, tudo soa por síntese.

## Pendências
- **Nenhum clipe autorado.** É a próxima frente de áudio, e não depende de código.
- **Sem música nem ambiência.** Só efeitos pontuais por enquanto.
- **Inimigo não soa ao perceber Damião** — um "alerta" audível seria o par natural do
  `AudioDeStealth`, fechando o laço nos dois sentidos.

## Relacionados
- [Stealth e Percepção](cultista_ai.md) — quem escuta o som do outro lado
- [Resiliência Mental](resiliencia_mental.md) — as transições que o áudio sonoriza
