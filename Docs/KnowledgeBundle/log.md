---
type: Changelog
title: Log de Atualizações do Knowledge Bundle
description: Histórico cronológico de mudanças na base de conhecimento
---

# Log de Atualizações

## 2026-08-05 (3ª rodada) — Fragmento do Deserto inalcançável (mesma causa raiz da Cassilda)

Confirmado em playtest: cultistas caçando, Cassilda interagível e saída do Santuário todos
funcionais depois da 2ª rodada. Bug novo relatado: quest da Cassilda não fechava, "falta uma
parte das páginas perdidas".

Os 3 fragmentos ("páginas perdidas") estão espalhados um por cena: índice 0 no
`Deserto_Hali`, índices 1 e 2 no `Playtest_RuinasPalidas`. O Deserto tinha exatamente o
mesmo problema corrigido para o Santuário na rodada anterior — `DetectorDeInteracao` só
existia como override manual no Playtest, nunca ali —, só que ninguém tinha notado porque
o fragmento 0 é opcional para *começar* a quest (só bloqueia *terminar*). Como
`DetectorDeInteracao` já foi movido para o prefab do Damião na 2ª rodada, o Deserto herda o
componente automaticamente agora.

O que precisou de correção manual: o Deserto tinha um override próprio de
`EstadoPersistenteDoJogador` (para persistir a arma equipada entre cenas), que agora
duplicaria com a cópia recém-movida para o prefab. Override removido da cena
(`m_AddedComponents` limpo + bloco `MonoBehaviour` órfão apagado) — sem referências
externas ao fileID, confirmado por grep antes de apagar.

**Verificação:** compilação limpa, 349/349 testes EditMode. Validação manual (recolher o
fragmento 0 no Deserto e fechar a quest) pendente do Vini.

## 2026-08-05 (2ª rodada) — Combate jogável de ponta a ponta: IA surda, hitboxes e Cassilda inalcançável

Playtest depois do commit da manhã revelou quatro bugs reais que a auditoria estática não
pegava — todos exigiram instrumentação com log (`Debug.Log` condicional por flag de
Inspector) em vez de leitura de código, porque eram bugs de **dado em cena**, não de lógica
óbvia. Padrão que se repetiu 3 vezes: colisor/config muito maior ou deslocado do sprite
visual.

### IA: cultistas surdos por bug de timing na percepção sonora
`EnemyPerception.Update()` (roda todo frame, ~0,016s) resetava `_estaOuvindo` a cada frame,
mas o Damião só emite som a cada 0,15s (`PlayerMovement`). Saldo: 1 frame subindo suspeita
contra ~8 descendo — a suspeita nunca alcançava nem o limiar de Alerta. Trocado por uma
janela de memória (`MemoriaDoSom = 0.35s`) maior que o intervalo de emissão. Bug secundário
no mesmo arquivo: o reset dos limiares exigia `_jaEntrouCaca`, então um cultista que só
chegasse a Alerta (sem chegar a Caça) e esfriasse ficava com `_jaEntrouAlerta` travado em
`true` para sempre — surdo pelo resto da partida. Também corrigido: `EnemyStateMachine`
jogava o inimigo de `Attack` de volta para `Chase` a cada frame de recarga (toda vez que
`TentarAtacar()` retornava `false`, o que é quase sempre); agora só sai de `Attack` quando o
alvo realmente sai do alcance.

### Abdul Alhazred: pedra única no pé dele, hitbox no torso, colisores desproporcionais
`pontosDasPedras` no prefab tinha 1 elemento apontando pro próprio Transform do Abdul — só
1 pedra nascia, em cima dele, e `DefinirTotalDePedras(1)` fazia essa pedra valer por todas.
Array esvaziado para cair no fallback correto (4 pedras em diagonal); raio reduzido de 4,5
para 3,0 porque a 4,5 uma pedra caía quase em cima da saída da arena. A causa real de "sem
dano mesmo sem escudo" era a **hitbox do Abdul deslocada para o torso** (offset Y customizado
no `CircleCollider2D`) — confirmado só depois de instrumentar `ReceberGolpe` com log do
motivo da recusa; corrigido no Editor pelo Vini. Colisores da Pedra de Poder (0,70×0,90 pra
um sprite de 0,22×0,29) e do Esqueleto Invocado (1,30×1,70 pra um sprite de 0,42×0,54)
redimensionados para bater com o visual — o do esqueleto explicava "esqueletos não causam
dano": ele ficava empurrado para fora do próprio alcance de golpe (0,9) pelo colisor sólido
gigante. Escala visual da Pedra também aumentada (era ~40% do tamanho do Damião).

### Cassilda inalcançável: `DetectorDeInteracao` nunca existiu fora do Playtest
O componente que escuta a tecla E (`DetectorDeInteracao`) — e mais dois,
`EstadoPersistenteDoJogador` e `CongelamentoBridge` — tinham sido adicionados como **override
manual só na instância do Damião na cena de Playtest**, nunca no prefab. Fora dali (Santuário,
Deserto), Damião não tinha como interagir com nada. Provavelmente também a causa real do
"desarmado ao trocar de cena" investigado numa rodada anterior: sem
`EstadoPersistenteDoJogador`, a arma do baú não sobrevivia a uma transição. Os três
componentes movidos para `Player_Damiao.prefab`; overrides duplicados removidos da cena;
uma referência pendurada (`PromptDeInteracao.detector` apontava pro componente antigo
deletado) corrigida para o novo.

### Saída do Santuário: instrumentada, não resolvida
Reportado "não dá pra sair do templo" na mesma sessão. Auditoria do `PortalDeCena`
(`Saida_Santuario` → `Deserto_Hali`) não achou nada errado na configuração: trigger correto,
tamanho plausível, cena registrada em Build Settings, ponto de chegada (`VoltaDoSantuario`)
existe do outro lado, sem gating, matriz de colisão de física sem bloqueio. Como os bugs
anteriores desta rodada eram todos espaciais e invisíveis por grep, `OnTriggerEnter2D` ganhou
log incondicional (antes de qualquer filtro de tag/carência) em vez de mais uma correção às
cegas — pendente de teste em Play Mode para saber se é bloqueio físico (level design) ou algo
depois do log.

**Verificação:** compilação limpa, 349/349 testes EditMode. Luta do Abdul confirmada
vencível pelo Vini em playtest manual.

## 2026-08-05 — Destrava compilação e religa o sistema de itens (Blocos 0-2 do plano "Rumo a Carcosa")

Auditoria completa do estado real do projeto antes de planejar o restante do jogo (Templo,
Castelo, bosses finais). Corrigiu três premissas erradas do resumo da sessão anterior: o
projeto **não compilava**, o `TutorialHintUI` nunca sumiu das cenas (o problema era outro),
e a injeção de som na IA já tinha sido corrigida em disco — só faltava commitar. Achado mais
grave, fora de qualquer lista: `ItemDatabase.GarantirInstancia()` era método morto (sem
`RuntimeInitializeOnLoadMethod`), então todo item do jogo resolvia `null` em runtime.

### Bloco 0 — Compilação
- `InventoryManager.GetEfeitoDaArmaEquipada()` removido (lia `ItemDef.EfeitoDeCombate`,
  campo que não existe mais desde a migração para `WeaponFactory`).
- `MaoFisicaBridge`: `inv.Equipamentos` (nunca existiu) → `inv.Equipment`.
- `MaoFisicaBridge.TryAtacar` passou a usar `_armaEquipada.Execute()` como fonte do golpe,
  igual a `TryUsarHabilidade` — parava de depender do método morto acima.
- `SorteioDeArmaDaTumbaTests.cs` removido (referenciava classes já deletadas).

### Bloco 1 — Sistema de itens
- `ItemDatabase.GarantirInstancia()` ganhou `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`.
- Os 3 `ItemDef` das armas da Tumba corrigidos: `Tipo` estava `Armadura` (1) em vez de
  `Arma` (0); `ArmaFisica` nunca tinha sido preenchido (resolvia `MaoVazia` sempre).
- `Item_PatuaDasLuasGemeas.asset` recriado como `ItemDef` válido — serializava a classe
  `ItemConfig`, já deletada (Missing Script), então a quest da Cassilda não entregava nada
  ao fim. GUID preservado (o prefab da Cassilda já apontava para ele).
- `BauDaTumba` na cena de playtest populado com as 3 armas (array estava com 3 `fileID: 0`).
- `MaoFisicaBridge.VerificarSlotDeArma` passou a usar a sobrecarga `EquiparArma(TipoArmaFisica)`
  (preserva o id para o save) e a desequipar corretamente para mão vazia.

### Bloco 2 — Higiene
- Removidas duas rotinas `[InitializeOnLoad]` (`AutoFixAndTestRunner.cs`,
  `AutoFixArchitecture.cs`) que rodavam sozinhas ao abrir o Editor e reescreviam
  `BauDaTumba.armasPossiveis` com armas fictícias fora do lore ("Lâmina Enferrujada" etc.),
  junto dos assets fictícios (`Assets/Resources/Itens/Armas/*`) e de duplicatas órfãs dos
  `ItemDef` de arma no caminho antigo (`Config/Itens/`, sem `Resources`).
- Comentário obsoleto em `GameManager.cs` corrigido (citava `CultistaAI.OnEnable()`; quem
  recebe o som hoje é `EnemyPerception`).

### Commit único preservando trabalho de sessão paralela
O commit também incluiu sistemas que só existiam em disco, sem nenhum commit — risco real
de perda: a refatoração da IA de inimigos por composição (`EnemyBase` + `Enemies/Components`),
o inventário novo inteiro, Vigor/Estamina, e o Labirinto de Carcosa (árvore de progressão) +
níveis de personagem. Decisão de 2026-08-05: Vigor fica ativo (`PlayerMovement`/`EsquivaBridge`
já dependem dele); Labirinto e níveis ficam **congelados** — compilam, mas não são
instanciados em nenhuma cena do Vertical Slice.

**Verificação:** compilação limpa (0 erros/warnings), 349/349 testes EditMode passando.
Falta validação manual em Play Mode (abrir o baú, entregar os 3 fragmentos à Cassilda) —
não automatizável pela ponte MCP atual.

Plano completo em `C:\Users\Vini\.claude\plans\planejamento-macro-rumo-fluttering-parrot.md`.

## 2026-08-02 (7ª rodada) — Chão do Santuário vira Tilemap isométrico de losango

O que parecia "grid errado" (relatado como "voltou pra visão topdown") era, na
investigação anterior, descartado como só o grid de referência da janela Scene. Certo
em parte: a câmera e os dados da cena estavam corretos — mas o piso do Santuário nunca
tinha sido construído como Tilemap isométrico, era um `SpriteRenderer` retangular de cor
lisa desde sempre. Sem textura nenhuma, um retângulo liso não dá pista de ângulo — por
isso "parecia" topdown mesmo estando tudo certo por baixo.

### A receita real (confirmada na cena, não em código desatualizado)
Antes de construir, comparei o Deserto e a Tumba: `Grid` com `cellSize (1, 0.5, 1)` e
`cellLayout Isometric`. Achado no caminho: `BuildDesertTilemap.cs` (ferramenta antiga)
tem código que cria um Grid **retangular** (`cellSize 1×1`) — não bate com o que está de
fato salvo nas duas cenas. A cena é a fonte da verdade; o código da ferramenta ficou
para trás numa correção manual que ninguém propagou de volta ao script.

### `BuildSantuarioIsoFloor` (novo)
Mesma receita do Deserto/Tumba, aplicada ao Santuário: `SantuarioFloorGrid` isométrico,
`SantuarioFloor` (Tilemap de chão, sortingOrder -1000) e `Colisao` (Tilemap com
`TilemapCollider2D` na layer Obstacle, pintado nas células de borda 8-adjacentes ao
chão — mesmo algoritmo de `BuildIsoCollisionFromFloor.cs`, reaproveitando o tile de
colisão invisível já existente no projeto). Tile de piso é um losango de cor sólida
gerado por código (mesma paleta "calcário frio" do piso antigo) — segue sem arte real,
só a geometria virou isométrica de verdade.

**Erro cometido na primeira tentativa, corrigido antes de fechar:** calculei a
meia-altura do losango errado (achei que N=14 células dava meia-altura 7; a fórmula real
é `N/4`, dava 3,5) — a saída da cena (`Saida_Santuario`, y=-4,8) ficava fora do losango
inteiro, na ponta sem área nenhuma. Corrigido para N=28 (meia-altura 7), com folga sobre
o marco mais distante.

`Piso` e as 4 `Parede_*` antigas foram desativadas (não deletadas — reversível).

### Documentação nova, a pedido do Vini
`systems/tilemap_isometrico_losango.md` — a receita completa (Grid, chão, colisão de
borda, a matemática do tamanho e o erro acima como exemplo de onde é fácil errar),
escrita para ser seguida por qualquer ferramenta que edite a cena da Unity, não só por
mim — o pedido explícito foi para poder repetir isso pelo Antigravity também.

QA: 377/377 testes EditMode verdes (Runtime/Editor puro, nenhum POCO novo).

## 2026-08-02 (6ª rodada) — Script órfão no prefab do jogador

**Achado em playtest:** console mostrando "The referenced script on this Behaviour (Game
Object 'Player_Damiao') is missing!" ao entrar no Santuário. Investigado por eliminação:
o prefab (`Player_Damiao.prefab`) tinha um `MonoBehaviour` cujo guid de script
(`8d6086cf93599344cb7badf04101ab15`) não resolve a nenhum arquivo `.cs` em `Assets/` nem
`Packages/`. O `m_EditorClassIdentifier` ainda preservado na serialização apontava para
`FavelaAmarela.Player.AnomalyPowerBridge` — resto da remoção do Salto Dimensional (a
habilidade saiu, o componente ficou preso no prefab do jogador). Sem histórico de deleção
no git (nunca chegou a ser commitado). Afeta **toda** cena que usa o prefab — Deserto,
Tumba e Santuário —, não é específico de nenhuma delas.

Corrigido com `RemoverScriptOrfaoDoJogador` (novo, `Tools/FavelaAmarela/Remover script
orfao do prefab do jogador`), via `PrefabUtility.EditPrefabContentsScope` +
`GameObjectUtility.RemoveMonoBehavioursWithMissingScript` — edita o asset do prefab
diretamente, não uma instância de cena. Prefab foi de 7 para 6 `MonoBehaviour`s.

### Barra de Vitalidade usava sprite genérico da Unity
No mesmo print que mostrou o script órfão, a barra de vida aparecia como um retângulo
arredondado — o `UISprite` embutido da Unity — bem diferente da barra de Resiliência, que
usa pixel art real (`bar_background.png`/`bar_fill.png`). Pedido do Vini: "quero a barra de
vida igual da barra de resiliência".

`Trilho` e `Preenchimento` (filhos de `Barra_Vitalidade` no `HUD_ResilienciaBar.prefab`)
passaram a usar os mesmos sprites reais da barra de Resiliência — cor (vermelho) mantida,
só a moldura/forma mudou. Via `PrefabUtility.EditPrefabContentsScope`, o mesmo padrão do
conserto acima.

> **Nota de processo:** o primeiro `.cs` desta correção foi criado como arquivo novo e a
> Unity não chegou a indexá-lo a tempo (sem `.meta` gerado, o menu nunca registrou —
> mesma armadilha já documentada em `unity-para-de-indexar-cs-novos`). Consolidado dentro
> de `CorrigirSpritesDoHUD.cs`, um arquivo que a Unity já rastreava, em vez de depender de
> reiniciar o Editor.

QA: 377/377 testes EditMode verdes nas duas correções (nada de Core mudou em nenhuma).

## 2026-08-02 (5ª rodada) — Yug-Neth ficava para trás ao trocar de cena

**Bug de playtest relatado pelo Vini:** "o Mi-Go não sai da masmorra com o jogador".

**Causa:** `PortalDeCena.OnTriggerEnter2D` usa `SceneManager.LoadScene` não-aditivo — a
cena de origem é destruída inteira. Yug-Neth é um `GameObject` só daquela cena, sem
`DontDestroyOnLoad`; nem o próprio Damião sobrevive fisicamente à troca (é reconstruído do
zero em cada cena nova), só o estado dele atravessa via `GerenciadorDeSave`. Não havia
nenhum mecanismo trazendo Yug-Neth de volta na cena seguinte.

**Decisão de arquitetura (discutida com o Vini):** avaliadas duas saídas — recriar
Yug-Neth por chave de save (mesmo padrão do resto do jogo) vs. torná-lo `DontDestroyOnLoad`.
Ficou com a primeira: consistência com como tudo mais atravessa cena (Damião, arma,
Vitalidade, progresso de quest) pesa mais que economizar uma pequena peça de wiring, e
`DontDestroyOnLoad` deixaria Yug-Neth como o único objeto de gameplay com esse tratamento
especial — mais superfície de manutenção, não menos.

### `TravessiaDoCompanheiro` (novo, Runtime `GameLoop`)
No `Start()` (ordem de execução +100, depois de `PontoDeChegada` reposicionar o jogador):
se `ChavesDeSave.AbdulResolvido` já tem valor (ele foi libertado, por qualquer um dos dois
desfechos da conversa com Abdul) e nenhum `YugNethAI` existe ainda na cena, instancia o
prefab perto do jogador, chama `Bind()` + `IgnorarColisaoCom()`, e registra no
`GameManager` local. Deriva de `AbdulResolvido` em vez de gravar uma chave própria — mesma
escolha que `ChavesDeSave.YugNethLibertado` já documentava. Colocado no Deserto e no
Santuário (`Tools/FavelaAmarela/Montar travessia de cena do Yug-Neth`) — os dois destinos
alcançáveis a partir da Tumba hoje.

