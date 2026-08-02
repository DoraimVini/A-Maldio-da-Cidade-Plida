---
type: Architecture
title: Persistência — Chaves de Persistência e Save Manager
description: Como o estado do mundo e do jogador sobrevive a trocas de cena e a saves. Regras para não quebrar saves ao renomear ou mover objetos.
tags: [persistencia, save, arquitetura, chaves]
---

# Persistência — Chaves de Persistência

> **Decisão do Vini (2026-07-31).** Arquitetura escolhida a partir de um documento de
> referência que ele trouxe. Motivada por um bug concreto de playtest: **sair da Tumba
> fazia o jogador perder a arma do baú**, porque `SceneManager.LoadScene` destrói e recria
> tudo, e nada guardava estado entre cenas.

## O problema que isto resolve

A Unity recria cenas e objetos do zero a cada carregamento. Ela não tem como saber que o
"Baú" da cena recém-carregada é o mesmo baú que o jogador abriu meia hora atrás. A **chave
de persistência** é o documento de identidade que costura os dois: *"este é o baú XYZ —
veja no save se ele já foi aberto."*

## Regra dura: nunca use nome ou caminho de hierarquia como chave

Este é **o** erro que quebra sistemas de save. Se a chave for `Level1/Floresta/Bau_Magico`
e alguém renomear o objeto para `Bau_Encantado` ou movê-lo de pai:

1. A chave de busca muda.
2. O sistema procura o estado antigo e não encontra.
3. Conclui que é um objeto novo e aplica o estado padrão.
4. **O jogador reencontra fechado um baú que já tinha aberto** — sem nenhum erro no console.

Progresso perdido em silêncio é o pior tipo de bug: não aparece em teste, só em reclamação
de jogador. Por isso a chave é um **GUID gerado uma única vez** e serializado na cena
(`ObjetoPersistente`), imune a renomeação e a mudança de hierarquia.

## As peças

| Peça | Camada | Papel |
|---|---|---|
| `RegistroDeSave` | Core | O save **em memória**: mapa chave → estado. Fonte da verdade durante a partida. |
| `EstadoDeSave` / `EntradaDeSave` | Core | Formato **em disco** (`[Serializable]`, lista — `JsonUtility` não serializa `Dictionary`). |
| `ChavesDeSave` | Core | Constantes das chaves **globais** (flags e estado do jogador). |
| `IPersistente` | Runtime | Contrato de quem tem estado a salvar: captura e reaplica **só o próprio**. |
| `ObjetoPersistente` | Runtime | Dá GUID imutável a um objeto de cena. |
| `GerenciadorDeSave` | Runtime | Save Manager central: registra, coleta, serializa em JSON. `DontDestroyOnLoad`. |
| `EstadoPersistenteDoJogador` | Runtime | Faz arma empunhada e Vitalidade sobreviverem à troca de cena. |

## Padrão Observer: ninguém salva o próprio arquivo

Cada objeto sabe apenas **ler e escrever o próprio estado**. Quem junta tudo e grava é o
`GerenciadorDeSave`:

```
Objeto.Start()  →  GerenciadorDeSave.Registrar(this)
Salvar          →  Gerenciador pede CapturarEstado() de cada inscrito
                   monta chave → valor, serializa tudo num arquivo só
Carregar        →  Gerenciador lê o arquivo e chama AplicarEstado() em quem tem chave
```

Um arquivo, um formato, um lugar para depurar.

## Degradação graciosa (obrigatória)

Save corrompido ou desatualizado **nunca** pode derrubar o carregamento — a alternativa
custa a run inteira do jogador. As regras, todas cobertas por teste:

- Objeto na cena **sem** chave no save → assume o estado padrão (novo/fechado/vivo).
- Chave no save **sem** objeto correspondente (removido pelo level designer) → ignorada, sem
  `NullReferenceException`.
- Arquivo ausente, ilegível ou com JSON inválido → registro vazio, partida nova.
- Entrada nula, chave vazia ou chave repetida → ignorada / última vence.
- Valor de enum que não existe mais (arma removida numa versão futura) → volta ao padrão
  seguro (desarmado) com um aviso, em vez de estourar.

## JSON, nunca `PlayerPrefs`

Regra de Ouro 9 do `CLAUDE.md`. `PlayerPrefs` existe para **configuração de usuário**
(volume, resolução). Não serve para progresso: é lento para volume, não guarda estrutura
complexa e é trivial de adulterar. O save vai para
`Application.persistentDataPath/partida.json`.

## Convenção de nomes das chaves globais

Hierárquica e agrupada por domínio — nunca "magic string" solta:

