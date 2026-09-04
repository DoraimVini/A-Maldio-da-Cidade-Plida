---
type: GDD
title: Design de Expansão — Vertical Slice Macro (2 Fases)
description: Documento explicativo master da Demo/Vertical Slice completa (Fase 1: Deserto de Hali, Fase 2: Castelo de Carcosa & O Rei em Amarelo).
tags: [design, gdd, vertical-slice, demo, carcosa, king-in-yellow, 32ppu]
timestamp: 2026-07-28T00:00:00Z
---

# A Maldição da Cidade Pálida — Vertical Slice (2 Fases) — Versão 4.0

Documento Mestre de Arquitetura Narrativa, Game Design e Isometria 2D Real (32 PPU) para a demonstração (Demo / Vertical Slice) completa de ***A Maldição da Cidade Pálida (Favela Amarela)***.

> **Nota de escopo (2026-07-28):** esta numeração (Fase 1/Fase 2) é local a este Vertical Slice/demo. É diferente da ambição de "jogo completo com 6 fases" registrada no `GDD_Mestre.md` §1.8 — as duas contagens não foram reconciliadas e não devem ser lidas como a mesma coisa.
>
> **Revisão v4.0:** a antiga "Fase 2: As Ruínas Pálidas" (v3.0) deixou de ser uma fase própria. Seu conteúdo (S-Path, stealth urbano, queda Z4→Z5) foi absorvido como o miolo da Dungeon 1 (Tumba de Alhazred) da Fase 1 — a área já construída em `Assets/Scenes/Tumba_De_Alhazred.unity` foi repropositada, não descartada. O que era "Fase 3" virou "Fase 2".

---

## 1. ESTRUTURA MACRO DO VERTICAL SLICE (2 FASES)

O Vertical Slice é composto por **2 Fases contínuas** de progressão narrativa e de gameplay:

```
[FASE 1: O DESERTO DE HALI] ──────────────────────▶ [FASE 2: O CASTELO DE CARCOSA]
• Deserto Aberto, 32 PPU, 2 Dungeons + Portões       • Interior do Palácio Real
• Dungeon 1 (Tumba de Alhazred) = S-Path repaginado   • Quest do Castelo
  (stealth urbano, tempestades, queda Z4→Z5)          • BOSS FINAL: O REI EM AMARELO
• NPCs Alhazred e Cassilda
• Miniboss Byakhee nos Portões das Ruínas
```

---

## 2. DETALHAMENTO DAS 2 FASES

### 🏜️ FASE 1: O DESERTO DE HALI (Área Inicial Aberta)
* **Estilo de Jogo:** Navegação livre no deserto em 32 PPU, exploração de masmorras e coleta de relíquias.
* **Dungeon 1 (Tumba de Alhazred):** a área já construída (S-Path, Zonas 1-9 da cena `Tumba_De_Alhazred`), repropositada como dungeon dentro do Deserto. Furtividade urbana tensa, gestão de ruído físico, tempestades de areia, a Queda da Zona 4 para a Zona 5 (Cerco dos Cultistas, colapso do chão, travessia subterrânea) e o clímax: Miniboss **Abdul Alhazred** ➔ Drop: **O Necronomicon**.
* **Dungeon 2 (Templo da Serpente - Opcional):** Chefe Guardião ➔ Drop: **Coroa de Ossos do Rei em Amarelo** (efeito/build a definir — aguardando fechamento do roteiro).
* **Santuário de Yhtill:** Rainha Cassilda recita a Canção de Cassilda + Quest ➔ Recompensa: **Patuá das Luas Gêmeas**.
* **Portões das Ruínas (Fim da Fase 1):** Miniboss **Byakhee** ➔ Drop: **Anel do Sinal Amarelo** ➔ Abertura dos Portões ➔ Transição para a Fase 2.

---

### 🏰 FASE 2: O CASTELO DE CARCOSA (Fase Final & Boss)
* **Estilo de Jogo:** Terror psíquico e mistério nos salões majestosos e corrompidos do Palácio de Yhtill.
* **Quest do Castelo:** Desvendando o mistério da Segunda Peça da tragédia e o destino da nobreza fossilizada.
* **CHEFÃO FINAL DA DEMO / DO JOGO:** **O REI EM AMARELO** (*O Avatar de Hastur*).
* **Mecânica da Máscara Pálida:**
  * O Rei em Amarelo não possui barra de vida vulnerável a ataques físicos simples.
  * O confronto é um duelo de sanidade e esquiva psíquica: quando a Máscara Pálida do Rei começar a "cair", Damião **DEVE se virar de costas imediatamente** (olhar direto para a verdadeira face alienígena causa Colapso Psíquico Instantâneo / Game Over).
  * Vencer o Rei em Amarelo exige usar as 4 Relíquias acumuladas para selar o portal de Aldebaran e encerrar a demonstração com o desfecho triunfal da demo.

---

## 3. AS 4 RELÍQUIAS LENDÁRIAS E SUAS FUNÇÕES NAS 2 FASES

> Efeitos ainda não definidos para nenhuma relíquia — o roteiro está em fechamento. A tabela abaixo é só um mapa de proveniência (quem dropa o quê, onde); a coluna de função é referência histórica da v3.0 e **não deve ser tratada como decisão final**.

| Relíquia | Obtenção | Função na Fase 1 (Deserto) | Função na Luta Final (Fase 2: O Rei) |
| :--- | :--- | :--- | :--- |
| **📖 O Necronomicon** | Drop de Alhazred (Dungeon 1: Tumba de Alhazred) | A definir. | A definir. |
| **👑 Coroa de Ossos do Rei em Amarelo** | Drop do Chefe Guardião (Dungeon 2: Templo da Serpente, Opcional) | A definir. | A definir. |
| **🧿 Patuá das Luas Gêmeas** | Quest da Rainha Cassilda (Santuário de Yhtill) | A definir. | A definir. |
| **💍 Anel do Sinal Amarelo** | Drop do Byakhee (Portões das Ruínas) | A definir. | A definir. |

---

## 4. PADRÃO TÉCNICO DE ISOMETRIA 2D REAL (32 PPU)

* **Pixel-Per-Unit (PPU):** **`32`** para todos os sprites.
* **Tile de Chão:** Losangos 2:1 de **`32 x 16 px`**.
* **Grid CellSize:** **`(1.0, 0.5, 1.0)`**.
* **Escala dos Prefabs:** **`Transform.scale = (1.0, 1.0, 1.0)`** nativo.
* **Câmera:** Ortográfica plana sem rotação (`Quaternion.identity`), offset $Z = -10$, `Orthographic Size = 5.5 a 6.0`.
