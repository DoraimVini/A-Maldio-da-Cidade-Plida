---
type: Game System
title: Arte e Animação — estado medido
description: Auditoria do estado de animação do projeto, o defeito de alfa na folha do Abdul, e avaliação dos pacotes de sprite disponíveis.
tags: [arte, animacao, sprites, licenca, edital]
timestamp: 2026-08-19T00:00:00Z
---

# Arte e Animação — estado medido

Tudo abaixo foi **medido nos arquivos** em 2026-08-19, não estimado.

---

## 1. Nada no jogo anima

Existem **7 clipes `.anim` no projeto inteiro**. Todos são do Abdul, e **todos estão
desligados**. Nenhum dos três chefes tem componente `Animator`. Todo personagem desenha um
quadro parado — com a folha animada fatiada ao lado, sem consumidor.

| personagem | sprite usado | fatias disponíveis na folha |
|---|---|---|
| Damião | `Damiao_Robe_Idle` (1 quadro) | **208** em `damiao_spritesheet_cultist` |
| Abdul Alhazred | 1 quadro | 64 + 7 clipes + controller |
| Byakhee | 1 quadro | 26, nomeadas por animação |
| Rei em Amarelo | 1 quadro | 138 (folha "Necromancer" emprestada) |
| Cultista | `Cultista_Idle` (1 quadro) | 16 |
| Espectro de Hali | `EspectroHali_Idle` (1 quadro) | 16 |

O `Abdul_AC.controller` tem os 7 estados mas **0 transições, 0 parâmetros e nenhum estado
default**, e o `AbdulAlhazredAI` **não chama o Animator uma única vez**.

---

## 2. O bloqueio: a folha do Abdul não tem transparência

`Assets/Sprites/Bosses/Alhazred/abdul_alhazred_spritesheet.png` é **totalmente opaca** —
alfa 255 nos 16384 pixels de cada célula. O xadrez de transparência está **achatado dentro
do PNG** como dois cinzas (~242 e ~212) com ruído. Em jogo ele não renderiza recortado:
renderiza como um quadrado de 4×4 unidades com fundo claro.

**Causa:** a arte foi gerada por IA (Gemini) e exportada já achatada.

**Conserto certo:** reexportar do `abdul_alhazred_spritesheet.ase` (o fonte Aseprite está
no repositório, 1,5 MB) com transparência de verdade. Custa segundos e não tem perda.

**Por que não resolvi por código:** cheguei a remover o fundo por limiar de neutralidade +
luminância. A figura sai perfeita, mas **a aura roxa sai em blocos** — o brilho suave havia
sido composto sobre o cinza claro, e qualquer corte por limiar o serrilha em degraus
retangulares. Não é um problema que se resolva bem sem o fonte.

> ⚠️ **Consequência de planejamento:** se a folha do Abdul for substituída (por reexport ou
> por arte nova), **os 7 clipes morrem junto** — cada keyframe aponta para uma fatia
> específica dela por `fileID`. Ligar o `Animator` dele **antes** disso é trabalho jogado
> fora. Foi por isso que a ligação foi interrompida em 2026-08-19.

### Só o Abdul está quebrado

Varri toda a arte de personagem do projeto medindo o percentual de pixels opacos:

| arquivo | veredito |
|---|---|
| `abdul_alhazred_spritesheet.png` | ❌ **100% opaco — fundo assado** |
| `Byakhee_Spritesheet.png` | ✅ 41,5% — transparência correta |
| `Cultista_Sliced_Sheet.png` | ✅ 86,3% |
| `EspectroHali_*`, `Damiao_Robe_Idle`, `yug_neth_idle`, `CoisaDoCemiterio`, itens | ✅ |
| `Damiao_Concept_*.png` | opacos, mas são **arte-conceito**, não sprite |
| `bar_background.png`, `bar_fill.png` | opacos **por design** (barras de UI) |

---

## 3. O Byakhee é o único chefe animável hoje

A folha dele tem transparência correta, 26 quadros e nomes já em terminologia diegética:

`espreita` (4) · `rasante` (6) · `garras` (4) · `grito` (6) · `dano` (2) · `derrota` (4)

E os estados da `ByakheeFSM` mapeiam quase 1-para-1 (`Espreita`, `Rasante`,
`MergulhoDeGarras`, `GritoDirecionado`, `Pousado`, `Circundando`, `Frenesi`, `Derrotado`),
com `HandleEstadoMudou(anterior, atual)` já existindo como **ponto único** para dirigir o
Animator. É o menor caminho entre "nada anima" e "um chefe anima".

---

## 4. Pacotes avaliados (2026-08-19)

### Invalid.User Horror Battlers — ⚠️ não serve como sprite de mundo

6 PNG. Licença: uso livre, crédito opcional, proibido redistribuir como asset próprio.

**Não é pixel art:** pintura digital de 400×430, pose frontal estática, estilo *battler* de
RPG Maker, paleta rosa/lima pastel. Ao lado de personagens de 32×48 brigaria feio.
**Onde serviria:** ilustração — revelação de chefe, códex, cinemática de abertura.

### (DEMO) Lords Of Pain — Trevor Pupkin — ✅ perspectiva certa

Licença: *"can be used in both free and commercial projects... you can modify it"*, proibido
revender/redistribuir. Compatível com o edital.

**Isométrico de verdade**, 16 direções (22,5° cada), animado. Quadros de 256×256 com o
personagem ocupando ~50×38 px — o resto é preenchimento e sombra assada.

| conteúdo do DEMO | serve para |
|---|---|
| `enemy/skeleton` — walk (8 quadros × 16 dir) + death | **`EsqueletoInvocado`** — casamento direto |
| `playable character/warrior` — idle + walk | — |
| props, UI, vfx/glint | avulsos |

**Ressalvas:** é DEMO; o visual é pré-renderizado macio, mais escuro e menos contrastado que
o pixel art do projeto; e as 16 direções exigem suporte a sprite direcional que **o jogo não
tem** (usar 1 direção, ou 4/8, é o caminho barato).

**A versão paga** traria, pelo índice: 1 chefe (*Demonlord*), o Skeleton, e um NPC
*Subservient* com animações `working` e **`worship`** — que é literalmente o Cultista
Amarelo —, além de braseiros, colunas, gemas e ossos.

### Nenhum dos dois tem sprite de chefe para Abdul ou Rei em Amarelo

O DEMO do Lords of Pain só traz *skeleton* e *warrior*. Os Horror Battlers não são sprites
de mundo. **A busca por arte de chefe continua aberta.**

---

## 5. Ordem recomendada

1. **Reexportar o `.ase` do Abdul** com alfa — desbloqueia os 7 clipes que já existem.
2. **Ligar o Animator do Byakhee** — não depende de nada, e fecha metade do item 9 do
   roadmap.
3. Decidir sobre o Lords of Pain para o `EsqueletoInvocado` (e avaliar a versão paga pelo
   *Subservient*, que resolveria o Cultista).
4. Arte de chefe para Abdul e Rei em Amarelo segue sem candidato.
