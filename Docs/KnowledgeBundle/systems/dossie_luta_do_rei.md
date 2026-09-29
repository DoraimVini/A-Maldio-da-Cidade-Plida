---
type: Game System
title: Plano de implementação — O Rito do Olhar (a luta contra o Rei em Amarelo)
description: Redesenho da luta final aprovado em direção pelo Vini em 2026-09-28. O Rei não ataca nem morre; ele olha. Ser visto drena a mente, estar exposto no Altar avança o selo, e os Nobres Fossilizados são a única sombra. Cinco fases, cada uma muda a sala. Plano com regras, números, geometria, arquitetura, testes e etapas.
tags: [boss, rei-em-amarelo, castelo, final, plano, redesenho]
---

# Plano de implementação — O Rito do Olhar

> **Estado (2026-09-28, fim do dia): IMPLEMENTADO.** O estado atual, com os números que
> valem, está em [boss_rei_em_amarelo.md](boss_rei_em_amarelo.md). Este plano fica como registro
> do raciocínio. **Onde a implementação divergiu dele:**
>
> | Plano | Implementado | Por quê |
> |---|---|---|
> | Marcos 15/35/60/90; Verbo a cada 6 s, −10 RM; dreno 6→9 | Marcos **20/40/65/90**; Verbo a cada **8 s, −8 RM**; dreno 6/7/8/8; o último Nobre resiste | com os números do plano o jogador simulado **colapsava na Fase 4** (`ORitoCabeNoTempoTests`) |
> | Mover o Rei para ~(0 ; 68) (D1) | O Rei **não se move** | os pés da figura já estão em (0,1 ; 66,7); o "(4 ; 61,7)" era o pivô do quadro. Em 68 os pés sairiam do chão |
> | `Physics2D.Linecast` com camada de cobertura | `LinhaDeVisao` (segmento contra a pegada, geometria pura) | mesma resposta em PlayMode, EditMode e no guarda que lê a cena parada; sem camada nova na TagManager |
> | Três Ecos na Fase 5 | **Um** Eco | os três se manifestariam no mesmo lugar (nas costas do Damião): o triplo do dreno, nenhuma leitura nova |
> | Barra do selo da família `BarraAnimada`; vinheta pelo `TempestadeVisualOverlay` | `TelaDoRito`, montada em código | o overlay da tempestade pinta a tela inteira; a vinheta precisa do centro limpo |
> | O Anel fica em (0 ; 59,8) ou vai para (0 ; 57) | Necronomicon e Patuá **ao lado do Rei** (±5,5 ; 66), Anel em (0 ; 57,3) | saiu da busca de layout contra as regras do §6.2 — nas posições antigas os Nobres cobriam os fragmentos |
> | — | A câmera **sobe 2,2 un** durante o rito | a vista do Castelo tem 8,4 un de altura; com o Damião no Altar os pés do Rei ficavam fora do quadro |
>
> **Plano original, preservado abaixo:** substitui a luta reprovada no playtest de 2026-09-10
> (*"Eu detestei a luta contra o Rei, está muito repetitiva"*). A fotografia completa da luta
> antiga — números, testes, histórico — está no commit `9dc607d4` deste mesmo arquivo; o que
> importa dela está resumido no §12.
>
> **Direção decidida pelo Vini (2026-09-28):**
> - A luta parte do canon: o Rei não ataca, não tem vida, não morre. É **selado**.
> - **Nada de botão novo.** Uma tecla que só existe nesta luta foi recusada: *"isso não foi
>   pensado em momento nenhum e vai criar um botão só para essa luta?"*. A resposta é de level
>   design — **o olhar é do Rei, não do Damião**.
> - A Fase 5 usa os **Ecos de Carcosa** que já existem, e não combate corpo a corpo.

---

## 1. Em uma tela

| | |
|---|---|
| **A regra** | O Rei vê o Damião. **Estar na linha de visão dele drena Resiliência Mental.** Estar exposto **dentro do Altar de Selamento** avança o selo. **Atrás de um Nobre Fossilizado** o Damião está fora da vista e a mente se ancora. |
| **O laço** | Sair da sombra → ficar exposto no Altar → voltar para a sombra antes da mente quebrar. Decisão de **posição**, com os controles que o jogador já tem desde o Deserto. |
| **Escalada** | Cada fase **muda a sala**, não o controle: o olhar vira um farol, fragmentos puxam para campo aberto, a cobertura se desfaz, e no fim ficar parado é que mata. |
| **Vitória** | Selo em 100 %. O Rei se retira. |
| **Derrota** | Resiliência Mental em zero → Colapso (o mecanismo que já existe). |
| **Duração alvo** | 2 min a 3 min 30 s. Nenhuma fase abaixo de 15 s nem acima de 60 s. |
| **Arte nova necessária** | Nenhuma obrigatória. Tudo reaproveita o que está no projeto (§9). |

