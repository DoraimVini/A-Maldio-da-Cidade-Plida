---
type: Index
title: Scripts Core (POCO)
description: Catálogo de scripts C# puros de domínio
---

# Scripts Core (POCO)

Documentação dos scripts de domínio (Plain Old C# Objects) que contêm toda a regra de negócio do jogo.

## Combat
- [ResilienciaMental](resiliencia_mental_cs.md) — sanidade (o Colapso)
- [Vitalidade](vitalidade_cs.md) — vida corpórea (ser abatido)
- [FichaDeAtributos](ficha_de_atributos_cs.md) — os 5 atributos de toda unidade
- [MitigacaoDeDano](mitigacao_de_dano_cs.md) — fórmula de defesa (subtrativa com piso)
- [IDanificavel](idanificavel_cs.md) — contrato de quem pode receber golpe
- [AcumuloDeCongelamento](acumulo_de_congelamento_cs.md) — stacks de frio dos Cones de Gelo

## Enemies
- [CultistaFSM](cultista_fsm_cs.md)
- [AbdulFSM](abdul_fsm_cs.md) — a luta do boss (fases + Escudo Mágico)
- [EspectroFSM](espectro_fsm_cs.md)
- [PatrolRoute](patrol_route_cs.md)
- [CoisaDoCemiterioFSM](coisa_do_cemiterio_fsm_cs.md)

## Player
- [PlayerStateMachine](player_state_machine_cs.md) — FSM de ações exclusivas (Esquiva/Ataque)

## Interaction
- [SeletorDeInteracao](seletor_de_interacao_cs.md) — qual alvo o botão E usa

## Dialogo
- [NavegadorDeOpcoes](navegador_de_opcoes_cs.md) — cursor de escolha de diálogo

## Companion
- [SeguidorDeAlvo](seguidor_de_alvo_cs.md) — movimento do companheiro Yug-Neth

## Stealth
- [SoundBroadcastService](sound_broadcast_cs.md)

## GameLoop
- [GameLoopStateMachine](game_loop_sm_cs.md)

## Environment
- [EnvironmentState](environment_state_cs.md)
- [TempestadeOscilador](tempestade_oscilador_cs.md)

## Abilities
- [Esquiva](esquiva_cs.md)
- [IArma](iarma_cs.md) — contrato de arma física (`IArma` / `IArmaComHabilidade` / `ArmaResult`)
- [IAnomalyPower](ianomaly_power_cs.md) — ⚠️ contrato mantido, **sem implementações hoje**
