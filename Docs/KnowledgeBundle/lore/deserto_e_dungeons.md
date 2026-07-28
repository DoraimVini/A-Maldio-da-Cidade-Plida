---
type: Lore Reference
title: O Deserto de Hali e suas Dungeons
description: Região inicial aberta de dunas de cinzas, complexos subterrâneos ancestrais e o checkpoint de saída para a Fase 2.
tags: [lore, environment, desert, dungeons, hali, yhtill]
timestamp: 2026-07-27T18:55:00Z
---

# O Deserto de Hali e suas Dungeons

O **Deserto das Cinzas de Hali** é a área inicial aberta (Fase 1) de *A Maldição da Cidade Pálida*. Sob o céu tingido pelos sóis gêmeos, o deserto conecta o ponto de entrada de Damião a duas masmorras antigas, ao Santuário de Yhtill e aos Portões das Ruínas que dão saída da fase.

---

## 1. O Deserto Aberto (Dunas de Cinza)
* **Visual:** Dunas de areia amarelada e cinzas cósmicas sopradas por ventos uivantes. Sóis gêmeos esmaecidos no horizonte cor de osso.
* **Mecânicas:** Área de navegação aberta com **tempestades de areia** que variam por setor geográfico, conectando as duas dungeons, o Santuário de Yhtill e os Portões das Ruínas. O **Lago de Hali** no centro divide o mapa em setor sul (entrada, Dungeon 1) e setor norte (Portões, Santuário).
* **Documento de Level Design completo:** `systems/level_design_deserto_hali.md` (2026-07-28) — mapa topológico, zonas de tempestade, pontos de interesse, inimigos errantes e referências visuais.

---

## 2. Dungeon 1: A Tumba de Alhazred (Tumbas Mururats)
* **Localização:** Fenda subterrânea nas ruínas ocidentais do deserto.
* **Inimigos:** Cultistas cegos e estática anômala.
* **Chefe Final:** **Abdul Alhazred** (Miniboss), autor do *Necronomicon*.
* **Nota de implementação (2026-07-28):** esta dungeon corresponde à área já construída em `Assets/Scenes/Playtest_RuinasPalidas.unity` (Zonas 1-9 do S-Path), repropositada — não é conteúdo novo a construir do zero. O stealth urbano, as tempestades e a queda da Zona 4 para a Zona 5 (ver `systems/queda_z4_z5.md`) acontecem dentro desta dungeon, não numa fase separada.

---

## 3. Dungeon 2: O Templo da Serpente (Opcional)
* **Localização:** A definir — achada explorando o Deserto Aberto (não faz parte do caminho obrigatório da Fase 1).
* **Guardião:** O **Chefe Guardião** (nome próprio ainda não cunhado — ver `templo_da_serpente.md`, rascunho).
* **Progresso:** A derrota do Guardião libera a **Coroa de Ossos do Rei em Amarelo**.

---

## 4. Portões das Ruínas (Fim da Fase 1)
* **Localização:** Antecâmara da cidade sagrada de Carcosa, saída do Deserto de Hali.
* **Guardião:** O **Byakhee** (Miniboss alado).
* **Progresso:** A derrota do Byakhee libera o **Anel do Sinal Amarelo**, abre os Portões e marca a transição para a Fase 2 (O Castelo de Carcosa).
