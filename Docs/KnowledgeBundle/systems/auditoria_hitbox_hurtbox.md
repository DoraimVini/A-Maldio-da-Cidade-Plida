---
type: Game System
title: Auditoria de Hitbox e Hurtbox
description: Varredura completa de toda consulta de física, mensagem de colisão, i-frame e knockback em Assets/Scripts, com forma, tamanho, offset, momento, máscara e continuidade de cada uma.
date: 2026-09-03
---

# Auditoria de Hitbox e Hurtbox

Varredura de **todo** `Assets/Scripts` em 2026-09-03, atrás de `Physics2D.Overlap*`,
`Physics2D.Raycast`, `OnTrigger*2D`, `OnCollision*2D`, uso de `LayerMask` em contexto de
combate, i-frames e knockback.

## Definições em uso neste projeto

| termo | o que é | onde mora |
|---|---|---|
| **Hitbox** | a área que **causa** dano | `Runtime.Combat.Hitbox` — uma *consulta*, não um colisor |
| **Hurtbox** | a área que **recebe** dano | `Runtime.Combat.Hurtbox` — colisor **trigger** num GameObject **filho** |

**A hitbox não é um colisor.** É `Physics2D.OverlapCircle` com `ContactFilter2D`, rodada a cada
`FixedUpdate` enquanto a janela está aberta. A razão está no próprio doc da classe: um trigger só
dispara `OnTriggerEnter2D` em quem **entra**; se a janela abre com o alvo **já** sobreposto — o
caso mais comum, porque o inimigo mira em quem está perto — o evento nunca vem e o golpe passa
branco.

**A hurtbox fica num filho, não na raiz.** A raiz carrega o `Rigidbody2D` e a pegada de
movimento (achatada 2:1, no chão); a hurtbox cobre o **corpo desenhado**. Antes de 2026-08-21 um
colisor só fazia os três trabalhos, e o do Damião tinha 1,467 de pegada: largo demais para andar
(entalava em quina) e largo demais para apanhar.

---

## Tabela 1 — Combate

| arquivo / classe | papel | forma e tamanho | offset | momento | LayerMask | contínuo? |
|---|---|---|---|---|---|---|
| `Combat/Hitbox.cs` · `Hitbox` | **hitbox** (motor) | círculo, `raio` configurável | `deslocamento`, girado para a direção do golpe | `FixedUpdate` enquanto `_ativa` | `camadasAlvo` (injetada) | **contínuo** — janela de `duracaoAtiva`s, com conjunto de já-atingidos por ativação |
| `Player/MaoFisicaBridge.cs` · `MaoFisicaBridge` | **hitbox** (jogador) | círculo, raio **0,42 – 0,85** pela arma | alcance **0,95 – 1,60** à frente | `Armar` no golpe → `FixedUpdate` | `camadaInimigos │ EnemyHurtbox` | **contínuo** — janela **0,07 – 0,15 s** pela arma |
| `Enemies/ByakheeAI.cs` · `ByakheeAI` | **hitbox** (chefe) | círculo, via `hitboxDasGarras` | `alcanceDasGarras = 1,5` | `Armar` na entrada de `Pousado` | do prefab | **contínuo** — janela **0,25 s** |
| `Enemies/Components/EnemyCombat.cs` · `EnemyCombat` | **hitbox** (Cultista) | círculo **1,2** | **nenhum** — centrado no próprio inimigo | `TentarAtacar`, no `Update` da IA | `Player │ Aliados` (padrão forçado) | ❌ **instantâneo e radial** |
| `Enemies/EsqueletoInvocado.cs` · `EsqueletoInvocado` | **hitbox** (invocado) | **nenhuma geometria** no instante do dano | — | `FixedUpdate` porteia por `Distance`, dano sai na cadência | — | ❌ **instantâneo, sem forma** |
| `Enemies/SsethFarejadorAI.cs` · `SsethFarejadorAI` | **hitbox** (Sseth) | `Vector2.Distance <= alcanceDeGolpe` | — | `Update` | — | ❌ **instantâneo e radial** |
| `Enemies/ConeDeGelo.cs` · `ConeDeGelo` | **hitbox** (projétil do Abdul) | colisor do prefab | move-se; `velocidade = 6` | `OnTriggerEnter2D`; anda no `FixedUpdate` | `camadasQueBloqueiam` | ⚠️ **instantâneo na entrada** — não dispara se nascer já sobreposto |
| `Enemies/CoisaDoCemiterioAI.cs` · `CoisaDoCemiterioAI` | **hitbox** (morte instantânea) | colisor do prefab | — | `OnTriggerEnter2D`, tag `Player` | tag, não máscara | ⚠️ **instantâneo na entrada** → `ForcarColapso()` |
| `Combat/Hurtbox.cs` · `Hurtbox` | **hurtbox** | trigger do prefab, por ator (ver Tabela 2) | filho, offset por ator | passiva — consultada | camadas **13** `PlayerHurtbox` / **14** `EnemyHurtbox` | passiva; força `isTrigger` no `Awake` |
| `Player/ArtefatosBridge.cs` · `ArtefatosBridge` | utilitário (**não causa dano**) | `OverlapCircleAll`, raio da habilidade | centrado no jogador | chamada da habilidade | `camadasDeEntidade` | ❌ instantâneo — `RevelarEntidades`, `AplacarSerpentes` |

