---
type: Game System
title: Loot e Drop — Tabelas, Graus de Impregnação e Sorteio
description: Como inimigos, chefes e baús entregam itens. Tabela de drop autorada, quatro graus diegéticos e a regra que impede explosão de build.
tags: [loot, drop, itens, raridade, rng, inventario]
---

# Loot e Drop

> **Status:** Design escrito em 2026-08-10. **Não implementado** — nenhuma linha de código
> existe ainda. Decisão do Vini na mesma data: documentar agora, implementar **depois** do
> Vertical Slice (ver [roadmap_vertical_slice.md](../roadmap_vertical_slice.md)).

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

> **O sorteio escolhe *qual* `ItemDef` cai. Ele nunca gera atributos.**

Essa única regra é o que impede o sistema de virar Path of Exile — a preocupação explícita do
Vini no `CLAUDE.md` §1. Consequências diretas:

- Todo item continua **autorado à mão** e determinístico, preservando a invariante que o
  `ItemDef` já documenta: *"Todos os status são fixos e determinísticos"*.
- Não existe afixo aleatório, nem rolagem de atributo, nem "mesma espada com números
  diferentes". Duas cópias do mesmo item são idênticas.
- O `ItemInstance` continua guardando só `ItemDefId` + quantidade — **nenhuma mudança** na
  serialização de save.
- Um item novo é um asset novo, não uma nova combinação emergente. O catálogo cresce no ritmo
  que o time autora, e não explode sozinho.

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

**Pendente de decisão do Vini, e bloqueia implementação da árvore (não do resto do
documento):** qual é o pré-requisito real? Candidatos, sem decisão tomada:
- Contagem: precisa ter N itens Marcados coletados antes de Impregnados começarem a dropar.
- Progresso de história: tier libera por dungeon completada (ex.: só depois da Tumba).
- Nível de personagem, se a progressão por nível (`CLAUDE.md` §1) entrar em jogo — mas isso
  também está fora do VS e sem forma definida.

### Armas e armaduras precisam fazer sentido no jogo

Instrução explícita do Vini: **antes de a tabela crescer, pensar em que arma/armadura cabe
no contexto** — Carcosa, os arquétipos de inimigo (Cultista, Sseth, Naga, Espectro), os
locais (Tumba, Templo, Castelo). Não é gerar item por gerar; cada entrada nova da árvore
precisa de motivo narrativo, igual às 3 armas da Tumba (cada uma tem lore própria em
`armas_da_tumba.md`) — não um Cravo genérico "arma de dano médio nº 4".

## Pendente de decisão do Vini

Nada abaixo está resolvido — são as perguntas que sobraram do design:

1. **Contrapartida do Impregnado**: o que exatamente o grau alto cobra?
2. **Curva de chance por arquétipo**: um Cultista comum larga arma com que frequência? Se for
   alto demais, mata a escassez que o `CLAUDE.md` §1 protege.
3. **Nível de personagem**: o `CLAUDE.md` prevê progressão por nível junto do loot. Se drop
   escalar por nível, a tabela precisa de faixa de nível por entrada — o que muda a forma da
   `TabelaDeDrop`. **Decidir antes de implementar**, não depois.
4. ~~Armadura ainda não existe~~ — **resolvido parcialmente (2026-08-11):** três peças
   **Inerte** autoradas, uma por slot de armadura, cada uma com `DefesaFisica +1` (teto
   propositalmente baixo — bem abaixo do Elmo de Set, que é Relíquia): `Capuz de Farrapos`
   (Elmo), `Colete de Sucata` (Peitoral), `Caneleiras de Ferro Enferrujado` (Grevas). Ainda
   **sem `TabelaDeDrop`** — os assets existem em `Config/Resources/Itens/`, mas nada os
   sorteia ou instancia no mundo ainda (isso depende da arquitetura da seção acima, que
   segue não implementada). Faltam os graus Marcado/Impregnado de armadura.
5. **Pré-requisito de tier da árvore** (Inerte→Marcado→Impregnado): contagem, progresso de
   história ou nível de personagem? Bloqueia a árvore, não o resto do sistema.
6. **Slot `Amuleto` some do `EquipmentSlot`** — proposto porque o único amuleto conhecido
   (Patuá) virou Artefato sem slot. Confirmar antes de mexer no enum: pode haver amuleto
   equipável não-Artefato no futuro que ainda precise do slot.
7. ~~"Anel do Byakhee" × "Anel do Sinal Amarelo"~~ — **resolvido**: mesmo item.
8. ~~Fonte da Coroa de Ossos~~ — **resolvido**: drop do Nagaraja, substitui a peça RNG do Set.
9. **Par ativo+passivo de cada Artefato**: só a existência dos 4 foi definida, não o que cada
   habilidade ativa faz (a passiva do Patuá já existe: -40% dreno de RM no escuro).

## Relacionados

- [Habilidades de Item — Arquitetura Data-Driven](habilidades_de_item.md) — como sair do
  padrão "uma classe C# por arma" sem perder testabilidade
- [Inventário e Consumíveis](inventario_e_consumiveis.md) — onde o item sorteado vai parar
- [As Três Armas da Tumba](armas_da_tumba.md) — as 3 primeiras entradas do catálogo
- [Ficha de Atributos](ficha_de_atributos.md) — o que os modificadores afetam
- [Labirinto de Carcosa (Progressão)](progressao_labirinto_carcosa.md) — a outra metade da progressão
