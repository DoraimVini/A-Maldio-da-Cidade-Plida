# ⛔ COMMANDMENT — READ AND OBEY BEFORE ANY OTHER INSTRUCTION IN THIS FILE

> **This is not a Skill. It is a Commandment.** It outranks every other section of this
> document, every Skill, and every habit or default behavior. It is obeyed *first*, always,
> without being asked and without exception.

**At the START of every single work session on this project — before reading code, before
answering a question, before touching a single file — you MUST activate every Skill that
applies to this project.**

Activation means **invoking the Skill tool** for each applicable skill, not merely remembering
that it exists or having read it once in a previous session. A Skill that was not invoked is a
Skill that is not in force.

The project Skills live in `.claude/skills/` (mirrored for another tool in `.agents/skills/`):

| Skill | Applies to |
|---|---|
| `favela-qa-pipeline` | **Any** C# or asset change. Mandatory compile→test cycle. |
| `favela-isometric-standards` | Physics, Rigidbody2D, colliders, camera, grid, prefabs. |
| `favela-pixelart-standards` | Any sprite, texture or import setting. |
| `favela-lore-enforcer` | Any text, name, or description visible to the player. |
| `favela-session-briefing` | Session start: git state, last devlog entry, next roadmap item. |
| `query-knowledge-bundle` | Any question about mechanics, lore, enemies, stealth, architecture. |

**Why this Commandment exists (2026-08-21).** Over a long session I repeatedly worked *around*
these Skills instead of *through* them: I never invoked `favela-qa-pipeline` and ran ad-hoc test
commands instead; I edited physics and colliders without invoking
`favela-isometric-standards`, and consequently shipped four Dynamic `Rigidbody2D` with no
`FreezeRotation` (the boss and the Castle mobs visibly spun); and I wrote a tool that
overwrote already-calibrated collider volumes because I never checked for the guard that
already protected them. None of these were knowledge failures. Every one was a failure to run
the process that was already written down. Vini had to catch each of them himself — which is
the opposite of what a solo developer with no team needs.

**Corollaries, equally binding:**