### Geometria por família de arma

| família | alcance | raio | janela ativa |
|---|---|---|---|
| Lâmina Fina (adaga) | 0,95 | 0,42 | **0,07 s** |
| Maça | 1,20 | 0,60 | 0,10 s |
| Alfanje | 1,60 | 0,85 | **0,15 s** |

Padrão do código quando não há arma: 1,20 / 0,60 / 0,10 s. A geometria virou propriedade da
**arma** em 2026-08-27; antes havia um `alcance = 1.2f` no `MaoFisicaBridge` valendo para todas.

---

## Tabela 2 — Hurtbox por ator, contra a arte desenhada

Medido por quadro (não pela folha inteira) em 2026-09-03.

| ator | maior quadro | hurtbox (mundo) | cobertura |
|---|---|---|---|
| Damião | 1,00 × 2,53 | 0,84 × 2,27 @ y 1,25 | 90% |
| Cultista | — × 2,25 | 1,76 × 2,31 @ y 1,28 | 103% |
| Byakhee | 4,22 × 4,59 | 3,69 × 4,41 @ y 2,56 | 96% |
| Esqueleto Invocado | 1,16 × 2,09 | 1,30 × 1,95 @ y 1,00 | 93% |
| Abdul Alhazred | 1,56 × 2,06 | 1,40 × 2,00 @ y 1,03 | 97% |
| Pedra de Poder | 0,76 × 1,24 | 0,72 × 1,29 @ y 0,75 | 104% |

> **Armadilha de método.** A primeira varredura acusou Byakhee em 15% e Pedra em 49%. As duas
> eram artefato: medi a **folha inteira** em vez do quadro, e no caso da Pedra medi a aura
> composta em vez do cristal. Medir por quadro é obrigatório aqui.

---

## Tabela 3 — i-frames, knockback e hitstop

| arquivo / classe | o que faz | como | duração |
|---|---|---|---|
| `Player/EsquivaBridge.cs` · `EsquivaBridge` | **i-frames** | **desliga o `Collider2D` da Hurtbox**, com `try/finally` | `EsquivaConfig.duration = 0,15 s` |
| `Combat/RepulsaoDeImpacto.cs` · `RepulsaoDeImpacto` | knockback | `Empurrar(direcao, forca)` decai `linearVelocity` | `DuracaoDoEmpurrao = 0,18 s` |
| `Combat/HitStop.cs` · `HitStop` | hitstop | `Time.timeScale = 0,05` por dano | teto `DuracaoMaxima = 0,12 s` |
| `Enemies/YugNethAI.cs` · `YugNethAI` | matriz | `Physics2D.IgnoreCollision(..., true)` | permanente — o companheiro não empurra |

**Os i-frames não são troca de camada, e isso é deliberado.** O dano deste jogo é resolvido por
*consulta* (`OverlapCircle`), e consulta **não olha a matriz de colisão** — só a máscara. Um
`IgnoreLayerCollision` não daria invencibilidade nenhuma contra o caminho que o jogo realmente
usa. Desligar o colisor da hurtbox dá.

O `try/finally` também não é zelo: sem ele, uma troca de cena ou uma morte no meio da esquiva
deixaria o Damião **permanentemente invulnerável**.

---

## Tabela 4 — Consultas de física fora de combate

