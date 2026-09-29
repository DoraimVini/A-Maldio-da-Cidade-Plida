---
type: Game System
title: Boss Rei em Amarelo — O Rito do Olhar, no Trono de Aldebaran
description: O confronto final, desde 2026-09-28. O Rei não ataca nem morre; ele olha. Ser visto drena a Resiliência Mental, ser visto dentro do Altar de Selamento avança o selo, e atrás dos Nobres Fossilizados a mente se ancora. Cinco fases, cada uma muda a sala.
tags: [boss, rei-em-amarelo, combate, castelo, final, rito-do-olhar]
---

# Boss Rei em Amarelo — O Rito do Olhar

> **Status (2026-09-28):** implementado de ponta a ponta — Core, Runtime, sala montada na Z5,
> guardas de geometria e testes PlayMode verdes. **Calibrado por simulação, ainda não jogado:**
> a calibração fina é ao vivo, com o Vini. Plano de origem, com o raciocínio de cada decisão:
> [dossie_luta_do_rei.md](dossie_luta_do_rei.md).

Substitui a luta dos escudos + Confronto, reprovada no playtest de 2026-09-10 (*"Eu detestei a
luta contra o Rei, está muito repetitiva"*). O retrato completo da luta antiga está no commit
`9dc607d4` de `dossie_luta_do_rei.md`; o resumo está em [Histórico](#histórico).

## A regra

| Situação do Damião | Selo | Resiliência Mental |
|---|---|---|
| Visto **e** dentro do Altar de Selamento | **avança** | **drena** |
| Visto fora do Altar | parado | **drena** |
| Na sombra de um Nobre (não visto) | parado | **se ancora** |

Não existe estado seguro que avance o selo: para vencer é preciso ser visto. O laço é de
**posição** — sair da sombra, pagar no Altar, voltar antes de a mente quebrar — com os
controles que o jogador já tem. **Nenhum botão novo** (decisão do Vini: *"vai criar um botão só
para essa luta?"*).

- **Vitória:** selo em 100 → `OnVitoria` → `SequenciaDeSelamento` (o jogo volta ao Menu).
- **Derrota:** Resiliência em zero → o Colapso de sempre; o rito só se encerra.
- **"Visto"** = o segmento do **olho do Rei** até os **pés do Damião** não atravessa a pegada de
  nenhum Nobre de pé (`LinhaDeVisao.Cruza`, Liang–Barsky, sem alocação). Não depende de
  `LookDirection` — foi ela que matou a mecânica "dê as costas".
- **O olho** é o centro do colisor do corpo do Rei, **(0,1 ; 66,7)** — os pés da figura
  desenhada. Não o `transform.position`: o pivô do quadro de 88 × 129 px fica 3,9 un à direita
  e 4,3 un abaixo da figura, com a escala 3,7 que o Vini escolheu.

## As cinco fases (números do Core, calibrados por simulação)

| | Chegada | Máscara | Peça | Verbo | Queda |
|---|---|---|---|---|---|
| Selo | 0 → 20 | 20 → 40 | 40 → 65 | 65 → 90 | 90 → 100 sozinho, em 30 s |
| Dreno visto (RM/s) | 6 | 7 (× 3 com a Máscara aberta) | 8 | 8 | — |
| O que muda | só Rei, Altar e Nobres | o olhar vira **farol**: cone de 70° varrendo ± 60° em 8 s; fora dele não se é visto. A cada 3 varreduras a **Máscara abre** 2 s (cone dobra, dreno × 3), com som 0,5 s antes | um **fragmento da Peça** arde num altar de relíquia; 1,5 s visto em cima dele o lê: **+8 selo, −15 RM** de uma vez; reaparece no altar seguinte 10 s depois | o **Verbo** pulsa a cada 8 s: quem está visto perde 8 RM, e **um Nobre se desfaz** — do mais perto do Altar ao mais longe; o último resiste | o Rei se curva e se apaga; não há olhar nem Ancoragem; o **Eco da Queda** pune quem fica parado 1,5 s (3 RM/s nas costas) |
| Animação | `idle` | `selar` | `dano` | `desvelo` | `queda` + alpha a zero em 3 s |

Base: selo **1,0/s** no Altar, Ancoragem **3 RM/s** na sombra.

**Relíquias são modificadores, não chaves** — lidas do que está **equipado** ao entrar no Trono:
Necronomicon × 1,3 no selo, Patuá × 2 na Ancoragem, Anel × 0,8 no dreno. Sem elas o rito
continua vencível, só mais longo; o Rei diz na chegada quais faltam. Isso fecha o caso do Abdul
(que deixou de poder ser poupado — [companheiro_mi_go.md](companheiro_mi_go.md)).

**A simulação** (`ORitoCabeNoTempoTests`, jogador disciplinado contra a FSM real): ~3 min com
as três relíquias, ~4 min 20 s sem o Patuá, ~7 min 20 s sem nenhuma — e ainda sela. Os números
do plano original (marcos 15/35/60/90, Verbo a cada 6 s custando 10) **colapsavam** o jogador na
Fase 4; a calibração está no `ParametrosDoRito`.

Todos os números moram também no prefab do Rei (`ConfiguracaoDoRito`, calibráveis sem
recompilar). A simulação usa os padrões do Core — calibração que ficar deve voltar para lá.

## A sala (Z5 do `Castelo_Carcosa`)

Losango de 30 × 16 un centrado em (0 ; 62). Montada por `Tools/FavelaAmarela/Trono: montar o
Rito do Olhar` (idempotente). O layout saiu de uma **busca** contra as seis regras do plano, não
de mão:

| Peça | Onde |
|---|---|
| Rei | não se move — a figura já está de pé no fundo (decisão D1 cumprida sem mexer) |
| Altar de Selamento | (0 ; 62), elipse 4 × 2 com círculo no chão; a pedra fica na borda do fundo, para o Damião ficar na frente dela |
| 5 Nobres Fossilizados (escala 2, pegada 2,4 × 2,0) | (−4,5 ; 62), (4,5 ; 62), (−8,5 ; 63), (8,5 ; 63), (−5 ; 58,5) |
| Altares de fragmento | Necronomicon (−5,5 ; 66) e Patuá (5,5 ; 66), ao lado do Rei; Anel (0 ; 57,3), perto da entrada |
| Eco da Queda | inativo até a Fase 5 |

29 % do chão fica na sombra com todos de pé; a sombra mais perto está a ~4 un do centro do
Altar, a mais longe (a que resiste) a ~8,5.

## O que o jogador vê

- **O fio do olhar:** linha amarela dos pés do Rei até o Damião, só enquanto ele é visto.
- **O farol** da Máscara: triângulo translúcido no chão (um `LineRenderer` com a largura indo de
  zero ao máximo), mais forte com a Máscara aberta.
- **O Altar:** o feixe da pedra acende e o círculo clareia enquanto o selo avança.
- **A barra do selo** no topo, com os marcos das fases, e a **vinheta amarela** que cresce com o
  tempo visto (`TelaDoRito`, montada em código — nenhum asset novo).
- **A câmera sobe 2,2 un** durante o rito (`IsometricCameraController.DeslocamentoDeEnquadramento`):
  com o Damião no Altar, a vista do Castelo (8,4 un de altura) deixava os pés do Rei fora do
  quadro — o olhar que decide a luta vinha de fora da tela.
- **Silhueta atrás dos Nobres:** `OcclusaoDitherFade` + `OcclusionDither.mat` (skill de
  isometria, regra 6). Esconder-se é ficar do lado da entrada, que o Y-sort já desenha por cima;
  o dither serve a quem passa por trás da estátua.
- **Falas provisórias** (texto final é do Vini) em `ReiEmAmareloAI.Falas`, curtas na tela porque
  a caixa de fala do HUD cobre a metade de baixo da vista.

## Arquitetura

| Camada | Peça | Papel |
|---|---|---|
| Core | `RitoDoReiFSM` | fases, selo, eventos (`OnFaseMudou`, `OnSelado`, `OnColapso`); `PularPara` para depuração |
| Core | `ExposicaoAoRei`, `ModificadoresDoRito`, `ParametrosDoRito`, `RitoDoRei` (ids) | a tabela da regra e os números |
| Core | `OlharDoRei`, `FragmentosDaPeca`, `VerboDoRei` | as mecânicas das fases 2, 3 e 4 |
| Core | `LinhaDeVisao`, `CaixaDeCobertura` | o segmento contra a pegada |
| Runtime | `ReiEmAmareloAI` | mede o mundo por quadro, entrega ao Core, aplica na mente, traduz eventos em cena |
| Runtime | `CoberturaDoTrono`, `AltarDeSelamento`, `PontoFocalDeReliquia` | Nobre, Altar, altar de fragmento |
| Runtime | `SinaisDoOlhar`, `TelaDoRito`, `ConfiguracaoDoRito` | fio + farol, barra + vinheta, números no Inspector |
| Runtime | `ResilienciaBridge.SofrerDrenoContinuo` | dreno por quadro **sem mitigação** — a `MitigacaoDeDano` por fatia esmagaria o olhar conforme o equipamento |
| Editor | `MontarORitoDoOlhar` | monta a sala e limpa o prefab do Rei |
| Editor | `CarcosaDebuggerWindow` | iniciar o rito, **pular para a fase N**, estado ao vivo |

O Rei **perdeu** `IDanificavel`, `Vitalidade`, hurtbox, barra de vida, `IFonteDeEspolio`,
`DropAoAbater` e `ExposicaoAoAbater` (decisão D3: selá-lo termina o jogo; o espólio caía na
tela do desfecho). As três armas T3 ficaram **sem fonte, declaradas** em
`ArmaAlcancavelTests.SemFonteAinda`; o `Drop_ReiEmAmarelo` fica guardado.

## Testes

- `RitoDoReiTests` (21) e `ORitoCabeNoTempoTests` (4) — o Core e a simulação.
- `LinhaDeVisaoTests` (7) — o segmento contra a caixa, quinas e paralelas.
- `OTronoDoOlharTests` (7, cena) — as seis regras de geometria do plano, com a mesma conta do
  jogo: Altar visto, sombra a ≤ 11 un, sombra que cabe o Damião, fragmentos vistos, ordem do
  Verbo, entrada livre — e a ligação do Rei.
- `OTronoCabeNaSalaTests` — pés do Rei no chão (pelo olho, não pelo pivô), corpo na sala, escala.
- `ORitoDoOlharTests` (PlayMode, cena real) — linha de visão segue os Nobres, mente paga e se
  ancora, Nobre desfeito expõe; e as cinco fases até o selamento (farol, fragmento, Verbo, Eco,
  `OnVitoria`).

## Pendente

- **O Vini jogar.** A regra central (Fase 1) é a que decide se o resto vale; os números são de
  simulação.
- Falas finais e som próprio (a Canção de Cassilda, a voz do Verbo) — hoje o rito usa
  `EntrouEmPanico` e `ItemRecolhido`.
- O Eco aparece "nas costas" pela `LookDirection` — cosmético; a regra dele é imobilidade.
- A caixa de fala do HUD cobre metade da tela; as falas do rito ficam 3,5–4,5 s por isso.

## Histórico

- **2026-08-11 → 2026-09-10:** ritual de relíquias (botão E nos pontos focais) + selamento em
  ciclos de reação: estar de costas (`DetectorDeCostas`), depois dentro de um escudo
  (`EscudoDeReliquia`, elipse `AbrigoDeReliquia` — hoje reaproveitada pelo Altar). Em 10/09
  ganhou o Confronto (o Rei sangrava entre desvelos). Reprovada por ser um laço só, repetido de
  5 a 8 vezes. Código, ferramentas (`AbrigosDoTrono`, `ConfrontoDoRei`) e testes
  (`ReiEmAmareloFSMTests`, `OConfrontoDoReiTests`, `OAltarResponde`) removidos em 2026-09-28.
- **Arte:** Sucart (*Moonstone Keeper*), 5 clipes sem ataque — crédito obrigatório, já na tela de
  Créditos (`AnimacaoDoReiEmAmareloTests` guarda a licença).
- **A Coroa de Ossos** não pesa no rito; continua sem fonte jogável (Templo fora do VS).

## Relacionados
- [O Rito do Olhar — plano](dossie_luta_do_rei.md) — o raciocínio, as decisões D1–D3, os riscos
- [Level design do Castelo](level_design_castelo_carcosa.md) — a Z5 original
- [Artefatos](artefatos.md) — "só vale o que está equipado"
- [Resiliência Mental](resiliencia_mental.md) — a barra que decide a luta
- [Renderização isométrica](renderizacao_isometrica.md) — o dither de oclusão
- [Companheiro Mi-Go](companheiro_mi_go.md) — o Abdul obrigatório
