---
type: Game System
title: Yug-Neth — Companheiro Mi-Go e Escolha Ramificada com Abdul
description: Yug-Neth, o filhote Mi-Go libertado da Tumba — obrigatório para abrir os Portões de Carcosa — e a conversa que decide como ele é libertado.
tags: [companion, dialogue, branching, mi-go, yug-neth, abdul]
---

# Yug-Neth — Companheiro Mi-Go e Escolha Ramificada com Abdul

> **Atualizado 2026-07-30 (segunda rodada):** nome oficial confirmado — **Yug-Neth**. Lore
> completa, roteiro de diálogo integral e sprite em `lore/migo_companion.md`. Esta seção
> documenta especificamente a **implementação em código**, que diverge do design
> narrativo em alguns pontos deliberadamente simplificados pelo Vini — ver "Divergências
> do design narrativo" no fim.

Decisão dos diretores do projeto (2026-07-30): **Yug-Neth**, o filhote Mi-Go que Abdul
mantém acorrentado na Tumba, vira **companheiro obrigatório** — sem ele, os Portões de
Carcosa não abrem. Isso **substitui** a aplicação anterior do Mi-Go no bestiário
(`lore/bestiary.md` §8), que o descrevia como "Lore Oculta / Easter Egg, não inimigo
combatível". Ele segue não sendo combatível, mas agora é central à progressão.

## A conversa com Abdul

Abdul começa a interação (botão E, "Falar com o vulto") enquanto dorme em Transe. Depois
das falas, uma **escolha ramificada** aparece:

| Opção | Resultado |
|---|---|
| **Lutar** | A luta acontece normalmente. Ao derrotá-lo: dropa o **Necronomicon** (item a coletar depois) *e* liberta Yug-Neth. |
| **Concordar** | Abdul é poupado — sem luta, sem Necronomicon — mas Yug-Neth é libertado do mesmo jeito. |

**Yug-Neth é obrigatório nos dois caminhos.** Só o Necronomicon é exclusivo da luta. Ambos
os caminhos só existem através desta conversa com Abdul — **não há gatilho separado nas
correntes de Yug-Neth** (decisão explícita do Vini: simplifica o design de duas entradas
do documento de lore para uma só). Depois de resolvida a conversa, Abdul deixa de ser
interagível (`PodeInteragir` vira `false` permanentemente).

## O companheiro

- **Passivo e frágil** (decisão do Vini): não ataca, não conjura. Toda a mecânica é
  **proteção** — o jogador é responsável por mantê-lo vivo depois de libertado.
- Reaproveita `VitalidadeBridge` (mesma peça do Damião) para vitalidade e recebimento de
  dano — `Ficha_YugNeth`: Vitalidade 40, Ataque 0, Defesa 0. Bem mais frágil que um Cultista.
  (O documento de lore descreve um recurso "Resiliência do Companheiro" com dano fixo por
  fonte — **não implementado**; decisão do Vini de manter `Vitalidade` por ora.)
- **Já existe na cena, cativo, antes de libertado.** Vaga de um lado para o outro perto de
  onde Abdul o prendeu (`PatrolRoute` ping-pong, `loop: false` — mesma peça já usada pelo
  Cultista em Errante). Não segue ninguém e não é alvo de nada nesse estado: durante a
  luta com Abdul, ele ainda está sob controle dele, então Cones de Gelo/esqueletos não o
  miram (confirmado — não implementar proteção durante a luta).
- **Uma vez libertado** (`Bind(Transform)` chamado por Abdul), passa a seguir quem o
  libertou via `SeguidorDeAlvo` (POCO, `Core.Companion`) — fica parado dentro de uma
  distância de conforto, anda até o alvo quando fica pra trás.

## Morte do companheiro = incapacitação recuperável (revisado 2026-07-31)

> ⚠️ **Revogado (2026-07-31):** esta seção descrevia "sem resgate, run acaba na hora,
> estilo Ashley/RE4" — **não é mais verdade**. O Vini decidiu que faz mais sentido tratar
> a queda de Yug-Neth como uma incapacitação temporária, não como fim de jogo.

Se a Vitalidade de Yug-Neth chega a zero, ele **não morre**: cai e fica inerte exatamente
onde estava (`YugNethAI.EstaIncapacitado`). Enquanto incapacitado:

- Ele para de seguir Damião (fica parado no lugar).
- **Bloqueia os Portões de Carcosa** — ele é a chave dimensional, então sem ele reanimado
  não há como atravessar. Isso reforça que ele é obrigatório sem tornar a run inteira perdida.
- Reanimar exige levar Damião a um **`RefugioDeLuz`** (Poste de Luz) — `Reanimar()` cura a
  Vitalidade ao máximo e ele volta a seguir.

**Por que a mudança:** "sem resgate" tornava qualquer perda dele (mesmo por descuido, fora
de uma luta) equivalente a perder a run inteira. Incapacitação recuperável mantém a pressão
(ele fica vulnerável, você precisa protegê-lo) sem punir com a mesma dureza de um Colapso.

