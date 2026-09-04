---
type: Game System
title: Quest de Cassilda — "A Canção Incompleta"
description: A quest do Santuário de Yhtill — 3 fragmentos com a Canção de Cassilda embutida, o recital final sem punição, e como o progresso sobrevive a trocas de cena.
tags: [quest, cassilda, yhtill, fragmentos, santuario, recital]
---

# "A Canção Incompleta" — Quest de Cassilda

Lore, roteiro completo e perfil da personagem em
[lore/cassilda_e_byakhee.md](../lore/cassilda_e_byakhee.md). Este documento é sobre a
**implementação**.

> Cassilda não pode deixar o Santuário — a geometria de Carcosa a prende ao ponto. Os
> diários dos nobres que ela mandou explorar estão espalhados pelo mundo. Ela não pede que
> os tragam de volta; pede as **páginas**, para poder cantar o nome de cada um direito.

## Escopo: 3 fragmentos, não 5

**Decisão do Vini (2026-08-01).** O design original pede 5, mas os de nº 4 e 5 (Crônicas de
Lord Aldaron) ficam no **Templo da Serpente** — dungeon que não existe e está fora do
Vertical Slice. Com 5, a quest seria impossível de fechar; Cassilda só entrega o Patuá com
todos.

| # | Fragmento | Onde | Cena |
|---|---|---|---|
| 0 | Diário de Lady Seraphel | Deserto, perto da chegada (−10, −12) | `Deserto_Hali` |
| 1 | Anotações de Lord Morthis | Tumba (12, 4) | `Tumba_De_Alhazred` |
| 2 | Carta de Lady Vaine | Tumba, perto da Câmara do Baú (30, −12) | `Tumba_De_Alhazred` |

> Os textos dos fragmentos 4 e 5 continuam no doc de lore, prontos para quando a Dungeon 2
> existir. Aumentar `totalDeFragmentos` na Cassilda e criar mais dois `FragmentoDeYhtill` é
> tudo o que será preciso — nada no código muda.

**Decisão do Vini (2026-08-02):** as duas primeiras estrofes da *Canção de Cassilda*
(poema de Robert W. Chambers) foram redistribuídas nos 3 fragmentos acima — cada nobre
escreve um pedaço do que ouve sussurrado no vento antes de morrer. É o que sustenta o
recital final (seção abaixo): a rainha "relê" essas duas estrofes de memória antes de
cobrar as duas seguintes.

## O recital final: entregar tudo não é terminar a quest

**Decisão do Vini (2026-08-02).** Com os 3 fragmentos entregues, a quest **não conclui**
— entra num estado novo, `EstadoDaQuest.Recitando`. Cassilda cantou a Canção por eras até
esquecer as palavras; ela não consegue mais *evocar* o final, mas *reconhece* quando
Damião diz certo. Ele precisa responder as **duas últimas estrofes**, cada uma como
escolha entre 3 opções (`PainelDeEscolha`, mesmo componente da conversa com Abdul).

- **Errar não tem custo mecânico.** Só a reação fria dela — a mesma estrofe reabre no
  próximo aperto de E, sem limite de tentativas. O Santuário é área de calmaria
  (tempestade 0–0) com um Refúgio a poucos passos; um dreno de RM ali seria contradição.
- **Acertar a 3ª não devolve à 4ª se errar a 4ª depois** — nunca volta ao começo. A regra
  vive em `RecitalDaCancao.Responder` (Core, 9 testes).
- **O recital não é persistido.** Sair do Santuário no meio das duas perguntas as reseta
  ao voltar — são ~20 segundos de conversa, não vale o custo de mais chaves de save.
- As opções erradas erram **por tom**, nunca por um detalhe decorável: a canção é sobre
  coisas que não são ouvidas e secam, e as erradas soam heroicas ou esperançosas demais.
  A 4ª estrofe usa o epíteto "Perdida Carcosa", plantado no fragmento da Vaine — quem leu
  os fragmentos tem uma pista real, não decoreba.

