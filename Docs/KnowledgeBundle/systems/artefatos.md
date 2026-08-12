---
type: Game System
title: Artefatos — Inventário de 4 Slots, Passivas e a Barra F1–F4
description: As relíquias com passiva e habilidade próprias. Quatro slots, uma habilidade por Artefato, e a regra de que só vale o que está equipado.
tags: [artefatos, reliquias, habilidades, inventario, hud]
---

# Artefatos

> **Status:** Implementado em 2026-08-11. Motor, UI e os 4 Artefatos autorados. Persistência
> (posse + porte) e **caminho de aquisição por gameplay** entraram em 2026-08-12. Falta
> **wiring de cena** (ver o fim deste documento) e UI para gerenciar os dormentes.

Um **Artefato** é uma relíquia que carrega **uma passiva e uma habilidade ativa própria**. Não
é arma nem armadura: não disputa espaço de corpo, não tem `EquipmentSlot`. Vive num inventário
separado.

## Posse e porte são coisas diferentes (2026-08-12)

**Posse** não tem teto nem custo: um Artefato recolhido fica no inventário de Artefatos para
sempre e **não ocupa espaço no Bolsão Frio**. **Porte** são os **quatro slots** — o resto fica
**dormente**, guardado e sem efeito.

> **Coletar não basta. Um Artefato só concede passiva e habilidade enquanto ocupa um dos
> quatro slots.**

Recolher o quinto Artefato com os quatro slots cheios **não o recusa e não o perde**: ele entra
dormente. Um limite que causasse perda silenciosa de progresso seria a pior leitura possível.

### A distinção de API que o combate depende

| Método | Significado | Quem usa |
|---|---|---|
| `Contem(id)` | Está **portado** num slot (ativo) | `PontoFocalDeReliquia` — o rito do Rei em Amarelo exige a relíquia na mão |
| `Possui(id)` | **Tem**, portado ou dormente | `PortaDeAklo` — carregar o tomo basta, o slot não importa |

Trocar um pelo outro é bug silencioso: o rito passaria a aceitar relíquia guardada, ou a porta
passaria a exigir gerenciamento de slot para deixar entrar numa dungeon. Há teste travando a
distinção em `PosseDeArtefatosTests`.

**Portar implica possuir:** `Equipar` registra a posse se ainda não havia. Exigir posse prévia
no POCO só criaria uma ordem de chamada para o chamador decorar; a política de "só equipa o que
já é seu" mora na `ArtefatosBridge`, que serve a UI.

## O elo que faltava: adquirir por gameplay

Até 2026-08-12 **nenhum caminho de gameplay concedia Artefatos.** O campo `ArtefatoDef.Item`
— o vínculo entre o `ItemDef` coletável e o Artefato — estava autorado nos quatro assets e
**não era lido por nenhuma linha de código**. `ColetavelDeItem` só fazia `Main.Add(...)`, então
recolher o Necronomicon punha um item na mochila e mais nada.

Consequência: os únicos dois lugares que colocavam Artefato no inventário eram o **Carcosa
Debugger** e o **restore de save** (que só restaura o que nunca foi concedido). Como o rito do
Rei em Amarelo exige 3 relíquias **portadas**, **o chefe final do Vertical Slice era
incompletável fora do Editor** — sem erro no console, só um ponto focal que não reagia.

Agora `ArtefatosBridge.ArtefatoDoItem(ItemDef)` lê o vínculo, e `ColetavelDeItem` roteia a
relíquia para o inventário de Artefatos **sem passar pela mochila** — o `ItemDef` é veículo de
entrega, não destino.