| arquivo / classe | uso | forma | máscara | contínuo? |
|---|---|---|---|---|
| `Core/Combat/CortesaoPalido.cs` | campo de visão + linha de visão | `OverlapCircle(6)` + `Raycast` | do prefab | por tick da IA |
| `Interaction/DetectorDeInteracao.cs` | prompt de interação | `OverlapCircle(1,5)`, buffer de **8** | do prefab | contínuo |
| `Navegacao/NavegacaoDoMundo.cs` | célula livre | `OverlapBox` | `camadasQueBloqueiam` | sob demanda |
| `Environment/VeuDaTempestade.cs`, `TempestadeRajadaAleatoria.cs` | zonas de tempestade | trigger | — | Enter/Exit |
| `GameLoop/*Trigger.cs`, `PortalDeCena.cs`, `RefugioDeLuz.cs`, `ArenaDosPortoes.cs` | gatilhos de cena | trigger | — | Enter/Exit |
| `Level/PressaoPsiquicaZone.cs`, `CasteloDeCarcosaZone.cs` | zonas do Castelo | trigger | — | Enter/Exit |
| `Rendering/OcclusaoDitherFade.cs` | fade de oclusão | trigger | — | Enter/Exit |

---

## O que a auditoria encontrou

### 1. A migração para janela ativa parou no Byakhee

O `Hitbox` com janela existe, está correto, e o doc dele descreve exatamente o defeito que ele
conserta — *"não há janela… não há direção. Estar **atrás** do Byakhee, a 1,4 unidade, levava
garrada igual. Lê como injustiça, porque contradiz o que se vê."*

**Só o jogador e o Byakhee o usam.** O Cultista — o inimigo que o jogador mais encontra — ainda
acerta por `OverlapCircle` centrado nele mesmo, num instante: sem janela para esquivar e sem
direção, então estar atrás dele não protege.

### 2. O Esqueleto Invocado não tem geometria nenhuma no golpe

Ele é o caso extremo: `TentarGolpear` chama `ReceberDanoFisico` direto na cadência, e a única
condição é o `Distance <= alcanceDeGolpe` do `FixedUpdate`. Não há forma, direção nem janela.

**Isso piorou em 2026-09-03**, quando ele ganhou um golpe animado de 5 quadros com arco de
lâmina: a animação agora **promete uma janela que o código não tem**.

### 3. As camadas 11 e 12 estão vazias

O doc do `Hurtbox` afirma que `PlayerHitbox` (11) e `EnemyHitbox` (12) *"estavam declaradas em
`TagManager.asset` desde sempre"*. Conferido: **as camadas 11 e 12 estão vazias**; só
`PlayerHurtbox` (13) e `EnemyHurtbox` (14) existem.

É inofensivo — a hitbox deste projeto é consulta, não colisor, e não precisa de camada própria —
mas a afirmação do doc é falsa hoje e vai enganar quem for procurar.

### 4. O Cone de Gelo e a Coisa do Cemitério dependem de `OnTriggerEnter2D`

É o modelo certo para um projétil que viaja. Mas vale a mesma ressalva que motivou o `Hitbox`:
se o objeto nascer **já sobreposto** ao alvo, o evento nunca vem. Para o Cone de Gelo isso é
plausível quando o Abdul conjura colado no jogador.

## A ferramenta: Visualizador de Golpes

`Assets/Scripts/Diagnostico/VisualizadorDeGolpes.cs` desenha, por cima do jogo, o que esta
auditoria mediu em texto. **F11** alterna.

| cor | o que é |
|---|---|
| verde | hurtbox |
| **amarelo** | hurtbox com o colisor **desligado** — i-frames acontecendo |
| vermelho | hitbox registrada pelo código de combate |
| azul | gatilho que não é hurtbox: zonas, portais, interação |
| cinza | pegada de movimento (desligado por padrão) |

**Ele precisa da API de registro porque a hitbox daqui não é um colisor.** Uma varredura de
`Collider2D` acha todas as hurtboxes e **zero hitboxes**. `RegistrarCirculo(centro, raio, cor)`
é chamado de dentro de `Hitbox.Consultar`, `EnemyCombat`, `EsqueletoInvocado` e
`SsethFarejadorAI`, com a geometria **exata** que foi para a física.

