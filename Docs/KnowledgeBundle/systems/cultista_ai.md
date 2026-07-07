---
type: Game System
title: IA do Cultista (FSM)
description: Máquina de estados finitos que controla o comportamento dos Cultistas Amarelos.
tags: [enemies, ai, fsm, stealth]
timestamp: 2026-07-07T11:00:00Z
---

# IA do Cultista (FSM)

Os **Cultistas Amarelos** são os inimigos principais. Seu comportamento é controlado por uma FSM (Finite State Machine) com 3 estados.

## Estados

| Estado | Descrição |
|--------|-----------|
| **Errante** | Estado padrão. O Cultista segue sua [rota de patrulha](patrulha.md). Não persegue o jogador. |
| **Alerta** | Ativado ao ouvir um som. Pausa de 1.5s (telegrafada ao jogador) antes de decidir caçar. |
| **Caça** | O Cultista se move em direção à última posição conhecida do som. |

## Regras de Transição

```
Errante ──(ouve som dentro do raio)──▶ Alerta
Alerta  ──(1.5s + estímulo recente)──▶ Caça
Alerta  ──(8s sem novo estímulo)─────▶ Errante
Caça    ──(10s sem ouvir nada)───────▶ Errante
```

### Detalhes Críticos

- **Pausa telegrafada de 1.5s:** Quando o Cultista entra em Alerta, ele espera 1.5 segundos E verifica se houve estímulo sonoro recente (nos últimos 1.5s) antes de ir para Caça. Isso dá ao jogador uma janela de reação.
- **Timeout de Caça:** Se o jogador parar de emitir som por 10 segundos, o Cultista desiste e volta a patrulhar.
- **Timeout de Alerta:** Se nenhum som novo chega em 8 segundos, o Cultista volta ao estado Errante.
- **UltimaOrigemConhecida:** O Cultista se move em direção à posição de onde veio o último som, não à posição real do jogador.

## Integração com Outros Sistemas

- Recebe estímulos de [Propagação Sonora](sound_propagation.md) via `ReceberEstimuloSonoro(origem, distância, raioEfetivo)`
- Usa [Patrulha](patrulha.md) para definir rota no estado Errante
- Emite evento `OnStateChanged(anterior, novo)` para o adapter visual `CultistaAI`