---

## 2. O que fica em aberto antes de começar

Estas decisões são do Vini. As duas primeiras **bloqueiam a Etapa 2**; a terceira pode esperar.

| # | Decisão | Por que importa | Recomendação |
|---|---|---|---|
| D1 | **Onde fica o Rei.** Hoje ele está em (4,0 ; 61,7), perto do **centro** da sala, com escala 3,7. | A luta é de linha de visão: com o Rei no meio, as sombras apontam para todos os lados e o Altar não tem "frente". Com o Rei **ao fundo**, as sombras caem na direção do jogador e a sala ganha leitura. | Rei ao fundo, em ~(0 ; 68), mantendo a escala 3,7 que o Vini escolheu. |
| D2 | **O Abdul poupado.** Sem o Necronomicon o rito não fecha (achado de 2026-09-28). | Nesta luta cada relíquia é modificador, não chave (§4). | O rito **aceita as relíquias que o jogador tiver**. Sem o Necronomicon, o selo avança mais devagar. A escolha pacífica fica mais difícil, não impossível. |
| D3 | **O espólio do Rei** (3 armas garantidas; o jogo volta ao Menu 5 s depois). | Sem vida, o Rei não é "abatido". | Tirar o `DropAoAbater` do Rei. O `Drop_ReiEmAmarelo.asset` fica no projeto para uso futuro. |

---

## 3. A regra central: a Exposição

### 3.1 Definições

- **Exposto:** uma linha do Rei até o Damião **não encontra cobertura**. Consulta
  `Physics2D.Linecast` da base do Rei até a base do Damião, com máscara só da camada de cobertura
  — nem o colisor do Rei nem o do Damião interferem. Uma vez por `FixedUpdate`, sem alocação.
- **No Altar:** a base do Damião dentro da elipse do Altar de Selamento (semieixos 2 × 1 un, a
  mesma geometria testada do `AbrigoDeReliquia`, que é reaproveitada).
- **Coberto:** não exposto.

A regra não depende de `LookDirection`, que só atualiza enquanto o Damião anda e foi a causa da
morte da mecânica "dê as costas" (§12). Parado ou andando, posição é posição.

### 3.2 O que acontece a cada segundo

| Situação | Selo | Resiliência Mental |
|---|---|---|
| Exposto **e** no Altar | **avança** | **drena** |
| Exposto fora do Altar | parado | **drena** |
| Coberto | parado | **se ancora** (recupera) |

Não existe estado seguro que avance o selo. Para vencer é preciso ser visto.

### 3.3 Números base (sem relíquias)

| Parâmetro | Valor | Observação |
|---|---|---|
| Resiliência Mental | 100 | O valor do jogo; `RMMaxima` não tem efeito passivo (`NomesDeAtributo.SemEfeito`) |
| Avanço do selo no Altar | 1,0 %/s | |
| Ancoragem na sombra | 3 RM/s | **Mecânica nova**: hoje não há recuperação passiva de Resiliência no jogo |
| Dreno exposto | por fase (§5) | 6 → 9 RM/s |

Todos os números moram no prefab do Rei e são calibráveis sem recompilar.

---

## 4. As relíquias como modificadores

As relíquias deixam de ser chave e viram **ferramentas do rito**. O Rei lê o que está
**equipado** nos slots de Artefato (`ArtefatosBridge.Inventario.Contem`, a mesma checagem que os
altares já fazem).

| Relíquia | Efeito | Com ela | Sem ela |
|---|---|---|---|
| **Necronomicon** — as palavras do selo | Selo × 1,3 | 1,3 %/s | 1,0 %/s |
| **Patuá das Luas Gêmeas** — ancora no escuro | Ancoragem × 2 | 6 RM/s | 3 RM/s |
| **Anel do Sinal Amarelo** — filtra o olhar | Dreno × 0,8 | −20 % | integral |

