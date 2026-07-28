---
type: Game System
title: Espectro
description: Manifestação roteirizada do Espectro durante cutscenes (ex.: Cerco da Zona 4)
tags: [enemies, cutscene, spectral]
timestamp: 2026-07-07T17:00:00Z
---

# Espectro

O **Espectro** é uma manifestação espectral de Carcosa — diferente do Cultista Amarelo (humano corrompido), o Espectro não tem corpo físico constante: ele materializa e se move em direção a um alvo por vontade da Cidade Pálida, não por instinto ou percepção sonora.

Hoje o Espectro só é usado em **momentos roteirizados** (cutscenes), como o [Cerco da Zona 4](queda_z4_z5.md). Ele não participa da IA reativa normal de stealth (isso é papel do [Cultista](cultista_ai.md)).

## Estados (EspectroFSM)

| Estado | Descrição |
|--------|-----------|
| **Latente** | Ainda não existe visualmente — estado inicial. |
| **Manifestando** | Materializa (fica visível, tonalidade amarelo-espectral). |
| **Cercando** | Avança em linha reta até uma posição-alvo definida pelo diretor da cutscene. |

As transições só andam para frente (`Latente → Manifestando → Cercando`) e são **validadas** — pular etapa ou retroceder é rejeitado silenciosamente, mesmo padrão do `GameLoopStateMachine.TryTransition`. Isso é diferente da `CultistaFSM`, que reage a estímulos sonoros e pode voltar a estados anteriores com o tempo.

## Quem controla o Espectro

Não há percepção nem decisão própria: um componente externo (o diretor de cutscene, ex.: `CercoZ4Cutscene`) chama `Manifestar()` e depois `IniciarCerco(alvo)` na hora roteirizada certa. Ver [Implementação: EspectroFSM](../scripts/core/espectro_fsm_cs.md) e [EspectroAI](../scripts/runtime/espectro_ai_cs.md).

## Física: atravessa paredes (2026-07-17)

Sendo um fantasma, o Espectro **atravessa geometria estática**. Seu `Rigidbody2D` é setado como **Kinematic** no `Awake` do `EspectroAI` — movido por `linearVelocity`, passa por paredes/barreira de anomalia sem resposta de colisão. (Tentou-se antes `excludeLayers`, mas as paredes têm `ForceReceiveLayers` = todas, que anula a exclusão; Kinematic é robusto e independe de layer.) Sem isso, o Espectro do cerco encalhava na barreira ao avançar.