**`TipoDeDerrota.EscoltaPerdida` foi removido** do código (enum, pool de frases em
`FrasesDeColapso`, e o `GameManager` não encerra mais a run ao recebê-lo abatido) — dead
code deletado, não deprecado in-place.

## Yug-Neth nunca leva dano do jogador (regra dura)

Decisão do Vini (2026-07-31, após achar o bug jogando): **o golpe de Damião nunca o
atinge**, em contexto nenhum, nem por acidente numa troca de golpes apertada. Ele é o
companheiro obrigatório — deixar o jogador matá-lo por descuido de mira inviabilizaria a
run por um motivo que não é decisão dele.

Isso são **duas regras diferentes**, com implementações separadas de propósito:

| Regra | Como | Vale quando |
|---|---|---|
| Não leva dano do jogador | Marcador `Aliado` — `MaoFisicaBridge` pula qualquer alvo que o carregue | **Sempre** |
| Não leva dano nenhum durante a luta do Abdul | `VitalidadeBridge.IgnorarDano = true` enquanto cativo | Até ser libertado |

Invulnerabilidade total não resolveria a primeira: ela **mataria** a mecânica de
incapacitação descrita acima, que depende de ele poder cair para inimigos depois de livre.
`Bind()` (a liberação) é o ponto exato onde `IgnorarDano` vira `false` — antes dele,
intocável; depois, frágil e sob sua responsabilidade, mas ainda imune a você.

O marcador `Aliado` é um componente vazio, sem lógica. É deliberado: qualquer companheiro
futuro fica protegido só de ganhá-lo, sem o sistema de combate precisar conhecer nomes de
personagem.

## Arquitetura

- `YugNethAI` (Runtime) — `VitalidadeBridge` + `SeguidorDeAlvo` (livre) + `PatrolRoute`
  (cativo). Já colocado na cena pelo tool de montagem, nunca instanciado em runtime; a
  liberação é só uma chamada de `Bind(Transform)` na instância existente.
- `AbdulAlhazredAI.LibertarYugNeth()` — ponto único de liberação, chamado tanto por
  `ResolverEscolha` (trégua) quanto por `HandleDerrotado` (vitória em combate). Idempotente
  (`_yugNethJaLibertado`). Referência à instância de cena via campo `yugNethNaArena`
  (nunca busca por tag).
- `GameManager.RegistrarYugNeth(YugNethAI)` — registro pontual (não no bootstrap: Yug-Neth
  só passa a valer para a run quando libertado). Expõe `GameManager.YugNeth` para quem
  precisar consultar `EstaIncapacitado` (hoje, futuramente os Portões de Carcosa).