### `EstadoPersistenteDoCompanheiro` (novo, Runtime `Persistencia`, no prefab do Yug-Neth)
Fecha de graça um gap que a primeira versão desta correção teria deixado: sem isto, cada
travessia o recriava com Vitalidade cheia, perdendo dano sofrido e até a incapacitação. Uma
chave nova (`ChavesDeSave.YugNethVitalidadeAtual`) mais o mesmo padrão de
`EstadoPersistenteDoJogador` (registra no `Start`, aplica a diferença via `Ferir`). A
incapacitação **não ganhou chave própria**: ela só acontece quando a Vitalidade chega a
zero, e restaurar o valor salvo como zero dispara `VitalidadeBridge.OnAbatido` pelo caminho
normal — `YugNethAI.HandleAbatido` cuida do resto sozinho, sem duplicar a fonte da verdade.

`MontarInteracaoEDialogoAbdul.ObterOuCriarPrefabYugNeth` ganhou um passo idempotente
(`GarantirEstadoPersistente`, via `PrefabUtility.EditPrefabContentsScope`) para adicionar o
componente ao prefab já existente, não só a prefabs novos.

**Pendência que esta correção não fecha:** nada impede o jogador de atravessar um portal
com Yug-Neth incapacitado na cena que está deixando — ele reaparece corretamente
incapacitado na cena seguinte (Vitalidade salva em zero), mas não há uma `TrancaDeArena` no
portal de saída da Tumba condicionada a ele estar de pé.

QA: 377/377 testes EditMode verdes (nenhum POCO novo — tudo Runtime/Editor, mesmo padrão
de `EstadoPersistenteDoJogador`, que também não tem teste Core). Verificado via MCP: o
prefab `YugNeth.prefab` ganhou `EstadoPersistenteDoCompanheiro`, a instância cativa na
Tumba herdou o componente, e `Travessia_YugNeth` está configurado e salvo no Deserto e no
Santuário com o prefab corretamente atribuído.

## 2026-08-02 (4ª rodada) — Ramificação A/B/C do primeiro encontro com Cassilda

O roteiro do lore sempre teve 3 respostas possíveis à saudação de Cassilda ("Onde estou?" /
"Você está presa aqui?" / silêncio), mas nunca tinha sido ligada — a saudação e o pedido
saíam concatenados numa fala só, sem escolha. Ligado agora, reaproveitando o
`PainelDeEscolha` que a rodada anterior colocou no Santuário para o recital.

`CassildaNPC.InteragirPrimeiroEncontro` segue o mesmo ritmo de fala-por-aperto do resto do
NPC: primeiro aperto mostra a saudação e abre o painel na mesma hora (mesmo padrão de
sobreposição já usado no recital e na conversa do Abdul); o jogador escolhe, Cassilda reage
(`reacoesDoPrimeiroEncontro`, mesma ordem das opções); no aperto seguinte ela faz o pedido
da quest e só aí `CancaoIncompleta.Iniciar()` roda de fato — o estado da quest fica
`NaoIniciada` durante toda a troca, não só na primeira fala.

**Puramente cosmético**, como o resto do roteiro nesse ponto: a opção escolhida não é
salva, só muda a reação. Sem `painelDeEscolha` atribuído, não trava o jogo — a escolha vira
decoração perdida e o encontro segue no mesmo ritmo de 2 apertos.

Nenhuma peça de Core mudou (é só orquestração de fala em `CassildaNPC`), então os 377/377
testes EditMode continuam de pé sem alteração. `MontarPrefabDaCassilda` ganhou os dois
arrays novos (`opcoesDoPrimeiroEncontro`, `reacoesDoPrimeiroEncontro`); reexecutado — a
instância em cena herdou o conteúdo automaticamente do prefab, sem precisar salvar a cena
de novo. Atualizados `systems/quest_cassilda.md` e `roadmap_vertical_slice.md` (item 8 sem
mais pendência de ramificação — só falta arte).

## 2026-08-02 (3ª rodada) — Enigma da Canção de Cassilda e prefab da rainha

### Core: o recital final
Novo `RecitalDaCancao` (POCO, `Core/Quests`, 9 testes): guarda quantas estrofes o recital
cobra, qual opção fecha cada uma e quantos erros o jogador acumulou. `Responder` avança na
acerto, não avança no erro — **nunca volta para uma estrofe já fechada**, é a regra que
protege o desenho sem punição decidido pelo Vini.

`CancaoIncompleta` ganhou o estado `EstadoDaQuest.Recitando`: entregar o 3º fragmento não
conclui mais a quest sozinho, só quando o `Recital` interno também estiver completo.
Construtor aceita `params int[]` com as respostas certas — vazio mantém o comportamento
antigo (recital nasce `Completo`), o que preserva os 13 testes já existentes sem alteração.

### Runtime: Cassilda vira prefab, fluxo de fala por aperto
`CassildaNPC` reescrito para conduzir o recital no mesmo ritmo já usado na conversa com
Abdul (`AbdulAlhazredAI`): cada aperto de **E** avança uma fala — abertura, recapitulação
das 2 estrofes conhecidas, pergunta da 3ª, pergunta da 4ª — e a última pergunta abre um
`PainelDeEscolha` com 3 opções. Errar mostra a reação fria dela e reabre a mesma pergunta
no próximo aperto; sem estado persistido — sair do Santuário no meio reseta o recital.

Cassilda deixou de ser um `GameObject` solto remontado por duas ferramentas de Editor
diferentes (risco real de conteúdo divergente) e virou
`Assets/FavelaAmarela/Art/Characters/Cassilda/Cassilda.prefab`
(`MontarPrefabDaCassilda`, nova ferramenta), com todo o conteúdo textual — saudação,
pedido, falas por fragmento, falas/perguntas/opções do recital. `caixaDeTexto` e
`painelDeEscolha` ficam de fora do prefab de propósito: são referências de cena, e um
asset de prefab não pode apontar para um objeto de cena — essas duas são ligadas por
`MontarSantuarioDeYhtill`, que também passou a criar o `PainelDeEscolha` do recital dentro
do HUD do Santuário (não existia antes).

`MontarSantuarioDeYhtill` e `MontarCenaDoSantuario` ganharam autocorreção: encontrando uma
Cassilda que não é instância do prefab (a antiga, solta), destroem e reinstanciam a partir
do asset — sem isso, reexecutar as ferramentas continuaria reaproveitando o objeto velho
com conteúdo desatualizado. Rodado e verificado: a Cassilda solta da cena foi substituída
pela instância do prefab, `PainelDeEscolha` criado sob o HUD com `PlayerInput` e
`PlayerMovement` do Damião ligados, cena salva.

### Conteúdo: o poema nos fragmentos
As duas primeiras estrofes da Canção de Cassilda (poema de Robert W. Chambers) foram
distribuídas nos 3 diários já existentes (Seraphel, Morthis, Vaine) — decisão do Vini de
manter a quest em 3 fragmentos, não expandir para 4 ou 5. O fragmento da Vaine planta o
epíteto "Perdida Carcosa", que é a pista real por trás da resposta certa da 4ª estrofe no
recital. As opções erradas das duas perguntas erram por **tom** (grandiosidade heroica,
esperança), nunca por um detalhe decorável — quem leu os fragmentos tem vantagem real, não
precisa ter memorizado.

### Testes e documentação
377/377 testes EditMode passando (9 novos de `RecitalDaCancao`, resto sem regressão).
Atualizados
`systems/quest_cassilda.md` (recital, prefab, escopo), `lore/cassilda_e_byakhee.md` (nota
de divergência do design original de 5 fragmentos) e `roadmap_vertical_slice.md` (item 8
de "jogável com ressalvas" para "jogável de ponta a ponta").

## 2026-08-02 (2ª rodada) — Barra de itens com teclas 1–8 e rajadas de tempestade

### Barra de ações vira barra de itens
Pedido do Vini: **retangular, mais transparente, ocupando menos tela, e com utilidade**. A
barra antiga só *mostrava* arma e habilidade — era painel informativo.

Novo `BarraDeItens`: as **8 posições do inventário** na tela, acionáveis pelas **teclas 1–8**
(`InventarioBridge.Usar`). Faixa de ~7% da altura × 42% da largura, fundo a 35% de opacidade,
slot vazio quase invisível (25%). O número da tecla aparece no canto de cada slot — ensina o
atalho sem tutorial.

As 8 teclas casam com `Inventario.PosicoesPadrao`: uma tecla por posição, sem paginação.
Mesmo motivo de o inventário ser enxuto — o jogador deve saber de cor o que tem.

> **Input por `Keyboard.current`**, não pelo asset de ações: são 8 atalhos fixos de UI, e
> criar 8 `InputAction` no asset só se paga quando existir remapeamento. Continua sendo o
> Input System novo, nunca o `Input` legado.

### Tempestade: cobertura total + rajadas aleatórias
**Correção de rumo minha.** Quando o Vini escreveu *"não cobre o mapa todo"*, ele estava
**relatando que a tempestade não cobria** — eu li como pedido para não cobrir e fiz uma
vinheta (centro limpo). Errado. A vinheta foi descartada.

Agora `Areia_Tempestade.png`: ruído alongado na horizontal (areia varrida pelo vento, não
estática), alpha 0,62–1,0 — **cobre a tela inteira**, com variação de *textura*, não de
cobertura.

**Rajadas** ligadas ao `TempestadeAmbiente`, usando o `AgendadorDeRajada` — POCO que existia
no projeto e **nunca tinha sido usado por ninguém**. Duas camadas agora:

| Camada | O que faz |
|---|---|
| Oscilador | respira dentro da faixa do setor (ondulação constante) |
| Rajada | soma um pico por cima, em intervalos aleatórios (8–20 s, 4 s de duração) |

A rajada **soma** à faixa em vez de substituí-la: funciona em qualquer setor sem brigar com
o `TempestadeZonaTrigger`, que é quem define a base. Numa zona calma vira lufada; no leste,
apagão. Subida/descida interpolada em 1,2 s — sem isso a rajada "liga" de estalo e lê como
bug de render, não como vento.

QA: 0 erros, **363/363 testes EditMode verdes**. Três cenas verificadas no YAML.

## 2026-08-02 — Quest de Cassilda fechada, Santuário vira cena, e a vinheta da tempestade

### Quest fechada: o Patuá existe
`Item_PatuaDasLuasGemeas.asset` + `Patua_DasLuasGemeas.prefab`, ligados no `prefabPatua` da
Cassilda. Antes, concluir a quest mostrava a fala final e **não entregava nada**.

Criado também o **`ColetavelDeItem`** — coletável genérico que põe o item no inventário, em
vez de mais um script por relíquia (`PatuaPickup`, `NecronomiconPickup`…). **Inventário
cheio não some com o item**: ele fica no chão e avisa, porque perder relíquia por falta de
espaço seria perda silenciosa de progresso.

### Santuário de Yhtill virou cena própria
Decisão do Vini: `Santuario_Yhtill.unity`, com portal de ida e volta como a Tumba. Cassilda
e o Refúgio **mudaram-se para dentro**; o marco no Deserto virou só a porta. Registrada em
Build Settings (sem isso o `LoadScene` por nome falharia em runtime).

**Calmaria** implementada com `TempestadeAmbiente` de faixa **0–0**, não com ausência de
driver: sem driver, o `EnvironmentState` fica no valor inicial dele (0,3) e o Santuário
teria tempestade fraca **por acidente**.

> **Armadilha registrada:** mover um NPC de cena **não carrega a configuração**. A Cassilda
> do Santuário é objeto novo; a do Deserto foi removida com o Patuá e as falas por fragmento
> junto. As ferramentas tiveram de ser reapontadas e reexecutadas.

**Corrigida uma mentira no texto:** a fala final dizia *"Cinco nomes"* — resquício do design
de 5 fragmentos, sendo que a quest tem 3. Reescrita sem número, para não mentir de novo
quando os fragmentos 4 e 5 voltarem com o Templo da Serpente.

### Tempestade: de filtro chapado para vinheta
Relatado pelo Vini: *"não cobre o mapa todo"*. O véu era uma `Image` de cor sólida esticada
na tela inteira — ao subir a intensidade, tingia tudo por igual e lia-se como filtro por
cima do jogo, escondendo justamente o chão que o jogador precisa ver.

Gerada `Vinheta_Tempestade.png` (512², degradê radial por *smoothstep*, sem anel duro):
centro limpo até 35% do raio, opaco na borda. Agora "visibilidade reduzida" significa o que
deveria — você enxerga perto de si e perde o horizonte, casando com a tabela de visibilidade
do design §3. `alphaMaximo` subiu de 0,5 para 0,85, já que a opacidade se concentra nas
bordas.

> `preserveAspect` fica **false** de propósito: preservando o aspecto, sobrariam cantos sem
> véu numa tela widescreen — exatamente onde a tempestade deveria ser mais densa.

QA: 0 erros, **363/363 testes EditMode verdes**. Cenas verificadas no YAML.

## 2026-08-01 (6ª rodada) — Softlock do Abdul, o jogo deixa de ser mudo, e a causa dos travamentos

### Softlock na luta do Abdul (crítico)
Relatado pelo Vini jogando: *"depois da quarta pedra não dá para matá-lo"*. Era **bug real e
fatal**:

```
Quebra Pedra → escudo cai por 10s → escudo VOLTA
Quebra a última → não há mais o que quebrar → escudo volta e nunca mais cai
                → se Abdul ainda estiver acima de 35%, a luta é IMPOSSÍVEL de terminar
```

**Fix:** a última Pedra derruba o escudo **de vez**. Segue o que o design já dizia
("escudo sustentado pelas Pedras de Poder") — as Pedras são as âncoras; sem nenhuma de pé,
não há escudo. Com Pedras sobrando a janela continua temporária, que é o que dá tensão à
Fase 1. `AbdulFSM` ganhou `DefinirTotalDePedras` + `EscudoDestruido`; o adaptador informa
quantas nasceram. **3 testes novos**, incluindo degradação graciosa (sem total informado,
mantém o comportamento antigo em vez de derrubar o escudo cedo).

### Pedras "não apareciam direito"
O losango antigo punha duas Pedras nos **eixos** — e como o eixo Y é comprimido pela
perspectiva isométrica, elas nasciam quase em cima do Abdul. Trocado para as quatro
**diagonais** na proporção isométrica 2:1.

### Esqueletos aumentados
Estavam em escala 0,5 × 0,8 contra 1,8 × 1,8 do Cultista — mais de 3× menores que um inimigo
comum. Agora 1,3 × 1,7: silhueta mais estreita e alta que a do Cultista (leitura esquelética,
e claramente um lacaio). O collider é local, então acompanhou sozinho.

### Entradas do Deserto reduzidas para 4 unidades
Estavam em 6 un e dominavam o terreno — os marcos ficam ~12 un um do outro. Regeneradas a
partir dos recortes em resolução cheia **mantendo os GUIDs**, então nenhuma referência de
cena precisou ser religada.

### O jogo inteiro estava mudo
O `TutorialHintUI` **não estava em cena nenhuma**. Baú, patuá, Necronomicon, Refúgio,
Cassilda e fragmentos falavam para o vazio. Agora a caixa existe nas duas cenas e está ligada
em todos. Também instalado o **HUD no Deserto**, que não tinha nenhum.

### A causa dos travamentos da sessão (era minha, não do MCP)
Por horas os travamentos pareceram instabilidade da ponte MCP. **Não eram.** As ferramentas
chamavam `EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()`, que abre um diálogo
**modal**. Disparada pela ponte, a Unity trava inteira esperando um clique que ninguém vê.
Substituído por salvamento silencioso nas 8 ferramentas.

> **Segunda armadilha, também minha:** `AssetDatabase.GetBuiltinExtraResource<Font>("Arial.ttf")`
> **lança exceção** na Unity 6. O projeto já tinha resolvido isso em `DanoFlutuante.cs` (é
> `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`) e tem até `FonteBuiltinTests`
> cobrindo. Eu não consultei antes de escrever.

QA: 0 erros, **363/363 testes EditMode verdes**. Cenas verificadas no YAML.

## 2026-08-01 (5ª rodada) — Armas viram itens e quest de Cassilda

### Armas são itens (decisão do Vini)
O Vini perguntou se armas não deveriam ir para o inventário — e a pergunta expôs uma
inconsistência real: havia **dois sistemas paralelos** que não se falavam (o slot único da
Mão Física e o inventário). O design já pressupunha a unificação: `systems/abilities.md` diz
que trocar o que está empunhado **só pode ser feito sob a luz de um Refúgio**, o que exige
haver onde guardar a arma fora de uso.

- `DefinicaoDeItem` ganhou `ArmaEquipavel` (+ `EhEquipavel`). **Arma nunca empilha** (duas
  iguais numa pilha seriam indistinguíveis) e **não é consumida ao ser usada** — empunhar
  muda de estado, não gasta.
- `InventarioBridge.Usar` intercepta armas e empunha em vez de consumir.
- `BauDaTumba` agora **guarda a arma no inventário além de empunhá-la**. Se não couber, ela
  continua empunhada — perder a arma do baú por inventário cheio seria pior que a
  inconsistência.
- `ItemConfig` ganhou os campos de arma. **6 testes novos.**

### Quest de Cassilda — "A Canção Incompleta"
`CancaoIncompleta` (Core, **13 testes**), `FragmentoDeYhtill` e `CassildaNPC`. Cassilda no
Santuário do Deserto, 3 fragmentos distribuídos entre Deserto e Tumba. Ver
[quest_cassilda.md](systems/quest_cassilda.md).

**Reduzida de 5 para 3 fragmentos** (decisão do Vini): os de nº 4 e 5 ficam no Templo da
Serpente, dungeon que não existe. Com 5, a quest seria impossível de fechar no VS.

O progresso atravessa as **duas cenas** por chaves de save separadas para *recolhido* e
*entregue* — voltar ao Santuário depois da Tumba encontra a quest onde estava.

