---
type: Game System
title: IA do Cultista (FSM)
description: Máquina de estados finitos que controla o comportamento dos Cultistas Amarelos.
tags: [enemies, ai, fsm, stealth]
timestamp: 2026-07-07T11:00:00Z
---

# IA do Cultista (FSM)

Os **Cultistas Amarelos** são os inimigos principais. Seu comportamento é controlado por uma FSM (Finite State Machine) com 5 estados.

## Estados

| Estado | Descrição |
|--------|-----------|
| **Errante** | Estado padrão. O Cultista segue sua [rota de patrulha](patrulha.md). Não persegue o jogador. |
| **Alerta** | Ativado ao ouvir um som. Pausa de 1.5s (telegrafada ao jogador) antes de decidir caçar. |
| **Caça** | O Cultista se move em direção à última posição conhecida do som. |
| **Atacar** | Damião está ao alcance de golpe corpo-a-corpo. O Cultista planta os pés e desfere golpes numa cadência fixa (default 1,2 s). |
| **Atordoado** | Interrompido por um golpe físico (ex.: habilidade do [Alfanje de Alhazred](abilities.md)). Ignora estímulos sonoros até passar. |

## Regras de Transição

```
Errante   ──(ouve som dentro do raio)──▶ Alerta
Alerta    ──(1.5s + estímulo recente)──▶ Caça
Alerta    ──(8s sem novo estímulo)─────▶ Errante
Caça      ──(alvo ao alcance de golpe)─▶ Atacar
Caça      ──(10s sem ouvir nada)───────▶ Errante
Atacar    ──(alvo saiu do alcance)─────▶ Caça
Qualquer  ──(AtordoarPor(duração))─────▶ Atordoado
Atordoado ──(duração passou)───────────▶ Errante
```

### Detalhes Críticos

- **Pausa telegrafada de 1.5s:** Quando o Cultista entra em Alerta, ele espera 1.5 segundos E verifica se houve estímulo sonoro recente (nos últimos 1.5s) antes de ir para Caça. Isso dá ao jogador uma janela de reação.
- **Timeout de Caça:** Se o jogador parar de emitir som por 10 segundos, o Cultista desiste e volta a patrulhar.
- **Timeout de Alerta:** Se nenhum som novo chega em 8 segundos, o Cultista volta ao estado Errante.
- **UltimaOrigemConhecida:** O Cultista se move em direção à posição de onde veio o último som, não à posição real do jogador.
- **Atordoado:** `AtordoarPor(duração)` interrompe qualquer estado atual. Enquanto atordoado, `ReceberEstimuloSonoro` é ignorado por completo (não atualiza `UltimaOrigemConhecida` nem `TimeSinceLastStimulus`). Ao passar a duração, volta pra Errante — perde totalmente o rastro, não retoma a Caça de onde parou. Se o golpe atordoa é decidido pela **arma** (o Alfanje de Alhazred atordoa na habilidade "Golpe do Deserto"), não pela FSM — a FSM só sabe executar o atordoamento quando mandado.
- **Atacar é governado por proximidade, não por som.** O adaptador `CultistaAI` faz um `OverlapCircle` na camada do jogador a cada `FixedUpdate` e informa a FSM por `AtualizarAlcanceDoAlvo(bool)`. **Não é visão** — é toque/proximidade, coerente com a percepção só-sonora do Cultista (percepção graduada com visão segue fora de escopo). Enquanto o alvo está ao alcance, os timeouts sonoros não tiram o Cultista de Atacar.
- **Telegrafo do primeiro golpe:** ao entrar em Atacar o timer de cadência **reinicia do zero**, então o primeiro golpe só sai após uma cadência completa — janela para o jogador recuar. Sair e reentrar no alcance **não** acumula progresso do timer (não dá para "carregar" um golpe instantâneo entrando e saindo).
- **O golpe não calcula dano.** A FSM só emite `OnGolpeDesferido`; o Runtime traduz isso em dano usando o `Ataque` da [ficha](ficha_de_atributos.md) do Cultista, mitigado pela `Defesa` da ficha do alvo. Regra de negócio de dano vive no Core, nunca no adaptador.
- **Importante:** nem todo inimigo reage a golpes físicos da mesma forma — o Espectro (`EspectroFSM`) é uma FSM totalmente separada e não tem conceito de Atordoado; imunidade a armas físicas é uma regra do resolvedor de combate (Runtime), não da FSM do Cultista.

## Integração com Outros Sistemas

- Recebe estímulos de [Propagação Sonora](sound_propagation.md) via `ReceberEstimuloSonoro(origem, distância, raioEfetivo)`
- Usa [Patrulha](patrulha.md) para definir rota no estado Errante
- Emite evento `OnStateChanged(anterior, novo)` para o adapter visual `CultistaAI`
- Recebe `AtordoarPor(duração)` de armas físicas que implementam [`IArma`](abilities.md) (ex.: habilidade do Alfanje de Alhazred)
- Recebe `AtualizarAlcanceDoAlvo(bool)` do `CultistaAI` (proximidade física) e emite `OnGolpeDesferido` a cada golpe
- Tem [Vitalidade](vitalidade.md) e [ficha de atributos](ficha_de_atributos.md) próprias: pode ser abatido por armas e tem `Defesa` que mitiga o dano recebido
