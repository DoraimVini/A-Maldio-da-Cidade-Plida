---
type: Lore Reference
title: Abdul Alhazred (O Árabe Louco)
description: Detalhes narrativos e mecânicos de Abdul Alhazred como NPC e Miniboss. Inclui a cena de escolha com Yug-Neth e os dois caminhos possíveis.
tags: [lore, npc, miniboss, alhazred, necronomicon, migo, companion]
timestamp: 2026-07-30T13:00:00-03:00
---

# Abdul Alhazred (O Árabe Louco)

## Lore
Abdul Alhazred é o lendário poeta e erudito de Sanaa que passou anos explorando os desertos da Arábia e as ruínas de Nameless City, onde traduziu os segredos dos Antigos no tomo *Al-Azif* (*O Necronomicon*). Enlouquecido pela estática de Carcosa, ele se isolou na Tumba Mururat — onde mantém aprisionado **Yug-Neth**, um Mi-Go filhote, como chave dimensional para os Portões das Ruínas.

## Função no Jogo
* **Localização:** Clímax da Tumba de Alhazred (Dungeon 1 do Deserto de Hali), em `Assets/Scenes/Tumba_De_Alhazred.unity` (S-Path, Zona 9 — Tumba de Abdul).
* **Drop condicional:** *O Necronomicon* — **apenas no Caminho B (luta)**. Ver seção abaixo.
* **Companion libertado:** Yug-Neth — em ambos os caminhos.

---

## O Encontro — Cena de Escolha

Ao entrar na Zona 9, Damião encontra Alhazred em transe flutuante. **Yug-Neth está acorrentado no canto da arena** (correntes anômalas, bioluminescência apagada). Se Damião interagir com as correntes de Yug-Neth antes do grimório, Alhazred acorda e um **diálogo de escolha** é iniciado.

> Design completo da cena (diálogos, bifurcação, consequências) em **[migo_companion.md](migo_companion.md)**.

### Caminho A — Concordar com Alhazred
- Alhazred libera Yug-Neth voluntariamente e some com o Al-Azif.
- ❌ **Necronomicon não obtido** (impacto: sem tradução de Aklo, sem diálogo de Nagaraja traduzido).
- ✅ Yug-Neth se torna companion.
- Sem luta de boss neste caminho.

### Caminho B — Recusar / Lutar
- Alhazred entra em colapso mental → luta começa.
- **Yug-Neth permanece vulnerável na arena durante a luta** (mecânica de proteção ativa).
- ✅ **Necronomicon obtido** após a vitória.
- ✅ Yug-Neth se torna companion.

---

## O Boss Fight (Caminho B)

### Fase NPC (Pre-Fight)
Alhazred entra em colapso após a recusa de Damião. A estática de Carcosa toma conta da sala.

> ⚠️ **Mecânica de Proteção Ativa durante a luta:**
> Yug-Neth permanece no canto da arena com uma barra de **Resiliência do Companheiro (RC)**. Se a RC de Yug-Neth chegar a zero (por Cones de Gelo ou Summons de Alhazred) → **GAME OVER**. Damião deve se posicionar entre Alhazred e Yug-Neth para bloquear projéteis.

### Atributos Base
- **Mobilidade:** Flutuante. Usa teleporte para se reposicionar.
- **Vulnerabilidade:** Protegido por um Escudo Mágico. O escudo dita o ritmo do combate e o acesso ao dano real.
- **Tipo de Dano Causado:** Dano Mágico (Frio/Gelo), Dano Físico baixo (pelos summons) e Dreno de Sanidade passivo.

### Estrutura da Luta (2 Fases)

**Fase 1 (100% a 35% de vida)**
- Alhazred está protegido por um Escudo Mágico impenetrável.
- Ele conjura **pequenos esqueletos** para atrapalhar e flanquear o jogador.
- Para causar dano, Damião precisa quebrar **Pedras de Poder** espalhadas pela arena.
- Quebrar uma Pedra desativa o escudo temporariamente. Após um tempo (ou dano recebido), ele reconjura as pedras.
- **Atenção:** Os esqueletos podem se mover na direção de Yug-Neth — Damião deve interceptá-los.

**Fase 2 (Menos de 35% de vida)**
- O escudo mágico se torna **permanente** (não depende mais das pedras).
- Ele conjura **Cones de Gelo** — projéteis que podem acertar Yug-Neth se não bloqueados.
- **Mecânica de Debuff:** 3 stacks de gelo → Damião congela (Stun) por curto período.
- **Janela de Vulnerabilidade:** Após conjurar 3 magias, a "mana" de Alhazred esgota temporariamente, o escudo cai → única janela de golpe de misericórdia.

---

## Decisões de Conteúdo

**2026-07-28:** O miniboss genérico "Vulto" reservado para a arena `Zona9_TronoDoVulto` **não será implementado**. O miniboss da arena é Abdul Alhazred.

**2026-07-29 — Renomeação:** A arena é `Zona9_TumbaDeAbdul`. A luta está implementada — ver [systems/boss_abdul.md](../systems/boss_abdul.md).

**2026-07-29 — Layout:** A dungeon ganhou a `Zona6b_CamaraDoBau`, sala lateral com baú de armas, antes das Zonas 6-8.

**2026-07-30 — Revisão do Encontro:** O encontro com Alhazred foi expandido para um **sistema de escolha binária** que determina:
- Se o Necronomicon é obtido (somente via luta)
- O tom da relação de Damião com Yug-Neth (libertação por respeito vs. libertação por vitória)
- Design completo: [migo_companion.md](migo_companion.md)

