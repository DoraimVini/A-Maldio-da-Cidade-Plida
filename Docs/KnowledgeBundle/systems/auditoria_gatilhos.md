---
type: Game System
title: Auditoria de Gatilhos e Callbacks
description: Todo Collider2D marcado como trigger, para que serve, e se o callback do script casa com o colisor. Gerado por Tools/FavelaAmarela/Auditar Física 2D.
date: 2026-09-04
---

# Auditoria de Gatilhos e Callbacks

> **Gerado por ferramenta.** Rode `Tools/FavelaAmarela/Auditar Física 2D` para atualizar.

## As regras, citadas da 6000.4

Da Script Reference offline de `MonoBehaviour.OnTriggerEnter2D`:

> *"This message is sent to the trigger Collider2D and the Rigidbody2D (if any) that the trigger Collider2D belongs to, and to the Rigidbody2D (or the Collider2D if there is no Rigidbody2D) that touches the trigger."*
>
> *"Note: Trigger events are only sent if one of the Colliders also has a Rigidbody2D attached."*

Três consequências que mudam o que conta como defeito:

1. **Script em objeto SÓLIDO com `OnTriggerEnter2D` é legítimo** — ele recebe ao *entrar* no gatilho de outro. Acusar isso seria acusar o padrão certo.
2. **Zona sem `Rigidbody2D` funciona**, desde que quem entre tenha um. Aqui quem entra é o Damião, que tem. Acusar zona sem corpo seria falso positivo.
3. **A mensagem vai para o GameObject do colisor.** Script no pai com o trigger num filho **nunca recebe** — este é defeito de verdade, e silencioso.

## `OnCollision*2D` no projeto

**Nenhum script declara `OnCollisionEnter2D`, `Stay` ou `Exit`.** Medido pelo `TypeCache`, sobre todos os tipos do projeto.

Isso responde metade da pergunta "scripts em objetos sólidos usam callback de colisão?": **não há nenhum**. Nada neste jogo depende de evento de colisão sólida — o colisor sólido só barra movimento, e a física resolve sozinha. Não é lacuna: é o modelo do projeto, em que dano sai de consulta (`Physics2D.OverlapCircle`) e não de sobreposição de colisor.

## Fora do esperado

Nenhum. Todo gatilho tem dono, e todo callback casa com o colisor.

## Os 82 gatilhos

