---
type: Game System
title: Ficha de Atributos e Matemática do Combate
description: Atributos base de toda unidade (Vitalidade/Ataque/Defesa/Conjuração/Resistência) e a fórmula de mitigação de dano.
tags: [combat, attributes, balance]
---

# Ficha de Atributos e Matemática do Combate

**Toda unidade do jogo tem uma ficha** — Cultista Amarelo, Damião, Aparição Primordial,
Espectro. Decisão do Vini (2026-07-29): em vez de campos de vida/dano espalhados por cada
`MonoBehaviour`, os números vivem numa ficha autorada como asset.

## Os 5 atributos

| Atributo | Canal | O que faz |
|---|---|---|
| **VitalidadeMax** | — | Teto da [Vitalidade](vitalidade.md) corpórea |
| **Ataque** | Físico | Dano bruto do golpe corpo-a-corpo da unidade |
| **Defesa** | Físico | Mitiga o dano físico recebido |
| **Conjuração** | Anômalo | Dano bruto das magias (0 se a unidade não conjura) |
| **ResistenciaAnomala** | Anômalo | Mitiga o dano de conjuração recebido (defesa mágica) |

Dois canais separados, deliberadamente:

- **Físico:** `Ataque` → mitigado por `Defesa` → fere a **Vitalidade**.
- **Anômalo:** `Conjuração` → mitigado por `ResistenciaAnomala` → drena a **[Resiliência Mental](resiliencia_mental.md)**.

> **Damião é um caso especial no `Ataque`:** o dano dele vem da **arma** equipada
> (Cravo 40, Estilete 25, Alfanje 60), não da ficha. O `Ataque` da ficha do Damião é o
> **golpe desarmado**, que por decisão de design é **0** — o gesto de mão vazia existe
> (faz barulho, entra no estado Atacando) mas não mata. Para os inimigos, o `Ataque` da
> ficha **é** o dano do golpe.

## Fórmula de mitigação (subtrativa com piso)

```
danoFinal = max(danoBruto × 0,15 ,  danoBruto − defesa)
```

A defesa subtrai um valor plano ("a armadura absorve X"), mas o **piso de 15%** garante
que nenhuma pilha de defesa deixe alguém invulnerável — sempre passa um mínimo. Escolhida
(2026-07-29) por ser intuitiva para o jogador e escalar com segurança conforme as
armaduras coletáveis previstas entrarem.

A fórmula é **simétrica**: a mesma função (`MitigacaoDeDano.Aplicar`) resolve o golpe do
Cultista no Damião e o golpe da arma do Damião no Cultista. Vive isolada no Core, testada
em `MitigacaoDeDanoTests`.

## Escala e balanceamento atual

**Escala unificada 0–100** para todos os atores — barra lê-se como porcentagem e o tuning
é fino.

| Ficha | Vitalidade | Ataque | Defesa |
|---|---|---|---|
| `Ficha_Damiao` | 100 | 0 *(desarmado)* | 4 |
| `Ficha_Cultista` | 100 | 24 | 5 |

**Contas que fecham esses números:**

- Golpe do Cultista no Damião: `max(24×0,15 ; 24−4)` = **20** → **5 golpes** para derrubar
  Damião (~6 s no corpo-a-corpo, com cadência de 1,2 s). Punitivo o suficiente para
  empurrar ao stealth, com janela de fuga.
- Armas contra o Cultista (defesa 5): Alfanje `60−5=55` → **2 golpes**; Cravo `40−5=35` →
  **3 golpes**; Estilete `25−5=20` → **5 golpes**.

Para re-balancear, edite os assets em `Assets/FavelaAmarela/Config/` — **sem tocar código**.

## Arquitetura

- POCO `FavelaAmarela.Core.Combat.FichaDeAtributos` — imutável, valida na construção.
- `FichaAtributosConfig` (`ScriptableObject`, Runtime) autora os valores no Inspector e
  produz o POCO via `CriarFicha()`. Um asset por tipo de unidade.
- O Core **não** conhece `ScriptableObject`: o SO nasce por cima do POCO, nunca o contrário.

## Feedback visual (provisório)

Enquanto não há animações de golpe/impacto, todo dano aplicado spawna um **número
flutuante** (`DanoFlutuante`) na posição do alvo — pedido explícito do Vini como forma de
verificar que dano, mitigação e cadência funcionam. Cores distintas: dano no Cultista em
amarelo-pálido, dano no Damião em vermelho. É um **diagnóstico**, substituível por VFX
diegético depois sem tocar o Core.