1. **Documentation before assertion.** This project's exact engine ships its own offline
   reference at
   `C:\Program Files\Unity\Hub\Editor\6000.4.4f1\Editor\Data\Documentation\en\ScriptReference\`.
   Consult it — and quote it — before using or explaining any Unity API. Never answer Unity
   behavior from memory alone; the engine renames APIs between versions
   (`Rigidbody2D.velocity` → `linearVelocity`).
2. **Audit against the standard before investigating a symptom.** When something behaves
   strangely, sweep *all* prefabs and scenes for the relevant fields first (constraints,
   gravityScale, collisionDetection, collider existence, wiring). Comparing actors side by
   side exposes the outlier; studying one actor in isolation hides it.
3. **Never let work sit unbacked in silence.** If the working tree accumulates significant
   uncommitted work, say so and offer a checkpoint. Do not commit without explicit approval —
   but do not stay quiet about the risk either.
4. **Verify, then report.** A tool's exit code and its own log are not evidence. Re-read the
   file on disk, or run the suite, and report the real number.

---

# Caminho para Carcosa

> **Título oficial, visível ao jogador: "Caminho para Carcosa"** (decisão do Vini, 2026-08-11).
> O projeto carrega outros nomes por razões históricas — **A Maldição da Cidade Pálida**
> (repositório no GitHub), **Peregrino Amarelo** (pasta local), **Favela Amarela**
> (namespaces `FavelaAmarela.*` e nome das skills). Esses ficam como estão: renomear
> namespace e repositório é retrabalho sem ganho. **Mas todo texto novo mostrado ao jogador
> usa "Caminho para Carcosa".**

## 1. Contexto Geral
- Engine: Unity 6000.4.4f1, 2D isométrico.
- Linguagem: C#, com separação estrita entre lógica pura (POCO) e adaptadores Unity.
- Gênero: jogo de stealth / horror cósmico. Furtividade e horror cósmico continuam sendo o núcleo tonal e narrativo — mas o **combate aberto já é um pilar sistêmico próprio**, não um mero fallback "quando o stealth falha": toda unidade tem uma ficha de atributos (Vitalidade/Ataque/Defesa/Conjuração/Resistência Anômala), uma fórmula de mitigação de dano por defesa, e armas com ataque básico + habilidade própria (cooldown independente) — ver `Docs/KnowledgeBundle/systems/ficha_de_atributos.md`. O jogo também vai incorporar outros sistemas de progressão (inventário, barra de ações, árvore de talentos/habilidades, loot com raridade e níveis de personagem) por decisão explícita do Vini — ver os bullets abaixo para o que já foi decidido e o que ainda precisa de confirmação. **Não proponha mecânicas de ARPG novas por conta própria** além das já decididas; qualquer outra (ex.: moeda, crafting) precisa ser confirmada com ele antes.
- Inventário e barra de ações: **LIBERADOS para desenvolvimento** (decisão de 2026-07-31; antes eram "previstos, sem data"). Foram destravados porque o item 2 do escopo do edital (Sistema de Consumíveis) depende deles — ver §1.1. Continuam valendo as restrições de forma: enxuto (sem grind de itens) e terminologia diegética da skill `favela-lore-enforcer`. A barra de ações já existe (`BarraDeAcoes`, slot de arma + habilidade); o que falta é o inventário em si.
- Árvore de talentos/habilidades e níveis de personagem: **JÁ EXISTEM em código** — o POCO `Progressao` (`Assets/Scripts/Core/Progression/Progressao.cs`, namespace `FavelaAmarela.Core.Progression`) implementa nível de Exposição, curva fechada (cap 12), pontos e os Ecos do Labirinto de Carcosa; o adaptador `ProgressionBridge` (`Assets/Scripts/Progression/`, namespace `FavelaAmarela.Runtime.Progression`) traduz `EcoDef`↔id e **se auto-instancia** via `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` + `DontDestroyOnLoad`, como o `GerenciadorDeSave`. Ao mexer, seguir a terminologia diegética (nada de "Skill Tree"/"Talent Point" visível ao jogador). **Histórico:** era um `MonoBehaviour` (`ProgressionManager`) no namespace desviante `FavelaAmarela.Progression` e que não estava em cena nenhuma — `Instance` sempre `null`, progressão inerte. Convertido na Fase 3 da refatoração de managers (2026-08-18); o desvio de namespace foi corrigido junto.
- Loot e raridade: **modelo de composição decidido em 2026-08-27** — ver `systems/loot_e_drop.md`. **Esta decisão REVOGA a regra anterior** de escopo contido ("nada de profundidade tipo Path of Exile / Last Epoch") e a invariante de que "o sorteio nunca gera atributos". **Motivo do Vini:** sem geração, uma arma de nível máximo entrega os mesmos status de uma arma de nível 1 — não existe curva de poder, e a segunda cópia de um item nunca interessa, que é o loop de loot mais fraco de um ARPG. **O modelo agora é base + afixos rolados:** o `ItemDef` autorado passa a ser a BASE (slot, ícone, tags, implícitos, nível do item, e o moveset quando for arma); grau e afixos vivem na `ItemInstance`, rolados por `GeradorDeItem` dentro de faixas autoradas em `AfixoDef`, sempre através de `IFonteDeAleatoriedade` (injetável, para o sorteio ser determinístico em teste). **A nova invariante, mais fraca e ainda vigente:** o gerador NUNCA inventa um afixo — ele só escolhe de um pool autorado e rola dentro de uma faixa autorada. **Os valores rolados são GRAVADOS no save, não a semente** — semente re-rola todo item já dropado assim que um `AfixoDef` é editado. Itens únicos (as 3 armas da Tumba, as relíquias) continuam autorados à mão, como em D2/PoE. **Consequência que deixou de ser cosmética:** o nível do item passa a governar o pool, então conceder Exposição no mundo virou pré-requisito — sem isso o sistema entrega sempre o piso.

### 1.1 Escopo corrente: Vertical Slice
**Redefinido em 2026-07-31.** O Vertical Slice do edital são os **14 itens da lista priorizada de produção** (GDD v3.0), não só a Tumba: **a Fase 1 completa** (Deserto de Hali povoado + tempestade ligada + Santuário de Yhtill + boss Byakhee) **e a última fase do jogo** (Castelo de Carcosa + boss Rei em Amarelo), mais os sistemas de suporte (status ailments, consumíveis, companheiro). Estado real item a item, sempre atualizado: `Docs/KnowledgeBundle/roadmap_vertical_slice.md`.

**É um recorte "abertura + desfecho", não duas fases seguidas.** O jogo completo tem 6 fases; o VS pula as 4 do meio de propósito — as dungeons iniciais mostram o loop de jogo, e o Rei em Amarelo mostra onde ele desemboca. Consequência prática: **o Castelo não é candidato a corte** se o prazo apertar; cortá-lo remove metade da tese do VS. Isso substitui a definição anterior ("só a Tumba de Alhazred jogável de ponta a ponta") — a Tumba passa a ser **uma peça concluída** do VS, não o VS inteiro.

Consequências práticas ao propor trabalho:
- **Inventário/barra de ações estão liberados** (item 2 depende deles) — ver bullet acima.
- ~~Continuam **fora** do VS: loot/raridade/níveis de personagem~~ — **REVOGADO em 2026-08-29 pelo Vini**, que autorizou o merge de `develop_items` em `develop_manager` ("pode dar o merge da develop_items na develop_manager"). As duas branches eram separadas de propósito, para a ambição de uma não pôr a entrega da outra em risco; **a separação acabou** e as duas apontam para o mesmo commit. A build do edital passa a sair com itemização a dado, afixos rolados, escala por nível, física de impacto e Mão Secundária dentro.

  > **O que isso obriga daqui em diante.** `develop_manager` deixou de ser a branch conservadora: não existe mais uma cópia "só o VS, sem risco" para voltar atrás. Trabalho novo de ambição vai para uma branch nova, não para dentro dela — e a suíte tem de ficar verde **na `develop_manager`**, não só na branch de origem, antes de qualquer merge. No dia do merge ela estava em **886 testes, 863 passando, 0 falhas**.

- Continuam **fora** do VS: árvore de talentos, percepção graduada, fast travel.
- **Progressão por nível: reafirmado FORA do VS em 2026-08-18.** O motor existe e funciona (`Progressao` + `ProgressionBridge`, ligado desde a Fase 3), mas **ninguém concede Exposição no mundo e não há um único `EcoDef` autorado** — e pôr isso de pé atrasaria o edital. Decisão do Vini: não incluir níveis nem talentos no Vertical Slice; quando o VS estiver completo, avaliar expô-los pelo **Carcosa Debugger** primeiro. ~~**Consequência que precisa ficar clara:** com o nível travado em 1, o loot só entrega tier 1 — isso é esperado no VS, não bug.~~ **Atualização de 2026-08-28/29: DESATUALIZADO, e a instrução se inverteu.** O elenco concede Exposição de verdade (Cultista 25, Abdul 150, Byakhee 200), o que põe o jogador no **nível 3** ao chegar no Byakhee — contado nas cenas, não estimado. Com o merge, isso vale também para a build do edital.

  **Por que isso deixou de ser opcional e virou pré-requisito:** o Vini jogou e relatou *"não tem como ganhar da Byakhee, os itens são fracos demais"*. Ele estava certo, e a causa era exatamente o nível travado — o Baú da Tumba entregava a arma no nível 1 para sempre, e a luta pede 14 acertos contra os 5 do chefe. No nível 3 a troca é 9 por 9. **Nível travado em 1 não era "esperado", era o que quebrava o segundo chefe do Vertical Slice.**

  Continua valendo o que a nota original protegia: **não autorar tiers altos à mão** para compensar. O tier vem da curva (`Core/Loot/CurvaDeGrau.cs`) e do nível do item, e é assim que se mexe nele.
- O escopo é grande e o prazo é de edital: ao sugerir trabalho, prefira o que fecha um item da lista inteiro a polir algo já jogável. Quando em dúvida se algo pertence ao VS, pergunte ao Vini.
- Ambientação: Ruínas Pálidas (Ruins of Hali) dentro da Cidade Pálida (Carcosa). Protagonista: Damião.
- Controle de versão: Git.

## 2. Filosofia POCO + Unity (regra de ouro arquitetural)
Toda lógica de domínio vive em classes C# puras (POCO), sem herdar de `MonoBehaviour`, em `FavelaAmarela.Core.*`:
- `Core.Enemies` (`CultistaFSM`, `CultistaState`, `PatrolRoute`)
- `Core.Combat` (`ResilienciaMental`)
- `Core.Stealth` (`SoundBroadcastService`, `SomEmitido`)
- `Core.Environment` (`EnvironmentState`)
- `Core.GameLoop` (`GameLoopStateMachine`)
- `Core.Abilities` (`IAnomalyPower`, `DimensionalLeap`)

Os `MonoBehaviour` em `FavelaAmarela.Runtime.*` e nas pastas `Player/`, `Enemies/`, `GameLoop/`, `Camera/`, `UI/` são só adaptadores: leem input, instanciam/injetam os POCOs correspondentes (via métodos `.Bind()`, ver `GameManager.cs`) e sincronizam estado com o mundo visual. Nunca o contrário.

Exemplos canônicos a seguir (não inventar um padrão novo): `CultistaFSM` (Core) + `CultistaAI` (Runtime); `ResilienciaMental` (Core) + `PlayerMovement`/`PlayerStealthState` (Player) + `ResilienciaBar` (UI).

## 3. Fontes de documentação obrigatórias
Antes de usar uma API da Unity que você não tenha certeza absoluta (assinatura, comportamento, ou se foi renomeada/removida), **consulte a documentação oficial da versão exata do projeto (Unity 6.4 / 6000.4)** em vez de confiar em memória de versões antigas:
- Manual: https://docs.unity3d.com/6000.4/Documentation/Manual/index.html
- Script Reference: https://docs.unity3d.com/6000.4/Documentation/ScriptReference/index.html

Isso importa porque a Unity 6 renomeou/alterou APIs comuns (ex.: `Rigidbody2D.velocity` → `Rigidbody2D.linearVelocity`, já refletido em `PlayerMovement.cs`). Nunca assuma o nome de uma API de "Unity clássico" sem checar a Script Reference da 6000.4 primeiro.

Para as classes `Core/` (POCO), não existe documentação "Unity" a seguir — são C# puro. Siga as convenções padrão de C#/.NET (nullable reference types, `IDisposable`, `readonly struct`, etc.) e não introduza nenhum tipo/atributo da `UnityEngine` além do já permitido em `Assets/Scripts/Core/CLAUDE.md`. Na dúvida sobre um recurso de linguagem C#, prefira a documentação oficial da Microsoft (learn.microsoft.com/dotnet/csharp) a suposições.

### 3.1 Knowledge Bundle OKF (Game Design como código)
O projeto mantém uma base de conhecimento interna em formato [Open Knowledge Format (OKF)](https://github.com/GoogleCloudPlatform/knowledge-catalog/tree/main/okf) no diretório `Docs/KnowledgeBundle/`.

Essa base contém documentação de **sistemas de gameplay**, **regras de negócio**, **scripts** e **testes** em arquivos Markdown com frontmatter YAML — projetada para ser lida tanto por humanos quanto por agentes de IA.

**Regras de uso obrigatório:**
1. Antes de **implementar ou alterar** qualquer mecânica de jogo (dano, movimentação, UI, IA de inimigos, stealth), **leia o arquivo relevante** em `Docs/KnowledgeBundle/`. Comece pelo `index.md` e navegue pelos links.
2. Use o campo `type` do frontmatter YAML para filtrar documentos relevantes (ex: `type: Game System` para regras de mecânica, `type: C# Script` para detalhes de implementação).
3. Se uma **nova mecânica** for criada ou uma existente for **refatorada significativamente**, **atualize o OKF correspondente** (ou crie um novo arquivo `.md` seguindo o padrão) e atualize o `index.md` do diretório pai. Não deixe a base de conhecimento ficar desatualizada.
4. Em caso de **conflito** entre o OKF e o código existente, o **código-fonte** é a verdade para *como* funciona, mas o **OKF** é a verdade para *como deveria* funcionar. Sinalize a divergência.