- `RefugioDeLuz` (Runtime, `GameLoop`) — trigger de proximidade (não botão E: é "descansar
  sob a luz", não uma ação deliberada) que chama `YugNethAI.Reanimar()` se ele estiver
  incapacitado.
- `Aliado` (Runtime, `Combat`) — marcador vazio que faz `MaoFisicaBridge` pular o alvo. Está
  no prefab `YugNeth.prefab`; a instância da cena o herda por ser `PrefabInstance` conectada.
  Ferramenta de Editor: `Tools/FavelaAmarela/Proteger Yug-Neth do golpe do jogador`.
- `NavegadorDeOpcoes` (Core, `Core.Dialogo`) — cursor puro de uma escolha de N opções
  (setas + confirmar), usado pelo `PainelDeEscolha` (Runtime/UI). Genérico — qualquer
  diálogo ramificado futuro reusa a mesma peça.
- `MontarInteracaoEDialogoAbdul` (Editor) — cria `Ficha_YugNeth.asset` e o prefab
  `YugNeth.prefab` se não existirem, coloca uma instância na cena ao lado do Abdul (cativa),
  e liga os campos do `AbdulAlhazredAI`. Usa o sprite real (`yug_neth_idle.png`) quando
  encontrado, placeholder colorido caso contrário.

## Travessia de cena (2026-08-02)

**Bug relatado pelo Vini em playtest:** Yug-Neth não saía da masmorra com o jogador.
**Causa:** `PortalDeCena` usa `SceneManager.LoadScene` não-aditivo — a cena de origem é
destruída inteira, e Yug-Neth é um `GameObject` só daquela cena, sem `DontDestroyOnLoad`.
Nem o próprio Damião sobrevive fisicamente à troca (é reconstruído do zero em cada cena);
só o `GerenciadorDeSave` (chaves) atravessa.

**Decisão de arquitetura:** entre recriar Yug-Neth por chave (mesmo padrão usado em todo
o resto do jogo) e torná-lo `DontDestroyOnLoad`, ficou com a primeira — consistência com
como o Damião, a arma, a Vitalidade e o progresso de quest já atravessam cena, em vez de
um objeto de gameplay virar exceção arquitetural sozinho.

- **`TravessiaDoCompanheiro`** (Runtime, `GameLoop`) — colocado no Deserto e no Santuário
  (`Tools/FavelaAmarela/Montar travessia de cena do Yug-Neth`). No `Start()` (ordem +100,
  depois de `PontoDeChegada` reposicionar o jogador), se
  `ChavesDeSave.AbdulResolvido` já tem valor e nenhum `YugNethAI` existe na cena, instancia
  o prefab perto do jogador, chama `Bind()` e `IgnorarColisaoCom()`, e registra no
  `GameManager` da cena nova. Deriva de `AbdulResolvido` em vez de gravar uma chave própria
  — mesma escolha já documentada em `ChavesDeSave.YugNethLibertado`.
- **`EstadoPersistenteDoCompanheiro`** (Runtime, `Persistencia`, no prefab `YugNeth.prefab`)
  — a Vitalidade dele agora atravessa cena, mesmo padrão de `EstadoPersistenteDoJogador`.
  A incapacitação **não precisa de chave própria**: só acontece quando a Vitalidade chega a
  zero, e restaurar o valor salvo como zero dispara `VitalidadeBridge.OnAbatido` pelo
  caminho normal — `YugNethAI.HandleAbatido` cuida do resto sozinho.

**Pendência que essa correção não fecha:** se ele cair numa cena e o jogador atravessar
outro portal antes de reanimá-lo lá, ele reaparece na cena seguinte **já incapacitado**
(a Vitalidade salva é zero) — correto. Mas nada hoje **impede** essa saída; não há uma
`TrancaDeArena` no portal de saída da Tumba condicionada a ele estar de pé.

## Pendências conhecidas

- **Nenhum inimigo mira nele, mesmo depois de libertado.** `CultistaAI.DetectarAlvoAoAlcance`
  só considera a camada Player. "Impedir que ele seja morto" hoje depende de você buscá-lo
  ativamente perto de perigo, não de uma IA que o ataque de propósito.
- **Diálogo genérico:** a conversa ainda usa `TutorialHintUI` como caixa de texto — não
  há nome de personagem, retrato, nem o roteiro completo (A/B/C + bifurcação) do documento
  de lore, só a bifurcação final (lutar/concordar). Ver `systems/interacao.md`.
- **Necronomicon ainda não existe como prefab/pickup.** Confirmado: é um item a coletar
  depois da derrota (mesmo padrão de `BauDaTumba`/`PatuaPickup` — `IInteragivel`), não um
  efeito automático. Falta construir esse componente.
- **Refúgio ainda é mínimo.** `RefugioDeLuz` só reanima Yug-Neth — regenerar RM de Damião,
  pausar dreno de tempestade e servir de ponto de save são `TODO(design)` no próprio arquivo.

## Divergências do design narrativo (`lore/migo_companion.md`)

O documento de lore descreve um encontro mais rico que o implementado agora. Divergências
confirmadas com o Vini (2026-07-30):

1. **Gatilho único, não duplo.** O documento descreve dois objetos interagíveis (correntes
   de Yug-Neth → diálogo; grimório → luta direta sem escolha). Implementado: só a conversa
   com Abdul, sempre com a escolha completa.
2. **Sem risco durante a luta do Abdul especificamente.** O documento descreve Yug-Neth
   vulnerável na arena durante o Caminho B, com "Resiliência do Companheiro" e bloqueio de
   projétil. Confirmado como **não implementado ali**: durante a luta ele está sob controle
   de Abdul, não é alvo de nada. **Fora da luta** (como companion livre), ele passou a ter
   risco real e uma consequência de queda (incapacitação + Refúgio) — ver seção acima.
3. **Recurso mantido como `Vitalidade`**, não o "RC" (Resiliência do Companheiro, dano fixo
   por fonte, máx 100) descrito no documento.
4. **Roteiro reduzido**: só a bifurcação final (lutar/concordar) está implementada, sem o
   A/B/C inicial nem o diálogo de contexto extra.

Os efeitos passivos fora do Abdul (perto de Nagas, Byakhee, escuro, Portões) referenciam
conteúdo que ainda não existe no jogo (Dungeon 2, luta do Byakhee) — documentados no lore,
não bloqueiam nada agora.

## Sorting Layer (resolvido — NÃO criar "Characters")

O documento de lore pede uma Sorting Layer `Characters` separada. **Investigado e
descartado (2026-07-30):** todo o sistema de oclusão isométrica do projeto
(`OcclusaoDitherFade` + `DynamicYSort`, ver `systems/renderizacao_isometrica.md`) depende
de chão, paredes e personagens estarem **na mesma Sorting Layer**, com profundidade
resolvida só por `sortingOrder` explícito (`-y*10`). O Unity ordena primeiro por Sorting
Layer e só depois por `sortingOrder` — uma segunda layer para personagens faria uma parede
no `Default` nunca mais desenhar na frente de um personagem em `Characters`, mesmo quando
o jogador está atrás dela, quebrando a oclusão que dá a sensação isométrica. Mesmo caso já
documentado para o Transparency Sort Mode (skill `favela-isometric-standards`): "ideal
seria Custom Axis, mas hoje é Default, funciona porque o sorting é por sortingOrder
explícito". **Yug-Neth fica em `Default` com `DynamicYSort`, igual a todo o resto.**