Os efeitos não têm contrapartida. A proposta anterior dava ao Necronomicon +20 % de dreno, mas
com as relíquias sempre desejáveis esse custo só mexe num número que o jogador não escolhe.

**Sem as três relíquias a luta continua possível, só mais dura.** Isso resolve o caso do Abdul
poupado (D2) e dá sentido a ter trazido cada uma. O Rei avisa o que falta (§7).

---

## 5. As cinco fases

O selo vai de **0 a 100**, uma unidade só. Cada fase começa num marco do selo.

### Fase 1 — A Chegada (selo 0 → 15)

**A sala:** o Rei ao fundo, o Altar no centro, os **5 Nobres** espalhados entre os dois.
**Dreno exposto:** 6 RM/s.
**O que ensina:** ser visto custa, e a sombra do Nobre salva. Com as três relíquias são ~11,5 s
exposto no Altar e ~55 RM — dá para fazer sem se esconder, mas no limite. A fala manda procurar
a sombra.
**Transição:** o Rei toca `selar`. A Máscara aparece.

### Fase 2 — A Máscara (selo 15 → 35)

**O que muda:** o olhar vira um **farol**. Um cone de 70° varre a sala de um lado ao outro em
8 s. **Fora do cone o Damião não é visto**, com ou sem Nobre. O Altar fica dentro do cone só
parte do tempo.
**A cada 3 varreduras a Máscara abre** por 2 s: o cone dobra de largura e o dreno triplica. Sinal
sonoro (`EntrouEmPanico`, o som que já existe) meio segundo antes.
**Dreno exposto:** 7 RM/s (21 com a Máscara aberta).
**A pergunta nova:** *onde vou estar quando ele virar para cá?*
**Transição:** o Rei toca `dano`; a Máscara racha.

### Fase 3 — A Peça (selo 35 → 60)

**O que muda:** fragmentos da peça aparecem nos **3 altares de relíquia que já existem** — todos
em campo aberto. Ficar 1,5 s exposto em cima de um fragmento o **lê**: **+8 de selo na hora e
−15 RM de uma vez**. O fragmento some e reaparece em outro altar 10 s depois. O Altar central
continua valendo.
**Dreno exposto:** 8 RM/s.
**A pergunta nova:** *vale correr o risco pelo atalho?*
**Transição:** o Rei toca `desvelo`. A sala escurece por meio segundo.

### Fase 4 — O Verbo (selo 60 → 90)

**O que muda:** o Rei fala. **A cada 6 s um pulso**: quem estiver exposto nesse instante perde
**10 RM** — e **um Nobre se desfaz**, na ordem do mais próximo do Altar para o mais distante.
Com 5 Nobres a cobertura acaba em 30 s.
**Dreno exposto:** 9 RM/s.
**A pergunta nova:** *chego ao fim antes de ficar sem onde me esconder?*
**Transição:** o Rei toca `queda`, se curva, e começa a se apagar.

### Fase 5 — A Queda (selo 90 → 100, sozinho em 30 s)

**O que muda:** o Rei se retira. O selo fecha sozinho, a 1/3 % por segundo. Não há mais olhar
nem Ancoragem. **Três Ecos de Carcosa** se manifestam, com a regra que já têm no Castelo: quem
fica **parado** é alcançado por um Eco nas costas, que drena 3 RM/s até ele voltar a andar. Aqui
o limite de imobilidade cai de 5 para **1,5 s**.
**A inversão:** a luta inteira ensinou que ficar quieto atrás da estátua salva. Agora a sombra é
armadilha. É preciso continuar se mexendo até o selo fechar.
**Vitória:** selo em 100. A tela se desvanece para o amarelo e depois para o branco, e a
`SequenciaDeSelamento` que já existe fecha o jogo.

### 5.1 Números consolidados

| | F1 | F2 | F3 | F4 | F5 |
|---|---|---|---|---|---|
| Selo | 0 → 15 | 15 → 35 | 35 → 60 | 60 → 90 | 90 → 100 (auto) |
| Dreno exposto (RM/s) | 6 | 7 (21 aberta) | 8 | 9 | — |
| Evento | — | farol 70°/8 s; Máscara abre 2 s a cada 3 varreduras | fragmentos +8 selo / −15 RM | pulso a cada 6 s: −10 RM se exposto e −1 Nobre | 3 Ecos, imobilidade 1,5 s |
| Duração alvo | 15–25 s | 30–45 s | 30–45 s | 35–50 s | 30 s |