**Estrutura do bundle:**
```
Docs/KnowledgeBundle/
├── index.md            ← Ponto de entrada (leia primeiro)
├── architecture/       ← Decisões arquiteturais e padrões estruturais
│   ├── index.md
│   └── *.md
├── scripts/            ← Documentação de scripts C# (core/ e runtime/)
│   ├── index.md
│   └── *.md
├── systems/            ← Regras de game design e mecânicas
│   ├── index.md
│   └── *.md
├── unity64_gotchas/    ← APIs renomeadas e armadilhas de performance da Unity 6.4
│   ├── index.md
│   └── *.md
├── tests/              ← Estrutura de testes e QA
│   ├── index.md
│   └── *.md
└── lore/               ← Terminologia diegética e regras de universo narrativo
    ├── index.md
    └── *.md
```

### 3.2 Studio Knowledge Base (Regras Globais de Engenharia)
As regras primordiais de arquitetura de software deste estúdio (POCO vs MonoBehaviour, Pipelines HD-2D e Codificação Iterativa) não vivem neste repositório.
Sempre que você for desenhar a fundação de um novo script ou lidar com importação de sprites de Inteligência Artificial, você DEVE usar as suas ferramentas nativas de busca (Grep/Read) para vasculhar o diretório raiz do estúdio em:
`C:\Users\Vini\Desktop\Studio_Knowledge_Base`
Leia os arquivos lá antes de cometer erros arquiteturais clássicos da Unity.