Regras testadas que protegem o progresso: entregar o mesmo fragmento duas vezes não conta;
`Concluir` exige todos; concluir de novo não dá o Patuá outra vez; entregar inicia a quest
implicitamente (o jogador pode achar uma página antes de falar com ela).

### Descoberta grave: o jogo inteiro estava mudo
O `TutorialHintUI` — a caixa que mostra texto ao jogador — **não estava em cena nenhuma do
projeto**. Baú, patuá, Necronomicon, Refúgio, Cassilda e fragmentos todos tinham o campo
vazio: a mecânica rodava e o jogador não via texto nenhum. Criada a ferramenta
`Montar caixa de dialogo nas cenas`, que monta a caixa e liga em todos.

> ⚠️ **Não executada com sucesso ainda** — o Editor travou num diálogo modal
> (`SaveCurrentModifiedScenesIfUserWantsTo` abre prompt quando há cena suja). Enquanto não
> rodar, a quest e todos os textos do jogo continuam mudos.

QA: 0 erros de compilação, **360/360 testes EditMode verdes** (341 + 13 da quest + 6 das
armas). Cassilda e os 3 fragmentos verificados no YAML das duas cenas.

## 2026-08-01 (4ª rodada) — Setores do Deserto, inventário e Refúgios de Luz

Três itens do roadmap na sequência pedida pelo Vini: **item 4 → item 2 → Refúgio**.

### Item 4 — Setores do Deserto
Os marcos **já estavam** nas posições certas da topologia; o que faltava eram os setores
como **entidades de jogo**. Criados 6 volumes de `TempestadeZonaTrigger` ladrilhando o mapa
(x ∈ [−21,5; 21,5], y ∈ [−15,5; 15,5]) com as faixas da tabela §3 do design — intensidade
≈ 1 − visibilidade. **Sem sobreposição**, porque o trigger age no `OnTriggerEnter2D` e
volumes sobrepostos fariam o resultado depender da ordem de entrada. Terreno **não** foi
regerado. Ver [level_design_deserto_hali.md](systems/level_design_deserto_hali.md) §7.1.

Duas divergências do doc, ambas registradas: ele diz "5 setores" (§1.3) mas a tabela §3
lista 6 (implementei as 6, que têm números acionáveis); e previa ~22×16 un, mas a cena tem
43×31 (não redimensionei — mexer no terreno seria destrutivo).

### Item 2 — Inventário
Core POCO com **21 testes**: `DefinicaoDeItem`, `PilhaDeItens`, `Inventario`, `EfeitoDeUso`.
Runtime: `ItemConfig` (autoria em asset) e `InventarioBridge` (aplica o efeito no mundo).
Enxuto por decisão de design — 8 posições, sem peso, sem categorias. Ver
[inventario_e_consumiveis.md](systems/inventario_e_consumiveis.md).

O Core **não sabe** o que é Vitalidade nem Resiliência: devolve um `EfeitoDeUso` e a ponte
decide onde aplicar. É o que mantém o inventário testável sem a Unity.

Regras que evitam bug silencioso, todas com teste: completa pilhas antes de abrir posição
nova; `Remover` nunca retira parcialmente; relíquia não some ao ser "usada"; e **usar só
consome se o efeito tiver onde agir** (uma Ancoragem sem Resiliência injetada gastaria o
item à toa).

### Refúgios de Luz — e o save finalmente grava
O `RefugioDeLuz` existia em código mas **não estava em cena nenhuma**. Colocados os 3 que o
design especifica (Entrada, Santuário, Portões), com trigger circular e tag `PontoDeLuz`.

Ele cresceu de "reanima Yug-Neth" para as três funções que o design pedia: **Ancoragem**
(devolve RM, com intervalo para não virar farm de cura), **reanimar Yug-Neth** (sem
intervalo — é estado, não recurso) e **salvar em disco**. Este último fecha um laço que
estava aberto: o `GerenciadorDeSave` tinha `GravarEmDisco()` pronto e **ninguém chamava**,
então fechar o jogo perdia tudo.

> **Ainda falta ler:** `CarregarDoDisco()` continua sem chamador. O jogo grava mas não lê.

### Gotcha caro: a Unity parou de indexar `.cs` novos
`Inventario.cs` e `PilhaDeItens.cs` eram **ignorados em silêncio** — `CS0246` nos testes
como se não existissem, enquanto `DefinicaoDeItem.cs` (mesma pasta, mesmo namespace)
compilava. Diagnóstico decisivo: **erro de sintaxe proposital não gerou erro nenhum**,
provando que a Unity nem lia o arquivo.

Descartados sem sucesso: sintaxe, BOM, namespace, cobertura do `.asmdef`, `.meta` da pasta,
colisão de GUID, 4× `Assets/Refresh`, recriação em caminho novo e rewrite do `.asmdef`.
Contornado consolidando o domínio de itens em `DefinicaoDeItem.cs`, com nota no topo do
arquivo. **Separar depois de um restart do Editor** — é só mover os blocos.

QA: 0 erros de compilação, **341/341 testes EditMode verdes** (320 + 21 do inventário).
Cenas verificadas no YAML.

## 2026-08-01 (3ª rodada) — Tempestade ligada ao Deserto + entradas em cena

### Tempestade de Memória (item 5 do VS)
A infra existia desde a demo das Ruínas mas **nunca tinha sido instalada no Deserto** — a
tempestade simplesmente não acontecia lá. Instalados via
`Tools/FavelaAmarela/Montar Deserto de Hali`:
- `TempestadeAmbiente` (driver) — o `GameManager` já o encontra e injeta o `EnvironmentState`.
- `Canvas_Deserto` + `Veu_Tempestade` + `TempestadeVisualOverlay` — **o Deserto não tinha
  Canvas nenhum**. Sem o véu a tempestade seria invisível, e a mecânica depende de o jogador
  *perceber* a rajada para aproveitá-la.

> **Metade do item já estava pronta sem ninguém notar:** o acoplamento
> *intensidade→detecção* funciona desde sempre, porque a percepção do Cultista é 100% sonora
> e `PlayerStealthState.AplicarAbafamentoTempestade` já reduz o raio de ruído do Damião
> conforme a tempestade. O *stealth invertido* que o roadmap pedia é exatamente isso.
> *Intensidade→velocidade* foi **descartada** por decisão do Vini (a tempestade atrapalha só
> os inimigos, não o jogador).

**Falta:** variar a faixa por setor (`TempestadeZonaTrigger`) — depende do item 4 (topologia
de 5 setores), então hoje o Deserto inteiro usa a faixa padrão do componente.

### Entradas das localizações em cena
Os 5 sprites fatiados na rodada anterior foram ligados às localizações que **já existiam**
no `Deserto_Hali`. Ver [entradas_do_deserto.md](systems/entradas_do_deserto.md).

Duas armadilhas: as localizações já tinham o **quadrado placeholder** da Unity (logo, "só
atribui se vazio" não substituiria nada) e usavam **tint** para se diferenciar (que tingiria
a arte nova se não voltasse a branco). Cada uma ganhou `DynamicYSort`.

### Fix: Yug-Neth renderizava 5× menor
`yug_neth_idle.png` estava com **PPU 160** enquanto a convenção do projeto é 32. O sprite
(40×50 px) media **0,25 × 0,31 unidades** — um quarto de tile, quase invisível ao lado do
Damião. Corrigido para PPU 32: agora **1,25 × 1,56 un**.

> Uma memória do projeto afirmava que esse sprite tinha sido "verificado, PPU 32, nada a
> corrigir". Estava errada — o arquivo sempre esteve em 160.

QA: 0 erros de compilação, **320/320 testes EditMode verdes**. Cenas verificadas no YAML.

## 2026-08-01 (2ª rodada) — Sprites das 5 entradas do Deserto

Fatiada a folha 2048×2048 com os dioramas isométricos das entradas das localizações do
Deserto de Hali. Detalhes completos em [entradas_do_deserto.md](systems/entradas_do_deserto.md).

**Fatiamento automatizado** (reprodutível, não recorte manual): preenchimento a partir das
bordas por luminância → preenchimento de buracos → componentes conexos. Um limiar de cor
simples **não funcionaria** — partes da arte (a pedra do Santuário) têm exatamente a cor do
fundo; o que as distingue é estarem cercadas. Os rótulos de texto caem fora da caixa de todos
os dioramas e foram excluídos sem intervenção.

**Escala:** o original dava ~1000 px por diorama = **31 unidades de mundo** a PPU 32, com o
Damião medindo ~1–2. Reduzidos para 192 px (~6 un), decisão do Vini.

**Import:** PPU 32, Point, sem compressão, pivot Bottom-center (para o Y-sort isométrico),
GUID determinístico. Verificado no `.meta` depois da importação, não só assumido.

> **Achado:** a convenção real do projeto é **PPU 32** (Damião, Cultista, Abdul, tiles de
> areia). Os PPU 100/160 que aparecem em outros assets são exceções de concept art — e
> `yug_neth_idle.png` está em **160**, o que o faz renderizar 5× menor que o pretendido.
> Não corrigido nesta rodada (fora do pedido), mas é bug.

**Pendente:** nenhuma entrada foi posicionada em cena; depende de alinhar o blockout do
Deserto à topologia de 5 setores.

## 2026-08-01 — Sangramento exibindo "0" e Cultistas ressuscitando

Dois bugs de playtest. (Um terceiro item relatado, "vida permanece", era **confirmação de que
a persistência de Vitalidade está correta** — não bug.)

### Sangramento aparentava causar zero de dano
O dano **estava** sendo aplicado; o número na tela é que mentia. O escoamento entrega frações
minúsculas por tick (1 acúmulo × 4/s × 0,02 s = **0,08**), e `DanoFlutuante.Mostrar` faz
`Mathf.RoundToInt` — exibindo **"0"** a cada `FixedUpdate`. A Ferida de Aklo parecia inútil.

**Fix:** `CultistaAI` e `AbdulAlhazredAI` acumulam o dano de sangramento e só instanciam um
número quando ele passa de 1 (`AcumularNumeroDeSangramento`). O dano na Vitalidade continua
por tick, suave — só a **exibição** é que agrupa.

> **Bônus de performance:** a versão antiga instanciava um `GameObject` por `FixedUpdate` por
> inimigo sangrando (~50/s cada) — alocação em hot path, proibida pela Regra de Ouro 1.

### Cultistas ressuscitavam ao sair e voltar da dungeon
`Abater()` fazia `Destroy(gameObject)`; como são objetos de cena, recarregar a cena os trazia
de volta. Sair da Tumba e retornar repovoava tudo o que o jogador já tinha limpado.

**Fix:** cada Cultista ganhou um `ObjetoPersistente` (GUID imutável). `Abater()` registra a
chave via `ChavesDeSave.ChaveDeAbatido(guid)`; o `Start()` destrói o Cultista se ele já
constar como abatido. Sem `ObjetoPersistente` ele simplesmente respawna — degradação
graciosa, não erro. Ferramenta: `Tools/FavelaAmarela/Marcar inimigos como persistentes`.

**Descoberta:** a Tumba tem **2 Cultistas**, não 41 como o `roadmap_vertical_slice.md`
afirmava. Corrigido no roadmap — povoar a Tumba também é trabalho pendente, não só o Deserto.

QA: 0 erros de compilação, **320/320 testes EditMode verdes** (eram 322; os 2 a menos são os
`SaveDataTests` removidos junto com o sistema de save órfão).

## 2026-07-31 (7ª rodada) — Playtest da luta do Abdul: 3 bugs

### 1. CRÍTICO — a barra de Vitalidade nunca diminuía
Sintoma: levar dano físico não produzia feedback nenhum. **Não era o código** — a
`VitalidadeBar`, o `VitalidadeBridge` e o bootstrap estavam corretos, e a Vitalidade *estava*
caindo.

**Causa:** as `Image` do HUD estavam **sem sprite** (`m_Sprite: {fileID: 0}`). Uma `Image`
sem sprite não respeita `Image.Type.Filled` nem `fillAmount`: a Unity cai em
`Graphic.OnPopulateMesh` e desenha sempre um retângulo cheio. A barra não tinha como se
mexer. Atingia as duas `Filled` da cena — `Preenchimento` (Vitalidade) e `Recarga`
(habilidade).

**Fix:** `Tools/FavelaAmarela/Corrigir sprites faltando no HUD` atribui o sprite embutido
`UI/Skin/UISprite.psd` (placeholder — a arte real das barras segue pendente). 7 Images
corrigidas, verificado no YAML.

> **Padrão a lembrar:** `fillAmount` sem sprite é no-op silencioso. Ao montar barra nova,
> conferir o sprite antes de suspeitar da lógica.

### 2. Yug-Neth entalava o jogador
Ele está na camada `Enemy` com colisor sólido, então o corpo dele barrava a passagem de
Damião na arena. **Fix:** `YugNethAI.IgnorarColisaoCom(Collider2D)` +
`Physics2D.IgnoreCollision` com o colisor do jogador, chamado no bootstrap do `GameManager`.
Resolver por layer exigiria uma camada "Aliado" fora da taxonomia fechada do projeto; ignorar
o par de colisores atinge só o problema e **mantém** a colisão dele com paredes (ele segue
Damião sem atravessar cenário).

### 3. Esqueletos não aparecem — NÃO RESOLVIDO
Auditado tudo o que poderia causar e **está tudo correto**: `prefabEsqueleto` atribuído na
instância da cena (guid resolve para o asset certo), `esqueletosPorInvocacao: 2`,
`intervaloDeConjuracao: 3`, `tempoDeVida: 20`, evento `OnInvocarEsqueletos` assinado no
adaptador, `FixedUpdate` tickando a FSM, prefab com `DynamicYSort` e sprite (igual ao da
Pedra de Poder, que aparece).

Como a análise estática não achou a causa, **instrumentei em vez de chutar**: o `return`
mudo de `prefabEsqueleto == null` virou `LogWarning`, e um novo campo `logarInvocacoes`
(ligado por padrão) loga cada esqueleto com posição e alvo. Próximo playtest diz se eles
nascem (e onde) ou se a invocação nem é chamada.

QA: 0 erros/0 warnings, **308/308 testes EditMode verdes**.

## 2026-07-31 (8ª rodada) — Objetos de mundo persistem; Tranca de Arena genérica

Fecha o ciclo aberto na 7ª rodada: a fundação de persistência existia, mas nenhum objeto de
mundo a usava — voltar à Tumba reabria o baú (re-sorteando arma), fazia o patuá reaparecer e
ressuscitava o Abdul. QA: **0 erros de compilação, 320/320 testes EditMode verdes** (eram 322;
os 2 a menos são do `SaveDataTests` removido junto com o sistema órfão).

### Remoção de sistema de save órfão
Havia **dois** sistemas de save no projeto. O antigo (`SaveSystem`/`SaveData`, namespace em
inglês `FavelaAmarela.Core.Persistence`) era um DTO de schema fixo com campos de features já
removidas (`saltoDesbloqueado`, `armaDesbloqueada`), não referenciado por nenhum código de
gameplay. Removido: os 3 arquivos + a pasta `Core/Persistence` + `systems/persistencia.md`
(era inteiramente sobre ele), com `systems/index.md` e `tests/test_patterns.md` reapontados.

### Persistência dos objetos de mundo
`ChavesDeSave` ganhou `BauDaTumbaAberto`, `PatuaColetado` e os valores
`ValorAbdulDerrotado`/`ValorAbdulPoupado`. `GerenciadorDeSave` ganhou `DefinirValor`/`ObterValor`
(o desfecho do Abdul não é booleano).

**Write-through + chave global**, não `ObjetoPersistente`/`IPersistente`:
- Write-through porque `CapturarTudo()` só enxerga quem está carregado e registrado — um
  pickup já desativado seria pulado em silêncio.
- Chave global e não GUID porque cada um destes é único e narrativo (quests vão perguntar
  "Abdul foi resolvido?" pelo nome). GUID continua certo para objetos **repetidos**.

Detalhes que exigiram cuidado:
- **Baú não reequipa ao restaurar** — só marca aberto e troca sprite. A arma já atravessa a
  cena sozinha; equipar de novo entregaria uma segunda arma.
- **Necronomicon renasce se não foi pego** — é spawn de runtime; sair sem recolher o
  destruiria para sempre. Reinstanciado só se `NecronomiconColetado` não estiver marcado.
- **Yug-Neth é derivado de `AbdulResolvido`**, sem chave própria (os dois desfechos chamam
  `LibertarYugNeth()`, não há outro gatilho — segunda chave seria segunda fonte da verdade).

> **Bug pego na revisão do plano, antes de virar código:** a versão inicial restaurava
> Yug-Neth com `yugNethNaArena.Bind(...)` direto, o que pularia
> `GameManager.RegistrarYugNeth(...)` — os futuros Portões de Carcosa achariam que ele nunca
> foi libertado. Corrigido chamando o `LibertarYugNeth()` já existente (idempotente, faz as
> duas coisas) em vez de duplicar meia lógica dele.

**Não se mexeu na `AbdulFSM`.** No caminho "poupado" ela fica em `Transe`, que é o correto:
`PodeInteragir` já checa `!_poupado`, e a traição da trégua segue funcionando porque
`IniciarLuta()` só exige `CurrentState == Transe`. No "derrotado" o objeto some e ninguém mais
consulta a FSM.

### Tranca de Arena (padrão novo, genérico)
Correção de rumo do Vini: a primeira versão travava o portal checando chave de save no
`OnTriggerEnter2D`. Ele apontou que **nenhum chefe do jogo pode ser abandonado antes do
desfecho** — isso vai se repetir em Byakhee e Rei em Amarelo — e pediu algo melhor que um `if`
copiado três vezes.