### 5.2 A conta, com as três relíquias

| Fase | Exposição no Altar | RM gasta (aprox.) |
|---|---|---|
| F1 | 15 / 1,3 = 11,5 s × 4,8 | ~55 |
| F2 | 20 / 1,3 = 15,4 s × 5,6 + Máscara | ~100 |
| F3 | (25 − 2 fragmentos) / 1,3 = 6,9 s × 6,4 + 2 × 15 | ~75 |
| F4 | 30 / 1,3 = 23 s × 7,2 + ~4 pulsos | ~205 |
| **Total** | **~57 s exposto** | **~435 RM** |

Com 100 de Resiliência, o jogador precisa ancorar **~335 RM**: ~56 s na sombra a 6 RM/s. Somados
os deslocamentos, as quatro fases dão **~2 min 10 s**, e a Fase 5 soma 30 s: **~2 min 40 s** para
um jogo limpo. Estes números são o ponto de partida. A calibração fina é ao vivo (Etapa 3), e o
guarda de simulação (§10) segura os limites.

**Contagem de repetição**, o critério que saiu do "detestei": 3 a 6 idas e voltas por fase, e a
regra muda a cada 15–50 s. A luta anterior repetia a mesma pergunta de 5 a 8 vezes do início ao
fim.

---

## 6. A sala — a Z5 ajustada, não reconstruída

O losango de 30 × 15 do Trono fica. O que muda são as peças dentro dele.

| Peça | Onde | De onde vem |
|---|---|---|
| **Rei** | ao fundo, ~(0 ; 68) — decisão D1 | já em cena; só move |
| **Altar de Selamento** | centro, ~(0 ; 62) | **novo objeto**, com a arte dos altares de relíquia (base `Ruin_3` + feixe) |
| **3 altares de relíquia** | onde estão: Necronomicon (−8,9 ; 62,6), Patuá (8,7 ; 63,1), Anel (0 ; 59,8) | já em cena; viram os pontos de fragmento. O do Anel está a 2,2 un do Altar central; **provavelmente move** para ~(0 ; 57) |
| **5 Nobres Fossilizados** | entre o Rei e o Altar, deslocados para os lados | arte já existe; **novas instâncias** na Z5 |
| **3 Ecos de Carcosa** | inativos até a Fase 5 | componente e arte já existem; novas instâncias |
| **Escudos das relíquias** | saem | eram da mecânica antiga |

### 6.1 A escala dos Nobres na Z5

Os Nobres da Z2 têm sprite de 1,25 × 1,75 un e colisor de **1,20 × 1,00**. O colisor do Damião
tem **0,6** de largura. Atrás de um Nobre desse tamanho a sombra mal cabe o Damião: sobram 0,3 un
de cada lado, e isso é mais precisão do que se pede ao jogador. **Na Z5 os Nobres entram em
escala 2** (sprite 2,5 × 3,5 un, colisor 2,4 × 2,0), com folga de ~0,9 un para cada lado. Como
passam da altura do Damião, levam o `OcclusaoDitherFade` (skill `favela-isometric-standards`,
regra 6): quando o Damião está atrás, a estátua abre buracos e a silhueta dele aparece.

### 6.2 Regras de geometria (viram guardas, §10)

1. O **Altar de Selamento é sempre exposto** com todos os Nobres de pé.
2. De qualquer ponto do Altar existe sombra alcançável em **≤ 1,5 s correndo** (≤ 11 un).
3. Toda sombra tem pelo menos **2 × a largura do Damião** a 1 un atrás do Nobre.
4. Os **3 altares de fragmento são expostos**.
5. Na ordem de desfazimento da Fase 4, a sombra mais próxima do Altar some primeiro.
6. O Rei continua com os pés dentro da sala (guarda que já existe).

As posições finais saem de uma ferramenta de cena (§8), não de mão, e os guardas as conferem.

---

## 7. O que o jogador vê, ouve e lê

### 7.1 A exposição precisa ser visível

É o ponto que mais pode dar errado. Num isométrico, "atrás da estátua" é uma linha no chão que o
olho do jogador não traça sozinho. Três sinais, todos baratos:

- **O fio do olhar:** enquanto exposto, uma linha fina amarela do Rei até o Damião
  (`LineRenderer`, sem arte). Some no instante em que ele entra na sombra.
