---
type: C# Script
title: CoisaDoCemiterioFSM.cs / CoisaDoCemiterioState.cs
description: Máquina de estados pura da Coisa do Cemitério (bestiário #5)
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Core/Enemies/CoisaDoCemiterioFSM.cs
tags: [core, enemies, state]
timestamp: 2026-07-09T00:00:00Z
---

# CoisaDoCemiterioFSM

**Namespace:** `FavelaAmarela.Core.Enemies`
**Tipo:** `public sealed class` (`CoisaDoCemiterioFSM`) + `public enum` (`CoisaDoCemiterioState`)

POCO que implementa a FSM da [Coisa do Cemitério](../../systems/coisa_do_cemiterio.md). Reaproveita a mesma fonte de estímulo do [CultistaFSM](cultista_fsm_cs.md) (`SoundBroadcastService`), mas nunca tem um estado parado/inconsciente — está sempre se aproximando, de forma imprecisa até um som revelar a posição exata.

## Estados (`CoisaDoCemiterioState`)
- `Farejando`: se aproxima devagar da última posição aproximada conhecida.
- `AlvoPreciso`: um estímulo sonoro recente revelou a posição exata — avança direto.

## API Pública (`CoisaDoCemiterioFSM`)

### Propriedades (readonly)
- `CurrentState`: estado atual (`Farejando` por padrão na construção).
- `TimeInState`: tempo acumulado no estado atual.
- `TimeSinceLastStimulus`: tempo desde o último estímulo sonoro válido (inicia em `999f`, ou seja "há muito tempo").
- `UltimaOrigemConhecida` (`Vector2?`): última posição de estímulo sonoro válido; `null` se nenhum estímulo foi recebido ainda.

### Construtor
- `CoisaDoCemiterioFSM(float duracaoAlvoPreciso = 6f)`

### Métodos
- `ReceberEstimuloSonoro(Vector2 origemSom, float distanciaAoJogador, float raioEfetivo)`: se a distância está dentro do raio efetivo, atualiza `UltimaOrigemConhecida` e zera `TimeSinceLastStimulus`; transiciona `Farejando → AlvoPreciso`.
- `Tick(float dt)`: avança `TimeInState`/`TimeSinceLastStimulus`; se em `AlvoPreciso` e `TimeSinceLastStimulus >= duracaoAlvoPreciso`, volta para `Farejando`.

### Eventos
- `OnStateChanged(CoisaDoCemiterioState anterior, CoisaDoCemiterioState novo)`

## Imunidade a combate físico
Não reage a golpes de [IArma](iarma_cs.md) — a imunidade não é lógica desta FSM, é o resolvedor de golpe (Runtime, `MaoFisicaBridge`) simplesmente não reconhecer este componente como alvo válido.

## Insta-kill
Se a criatura encosta no jogador, o adapter Runtime chama `ResilienciaMental.ForcarColapso()` (mesmo contrato reaproveitado de `ColapsoTrigger` — ver [dependency_map.md](../../architecture/dependency_map.md)).
