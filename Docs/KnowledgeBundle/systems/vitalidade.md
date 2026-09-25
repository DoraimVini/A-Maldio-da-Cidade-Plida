---
type: System
title: Vitalidade Corpórea
description: Recurso de vida física (a "carne"), distinto da Resiliência Mental; zerá-lo abate o ator.
---

# Vitalidade Corpórea

A **Vitalidade** é a barra de vida **física** (a "carne") de um ator — Cultista Amarelo,
Aparição Primordial ou o próprio Damião. É **distinta da [Resiliência Mental](resiliencia_mental.md)**:
são dois vetores de derrota separados, decisão de design do Vini (2026-07-29).

| Recurso | O que modela | Zerar significa |
|---|---|---|
| **Resiliência Mental** | Sanidade / horror cósmico | **Colapso** (derrota psicológica) |
| **Vitalidade** | Integridade corpórea / carne | **Abatido** (morte física) |

> **Por que duas barras?** O núcleo do jogo é stealth + horror, mas o combate aberto da
> Tumba de Alhazred precisa de uma consequência corpórea concreta ("bater mata"). A
> Resiliência Mental continua sendo o recurso do horror; a Vitalidade é o recurso da
> pancadaria. O precedente já existia no GDD (ver `combate.md`, "Instinto de Rato",
> que já citava "Vida ou Resiliência Mental").

## Regras

- **Começa cheia** (`Atual = Max`) na construção.
- **`Ferir(valor)`** — aplica dano físico (equivalente diegético de *TakeDamage*), clampado a zero.
- **`Curar(valor)`** — cura física, clampada ao máximo.
- **`Restaurar(valor)`** — reconstrução de estado a partir de save (não é dano/cura diegético).
- **`EstaAbatido`** — `Atual <= 0`. No evento em que cruza para zero, `VitalidadeChangedArgs.AcabouDeAbater` fica `true` **uma única vez** — é o gatilho para tirar o inimigo de cena / disparar a derrota do jogador.
- Não dispara `OnChanged` se o valor não muda de fato (ex.: ferir um alvo já abatido).

## Aplicação atual

- **Cultista Amarelo (`CultistaAI`):** tem `vitalidadeMax` (default 20) no Inspector.
  `ReceberGolpe` consome `ArmaResult.Dano`; ao ser abatido, o Cultista é removido de cena
  (`Destroy`, futuramente com animação de queda / drop).
- **Golpe desarmado** chega com `Dano = 0`, então **não fere** — coerente com a decisão de
  que o ataque de mão vazia existe mas não mata (ainda não implementado o gesto desarmado).

## Arquitetura

POCO puro em `FavelaAmarela.Core.Combat.Vitalidade`, espelhando o contrato da
`ResilienciaMental`: estado só-leitura, mutação por métodos explícitos, evento `OnChanged`
com `readonly struct VitalidadeChangedArgs` (sem alocação em hot path). 10 testes EditMode
em `VitalidadeTests.cs`. Terminologia segue a skill `favela-lore-enforcer` (dano físico =
*Ferir*, morte = *Abatido*, distinto de *Trauma*/*Colapso* da sanidade).

## Pendências relacionadas

- Vitalidade do **Damião** + barra no HUD (peça 2 — inimigo fere o jogador).
- Vitalidade do **Abdul** com fases (peça 5 — boss).
- **Sangramento** (DoT da habilidade do Estilete de Irem) aplicado como `Ferir` ao longo do tempo.
