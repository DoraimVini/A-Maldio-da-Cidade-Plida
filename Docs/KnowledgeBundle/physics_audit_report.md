---
type: Game System
title: Auditoria de Física 2D — relatório consolidado
description: Estado medido da física do projeto em 2026-09-04 — Physics2D, Rigidbody2D, Collider2D, consultas, movimento e testes. Consolida os três relatórios gerados por ferramenta e registra o que foi corrigido e o que não foi.
date: 2026-09-04
---

# Auditoria de Física 2D — relatório consolidado

> **O que este documento é.** Um retrato **medido** do estado da física em 2026-09-04, não um
> plano. Todo número aqui saiu de ferramenta ou de leitura de arquivo no disco, e as três
> tabelas geradas por ferramenta continuam vivas em
> [`auditoria_rigidbody2d.md`](systems/auditoria_rigidbody2d.md),
> [`auditoria_colisores.md`](systems/auditoria_colisores.md) e
> [`auditoria_gatilhos.md`](systems/auditoria_gatilhos.md).

> **A primeira versão deste documento listava dez pendências.** O Vini respondeu: *"ao invés
> de jogar no documento sem corrigir, corrija antes de subir para documento."* Ele estava certo
> — relatório que enfileira achado sem conserto é lista de dívida disfarçada de auditoria.
> Sobraram **quatro**, e cada uma diz por que resistiu. Uma delas depende de um número que só
> ele pode dar.

---

## 1. Physics2D — a linha de base

Lido de `ProjectSettings/Physics2DSettings.asset` e `TimeManager.asset`.

| campo | valor | leitura |
|---|---|---|
| `Gravity` | **(0, 0)** | Correto para isométrico de cima. Casa com `gravityScale 0` em todo corpo |
| `Default Material` | **nenhum** | Ver §3.3 |
| `Velocity Iterations` | 6 | Padrão da engine |
| `Position Iterations` | 4 | Padrão da engine |
| `Default Contact Offset` | 0.01 | Padrão |
| `Simulation Mode` | **FixedUpdate** | O movimento tem de ser escrito em `FixedUpdate` — ver §5 |
| `Queries Hit Triggers` | **sim** | **Load-bearing**: as hurtboxes são triggers, e sem isto nenhum golpe acha nada |
| `Queries Start In Colliders` | não | Consulta que nasce dentro de um colisor o ignora |
| `Callbacks On Disable` | sim | |
| `Reuse Collision Callbacks` | sim | |
| **`Auto Sync Transforms`** | **NÃO** | **O campo mais importante da tabela** — ver abaixo |
| `Fixed Timestep` | **0.02** (50 Hz) | |
| `Maximum Allowed Timestep` | **0.05** | Bem abaixo do padrão 0.333 da engine |

### Por que `Auto Sync Transforms: não` domina o resto

Com ele desligado, escrever `transform.position` num objeto que tem `Rigidbody2D` **não move o
colisor na hora**. As consultas continuam enxergando a posição anterior até o próximo passo de
física. É isso que separa "manipular transform é feio" de "manipular transform causa erro
mensurável" — e é o que dá peso à §5.

### Matriz de colisão

**13 camadas em uso, 27 pares desligados.** O desenho é coerente e vale registrar:

- **`PlayerHurtbox` e `EnemyHurtbox` não colidem com absolutamente nada** — nem com o cenário,
  nem entre si. Elas existem **só para serem achadas por consulta**. Consulta não olha a matriz,
  só a máscara, então isso não atrapalha o combate e evita que a hurtbox empurre o mundo.
- **`Enemy × Enemy` desligado** — inimigos não se empurram.
- **`Enemy × Player` desligado** — inimigos **atravessam** o Damião. Quem barra o movimento
  entre eles é a pegada, não o corpo.

---

## 2. Rigidbody2D — 35 corpos

| | |
|---|---|
| Total | **35** (6 cenas do Build Settings + todos os prefabs) |
| Dynamic | 31 |
| Static | 4 |
| `gravityScale` | **unânime em 0** |
| `sleepMode` | **unânime em `StartAwake`** |

### Três campos divergem, e os três pelo mesmo motivo

| campo | divergência |
|---|---|
| `collisionDetectionMode` | `Continuous` × 31, `Discrete` × 4 |
| `interpolation` | `Interpolate` × 31, `None` × 4 |
| `constraints` | `FreezeRotation` × 31, nenhuma × 4 |

