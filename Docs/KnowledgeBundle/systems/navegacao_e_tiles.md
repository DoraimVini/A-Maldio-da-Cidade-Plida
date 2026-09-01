---
type: Game System
title: Navegação e Tiles
description: Busca de caminho A* sobre a grade isométrica, e os Rule Tiles que tornam o mundo construível.
tags: [navegacao, pathfinding, tilemap, ia, level-design]
---

# Navegação e Tiles

**Criado em 2026-09-01.** Duas peças que se explicam juntas: sem geometria não há o que
contornar, e sem contorno a geometria trava as unidades.

## O estado que isto conserta

A auditoria do tilemap (2026-08-31) mediu o Deserto de Hali:

> A geometria sólida da fase são **quatro bordas de mapa e o Lago de Hali**. Um obstáculo
> dentro da área jogável inteira. Sem tilemap de colisão.

E as nove unidades que se movem — Cultista, Coisa do Cemitério, Espectro, Esqueleto, Nagaraja,
Byakhee, Yug-Neth e o Cone de Gelo — **todas iam em linha reta**: escreviam velocidade na
direção do alvo. Isso nunca incomodou, porque num plano aberto linha reta é o caminho certo.
Era **dívida invisível**, que venceria no dia em que o mapa ganhasse geometria.

## Busca de caminho

### Por que A* na grade, e não NavMesh

O mundo **já é** uma grade. Um NavMesh exigiria pacote novo, é 3D por construção, e produziria
uma **segunda representação do mundo** para divergir da primeira — este repositório já tem
cicatrizes disso: dois números de dano por inimigo, sete zooms de câmera em sete ferramentas,
oito listas de cenas escritas à mão.

### As peças

| Peça | Camada | Papel |
|---|---|---|
| `Celula`, `IMapaDeNavegacao` | **Core (POCO)** | A abstração: coordenada inteira e "dá para pisar aqui?" |
| `MapaDesenhado` | **Core (POCO)** | Mapa a partir de uma string — é o que os testes usam |
| `BuscaDeCaminho` | **Core (POCO)** | A* de 8 direções, sem `UnityEngine` |
| `NavegacaoDoMundo` | Runtime | A ponte: converte mundo↔célula e responde à pergunta |
| `SeguidorDeCaminho` | Runtime | Traduz caminho em direção (ou próximo ponto) |

### Dois detalhes que decidem se funciona em movimento

**Não corta quina.** A diagonal só passa se os **dois** ortogonais adjacentes estiverem livres.
Sem isso o caminho parece válido — as duas células são livres e adjacentes — e o ator, que tem
largura, encosta no vértice e trava. O sintoma se lê como *"a IA travou"*, e a evidência aponta
para o lugar errado.

**Teto de nós.** Com onze Cultistas perseguindo, um alvo inalcançável faria cada um varrer o
mapa inteiro. Isso não é lentidão, é travamento. O teto troca "caminho ótimo sempre" por "o
jogo não congela".

### A ponte pergunta à FÍSICA, não ao tilemap

Foi a decisão de arquitetura da ponte, e veio de uma medição: **tudo que bloqueia neste jogo
está na layer `Obstacle`** — o tilemap `Colisao`, as paredes do Santuário, os nobres
fossilizados do Castelo, e o **Lago de Hali**, que é um `PolygonCollider2D` solto e não pertence
a tilemap nenhum.

Lendo o tilemap, o inimigo contornaria paredes e **atravessaria o lago**. Perguntando à física,
navegação e colisão concordam por construção.

A sonda é **preguiçosa, com cache**: assar o Deserto inteiro no arranque seriam milhares de
consultas para células que ninguém visita.

### Ligada sem reescrever IA nenhuma

O `SeguidorDeCaminho` **não move nada** — devolve uma direção (ou o próximo ponto), e quem
escreve no `Rigidbody2D` continua sendo o componente de cada unidade. Cada uma mantém a própria
aceleração, velocidade e animação.

