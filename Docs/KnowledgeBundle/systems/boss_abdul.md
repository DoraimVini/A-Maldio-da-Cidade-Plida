---
type: Game System
title: Luta contra Abdul Alhazred (Aparição Primordial)
description: FSM de fases, Escudo Mágico, Pedras de Poder, Cones de Gelo e a janela de exaustão.
tags: [boss, combat, fsm, abdul]
---

# Luta contra Abdul Alhazred

Clímax da Tumba de Alhazred (Dungeon 1 do Deserto de Hali). Abdul é uma **Aparição
Primordial** — logo, **imune a golpe crítico de furtividade**: a furtividade serve para
chegar até a luta, não para resolvê-la. Lore e design narrativo em
[abdul_alhazred.md](../lore/abdul_alhazred.md).

## O escudo é o coração da luta

**O dano só entra quando o Escudo Mágico está baixo.** É o que impede a luta de ser "bata
até cair". A FSM (`AbdulFSM`) é a fonte única de verdade: o adaptador consulta
`PodeReceberDano` antes de aplicar qualquer golpe.

| Estado | O que acontece | Vulnerável? |
|---|---|---|
| **Transe** | Pré-luta: flutua murmurando Aklo. É **interagível** — conversar com ele leva à escolha lutar/concordar. | Não |
| **Fase 1** (100%→35%) | Escudo sustentado pelas **Pedras de Poder**; invoca esqueletos em cadência. | Só com Pedra quebrada |
| **Fase 2** (<35%) | Escudo **permanente**; conjura Cones de Gelo e esqueletos gastando mana. | Não |
| **Exausto** | Mana esgotada após o ciclo de magias: escudo cai. | **Sim** — janela do golpe final |
| **Derrotado** | Abatido; dropa o **Necronomicon**. | — |

### Transições

```
Transe   ──(conversa → escolhe "Lutar")───────▶ Fase1
Transe   ──(poupado, depois atacado)──────────▶ Fase1  (traição da trégua)
Fase1    ──(quebrar Pedra de Poder)───────────▶ escudo cai por N s, depois volta
Fase1    ──(vida <= 35%)──────────────────────▶ Fase2  (escudo sobe, permanente)
Fase2    ──(conjurou 3 magias)────────────────▶ Exausto (escudo cai)
Exausto  ──(recupera mana)────────────────────▶ Fase2  (escudo volta, ciclo reinicia)
Qualquer ──(vida <= 0)────────────────────────▶ Derrotado  → drop do Necronomicon
```

### Detalhes que definem a luta