- **Vinheta amarela** nas bordas da tela, crescendo com o tempo exposto. Reaproveitar o
  `TempestadeVisualOverlay`, que já pinta a tela por cima do jogo (a confirmar na Etapa 2).
- **O Altar reage:** o feixe do Altar pulsa quando o selo avança.

### 7.2 Barras

- **Resiliência Mental:** a barra do HUD que já existe. É a barra que decide a luta.
- **Selo:** barra nova de 0 a 100, no topo da tela, da família `BarraAnimada<TFonte>` (como a do
  companheiro). Pisca ao avançar e marca os limiares das fases.
- A barra de vida flutuante do Rei **sai**: ele não tem vida.

### 7.3 Falas (rascunho — texto final é do Vini; vocabulário da skill `favela-lore-enforcer`)

| Quando | Fala provisória |
|---|---|
| Entrar no Trono | *"Ele te vê. Tudo o que te vê te desfaz."* |
| Fase 1, primeira exposição | *"Fica no altar, e o selo aperta. Esconde-te na sombra dos nobres, e a tua mente volta."* |
| Relíquia faltando | *"Sem {nome}, o selo pesa mais."* — uma por relíquia ausente, ao entrar |
| Fase 2 | *"A Máscara se volta. Não estejas onde ela olha."* |
| Fase 3 | *"Páginas da Peça ardem nos altares. Lê-las custa o que resta de ti."* |
| Fase 4 | *"Ele fala. A pedra não resiste à voz."* |
| Fase 5 | *"Ele se vai — e o que fica não descansa. Não pares."* |

### 7.4 As animações do Rei, uma por transição

`idle` durante as fases · `selar` na entrada da F2 · `dano` na F3 · `desvelo` na F4 · `queda` na
F5, seguida de apagar o sprite (alpha a zero em 3 s). A falta de clipe de ataque deixa de
importar: o ataque dele é o olhar.

### 7.5 Som

Com o que já existe: `EntrouEmPanico` antes da Máscara abrir e o Colapso de sempre. **Áudio
novo** (a Canção de Cassilda em fragmentos, a voz do Verbo) é polimento, fora do escopo deste
plano.

---

## 8. Arquitetura

### 8.1 Core (POCO, testável sem Unity)

| Classe | Responsabilidade |
|---|---|
| `RitoDoReiFSM` | As fases: `Aguardando → Chegada → Mascara → Peca → Verbo → Queda → Selado`, ou `Colapso`. Recebe `Tick(dt, LeituraDoRito)` e expõe o selo, a fase e os eventos (`OnFaseMudou`, `OnSelado`, `OnColapso`, `OnPulsoDoVerbo`, `OnMascaraAbriu`). **Substitui a `ReiEmAmareloFSM`.** |
| `LeituraDoRito` (`readonly struct`) | O que o adaptador mede por quadro: `Exposto`, `NoAltar`, `SobreFragmento`, `Imovel`. |
| `ModificadoresDoRito` (`readonly struct`) | Os multiplicadores das relíquias (§4), montados a partir dos ids equipados. |
| `ExposicaoAoRei` | Regra da §3.2: dado leitura, modificadores, fase e `dt`, devolve Δselo e ΔRM. |
| `OlharDoRei` | O farol da Fase 2: ângulo do cone no tempo, Máscara aberta ou não, "este ponto está no cone?". |
| `FragmentosDaPeca` | Qual altar tem fragmento, relógio de leitura (1,5 s), reaparecimento. |
| `VerboDoRei` | O relógio dos pulsos e a ordem de desfazimento dos Nobres. |

### 8.2 Runtime (adaptadores)

| Peça | Responsabilidade |
|---|---|
| `ReiEmAmareloAI` (reescrito) | Monta a FSM com os modificadores das relíquias, faz o `Linecast` por `FixedUpdate`, aplica ΔRM na `ResilienciaBridge` (`SofrerTrauma` / `Ancorar`), toca animações, fala, liga o fio e a vinheta. Perde `IDanificavel`, `Vitalidade`, `Hurtbox` e escudos. |
| `CoberturaDoTrono` | No Nobre: camada de cobertura, `Desfazer()` com fade, dither. |
| `AltarDeSelamento` | O centro: elipse "no altar?" e o pulso do feixe. |
| `FragmentoNoAltar` | No ponto focal existente: mostra e esconde o fragmento, responde "está em cima?". |
| `BarraDoSelo` | Barra da família `BarraAnimada`, ligada ao selo por evento. |
| `EcoDeCarcosa` | Sem mudança de código; as instâncias da Z5 entram com imobilidade de 1,5 s e são ativadas na F5. |