| Forma | Quem usa |
|---|---|
| `DirecaoPara(alvo)` | `EnemyMovement` (Cultista), Coisa do Cemitério, Espectro, Nagaraja |
| `ProximoPontoPara(alvo)` | Yug-Neth e Esqueleto Invocado, que passam um **destino** ao seguidor de alvo deles |

**Degrada para linha reta** sem malha ou sem caminho — o comportamento de hoje. Um seguidor que
devolvesse zero trocaria "sem malha" por "inimigo paralisado", que é pior.

### Quem contorna e quem não

| Unidade | Contorna? | Razão |
|---|---|---|
| Cultista | sim | onze deles perseguem no Deserto |
| Coisa do Cemitério | sim | caça por faro, e faro não atravessa parede |
| Espectro de Hali | sim | cercar sem contornar é encostar no muro |
| Esqueleto Invocado | sim | nasce numa arena com as Pedras no caminho |
| **Yug-Neth** | sim | **o companheiro** — some atrás do muro e o jogador não entende por quê |
| Byakhee | **não** | **voa**; os rasantes existem porque ele ignora o terreno |
| Cone de Gelo | **não** | é projétil: linha reta é o correto |
| Abdul, Rei em Amarelo, Pedra de Poder | **não** | não se deslocam |

`NavegacaoNasUnidadesTests` guarda os **dois** lados, e exige que unidade nova entre numa das
duas listas.

## Rule Tiles

### O gargalo

Nove assets de tile, **zero Rule Tiles**, nenhuma Tile Palette, e quatro ferramentas construindo
tilemap por código. Construir por código é ótimo para repetibilidade e péssimo para *desenhar*:
você não sente o nível, você o compila.

### Os dois pincéis

- **`RuleTile_Areia`** — sorteia entre as 5 variações de areia a cada célula, sem colisão. Hoje
  o Deserto é pintado com um tile repetido, e a repetição se vê da tela inteira.
- **`RuleTile_Muro`** — `wall_stone` com `ColliderType.Grid`. É o pincel que **torna o Deserto
  construível**: pintar ruína passa a produzir obstáculo.

Usam **`IsometricRuleTile`**, não o `RuleTile` genérico. Em runtime são idênticos; a diferença
está no **editor**, que desenha a matriz de vizinhança em **losango** em vez de cruz. Autorar
regra isométrica num editor quadrado é o caminho curto para regras erradas que parecem certas.

### `Grid` e não `Sprite`, sempre

Num grid isométrico, `ColliderType.Grid` faz o colisor seguir o **losango da célula**;
`Sprite` o deriva do contorno do PNG, que é o **retângulo** em volta do losango — e os cantos
vazios viram parede. O jogador "encosta no nada" perto das quinas, e isso se lê como controle
ruim, nunca como colisão errada.

> ⚠️ **A mina que estava armada.** Os cinco tiles de **areia** — que são chão — estavam em
> `ColliderType.Sprite`, gerando geometria de colisão por célula. Ficava inerte porque o tilemap
> de chão não tem `TilemapCollider2D`: era **mina, não bug**. No dia em que alguém acrescentasse
> um, o Deserto inteiro viraria parede. Todos passaram para `None`, e
> `GeometriaDosTilesTests` proíbe `Sprite` em qualquer tile do projeto.

### O que ainda falta, e não é sistema

Regra de terreno com **bordas e cantos** — areia encontrando rocha, muro dobrando esquina —
exige **arte de canto**. Os nove tiles do projeto são todos `spriteMode: Single`, um sprite
cada. É falta de arte, não falta de ferramenta.

## Nota de build

`FavelaAmarela.Editor.asmdef` lista referências explícitas e não incluía o pacote de Rule
Tiles, então o tipo não resolvia mesmo com o pacote instalado. `Unity.2D.Tilemap.Extras` e
`.Editor` foram acrescentadas.

## Relacionados

- [Auditoria da malha](https://claude.ai/code/artifact/aa47ff6f-cc7e-4eb7-b0c2-c3b444dc4067) — o plano em sete fases que originou este trabalho
- [Ficha de Atributos](ficha_de_atributos.md) — o padrão "um dono por número", que a ponte de navegação segue