| Ruim | Bom |
|---|---|
| `chefe_morto` | `Quest.Tumba.AbdulResolvido` |
| `pegou_espada` | `Jogador.Equipamento.Arma` |

Vivem como **constantes** em `ChavesDeSave`, não como literais espalhados: um erro de
digitação num literal cria uma chave nova em silêncio e o progresso associado some.

## Estado atual e o que falta

**Pronto e instalado em cena** (Tumba e Deserto, via
`Tools/FavelaAmarela/Montar persistência em TODAS as cenas jogáveis`):
- Núcleo completo (Core + manager + GUID), **322 testes verdes**.
- `GerenciadorDeSave` e `EstadoPersistenteDoJogador` presentes nas duas cenas jogáveis.
- `PortalDeCena` chama `CapturarTudo()` antes de carregar a cena nova.
- **A arma do baú e a Vitalidade atravessam a porta da Tumba.**

> **Instalar nas duas pontas é obrigatório.** Capturar o estado ao sair da Tumba não serve
> de nada se o Damião do Deserto não tiver um `EstadoPersistenteDoJogador` para reaplicar na
> chegada. Cena jogável nova precisa rodar a ferramenta.

**Objetos de mundo (adicionado 2026-07-31):** baú, patuá, Necronomicon e o desfecho do Abdul
agora persistem. Voltar à Tumba não reabre o baú nem ressuscita o chefe.

Eles usam **write-through com chave global**, não o par `ObjetoPersistente`+`IPersistente`:
- **Write-through** porque `CapturarTudo()` só enxerga quem está carregado e registrado; um
  pickup que já se desativou seria pulado em silêncio. Gravar no instante do fato não tem
  esse buraco.
- **Chave global** (`ChavesDeSave`) e não GUID porque cada um destes é **único e narrativo** —
  quests vão perguntar "o Abdul foi resolvido?" pelo nome, não por um GUID opaco. O GUID do
  `ObjetoPersistente` continua sendo o caminho certo para objetos **repetidos** (baús de
  outras dungeons, portas genéricas), onde um nome global não escalaria.

Dois detalhes que a implementação precisou cobrir:
- **O baú não reequipa nada ao restaurar** — só marca-se aberto e troca o sprite. A arma
  empunhada atravessa a cena por conta própria; equipar de novo entregaria uma segunda arma,
  possivelmente diferente da que o jogador carrega.
- **O Necronomicon renasce se não tiver sido pego.** Ele é instanciado em runtime ao derrotar
  Abdul; sair da cena sem recolhê-lo o destruiria para sempre. `AplicarEstadoSalvo` o
  reinstancia, mas só se `NecronomiconColetado` ainda não estiver marcado.
- **A libertação de Yug-Neth é derivada de `AbdulResolvido`**, não gravada em chave própria:
  os dois desfechos chamam `LibertarYugNeth()` e não existe outro gatilho, então uma segunda
  chave seria uma segunda fonte da verdade com risco de dessincronizar. A restauração chama o
  próprio `LibertarYugNeth()` (idempotente) em vez de `Bind()` direto — é o que garante que o
  `GameManager.RegistrarYugNeth` também aconteça, sem o qual os futuros Portões de Carcosa
  achariam que ele nunca foi libertado.

`AplicarEstadoSalvo` também **destranca a arena** (ver [tranca_de_arena.md](tranca_de_arena.md)):
uma luta já resolvida nunca pode deixar a saída fechada.

**O save agora grava em disco (2026-08-01):** o `RefugioDeLuz` chama `CapturarTudo()` +
`GravarEmDisco()` ao jogador descansar sob a luz. É o **único ponto do jogo que salva**,
como o GDD §8.3 já decidia. Três Refúgios existem no Deserto (Entrada, Santuário, Portões).
Até aqui a gravação estava pronta mas ninguém a chamava — fechar o jogo perdia tudo.

**Falta:**
- **`CarregarDoDisco()` não é chamado por ninguém.** O jogo grava mas nunca lê: começar uma
  sessão nova ignora o arquivo salvo. Falta o ponto de carregamento (menu inicial, ou o
  bootstrap do `GerenciadorDeSave`).
- Resiliência Mental ainda não é persistida (a chave existe, o gancho não).
- O **inventário** não entra no save — ver [systems/inventario_e_consumiveis.md](../systems/inventario_e_consumiveis.md).
- A **incapacitação** do Yug-Neth (se ele está caído) não é persistida — é recuperável dentro
  da mesma sessão via `RefugioDeLuz`, que ainda não foi posicionado em nenhuma cena. Ao trocar
  de cena, um Yug-Neth caído volta de pé.