### 8.3 Ferramenta

`Tools/FavelaAmarela/Trono: montar o Rito do Olhar` — idempotente. Move o Rei (D1), cria o Altar
de Selamento, instancia e escala os 5 Nobres com camada e dither, instancia os 3 Ecos, tira os
escudos, grava os parâmetros no prefab. Pela Unity CLI com o Editor aberto; nunca editando YAML
à mão.

### 8.4 O que sai

`EscudoDeReliquia`, `AbrigosDoTrono` (ferramenta), `ConfrontoDoRei` (ferramenta),
`OConfrontoDoReiTests`, `Ficha_Rei` do prefab, `DropAoAbater` do Rei (D3), e a
`ReiEmAmareloFSM` com seus 26 testes — substituída, não adaptada. O `AbrigoDeReliquia` (POCO) fica:
a elipse é reaproveitada pelo Altar. O `DetectorDeCostas` fica como está (já aposentado).
Remoção só na Etapa 5, com a luta nova aprovada.

---

## 9. Reaproveitamento

| Existe | Uso |
|---|---|
| Arte dos Nobres Fossilizados | cobertura da Z5 |
| Arte e animação dos altares | Altar de Selamento e fragmentos |
| `EcoDeCarcosa` + arte | Fase 5, sem código novo |
| `ResilienciaMental`, `ResilienciaBridge`, Colapso | a barra e a derrota |
| `AbrigoDeReliquia` | elipse do Altar |
| `ArtefatosBridge` | quais relíquias estão equipadas |
| `OcclusaoDitherFade` | silhueta atrás do Nobre |
| `BarraAnimada<TFonte>` | barra do selo |
| `TempestadeVisualOverlay` | vinheta (a confirmar) |
| `SequenciaDeSelamento` | o desfecho |
| 5 clipes do Rei | uma por transição |
| `CarcosaDebuggerWindow` | concede relíquias; **ganha** "pular para a fase N" e "mostrar linha de visão" |

---

## 10. Testes

### EditMode (Core)

| Arquivo | Guarda |
|---|---|
| `RitoDoReiFSMTests` | transições nos marcos 15/35/60/90/100; Colapso com RM zero; Fase 5 fecha sozinha em 30 s; nada avança antes de `Iniciar` |
| `ExposicaoAoReiTests` | a tabela da §3.2 célula por célula; cada relíquia sozinha e as três juntas; sem relíquias ainda é vencível |
| `OlharDoReiTests` | o cone varre 70° em 8 s; a Máscara abre a cada 3 varreduras e dobra o cone; ponto fora do cone não é visto |
| `FragmentosDaPecaTests` | 1,5 s para ler, +8/−15, reaparece em outro altar em 10 s, nunca no mesmo seguido |
| `VerboDoReiTests` | pulso a cada 6 s, −10 só se exposto, ordem de desfazimento do mais próximo ao mais distante |
| `ORitoCabeNoTempoTests` | **simulação** de um jogador disciplinado (expõe até 25 RM, ancora até 90): com as 3 relíquias termina entre 2 min e 3 min 30 s, cada fase entre 15 e 60 s; sem o Patuá leva ≥ 25 % mais; sem nenhuma relíquia ainda termina |

### EditMode (cena)

| Arquivo | Guarda |
|---|---|
| `OTronoDoOlharTests` | as 6 regras de geometria da §6.2, medidas na cena; o Rei continua dentro da sala |

### PlayMode (cena real)

| Arquivo | Guarda |
|---|---|
| `ALinhaDeVisaoDoReiTests` | Damião atrás de um Nobre não está exposto; no Altar está; Nobre desfeito → exposto; o colisor do Rei nem o do Damião bloqueiam a linha |
| `ORitoDoOlharDePontaAPontaTests` | com as 3 relíquias concedidas e o Damião teleportado entre Altar e sombra por roteiro, o rito chega a Selado; parado na F5, um Eco se manifesta |

A suíte inteira tem de continuar verde a cada etapa (EditMode e PlayMode, pela Unity CLI com a cena
limpa).

---

## 11. Etapas

