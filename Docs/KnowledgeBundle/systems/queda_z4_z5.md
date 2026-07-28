---
type: Game System
title: Queda Z4 → Z5 (Cerco e Colapso)
description: Sequência roteirizada que transiciona Damião da Praça do Cerco (Z4) para a Transição Dimensional (Z5)
tags: [game-loop, cutscene, level-transition]
timestamp: 2026-07-07T17:00:00Z
---

# Queda Z4 → Z5 (Cerco e Colapso)

Transição de zona **roteirizada e só de ida** — não existe caminho de volta da Zona 5 para a Zona 4 (o entulho que bloqueia o retorno é a própria barreira anômala gerada entre as duas zonas, só atravessável com o Salto Dimensional já destravado).

## Sequência de eventos

1. **Gatilho:** Damião entra num trigger perto da parede no final da Praça do Cerco (Zona 4).
2. **Cerco:** Cultistas e [Espectros](espectro.md) são instanciados ao redor de Damião e avançam em sua direção — uma pequena cutscene de tensão, sem risco real de Trauma (o jogador não pode ser atacado durante o cerco).
3. **Pausa de tensão:** ~1.5s parado, cercado, antes do colapso.
4. **Colapso do chão:** tremor de câmera, fade para preto, teleporte para o marcador de chegada na Zona 5 (Transição Dimensional), fade de volta.
5. Damião recupera o controle já na Zona 5, onde futuramente encontra o patuá (destrava o Salto Dimensional) e uma arma inicial.

## Regras de negócio

- O gatilho só dispara uma vez (`_disparado`), mesmo que o Collider2D seja reativado.
- O movimento de Damião é travado (`PlayerMovement.enabled = false`) do início do cerco até o fim do fade de entrada na Zona 5.
- **Invulnerabilidade de cutscene (2026-07-17):** durante toda a sequência, `GameManager.JogadorInvulneravel = true`. Fontes de morte instantânea por toque (Coisa do Cemitério) e ambiente (`ColapsoTrigger`) respeitam a flag e **não** aplicam Colapso — antes, a Coisa alcançava Damião preso no cerco e o insta-matava. A flag volta a `false` no fim da sequência (já teleportado, longe da ameaça).
- **Tempestade nula na Z5 (2026-07-17):** ao teleportar pra Z5, chama `GameManager.TempestadeAmbiente.DefinirFaixa(0,0)` explicitamente — o teleporte adormece o Rigidbody e o `TempestadeTrigger_Z5_Nula` não dispara sozinho (ver [Environment](environment.md)).
- **Limpeza do cerco (2026-07-17):** após a queda, `CercoZ4Cutscene.LimparAtores()` destrói os Cultistas/Espectros instanciados — eram set-piece da Z4 e não devem persistir/perseguir na Z5.
- Os Cultistas instanciados no cerco têm a IA normal (`CultistaAI`) desativada — não devem patrulhar nem reagir a som durante a cutscene, só avançar até a posição roteirizada.
- Os Espectros seguem sua própria FSM roteirizada (ver [Espectro](espectro.md)), disparada externamente pelo diretor da cutscene, e **atravessam paredes** (Kinematic).

## Implementação

- [QuedaZ4Z5Trigger](../scripts/runtime/queda_z4z5_trigger_cs.md) — orquestra a sequência inteira.
- [CercoZ4Cutscene](../scripts/runtime/cerco_z4_cutscene_cs.md) — só a parte do cerco (instancia e aproxima os inimigos).
- `ScreenFader` e `IsometricCameraController.Shake` — efeitos de fade e tremor, reaproveitados de outros pontos do jogo.
