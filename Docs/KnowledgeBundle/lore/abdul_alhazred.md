---
type: Lore Reference
title: Abdul Alhazred (O Árabe Louco)
description: Detalhes narrativos e mecânicos de Abdul Alhazred como NPC e Miniboss.
tags: [lore, npc, miniboss, alhazred, necronomicon]
timestamp: 2026-07-27T18:55:00Z
---

# Abdul Alhazred (O Árabe Louco)

## Lore
Abdul Alhazred é o lendário poeta e erudito de Sanaa que passou anos explorando os desertos da Arábia e as ruínas de Nameless City, onde traduziu os segredos dos Antigos no tomo *Al-Azif* (*O Necronomicon*). Enlouquecido pela estática de Carcosa, ele se isolou na Tumba Mururat.

## Função no Jogo
* **Fase NPC:** Inicialmente encontrado murmurando trechos poéticos proibidos em Aklo.
* **Transição:** Ao tentar acessar o tomo, Alhazred sofre colapso mental e se corrompe em um **Miniboss Anômalo**.
* **Ataques:** Evocação de ventos congelantes e círculos de dreno de Resiliência Mental.
* **Drop:** *O Necronomicon*.
* **Localização:** Clímax da Tumba de Alhazred (Dungeon 1 do Deserto de Hali), a área já construída em `Assets/Scenes/Playtest_RuinasPalidas.unity` (S-Path, Zonas 1-9).

## Decisão de conteúdo (2026-07-28)
Resolvido: o miniboss genérico "Vulto" que `LevelBlockoutTypes.cs` reservava para a arena "Zona9_TronoDoVulto" **não será implementado agora**. O miniboss da arena é o **Abdul Alhazred**, que dropa o Necronomicon ali. A renomeação da zona/constante de código (`Zona9_TronoDoVulto` → algo como `Zona9_TronoDeAlhazred`) fica para quando a Fatia 4 do roadmap (repropriar a cena como Dungeon 1) for executada.
