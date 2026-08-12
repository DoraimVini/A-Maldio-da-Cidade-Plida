---
type: Game System
title: Estado do Ambiente
description: Sistema que modela o estado corrente do mundo de Carcosa e suas zonas de anomalia.
tags: [environment, world, anomaly, atmosphere]
timestamp: 2026-07-07T11:00:00Z
---

# Estado do Ambiente

O mundo de Carcosa é dividido em zonas com diferentes níveis de anomalia. O `EnvironmentState` (Core) modela o estado corrente do ambiente que afeta outros sistemas.

## Conceito

Diferentes áreas do mapa possuem diferentes intensidades de influência de Hastur/Carcosa. Zonas de alta anomalia:
- Drenam [Resiliência Mental](resiliencia_mental.md) passivamente
- Alteram comportamento dos [Cultistas](cultista_ai.md)
- Modificam a atmosfera visual e sonora (via adapters)

## Integração

- Alimenta o dreno passivo de RM (ver "Fatores de Dreno" em [Resiliência Mental](resiliencia_mental.md))
- Pode alterar raio efetivo de [Propagação Sonora](sound_propagation.md) (sons viajam mais longe em zonas anômalas)
- Adapters visuais (shaders, iluminação) observam mudanças de estado

## Tempestade de Areia (StormIntensity)

> **Decisão de design (2026-07-28):** A tempestade de areia foi **relocada da Dungeon 1 (Tumba de Alhazred) para o Overworld do Deserto de Hali**. Subterrâneos não têm tempestade. A dungeon passa a ter StormIntensity = 0 por padrão. **FEITO em 2026-07-30:** todos os triggers de tempestade (incluindo o `Z5_Nula`) e o `TempestadeAmbiente`/overlay foram **removidos da cena** `Playtest_RuinasPalidas.unity`, junto com o resto do legado das Ruínas Pálidas — a Tumba virou uma dungeon única e fechada. Os scripts continuam no projeto, prontos para o Overworld. Ver `systems/level_design_deserto_hali.md` §3 para o novo zoneamento do overworld.

`EnvironmentState.StormIntensity` (0..1) hoje só afeta o abafamento sonoro
(`PlayerStealthState.AplicarAbafamentoTempestade`) — raio de ruído do
jogador é reduzido em tempestade forte, o que paradoxalmente **ajuda** a
furtividade sonora.

**Em construção (2026-07-08):** uma camada visual de baixa visibilidade,
pra reforçar a atmosfera da abertura do jogo e desacelerar o jogador por
cautela (não por redução de velocidade). Peças:

- `EnvironmentState.OnStormIntensityChanged` (evento, ✅ implementado) —
  dispara só quando o valor muda de verdade.
- `TempestadeOscilador` (Core, ✅ implementado, testado) — POCO que varia a
  intensidade suavemente entre um mínimo e máximo (rajadas de vento via
  onda senoidal), em vez de um número estático por zona.
- `TempestadeAmbiente` (Runtime, ✅ implementado) — tica o oscilador em
  `Update()` e empurra o valor pro `EnvironmentState` injetado pelo
  `GameManager`.
- `TempestadeZonaTrigger` (Runtime, ✅ implementado) — redefine a faixa
  min/max do oscilador ao entrar numa zona. Diferente dos outros triggers
  de progressão, dispara **toda vez** que o jogador entra, não só uma vez.
- `TempestadeVisualOverlay` (Runtime/UI, ✅ implementado, família do
  `ScreenFader`) — se inscreve em `OnStormIntensityChanged` e ajusta o
  alpha de um véu semitransparente na tela, reduzindo visibilidade por
  cautela sem mexer em velocidade de movimento.
- `AgendadorDeRajada` (Core, ✅ implementado, testado) — POCO que decide
  *quando* uma rajada forte acontece (RNG injetável, mesmo padrão de
  `BarraEnferrujada`), sem calcular intensidade — só expõe `EstaEmRajada`.
- `TempestadeRajadaAleatoria` (Runtime, ✅ implementado) — variante do
  `TempestadeZonaTrigger` para zonas com rajadas: tica o `AgendadorDeRajada`
  só enquanto o jogador está dentro do trigger e alterna a faixa do
  `TempestadeAmbiente` entre calmaria e rajada.
- ✅ Colocado em cena (2026-07-10): `TempestadeAmbiente` global, o véu de
  UI e 4 triggers de zona — ver tabela abaixo.

### Zoneamento (colocado em cena, 2026-07-10)

> **Nota (2026-07-28):** Este zoneamento pertencia à "Fase 1: Ruínas Pálidas" original. Com a reestruturação, a tempestade de areia foi **relocada para o Overworld do Deserto de Hali**. **FEITO em 2026-07-30:** todos os triggers de tempestade (incluindo o `Z5_Nula`) e o `TempestadeAmbiente`/overlay foram **removidos da cena** `Playtest_RuinasPalidas.unity`, junto com o resto do legado das Ruínas Pálidas — a Tumba virou uma dungeon única e fechada. Os scripts continuam no projeto, prontos para o Overworld. Ver `systems/level_design_deserto_hali.md` §3.

4 triggers de tempestade na cena `Playtest_RuinasPalidas`, sobre o `TempestadeAmbiente` global:

| Trigger | Zona | Componente | Faixa |
|---------|------|------------|-------|
| `TempestadeTrigger_Z1_Spawn` | Z1 (Rua de Entrada) | `TempestadeZonaTrigger` | 0.2–0.6 (moderada) |
| `TempestadeTrigger_Z2_Rajadas` | Z2 (Vila das Casas) | `TempestadeRajadaAleatoria` | calma 0.1–0.3, rajada 0.6–0.9 (aleatória, intervalo 8–15s, duração 4s) |
| `TempestadeTrigger_Z3Z4_Forte` | Z3 (Beco do Vento) + Z4 (Praça do Cerco) | `TempestadeZonaTrigger` | 0.6–0.9 (forte e estável) |
| `TempestadeTrigger_Z5_Nula` | Z5 (Subterrâneo) | `TempestadeZonaTrigger` | 0–0 (nula — área fechada, sem céu) |

Z3+Z4 usam um único trigger combinado porque são fisicamente contíguas (chão de Z3 termina onde começa o de Z4). Coordenadas/tamanho exatos dos `BoxCollider2D` foram calculados a partir da geometria real dos pisos de cada zona; ajustar em play-test se necessário.

**Correções (2026-07-17):**
- O pulo Z4→Z5 é um teleporte (`rb.position = destino`), e o teleporte **adormece o Rigidbody**, que não gera `OnTriggerEnter2D` — por isso o `Z5_Nula` não disparava no pouso e a tempestade forte da Z3/Z4 "vazava". O `QuedaZ4Z5Trigger` agora chama `GameManager.TempestadeAmbiente.DefinirFaixa(0,0)` **explicitamente** após o teleporte.
- Os colliders de `Z3Z4_Forte` e `Z5_Nula` foram realinhados para se encontrarem **exatamente na barreira de anomalia (y = -30.25)** — antes o `Z3Z4_Forte` vazava até -30.5, então andar de volta pra barreira dentro da Z5 religava a tempestade.