`TrancaDeArena` (novo, Runtime/GameLoop) não sabe qual chefe a controla, nem que existe save,
nem que a saída é um `PortalDeCena`: recebe `Collider2D[]` e liga/desliga, dirigida por evento
da FSM do chefe. Chefe novo reaproveita só ligando campos no Inspector.
- `Trancar()` ao entrar na Fase 1 (inclui a traição, mesmo `IniciarLuta()`); `Destrancar()` ao
  derrotar **e** em `AplicarEstadoSalvo` (uma arena nunca pode nascer trancada).
- Desliga o `Collider2D`, não o GameObject — a saída pode carregar visual/luz/som que devem
  continuar existindo.
- Ferramenta `Tools/FavelaAmarela/Montar Tranca de Arena do Abdul` acha o portal da arena por
  **distância ao Abdul**, não por nome (`"Saida_TumbaAlhazred (1)"` é sufixo automático da
  Unity, frágil). Verificado no YAML: `saidas` → collider do portal da arena (41.43, -17.6),
  e `trancaDaArena` do Abdul → o componente certo.

Doc novo: [architecture/tranca_de_arena.md](architecture/tranca_de_arena.md).

## 2026-07-31 (7ª rodada) — Barra de vida travada (2 causas) e fundação de persistência

### Bug crítico: a barra de Vitalidade nunca diminuía
Relatado em playtest. **Duas causas independentes**, ambas corrigidas:

1. **`Image` sem sprite.** Uma `Image` sem sprite ignora `Image.Type.Filled` e `fillAmount`:
   a Unity cai num caminho de desenho alternativo que produz sempre um retângulo cheio. O
   código da `VitalidadeBar` estava correto o tempo todo. Ferramenta
   `Tools/FavelaAmarela/Corrigir sprites faltando no HUD` (5 Images corrigidas).
2. **Ordem de `Awake`.** `GameManager.Awake` lia `VitalidadeBridge.Vitalidade` para injetar
   no HUD, mas essa POCO nascia no `Awake` da bridge — e a Unity **não garante ordem de
   `Awake` entre GameObjects**. Recebia `null`, e `InjetarVitalidade` tinha um `return`
   mudo. Corrigido com **inicialização preguiçosa** em `VitalidadeBridge`: `Vitalidade` e
   `Atributos` agora garantem a criação na primeira leitura, eliminando a dependência de
   ordem para todos os consumidores.

> **Autocorreção:** cheguei a afirmar que não existia `HUDController` na cena. Errado — ele
> existe dentro de `HUD_ResilienciaBar.prefab` e está corretamente ligado à `VitalidadeBar`
> por override de instância. Eu tinha procurado só no YAML da cena, não nos prefabs.

Silêncios viraram erros altos: `GameManager` sem `HUDController` e `InjetarVitalidade(null)`
agora logam `LogError`. Foi o silêncio que escondeu o bug.

**Descoberta lateral:** não existe `ResilienciaBar` na cena da Tumba — a Resiliência Mental,
recurso central do jogo, não tem barra no HUD. Não criei UI por conta própria.

### Outros fixes do mesmo playtest
- **Yug-Neth travava a passagem:** está na layer `Enemy` com colisor sólido. Resolvido com
  `Physics2D.IgnoreCollision` contra o colisor de Damião (novo `YugNethAI.IgnorarColisaoCom`,
  chamado no bootstrap do `GameManager`) — **não** por layer nova, que mudaria a taxonomia
  fechada do projeto. Ele continua colidindo com paredes.
- **Esqueletos "não apareciam":** tudo conferia estaticamente (prefab atribuído, GUID válido,
  evento assinado, `tempoDeVida` 20 s, `DynamicYSort` presente). Instrumentei
  `HandleInvocarEsqueletos` com log opcional e um aviso no `return` que antes era mudo — o
  Vini confirmou depois que os placeholders aparecem.

### Fundação de persistência (decisão do Vini: documentar **e** construir)
Motivada por outro achado do playtest: **a arma do baú sumia ao sair da dungeon**. Ver
[architecture/persistencia.md](architecture/persistencia.md) para a arquitetura completa.

**Core** (`Core.Persistencia`, POCO puro, **14 testes**):
- `RegistroDeSave` — mapa chave → estado, com degradação graciosa (chave nula, entrada nula,
  chave repetida, save corrompido: nada lança).
- `EstadoDeSave`/`EntradaDeSave` — formato em disco (lista, porque `JsonUtility` não
  serializa `Dictionary`).
- `ChavesDeSave` — constantes das chaves globais, convenção hierárquica (`Quest.Tumba.X`).

**Runtime** (`Runtime.Persistencia`):
- `ObjetoPersistente` — GUID imutável por objeto de cena. **Nunca** nome ou caminho de
  hierarquia: renomear quebraria o save em silêncio.
- `IPersistente` + `GerenciadorDeSave` — padrão Observer, um arquivo JSON só,
  `DontDestroyOnLoad`. **Nunca `PlayerPrefs`** (Regra de Ouro 9).
- `EstadoPersistenteDoJogador` — arma e Vitalidade atravessando a troca de cena.
- `MaoFisicaBridge.EquiparArma(ArmaDaTumba)` — sobrecarga que guarda o **identificador**
  (a instância de `IArmaComHabilidade` não é serializável). `BauDaTumba` passou a usá-la.
- `PortalDeCena` chama `CapturarTudo()` antes de trocar de cena.

> **Gotcha de assembly:** a primeira versão do `ObjetoPersistente` usava `UnityEditor` dentro
> de `#if UNITY_EDITOR`. Nenhum outro script de Runtime do projeto faz isso, e
> `FavelaAmarela.Runtime.asmdef` não referencia o assembly de Editor. Reescrito para usar só
> `Reset`/`OnValidate` (mensagens comuns de `MonoBehaviour`), com o carimbo em massa movido
> para a ferramenta de Editor `Gerar chaves de persistência`.

**Instalado em cena:** `GerenciadorDeSave` + `EstadoPersistenteDoJogador` nas **duas** cenas
jogáveis (Tumba e Deserto), via `Tools/FavelaAmarela/Montar persistência em TODAS as cenas
jogáveis`. Instalar nas duas pontas é obrigatório: capturar na saída não adianta se a cena
de chegada não tiver quem reaplique. **A arma do baú agora atravessa a porta.** Verificado no
YAML das duas cenas. QA: **322/322 testes EditMode verdes** (308 + 14 de persistência).

**Ainda não ligado:** objetos de mundo (baú, patuá, Abdul) não implementam `IPersistente` —
voltar à Tumba reabre o baú. Gravar/carregar do **disco** existe mas ninguém chama: a
persistência hoje vive só em memória, entre cenas da mesma sessão; fechar o jogo perde tudo.
O ponto de save natural é o `RefugioDeLuz`.

## 2026-07-31 (6ª rodada) — Saída da Tumba: ida e volta pela mesma porta

Segundo achado do playtest: dava para entrar na Tumba pelo Deserto, mas **não para sair**.
Decisão do Vini: a saída fica na **própria porta de entrada**, sem inventar um segundo local.

### O problema não era só "falta um trigger"
`PortalDeCena` usava `SceneManager.LoadScene`, que recarrega a cena do zero — o jogador
reapareceria no ponto inicial do Deserto (-12, -13), não na porta da Tumba (-17, -2). E
colocar a chegada em cima do portal de entrada criaria um **pingue-pongue infinito**: o
trigger dispararia no mesmo frame do carregamento e devolveria o jogador para dentro.

### Duas peças pequenas
- **`PontoDeChegada.cs` (novo, Runtime/GameLoop):** marca onde Damião aparece ao chegar numa
  cena vindo de um portal específico. O portal grava o identificador num `static` (sobrevive
  à troca de cena por não ser objeto de cena) e o ponto correspondente reposiciona o jogador.
  Preserva o Z do jogador — sobrescrever tiraria ele do plano de render.
- **`PortalDeCena`:** ganhou `chegarEm` (para onde apontar na cena destino) e
  `carenciaAoCarregar` (0,5 s ignorando contato após carregar). A carência é o que permite
  ida e volta pela **mesma** porta sem quicar.

### Cenas (via `Tools/FavelaAmarela/Montar saída da Tumba (ida e volta)`)
- Tumba: `Saida_TumbaAlhazred` em (0.84, 0.98) — a entrada — destino `Deserto_Hali`,
  chegada `TumbaAlhazred`.
- Deserto: `Chegada_TumbaAlhazred` em (-17, -2), colado na `Entrada_TumbaAlhazred`.

Verificado no YAML das duas cenas (`cenaDestino`, `chegarEm`, `identificador`), não só pelo
log de sucesso do MCP.

> **Escopo honesto:** isto é porta de ida e volta, **não** persistência entre cenas.
> Inventário, Vitalidade e progresso continuam se perdendo na troca — a arquitetura
> multi-cena com save segue pendente.

QA: **308/308 testes EditMode verdes**. Sem teste novo: `PontoDeChegada`/`PortalDeCena` são
MonoBehaviours dependentes de cena e `SceneManager`, fora do alcance da suíte POCO.

## 2026-07-31 (5ª rodada) — Fix: golpe do jogador feria Yug-Neth

Achado pelo Vini em playtest: o companheiro **obrigatório** levava dano do próprio Damião
durante a luta do Abdul. Bug real, não cosmético — dá para inviabilizar a run matando quem
abre os Portões de Carcosa.

**Causa:** `MaoFisicaBridge.ResolverGolpe` acerta qualquer `IDanificavel` no alcance, e
`YugNethAI` exige `VitalidadeBridge`, que implementa `IDanificavel`. Nada distinguia aliado
de inimigo.

**Duas regras do Vini, duas soluções distintas** (uma só não resolve as duas):

| Regra | Solução | Escopo |
|---|---|---|
| "Nessa luta ele não pode levar dano" | `VitalidadeBridge.IgnorarDano = true` enquanto cativo, liberado em `Bind()` | Temporário |
| "Nunca pode levar dano do jogador" | Novo marcador `Aliado` que a arma respeita | Permanente |

Invulnerabilidade total não serviria para a segunda regra: mataria a mecânica de
incapacitação/Refúgio construída horas antes, que **depende** de ele poder cair para
inimigos depois de livre.

### Core / Runtime
- **`Aliado.cs` (novo, Runtime/Combat):** marcador sem lógica nem estado. `MaoFisicaBridge`
  pula qualquer alvo que o carregue, **antes** de checar `IDanificavel` (um aliado
  normalmente também é danificável — é o marcador que protege, não a falta de vitalidade).
  Escolhido em vez de uma checagem de tipo (`é o Yug-Neth?`) para não amarrar o combate a um
  personagem: companheiro futuro fica protegido só de ganhar o componente.
- **`YugNethAI`:** `IgnorarDano = true` no `Awake` (cativo = intocável), `false` no `Bind`
  (livre = vulnerável, incapacitação passa a valer). Ganhou `[RequireComponent(typeof(Aliado))]`.
- **`ProtegerYugNethDoJogador.cs` (Editor):** grava o marcador no prefab que já existia.

> **Gotcha caro registrado:** a primeira versão da ferramenta checava
> `prefab.GetComponent<Aliado>()` antes de salvar e logou "nada a fazer" — mas o YAML em
> disco continuou sem o componente. Com `[RequireComponent]`, a Unity adiciona o componente
> **em memória** ao carregar o prefab, então a checagem passa e o salvamento é pulado.
> Correção: gravar incondicionalmente. Confirmado no YAML depois (`grep` do guid).

**Instância na cena:** é `PrefabInstance` conectada com `m_AddedComponents: []`, então herda
o marcador do prefab — nenhuma edição de cena foi necessária.

**Sem teste EditMode novo:** a proteção vive em `MaoFisicaBridge` (MonoBehaviour + Physics2D
`OverlapCircle`), fora do que a suíte POCO cobre. Verificado por leitura e pelo YAML, não por
teste automatizado — vale um teste PlayMode quando essa camada existir.

QA: 0 erros/0 warnings de compilação, **308/308 testes EditMode verdes**.

**Também relatado no mesmo playtest e ainda pendente:** não há saída da Tumba depois da luta
do Abdul — a dungeon é um beco sem saída. Virou tarefa própria.

## 2026-07-31 (4ª rodada) — Fix: erro falso das Pedras de Poder em playtest

Achado pelo Vini jogando de verdade: ao começar a luta do Abdul, o console cuspia 4×
`[PedraDePoder] não está vinculada a um Abdul — quebrá-la não derrubará escudo nenhum`.

**Era alarme falso** — as pedras estavam vinculadas e a luta funcionava. Causa: `Instantiate`
dispara o `Awake` do prefab **sincronamente**, antes de quem instanciou conseguir chamar
`Bind()` na linha seguinte. A validação de `Awake` inspecionava o campo nessa janela e
acusava toda pedra nascida em runtime de estar órfã.

**Fix:** validação movida de `Awake` para `Start` em `PedraDePoder`. Mantém a rede de
segurança da regra 7 do `CLAUDE.md` (pega uma pedra colocada à mão na cena sem vínculo) sem
o falso positivo, porque `Start` roda no frame seguinte, já com a injeção feita. A
inicialização da `Vitalidade` continua em `Awake`.

**Auditado o resto:** `EsqueletoInvocado` e `ConeDeGelo` também nascem em runtime e recebem
`Bind`/`Lancar` depois do `Instantiate`, mas **não** validam nada em `Awake` — imunes ao
mesmo problema. `NecronomiconPickup` não recebe injeção. `PedraDePoder` era o único caso.

> **Padrão a lembrar:** campo injetado por `Bind()` após `Instantiate` **não** pode ser
> validado em `Awake`. Use `Start`. Só campos vindos do Inspector podem ser validados em
> `Awake`, porque já estão preenchidos quando ele roda.

QA: recompilado com 0 erros/0 warnings, **308/308 testes EditMode verdes**.

## 2026-07-31 (3ª rodada) — Escopo do Vertical Slice redefinido; inventário destravado

Decisão de governança, sem alteração de código. Resolvida a divergência que o
`roadmap_vertical_slice.md` apontava entre duas definições concorrentes de Vertical Slice.

### Decisões (Vini, 2026-07-31)
1. **O VS são os 14 itens da lista priorizada do GDD v3.0** — Fase 1 completa (Deserto +
   Santuário de Yhtill + Byakhee) **e a última fase do jogo** (Castelo de Carcosa + Rei em
   Amarelo). Substitui a definição estreita do `CLAUDE.md` ("só a Tumba jogável de ponta a
   ponta"). A Tumba passa a ser **uma peça concluída** do VS.
   - **É um recorte "abertura + desfecho"**, não duas fases seguidas: o jogo completo tem
     6 fases e o VS pula as 4 do meio de propósito. O que a lista de produção chamava de
     "Fase 2" é a **fase final** — isso reconcilia parcialmente a numeração que estava
     pendente desde 2026-07-28.
   - Consequência: **o Castelo não é candidato a corte.** Uma recomendação anterior minha
     dizia o contrário, escrita antes de eu saber que era a fase final; corrigida no
     roadmap.
2. **Inventário e barra de ações destravados.** Eram "previstos, sem data, não implementar
   sem confirmar" desde 2026-07-07. Liberados porque o item 2 (Consumíveis) depende deles.

### Documentação
- `CLAUDE.md`: §1.1 reescrita; bullet de inventário passou de "previsto/sem data" para
  "liberado", com as restrições de forma (enxuto, terminologia diegética) mantidas.
- `roadmap_vertical_slice.md`: banner de aviso virou registro da decisão; seção "Risco de
  escopo" virou "Consequências da decisão", com nova ordem recomendada (tempestade →
  povoar o Deserto → inventário → arte em paralelo; Castelo como candidato a corte).

## 2026-07-31 (2ª rodada) — Yug-Neth: morte vira incapacitação recuperável; Refúgio mínimo

Correção de rumo confirmada pelo Vini: a morte do companheiro **deixa de ser fim de run
permanente** (estilo Ashley/RE4, decisão de 2026-07-30) e vira **incapacitação
recuperável** — ele cai no lugar, bloqueia os Portões de Carcosa, e é reanimado num
Refúgio de Luz. QA: **308/308 testes EditMode verdes**.

### Decisões (Vini, 2026-07-31)
1. Ao cair, Yug-Neth **fica caído no lugar** (não some, não reaparece em outro ponto) —
   se ele cair longe de um Refúgio, isso vira um trajeto arriscado sem companion.
2. Incapacitado, ele **bloqueia os Portões de Carcosa** (não a run inteira) — reforça que
   é obrigatório sem repetir a dureza do Colapso.
3. **Construir o Refúgio mínimo agora**, não adiar — mesmo sem os sistemas maiores
   (regen de RM, pausa de tempestade, save) que crescem depois.

### Core
- **`TipoDeDerrota.EscoltaPerdida` removido** (enum + pool de frases em `FrasesDeColapso`) — dead code deletado, não deprecado in-place. 2 testes que travavam esse pool também removidos.

### Runtime
- **`YugNethAI`**: novo estado **Incapacitado** (terceiro, além de Cativo/Livre). `HandleAbatido` não dispara mais um evento de "fim de jogo" — trava o movimento, tinge de cinza e expõe `EstaIncapacitado`. Novo `Reanimar()`: cura a Vitalidade ao máximo e volta a seguir (idempotente).
- **`RefugioDeLuz.cs` (novo):** versão mínima de Poste de Luz — trigger de **proximidade** (não botão E: é "descansar sob a luz", não uma ação deliberada) que chama `Reanimar()` se Yug-Neth estiver incapacitado. `TODO(design)` documentado para o que falta (regen de RM, pausa de dreno, save).
- **`GameManager`**: não assina mais um evento de derrota do companheiro. Expõe `GameManager.YugNeth` para quem precisar consultar `EstaIncapacitado` (candidato natural: o trigger dos Portões de Carcosa, ainda não construído).

