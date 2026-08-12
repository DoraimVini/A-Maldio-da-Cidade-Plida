---
type: Lore
title: Arcos Narrativos — Guideline Mestre
description: Estrutura dos arcos narrativos do Vertical Slice, cruzada com o estado real de implementação. Fonte para qualquer decisão de escrita, quest ou cutscene.
tags: [narrativa, arcos, guideline, gdd]
timestamp: 2026-08-10T00:00:00Z
---

# Arcos Narrativos — A Maldição da Cidade Pálida (Favela Amarela)

> **Como usar este documento:** é a referência para qualquer decisão de escrita, diálogo,
> quest ou cutscene. Fonte primária: [GDD Unificado v3.0](https://sixth-periodical-98a.notion.site/GDD_Unificado_Peregrino_Amarelo-3ad05465f1ab8066940ae4278c78bbf2) (Notion, 30/07/2026) +
> `GDD_Mestre.md` local, cruzados com o estado real do código em 2026-08-10. Onde os dois
> divergem, este documento sinaliza explicitamente — **não resolva a divergência sozinho,
> confirme com o Vini**. Terminologia sempre via [glossary.md](glossary.md).

## 1. Premissa Canônica (não mexer sem aprovação)

**Damião**, jovem da **Favela do Rato Baleado** (Rio de Janeiro), estudante de Direito, entra
para o tráfico local (com **Juninho**) para pagar a faculdade. Tocado pelo sonho de
**Hastur**, descobre que uma ONG da favela (fachada de **Natasha**) é uma seita de adoração
ao Rei em Amarelo. Ao tentar proteger os amigos e **Martha** de um ritual numa igreja, é
capturado e sacrificado — ventre cortado com a lâmina que marca o símbolo de Hastur.

Damião morre na Terra e acorda no **Deserto de Hali**, em **Carcosa**. O que parece
travessia pós-morte é na verdade uma **peregrinação por memórias arrancadas**: a diáspora
africana, o quilombo de **Malik Nazinga**, a favela, a seita, a própria morte, o futuro de
Martha. Carcosa não é só uma dimensão alienígena — é uma **máquina cósmica de captura de
memória e identidade**.

### A Dualidade Central
- **O Rei em Amarelo:** Avatar supremo de Hastur, governa Carcosa do trono do palácio.
- **A Criatura Amarela (O Observador):** não é Hastur nem o Rei em Amarelo — é a **forma
  atemporal do próprio Damião** após sua transformação final na matéria amarela de Carcosa.
  Ele se torna o Observador que atravessa tempestades de memória observando a si mesmo no
  passado (a figura misteriosa dos pesadelos da boca de fumo, no curta-metragem). **Toda
  aparição da Criatura Amarela no jogo é, narrativamente, Damião olhando para si mesmo.**

## 2. Estrutura Macro do Vertical Slice (2 Fases)

O jogo completo tem **6 fases**; o Vertical Slice recorta deliberadamente **abertura +
desfecho** (Fase 1 + última fase), pulando as 4 do meio. É um corte de **pitch**, não de
continuidade — a Fase 1 mostra o loop de jogo, o Castelo mostra onde ele desemboca. Ver
[roadmap_vertical_slice.md](../roadmap_vertical_slice.md) §"Consequências da decisão de
escopo" para o raciocínio completo.

```
[FASE 1: O DESERTO DE HALI] ───────────────────▶ [FASE 2: O CASTELO DE CARCOSA]
Entrada (Garganta de Pedra Pálida)                Interior do Palácio Real de Yhtill
Ruínas Submersas + Patuá de Malik                 Galerias da Burocracia do Horror
Dungeon 1 — Tumba de Alhazred (Baú RNG)           Filas de Almas Esvaziadas + Espectros Roxos
Santuário de Yhtill (Cassilda + Fragmentos)       Sidequest: Avatar de Nyarlathotep (Set 4/4)
Dungeon 2 — Templo da Serpente (opcional)         CHEFE FINAL: O REI EM AMARELO
Portões das Ruínas (Boss Byakhee + Yug-Neth)
```

### Estado real por beat (auditado em 2026-08-10, código = verdade)

| Beat narrativo | Papel no arco | Estado no código |
|---|---|---|
| Sacrifício em Terra → despertar em Hali | Gancho de abertura, estabelece o mistério | Não verificado nesta rodada — checar cutscene/scene de abertura |
| Ruínas Submersas + Patuá de Malik | 1ª pista da linha ancestral (Damião→Malik→Kalunga→Martha) | `PatuaPickup.cs` existe; conectar com narrativa a confirmar |
| **Tumba de Alhazred (Dungeon 1)** | Introduz combate, armas RNG, Yug-Neth cativo | ✅ **Jogável de ponta a ponta** — combate completo, Boss Abdul com 2 fases, conversa ramificada (lutar × concordar), Yug-Neth libertável |
| **Santuário de Yhtill — Quest Cassilda** | Sub-arco de redenção/memória musical | ✅ **Jogável de ponta a ponta** — mas GDD/lore original previa **5 Fragmentos**; o implementado é **3 + recital das 2 estrofes finais** (ver [cassilda_e_byakhee.md](cassilda_e_byakhee.md) §I.5). Divergência assumida, não é bug. |
| **Templo da Serpente (Dungeon 2)** | Conteúdo opcional, Nagaraja + Avatar de Set | ⚠️ Scripts dos bosses existem (`NagarajaAI.cs`, `AvatarDeSetAI.cs`), mas a **cena jogável não está presente neste checkout** — o trabalho de montagem (12 zonas, pintura de chão, colisão perimetral) foi feito localmente após 2026-08-06 e não chegou a ser sincronizado; ver nota de recuperação no fim deste documento. |
| **Portões das Ruínas — Boss Byakhee** | Fecha a Fase 1, entrega Yug-Neth como chave dimensional | ❌ **Zero código.** O GDD trata o Byakhee como entidade pronta no bestiário — na prática é só design. |
| **Castelo de Carcosa (Fase 2 inteira)** | Desfecho — mostra "onde o jogo vai" | ❌ **Zero código.** Nenhuma cena, nenhum script de Espectro Roxo, nenhuma mecânica de burocracia do horror. |
| **Rei em Amarelo (chefe final)** | Clímax do Vertical Slice | ❌ **Zero código.** A mecânica de "virar de costas quando a Máscara cai" não existe em nenhuma forma. |

> Para o estado item-a-item completo contra a lista priorizada do edital, ver
> [roadmap_vertical_slice.md](../roadmap_vertical_slice.md) — é a fonte mais atualizada e
> deve ser consultada antes de assumir que algo do GDD já está pronto.

## 3. Sub-Arcos de Personagem

### 3.1 Yug-Neth (companheiro Mi-Go)
Encontrado cativo na arena de Abdul Alhazred (Zona 9). A decisão de libertá-lo ramifica:
- **Caminho A (concordar com Alhazred):** liberta Yug-Neth sem luta; Alhazred leva o
  Necronomicon; Damião perde a tradução de Aklo.
- **Caminho B (recusar/lutar):** boss fight; vitória dá Necronomicon **e** liberta Yug-Neth.

**Divergência de mecânica (código diverge do GDD, decisão deliberada de 30/31-07-2026):**
o GDD descreve uma barra própria de **Resiliência do Companheiro (RC)** que, ao chegar a 0,
causa **Game Over por Colapso do Companheiro**. O implementado usa a barra de **Vitalidade
comum** e, se Yug-Neth cai, o resultado é **incapacitação recuperável** — ele bloqueia os
Portões, e é reanimado num `RefugioDeLuz`. Não é mais permadeath de companheiro. Ver
[companheiro_mi_go.md](../systems/companheiro_mi_go.md). **Se for escrever diálogo/reação
em torno da "morte" de Yug-Neth, escreva para a incapacitação recuperável, não para um
game over — é o que existe de fato.**

### 3.2 Abdul Alhazred
Miniboss/NPC da Tumba (Zona 9). Ponto de decisão binário (ver 3.1). Guarda o Necronomicon e
Yug-Neth. ✅ Implementado por completo, incluindo a traição da trégua se o jogador tentar
os dois caminhos.

### 3.3 Rainha Cassilda
Quest "A Canção Incompleta" no Santuário de Yhtill. Ramificação A/B/C cosmética no primeiro
encontro (via `PainelDeEscolha`). Entregar os 3 fragmentos abre um recital sem punição das
2 estrofes finais antes do Patuá das Luas Gêmeas. Progresso atravessa cenas via save; a
escolha do primeiro encontro e o recital em si **não são persistidos** (decisão de design).

## 4. Glossário e Terminologia

Nunca usar termos genéricos de RPG em texto visível ao jogador. Tabela completa em
[glossary.md](glossary.md) — pontos-chave: **Resiliência Mental** (não "HP"/sanidade
genérica), **Colapso** (não "Game Over"), **Ancorar** (não "curar"), **Cultista Amarelo**
(não "inimigo genérico"), **Errante** (não "patrol").

## 5. Divergências Técnicas Notadas (fora do escopo narrativo, mas relevantes)

- **PPU:** ~~era listado aqui como divergência (16 vs 32) — correção 2026-08-10: não é uma
  divergência real.~~ O GDD (Notion, `32 PPU`) está **correto**. O projeto foi deliberadamente
  promovido de 16 para 32 PPU em 2026-07-28 (ver `favela-isometric-standards/SKILL.md` linha
  21 e `favela-pixelart-standards/SKILL.md`, que já exigem 32 de forma explícita). O
  `CLAUDE.md` raiz tinha duas menções residuais de "PPU 16" que sobraram de antes dessa
  promoção — já corrigidas nesta mesma sessão. PPU real e vigente do projeto é **32**.
- **Arma "Cravo de Ferro" (GDD) = `CravoDeAklo` (código):** dano bruto bate (40), mas a
  habilidade mudou de "alto poder de mitigação física" (GDD) para **"Fincar o Aklo"** —
  interrompe a canalização anômala do alvo (código). O nome também mudou de tema (Ferro →
  Aklo, ligando à lore do Necronomicon). GDD desatualizado no texto, código é a versão
  vigente.
- **Consumíveis do GDD (Chá Calmante, Frasco de Incenso, Sino de Estática, Frasco de
  Veneno de Yig, Fragmento de Yuggoth):** nenhum existe como item autorado no código — só a
  fundação genérica de inventário (`Inventario`, `ItemConfig`) existe. São 100% design,
  ainda não implementados.
- **Relíquias e Set Lendário (Anel do Sinal Amarelo, Elmo/Peitoral/Grevas/Arma de Set):**
  idem — não existem como itens específicos no código, só um gerador genérico de relíquias
  no Editor (`GeradorDeReliquias.cs`).

## 6. Nota de Recuperação (2026-08-10)

Este projeto passou por um incidente de perda de dados local (pasta de trabalho apagada por
interação de sincronização do Google Drive). A branch `feat/fase1-deserto-hali` (50 commits
até 2026-08-06, `fb94ae4`) foi recuperada via GitHub e é a base deste checkout. **Os últimos
commits locais feitos após 2026-08-06** — que incluíam a montagem física da cena do Templo
da Serpente (12 zonas, colisão, bosses posicionados) e retoques do sistema de menus/save —
**não foram recuperados** e precisam ser refeitos. Isso explica por que os scripts do Templo
existem mas a cena jogável, não.