### 3.3 Integração com o Obsidian e Devlog
- **Cofre Obsidian:** A pasta `Docs/KnowledgeBundle/` está conectada por link simbólico (Junction) ao cofre Obsidian em `C:\Users\Vini\Desktop\Studio_Knowledge_Base\Projeto_Amarelo`. Qualquer arquivo criado ou modificado em `Docs/KnowledgeBundle/` aparecerá instantaneamente no Obsidian do Vini.
- **Rotina de Devlog:** Ao finalizar com sucesso qualquer tarefa de codificação ou design, você DEVE documentar as alterações no arquivo `Docs/KnowledgeBundle/log.md`. 
  - Use a estrutura: `## AAAA-MM-DD — [Título Curto do Devlog]`
  - Liste de forma concisa e técnica todas as modificações realizadas no Core, Runtime, Testes e Documentação.
  - Faça isso de forma semelhante a uma mensagem de commit detalhada do GitHub.

## 4. Regras de Ouro
1. **Nunca aloque lixo em `Update`/`FixedUpdate`/`LateUpdate`.** Sem `new`, `GetComponent` em hot path, `FindObjectOfType`, LINQ dentro de loops. Cache em `Awake`/`Start`. Prefira `readonly struct` para event args (ver `SomEmitido`, `ResilienciaChangedArgs`).
2. **Documentação XML obrigatória em todo membro público (`/// <summary>`), em português** — é a convenção já usada em todo o código (`ResilienciaMental`, `PlayerStealthState`, `CultistaFSM`), inclusive citando o vocabulário diegético do lore-enforcer diretamente no doc.
3. **Composição sobre herança.** Interfaces como `IAnomalyPower` para comportamento plugável. Nada de "God objects".
4. **Convenções de nomenclatura:**
   - Campos privados com prefixo `_` (ex.: `_atual`, `_fsm`).
   - `PascalCase` para propriedades, métodos e classes públicas; `camelCase` para variáveis locais e campos serializados.
   - `sealed` por padrão quando a classe não precisa ser herdada (padrão já usado em `ResilienciaMental`, `PatrolRoute`, `SoundBroadcastService`).