### Documentação
- `systems/companheiro_mi_go.md`: seção de morte reescrita; arquitetura e pendências atualizadas.
- `roadmap_vertical_slice.md`: item 3 (Companheiro) atualizado — a mecânica de incapacitação conta como feita; só falta a barra no HUD.

## 2026-07-31 — Sangramento com acúmulo (stack até 10 → estouro) e roadmap do Vertical Slice
## 2026-07-31 — A luta do Abdul fecha de ponta a ponta

Últimas pendências mecânicas do boss resolvidas. QA: **296/296 testes EditMode verdes**
(+18 nesta rodada), compilação limpa.

### Core
- **`Sangramento.cs` (novo, 11 testes):** a Ferida de Aklo do Estilete de Irem. `Tick(dt)` devolve **quanto dano sai no intervalo** e quem chama decide onde aplicar — mantém a regra pura e reutilizável (um veneno futuro usa a mesma peça). Reaplicar **renova, não empilha** (fica com o mais forte, reinicia a duração): empilhar transformaria a arma mais fraca num acumulador infinito. O último tick é proporcional ao tempo que sobrou, então a ferida nunca entrega mais dano do que a duração previa.
- **`PlayerState.Congelado` + `PlayerStateMachine.ForcarEstado` (novo, 7 testes):** caminho dos efeitos **impostos** pelo inimigo, distinto de `TryEntrarAcao` (ações escolhidas, que recusam se já houver ação em curso). Um atordoamento não pode falhar só porque o jogador estava esquivando — seria exatamente a janela para ignorá-lo.

### Runtime
- **`CongelamentoBridge.cs` (novo):** liga `AcumuloDeCongelamento` (a regra) à FSM do jogador (o efeito). Tinge o Damião de azul enquanto congelado.
- **`ConeDeGelo.cs` (novo):** projétil da Fase 2. Aplica acúmulo de frio **e** dano anômalo — segue o canal correto da ficha (`Conjuracao` do lançador → mitigada pela `ResistenciaAnomala` do alvo → drena **Resiliência Mental**, não Vitalidade: é magia, não pancada). Autodestrói ao acertar, em parede ou por tempo de vida.
- **`EsqueletoInvocado.cs` (novo):** perseguidor frágil que dá a pressão da Fase 1 (reaproveita `SeguidorDeAlvo` com distância de conforto zero — quer encostar, não acompanhar). **Expira sozinho**: sem tempo de vida, uma luta longa acumularia dezenas e viraria multidão impossível em vez de pressão pontual.
- **`NecronomiconPickup.cs` (novo):** o tomo cai ao derrotar Abdul e é recolhido com **E** (`IInteragivel`), não por efeito automático. Exclusivo do caminho da luta.
- **`CultistaAI` e `AbdulAlhazredAI`** passaram a consumir `SangramentoPorSegundo` do `ArmaResult` e a escoar a ferida por frame.
- **`AbdulAlhazredAI`** ganhou `_alvoDasConjuracoes` (capturado na conversa, sem `FindObjectOfType`) para os Cones mirarem o Damião e os esqueletos nascerem já perseguindo.

### Decisão de design: o sangramento atravessa o Escudo Mágico
A ferida é aberta na janela de vulnerabilidade e **continua drenando enquanto Abdul se
protege**. É isso que torna o Estilete — a arma de menor dano do baú — viável contra um
boss cujo escudo fecha a janela de golpe: em vez de disputar DPS numa janela curta, ele
cobra durante a espera. Sem essa regra, a arma mais fraca seria só a pior escolha, e a
premissa "vencível com qualquer uma das 3 armas" (baú é RNG) não se sustentaria.

### Bug corrigido: slicer invalidava referências de sprite
`SliceSpritesheetAbdul` usava `GUID.Generate()` por frame, então **reexecutar o slicer
quebrava toda referência existente** aos sprites — o Abdul ficava **invisível sem nenhum
erro no console**. Trocado por GUID **determinístico** (MD5 do nome do frame): mesmo nome ⇒
mesmo ID, sempre. Ferramenta `Fechar Luta do Abdul` reconecta o sprite se ele ficar órfão.

### Editor
- **`MontarPrefabsDaLutaDoAbdul.cs` (novo):** cria os 4 prefabs + o visual do escudo (placeholder colorido) e liga tudo nos campos do Abdul. Idempotente.
- **`FecharLutaDoAbdul.cs` (novo):** reconecta o sprite do Abdul e garante o `CongelamentoBridge` no Damião.

## 2026-07-30 — Auditoria e limpeza completa do Knowledge Bundle

Varredura de todo o `Docs/KnowledgeBundle/` contra o estado real do código, depois da sessão
longa de mudanças. **Zero links quebrados** ao final (validado por script). Motivação: o GDD
é compartilhado com os diretores (Nicolas Lobato, Thiago Tuchu) via Notion, então
afirmações falsas ali têm custo real.

### Correções de maior impacto (documentos que os diretores leem)
- **`lore/world_rules.md`** — a regra 5 dizia *"Damião não tem armas... Nunca proponha mecânicas de combate direto"*, **factualmente falso**: existe um sistema de combate completo e testado. Reescrita para "stealth é o núcleo tonal; o combate é um pilar sistêmico real", com o limite correto (o que não existe é farming/loot genérico de ARPG). A regra 1 (realidade maleável) deixou de citar o Salto Dimensional como prova — a premissa cosmológica continua, sem ferramenta de jogador atrelada. A regra 4 passou a descrever as **duas formas de perder** (Colapso mental × morte corpórea) mais a escolta perdida.
- **`GDD_Mestre.md` → v1.3** — estava "meio-corrigido" (§3.2 já dizia que o Salto foi removido, mas §1.4, §3.1, §5.4, §7.3 e §14 ainda o citavam como mecânica ativa). Todas corrigidas. Também: nome errado da arma (**Cravo de Ferro** → **Cravo de Aklo**), §4.3 documentando o HUD real (Vitalidade, Barra de Ações, Prompt, Painel de Escolha), novo §4.3.1 sobre a camada de interação por botão E, e novo §3.4.1 com a estrutura da luta do Abdul.

### Arquivos removidos (documentavam código que não existe mais)
`systems/dimensional_leap.md`, `scripts/core/dimensional_leap_cs.md`,
`scripts/runtime/anomaly_power_bridge_cs.md`, `systems/queda_z4_z5.md`.

### Documentação nova (22 arquivos)
Todo o sistema construído nesta sessão estava ausente da documentação técnica de scripts.
Criados em `scripts/core/`: `Vitalidade`, `FichaDeAtributos`, `MitigacaoDeDano`,
`IDanificavel`, `AcumuloDeCongelamento`, `AbdulFSM`, `SeletorDeInteracao`, `SeguidorDeAlvo`,
`NavegadorDeOpcoes`, `PlayerStateMachine`. Em `scripts/runtime/`: `VitalidadeBridge`,
`FichaAtributosConfig`, `DanoFlutuante`, `IInteragivel`, `DetectorDeInteracao`,
`AbdulAlhazredAI`, `YugNethAI`, `PedraDePoder`, `BauDaTumba`, `PromptDeInteracao`,
`PainelDeEscolha`, `VitalidadeBar`, `BarraDeAcoes`. Ambos os índices reescritos.

### Reescritos por estarem completamente desatualizados
- `scripts/runtime/player_movement_cs.md` — descrevia a arquitetura antiga (Salto, `Invoke`); agora reflete `PlayerStateMachine`, ataque/habilidade e `MovimentoBloqueado`.
- `scripts/runtime/patua_pickup_cs.md` — descrevia o destravamento do Salto; agora é `IInteragivel` com efeito pendente de design.
- `scripts/core/cultista_fsm_cs.md` — listava 3 estados; são **5** (com `Atacar` e `Atordoado`).

### Notas de status (script existe, mas sem instância na cena)
`TempestadeAmbiente`, `TempestadeVisualOverlay`, `TempestadeZonaTrigger`,
`QuedaZ4Z5Trigger`, `CercoZ4Cutscene` ganharam aviso no topo — continuam no projeto
(reaproveitáveis no Overworld), mas não estão mais instanciados na Tumba.

### Outras correções
`abilities.md` (Salto removido dos poderes cadastrados; gating inicial agora é o baú RNG),
`architecture/dependency_map.md` e `poco_adapter_pattern.md` (pares POCO/Adapter novos),
`namespace_conventions.md`, `tests/test_patterns.md` (8 suites novas listadas),
`lore/glossary.md` (Dash → Esquiva), `lore/reliquias_cosmicas.md`, `systems/level_design.md`,
`systems/sound_propagation.md`, `systems/espectro.md`, `systems/persistencia.md`,
`lore/deserto_e_dungeons.md`, `camera_controller_cs.md`, `esquiva_bridge_cs.md`,
`mao_fisica_bridge_cs.md`, `game_manager_cs.md`. As pendências de "remover tempestade da
dungeon" em `environment.md` e `level_design_deserto_hali.md` foram marcadas como concluídas.

### Pendência de design sinalizada, não resolvida
A **Z6 do Templo da Serpente** (Dungeon 2, conteúdo futuro) tinha o Salto Dimensional como
solução de travessia. Com a habilidade removida, esse design ficou sem ferramenta —
marcado no GDD como pendente de redesenho, não corrigido silenciosamente.

## 2026-07-30 — Abdul vira prefab, Pedras de Poder por fase, traição da trégua

Três decisões do Vini implementadas. QA: **278/278 testes EditMode verdes**.

### Traição da trégua (`AbdulAlhazredAI.ReceberGolpe`)
Atacar Abdul depois de escolher "Concordar" **reabre a luta de verdade** — o jogador trai a
trégua e ainda pode derrotá-lo para pegar o Necronomicon. O golpe que trai **não causa
dano**: só desperta a luta, porque o Escudo Mágico sobe junto com `IniciarLuta()` e só cai
ao quebrar uma Pedra de Poder, exatamente como no caminho normal. Antes disso, o golpe era
descartado silenciosamente (efeito colateral do código, não decisão de design).

### Pedras de Poder nascem na Fase 1, não ficam plantadas na dungeon
Correção de design do Vini: as Pedras são **âncoras do ritual de Abdul**, não cenário
permanente da cripta. Agora `AbdulAlhazredAI` assina `AbdulFSM.OnStateChanged` e:
- **entra na Fase 1** → instancia as Pedras (losango ao redor dele, ou `pontosDasPedras` manuais) e faz `Bind(this)` em cada uma;
- **sai da Fase 1** (virada para Fase 2 ou derrota) → destrói as que sobraram, já que o escudo deixa de depender delas.

`PedraDePoder` ganhou `Bind(AbdulAlhazredAI)` — mesmo padrão de injeção do resto do
Runtime, em vez de arrastar referência no Inspector de um prefab compartilhado.

### Abdul virou Prefab
Ele era um `GameObject` solto montado à mão (diferente de Cultista/Espectro/Coisa, que já
eram prefabs). `ConverterAbdulEmPrefab` (Editor, execução única) usa
`SaveAsPrefabAssetAndConnect` — preserva todos os valores já configurados e deixa a
instância da cena conectada ao asset. Referências de cena (`painelDeEscolha`,
`caixaDeDialogo`, `yugNethNaArena`) sobrevivem como override da instância, verificado.

### Ferramentas de Editor
- **`ConverterAbdulEmPrefab.cs` (novo):** a conversão acima.
- **`MontarPedrasDePoder.cs` (novo):** cria `PedraDePoder.prefab`, liga no campo do Abdul e **remove as Pedras soltas** que sobraram na cena do modelo antigo (eram visíveis na cripta antes da luta — o bug que esta mudança corrige).
- **`SetupArenaDoAbdul.cs`:** deixou de ser destrutivo. Antes ele **apagava a raiz `TumbaDeAbdul_Conteudo` inteira** a cada execução — o que agora destruiria o Abdul-prefab e todo o wiring de cena feito nele. Passou a só criar o que ainda não existe, e não planta mais Pedras.

### Sorting Layer — resolvido (NÃO criar "Characters")
O doc de lore do Yug-Neth pedia uma Sorting Layer `Characters` separada. **Descartado após
investigação:** o Unity ordena primeiro por Sorting Layer e só depois por `sortingOrder`,
então uma layer separada para personagens faria uma parede no `Default` nunca mais desenhar
na frente de um personagem — quebrando a oclusão dither (`OcclusaoDitherFade`) que dá a
sensação isométrica. Tudo fica em `Default` com `DynamicYSort`, como o resto do projeto.
Documentado em `systems/companheiro_mi_go.md`.

## 2026-07-30 — Yug-Neth: nome oficial, cativeiro pré-cena e alinhamento com o design narrativo

O Vini escreveu (em ferramenta paralela) o design narrativo completo do companion —
`lore/migo_companion.md` (novo), `lore/abdul_alhazred.md` reescrito, `GDD_Mestre.md` v1.2,
`bestiary.md` §8 e `lore/index.md` atualizados. Li tudo, comparei com a implementação da
rodada anterior e alinhei o que fazia sentido para o escopo atual, com 3 decisões dele.
QA: **278/278 testes EditMode verdes**, sem regressão.

### Decisões de alinhamento (Vini, 2026-07-30)
1. Manter `Vitalidade`/`VitalidadeBridge` — **não** implementar o recurso "Resiliência do
   Companheiro" (RC, dano fixo por fonte, máx 100) descrito no documento de lore.
2. Gatilho único: só a conversa com Abdul leva à escolha (lutar/concordar) — **não** dois
   objetos separados (correntes vs. grimório) como o documento descrevia.
3. Yug-Neth **já existe na cena, cativo** (anda de um lado para o outro) antes de ser
   libertado — não é mais instanciado sob demanda via prefab. Também confirmado: durante a
   luta com Abdul ele não é alvo de nada (ainda sob controle dele); o Necronomicon é um
   item a coletar depois da derrota, não um efeito automático.

Ficou como pendência de decisão: o que acontece se o jogador atacar Abdul depois de já ter
escolhido "Concordar" — hoje o golpe é descartado silenciosamente (efeito colateral do
código, não escolha de design).

### Rename completo: Mi-Go → Yug-Neth
- `MiGoCompanionAI.cs` → `YugNethAI.cs` (classe, namespace docs, `AddComponentMenu`).
- `GameManager`: `_miGo`/`RegistrarMiGo`/`HandleMiGoAbatido` → `_yugNeth`/`RegistrarYugNeth`/`HandleYugNethAbatido`.
- `AbdulAlhazredAI`: `prefabMiGoCompanion`/`LibertarMiGo`/`_miGoJaLibertado` → `yugNethNaArena` (referência de cena, não mais prefab)/`LibertarYugNeth`/`_yugNethJaLibertado`.
- `FrasesDeColapso`: comentário do `EscoltaPerdida` cita Yug-Neth pelo nome.
- Assets: `Ficha_MiGo.asset` → `Ficha_YugNeth.asset` (corrigido também o `m_Name` interno, que um `mv` de shell não atualiza sozinho); prefab antigo `MiGo_Companion.prefab` (referenciava o tipo removido) excluído e recriado como `YugNeth.prefab`.

### `YugNethAI` — estado cativo → livre
- Novo campo/estado: **cativo** por padrão (vaivém via `PatrolRoute` ping-pong, `loop: false` — reaproveita a mesma peça já testada do `CultistaAI.Errante`, sem inventar sistema novo) e **livre** a partir de `Bind(Transform)` (passa a seguir via `SeguidorDeAlvo`). `Liberado` exposto como propriedade de leitura.
- `MontarInteracaoEDialogoAbdul` (Editor) reescrita: em vez de só criar um prefab reutilizável, agora **instancia Yug-Neth diretamente na cena** (`PrefabUtility.InstantiatePrefab`, mantendo o vínculo de prefab) ao lado do Abdul, e usa o **sprite real** (`yug_neth_idle.png`, já presente no projeto com `.meta` corretamente configurado — PPU 32, Point, sem compressão, pivot Bottom, tudo conferido e correto) em vez do placeholder quadrado quando disponível.

### Documentação
- `systems/companheiro_mi_go.md`: renomeado no título/conteúdo para Yug-Neth, seção nova "Divergências do design narrativo" listando exatamente onde a implementação diverge do documento de lore (gatilho único, sem risco durante a luta, recurso mantido como Vitalidade, roteiro reduzido à bifurcação final).

## 2026-07-30 — Companheiro Mi-Go obrigatório + escolha ramificada com Abdul
## 2026-07-30 — Companheiro Mi-Go obrigatório + escolha ramificada com Abdul

Decisão dos diretores: o filhote Mi-Go acorrentado por Abdul vira **companheiro
obrigatório** (sem ele, os Portões de Carcosa não abrem) — substitui a aplicação anterior
de "lore oculta/easter egg" do bestiário. A conversa com Abdul agora ramifica: **lutar**
(Necronomicon + Mi-Go) ou **concordar** (só Mi-Go, Abdul poupado). Perder o Mi-Go encerra a
run na hora (estilo escolta da Ashley em RE4) — sem resgate no meio da luta. QA:
**278/278 testes EditMode verdes** (+14: FrasesDeColapso, NavegadorDeOpcoes, SeguidorDeAlvo).

### Core
- **`FrasesDeColapso.cs`:** novo `TipoDeDerrota.EscoltaPerdida` + pool de frases próprio.
- **`Core/Dialogo/NavegadorDeOpcoes.cs` (novo):** cursor puro de uma escolha de N opções (Avançar/Retroceder com wraparound) — a mecânica por trás de qualquer diálogo ramificado, não só o do Abdul. `OpcaoDeDialogo` (texto + id opaco).
- **`Core/Companion/SeguidorDeAlvo.cs` (novo):** regra de movimento "fica parado dentro de uma distância de conforto, anda até o alvo quando fica pra trás" — usada pelo Mi-Go.