Peças novas: `RecitalDaCancao` (Core, POCO, 9 testes) e `CancaoIncompleta.Recital` +
`CancaoIncompleta.Responder` (Core) fazem `Concluir()` exigir fragmentos **e** recital
completos. Em `CassildaNPC` (Runtime), o fluxo avança uma fala por aperto — abertura,
recapitulação, pergunta 3, pergunta 4 — no mesmo ritmo já usado na conversa com Abdul
(`AbdulAlhazredAI`).

## Cassilda agora é um prefab

**Decisão do Vini (2026-08-02).** Até aqui Cassilda era um `GameObject` solto, remontado
em cena por duas ferramentas de Editor diferentes que escreviam o mesmo conteúdo
(risco real de divergência). Agora é
`Assets/FavelaAmarela/Art/Characters/Cassilda/Cassilda.prefab`
(`Tools/FavelaAmarela/Montar Prefab da Cassilda`), com **todo o conteúdo textual** —
saudação, pedido, falas por fragmento, e as falas/perguntas/opções do recital.

O que **fica de fora do prefab**, por não poder: `caixaDeTexto` e `painelDeEscolha` são
referências de **cena** (o Canvas do Santuário), e um asset de prefab não pode apontar
para um objeto de cena. Essas duas são ligadas por `MontarSantuarioDeYhtill`, que também
cria o `PainelDeEscolha` do recital dentro do HUD do Santuário (não existia antes) —
mesmo padrão de `MontarInteracaoEDialogoAbdul`.

`MontarSantuarioDeYhtill` e `MontarCenaDoSantuario` se autocorrigem: se encontram uma
Cassilda que não é instância do prefab (a antiga, solta), destroem e reinstanciam a partir
do asset — sem isso, reexecutar as ferramentas continuaria reaproveitando o objeto velho.

## As peças

| Peça | Camada | Papel |
|---|---|---|
| `CancaoIncompleta` | Core | Estado da quest: quantos entregues, quais, quando fecha. **13 testes.** |
| `FragmentoDeYhtill` | Runtime | Uma página no mundo. `IInteragivel` (botão E), persistente. |
| `CassildaNPC` | Runtime | A rainha: dá a quest, recebe fragmentos, entrega o Patuá. |

## Regras que protegem o progresso

Todas com teste no Core:

1. **Entregar o mesmo fragmento duas vezes não conta.** Sem isso, um duplo-clique ou bug de
   UI inflaria o progresso e daria o Patuá cedo demais.
2. **`Concluir` exige todos os fragmentos** — a rainha não adianta a recompensa.
3. **Concluir duas vezes não dá o Patuá de novo.**
4. **Entregar inicia a quest implicitamente**: o jogador pode achar uma página antes de
   falar com Cassilda.
5. **Falar com ela de novo não reinicia nada.**
6. **`Concluir` também exige o recital completo** (2026-08-02) — ter os 3 fragmentos não
   basta se ainda falta uma estrofe. Ver "O recital final" acima e `RecitalDaCancao`
   (Core, 9 testes).

## Como o progresso atravessa cenas

Os fragmentos estão em **duas cenas diferentes** e Cassilda em uma só — o progresso precisa
sobreviver à troca. Cada etapa vira uma chave no save:

- `Quest.Cassilda.Fragmento{i}` — a página foi **recolhida** do mundo.
- `Quest.Cassilda.Entregue{i}` — a página foi **entregue** à rainha.
- `Quest.Cassilda.Concluida` — o Patuá foi dado.

`CassildaNPC.Start` reconstrói o estado a partir dessas chaves, então voltar ao Santuário
depois de ir à Tumba encontra a quest exatamente onde estava. Ver
[architecture/persistencia.md](../architecture/persistencia.md).