5. **Namespaces:** `FavelaAmarela.Core.<Domínio>` para POCOs, `FavelaAmarela.Runtime.<Domínio>` para adaptadores, `FavelaAmarela.Player` para os scripts do jogador, `FavelaAmarela.Tests.EditMode` para testes.
6. **Testabilidade:** toda lógica nova em `Core/` deve ser testável sem a Unity rodando, e ganhar um teste NUnit EditMode correspondente instanciando o POCO diretamente (ex.: `new CultistaFSM()`), sem cena nem `MonoBehaviour`.
7. **Nulabilidade / robustez:** valide referências do Inspector em `Awake()` com `Debug.LogError` e siga com um fallback seguro — nunca deixe uma `NullReferenceException` estourar em produção.
8. **Eventos, não polling:** camadas de UI/áudio/câmera observam eventos C# (`event Action`) expostos pelos POCOs (ex.: `ResilienciaMental.OnChanged`), nunca fazem polling de estado a cada frame.
9. **Salvamento (quando for implementado):** JSON com classes POCO `[Serializable]`. Nunca `PlayerPrefs` para dados de progresso.

## 5. Movimentação e Física
- Isométrico 2D via `Rigidbody2D` com `gravityScale = 0` sempre (ver skill `favela-isometric-standards`).
- Movimentação real do projeto usa `Rigidbody2D.linearVelocity` atribuído em `FixedUpdate` (não `MovePosition`) — siga essa convenção existente em `PlayerMovement.cs`.
- `CollisionDetectionMode2D.Continuous` para atores que se movem.
- A câmera fica sempre com rotação `Quaternion.identity` (sem tilt) — a "sensação" isométrica vem do Y-sorting (`sortingOrder` por `-worldCenter.y`, ver `LevelBlockoutGenerator`) e do remapeamento de input em `BaseIsometrica.ParaMundo`, não de uma câmera 3D inclinada. Ver skill `favela-isometric-standards`.
- Qualquer alteração em física, câmera, prefab de sala ou Rigidbody de inimigo/player deve respeitar as constantes fixas da skill `favela-isometric-standards` (gravityScale 0, câmera sem rotação, PPU 32, Y-sorting por Custom Axis).

