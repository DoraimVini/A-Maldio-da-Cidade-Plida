---
type: Game System
title: Loot e Drop — Tabelas, Graus de Impregnação e Sorteio
description: Como inimigos, chefes e baús entregam itens. Tabela de drop autorada, quatro graus diegéticos e a regra que impede explosão de build.
tags: [loot, drop, itens, raridade, rng, inventario]
---

# Loot e Drop

> **Status:** Design escrito em 2026-08-10. **Motor implementado em 2026-08-11** (fatia
> básica): o sorteio, a tabela e o espólio ao abater existem e estão testados; o `BauDaTumba`
> já consome tabela. Continuam **fora** desta fatia: graus Marcado/Impregnado autorados,
> curva de chance balanceada por arquétipo, e as demais tabelas por arquétipo — ver
> [Pendente de decisão do Vini](#pendente-de-decisão-do-vini). Escopo maior segue **depois**
> do Vertical Slice (ver [roadmap_vertical_slice.md](../roadmap_vertical_slice.md)).

## O que existe hoje (2026-08-11)

| Peça | Arquivo | Camada |
|---|---|---|
| `GrauDeImpregnacao` | `Assets/Scripts/Core/Loot/GrauDeImpregnacao.cs` | Core |
| `IFonteDeAleatoriedade` | `Assets/Scripts/Core/Loot/IFonteDeAleatoriedade.cs` | Core |
| `CandidatoDeDrop` / `ItemSorteado` | `Assets/Scripts/Core/Loot/` | Core (`readonly struct`) |
| `SorteioDeDrop` | `Assets/Scripts/Core/Loot/SorteioDeDrop.cs` | Core — a regra |
| `EntradaDeDrop` / `TabelaDeDrop` | `Assets/Scripts/Inventario/` | Data (SO) |
| `FonteDeAleatoriedadeUnity` | `Assets/Scripts/Itens/FonteDeAleatoriedadeUnity.cs` | Runtime |
| `DropAoAbater` | `Assets/Scripts/Itens/DropAoAbater.cs` | Runtime |
| `ColetavelDeItem.Configurar(...)` | `Assets/Scripts/Itens/ColetavelDeItem.cs` | Runtime |
| `Drop_Cultista` / `Drop_BauDaTumba` | `Assets/FavelaAmarela/Config/Drops/` | Assets |
| `SorteioDeDropTests` (16 casos) | `Assets/Tests/EditMode/SorteioDeDropTests.cs` | Testes |

**Dois modos de sorteio**, porque baú e inimigo têm semânticas diferentes:
`Sortear(...)` roda cada entrada por chance independente (inimigo pode largar nada, um ou
vários, até o teto); `SortearUm(...)` escolhe **exatamente uma** entrada ponderada pela chance
— é o baú, que sempre entrega uma peça.

> **Falta wiring de cena/prefab:** o `DropAoAbater` ainda não está anexado a nenhum prefab de
> inimigo, e o campo `tabela` do `BauDaTumba` na cena precisa apontar para `Drop_BauDaTumba`
> (o array `armasPossiveis` antigo foi removido). Passo manual no Editor.

## O que muda em relação à decisão anterior

O `CLAUDE.md` §1 dizia que loot ficaria fora do VS e que a forma exata não estava desenhada.
A segunda parte deixa de valer com este documento; a primeira continua valendo — **o sistema
está desenhado, não agendado**.

Muda também o alcance: o plano antigo (`inventario_e_consumiveis.md`, "Pendentes") era
configurar `ColetavelDeItem` **chefe por chefe, à mão**. O novo é o oposto — **todo inimigo e
todo baú rola drop de arma e armadura**, por tabela. As 3 armas da Tumba deixam de ser
exclusivas do baú: elas são as três primeiras entradas de um catálogo que vai crescer, não um
conjunto fechado.

## A regra que segura o escopo

> ⚠️ **REVOGADA EM 2026-08-27.** A regra abaixo vigorou de 10/08 a 27/08 e está preservada por
> honestidade histórica. A regra em vigor é a da seção seguinte.

> ~~**O sorteio escolhe *qual* `ItemDef` cai. Ele nunca gera atributos.**~~

**Por que caiu.** O Vini apontou o furo que ela criava: sem geração, **uma arma de nível máximo
entrega exatamente os mesmos status de uma arma de nível 1**. A regra protegia contra explosão
de build, mas ao custo de não haver curva de poder nenhuma — e de a segunda cópia de um item
nunca interessar, que é o loop de loot mais fraco que um ARPG pode ter. A escolha dele foi
**base + afixos rolados**, o modelo de D2/PoE.

## A regra em vigor (2026-08-27)

> **O gerador nunca inventa um afixo. Ele escolhe de um pool autorado e rola dentro de uma
> faixa autorada.**

É uma invariante mais fraca que a anterior, mas continua sendo um teto real: o conteúdo é
autorado, o que varia é o *valor* dentro de limites que uma pessoa escreveu. Consequências
diretas:

- **O `ItemDef` passa a ser a BASE**, não o item acabado: slot, ícone, `Tipo`, `Empunhadura`,
  tags, modificadores implícitos, nível do item, e o moveset quando for arma.
- **Grau e afixos vivem na `ItemInstance`**, por instância. Duas cópias da mesma base **não**
  são mais idênticas.
- O `ItemInstance` ganha `Grau`, `NivelDoItem` e `List<AfixoRolado>` — **e o save muda para
  v2**, gravando os **valores rolados**, nunca a semente. Semente re-rolaria todo item já
  dropado assim que um `AfixoDef` fosse editado.
- **`.Def` continua existindo e apontando para a base**, então as 45 chamadas a `.Def` no
  código seguem válidas. É como o D2 funciona: o item tem registro-base, e os mods são da
  instância.
- **Itens únicos continuam autorados à mão** — as 3 armas da Tumba e as relíquias são base +
  modificadores fixos, exatamente o que D2 e PoE chamam de *unique*.
- Toda aleatoriedade passa por `IFonteDeAleatoriedade`, que **já existe e já é fakeada nos
  testes** — o gerador é determinístico sob teste.

### O que isso cobra, e que antes não cobrava

- **Conceder Exposição no mundo virou pré-requisito.** O pool é filtrado por nível do item; com
  `ProgressionBridge.AdicionarExposicao` nunca chamado, o nível fica em 1 e o gerador entrega
  sempre o piso.
- **Os cinco `StatType` decorativos viraram defeito ativo.** Um afixo que role `Furtividade` ou
  `DefesaAnomalia` produz um item que mente para o jogador — `DefesaAnomalia` é exibida na
  ficha e não é aplicada no combate. Ou implementar, ou proibir em pool.
- **Nível do item ≠ nível do jogador.** O gate hoje compara com o nível do *jogador*; num
  sistema de afixos isso faz uma zona inicial dropar tier máximo assim que o jogador sobe.

O que o jogador percebe como "raridade" é **quão difícil é o item cair**, não quão bem ele
rolou.

## Os quatro graus de impregnação

Raridade é diegética: mede **quanto de Carcosa já entrou no objeto**, não "qualidade de loot".
Nada de Comum/Raro/Épico/Lendário na tela (skill `favela-lore-enforcer`).

| Grau | Significado no mundo | Papel mecânico |
|---|---|---|
| **Inerte** | Matéria comum, que Carcosa ainda não tocou. | Base. Sem modificadores ou quase. |
| **Marcado** | Carrega o Sinal em algum canto. | Um modificador pequeno. |
| **Impregnado** | Saturado — o objeto já não é bem um objeto. | Modificador relevante, com contrapartida. |
| **Relíquia** | Peça única e nomeada, com história própria. | Autorada individualmente. Nunca sorteada em tabela genérica. |

Quatro graus é teto, não ponto de partida — o `CLAUDE.md` pede escopo contido, e cada grau a
mais multiplica o trabalho de autoria.

**Relíquias não entram em tabela genérica.** Necronomicon, Patuá das Luas Gêmeas, Anel do
Sinal Amarelo e as 4 peças do Set de Set são drops **roteirizados** de fonte específica (ver
tabela em [GDD_Mestre.md](../GDD_Mestre.md) §3.4). Um Cultista qualquer nunca larga o Elmo de
Set — senão o Templo perde a razão de existir.

**Contrapartida no Impregnado.** Um item muito impregnado deveria cobrar algo (dreno de RM,
ruído a mais), não ser só "melhor". É o que mantém a escassez do survival horror em vez de
virar uma escada de upgrades. *Forma exata pendente de decisão do Vini.*

## A escada de armas (2026-09-01)

**O defeito que ela conserta.** Medido: o jogo tinha **três armas** — as três do baú da Tumba,
entregues no começo. Depois dele não existia no jogo uma arma que o jogador já não tivesse.
Somado aos 80,6% de Inerte no nível 1, **oito em cada dez drops eram uma arma repetida sem afixo
nenhum**. Era catálogo, não matemática.

| Família | T1 | T2 | T3 |
|---|---|---|---|
| **Alfanje** — erra e explode | de Alhazred 40–61 | das Ruínas Pálidas 58–88 | do Rei 84–128 |
| **Maça** — equilibrado | de Aklo 33–49 | de Aldebaran 48–71 | do Sinal Amarelo 69–103 |
| **Estilete** — rápido e certeiro | de Irem 24–35 | de Yhtill 35–51 | da Máscara Pálida 50–74 |

**O tier muda só a faixa de dano.** Crítico, precisão, alcance, raio e cadência são identidade da
*família*: escalá-los junto faria as três convergirem para "a mais forte, com números maiores", e
a escolha entre elas morreria no tier 2. O Alfanje continua sendo o que erra e explode em
qualquer degrau.

Passo de **×1,45 por tier**, contra ×1,25 por nível de item — achar um tier precisa valer mais que
subir um nível, senão o degrau não é um evento.

Cada degrau tem **`HabilidadeDef` própria**, porque ela carrega o `NomeDaArma`, que o jogador lê.

### O afixo agora acompanha o nível

`AfixoDef.Rolar` recebia só a aleatoriedade: a base escalava e o afixo não. O `afixo_cravado`
(+2 a 5) valia 4–11% num Alfanje de nível 1 e **1–3%** num de nível 12 — de marginal a invisível.

Agora escala pela mesma lei do dano branco, com **`EscalaComONivelDoItem` por afixo**. Ficam
planos os que são **taxa por segundo** (RegenRM, RegeneracaoVigor) e os que já são **fração**
(crítico, precisão, aumento percentual): multiplicar RegenRM por 3,75 no nível 12 anularia a
Resiliência como recurso.

Pool ampliado de **8 para 15**, cobrindo os quatro eixos que o combate ganhou em 2026-08-28 e que
nenhum afixo rolava.

### Espaço para armas à distância e de fogo

`BaseDeArma.Entrega` (`TipoDeEntrega`): `CorpoACorpo` — o único implementado —, `Projetil` e
`Fogo`. A matemática de `PerfilDeArma` e `ResolucaoDeGolpe` é **a mesma** para uma lâmina e para
um cano: uma espingarda rola igual a uma espada. O que faltará é munição, recarga e o golpe
resolver no impacto em vez de no gesto.

## A curva de grau por nível (2026-08-28)

**Decisão do Vini**, textual: *"Nível 1: maioria dos itens de mais baixo tier, e construir uma
escala de RNG onde seja possível o drop de uma arma ou armadura lendária na primeira fase, mas
ter um drop realmente baixo. E ir escalonando conforme a progressão de tier e de item, com base
no itemlvl + playerlvl, onde no endgame você ignore totalmente os itens de T1."*

O grau **não é autorado na entrada da tabela** — é sorteado por `Core.Loot.CurvaDeGrau`, com o
grau da entrada servindo de **piso**. Um chefe que declara Impregnado nunca larga Inerte por
azar; um Cultista que declara Inerte pode surpreender.

| Grau | Peso base | Deslocamento por nível |
|---|---|---|
| Inerte | 100 | × 0,75 |
| Marcado | 20 | × 1,15 |
| Impregnado | 4 | × 1,35 |

O peso de cada grau é multiplicado pelo seu deslocamento elevado a `nível − 1`. Nenhum peso é
zero em nível nenhum, e é essa a diferença entre uma **curva** e um **portão**:

| Nível do jogador | Inerte | Marcado | Impregnado |
|---|---|---|---|
| 1 | 80,6 % | 16,1 % | **3,2 %** |
| 6 | 32,8 % | 37,0 % | 30,2 % |
| 12 (teto) | **1,7 %** | 44,3 % | 54,0 % |

Portão ("Impregnado só a partir do nível 5") faz o loot da primeira fase ser sempre igual e
tira o motivo de abrir o próximo baú. Peso baixo produz a história que o jogador conta depois.
No teto, o Inerte **some por peso, não por bloqueio** — o que faz um drop ruim no endgame ser
azar, e não bug.

**Relíquia nunca é sorteada.** `CurvaDeGrau.EhSorteavel` a exclui, e `RegrasDeGrau.PodeSerGerado`
delega para lá — a regra tem uma fonte só, porque duas cópias em camadas diferentes divergem em
silêncio e o sintoma seria uma relíquia aleatória. Uma relíquia autorada numa tabela de chefe
**atravessa a curva intacta**.

### O nível do item acompanha o jogador

`nivelDoItem = max(nivelDaTabela, nivelDoJogador)` no momento em que o item é obtido. A tabela
declara o **piso** (um chefe nunca larga item de nível 1), e o jogador puxa para cima (o Deserto
deixa de entregar tier 1 no endgame).

O nível do item multiplica a faixa de dano branco pela `EscalaDeNivel` (+25% por nível) e abre o
pool de afixos, que é filtrado por `AfixoDef.NivelMinimoDoItem`.

> ⚠️ **Isto vale para as TRÊS portas de entrada de item**, e por muito tempo valeu só para uma.
> `DropAoAbater` rolava certo; o **Baú da Tumba** montava `new ItemInstance(id, 1)` à mão e o
> **pickup autorado em cena** também. As três armas da Tumba — a única fonte de arma do jogo até
> o primeiro chefe — nasciam no piso da escala e ficavam lá para sempre. Era a causa real do
> *"não tem como ganhar da Byakhee"*: a arma no nível 1 precisa de 14 acertos contra os 5 do
> chefe. Corrigido em 2026-08-28; `ItemizacaoDestravadaTests` guarda as três portas.

**Consumível e chave não rolam grau.** Só Arma, Armadura e Amuleto passam pelo gerador — um
"Tônico Impregnado" seria ruído diegético, e nenhum dos dois tem afixo ou escala.

## Arquitetura

Segue a divisão POCO/Unity do `CLAUDE.md` §2 — a regra do sorteio é lógica pura e testável
sem a Unity; a instanciação no mundo é adaptador.

| Peça | Camada | Papel |
|---|---|---|
| `TabelaDeDrop` | Data (ScriptableObject) | **O que esta fonte pode largar**: entradas garantidas + entradas por chance. Asset autorado, um por arquétipo (`Drop_Cultista`, `Drop_BauDaTumba`, `Drop_Nagaraja`). |
| `EntradaDeDrop` | Data (`[Serializable]`) | Uma linha da tabela: `ItemDef` + chance + faixa de quantidade. |
| `SorteioDeDrop` | **Core (POCO)** | A regra: recebe a tabela e uma fonte de aleatoriedade **injetada**, devolve a lista de itens sorteados. Sem `UnityEngine.Random` — é o que torna o sorteio testável com semente fixa. |
| `DropAoAbater` | Runtime (MonoBehaviour) | Componente no inimigo. Assina o evento de abatimento da `Vitalidade`, roda o sorteio e instancia os `ColetavelDeItem` no chão. |

### Por que a fonte de aleatoriedade é injetada

`UnityEngine.Random` é estático e global: com ele, nenhum teste de tabela de drop é
determinístico, e a Regra de Ouro 6 (`CLAUDE.md` §4) exige que toda lógica nova em `Core/`
seja testável instanciando o POCO direto. Injetando um `IFonteDeAleatoriedade`, a suíte
EditMode consegue afirmar coisas como *"com semente X, esta tabela larga exatamente o
Estilete"* — e o balanceamento deixa de depender de playtest.

### O que acontece com o `BauDaTumba`

Ele vira **um consumidor da tabela**, não um caso especial. Hoje ele tem `ItemDef[]
armasPossiveis` e um `Random.Range` inline — que é, na prática, uma tabela de drop
hard-coded com 3 entradas de peso igual. Migra para uma `TabelaDeDrop` (`Drop_BauDaTumba`),
e o campo `forcarArma` continua existindo como ferramenta de teste de build.

Isso **não** altera a garantia do `boss_abdul.md`: a luta segue vencível com qualquer uma das
3 armas, porque a premissa é sobre balanceamento do Abdul, não sobre o mecanismo de sorteio.

> **Feito pela metade até 2026-08-28.** A migração para `Drop_BauDaTumba` aconteceu, mas o baú
> parou no meio: sorteava *qual* arma pela tabela e depois montava `new ItemInstance(id, 1)` à
> mão para entregá-la. Nível 1, grau Inerte, zero afixos — o `GeradorDeItem`, a `CurvaDeGrau` e
> a `EscalaDeNivel` existiam, estavam testados, e este caminho não chamava nenhum dos três. É o
> modo de falha que este repositório mais repete: **a peça existe, não dá erro, e a ligação não
> acontece.**

### Ordem de resolução do sorteio

1. **Garantidos primeiro** — o que a fonte sempre larga (Elmo de Set no Avatar, Necronomicon
   no Abdul). Não passa por chance.
2. **Chance depois**, entrada por entrada, cada uma independente das outras.
3. **Teto por fonte** — um máximo de itens por abate, para um inimigo azarado não vomitar a
   tabela inteira no chão.
4. **Nada cai duas vezes** na mesma resolução.

### Onde o item aparece

O sorteio devolve dados; quem materializa é o `DropAoAbater`, instanciando o prefab de
`ColetavelDeItem` já configurado com o `ItemDef` sorteado. O item **fica no chão e é recolhido
com E** — mesma interação deliberada de todo o resto (ver [interacao.md](interacao.md)).
Coleta automática por toque não entra: já foi decidido contra em todo o projeto.

**Chave de save:** drop de inimigo nasce **sem** `chaveDeSave` — é justamente o caso que o
`ColetavelDeItem` documenta como "reaparece a cada carregamento de cena, certo para drops de
inimigo". Item roteirizado de chefe (Necronomicon) é que precisa de chave, porque é único.

## Equipamento: 6 slots + Artefatos à parte

**Revisão de 2026-08-10 (2ª rodada).** O `EquipmentSlot` atual (`Nenhum, Arma, Elmo,
Peitoral, Grevas, Amuleto, Anel`) tem um slot de arma só. A forma real do corpo de Damião,
por decisão do Vini:

| Categoria | Slots | Ocupam de |
|---|---|---|
| Armadura | Elmo, Peitoral, Grevas | 3 |
| Armas | Mão Principal, Mão Secundária | 2 |
| Joia | Anel | 1 |

Seis slots equipáveis, não sete — **`Amuleto` sai do enum de equipamento**. O motivo é o
próximo item: um amuleto (o Patuá) não é mais coisa que se veste, é Artefato.

### Artefatos não ocupam slot

Um Artefato é **sempre ativo assim que coletado** — não compete por espaço de corpo com
arma/armadura, e por isso vive fora do `EquipmentInventory`. Cada um carrega **uma passiva e
uma habilidade ativa própria** (não os `Modificadores` genéricos do `ItemDef` — algo mais
perto de um `IAnomalyPower` individual). Ver arquitetura proposta em
[habilidades_de_item.md](habilidades_de_item.md).

**Os 4 conhecidos hoje:**

| Artefato | Origem | Status do nome/design |
|---|---|---|
| **Necronomicon** | Drop de Abdul Alhazred | Reclassificar: hoje está implementado como `ItemType.Chave` (decisão da rodada anterior, antes deste desenho). Precisa virar `Artefato` — ver conflito abaixo. |
| **Patuá das Luas Gêmeas** | Quest de Cassilda | Existe como `ItemDef` (`Item_PatuaDasLuasGemeas.asset`), hoje `Tipo: Amuleto`/slot Amuleto. Precisa virar `Artefato` sem slot. |
| **Anel do Byakhee** | Drop do Byakhee (Portões das Ruínas) | **Confirmado (Vini, 2026-08-10):** mesmo item que o GDD chama de "Anel do Sinal Amarelo" — nome único, sem duplicata. Renomear a referência no `GDD_Mestre.md` §3.4 quando o catálogo for implementado. |
| **Coroa de Ossos** | Drop do Nagaraja (Z10, Templo da Serpente) | **Confirmado (Vini, 2026-08-10).** Substitui a entrada antiga "1 peça RNG do Set Lendário" do Nagaraja em `templo_da_serpente.md` §IV — o Set Lendário RNG fica só com Avatar de Set (peça garantida) e Z7/Cripta das Larvas (50%). Atualizar `templo_da_serpente.md` quando o Templo for remontado. |

> **Mais artefatos virão** "conforme o progresso do projeto" (palavras do Vini) — a lista
> acima não é teto, é o que já tem nome. Cada um é autorado individualmente, como as
> Relíquias no resto deste documento.

### Conflito com o que já foi implementado

A rodada anterior desta mesma sessão criou `Item_Necronomicon.asset` como `Tipo: Chave`, com
um modificador passivo genérico (`Modificadores: Conjuração +15`) — resposta correta *para o
desenho de então*, que ainda não tinha os Artefatos com par ativo+passivo. Esse desenho
mudou. **Não voltei a mexer no asset agora** — a sessão já tem QA pendente e o par
ativo+passivo do Necronomicon ainda não está definido (qual é a habilidade ativa dele?).
Fica registrado aqui como retrabalho necessário antes da implementação:
1. `ItemType` ganha um valor `Artefato`.
2. `Item_Necronomicon.asset` migra de `Chave` para `Artefato`.
3. `GerenciadorEfeitosPassivos` (que hoje filtra por `Tipo == ItemType.Chave` para aplicar
   bônus passivo de itens na mochila) precisa reconhecer `Artefato` em vez de — ou além de —
   `Chave`. A checagem da `PortaDeAklo` (`PossuiItemNaMochila`) não quebra: ela busca por
   `Id`, não por `Tipo`.

## A Árvore de Itens (com tiers de verdade)

**Revisão de 2026-08-10 (2ª rodada).** Correção do desenho original deste documento: não é
catálogo plano. É árvore — os 4 graus de impregnação (Inerte → Marcado → Impregnado →
Relíquia/Artefato) são **tiers de desbloqueio**, não só rótulo de raridade.

```
Inerte ──(pré-requisito ainda não definido)──▶ Marcado ──▶ Impregnado ──▶ Relíquia
                                                                              │
                                                                     (Artefatos ficam
                                                                      fora da árvore —
                                                                      são nó único, sem
                                                                      pré-requisito de
                                                                      tier: caem prontos
                                                                      de fonte específica)
```

O que isso muda na `TabelaDeDrop`: uma entrada de tabela não é só `ItemDef` + chance — ela
também carrega **de qual tier em diante** aparece. Um Cultista comum no Deserto não deveria
rolar Impregnado no primeiro minuto de jogo; a árvore é o que impede isso, gateando por
progresso do jogador em vez de só por peso de sorteio.

**RESOLVIDO (Vini, 2026-08-11): o gate é o nível de Exposição.** Cada `EntradaDeDrop` carrega
um `NivelMinimo`; o `SorteioDeDrop` descarta as entradas cujo `NivelMinimo` supera o
`ProgressionManager.NivelAtual` do jogador **antes** de rolar a chance. Graus mais impregnados
não precisam de mecanismo próprio — basta autorá-los com `NivelMinimo` mais alto.

> **Divergência doc↔código corrigida:** este documento e o `CLAUDE.md` §1 diziam que nível de
> personagem era "previsto, sem data / sem forma definida". **Não é:** o
> `ProgressionManager` (`Assets/Scripts/Progression/ProgressionManager.cs`) já existe e é
> funcional — tem `NivelAtual`, curva de Exposição (cap 12) e a árvore de Ecos do Labirinto de
> Carcosa. Foi o que permitiu fechar este gate sem inventar sistema novo.

**Entradas `Garantido` furam o gate de propósito** — drop roteirizado de chefe (Necronomicon
no Abdul) tem de cair independente do nível, senão a quest quebra para quem chegou cedo.

Duas consequências de escala que ficam de graça: o balanceamento vira dado autorado (mexer em
`NivelMinimo` no Inspector, sem tocar código), e o gate é testável com semente fixa.

### Armas e armaduras precisam fazer sentido no jogo

Instrução explícita do Vini: **antes de a tabela crescer, pensar em que arma/armadura cabe
no contexto** — Carcosa, os arquétipos de inimigo (Cultista, Sseth, Naga, Espectro), os
locais (Tumba, Templo, Castelo). Não é gerar item por gerar; cada entrada nova da árvore
precisa de motivo narrativo, igual às 3 armas da Tumba (cada uma tem lore própria em
`armas_da_tumba.md`) — não um Maça genérico "arma de dano médio nº 4".

## Pendente de decisão do Vini

Nada abaixo está resolvido — são as perguntas que sobraram do design:

1. **Contrapartida do Impregnado**: o que exatamente o grau alto cobra?
2. ~~**Curva de chance por arquétipo**~~ — **resolvido (2026-08-28):** `Drop_Cultista` larga
   cada entrada com 15 % a 30 %, teto de 2 itens por abate. As tabelas de chefe garantem o item
   de rito e sorteiam arma + armadura a 60 %, com grau **mínimo Marcado** e teto de 2 a 3 —
   *"que ele sinta a progressão do personagem"* (pedido do Vini). O que segura a escassez agora
   é o **grau**, não a frequência: o Cultista larga bastante coisa Inerte, e coisa boa é rara
   pela curva acima.
3. ~~**Nível de personagem**~~ — **resolvido (2026-08-28):** a `TabelaDeDrop` ganhou um
   `nivelDoItem` único (piso da fonte), e não faixa por entrada — o jogador puxa o nível para
   cima, então a faixa por entrada seria uma segunda fonte da verdade para a mesma coisa. E a
   progressão deixou de ser teórica: o elenco concede Exposição de verdade (Cultista 25, Abdul
   150, Byakhee 200), o que põe o jogador no **nível 3** ao chegar no Byakhee. Ver
   [ficha_de_atributos.md](ficha_de_atributos.md) e `EconomiaDeExposicaoTests`.

   > Isto **revoga** para a branch `develop_items` a nota do `CLAUDE.md` §1.1 de que "com o
   > nível travado em 1, o loot só entrega tier 1 — isso é esperado no VS, não bug". Continua
   > valendo para a build do edital, que sai de `develop_manager`.
4. ~~Armadura ainda não existe~~ — **resolvido parcialmente (2026-08-11):** três peças
   **Inerte** autoradas, uma por slot de armadura, cada uma com `DefesaFisica +1` (teto
   propositalmente baixo — bem abaixo do Elmo de Set, que é Relíquia): `Capuz de Farrapos`
   (Elmo), `Colete de Sucata` (Peitoral), `Caneleiras de Ferro Enferrujado` (Grevas). Ainda
   **sem `TabelaDeDrop`** — os assets existem em `Config/Resources/Itens/`, mas nada os
   sorteia ou instancia no mundo ainda (isso depende da arquitetura da seção acima, que
   segue não implementada). Faltam os graus Marcado/Impregnado de armadura.
5. **Pré-requisito de tier da árvore** (Inerte→Marcado→Impregnado): contagem, progresso de
   história ou nível de personagem? Bloqueia a árvore, não o resto do sistema.
6. **Slot `Amuleto` some do `EquipmentSlot`** — **parcialmente resolvido (2026-08-11):** o Patuá
   saiu do slot Amuleto e virou Artefato, então nenhum item usa mais esse slot. O **enum não foi
   mexido** de propósito: `EquipmentSlot` é serializado por índice e removê-lo remapearia todo
   asset e a `anatomia` do `InventoryManager`. Fica como fatia própria.
7. ~~"Anel do Byakhee" × "Anel do Sinal Amarelo"~~ — **resolvido**: mesmo item.
8. ~~Fonte da Coroa de Ossos~~ — **resolvido**: drop do Nagaraja, substitui a peça RNG do Set.
9. ~~Par ativo+passivo de cada Artefato~~ — **resolvido (2026-08-11):** os 4 Artefatos estão
   autorados com passiva e habilidade, e o sistema está implementado. Ver
   [artefatos.md](artefatos.md). A regra de ativação mudou junto: **só vale o que está
   equipado** num dos 4 slots, substituindo o "sempre ativo assim que coletado" da seção acima.

## Relacionados

- [Habilidades de Item — Arquitetura Data-Driven](habilidades_de_item.md) — como sair do
  padrão "uma classe C# por arma" sem perder testabilidade
- [Inventário e Consumíveis](inventario_e_consumiveis.md) — onde o item sorteado vai parar
- [As Três Armas da Tumba](armas_da_tumba.md) — as 3 primeiras entradas do catálogo
- [Ficha de Atributos](ficha_de_atributos.md) — o que os modificadores afetam
- [Labirinto de Carcosa (Progressão)](progressao_labirinto_carcosa.md) — a outra metade da progressão