**Entrega automática ao falar:** Cassilda recebe de uma vez tudo o que Damião carrega, com
uma fala por página. Não há tela de seleção — pedir para escolher página por página seria
burocracia num momento que é de luto.

## O Santuário virou cena própria (2026-08-02)

Decisão do Vini: o Santuário deixou de ser área do overworld e virou
`Assets/Scenes/Santuario_Yhtill.unity`, com portal de ida e volta — mesmo padrão da Tumba.
**Cassilda e o Refúgio mudaram-se para dentro**; o marco no Deserto virou só a porta.

| Peça | Onde |
| :--- | :--- |
| Porta (Deserto → Santuário) | `Santuario_Yhtill` no Deserto, chegada `SantuarioDeYhtill` |
| Volta (Santuário → Deserto) | `Saida_Santuario`, chegada `VoltaDoSantuario` |
| Cassilda | dentro, em (0, 2.5) |
| Refúgio (save + Ancoragem) | dentro, em (−4.5, −1) |

**Calmaria sobrenatural:** implementada com um `TempestadeAmbiente` de faixa **0–0**, não
com a ausência de driver. Sem driver, o `EnvironmentState` ficaria no valor inicial dele
(0,3) e o Santuário teria uma tempestade fraca **por acidente** — justamente onde o design
promete silêncio.

> **Armadilha ao mover um NPC de cena:** a Cassilda do Santuário é um objeto **novo**; a do
> Deserto foi removida junto com tudo o que estava configurado nela (Patuá, falas por
> fragmento). As ferramentas precisaram ser reapontadas para a cena nova e reexecutadas.
> Mover um NPC entre cenas não carrega a configuração — é remontagem.

## Primeiro encontro: escolha A/B/C (2026-08-02)

O roteiro do lore sempre teve 3 respostas possíveis à saudação de Cassilda ("Onde estou?" /
"Você está presa aqui?" / silêncio), mas nunca tinha sido ligado — a saudação era mostrada
direto, sem escolha. Agora usa o mesmo `PainelDeEscolha` do recital: a saudação abre o
painel na mesma hora que aparece, o jogador escolhe, Cassilda reage (`reacoesDoPrimeiroEncontro`,
mesma ordem das opções), e só no aperto seguinte de E ela faz o pedido da quest e
`CancaoIncompleta.Iniciar()` roda de fato.

**Puramente cosmético** — a opção escolhida não é salva nem afeta nada além dessa reação.
Isso é intencional: é sabor de caracterização, não um ramo de quest.

Sem `painelDeEscolha` atribuído, o encontro não trava: a escolha vira decoração perdida e
o jogador ainda vê a saudação e o pedido, no mesmo ritmo de 2 apertos.

## Pendente

- **Cassilda já tem sprite real** (2026-08-02, `Cassilda_Sprite.png`, pivô `BottomCenter`
  corrigido). **Fragmentos e o chão do Santuário ainda são placeholder** — o chão virou
  Tilemap isométrico de losango de verdade (`tilemap_isometrico_losango.md`), mas a cor
  do tile ainda é sólida, sem textura de piso desenhada.

## Recompensa

**Patuá das Luas Gêmeas** — `Item_PatuaDasLuasGemeas.asset` + `Patua_DasLuasGemeas.prefab`.
Cassilda o larga no chão ao concluir; Damião recolhe com **E** e ele vai para o inventário.

Efeito no GDD (§148): **−40% de dreno de RM no escuro**. O item existe e é carregado, mas o
**efeito é inerte** — não há sistema de dreno por escuridão ainda.

> **Não confundir com o patuá da Tumba** (`Patua_Pickup.prefab`): são itens diferentes.
> Aquele perdeu o propósito quando o Salto Dimensional saiu do jogo e segue sem efeito
> definido.

A fala final **não cita número de fragmentos** ("Os nomes deles", não "Cinco nomes"): o
roteiro do lore foi escrito para 5, e um número fixo viraria mentira quando o escopo mudasse
de novo.
