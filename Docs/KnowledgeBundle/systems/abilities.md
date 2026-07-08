---
type: Game System
title: Habilidades Anômalas
description: Sistema de poderes sobrenaturais baseado na interface IAnomalyPower.
tags: [abilities, powers, anomaly, design-pattern]
timestamp: 2026-07-07T11:00:00Z
---

# Sistema de Habilidades Anômalas

O protagonista Damião possui acesso a poderes sobrenaturais (Anomalias) causados pela influência de Carcosa. Cada habilidade anômala **custa Resiliência Mental** — distorcer a realidade cobra um preço na sanidade.

## Contrato: IAnomalyPower

Toda habilidade anômala implementa a interface `IAnomalyPower`:

| Membro | Retorno | Descrição |
|--------|---------|-----------|
| `PowerName` | `string` | Nome diegético da habilidade |
| `CanActivate(currentResilience, timeSinceLastUse)` | `bool` | Verifica cooldown e custo de RM |
| `Execute(currentResilience)` | `PowerResult` | Executa e retorna resultado |

## PowerResult

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `Success` | `bool` | Se a habilidade foi executada |
| `DurationSeconds` | `float` | Duração do efeito |
| `CooldownSeconds` | `float` | Tempo até poder usar de novo |
| `ResilienceCost` | `float` | Quanto de RM foi consumido |

## Habilidades Implementadas

- [Salto Dimensional](dimensional_leap.md) — Ghost Dash (implementa `IAnomalyPower`)
- [Esquiva](esquiva.md) — Dodge físico (**NÃO** implementa `IAnomalyPower` — sem custo de RM)

## Regra de Design

Habilidades que distorcem a realidade de Carcosa **DEVEM** implementar `IAnomalyPower` e ter custo de Resiliência Mental. Habilidades puramente físicas (como a Esquiva) **NÃO** implementam essa interface.

## Contrato: IArma (Famílias de Arma)

Espelha `IAnomalyPower`, mas para armas equipadas na **Mão Física** — mundanas, sem custo de RM. Cada família de arma implementa `IArma` e define seu próprio "verbo de combate" (composição, não uma árvore de herança).

| Membro | Retorno | Descrição |
|--------|---------|-----------|
| `NomeDaArma` | `string` | Nome diegético da arma |
| `CanActivate(timeSinceLastUse)` | `bool` | Só cooldown, sem custo de recurso |
| `Execute()` | `ArmaResult` | Executa e retorna resultado (`Success`, `DurationSeconds`, `CooldownSeconds`, `Atordoou`, `DuracaoAtordoamento`) |

### Famílias de arma planejadas (roadmap, decisão de 2026-07-07)

Inspirado em *Source of Madness*: equipamento em **2 slots** (Mão Física + Mão Anômala), não uma grade de inventário. Famílias candidatas:

| Família | Tipo | Status |
|---------|------|--------|
| **Barra Enferrujada** | Física (`IArma`) | ✅ Implementada (Core) — 35% de chance de atordoar por golpe |
| **Lâmina do Sinal** | Física (`IArma`) | Planejada — bônus de dano se atacar por trás em modo Furtivo |
| **Talismã do Vento Negro** | Anômala (`IAnomalyPower`) | Planejada — empurra inimigos, custa RM |
| Garra Enegrecida, Vidro da Máscara | — | Ideias de brainstorm, não priorizadas ainda |

A Barra Enferrujada não garante atordoamento a cada golpe — a chance é decidida pela própria arma (`Func<double>` injetável pra testes determinísticos), e a FSM do alvo (ver [IA do Cultista](cultista_ai.md)) só executa o atordoamento quando mandada via `AtordoarPor(duração)`.

### Gating da Mão Física (progressão)

Igual ao Salto Dimensional: Damião **não nasce armado**. `MaoFisicaBridge.desbloqueadaNoInicio` é `false` por padrão, e `TryAtacar()` retorna sem efeito até `DesbloquearArma()` ser chamado — previsto pra acontecer junto do pickup do patuá na Zona 5 (mesma zona, mesmo momento de virada de poder: Damião sai da Zona 4 indefeso e chega na Zona 5 com Salto **e** arma de uma vez).

## Guardrails de Combate

Ver o documento agnóstico de projeto em `Studio_Knowledge_Base/generic_systems/realtime_combat_patterns.md` (referência Death Trash > Last Epoch — combate lento, com peso e consequência, não build-heavy). Regras específicas *deste* projeto:

- Combate continua **secundário ao stealth** — é a exceção arriscada, não o loop principal. Nada de encontros com hordas.
- **Números de dano flutuantes são desejados** — não é incompatível com terminologia diegética, só troca o rótulo (ex.: "-12 Trauma" em vez de "-12 HP" genérico). Ainda não implementado; quando for, seguir a skill `favela-lore-enforcer` pro texto exato.
- Evitar HUD de vida numérica gigante estilo boss fight, e evitar árvore de talentos com dezenas de modificadores simultâneos — o "inventário enxuto" (seção 1 do `CLAUDE.md` raiz) continua valendo.