### Runtime
- **`MiGoCompanionAI.cs` (novo):** o companheiro — `VitalidadeBridge` (reaproveitada, mesma peça do Damião) + `SeguidorDeAlvo`. Alvo injetado via `Bind(Transform)` (nunca busca por tag). Passivo, não ataca.
- **`AbdulAlhazredAI.cs`:** `Interagir` agora termina em `ApresentarEscolha` (painel de 2 opções) em vez de chamar a luta direto. `LibertarMiGo()` é o ponto único de spawn, chamado tanto pela trégua quanto por `HandleDerrotado` (a luta também liberta o Mi-Go, só o Necronomicon é exclusivo dela). Flag `_poupado` fecha `PodeInteragir` permanentemente após a trégua.
- **`GameManager.cs`:** `RegistrarMiGo(MiGoCompanionAI)` — registro pontual (o companheiro não existe no bootstrap, só nasce quando libertado em algum ponto do meio-jogo) que assina `OnMiGoAbatido` → transição para Colapso com `TipoDeDerrota.EscoltaPerdida`.
- **`PainelDeEscolha.cs` (novo, UI):** caixa de escolha navegável pelo eixo vertical do Move + confirmação pelo Interact (E) — reusa os mesmos controles já existentes, sem botão novo.
- **`PlayerMovement.cs`:** novo `MovimentoBloqueado` (trava movimento enquanto o painel de escolha está aberto, senão W/S andaria o Damião durante a navegação).

### Ferramentas de Editor
- **`MontarInteracaoEDialogoAbdul.cs` (novo):** monta tudo de uma vez na cena aberta — `DetectorDeInteracao` + `PromptDeInteracao` no Damião (a camada de interação existia em código desde a sessão anterior mas nunca tinha sido instanciada na cena — o botão E não fazia nada até agora), `PainelDeEscolha` no HUD, `Ficha_MiGo.asset` e o prefab `MiGo_Companion` (criados se não existirem, sprite placeholder), e liga os 3 campos novos do `AbdulAlhazredAI` na cena. Idempotente, não sobrescreve campo já preenchido, não salva a cena sozinho.

### Documentação
- **`systems/companheiro_mi_go.md` (novo)** + entrada no índice.
- **`lore/bestiary.md` §8:** aplicação do Mi-Go atualizada — de "easter egg, não combatível" para "companheiro obrigatório", com nota explícita da mudança de decisão.

## 2026-07-30 — Tumba vira dungeon fechada: legado das Ruínas fora, Salto removido, Abdul interativo
## 2026-07-30 — Tumba vira dungeon fechada: legado das Ruínas fora, Salto removido, Abdul interativo

Decisões do Vini (2026-07-30): a Tumba é uma **dungeon única e fechada**, então toda a
lógica herdada das Ruínas Pálidas sai; o **Salto Dimensional é removido do jogo** (mas a
interface `IAnomalyPower` **fica**, como contrato para poderes anômalos futuros); o patuá
foi revisto e **não destrava mais o Salto** — ganhará outro propósito, ainda não definido.
QA: **264/264 testes EditMode verdes** (268 antes, −4 do `DimensionalLeapTests` removido).

### Camada de interação (nova)
- A ação `Interact` (**E** / botão Norte) e a tag `Interactable` **existiam no projeto mas nenhum código as lia** — tudo era `OnTriggerEnter2D` automático. Agora existe interação deliberada: `SeletorDeInteracao` (Core, POCO, 10 testes), `IInteragivel` (Runtime), `DetectorDeInteracao` (no Damião) e `PromptDeInteracao` (UI). Ver `systems/interacao.md`.
- **Baú da Tumba** e **patuá** convertidos para o botão. Gatilhos de travessia (cena, tempestade, tutorial, colapso) seguem automáticos de propósito.

### Abdul interativo
- `AbdulAlhazredAI` implementa `IInteragivel`: enquanto dorme em `Transe` o prompt oferece **"Falar com o vulto"**; cada aperto avança uma fala e **a última desperta a Aparição**, iniciando a luta. Depois de desperto o prompt some. Encaixou sem refatorar a FSM — ela já nascia em `Transe` com `IniciarLuta()`. Falas editáveis por `[TextArea]` no Inspector.

### Remoção do Salto Dimensional
- **Excluídos:** `DimensionalLeap.cs`, `AnomalyPowerBridge.cs`, `SaltoDimensionalConfig.cs` + `.asset`, `DimensionalLeapTests.cs`.
- **`PlayerState.Saltando` removido** do enum; `PlayerStateMachineTests` passou a exercitar a FSM por `Esquivando` (os testes eram de comportamento genérico, não do Salto).
- **`PlayerMovement`** perdeu o campo de bridge, o vetor de dash, a ação de input, a troca de layer para intangibilidade e o `HandleFsmStateChanged` (que só existia para restaurar a layer pós-Salto).
- **`PatuaPickup`** não chama mais `DesbloquearSalto`; ficou com um `TODO(design)` marcando onde o novo efeito entra, e mensagem nova.
- `GameManager`, `EsquivaBridge`, `Esquiva` e 3 ferramentas de Editor tiveram as referências limpas. **`IAnomalyPower` preservada** (decisão explícita).
- **Não mexido:** a layer `AnomalyBarrier` e o conceito de parede anômala no `LevelBlockoutPlanner`/`Generator` — vivem no **blockout aposentado**, que não gera nada na cena viva. Mexer ali seria risco sem retorno.

### Limpeza do legado das Ruínas
- **`LimparLegadoDasRuinas.cs` (novo, Editor):** remove por tipo e **relata item a item** no Console; não salva a cena, para conferência humana antes. Removeu 7 objetos: `TempestadeAmbiente`, `TempestadeTrigger_Z2_Rajadas`, `VeuTempestade` (overlay no HUD), os 3 `TempestadeTrigger_*` de zona, e o `Trigger_QuedaZ4Z5` (que acumulava `QuedaZ4Z5Trigger` + `CercoZ4Cutscene`).
- Preservados de propósito: Cultistas, baú, Abdul, Pedras de Poder, HUD, GameManager, câmera, colapso, patuá e o chão/colisão isométricos.

## 2026-07-29 — Tumba jogável: arte do Abdul, Câmara do Baú e arena povoada

Fecha a última fatia do roadmap do Vertical Slice. QA: **258/258 testes EditMode verdes**.

### Arte do Abdul (import corrigido + fatiamento)
- O spritesheet `abdul_alhazred_spritesheet.png` estava **fora do padrão de pixel art**: PPU 100 (padrão é 32), **filterMode Bilinear** (saía borrado), compressão ligada nas 4 plataformas, e a folha não fatiada (1 sprite cobrindo os 1024×1024). Corrigido para PPU 32 / Point / sem compressão.
- **`SliceSpritesheetAbdul.cs` (novo, Editor):** fatia a folha em **28 frames nomeados por animação**, batendo com os estados da `AbdulFSM` — `transe`, `flutuar`, `cone_de_gelo`, `invocar`, `dissolver`, `dano`, `derrota`. A arte que o Vini fez já cobre exatamente a FSM, incluindo a queda de joelhos soltando o livro (o drop do Necronomicon). Pivot em (0.5, 0.1) — pés no chão, exigido pelo Y-sorting do projeto. Cuidado documentado no código: Aseprite conta Y de cima, Unity de baixo (errar espelha a folha).

### Layout: Câmara do Baú e renomeação da arena
- **`Zona9_TronoDoVulto` → `Zona9_TumbaDeAbdul`** (pendência que o lore registrava desde 2026-07-28).
- **`Zona6b_CamaraDoBau` (nova):** sala lateral a Leste da Cripta dos Primeiros, logo após a entrada da dungeon, guardando o baú. Decisão do Vini: o jogador precisa sair armado antes dos Cultistas do caminho. **Pendurada de lado de propósito** — inseri-la na descida deslocaria as Zonas 7-9 e jogaria os 41 Cultistas e waypoints já posicionados dentro de paredes. A ligação Leste↔Oeste reusa `MakeOverlapDoorway` medindo a sobreposição em Y (o helper é matemática 1D, serve para os dois eixos).
- **Testes do planner atualizados:** 10 salas (era 9), nomes na ordem nova, e **2 testes novos** que travam justamente o que importa: a câmara é lateral (mesmo Y da Cripta, X maior) e a descida segue alinhada em X com a Zona 5 — ou seja, nada foi deslocado.

### Cena povoada
- **`SetupArenaDoAbdul.cs` (novo, Editor):** posiciona Abdul (com o sprite `transe` e a `Ficha_Abdul`), **4 Pedras de Poder** nos cantos da arena (forçam deslocamento sob pressão dos esqueletos, que é o ponto da Fase 1) e o **Baú** na Câmara. As posições vêm do `LevelBlockoutPlanner`, não de coordenadas chutadas — se o layout mudar, rodar de novo recoloca tudo. Idempotente. Executado em `Playtest_RuinasPalidas`: Abdul na layer Enemy (as armas o acertam), com `DynamicYSort`.
- Baú e Pedras usam sprite placeholder (quadrado colorido) até a arte real existir — visível e funcional, não invisível.

## 2026-07-29 — Vertical Slice: morte do Damião, HUD completo, baú RNG e boss Abdul

Empurrão grande no roadmap do Vertical Slice (fechar a Tumba de Alhazred). QA: compilação
limpa, **256/256 testes EditMode verdes** (subiu de 218). Escopo do VS reafirmado: loot com
raridade/níveis fica para **depois** da entrega (ver `CLAUDE.md` §1.1).

### Morte corpórea do Damião
- **`FrasesDeColapso`:** novo enum `TipoDeDerrota` (Mental/Corpórea) e pool próprio de frases para morte física — morrer de porrada não diz "você abraçou Hastur". `Sortear()` sem argumento segue sendo Mental (compatível).
- **`SequenciaDeColapso.Tocar(tipo)`:** escolhe o pool conforme a causa.
- **`GameManager`:** observa `VitalidadeBridge.OnAbatido` → `GameState.Colapso` marcado como derrota corpórea. `DefinirInvulneravel` agora propaga para a `VitalidadeBridge` (`IgnorarDano`), para Damião não morrer de porrada no meio de uma cutscene roteirizada.

### HUD completo
- **`VitalidadeBar.cs` (novo):** barra da Vitalidade corpórea, mesmo contrato da `ResilienciaBar` (evento `OnChanged`, sem polling), com cor crítica e cor de abatido.
- **`BarraDeAcoes.cs` (novo):** barra de ações da Mão Física — arma empunhada, habilidade e **recarga** da habilidade. Antes disso a habilidade disparava às cegas.
- **`MaoFisicaBridge`:** novos `ProgressoCooldownHabilidade`, `HabilidadePronta` e evento `OnArmaTrocada` (a UI se redesenha por evento, não por polling).
- **`HUDController`:** `InjetarVitalidade` e `InjetarMaoFisica`; `GameManager` alimenta os dois no bootstrap.
- **`BuildHUDCompleto.cs` (novo, Editor):** monta o HUD na cena aberta e liga as views. Construir por código (não YAML na mão) deixa a Unity resolver anchors/fonte — mesmo padrão dos outros builders da pasta. Executado em `Playtest_RuinasPalidas`: o HUD já existia como instância de prefab e foi reaproveitado.

### Golpe desarmado
- **`MaoVazia.cs` (novo, Core):** o golpe de mão vazia como `IArma` com **dano 0** — a regra vive no Core, não no adaptador. `MaoFisicaBridge.TryAtacar` agora aceita desarmado (entra em Atacando, faz barulho, não mata).

### Baú da Tumba
- **`SorteioDeArmaDaTumba.cs` (novo, Core):** enum `ArmaDaTumba` + sorteio uniforme com RNG injetável + fábrica das 3 armas. 7 testes, incluindo um que garante que **as três armas têm habilidade útil contra boss** (o baú é RNG, nenhuma pode ser obrigatória).
- **`BauDaTumba.cs` (novo, Runtime):** trigger que sorteia e equipa (padrão do `PatuaPickup`), com opção de forçar arma para teste.

### Boss Abdul Alhazred
- **`AbdulState.cs` / `AbdulFSM.cs` (novos, Core):** FSM da luta — Transe → Fase 1 (escudo sustentado por Pedras de Poder, invoca esqueletos) → Fase 2 (escudo permanente, Cones de Gelo, mana limitada) → Exausto (janela do golpe de misericórdia) → Derrotado (drop do Necronomicon). **13 testes**, cobrindo que Pedras só valem na Fase 1 e que a exaustão é a única abertura na Fase 2.
- **`AcumuloDeCongelamento.cs` (novo, Core):** 3 acúmulos de Cone de Gelo congelam Damião; acúmulos **expiram** (é "não leve três seguidos", não punição inevitável). **10 testes**.
- **`AbdulAlhazredAI.cs` (novo, Runtime):** adaptador — `EhAparicaoPrimordial` = true (imune a crítico furtivo), dano só passa se `PodeReceberDano`, invocações/cones/drop tolerantes a prefab ausente.
- **`PedraDePoder.cs` (novo, Runtime):** cenário destrutível que derruba o escudo da Fase 1.
- **`Ficha_Abdul.asset` (novo):** Vitalidade 300, Defesa 5 (baixa de propósito: a mitigação é subtrativa e defesa alta puniria desproporcionalmente o Estilete).

### Documentação
- **`systems/boss_abdul.md` (novo)** + índice: FSM, transições, atributos e o estado real de implementação (incluindo o que falta).

### Gaps declarados (não escondidos)
- **Sangramento nunca é aplicado:** `ArmaResult.SangramentoPorSegundo` existe mas nenhum receptor consome — a identidade do Estilete de Irem depende disso.
- **Congelamento não está ligado ao Damião:** o POCO está pronto e testado, mas falta o componente no jogador, o prefab do Cone de Gelo e um estado "congelado" no `PlayerState` (decisão de design pendente).
- **Arte pendente:** baú (fechado/aberto), esqueleto, Cone de Gelo, Necronomicon, escudo, sprite do Abdul.

## 2026-07-29 — Revisão de governança: CLAUDE.md, pipeline de QA e sync de skills

Revisão pedida pelo Vini sobre se o `CLAUDE.md` estava "jogando contra a gente". Três achados de atrito real (não hipotético) desta sessão, todos corrigidos:

1. **Drift real entre `.claude/skills` e `.agents/skills`:** `favela-pixelart-standards` estava divergente — lado `.claude` ainda mandava PPU 16 (valor antigo), lado `.agents` já tinha PPU 32 (decisão de 2026-07-28, documentada em `favela-isometric-standards`, que estava sincronizado nos dois lados). Corrigido: `.claude` agora diz 32, ambos os arquivos ficam byte-idênticos. Os outros 3 pares de skills (`lore-enforcer`, `isometric-standards`, `qa-pipeline`) já estavam sincronizados.
2. **Pipeline de QA se contradizia:** a skill mandava rodar `tools/run_qa_tests.ps1` (batch mode), mas esse script falha com "another Unity instance running" quando o Editor está aberto — e o fluxo real usa o MCP `mcp-unity`, que exige o Editor aberto. `favela-qa-pipeline.md` reescrita (nos dois lados) para assumir MCP como caminho principal, com uma seção documentando a flakiness conhecida do bridge (timeout 120s, "Connection failed", resultado `0/0` ambíguo na 1ª chamada pós-recompile) e a orientação de repetir antes de tratar como falha real. O script batch vira caminho alternativo só para quando o usuário pedir uma rodada "limpa" ou o MCP estiver genuinamente indisponível — e só com o Editor fechado.
3. **Enquadramento de gênero desatualizado:** a frase "combate é alternativa quando o stealth falha" não refletia mais o peso real do sistema (ficha de 5 atributos, mitigação, armas com habilidade própria) construído nesta sessão. Seção 1 do `CLAUDE.md` atualizada para reconhecer o combate aberto como pilar sistêmico próprio.

**CORREÇÃO (minutos depois, mesma sessão):** o item 3 acima, na primeira versão, também escreveu que "loot com raridade/afixo já foi rejeitado" — **errado**. A fonte real (memória de sessão anterior) só dizia "quero avaliar depois, não decidido"; o assistente resumiu errado ao endurecer isso pra "rejeitado" no banner do `combate.md` mais cedo hoje, e o erro se propagou pro `CLAUDE.md` e pro item 3 desta entrada. O Vini corrigiu na hora: **loot, raridade e níveis de personagem estão confirmados** — só com escopo deliberadamente contido (nada de profundidade de build tipo Path of Exile/Last Epoch). `CLAUDE.md` §1 ganhou um bullet próprio para isso; `combate.md` teve o banner corrigido; nova entrada abaixo documenta a decisão real.

## 2026-07-29 — Loot, raridade e níveis: confirmados (com escopo contido)

Correção de rumo: um registro anterior desta mesma sessão (ver acima) tinha mischaracterizado a raridade/loot como "rejeitada". O Vini esclareceu: **vai ter** drop de item, raridade e progressão por nível — o que ele não quer é a profundidade de build de um Path of Exile ou Last Epoch ("não é para ser em demasia sem controle, com um monte de builds diferentes").

### Documentação
- `CLAUDE.md` §1: novo bullet "Loot, raridade e níveis de personagem" — **previstos**, escopo contido, sem data, forma exata (nº de raridades, afixos, curva de nível) ainda não desenhada, não implementar sem confirmar.
- `Docs/KnowledgeBundle/systems/combate.md`: banner de divergência corrigido — a parte que segue fora de escopo é o sistema de *Priming*/famílias/hibridização daquele documento, não a existência de raridade em si.
- Memória do projeto: `arquitetura-sistemas-internos-2d-arpg.md` atualizada (o item estava "aberto", agora resolvido); nova memória dedicada `loot-raridade-niveis-planejado.md` (mesmo padrão de `inventario-planejado.md`/`arvore-habilidades-planejada.md`); `revisao-governanca-claude-md-2026-07-29.md` corrigida com nota de autocorreção.

