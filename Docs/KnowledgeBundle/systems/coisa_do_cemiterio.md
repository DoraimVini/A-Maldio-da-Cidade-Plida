---
type: Game System
title: Coisa do Cemitério
description: Inimigo stealth-brute que caça por faro, imune a combate, insta-kill no toque.
tags: [enemies, stealth, fsm, insta-kill]
timestamp: 2026-07-08T12:00:00Z
---

# Coisa do Cemitério

A **Coisa do Cemitério** é o bestiário item #5 (ver `lore/bestiary.md`): não usa armas, não enxerga bem, caça farejando Damião. Diferente do Cultista, ela **nunca descansa** — não existe estado "Errante" inconsciente.

## Estados (CoisaDoCemiterioFSM)

| Estado | Descrição |
|--------|-----------|
| **Farejando** | Se aproxima devagar da última posição aproximada conhecida (imprecisa). |
| **AlvoPreciso** | Um estímulo sonoro recente revelou a posição exata — avança direto e mais rápido. |

Reaproveita a mesma fonte de estímulo do [Cultista](cultista_ai.md) (`SoundBroadcastService`/`ReceberEstimuloSonoro`), sem a pausa telegrafada de 1.5s do Cultista — vai direto de Farejando pra AlvoPreciso, sem aviso.

## Imunidades e insta-kill

- **Imune a golpe de arma física**: o resolvedor de golpe (`MaoFisicaBridge`) só reconhece `CultistaAI` nos colisores atingidos — a `CoisaDoCemiterioAI` (Runtime) simplesmente não aparece nessa busca. Não precisa de lógica de "imunidade" explícita.
- **Toque = Colapso instantâneo**: reaproveita `ResilienciaMental.ForcarColapso()`, já usado pelo `ColapsoTrigger.cs` (hoje só para abismos/ambiente). A `CoisaDoCemiterioAI` chama o mesmo método no `OnTriggerEnter2D` com o jogador (seu `Collider2D` é `isTrigger`, diferente do `Cultista.prefab` que é sólido).
  - **Exceção — invulnerabilidade de cutscene:** se `GameManager.JogadorInvulneravel` está `true` (Damião preso numa sequência roteirizada, ex.: a queda Z4→Z5), o toque **não** mata — só há a tensão da ameaça chegando, sem dano num momento em que o jogador não pode reagir. O `ColapsoTrigger` respeita a mesma flag.
- Único contraponto: **furtividade máxima de som** (ver [Propagação Sonora](sound_propagation.md)) — não há como se esconder da visão dela, só evitar estímulo sonoro.

## Posicionamento em cena

Na cena `Tumba_De_Alhazred`, a `CoisaDoCemiterio` foi posicionada na transição entre o **fim da Zona 2 (Vila das Casas)** e o **meio da Zona 3 (Beco do Vento)** — decisão de design (2026-07-10): força o jogador a usar furtividade sonora na passagem entre zonas, e como ela caça por som, naturalmente pode persegui-lo até a Zona 4 (Praça do Cerco) se ele fizer barulho ali perto.

## Status de implementação
- ✅ `CoisaDoCemiterioFSM` (Core, testado — 9 testes NUnit)
- ✅ `CoisaDoCemiterioAI` (Runtime bridge) — self-wire em `GameManager.Instance` (som + colapso), sem `Bind()` manual
- ✅ Prefab (`Assets/FavelaAmarela/Art/Enemies/CoisaDoCemiterio.prefab`) e posicionamento em cena (2026-07-10)