Cada etapa termina com suíte verde, devlog e commit. **A Etapa 2 existe para o Vini jogar a
regra central antes de o resto ser construído** — se esconder atrás de estátua não for divertido
na Fase 1, nada acima dela salva a luta.

| Etapa | Entrega | Critério de pronto |
|---|---|---|
| **0 — Decisões** | D1 (Rei ao fundo?), D2 (relíquias como modificador?), D3 (sem espólio?) | respostas do Vini |
| **1 — Core** | `RitoDoReiFSM`, `ExposicaoAoRei`, `ModificadoresDoRito` e testes | EditMode verde, sem tocar em cena |
| **2 — A Fase 1 jogável** | Linecast, Nobres, Altar, fio do olhar, barra do selo, ferramenta de cena; vitória ao fim da F1 (temporário) | **o Vini joga e aprova ou reprova a regra** |
| **3 — Fases 2 a 4** | farol, Máscara, fragmentos, Verbo, desfazimento; calibração ao vivo | `ORitoCabeNoTempoTests` e `OTronoDoOlharTests` verdes; o Vini joga |
| **4 — Fase 5** | Ecos na Z5, desfecho, `SequenciaDeSelamento` | `ORitoDoOlharDePontaAPontaTests` verde |
| **5 — Limpeza e entrega** | remover o que sai (§8.4), falas finais, roadmap e OKF atualizados, build | suíte verde, build de ENTREGA, o Vini joga de ponta a ponta |

**Tamanho honesto:** Etapa 1, uma sessão. Etapa 2, uma. Etapa 3, uma a duas, porque é onde a
calibração acontece. Etapas 4 e 5, uma juntas.

---

## 12. Riscos

| Risco | Mitigação |
|---|---|
| **Linha de visão ilegível no isométrico** — o jogador não entende por que está exposto | o fio do olhar é obrigatório desde a Etapa 2; o Vini julga ali |
| **Rei de 15 un atrás de um Nobre de 3,5** — "como isso me esconde?" | a linha sai dos **pés** do Rei; se ler errado na Etapa 2, mover a origem para a altura da Máscara e projetar no chão |
| **Laço "sai-e-volta" virar metrônomo de novo** | o critério da §5.2 é conferido na Etapa 3: se alguma fase pedir mais de 6 idas e voltas iguais, ela encurta |
| **Eco posicionado pela `LookDirection`** (`EcoDeCarcosa.cs:149`) — aparece "nas costas" de quem está parado olhando para o último lugar onde andou | cosmético; a regra do Eco é imobilidade, não olhar. Conferir na Etapa 4 |
| **Recuperação passiva de Resiliência é mecânica nova** no jogo | existe só no Trono, só atrás de cobertura. Não vaza para outras cenas |
| **Canal de volta ao Refúgio** — morrer na F4 repete tudo | medir na Etapa 3. Se a luta passar de 3 min, avaliar um marco no selo |

---

## 13. O que ficou da luta anterior

Da versão reprovada (histórico completo no commit `9dc607d4`):

- **Três relíquias, de três lugares:** Necronomicon (Abdul derrotado, Tumba), Patuá (quest da
  Cassilda, Santuário), Anel do Sinal Amarelo (drop garantido do Byakhee). A Coroa de Ossos segue
  sem fonte (Templo fora do VS).
- **Por que "dar as costas" morreu:** `LookDirection` só atualiza andando; quem parava para ler o
  aviso morria. Por isso nada aqui depende dela.
- **Por que os escudos morreram:** era um laço só — correr para o clarão, esperar, sobreviver —
  repetido de 5 a 8 vezes em ~50 s, e a regra do Confronto ("agora ele sangra") não chegava ao
  jogador.
- **Sucart:** 5 clipes (idle, selar, desvelo, dano, queda), sem ataque; crédito obrigatório já na
  tela de Créditos.

---

## Relacionados

- [boss_rei_em_amarelo.md](boss_rei_em_amarelo.md) — a luta antiga (será reescrito na Etapa 5)
- [level_design_castelo_carcosa.md](level_design_castelo_carcosa.md) — o design original da Z5 e a Pressão Psíquica
- [artefatos.md](artefatos.md) — slots de Artefato
- [companheiro_mi_go.md](companheiro_mi_go.md) — a escolha do Abdul
- [resiliencia_mental.md](resiliencia_mental.md) — a barra que decide a luta
- [../roadmap_vertical_slice.md](../roadmap_vertical_slice.md) — item 12