| origem | objeto | forma | propósito | corpo? | callbacks |
|---|---|---|---|---|---|
| Deserto_Hali | `Consumiveis_Deserto/Coletavel_consumivel_agua_cacimba_0` | Circle | Coletavel | não | — |
| Deserto_Hali | `Consumiveis_Deserto/Coletavel_consumivel_agua_cacimba_1` | Circle | Coletavel | não | — |
| Deserto_Hali | `Consumiveis_Deserto/Coletavel_consumivel_agua_cacimba_2` | Circle | Coletavel | não | — |
| Deserto_Hali | `Consumiveis_Deserto/Coletavel_consumivel_agua_cacimba_3` | Circle | Coletavel | não | — |
| Deserto_Hali | `Consumiveis_Deserto/Coletavel_consumivel_erva_ancoragem_0` | Circle | Coletavel | não | — |
| Deserto_Hali | `Consumiveis_Deserto/Coletavel_consumivel_erva_ancoragem_1` | Circle | Coletavel | não | — |
| Deserto_Hali | `Consumiveis_Deserto/Coletavel_consumivel_erva_ancoragem_2` | Circle | Coletavel | não | — |
| Deserto_Hali | `Consumiveis_Deserto/Coletavel_consumivel_raiz_yhtill_0` | Circle | Coletavel | não | — |
| Deserto_Hali | `Consumiveis_Deserto/Coletavel_consumivel_raiz_yhtill_1` | Circle | Coletavel | não | — |
| Deserto_Hali | `Deserto_Root/Coletavel_CartaDasAreias` | Box | Coletavel | não | — |
| Necronomicon | `Necronomicon` | Box | Coletavel | não | — |
| Patua_DasLuasGemeas | `Patua_DasLuasGemeas` | Circle | Coletavel | não | — |
| Patua_Pickup | `Patua_Pickup` | Box | Coletavel | não | — |
| Tumba_De_Alhazred | `Patua_Pickup` | Box | Coletavel | não | — |
| Abdul_Alhazred | `Abdul_Alhazred/Hurtbox` | Box | Hurtbox | não | — |
| Byakhee | `Byakhee/Hurtbox` | Box | Hurtbox | sim | — |
| Castelo_Carcosa | `Player_Damiao/Hurtbox` | Capsule | Hurtbox | sim | — |
| Cultista | `Cultista/Hurtbox` | Box | Hurtbox | sim | — |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_LesteTemploSerpente_0/Hurtbox` | Box | Hurtbox | sim | — |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0 (1)/Hurtbox` | Box | Hurtbox | sim | — |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0 (1)/Hurtbox` | Box | Hurtbox | sim | — |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0 (2)/Hurtbox` | Box | Hurtbox | sim | — |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0 (2)/Hurtbox` | Box | Hurtbox | sim | — |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0 (3)/Hurtbox` | Box | Hurtbox | sim | — |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0 (3)/Hurtbox` | Box | Hurtbox | sim | — |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0/Hurtbox` | Box | Hurtbox | sim | — |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0/Hurtbox` | Box | Hurtbox | sim | — |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0/Hurtbox` | Box | Hurtbox | sim | — |
| Deserto_Hali | `Inimigos_Deserto/Cultista_Setor_TumbaDeAlhazred_0/Hurtbox` | Box | Hurtbox | sim | — |
| Deserto_Hali | `Player_Damiao/Hurtbox` | Capsule | Hurtbox | sim | — |
| EsqueletoInvocado | `EsqueletoInvocado/Hurtbox` | Box | Hurtbox | sim | — |
| PedraDePoder | `PedraDePoder/Hurtbox` | Box | Hurtbox | não | — |
| Player_Damiao | `Player_Damiao/Hurtbox` | Capsule | Hurtbox | sim | — |
| Portoes_Das_Ruinas | `Player_Damiao/Hurtbox` | Capsule | Hurtbox | sim | — |
| Portoes_Das_Ruinas | `Portoes_Root/Byakhee/Hurtbox` | Box | Hurtbox | sim | — |
| Santuario_Yhtill | `Player_Damiao/Hurtbox` | Capsule | Hurtbox | sim | — |
| Tumba_De_Alhazred | `Inimigos_Playtest/Cultista/Hurtbox` | Box | Hurtbox | sim | — |
| Tumba_De_Alhazred | `Inimigos_Playtest/Cultista/Hurtbox` | Box | Hurtbox | sim | — |
| Tumba_De_Alhazred | `Player_Damiao/Hurtbox` | Capsule | Hurtbox | sim | — |
| Tumba_De_Alhazred | `TumbaDeAbdul_Conteudo/Abdul_Alhazred/Hurtbox` | Box | Hurtbox | não | — |
| Cassilda | `Cassilda` | Circle | Interacao | não | — |
| Castelo_Carcosa | `Castelo_Root/Z5_TronoDeAldebaran/Ponto_Focal_anel_sinal_amarelo` | Circle | Interacao | não | — |
| Castelo_Carcosa | `Castelo_Root/Z5_TronoDeAldebaran/Ponto_Focal_necronomicon` | Circle | Interacao | não | — |
| Castelo_Carcosa | `Castelo_Root/Z5_TronoDeAldebaran/Ponto_Focal_patua_luas_gemeas` | Circle | Interacao | não | — |
| Deserto_Hali | `Fragmento_0` | Circle | Interacao | não | — |
| Santuario_Yhtill | `Bau_DeYhtill` | Box | Interacao | não | — |
| Santuario_Yhtill | `Cassilda` | Circle | Interacao | não | — |
| Tumba_De_Alhazred | `Fragmento_1` | Circle | Interacao | não | — |
| Tumba_De_Alhazred | `Fragmento_2` | Circle | Interacao | não | — |
| Tumba_De_Alhazred | `TumbaDeAbdul_Conteudo/Bau_DaTumba` | Box | Interacao | não | — |
| Deserto_Hali | `Deserto_Root/Entrada_TumbaAlhazred` | Box | PortalDeCena | não | PortalDeCena.OnTriggerEnter2D+OnTriggerExit2D |
| Deserto_Hali | `Deserto_Root/Portoes_DasRuinas/Entrada_DosPortoes` | Box | PortalDeCena | não | PortalDeCena.OnTriggerEnter2D+OnTriggerExit2D |
| Deserto_Hali | `Deserto_Root/Santuario_Yhtill` | Box | PortalDeCena | não | PortalDeCena.OnTriggerEnter2D+OnTriggerExit2D |
| Portoes_Das_Ruinas | `Portoes_Root/Passagem_ParaOCastelo` | Box | PortalDeCena | não | PortalDeCena.OnTriggerEnter2D+OnTriggerExit2D |
| Portoes_Das_Ruinas | `Portoes_Root/Volta_AoDeserto` | Box | PortalDeCena | não | PortalDeCena.OnTriggerEnter2D+OnTriggerExit2D |
| Santuario_Yhtill | `Saida_Santuario` | Box | PortalDeCena | não | PortalDeCena.OnTriggerEnter2D+OnTriggerExit2D |
| Tumba_De_Alhazred | `Saida_TumbaAlhazred` | Box | PortalDeCena | não | PortalDeCena.OnTriggerEnter2D+OnTriggerExit2D |
| Tumba_De_Alhazred | `Saida_TumbaAlhazred (1)` | Box | PortalDeCena | não | PortalDeCena.OnTriggerEnter2D+OnTriggerExit2D |
| CoisaDoCemiterio | `CoisaDoCemiterio` | Box | VolumeDeContato | sim | CoisaDoCemiterioAI.OnTriggerEnter2D |
| ConeDeGelo | `ConeDeGelo` | Box | VolumeDeContato | sim | ConeDeGelo.OnTriggerEnter2D |
| Deserto_Hali | `Inimigos_Deserto/CoisaDoCemiterio` | Box | VolumeDeContato | sim | CoisaDoCemiterioAI.OnTriggerEnter2D |
| Castelo_Carcosa | `Castelo_Root/Z1_PortoesInternos` | Box | ZonaDeAmbiente | não | CasteloDeCarcosaZone.OnTriggerEnter2D |
| Castelo_Carcosa | `Castelo_Root/Z1_PortoesInternos/Refugio_DosPortoes` | Circle | ZonaDeAmbiente | não | RefugioDeLuz.OnTriggerEnter2D |
| Castelo_Carcosa | `Castelo_Root/Z2_SalaoDoBanquete` | Box | ZonaDeAmbiente | não | CasteloDeCarcosaZone.OnTriggerEnter2D |
| Castelo_Carcosa | `Castelo_Root/Z3_BibliotecaEsquecida` | Box | ZonaDeAmbiente | não | CasteloDeCarcosaZone.OnTriggerEnter2D |
| Castelo_Carcosa | `Castelo_Root/Z3_BibliotecaEsquecida/Pressao_Psiquica_0` | Circle | ZonaDeAmbiente | não | PressaoPsiquicaZone.OnTriggerEnter2D+OnTriggerExit2D |
| Castelo_Carcosa | `Castelo_Root/Z3_BibliotecaEsquecida/Pressao_Psiquica_1` | Circle | ZonaDeAmbiente | não | PressaoPsiquicaZone.OnTriggerEnter2D+OnTriggerExit2D |
| Castelo_Carcosa | `Castelo_Root/Z3_BibliotecaEsquecida/Pressao_Psiquica_2` | Circle | ZonaDeAmbiente | não | PressaoPsiquicaZone.OnTriggerEnter2D+OnTriggerExit2D |
| Castelo_Carcosa | `Castelo_Root/Z5_TronoDeAldebaran` | Box | ZonaDeAmbiente | não | CasteloDeCarcosaZone.OnTriggerEnter2D |
| Deserto_Hali | `Deserto_Root/Veu_DaTempestade_Templo` | Box | ZonaDeAmbiente | não | VeuDaTempestade.OnTriggerEnter2D |
| Deserto_Hali | `Refugios/Refugio_Entrada` | Circle | ZonaDeAmbiente | não | RefugioDeLuz.OnTriggerEnter2D |
| Deserto_Hali | `Refugios/Refugio_PortoesDasRuinas` | Circle | ZonaDeAmbiente | não | RefugioDeLuz.OnTriggerEnter2D |
| Deserto_Hali | `Refugios/Refugio_SantuarioDeYhtill` | Circle | ZonaDeAmbiente | não | RefugioDeLuz.OnTriggerEnter2D |
| Deserto_Hali | `Setores_Tempestade/Setor_DesertoCentral` | Box | ZonaDeAmbiente | não | TempestadeZonaTrigger.OnTriggerEnter2D |
| Deserto_Hali | `Setores_Tempestade/Setor_Entrada` | Box | ZonaDeAmbiente | não | TempestadeZonaTrigger.OnTriggerEnter2D |
| Deserto_Hali | `Setores_Tempestade/Setor_LesteTemploSerpente` | Box | ZonaDeAmbiente | não | TempestadeZonaTrigger.OnTriggerEnter2D |
| Deserto_Hali | `Setores_Tempestade/Setor_PortoesDasRuinas` | Box | ZonaDeAmbiente | não | TempestadeZonaTrigger.OnTriggerEnter2D |
| Deserto_Hali | `Setores_Tempestade/Setor_SantuarioDeYhtill` | Box | ZonaDeAmbiente | não | TempestadeZonaTrigger.OnTriggerEnter2D |
| Deserto_Hali | `Setores_Tempestade/Setor_TumbaDeAlhazred` | Box | ZonaDeAmbiente | não | TempestadeZonaTrigger.OnTriggerEnter2D |
| Portoes_Das_Ruinas | `Portoes_Root/Gatilho_DaArena` | Box | ZonaDeAmbiente | não | ArenaDosPortoes.OnTriggerEnter2D |
| Portoes_Das_Ruinas | `Portoes_Root/Refugio_DosPortoes` | Circle | ZonaDeAmbiente | não | RefugioDeLuz.OnTriggerEnter2D |
| Santuario_Yhtill | `Refugio_Santuario` | Circle | ZonaDeAmbiente | não | RefugioDeLuz.OnTriggerEnter2D |