## 2026-07-29 — Combate de mão dupla: fichas de atributos, dano visível e fix de layer

Fecha o loop de combate: o Cultista agora **revida**, o dano é **mitigado por defesa nos
dois lados** e **aparece na tela**. QA: recompilação limpa, **218/218 testes EditMode verdes**.

Decisões de design (Vini, 2026-07-29): **toda unidade tem ficha de atributos** (não só o
Damião), autorada como **ScriptableObject**; ficha com **5 atributos** incluindo
**Resistência Anômala** (defesa mágica); **números de dano flutuantes** como verificador
visual enquanto não há animações.

### Core
- **`FichaDeAtributos.cs` (novo):** POCO imutável com `VitalidadeMax`, `Ataque`, `Defesa`, `Conjuracao`, `ResistenciaAnomala`. Dois canais: físico (Ataque→Defesa→Vitalidade) e anômalo (Conjuração→ResistênciaAnômala→Resiliência Mental).

### Runtime
- **`FichaAtributosConfig.cs` (novo):** `ScriptableObject` (`[CreateAssetMenu]`) que autora a ficha no Inspector e produz o POCO via `CriarFicha()`, com clamp defensivo.
- **`VitalidadeBridge.cs` (novo):** componente de vitalidade de um ator de cena (Damião hoje, boss depois). Ponto único da mitigação por Defesa no lado recebedor; implementa `IDanificavel`; eventos `OnDanoSofrido`/`OnAbatido`.
- **`DanoFlutuante.cs` (novo):** número de dano em world space que sobe e desvanece. Usa `TextMesh` legado de propósito (world space sem Canvas nem assets importados). **Gotcha Unity 6:** a fonte built-in é `LegacyRuntime.ttf` — o antigo `Arial.ttf` faz `GetBuiltinResource` **lançar** `ArgumentException`; blindado com try/catch para o diagnóstico nunca derrubar o combate.
- **`CultistaAI.cs`:** consome `Ficha_Cultista` (substitui o `vitalidadeMax` solto); aplica a **própria Defesa** ao golpe recebido; detecta proximidade do Damião por `OverlapCircle` na camada Player (buffer pré-alocado + `ContactFilter2D`, zero alocação por frame) e alimenta `AtualizarAlcanceDoAlvo`; traduz `OnGolpeDesferido` em dano no alvo; cor própria no estado Atacar; gizmo do alcance de golpe.

### Assets
- **`Ficha_Cultista.asset` / `Ficha_Damiao.asset` (novos)** em `FavelaAmarela/Config/` — Cultista (Vit 100/Atq 24/Def 5), Damião (Vit 100/Atq 0 desarmado/Def 4). Balanceamento agora é edição de asset, sem tocar código.
- **`Cultista.prefab`:** ficha atribuída (campo novo, sem conflito com as 41 instâncias da cena).
- **`Player_Damiao.prefab`:** `VitalidadeBridge` adicionado com `Ficha_Damiao`.

### Correção de bug
- **Damião estava na layer `Default` (0), não `Player` (8)** — no prefab e na cena. Isso quebraria a detecção de proximidade do Cultista (que filtra pela camada Player), ou seja, **o inimigo nunca atacaria**. Corrigido para layer 8. Auditado antes: a matriz de colisão 2D é toda permissiva (nenhuma colisão muda) e **todo o resto identifica o jogador por tag** (`CompareTag("Player")`), não por layer — nenhum trigger afetado.

### Testes
- **`FonteBuiltinTests.cs` (novo):** 2 testes que transformam o gotcha da fonte em falha de CI (o nome antigo lança exceção — comportamento comprovado empiricamente, não presumido).
- **`FichaDeAtributosTests.cs` (novo):** 5 testes (round-trip dos 5 atributos, defaults, validações, integração ficha→mitigação).

### Documentação
- **`systems/ficha_de_atributos.md` (novo)** + entrada no índice: os 5 atributos, a fórmula, a tabela de balanceamento e as contas de golpes-para-abater.
- **`systems/cultista_ai.md`:** atualizada de 4 para 5 estados (novo Atacar, com regras de telegrafo e proximidade-não-visão) e **corrigidas referências obsoletas à Barra Enferrujada**, removida na Fatia 2b.

## 2026-07-29 — Estado Atacar do Cultista + fórmula de defesa + escala unificada

Continuação do fechamento do combate. Fundação POCO da parte 2 (o inimigo revidar) e a
"conta" da defesa, com os números batidos com o Vini. QA: recompilação limpa, testes verdes
(MitigacaoDeDano 8/8, CultistaFSMAtaque 6/6, ArmasDaTumba 7/7 após re-tune).

Decisões de design (Vini, 2026-07-29): defesa **subtrativa com piso** (`max(bruto×0,15, bruto−defesa)`);
Damião desarmado aguenta **5 golpes** de Cultista; **escala unificada 0–100** para todos os atores.

### Core
- **`CultistaState.cs`:** novo estado `Atacar` (entre `Caca` e `Atordoado`).
- **`CultistaFSM.cs`:** estado `Atacar` — entra de `Caca` por proximidade (`AtualizarAlcanceDoAlvo`, alimentado pelo Runtime, **sem visão**), desfere golpe por cadência (`OnGolpeDesferido`, default 1,2 s), volta a `Caca` fora do alcance, interrompível por atordoamento. Timer reinicia ao (re)entrar.
- **`MitigacaoDeDano.cs` (novo):** função pura da fórmula subtrativa com piso — a "conta" da defesa isolada e testável. Simétrica: serve para inimigos também.
- **Armas re-tunadas ×5 (escala 0–100):** Cravo 40/30, Estilete 25/15 (+15/s sangramento), Alfanje 60/40. Cultista `vitalidadeMax` 20→100 (prefab não serializava o campo, pega o novo default).

### Testes
- **`CultistaFSMAtaqueTests.cs` (novo):** 6 testes do estado Atacar (transição por proximidade, cadência, telegrafo do 1º golpe, volta a Caça, interrupção por atordoamento, reset do timer).
- **`MitigacaoDeDanoTests.cs` (novo):** 8 testes da fórmula (golpe 24 vs defesa 4 = 20, piso domina em defesa alta, defesa negativa, nunca excede o bruto etc.).

## 2026-07-29 — Vitalidade corpórea + morte do Cultista (combate peça 1)

Início do fechamento do loop de combate da Tumba de Alhazred. A Fatia 2b entregou a
"encanação" (armas POCO, `IDanificavel`, `MaoFisicaBridge`), mas nada consumia o dano: o
`CultistaAI.ReceberGolpe` ignorava `ArmaResult.Dano` de propósito e nenhum inimigo tinha
vida. Esta rodada fecha o elo "bateu → tirou vitalidade → foi abatido". QA: recompilação
limpa (0 erros/0 warnings via MCP), 10/10 testes EditMode da `Vitalidade` verdes.

Decisões de design registradas (Vini, 2026-07-29): dano físico do combate aberto tira uma
**barra de vida corpórea nova** (não a Resiliência Mental); o Cultista ganha um **estado de
Ataque na FSM** (peça 2, ainda por fazer); o **golpe desarmado deve existir mas com dano 0**.

### Core
- **`Vitalidade.cs` (novo):** POCO em `Core/Combat` espelhando `ResilienciaMental` — `Max`/`Atual`/`Percentual`, `Ferir`/`Curar`/`Restaurar`, `EstaAbatido`, evento `OnChanged` com `readonly struct VitalidadeChangedArgs` (flag `AcabouDeAbater` de disparo único). Vida física reutilizável por Cultista, Damião e Abdul.

### Runtime
- **`CultistaAI.cs`:** ganhou `vitalidadeMax` (Inspector, default 20) e uma `Vitalidade` interna; `ReceberGolpe` agora consome `resultado.Dano` e, ao ser abatido, remove o Cultista de cena (`Abater` → `Destroy`). Mantém a reação ao atordoamento. Desinscrição do evento no `OnDestroy`.

### Testes
- **`VitalidadeTests.cs` (novo):** 10 testes EditMode — construção/validação, `Ferir` com clamp e abate, disparo único de `AcabouDeAbater`, `Curar`/`Restaurar`, payload do `OnChanged`.

### Documentação
- **`systems/vitalidade.md` (novo)** + entrada no `systems/index.md`.
- **`systems/combate.md`:** banner de divergência no topo — o doc é uma visão aspiracional maximalista (famílias de dano/Priming/loot) que **não** corresponde ao combate enxuto decidido para a Tumba.

## 2026-07-28 — Fundação code-first do mapa do Deserto de Hali (v0)

Fundação em código do overworld da Fase 1. QA: compilação limpa, testes EditMode 8/8 verdes. (A Fatia 1 "Vitória→TransicaoDeFase" está registrada mais abaixo, por outra rodada.)

> **Nota de alinhamento:** este planner **v0** foi escrito antes de `systems/level_design_deserto_hali.md` (autorado em paralelo). Ainda **não** implementa a topologia de lá — 5 setores, Lago de Hali central impassável, ~22×16, posições de POI por bússola. Alinhamento a esse doc é a próxima fatia antes de gerar a cena definitiva.

### Core
- **`DesertOverworldTypes.cs` (novo):** `DesertOverworldConfig` (DTO), `DesertOverworldLayout`, `PointOfInterestSpec` + enum `PointOfInterestKind` (PlayerSpawn, EntradaTumbaAlhazred, EntradaTemploSerpente, SantuarioYhtill, PortoesDasRuinas). Reaproveita `WallSpec`/`FloorSpec` do blockout de salas.
- **`DesertOverworldPlanner.cs` (novo):** POCO irmão do `LevelBlockoutPlanner` — monta o overworld (chão + perímetro + pontos de interesse posicionados). Agnóstico de Unity/PPU.

### Runtime
- **`PortalDeCena.cs` (novo):** porta de trigger que carrega uma cena por nome via `SceneManager.LoadScene` — carregamento mínimo e pontual (não é a infra completa de multi-cena). Usada na entrada da Tumba para levar ao S-Path.

### Editor
- **`BuildDesertOverworld.cs` (novo):** menu `Tools/FavelaAmarela/Build Desert Overworld` — gera do zero `Assets/Scenes/Deserto_Hali.unity` jogável (câmera-seguidora, GameManager, prefab do Damião no spawn, chão-tilemap de areia, limites e marcadores dos pontos de interesse; a entrada da Tumba vira `PortalDeCena`→Playtest_RuinasPalidas). Registra as duas cenas em Build Settings.

### Testes
- **`DesertOverworldPlannerTests.cs` (novo):** 8 testes EditMode — estrutura (1 chão + 4 limites), 5 pontos de interesse sem categoria duplicada e dentro do chão, portais com/sem cena destino, robustez do perímetro.

## 2026-07-28 — Correções narrativas e estruturais do GDD (documentação)

### Decisões de design registradas

- **Origem canônica de Damião:** Damião é o personagem do curta-metragem *Favela Amarela* (Richard Abelha). No final do curta, ele morre na Terra. O jogo começa imediatamente após — ele acorda em Carcosa, cercado por Espectros de Hali, no Deserto das Cinzas. A sinopse anterior ("viaja até a favela para decifrar inscrições") foi removida — essa é a premissa do curta, não do jogo.
- **Universo multimídia:** O jogo faz parte de um universo multimídia em desenvolvimento concomitante: curta (existe), animação, HQ e longa-metragem. O jogo é a fatia dedicada ao encontro com o Rei em Amarelo. Fragmentos de lore na Tumba de Alhazred conectam diretamente com o trabalho audiovisual.
- **Progressão da Fase 1 corrigida:** Exploração livre do overworld como estrutura principal. Ordem dos eventos: (1) Tumba de Alhazred → Boss Abdul Alhazred → Drop Necronomicon; (2) Santuário de Yhtill → Quest Cassilda → Drop Patuá; (3) Templo da Serpente (opcional) → 2 bosses → Drop set lendário; (4) Portões das Ruínas → Miniboss Byakhee → Transição para Fase 2.
- **Templo da Serpente — 2 bosses:** A Dungeon 2 tem dois guardiões (nomes TBD), cada um dropando uma peça de um set de equipamento lendário. O set completo desbloqueia uma sidequest exclusiva dentro do Castelo de Carcosa (Fase 2 do demo, Fase 6 do jogo completo).
- **Patuá reposicionado:** O Patuá (Patuá das Luas Gêmeas) é a recompensa da conclusão do puzzle/quest narrativo da Rainha Cassilda no Santuário — não é um drop de combate. O `PatuaPickup.cs` da Zona 5 que destrava o Salto Dimensional é um item diferente (rename para "Fragmento de Hali do Salto" pendente na Fatia 3 do roadmap).
- **Cassilda adicionada como personagem principal:** Cassilda entra formalmente em §2.4 do GDD como NPC de quest. Abdul Alhazred também entra formalmente como personagem.
- **Castelo de Carcosa — escopo:** Demo/Vertical Slice: Castelo = Fase 2, chefe final = Rei em Amarelo. Jogo completo: Castelo = Fase 6 (última fase), mesmo confronto.

### Documentação atualizada

- `GDD_Mestre.md`: §1.1 (Elevator Pitch), §1.8 (Escopo/Universo multimídia), §2.1 (Sinopse — reescrita completa), §2.4 (Personagens — Damião, Vance, Rei em Amarelo, Cassilda, Alhazred), §2.7 (Progressão Narrativa — reescrita com estrutura correta).
- `lore/templo_da_serpente.md`: Atualizado de stub para design parcial com 2 bosses, set lendário e mecânica de descoberta por drop.
- `lore/reliquias_cosmicas.md`: Coroa agora é "Peça A do set lendário do Templo"; Patuá agora tem nota de reposicionamento (quest de Cassilda) e distinção do `PatuaPickup.cs` existente.

### Pendências restantes desta rodada

- Nomes dos 2 guardiões do Templo da Serpente (TBD). *(Resolvido: Naga e Avatar de Set)*
- Nome do segundo drop do set lendário (Peça B — além da Coroa). *(Resolvido: O Set de Set possui 4 peças - Elmo, Peitoral, Grevas, Arma)*
- Conteúdo da sidequest do Castelo que o set desbloqueia. *(Resolvido: Luta contra Avatar de Nyarlathotep)*
- Papel narrativo completo de Professor Alistair Vance (guia ou traidor). *(Resolvido: Removido, confusão com Sub Mask Net_Dead)*
- O que é o objeto narrativo da Garganta? *(Resolvido: 1ª página do diário de um nobre de Yhtill, quest de Cassilda)*
- Rename do `PatuaPickup.cs` para "Fragmento de Hali do Salto" (Fatia 3 do roadmap de código).

---

## 2026-07-28 — Level Design do Overworld: Deserto de Hali (documentação)

### Decisão de design

Criado o documento de Level Design do overworld da Fase 1 (`systems/level_design_deserto_hali.md`), definindo o conceito do espaço, mapa topológico, zonas de tempestade de areia, pontos de interesse, filosofia de inimigos errantes e referências visuais. Nenhum código alterado nesta rodada.

### Decisões de design registradas

- **Mapa topológico:** 5 setores geográficos separados pelo Lago de Hali (barreira natural impassável). Setor sul: Entrada + Tumba de Alhazred. Setor norte: Santuário de Yhtill + Portões das Ruínas. Setor leste (oculto): Templo da Serpente.
- **Tempestade relocada:** A tempestade de areia sai da Dungeon 1 (Tumba de Alhazred) e passa a pertencer ao Overworld do Deserto de Hali, com zonas de intensidade variável por setor. A Dungeon 1 ficará com StormIntensity = 0 permanente. Os triggers `Z1_Spawn`, `Z2_Rajadas`, `Z3Z4_Forte` em `Playtest_RuinasPalidas.unity` são candidatos a remoção em fatia futura de código.
- **Tempestade como stealth invertido:** No overworld, a tempestade forte abafa completamente os passos de Damião (stealth passivo) e cega os Byakhee — mas também drena RM na zona máxima (leste) e oculta o caminho do Templo da Serpente.
- **Inimigos no overworld:** Cada inimigo tem função de design específica (sem spawns genéricos). Cultistas errantes em pares no deserto central, Byakhee como sentinelas aéreos, Coisa do Cemitério como foreshadowing de terror, Sementes de Hastur na borda do Lago, Cultista do Templo como guia opcional. Zona de Entrada e Santuário ficam livres de inimigos (zonas de respiro).
- **3 formas de descoberta do Templo da Serpente:** Necronomicon reage ao ambiente, marco visual na tempestade, Cultista especial com mapa fragmentado. Quantas implementar: decisão pendente.
- **Dungeon 1 como atalho geográfico:** Damião entra na Tumba pelo sul e emerge por uma saída diferente mais ao norte — a dungeon encurta o caminho além de ser narrativa. Coordenada exata de emergência: TBD.

### Documentação atualizada

- `systems/level_design_deserto_hali.md`: criado (novo).
- `systems/environment.md`: adicionadas notas de decisão de relocação da tempestade para o overworld nos pontos §Tempestade de Areia e §Zoneamento.
- `lore/deserto_e_dungeons.md`: seção §1 expandida com Lago de Hali, variação por setor e link para o novo doc.

### Pendências sinalizadas (sem código)

- Sequência de abertura (como Damião chega à Garganta de Pedra Pálida) — GDD §2.7.
- Condicional do Santuário (Cassilda exige Necronomicon? Cria dependência implícita de D1).
- Ponto exato de emergência da Tumba de volta ao overworld.
- Quantidade de formas de descoberta do Templo da Serpente.
- A Coisa do Cemitério no overworld: antes ou depois da Dungeon 1?

---

## 2026-07-28 — Fatia 1 do roadmap: "Vitória" → "TransicaoDeFase" (código)