**Os quatro divergentes são os mesmos quatro objetos** em todos os casos: os
`*Grid/Colisao` — os tilemaps de colisão do Deserto, Santuário, Castelo e Portões.
São chão estático. `Continuous`, `Interpolate` e `FreezeRotation` **não fazem sentido** para
geometria que não se move.

### Nada foi corrigido aqui, e isso é o resultado

**Zero anomalias de `Rigidbody2D`.** Os 21 "pareamentos suspeitos" que a ferramenta acusou
(colisor sem corpo, ou corpo sem colisor) foram conferidos um a um e **todos são cenário
estático legítimo** — paredes, limites de mapa, o Lago de Hali, os Nobres Fossilizados, os
Portões. Confirmado inclusive que o `PortaoDosPortoes` abre por `barreira.enabled = false`, e
não movendo o transform.

---

## 3. Collider2D — 139 colisores

| papel | quantos | contra o que é conferido |
|---|---|---|
| Gatilho | 56 | nada (zona, portal, coletável não têm relação com sprite) |
| Cenário | 35 | nada (parede não tem tamanho esperado) |
| Hurtbox | 26 | silhueta × `0,72 / 0,86` (fatores de `Hurtbox.GarantirPara`) e centro |
| Pegada | 22 | proporção de chão 2:1 e linha do pé |

Formas: Box 93, Circle 29, Capsule 8, TilemapCollider2D 4, Composite 4, Polygon 1.

### Por que a comparação é por papel

Comparar todo colisor com o sprite inteiro, no limiar de 20% pedido originalmente, **marcaria
100% do elenco** — porque os dois maiores desvios são deliberados:

- a **hurtbox** nasce em `0,72 × 0,86` da silhueta → sozinho isso dá −28% e −14%;
- a **pegada** é área de *chão* de `0,60 × 0,30` num corpo que o sprite desenha com ~2,5 de
  altura → −40% e −88%.

A primeira versão da regra por papel ainda era grosseira demais (chamava de pegada todo colisor
sólido) e acusou **57 de 141**, incluindo as paredes do Santuário e o Lago de Hali. Separando
ator de cenário: **57 → 15**.

### Corrigido

- **Dois gatilhos órfãos no Byakhee** (`Portoes_Das_Ruinas`): um `Circle` e um `Box` marcados
  como trigger, **sem callback e sem componente que os explicasse**, presentes só na instância
  de cena e não no prefab. Não eram cosméticos — o Byakhee está na camada `Enemy`, que está em
  `DetectorDeInteracao.CamadasPadraoDeInteragiveis`, e cada um comia um slot do buffer de
  interação perto do chefe. **Removidos por ferramenta**, porque componente adicionado a
  `PrefabInstance` vive em dois lugares no YAML.

### Corrigido depois da primeira redação deste relatório

`RecalibrarColisoresDeAtor` refez **11 hurtboxes e 3 pegadas**, com os números vindo das mesmas
fontes que o jogo usa — os fatores `0,72 × 0,86` de `GarantirPara` e a razão 2:1 da célula, e
não constantes novas na ferramenta:

| ator | de | para |
|---|---|---|
| **Abdul** (hurtbox) | 0,48 × 0,94 | **1,62 × 2,47** |
| Cultista × 10 (hurtbox) | 1,00 × 2,31 | **1,76 × 2,31** |
| YugNeth (pegada) | 0,68 × 1,34 | 0,68 × 0,34 |
| Cortesão × 2 (pegada) | 0,60 × 0,60 | 0,60 × 0,30 |

A do Abdul era a mais grave e não estava na lista original: o chefe da Tumba tinha **metade** da
área atingível que a derivação daria. Isso existia porque `GarantirPara` **não corrige o que já
existe** (`if (existente != null) return existente;`), então todo valor errado gravado numa cena
sobrevive para sempre — foi por isso que o conserto teve de ser por ferramenta.

### Encontrado e NÃO corrigido: a escala

Sobram **29 achados, e todos são a mesma coisa**: nenhum ator instanciado em cena tem escala
uniforme.

| ator | escala da instância | esticado em Y |
|---|---|---|
| Abdul (`Tumba_De_Alhazred`) | 1,162 × 2,671 | **2,30×** |
| Cultista (10 no `Deserto_Hali`) | 0,630 × 0,804 | 1,28× |
| Cassilda (`Santuario_Yhtill`) | 1,478 × 1,925 | 1,30× |
| YugNeth | 0,901 × 1,133 | 1,26× |
| Byakhee (`Portoes`) | 1,021 × 0,938 | 0,92× (achatado) |