- **Pedras de Poder só valem na Fase 1.** Na Fase 2 quebrá-las não faz nada — é o que
  força o jogador a mudar de plano na virada de fase (de "procurar pedras" para "sobreviver
  ao ciclo e punir a exaustão").
- **A FSM não guarda a vida.** Quem tem a [Vitalidade](vitalidade.md) é o adaptador
  (`AbdulAlhazredAI`), que informa a fração restante por `AtualizarFracaoDeVida` após cada
  golpe. Mesma divisão de `CultistaFSM`/`CultistaAI`.
- **Vencível com qualquer uma das 3 armas.** O baú é RNG (ver
  [ficha_de_atributos.md](ficha_de_atributos.md) e `SorteioDeArmaDaTumba`), então nenhuma
  arma pode ser obrigatória. A `Defesa` dele é baixa (5) de propósito: a fórmula de
  mitigação é subtrativa, então defesa alta puniria desproporcionalmente as armas de dano
  baixo (o Estilete).

## Sangramento atravessa o Escudo Mágico

A **Ferida de Aklo** do Estilete de Irem continua sangrando (e acumulando rumo ao estouro)
**mesmo depois de o Escudo Mágico voltar** — a ferida foi aberta na janela de
vulnerabilidade e não se importa com o escudo.

É o que mantém a arma de menor dano do baú viável aqui: enquanto o Alfanje precisa da
janela aberta, o Estilete cobra durante a espera. Sem isso, a premissa "vencível com
qualquer uma das 3 armas" (o baú é RNG) não se sustentaria.

> A mecânica completa — acúmulos, teto, dano do estouro — é **da arma**, não desta luta:
> ver [As Três Armas da Tumba](armas_da_tumba.md).

## Cones de Gelo e congelamento

Cada Cone de Gelo que acerta Damião aplica um **acúmulo de frio**; ao chegar a **3**, ele
**congela** (atordoado brevemente) e o acúmulo zera. Acúmulos **expiram** com o tempo — a
mecânica é "não leve três seguidos", não uma punição inevitável. Regra em
`AcumuloDeCongelamento` (POCO, 10 testes EditMode).

## Atributos (`Ficha_Abdul.asset`)

| Atributo | Valor | Porquê |
|---|---|---|
| VitalidadeMax | 300 | ~6 golpes de Alfanje, ~9 de Maça — distribuídos entre janelas |
| Ataque | 8 | dano físico baixo (o perigo real é mágico) |
| Conjuração | 25 | dano dos Cones de Gelo |
| Defesa | 5 | baixa de propósito (ver acima) |
| ResistênciaAnômala | 20 | ele resiste a dano anômalo |

## Pedras de Poder: nascem na Fase 1 (não ficam na dungeon)

Decisão do Vini (2026-07-30): as Pedras **não ficam pré-plantadas na cripta**. São âncoras
do ritual de Abdul, então se manifestam quando ele desperta e somem quando deixam de
importar:

| Momento | O que acontece com as Pedras |
|---|---|
| Abdul em Transe (antes da conversa) | Não existem — a arena está limpa |
| Entra na **Fase 1** | Nascem (losango ao redor dele, ou nos `pontosDasPedras` manuais) |
| Vira **Fase 2** | As restantes são destruídas — o escudo vira permanente e não depende mais delas |
| **Derrotado** | As restantes são destruídas |

Implementação: `AbdulAlhazredAI` assina `AbdulFSM.OnStateChanged` e instancia
`prefabPedraDePoder`, chamando `PedraDePoder.Bind(this)` em cada uma (injeção, não
referência de Inspector). Ferramenta de Editor: `Tools/FavelaAmarela/Montar Pedras de Poder`.

## A arena fecha durante a luta

Entrar na Fase 1 tranca as saídas da arena; resolver a luta (derrota) as reabre. Vale também
para a traição da trégua, que passa pelo mesmo `IniciarLuta()`. A conversa em Transe não
tranca nada — poupar Abdul sem lutar nunca fecha a arena.

Isso não é específico dele: é o padrão `TrancaDeArena`, pensado para Byakhee e Rei em Amarelo
reaproveitarem sem código novo. Ver [tranca_de_arena.md](../architecture/tranca_de_arena.md).

## Estado persistido

O desfecho (derrotado × poupado) sobrevive à troca de cena — voltar à Tumba não ressuscita
Abdul nem reabre a conversa. Detalhes, incluindo o renascimento do Necronomicon não-coletado,
em [architecture/persistencia.md](../architecture/persistencia.md).

## Traição da trégua

Atacar Abdul depois de escolher "Concordar" **reabre a luta de verdade** — o jogador ainda
pode derrotá-lo e pegar o Necronomicon. O golpe que trai não causa dano: só desperta a luta
(o Escudo Mágico sobe com `IniciarLuta()` e só cai ao quebrar uma Pedra, igual ao caminho
normal). Ver `AbdulAlhazredAI.ReceberGolpe`.

## Estado de implementação

**Pronto e testado:** `AbdulFSM` (13 testes), `AcumuloDeCongelamento` (10 testes),
`AbdulAlhazredAI` (adaptador), `PedraDePoder` (cenário destrutível que derruba o escudo),
`Ficha_Abdul.asset`.

**Concluído em 2026-07-31 — a luta fecha de ponta a ponta:**
- **Sangramento aplicado** (`Sangramento` + `ExplosaoDeSangramento`, POCOs). Acúmulo até 10 → estouro percentual; **atravessa o Escudo Mágico**. Mecânica completa em [As Três Armas da Tumba](armas_da_tumba.md).
- **Congelamento ligado ao Damião**: novo estado `PlayerState.Congelado` +
  `PlayerStateMachine.ForcarEstado` (caminho dos efeitos **impostos**, distinto de
  `TryEntrarAcao`, que é para ações escolhidas) + `CongelamentoBridge` no jogador +
  projétil `ConeDeGelo` que aplica acúmulo e drena Resiliência Mental.
- **Prefabs criados** (arte placeholder): `PedraDePoder`, `EsqueletoInvocado`, `ConeDeGelo`,
  `Necronomicon`, e o visual do Escudo Mágico como filho do Abdul. Ferramenta:
  `Tools/FavelaAmarela/Montar Prefabs da Luta do Abdul`.
- **Necronomicon é pickup de verdade** (`NecronomiconPickup`, `IInteragivel`): cai no chão
  ao derrotar Abdul e o jogador recolhe com **E**. Efeito (traduzir Aklo) ainda pendente —
  não há sistema de tradução nem inventário.
- **Esqueletos invocados** (`EsqueletoInvocado`): perseguem o Damião (reaproveitam
  `SeguidorDeAlvo` com distância de conforto zero), são frágeis e **expiram sozinhos** —
  sem tempo de vida, uma luta longa viraria uma multidão impossível.

**Pendente:**
- ~~**Arte real** de todos os prefabs acima (hoje são retângulos coloridos) e as
  AnimationClips do Abdul (os 28 frames estão fatiados e nomeados por animação, mas não há
  Animator — ele usa o frame `transe` estático).~~

  ⚠️ **Vencido. Re-medido em 2026-09-03**, resolvendo cada `m_Sprite` por GUID: os quatro
  prefabs têm arte autorada — Cone de Gelo 30×12 px, Pedra de Poder 27×44, Esqueleto 20×46,
  Necronomicon 15×15. **Nenhum usa o sprite embutido da Unity.** E o Abdul tem `Animator`
  com `Abdul_AC_Mage` (5 estados, 5 clipes, estado default) desde 2026-08-19.

  **O que era verdade e ninguém tinha escrito:** nenhuma das quatro peças era *animada*.
  A **Pedra de Poder** ganhou aura girando em 2026-09-03 (12 quadros, anel roxo do pacote
  *Shader Cylinder*, matiz 267° contra os 264° do próprio cristal). Isso importa para a
  legibilidade da Fase 1: a Pedra sustenta o Escudo Mágico e quebrá-la é a única forma de
  causar dano ali — sem sinal na tela, "procurar e quebrar" era palpite. Guarda:
  `AuraDaPedraDePoderTests`.

  **A arte do Abdul foi trocada** na mesma data (pacote *sorcerer villain*): o desenho
  anterior ocupava 16×31 px — 38% da altura do Damião. Agora dá 50×66 parado. A energia da
  conjuração vinha em âmbar e foi rodada para **198°**, que é no grau o matiz do
  `ConeDeGelo.png` que este documento descreve — o conjurador brilhava laranja e disparava
  gelo. Ver `Art/Enemies/Abdul/PROCEDENCIA_Abdul.txt`.

  **Segue pendente:** arte animada de Esqueleto, Cone de Gelo e Necronomicon.
- **Efeito do Necronomicon** ao ser coletado.
- **Necronomicon como pickup:** confirmado que é um item a coletar depois da derrota (padrão `IInteragivel`, como o baú), não um efeito automático. `HandleDerrotado` instancia `prefabNecronomicon`, que ainda não existe.

**Resolvido desde então:** Abdul virou prefab (`Abdul_Alhazred.prefab`) e está posicionado na arena; as Pedras nascem por fase (ver acima).