**As marcas expiram por tempo, e não por quadro.** Limpar a cada quadro era o desenho óbvio, e
esconde justamente o que interessa: um golpe instantâneo existe por **um** quadro, e um quadro
a 60 fps é invisível. Com o piso de permanência, um golpe de janela zero aparece tanto quanto
um de janela longa — e a diferença entre eles fica legível na tela.

**Custo em release: zero.** Os métodos de registro carregam `[Conditional]`, então o compilador
apaga as chamadas fora do Editor e de build de desenvolvimento. A varredura de colisores, que
usa `Find*` (proibido em produção por `Assets/Scripts/CLAUDE.md`), vive atrás de
`#if UNITY_EDITOR || DEVELOPMENT_BUILD`.

### Três armadilhas que ele evitou

1. **`KeyCode` não funciona neste projeto.** `activeInputHandler: 1` — só Input System novo.
   `Input.GetKeyDown` lançaria em runtime. Usa `Keyboard.current.f11Key`.
2. **`Collider2D.bounds` fica vazio com o colisor desligado** (doc da 6000.4). Usá-lo faria a
   hurtbox do Damião **sumir justamente durante os i-frames** — o momento que mais interessa.
   O desenho vai por `Gizmos.matrix = localToWorldMatrix`, que funciona ligada ou desligada.
3. **`FindObjectsByType(Type, FindObjectsSortMode)` está ela própria obsoleta na 6000.4.** Só a
   sobrecarga genérica de dois parâmetros sobrevive.

## A suíte que mede rodando: `HitboxAuditTests`

`Assets/Tests/PlayMode/HitboxAuditTests.cs`, 10 testes. Tudo acima nesta página foi lido do
YAML e do código; esta suíte mede o **comportamento**, com física rodando.

Cobre: alcance no limite e além dele (+0,1); as quatro direções; o golpe **não** acertando pelas
costas; o chão como controle negativo de máscara; o golpe do inimigo no jogador, dentro e fora
do alcance; os i-frames antes e depois; e um teste que **só registra** a geometria medida.

Saída do registro, na rodada de 2026-09-03:

```
[HitboxAudit] medido em jogo, célula isométrica 1 x 0,5
  golpe do jogador : alcance 1,2  raio 0,6  janela 0,1 s (5 ticks de física)
  cobre de 0,6 a 1,8 à frente = 1,8 larguras de célula
  hurtbox do alvo  : 0,72 x 1,72 em (0,00, 1,00)
  hurtbox do Damião: 0,72 x 1,72 em (0,00, 1,01)
  acerto máximo de centro a centro: 2,16
```

### Três coisas que escrever esta suíte ensinou

**1. A mão vazia causa dano ZERO.** O log do próprio `MaoFisicaBridge` diz
`arma=DESARMADO (mão vazia) ... dano=0`. A primeira versão media *dano* e reprovava todo golpe
correto. Uma auditoria de **geometria** mede **acerto** — quanto dói é balanceamento, e muda.

**2. A ordem de `AddComponent` importa.** `PlayerMovement.Awake` resolve as bridges por
`GetComponent` e injeta nelas a `PlayerStateMachine` **que ele mesmo tica**. Montando o rig com
ele primeiro, ele não acha ninguém, ninguém recebe FSM, e uma FSM injetada por fora nunca
avança: o primeiro golpe entra em `Atacando` e o ator fica preso ali para sempre.

**3. `VitalidadeBridge.Awake` chama `Hurtbox.GarantirPara(gameObject, "PlayerHurtbox")` sem
condição.** No jogo está certo — ela só existe no Damião e no Yug-Neth, os dois aliados. Mas
num inimigo ela criaria a hurtbox na camada do JOGADOR, e a chamada seguinte com `EnemyHurtbox`
devolveria a que já existe, na camada errada. Por isso o rig usa um `AlvoDeTeste` próprio.

> **As três falhas eram do rig, não do jogo** — que é o resultado mais provável quando um teste
> novo falha num sistema que já roda. Vale duvidar do teste antes de duvidar do código.

### O que o limite de acerto realmente é

Não é `alcance + raio`. A hitbox é um círculo cuja borda externa fica ali, mas a hurtbox do alvo
é uma caixa que se estende **para trás** pela metade da própria largura — o contato acontece
quando as duas bordas se encostam. O teste mede a meia-largura do colisor que o jogo criou, em
vez de assumir, e lê alcance/raio/janela do componente **por reflexão**: uma cópia dos números
passaria a medir a cópia depois de alguém mexer na arma.