**Por que este é o único que não corrigi sozinho:** uniformizar exige escolher qual eixo vence,
e a escolha muda o tamanho visual de cada personagem. Igualar pela altura alarga o Abdul em
2,3×; igualar pela largura o deixa com menos da metade da altura. É decisão de olho, não de
regra — e o Vini ajustou a altura do Abdul à mão em 2026-09-03.

Isso derrubou uma afirmação escrita em dois arquivos do projeto ("todo o elenco está em escala
uniforme"): a medição antiga olhou a **raiz dos prefabs**, e é na **instância** que a escala é
sobrescrita.

### 3.3 Materiais físicos: não existe nenhum

| o quê | resultado |
|---|---|
| assets `.physicsMaterial2D` | **0** |
| componentes com `m_Material` atribuído (135 varridos) | **0** |
| `m_DefaultMaterial` global | **vazio** |
| colisores com elasticidade > 0 | **0** |
| colisores com atrito > 1 | **0** |

Os 139 colisores medem **atrito 0,4 e elasticidade 0** — exatamente o padrão desejado. Mas ele
vale **por acidente**: não há material nenhum, então todo colisor cai no built-in da engine.
`MateriaisDeFisicaTests` passou a guardar os três caminhos autorados (material no colisor,
material no corpo, padrão global) — o quarto é da engine e não há o que guardar.

> A doc de `Collider2D.friction` da 6000.4 diz que o material chega por **quatro** caminhos,
> incluindo *indiretamente pelo `Rigidbody2D.sharedMaterial`*. A auditoria lia só o do colisor;
> passou a ler `Collider2D.friction`/`.bounciness`, que já entregam o valor **resolvido**.

---

## 4. Consultas de física e seus parâmetros

Nove sítios de consulta em todo o projeto. **Nenhum usa `Physics.*` 3D** (varrido: zero).

| arquivo:linha | consulta | para quê | parâmetros validados |
|---|---|---|---|
| `Combat/Hitbox.cs:326` | `OverlapCircle` + `ContactFilter2D` | **o golpe** | raio e alcance da arma; janela ativa; máscara de hurtbox; `useTriggers = true`; portão de profundidade de 1 célula |
| `Enemies/Components/EnemyCombat.cs:99` | `OverlapCircle` | golpe do Cultista | **instantâneo e radial** — ver §7 |
| `Enemies/CortesaoPalido.cs:265` | `OverlapCircle` | **visão** do Cortesão | campo de visão 6; camada do jogador resolvida no `Awake` |
| `Enemies/CortesaoPalido.cs:285` | `Raycast` | **oclusão** da visão | `layerObstaculos`; distância até o alvo |
| `Enemies/SsethFarejadorAI.cs:107` | `OverlapCircle` | faro do Sseth | |
| `Interaction/DetectorDeInteracao.cs:169` | `OverlapCircle` (buffer) | achar o que é interagível | alcance 1,5; **buffer fixo, 8 → 16** |
| `Navegacao/NavegacaoDoMundo.cs:165` | `OverlapBox` | célula livre para navegar | |
| `Player/ArtefatosBridge.cs:262` | `OverlapCircleAll` | Revelação (relíquia) | máscara `~0` — ver §7 |
| `Player/ArtefatosBridge.cs:284` | `OverlapCircleAll` | Aplacamento (relíquia) | máscara `~0` — ver §7 |

**A hitbox deste projeto NÃO é um colisor.** É consulta rodada a cada `FixedUpdate` enquanto a
janela está aberta. Uma varredura de `Collider2D` acha todas as hurtboxes e **zero hitboxes** —
por isso o `VisualizadorDeGolpes` existe: o código de combate avisa a geometria que consultou.

**Callbacks:** as 19 declarações do projeto são **todas de trigger**. Medido por `TypeCache`
sobre todos os tipos: **zero** `OnCollisionEnter2D`/`Stay`/`Exit`. Nada depende de evento de
colisão sólida — colisor sólido aqui só barra movimento.

### Corrigido nesta frente

- **Buffer de interação 8 → 16.** O aviso apareceu em playteste. A máscara não dá para apertar:
  `AbdulAlhazredAI` e `NagarajaAI` **são** `IInteragivel` e vivem na camada `Enemy`.
- **Portão de profundidade** no golpe do Damião (§6).

---

## 5. Movimento que contorna a física

Varridos 52 sítios de escrita em `transform.position`/`localPosition`/`Translate` e de API de
`Rigidbody2D`. Depois de excluir UI, objetos sem corpo e criação de filhos em setup:

### Bypass real — **refatorado**

| arquivo | o que era | o que é |
|---|---|---|
| `Enemies/Components/EnemyMovement.cs` | escrevia `linearVelocity` dentro de `MoverPara`, chamado do **`Update`** por `EnemyStateMachine`, `AvatarDeSetAI` e `SsethFarejadorAI` | `MoverPara` apenas **guarda** a decisão; o **`FixedUpdate`** é o único lugar que escreve no corpo |
| `Enemies/NagarajaAI.cs` | `ExecutarRotinaDeLuta` escrevia `linearVelocity` do `Update` | guarda em `_velocidadeDesejada`; `FixedUpdate` novo aplica |
| `Cinematics/AberturaDesertoCinematica.cs` | `playerTransform.position +=` todo quadro, no Damião | `Rigidbody2D.position +=`, com o transform como reserva |

**Por que isso importa aqui e não é purismo.** Escrever velocidade em ritmo **variável** para
consumo em ritmo **fixo** (50 Hz) faz a mesma decisão valer por dois passos de física num quadro
rápido, e duas decisões se atropelarem num quadro lento — a segunda descarta a primeira antes de
ela ter existido. E `EnemyMovement` é o componente de movimento **compartilhado**: o defeito
valia para boa parte do elenco de uma vez.

O componente **já tinha o padrão certo** para o caminho com aceleração — guardava e aplicava no
`FixedUpdate`. Era só o caminho sem aceleração que escrevia direto. Os dois foram unificados.

`Parar()` continua zerando **também** de imediato, e não só no `FixedUpdate`:
`EnemyStateMachine.EnterState` chama `Parar()` ao entrar em Patrol justamente porque sair de
Chase com a velocidade cravada fazia o inimigo deslizar para fora da cena. Esperar 20 ms para
zerar reintroduziria uma versão curta disso.

### Baixo risco, deixados como estão

`GameLoop/PontoDeChegada.cs:58` e `GameLoop/TravessiaDoCompanheiro.cs:134` escrevem
`transform.position` em objetos com corpo — mas **uma vez, em `Start`**, antes de a física
importar. O certo seria `rb.position`; o custo de errar é zero.

`GameLoop/CercoZ4Cutscene.cs:159` usa `Rb.position` (**API certa** — sincroniza o corpo), só que
interpolando a `Time.deltaTime` numa corrotina.

### Nada usa `MovePosition`

Zero ocorrências no projeto, coerente com a convenção do `CLAUDE.md` §5 (`linearVelocity` em
`FixedUpdate`).

---

## 6. Testes PlayMode acrescentados

O PlayMode saiu de **35 para 50** testes nesta auditoria.

| classe | testes | para quê |
|---|---|---|
| `PhysicsQueryAuditTests` | **11** | Um tipo de consulta por região: coleta (o `OverlapCircle` do detector, dentro e fora do alcance, e a coleta em si), audição (dentro do raio, fora, e som fraco), efeito em área (dentro e fora), agressão. Mais **duas guardas de cobertura** que falham se os testes de golpe e i-frame sumirem do `HitboxAuditTests` — em vez de duplicar as asserções e criar duas fontes da verdade |
| `HitboxAuditTests` (acrescentado) | **+4** | Portão de profundidade: acerta dentro de uma célula; **prova** que é o portão e não a distância que rejeita (mesma posição medida duas vezes, com e sem portão); golpeia para o **norte** contra alvo colado; e afirma que a profundidade segue **o ator** e não o contêiner de cena |

### Os dois testes que existem por causa de um bug meu

- **`AProfundidadeDoGolpe_SegueOAtor_ENaoOConteinerDaCena`** — o portão media com
  `transform.root`, que numa cena real devolve o **contêiner de organização** (em y = 0)
  enquanto os atores estão em y ≈ −14. Resultado: **todo golpe do Damião era rejeitado**. O rig
  antigo montava tudo solto na raiz e em y = 0, as duas condições que escondem o defeito.
- **`GolpeandoParaONorte_AcertaQuemEstaPerto`** — a faixa do portão era em torno do **centro** do
  golpe. Atacando para o norte ela virava `[0,70; 1,70]`, e inimigo **colado** era rejeitado.
  Todos os testes golpeavam para a direita, onde o defeito não aparece.

**O rig do `HitboxAuditTests` foi reescrito** para espelhar a cena: atores dentro de contêineres,
montados em (12, −14) e não na origem, com todas as posições relativas ao jogador.

### O QA rodava a metade errada

`Tools/run_qa_tests.ps1` tinha padrão **EditMode**, e **todos** os testes de física vivem no
PlayMode. Quem rodava sem argumento validava 146 arquivos de configuração e **zero de combate**.
Somado ao parâmetro engolido (sem `[CmdletBinding()]`, `-Modo PlayMode` rodava EditMode e
imprimia `TESTS PASSED`), o jeito óbvio de rodar a suíte rodava a metade errada em silêncio.

Corrigido: `[CmdletBinding()]` + `[ValidateSet]`, e o **padrão passou a ser `Ambos`**.

**Estado atual:** EditMode 1052 (1029 passando, 23 aposentados) + PlayMode 50 (50 passando) =
**1102 testes, 0 falhando**.

---

## 7. O que continua aberto, e por quê

A lista encolheu de dez para quatro. O que saiu está nas §3 e §5.

| # | achado | por que não foi corrigido |
|---|---|---|
| 1 | **Nenhum ator instanciado em cena tem escala uniforme** (Abdul esticado 2,30× em Y) | **Precisa de decisão de olho.** Uniformizar exige escolher qual eixo vence: pela altura, o Abdul alarga 2,3×; pela largura, perde mais da metade da altura. É a única pendência que depende de um número que só o Vini pode dar |
| 2 | **`EsqueletoInvocado` ainda acerta instantâneo** | Ele não usa `EnemyCombat` — tem golpe próprio. A migração é a mesma receita já aplicada ao Cultista e ao Cortesão, mas mexe num inimigo que só aparece na luta do Abdul, e vale medir a luta antes |
| 3 | **`CortesaoPalido` é `MonoBehaviour` sem FSM separada** | Saiu de `Core/` para `Runtime/Enemies/` porque `Core` tem `references: []` e não enxerga `Hitbox`/`EnemyBase` — sem isso o ator **não tinha como** ganhar combate. Isso põe o arquivo onde ele deve morar, mas **não paga** a dívida POCO: separar `CortesaoPalidoFSM` continua devendo |
| 4 | **O `Deserto_Hali` tem 10 Cultistas**, quatro com o nome literalmente idêntico | Conteúdo de cena, não física. Registrado porque o relatório parecia ter linhas duplicadas e não tinha — e porque dez inimigos onde a nomenclatura sugere quatro é fato de balanceamento |

### O que foi corrigido nesta rodada

- Hurtbox do **Abdul** dobrada para o valor derivado (era metade)
- **10 hurtboxes de Cultista** alargadas em 76%
- **3 pegadas** deitadas na razão 2:1 (YugNeth e os dois Cortesãos)
- **`EnemyMovement`, `NagarajaAI` e a cinemática de abertura** deixam de contornar a física
- **Máscara das relíquias** apertada de `~0` para `Enemy` — e isso **derrubou um teste que
  estava verde por acidente**: o rig criava o inimigo na camada 0 e só passava porque a máscara
  aceitava tudo. O rig foi corrigido, não a máscara
- **Anel de invocação dos esqueletos** vira elipse isométrica (era círculo de mundo, ou seja 3
  células de profundidade)
- **`EnemyCombat` passa a golpear com janela e direção** — o golpe do Cultista deixa de ser um
  teste de posição de um quadro; agora dá para esquivar no tempo, e estar atrás protege
- **Dois gatilhos órfãos do Byakhee** removidos
- **Buffer de interação** 8 → 16

## 8. Ferramentas deixadas para trás

| ferramenta | o que faz |
|---|---|
| `Tools/FavelaAmarela/Auditar Física 2D` | Varre prefabs + cenas do Build Settings e regenera os três relatórios |
| **Shift+F11** em jogo | Despeja a auditoria de colisores da cena carregada no console — vê o que só existe em Play (hurtbox criada em `Awake`, colisor desligado por i-frame, inimigo de spawner) |
| **F11** em jogo | `VisualizadorDeGolpes` — desenha hurtbox (verde), hurtbox em i-frame (amarelo), hitbox consultada (vermelho), gatilho (azul), pegada (cinza) |

A medição vive no **Runtime** (`AuditoriaDeColisores`, `AuditoriaDeGatilhos`) e o Editor a
consome. É de propósito: duas cópias da mesma conta divergiriam em silêncio, e a que ninguém
olhasse envelheceria errada.