## 6. Terminologia diegética
Nunca use termos genéricos de RPG (HP, Mana, Enemy, Level Up) em texto visível ao jogador, nomes de habilidade ou descrições de `ScriptableObject`. A tradução completa (Resiliência Mental, Trauma, Colapso, Ancoragem, Cultista Amarelo, Salto Dimensional etc.) está na skill `favela-lore-enforcer` — consulte-a em vez de reimplementar a tabela aqui.

## 7. Guardrails disponíveis como Skills
Estas skills vivem em `.claude/skills/` e devem ser puxadas conforme o contexto:
- `favela-isometric-standards` — física/câmera/grid isométrico.
- `favela-lore-enforcer` — terminologia diegética.
- `favela-pixelart-standards` — configurações de import de sprite (PPU 32, Point filter, sem compressão).
- `favela-qa-pipeline` — ciclo compilar → testar antes de considerar uma mudança pronta (não commita sozinho; commit só quando pedido).

Essas mesmas regras também existem em `.agents/skills/` no formato usado por outra ferramenta (Antigravity) e em `.agents/AGENTS.md` (tabela de roteamento dela). Se o conteúdo de uma regra mudar, atualize os dois lados para não divergir.

## 8. Preferências pessoais de trabalho
- Responda sempre em **português do Brasil**, salvo pedido explícito de outro idioma.
- Mensagens de commit e documentação de projeto podem ser em português.
- Prefira `var` quando o tipo for evidente pela atribuição; seja explícito em campos e propriedades.
- Seja explícito com modificadores de acesso (sempre declare `private`).
- Métodos pequenos (~20 linhas), fazendo uma coisa só; se crescer, sugira dividir.
- Ao sugerir código, explique a abordagem em 1 parágrafo antes do código.
- Ao analisar um trecho, seja crítico: aponte problemas de performance, legibilidade e arquitetura mesmo que o código funcione.
- Ao refatorar, mantenha comentários existentes e adicione novos só quando o "porquê" não for óbvio.
- Avise sobre possíveis impactos em outras partes do sistema (ex.: "mudar este método pode afetar a classe X que depende dele").