## 2026-09-03, mais tarde: três fases, e a hitbox que saía do pé

Duas mudanças no golpe básico do jogador. A segunda mudou a primeira, então foram juntas.

### A hitbox saía do PÉ

`Hitbox.GarantirPara` fazia `SetParent(dono.transform, false)` e mais nada. O pivô de todo o
elenco é BottomCenter (o jogo ordena profundidade por `-worldCenter.y`), então a hitbox nascia
no chão:

| | cobertura vertical |
|---|---|
| círculo do golpe, origem no pé (raio 0,6) | y −0,60 a **+0,60** — metade **abaixo do chão** |
| hurtbox do alvo | y 0,14 a 1,86 |
| **sobreposição** | **0,46 de 1,72 = 27% do corpo** — a canela |
| com a origem no meio do corpo | 1,20 = **70%** |

Os números já estavam impressos no registro do `HitboxAuditTests` do dia (`hurtbox do alvo:
0,72 × 1,72 em (0,00, 1,00)`) e ninguém tinha lido o que implicavam. Também explicava a margem
apertada que o teste de alcance vinha raspando.

A altura é **derivada da arte** (`sprite.bounds.center.y`, a mesma fonte que
`Hurtbox.GarantirPara` usa), e não um `+0.5f` fixo: assim a garra de um Byakhee de 4,6 unidades
sai do corpo dele, e não da altura do peito do Damião.

### As três fases

`BaseDeArma` ganhou `Preparo` e `Recuperacao` ao lado da `JanelaAtiva` que já existia, e os 9
assets receberam os valores explicitamente (campo ausente no YAML depende de sutileza de
desserialização):

| arma | preparo | ativo | recuperação | total |
|---|---|---|---|---|
| Lâmina Fina | 0,1 | 0,07 | 0,2 | **0,37 s** |
| Maça | 0,1 | 0,10 | 0,2 | **0,40 s** |
| Alfanje | 0,1 | 0,15 | 0,2 | **0,45 s** |

A distinção entre as famílias sobrevive: a janela ativa continua sendo a de cada uma.

**O que mudou de verdade é o PREPARO.** Até aqui a hitbox era armada no mesmo quadro do
comando — o golpe saía do nada, sem telegrafo, e não havia instante em que ele já estava
decidido e ainda não tinha acertado.

A **recuperação não precisa de código**: é o resto do bloqueio da FSM depois de a janela
fechar. Escrever uma espera para ela seria um segundo relógio para o mesmo tempo.

> **Consequência de jogo, e ela é grande.** A FSM passa a trancar pela soma das fases, e não
> mais por `resultado.DurationSeconds`. A mão vazia vai de **0,20 s para 0,45 s** de
> compromisso. É o que "recuperação" significa — errar passa a custar — mas só o playteste
> diz se o toque ficou bom.

Desenhar durante os quadros ativos já funcionava: o `VisualizadorDeGolpes` registra de dentro
de `Hitbox.Consultar`. Com o preparo, agora há um intervalo visível em que nada é desenhado e
o golpe já foi comandado — que é exatamente o telegrafo.

### Medição depois das duas mudanças

```
[HitboxAudit] célula isométrica 1 x 0,5
  golpe do jogador : alcance 1,2  raio 0,6
  fases            : preparo 0,1 s  ativo 0,15 s (7,5 ticks) total 0,45 s
  cobre de 0,6 a 1,8 à frente = 1,8 larguras de célula
  hurtbox do alvo  : 0,72 x 1,72 em (0,00, 1,00)
  acerto máximo de centro a centro: 2,16
  origem do golpe  : y 1  (0 seria o pé)
```

Guardas novas: `OPreparo_AdiaOAcerto_ENaoOImpede` (os dois lados num teste só — um passaria
com a hitbox quebrada, o outro com o preparo ignorado) e `AHitbox_SaiDoCorpoENaoDoPe`, que
falha se a sobreposição vertical cair abaixo de 50% do corpo.

## Próximo passo recomendado

Migrar `EnemyCombat` (Cultista) e `EsqueletoInvocado` para `Hitbox` com janela e direção. É a
mudança que mais altera o toque do combate, e o Esqueleto é o mais urgente porque a animação
nova dele mente sobre a janela.
