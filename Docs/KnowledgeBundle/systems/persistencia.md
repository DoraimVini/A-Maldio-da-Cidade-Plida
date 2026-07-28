---
type: Game System
title: Persistência (Save)
description: Esqueleto de salvamento de progresso — SaveData JSON, SaveSystem e ResilienciaMental.Restaurar.
tags: [save, persistence, json, poco, fase1]
timestamp: 2026-07-16T12:00:00Z
---

# Persistência (Save)

Esqueleto de salvamento do progresso de Damião (Fase 1, Slice 4). Segue a regra §9 do CLAUDE.md: **JSON com classes POCO `[Serializable]`, nunca `PlayerPrefs`** para dados de progresso.

> **Estado (2026-07-16):** *esqueleto* implementado e testado — a estrutura de dados, a IO em disco e o gancho de restauração existem e passam nos testes. O **gatilho** de save já está decidido no GDD §8.3 (salvar nos **Postes de Luz / Refúgios**); o que falta é *implementá-lo* — o código que monta o `SaveData` ao tocar um poste — e a **orquestração no `GameManager`**. Isso é um slice futuro, deliberadamente fora deste esqueleto.

## Peças

| Peça | Camada | Papel |
|------|--------|-------|
| `SaveData` | Core (`Core.Persistence`) | DTO `[Serializable]` do estado de progresso. POCO puro (só `System`, sem UnityEngine). Campos públicos em camelCase (exigência do `JsonUtility`). |
| `ResilienciaMental.Restaurar(float)` | Core (`Core.Combat`) | Define a Resiliência a um valor absoluto salvo (clampado a [0, Max]), disparando `OnChanged` pra UI ressincronizar. Reusa o `Alterar` privado, então os flags de transição (entrou em pânico/colapso) vêm de graça. |
| `SaveSystem` | Runtime (`Runtime.GameLoop`) | Adaptador de IO: serializa/deserializa `SaveData` em JSON sob `Application.persistentDataPath` (slot único `save.json`). Toda IO é defensiva (try/catch + `Debug.LogError`, nunca estoura pro chamador). |

## O que o SaveData captura

| Campo | Fonte no jogo |
|-------|---------------|
| `versao` (int) | Versão do formato (migração futura); começa em 1 |
| `resilienciaAtual` (float) | `GameManager.Resiliencia.Atual` |
| `saltoDesbloqueado` (bool) | `AnomalyPowerBridge.SaltoDesbloqueado` (destravado pelo patuá da Z5) |
| `armaDesbloqueada` (bool) | `MaoFisicaBridge.ArmaDesbloqueada` (Barra Enferrujada) |
| `posX`, `posY` (float) | Posição de Damião no mundo |

O "patuá coletado" **não** é um campo próprio — ele *é* o que destrava o Salto (`PatuaPickup` chama `DesbloquearSalto`), então `saltoDesbloqueado` já o cobre.

## API do SaveSystem

- `bool ExisteSave()` — se há arquivo de save no disco.
- `void Salvar(SaveData)` — serializa e grava (sobrescreve o slot único).
- `SaveData Carregar()` — lê e deserializa; devolve `null` se não existir ou se o parse falhar.
- `void Apagar()` — remove o save (novo jogo / colapso final).

## Fora de escopo (slices futuros)

- **Implementar o gatilho de save nos Postes de Luz (Refúgios)** — *decidido* no GDD §8.3; falta o código que monta o `SaveData` e chama `SaveSystem.Salvar` quando Damião toca/descansa num poste.
- **Orquestração no `GameManager`**: montar o `SaveData` a partir do estado corrente e reaplicá-lo no load (incluindo teleporte pra `posX`/`posY` e reaplicar os unlocks nos bridges).
- **Múltiplos slots**: hoje é slot único.

## Testes

- `SaveDataTests` — round-trip JSON (`JsonUtility.ToJson` → `FromJson` preserva todos os campos) + default de `versao`.
- `ResilienciaMentalTests` (seção I) — `Restaurar` define/clampa o valor, dispara `OnChanged`, marca `EntrouEmPanico` ao cair pra faixa de pânico, e não dispara evento se o valor salvo já for o atual.
