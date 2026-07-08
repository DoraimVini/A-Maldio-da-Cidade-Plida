# A Maldição da Cidade Pálida (Favela Amarela)

## 1. Contexto Geral
- Engine: Unity 6000.4.4f1, 2D isométrico.
- Linguagem: C#, com separação estrita entre lógica pura (POCO) e adaptadores Unity.
- Gênero: jogo de stealth / horror cósmico. O núcleo continua sendo furtividade e horror cósmico, não farming/loot genérico de ARPG — mas o jogo vai incorporar sistemas de progressão (inventário, barra de ações, árvore de talentos/habilidades) por decisão explícita do Vini. **Não proponha mecânicas de ARPG novas por conta própria** — essas três abaixo já foram decididas; qualquer outra (ex.: níveis de personagem, moeda, crafting) precisa ser confirmada com ele antes.
- Inventário e barra de ações: **previstos** (decisão de 2026-07-07), para armas e armaduras coletadas via gameplay — sem data definida, não implementar por conta própria sem confirmar com o Vini antes. Quando entrar em desenvolvimento, deve ser enxuto (sem grind de itens) e seguir a terminologia diegética da skill `favela-lore-enforcer`.
- Árvore de talentos/habilidades: **também prevista** (decisão de 2026-07-07), sem data definida — não implementar por conta própria sem confirmar com o Vini antes. Ao desenhar, seguir a terminologia diegética (nada de "Skill Tree"/"Talent Point" genérico visível ao jogador) e a filosofia de composição sobre herança da seção 4 (habilidades plugáveis via interface, ver `IAnomalyPower`) em vez de um grafo de nós genérico copiado de ARPG.
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
- A câmera fica sempre com rotação `Quaternion.identity` (sem tilt) — a "sensação" isométrica vem do Y-sorting (`sortingOrder` por `-worldCenter.y`, ver `LevelBlockoutGenerator`) e do remapeamento de input em `PlayerMovement.ConvertToIsometric`, não de uma câmera 3D inclinada. Ver skill `favela-isometric-standards`.
- Qualquer alteração em física, câmera, prefab de sala ou Rigidbody de inimigo/player deve respeitar as constantes fixas da skill `favela-isometric-standards` (gravityScale 0, câmera sem rotação, PPU 16, Y-sorting por Custom Axis).

## 6. Terminologia diegética
Nunca use termos genéricos de RPG (HP, Mana, Enemy, Level Up) em texto visível ao jogador, nomes de habilidade ou descrições de `ScriptableObject`. A tradução completa (Resiliência Mental, Trauma, Colapso, Ancoragem, Cultista Amarelo, Salto Dimensional etc.) está na skill `favela-lore-enforcer` — consulte-a em vez de reimplementar a tabela aqui.

## 7. Guardrails disponíveis como Skills
Estas skills vivem em `.claude/skills/` e devem ser puxadas conforme o contexto:
- `favela-isometric-standards` — física/câmera/grid isométrico.
- `favela-lore-enforcer` — terminologia diegética.
- `favela-pixelart-standards` — configurações de import de sprite (PPU 16, Point filter, sem compressão).
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
