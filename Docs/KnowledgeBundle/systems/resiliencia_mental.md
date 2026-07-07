---
type: Game System
title: Resiliência Mental
description: Sistema central que gerencia o estresse e a "sanidade" do protagonista durante o gameplay.
tags: [gameplay, mechanics, ui, core-loop]
timestamp: 2026-07-07T10:00:00Z
---

# Resiliência Mental

A **Resiliência Mental** é o principal medidor de saúde não-física do protagonista em "A Maldição da Cidade Pálida". Diferente de uma barra de "HP" tradicional, ela flutua constantemente com base em eventos da cidade, interações com anomalias e tempo de exposição ao ambiente hostil.

## Regras de Negócio e Cálculos

O valor de Resiliência Mental (RM) vai de `0.0` a `100.0`.

### Fatores de Dreno
- **Exposição (Passiva):** -0.5 RM por segundo se em zona de alta anomalia.
- **Dano Direto (Ativa):** Ataques de inimigos drenam entre 10 e 25 RM instantaneamente.

### Fatores de Recuperação
- **Pontos de Luz:** Ficar sob postes de luz seguros restaura 2.0 RM por segundo.
- **Consumíveis:** Itens como "Chá Calmante" restauram 40 RM.

## Efeitos de Status (Thresholds)
- **>= 80 RM (Focado):** A visão fica levemente mais clara, [PlayerMovement](../scripts/runtime/player_movement_cs.md) ganha +10% de velocidade de movimento.
- **< 30 RM (Desesperado):** Visão de túnel, sons distorcidos, probabilidade de errar interações.
- **= 0 RM (Ruptura):** Game Over ou transição para estado de "Sombra".

## Implementação Técnica
A lógica principal está implementada em `ResilienciaMental.cs` e `ResilienciaBar.cs`.
A interface gráfica usa eventos C# padrão (`event Action<float> OnResilienciaChanged`) para desacoplar a UI da lógica de gameplay.
