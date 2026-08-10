---
type: Index
title: Scripts Runtime (Adapters)
description: Catálogo de scripts MonoBehaviour que integram a lógica POCO com a Unity
---

# Scripts Runtime (Adapters)

Os scripts nesta seção herdam de `MonoBehaviour` e vivem na Unity. Eles atuam como **adaptadores**: orquestram a injeção de dependências dos POCOs, leem inputs, e atualizam a interface gráfica ou a física `Rigidbody2D`.

## Player
- [PlayerMovement](player_movement_cs.md)
- [EsquivaBridge](esquiva_bridge_cs.md)
- [MaoFisicaBridge](mao_fisica_bridge_cs.md) — arma trocável (ataque básico + habilidade)

## Combat
- [VitalidadeBridge](vitalidade_bridge_cs.md) — vitalidade + mitigação por defesa
- [FichaAtributosConfig](ficha_atributos_config_cs.md) — `ScriptableObject` da ficha
- [DanoFlutuante](dano_flutuante_cs.md) — números de dano na tela (diagnóstico)

## Interaction
- [IInteragivel](iinteragivel_cs.md) — contrato dos objetos usáveis
- [DetectorDeInteracao](detector_de_interacao_cs.md) — acha o alvo e dispara o botão E

## Enemies
- [CultistaAI](cultista_ai_cs.md)
- [AbdulAlhazredAI](abdul_alhazred_ai_cs.md) — o boss (FSM, escudo, Pedras, conversa)
- [PedraDePoder](pedra_de_poder_cs.md) — cenário destrutível da Fase 1
- [YugNethAI](yug_neth_ai_cs.md) — companheiro Mi-Go (cativo → livre)
- [EspectroAI](espectro_ai_cs.md)

## Environment
- [TempestadeAmbiente](tempestade_ambiente_cs.md) — ⚠️ sem instância na cena atual

## GameLoop
- [GameManager](game_manager_cs.md)
- [BauDaTumba](bau_da_tumba_cs.md) — sorteia uma das 3 armas (RNG)
- [PatuaPickup](patua_pickup_cs.md) — ⚠️ efeito pendente de design
- [TransicaoDeFaseTrigger](transicao_de_fase_trigger_cs.md)
- [ColapsoTrigger](colapso_trigger_cs.md)
- [TutorialHintTrigger](tutorial_hint_trigger_cs.md)
- [TempestadeZonaTrigger](tempestade_zona_trigger_cs.md) — ⚠️ sem instância na cena atual
- [QuedaZ4Z5Trigger](queda_z4z5_trigger_cs.md) — ⚠️ sem instância na cena atual
- [CercoZ4Cutscene](cerco_z4_cutscene_cs.md) — ⚠️ sem instância na cena atual

## UI
- [HUDController](hud_controller_cs.md)
- [ResilienciaBar](resiliencia_bar_cs.md) — barra de sanidade
- [VitalidadeBar](vitalidade_bar_cs.md) — barra de vida corpórea
- [BarraDeAcoes](barra_de_acoes_cs.md) — slots de arma/habilidade com recarga
- [PromptDeInteracao](prompt_de_interacao_cs.md) — convite do botão E
- [PainelDeEscolha](painel_de_escolha_cs.md) — escolha de diálogo
- [ScreenFader](screen_fader_cs.md)
- [TutorialHintUI](tutorial_hint_ui_cs.md)
- [TempestadeVisualOverlay](tempestade_visual_overlay_cs.md) — ⚠️ sem instância na cena atual

## Camera
- [IsometricCameraController](camera_controller_cs.md)