Primeira fatia de código da reestruturação. Remove o estado terminal de "Vitória" da FSM do game loop, substituindo-o por uma transição de fim de fase/dungeon (RPG multi-fase, não roguelike — decisão registrada em `escopo-6-fases-sem-vitoria`). Também travadas duas decisões de conteúdo do Vini: o pickup existente vira "Fragmento de Hali do Salto" (rename de código pendente da Fatia 3) e o miniboss da Zona 9 é o Abdul Alhazred (o "Vulto" genérico não será implementado).

### Core
- `GameLoopStateMachine.cs`: `GameState.Vitoria` → `GameState.TransicaoDeFase`; grafo de transições e XML docs atualizados. Gameplay→TransicaoDeFase e TransicaoDeFase→Menu continuam válidas (mesmas arestas do antigo Vitoria).

### Runtime
- `GameManager.cs`: `TriggerVitoria()` → `TriggerTransicaoDeFase()`; campo serializado `telaVitoria` → `telaTransicaoDeFase`; lógica de timescale/telas ajustada.
- `VitoriaTrigger.cs` removido; criado `TransicaoDeFaseTrigger.cs` (mesmo padrão de trigger, agora reaproveitável em qualquer ponto de saída, ex. Portões das Ruínas). Verificado antes: o trigger antigo não estava anexado a nenhum GameObject e `telaVitoria` era referência nula nas duas cenas — rename sem perda de dado.
- Cenas `Playtest_RuinasPalidas.unity` e `cena_1.unity`: campo serializado do GameManager atualizado (era `{fileID: 0}` nas duas).

### Testes
- `GameLoopStateMachineTests.cs`: +3 testes (Gameplay→TransicaoDeFase válida, TransicaoDeFase→Menu válida, TransicaoDeFase→Gameplay rejeitada).
- QA: Runtime e Tests.EditMode compilam sem erros/avisos; **suíte EditMode 174/174 verde**.

### Documentação
- `systems/game_loop.md`, `architecture/dependency_map.md`, `scripts/runtime/game_manager_cs.md`, `scripts/runtime/index.md` atualizados; `scripts/runtime/vitoria_trigger_cs.md` → `transicao_de_fase_trigger_cs.md`.
- `lore/abdul_alhazred.md` e `systems/abilities.md`: pendências de conteúdo Vulto/Alhazred e patuá marcadas como resolvidas.

## 2026-07-28 — Reestruturação Fase 1 (Deserto de Hali) — documentação

### Decisão de design
A área jogável já construída (`Assets/Scenes/Playtest_RuinasPalidas.unity`, Zonas 1-9) passa a ser a **Dungeon 1 (Tumba de Alhazred)** dentro de uma nova **Fase 1: "O Deserto de Hali"** — overworld aberto (32 PPU) com duas dungeons, o Santuário de Yhtill (quest de Cassilda) e os Portões das Ruínas (checkpoint de saída). O que era "Fase 3: Castelo de Carcosa" no GDD de expansão renumerou para "Fase 2". Nenhum código foi alterado nesta rodada — só reconciliação de documentação (OKF) antes de qualquer implementação, conforme CLAUDE.md §3.1.

### Lore (`Docs/KnowledgeBundle/lore/`)
- `reliquias_cosmicas.md`: "Portões de Carcosa" → "Portões das Ruínas"; Coroa renomeada de "Coroa da Serpente" (Valusia) para **"Coroa de Ossos do Rei em Amarelo"**, obtenção mantida (drop do Chefe Guardião do Templo da Serpente), efeito marcado como "a definir" (roteiro em fechamento, sem especular habilidades de item).
- `deserto_e_dungeons.md`: seção 3 antiga ("Dungeon 2: Portões de Carcosa", que colocava o Byakhee como guardião de dungeon) dividida em "Dungeon 2: O Templo da Serpente (Opcional)" + "Portões das Ruínas (Fim da Fase 1)"; nota de que a Tumba de Alhazred = área já implementada.
- Novo stub `templo_da_serpente.md`: rascunho da Dungeon 2, localização/inimigos/nome do Guardião ainda TBD.
- `abdul_alhazred.md`: nota de localização + pendência sinalizada (Zona9_TronoDoVulto/"Vulto" sem relação escrita com Alhazred ainda).
- `cassilda_e_byakhee.md`: mesma renomeação de "Portões de Carcosa".
- `index.md`: linkados os 4 arquivos de lore que existiam mas não estavam no índice, + o novo stub.

### GDD (`Docs/KnowledgeBundle/`)
- `gdd_expansao_deserto_demo.md` (v3.0→v4.0): "3 Fases"→"2 Fases"; Fase 2 antiga (Ruínas Pálidas) fundida no bullet da Tumba de Alhazred; Fase 3→Fase 2; tabela de relíquias com efeitos marcados "a definir".
- `GDD_Mestre.md`: §1.8 (Escopo), §2.7 (Progressão Narrativa), §4.2 (Fluxo de Telas/diagrama) e §5.1/§5.4 (Level Design) atualizados para refletir que Ruínas Pálidas não é mais "Fase 1" e sim a Dungeon 1; §5.2 e §5.4 marcam PPU 32 e lista de zonas como parcial (1-5; 6-9 existem no blockout, não documentadas ainda).

### Systems e Skills
- `systems/abilities.md`: nota de colisão de nome entre o patuá existente (`PatuaPickup.cs`, destrava Salto) e a nova relíquia "Patuá das Luas Gêmeas".
- `systems/level_design.md`: PPU 16→32; nota de que é a Dungeon 1 agora.
- `.claude/skills/favela-isometric-standards/SKILL.md` e espelho em `.agents/`: PPU padrão do projeto 16→32.

### Memória (Claude Code)
- `escopo-6-fases-sem-vitoria.md`: marcado como superado no escopo da demo; ambição de 6 fases mantida como horizonte separado, não reconciliado.
- Nova memória `fase1-deserto-hali-multi-cena-32ppu.md`: registra as decisões desta rodada e o roadmap de código futuro (fatia por fatia — infra multi-cena, `SaveData` estendido, pickup genérico de relíquia, repropriação da cena, overworld do deserto, Templo da Serpente, Santuário de Yhtill, Portões/Byakhee).
- Notas de esclarecimento em `roadmap-camera-combate-urp.md` e `ia-inimigos-percepcao-graduada-fase2.md` (usam "Fase 2" no sentido antigo).

## 2026-07-27 — Placeholders jogáveis integrados (Damião, inimigos, patuá e arma)

### Arte / Placeholders (favela-pixelart-standards: PPU 16, Point, None)
- **Damião:** sprite idle `Damiao_Robe_Idle` (32×48, derivado por downscale do `Damiao_Concept_Robe`, com preenchimento de buracos e key de fundo por saturação) atribuído ao `Player_Damiao`; `Transform.scale = 0.5` (tile de chão = 16px = 1 unidade → personagem ~1.5 tile, corrigindo o "gigante" de 3 tiles); virou `Player_Damiao.prefab` preservando a ref `SequenciaDeColapso.damiaoSprite`.
- **Cultista:** idle 16×32 extraído do `Cultista_Spritesheet_16x32` (limpeza: chroma-key do xadrez cinza opaco queimado + despeckle de respingos). `Cultista_Idle.png` importado (pivot pés); `Cultista.prefab` → sprite novo + `DynamicYSort` (fator 10) + scale 0.5. Verificado na cena (bounds 1×2, AI/waypoints intactos).
- **Espectro:** idle 24×48 do `EspectroHali_Spritesheet_24x48` (remoção da faixa de rótulo queimada no topo + despeckle). `EspectroHali_Idle.png` importado; `EspectroHali.prefab` → sprite + `DynamicYSort` + scale 0.5 (spawna via Cerco Z4, alpha controlado pelo `EspectroAI`).
- **Barra Enferrujada:** sprite placeholder 16×16 desenhado (`Barra_Enferrujada.png`) — não existia visual da arma.
- **Patuá:** substituído o placeholder quadrado-amarelo chapado por amuleto/trouxinha pixel art 16×16 (`Patua.png` sobrescrito mantendo o guid → propaga a todos os usos).

### Pickups (Runtime.GameLoop — prefabs autorados por YAML)
- `Patua_Pickup.prefab` e `Arma_Pickup.prefab` criados: `SpriteRenderer` + `BoxCollider2D` (trigger) + `PatuaPickup`/`ArmaPickup` + `DynamicYSort`, layer 0 (igual aos triggers que já funcionam), scale 0.5. Posicionados na Zona 5. Patuá destrava o Salto Dimensional; arma destrava a Mão Física. **Validados em Play.**

### Gotcha registrado
- Sprite **single-mode** referenciado por YAML resolve no `fileID: 21300000` — não no `internalID` do `.meta` (que pegou o Patuá até corrigir). Ver memória `single-sprite-fileid-21300000`.

### Pendências (próximos slices)
- Colliders dos inimigos ficaram pequenos (0.16 pós-scale 0.5) — tunar; normalizar tamanhos nativos (voltar tudo a scale 1.0 reautorando px); avaliar mover pickups pra layer `Pickup`; **reorg de hierarquia** (raiz plana → containers de transform identidade) como slice dedicado.

## 2026-07-27 — Spritesheets e Fatiamento via Aseprite MCP (16x32 e 24x48)

### Arte & Automação Aseprite (aseprite-bridge MCP)
- **Fatiamento Automático Lua:** Executado script via MCP `aseprite-bridge` (`execute_lua_script`) para processar e fatiar individualmente os spritesheets em arquivos `.aseprite` nativos e exportar mapas de sprites com JSON metadata.
- **Cultista Amarelo (16x32 px):** Gerado [Cultista_Sliced.aseprite](file:///c:/Users/Vini/Desktop/projeto_amarelo_unity/Assets/Sprites/Cultistas/Cultista_Sliced.aseprite), [Cultista_Sliced_Sheet.png](file:///c:/Users/Vini/Desktop/projeto_amarelo_unity/Assets/Sprites/Cultistas/Cultista_Sliced_Sheet.png) e [Cultista_Sliced_Sheet.json](file:///c:/Users/Vini/Desktop/projeto_amarelo_unity/Assets/Sprites/Cultistas/Cultista_Sliced_Sheet.json) com as animações de *Idle*, *Walk*, *Attack* e *Death* organizadas por Tags de 4 frames.
- **Espectro de Hali (24x48 px):** Gerado [EspectroHali_Sliced.aseprite](file:///c:/Users/Vini/Desktop/projeto_amarelo_unity/Assets/FavelaAmarela/Art/Enemies/EspectroHali_Sliced.aseprite), [EspectroHali_Sliced_Sheet.png](file:///c:/Users/Vini/Desktop/projeto_amarelo_unity/Assets/FavelaAmarela/Art/Enemies/EspectroHali_Sliced_Sheet.png) e [EspectroHali_Sliced_Sheet.json](file:///c:/Users/Vini/Desktop/projeto_amarelo_unity/Assets/FavelaAmarela/Art/Enemies/EspectroHali_Sliced_Sheet.json) com as animações de *Idle*, *Move*, *Attack* e *Death* organizadas por Tags de 4 frames.
- **Importação Unity (favela-pixelart-standards):** Metafiles configuradas com `PPU = 16`, `Filter Mode = Point` e `Compression = None`.

## 2026-07-17 — Profundidade isométrica (oclusão) + correções de gameplay

### Renderização isométrica (novo sistema)
- `DynamicYSort` (Runtime/Rendering) — atualiza `sortingOrder = -y*10` nos atores que se movem em `LateUpdate` (o pré-requisito que faltava; sem ele a profundidade não funcionava)
- Shader `FavelaAmarela/SpriteDitherOcclusion` (Built-in RP) — recorte dither Bayer 4x4 por `_DitherAmount`
- `OcclusaoDitherFade` (Runtime/Rendering) — nas paredes altas; detecta o jogador atrás (trigger + Y) e faz o fade do dither via `MaterialPropertyBlock`. Silhueta atrás de paredes altas provada em Play (grey-box)
- Documentado em `systems/renderizacao_isometrica.md` (novo) + `systems/index.md`

### Correções de gameplay
- **Invulnerabilidade de cutscene:** `GameManager.JogadorInvulneravel` — Coisa do Cemitério e `ColapsoTrigger` não matam Damião durante a queda Z4→Z5 (antes ela o insta-matava encurralado)
- **Tempestade na Z5:** `QuedaZ4Z5Trigger` zera a faixa explicitamente no teleporte (o teleporte adormece o rigidbody e o `Z5_Nula` não disparava); colliders `Z3Z4_Forte`/`Z5_Nula` realinhados na barreira (-30.25)
- **Cerco:** `CercoZ4Cutscene.LimparAtores()` destrói os inimigos instanciados após a queda (não persistem/perseguem na Z5)
- **Espectro:** `Rigidbody2D` Kinematic no `EspectroAI` — fantasma atravessa paredes (excludeLayers não bastava por causa do `ForceReceiveLayers` das paredes)
- Docs atualizados: `coisa_do_cemiterio.md`, `environment.md`, `espectro.md`, `queda_z4_z5.md`

## 2026-07-16 — Fase 1 Slice 4: esqueleto de Save
- Criado o POCO `SaveData` (`Core.Persistence`) — DTO `[Serializable]` de progresso (versão, resiliência, unlocks de Salto/Arma, posição); campos serializados camelCase
- Adicionado `ResilienciaMental.Restaurar(float)` (Core) — define o valor salvo (clampado, dispara `OnChanged`), reusando o `Alterar` privado; 6 testes novos
- Criado o adapter Runtime `SaveSystem` (`Runtime.GameLoop`) — IO JSON em `Application.persistentDataPath` (slot único `save.json`), defensivo, nunca `PlayerPrefs`
- Novo `SaveDataTests` (round-trip JSON via `JsonUtility`)
- Documentado em `systems/persistencia.md` (novo) + `systems/index.md`
- Fora de escopo (slice futuro): gatilho de save, orquestração no `GameManager`, múltiplos slots
- QA: testes EditMode rodados no Test Runner pelo Vini — todos verdes (152 anteriores + 8 novos)

## 2026-07-10 — Cena da Tempestade de Areia + Prefab da Coisa do Cemitério
- Criado o POCO `AgendadorDeRajada` (Core/Environment) — decide quando uma rajada forte acontece, RNG injetável (padrão de `BarraEnferrujada`); 6 testes NUnit
- Criado o adapter Runtime `TempestadeRajadaAleatoria` — variante do `TempestadeZonaTrigger` que tica o `AgendadorDeRajada` só enquanto o jogador está no trigger e alterna a faixa da tempestade entre calmaria e rajada
- Colocados 4 GameObjects de trigger na cena `Playtest_RuinasPalidas`: `TempestadeTrigger_Z1_Spawn` (moderada), `TempestadeTrigger_Z2_Rajadas` (calma + rajadas aleatórias), `TempestadeTrigger_Z3Z4_Forte` (forte, cobre as duas zonas contíguas), `TempestadeTrigger_Z5_Nula` (nula, subterrâneo)
- Criado o prefab `Assets/FavelaAmarela/Art/Enemies/CoisaDoCemiterio.prefab` (espelhando `Cultista.prefab`, com `Collider2D.isTrigger = true` — diferença crítica pro toque→Colapso) e posicionado na transição Zona2→Zona3
- Corrigida divergência no OKF: `coisa_do_cemiterio.md` ainda listava `CoisaDoCemiterioAI` como não implementada (já existia desde 2026-07-08)
- Adicionado o utilitário de Editor `WireStormTriggers.cs` (`Tools/FavelaAmarela/Wire Storm Triggers`) — mesmo padrão do `WireConfigAssets.cs`, contornando a limitação do MCP `update_component` pra referências de objeto de cena
- Recompile limpo + 152/152 testes EditMode verdes (baseline 146 + 6 novos)

## 2026-07-10 — Slice 3: assets de Config dos bridges do Player
- Criados os 4 assets `.asset` em `Assets/FavelaAmarela/Config/` a partir dos ScriptableObjects de config (`LocomocaoConfig`, `EsquivaConfig`, `SaltoDimensionalConfig`, `BarraEnferrujadaConfig`), com os valores default dos `[SerializeField]`
- Atribuídos aos bridges do `Player_Damiao` na cena `Playtest_RuinasPalidas` (`PlayerMovement.locomocaoConfig`, `EsquivaBridge.config`, `AnomalyPowerBridge.config`, `MaoFisicaBridge.config`)
- Adicionado o utilitário de Editor `WireConfigAssets.cs` (`Tools/FavelaAmarela/Wire Config Assets`) — atribui via `SerializedObject`, contornando a limitação do MCP `update_component` (não resolve referência de asset para campo tipado de `ScriptableObject`)
- Recompile limpo + 146/146 testes EditMode verdes

## 2026-07-08 — Tempestade de Areia + Coisa do Cemitério
- Estendido `EnvironmentState` com o evento `OnStormIntensityChanged` e o POCO `TempestadeOscilador` (oscilação senoidal entre intensidade mínima e máxima)
- Adicionados os adapters Runtime `TempestadeAmbiente`, `TempestadeZonaTrigger` e `TempestadeVisualOverlay` para sincronizar a tempestade com a cena e a UI
- Criado o novo sistema "Coisa do Cemitério" (bestiário #5): `CoisaDoCemiterioFSM`/`CoisaDoCemiterioState` (Core), estados `Farejando`/`AlvoPreciso`, insta-kill via `ResilienciaMental.ForcarColapso()`
- Documentado em `systems/environment.md` (seção Tempestade de Areia) e no novo `systems/coisa_do_cemiterio.md`

## 2026-07-07 — Criação Inicial
- Criado bundle OKF completo com 6 seções
- Documentados todos os 7 domínios Core: Combat, Enemies, Stealth, Abilities, GameLoop, Environment, AI
- Incluída camada de Architecture Decisions, Unity 6.4 Gotchas, Testes e Lore
- Integrado ao `CLAUDE.md` raiz via seção 3.1