Isso **substitui** a regra anterior do [loot_e_drop.md](loot_e_drop.md) ("sempre ativo assim que
coletado"), por decisão do Vini em 2026-08-11. O motivo é escala: o desenho prevê **mais
Artefatos do que quatro**. Com ativação automática, o quinto Artefato quebraria o sistema; com
quatro slots, ele vira **escolha** — carregar um custa deixar outro para trás.

Consequência de UI: a barra de Artefatos tem exatamente quatro posições (F1–F4), uma por slot,
e é leitura direta do inventário.

## Os quatro Artefatos

| Artefato | Fonte | Passiva | Habilidade (ativa) |
|---|---|---|---|
| **Necronomicon** | Abdul Alhazred (Tumba) | `TraumaAnomalia +15` | **Recitar o Aklo** — revela entidades num raio de 10, por 6 s. Custa 12 de RM, recarrega em 25 s |
| **Patuá das Luas Gêmeas** | Quest da Cassilda | `RegenRM +1.5` | **Canção de Cassilda** — Ancoragem imediata de 25 de RM. Sem custo, recarrega em 45 s |
| **Anel do Sinal Amarelo** | Byakhee (Portões das Ruínas) | `Furtividade +0.3`, `RMMaxima +20` | **Resguardo do Sinal** — passos silenciosos por 8 s. Custa 8 de RM, recarrega em 30 s |
| **Coroa de Ossos** | Nagaraja (Templo da Serpente) | `DefesaAnomalia +5` | **Sibilo de Yig** — aplaca serpentinos num raio de 7, por 4 s. Custa 10 de RM, recarrega em 35 s |

**Anel do Sinal Amarelo = "Anel do Byakhee"** — mesmo item, nome único (confirmado 2026-08-10 e
2026-08-11). **Coroa de Ossos ≠ Elmo de Set**: coexistem, um é Artefato e o outro é armadura —
ver a nota de correção em [lore/reliquias_cosmicas.md](../lore/reliquias_cosmicas.md).

### Por que o custo de RM não pode colapsar Damião
`ArtefatoAtivo.PodeAtivar` exige RM **estritamente maior** que o custo. Gastar a última lasca de
Resiliência e entrar em Colapso por causa da própria habilidade seria punição sem aviso — o
jogador não tem como prever o resultado antes de apertar.

## Arquitetura

Divisão POCO/Unity do `CLAUDE.md` §2. **Não** estende a `MaoFisicaBridge`: aquela guarda um
único relógio de habilidade, e aqui são quatro recargas independentes.

| Peça | Camada | Papel |
|---|---|---|
| `IContextoDeArtefato` | Core | O que um efeito **pode** fazer no mundo. O Core declara o vocabulário; o Runtime executa |
| `IEfeitoDeArtefato` | Core | Efeito atômico. `Aplicar(ctx)` |
| `ArtefatoAtivo` | Core | A habilidade: nome, custo de RM, cooldown, duração e a lista de efeitos |
| `ResultadoDeArtefato` | Core (`readonly struct`) | O que o adaptador precisa cobrar |
| `InventarioDeArtefatos` | Core | Os 4 slots. Guarda **ids**, recusa duplicata |
| `ArtefatoDef` | Data (SO) | Dado autorado: passivas + bloco da ativa. `CriarAtivo()` monta o POCO |
| `ArtefatosBridge` | Runtime | Dona do inventário, dos 4 cooldowns e do contexto concreto |
| `MarcadorDeRevelacao` | Runtime | O sinal que paira sobre quem foi revelado |
| `BarraDeArtefatos` | UI | Os 4 slots F1–F4, com recarga |

### Por que o contexto é uma interface do Core
Um efeito precisa mexer no mundo (revelar inimigo, calar passo), mas o Core não pode tocar
`UnityEngine`. Invertendo — o Core **declara** `IContextoDeArtefato` e o Runtime implementa —
os efeitos ficam testáveis com um contexto falso que só anota o que foi pedido. É a mesma
disciplina que já separa `Core/Enemies` de `Runtime/Enemies`.

### Como cada efeito se costura ao que já existia
Nenhum dos quatro exigiu sistema novo, fora a revelação:

- **Ancoragem** → `ResilienciaMental.Ancorar` (já existia).
- **Silêncio** → `PlayerMovement.SilenciarPassos`, espelhando o `MascararOdor` que já existia
  para o faro do Sseth. Gateia só o broadcast contínuo do caminhar; **a Esquiva continua fazendo
  barulho de propósito**, senão Resguardo + Esquiva viraria um apagão sonoro completo.
- **Aplacamento** → `EnemyStateMachine.Atordoar` (já existia).
- **Revelação** → peça nova. O `MarcadorDeRevelacao` é um **objeto filho próprio**, com o
  próprio `SpriteRenderer` em ordem 30000, e não uma alteração no renderer do inimigo: assim
  atravessa parede sem brigar com o `DynamicYSort`, que reescreve o `sortingOrder` do inimigo
  todo `LateUpdate`.

## Passivas: a 4ª fonte do GerenciadorEfeitosPassivos
`GetBonus(StatType)` agora soma **quatro** fontes: equipamento, itens `Chave` na mochila,
**Artefatos portados** e Ecos da Memória. A bridge é ligada por `GameManager` via
`GerenciadorEfeitosPassivos.Bind(artefatos)`, e a barra redesenha pelo evento `OnArtefatosMudaram`.

**Só o que está portado conta.** Dormente não concede passiva nem habilidade — é o que dá
sentido ao limite de quatro ser uma escolha.

> **O Necronomicon deixou de ser `ItemType.Chave`.** Se voltar a ser, a passiva seria contada
> **duas vezes** (uma pelo laço da mochila, outra pelo laço dos artefatos). Há um teste
> guardando isso.
>
> **Nota de 2026-08-12:** nenhum `ItemDef` do projeto tem `ItemType.Chave` hoje, então o laço
> da mochila em `GerenciadorEfeitosPassivos` não encontra nada — é caminho morto, mantido para
> quando existir um item Chave com passiva.

> ⚠️ **Correção de 2026-08-12.** Este documento afirmava que a `PortaDeAklo` "não quebra com a
> migração: ela checa posse por `Id` (`PossuiItemNaMochila`), não por tipo". Isso era verdade
> **só porque o Artefato nunca era concedido de fato** — o `ItemDef` ficava parado na mochila.
> Agora que recolher a relíquia a consome para o inventário de Artefatos, a porta passou a
> checar `ArtefatosBridge.Possui`, com `PossuiItemNaMochila` mantido como **fallback de save
> antigo** (partidas anteriores a esta data têm o tomo só na mochila).

## Persistência (posse + porte)

Formato de `EstadoPersistenteDosArtefatos`: `"portados|possuídos"`. Antes da barra, os ids na
ordem dos slots com vazio virando campo em branco; depois, todos os possuídos. Exemplo:
`"necronomicon,,coroa_de_ossos,|necronomicon,coroa_de_ossos,patua_luas_gemeas"` — o Patuá está
dormente.

**Save sem a barra** é o formato anterior a 2026-08-12: lido como só a lista de portados, com a
posse deduzida deles.

O restore usa `InventarioDeArtefatos.Restaurar(possuidos, portadosPorSlot)`, e **não**
`Adquirir`/`Equipar` — mesmo papel de `Vitalidade.Restaurar`: reconstrução de estado, não ação
diegética. `Adquirir` porta no primeiro slot livre por conveniência, e usá-lo no load
**embaralharia a ordem das teclas** que o jogador escolheu. Entradas inconsistentes (id portado
que não consta como possuído, duplicata em dois slots) são descartadas em silêncio em vez de
derrubar o load.

## Input
Quatro ações novas (`HabilidadeArtefato1..4`, teclas **F1–F4**) no `InputSystem_Actions`,
consumidas em `PlayerMovement.Update()` pelo padrão polled do projeto (`FindAction` em `Awake`
+ `WasPressedThisFrame`). Dígitos 1–8 já são da `BarraDeItens` e Q é a habilidade da arma.

**Invocar um Artefato não trava a FSM** — diferente de golpear, não é ação exclusiva. Mas, por
entrar depois dos guardas de `Update`, herda de graça o bloqueio por diálogo (`MovimentoBloqueado`).

## Wiring de cena

**Automatizado (2026-08-11):** rodar `Tools/FavelaAmarela/Ligar sistemas novos` anexa a
`ArtefatosBridge` e a ponte de persistência dos slots ao Damião, nas 3 cenas. Idempotente.

**Ainda manual:**
1. Montar a `BarraDeArtefatos` no HUD (4 slots) e ligá-la ao campo `barraDeArtefatos` do
   `HUDController` — o `MontarHUDController.cs` ainda não a linka automaticamente.
2. Ligar os `ColetavelDeItem` dos Artefatos para chamar
   `ArtefatosBridge.EquiparNoPrimeiroSlotLivre(id)` ao recolher — **enquanto isso não for
   feito, coletar um Artefato não o coloca em nenhum slot**, e ele não faz nada.

## Relacionados
- [Habilidades de Item](habilidades_de_item.md) — `IEfeitoDeArtefato` é a primeira aplicação real daquele desenho
- [Loot e Drop](loot_e_drop.md) — de onde os Artefatos caem
- [Relíquias de Hali](reliquias_de_hali.md) — as passivas originais
- [Ficha de Atributos](ficha_de_atributos.md) — o que os modificadores afetam
