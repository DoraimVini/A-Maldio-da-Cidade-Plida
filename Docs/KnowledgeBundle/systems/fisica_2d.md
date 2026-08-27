---
type: Game System
title: Física 2D — espaços de coordenada, camadas e colisão
description: O modelo de física do jogo num lugar só — os dois espaços de direção, a taxonomia de camadas, quando usar matriz × excludeLayers × ContactFilter2D, e a colisão de cenário.
tags: [physics, isometric, layers, collision, camera, 2d]
timestamp: 2026-08-27T23:00:00Z
---

# Física 2D — espaços de coordenada, camadas e colisão

Este documento nasceu da **revisão completa de física de 2026-08-27**, pedida pelo Vini depois
de um playtest em que "tudo parecia meio fora". Não era uma coisa: eram nove, e sete delas
tinham a mesma forma — *a peça existe, não dá erro, e a checagem simplesmente não acontece*.

> **A física da Unity não sabe nada de isometria.** Nada em `Physics2D` conhece projeção;
> `Rigidbody2D` opera sempre em XY de mundo. A ilusão isométrica é **inteiramente** renderização
> + mapeamento de input. É por isso que o bug de direção não era "de física": eram dois espaços
> de coordenada que ninguém tinha reconciliado.

---

## 1. Os dois espaços de direção

O jogo tem **dois** espaços, e confundi-los foi a causa raiz da revisão inteira.

| espaço | o que é | quem produz |
|---|---|---|
| **input** | o que o teclado/analógico diz: W = (0,1), D = (1,0) | `PlayerInput` |
| **mundo** | para onde o personagem de fato anda e olha | `BaseIsometrica.ParaMundo` |

A conversão (`Assets/Scripts/Core/Player/BaseIsometrica.cs`):

```csharp
x = input.x - input.y;
y = (input.x + input.y) * alturaDaCelula;   // 0,5 = dimétrico, o cellSize.y do Grid
```

### A tabela dos 8 inputs — o oráculo

| tecla | ângulo cru | ângulo de MUNDO | o que se vê |
|---|---|---|---|
| D | 0° | **26,6°** | cima-direita |
| W+D | 45° | **90°** | reto para CIMA |
| W | 90° | **153,4°** | cima-esquerda |
| W+A | 135° | **180°** | reto para a ESQUERDA |
| A | 180° | **206,6°** | baixo-esquerda |
| S+A | 225° | **270°** | reto para BAIXO |
| S | 270° | **333,4°** | baixo-direita |
| S+D | 315° | **0°** | reto para a DIREITA |

**As diagonais do teclado produzem as cardinais da tela, e vice-versa.** É o comportamento
correto de um iso 2:1 — e até 2026-08-27 **nenhum consumidor de direção sabia disso**:
`LookDirection`, o ataque, a habilidade e a esquiva recebiam o **input cru**. Como a arte do
Damião é top-down cardinal de 4 direções calibrada para tela, **6 dos 8 inputs mostravam o
sprite errado**; só A e D acertavam por coincidência.

Hoje `LookDirection` é a direção **de mundo**, e é ela que vai para todos os consumidores.
Guarda: `EspacoDeDirecaoTests`.

### A base 0,5 não é um literal solto

O manual da 6.4 (*Isometric tilemap grid cells*) diz que a projeção é definida pelo `Cell Size`
do Grid: *"By default, Cell Size of the Isometric Cell Layout is (1, 0.5, 1) which simulates
**dimetric** projection angles. True isometric projection instead uses a Y value of 0.57735."*

O jogo usa `cellSize (1, 0.5)` — **dimétrico**, não isométrico verdadeiro. O `0,5` da conversão
**é** esse `cellSize.y`, e por isso é parâmetro com guarda contra o Grid das cenas, não número
escrito à mão.

---

## 2. Camadas: qual ferramenta para qual pergunta

Camada é **recurso escasso** (32 no total). A regra que a doc da 6.4 sustenta:

| ferramenta | responde a | permanência |
|---|---|---|
| **Camada** | *o que a coisa **é*** — categoria ampla | autoral |
| **Matriz de colisão** | a regra **padrão** entre categorias | autoral, Project Settings |
| **`includeLayers`/`excludeLayers`** | **exceção por objeto** | por instância, nunca global |
| **`ContactFilter2D`** | **queries** — independe da matriz | por chamada |

### `Physics2D.IgnoreLayerCollision` não serve para exceção por objeto

A doc: *"Choose whether to detect or ignore collisions between a specified pair of layers."* É
**global e por par de camada**. Era exatamente o defeito do `EsquivaBridge`: ele mutava um
ajuste global para dar quadros de invencibilidade a **um** ator, e ao restaurar para `false`
**sobrescrevia o que a matriz do Project Settings dizia**. A matriz tem `Enemy × Player`
desligado; depois da primeira esquiva da partida, inimigos passavam a empurrar o Damião
**permanentemente, em todas as cenas**.

O substituto documentado é `Collider2D.includeLayers` / `excludeLayers`. Atenção a um limite
que decidiu o desenho da esquiva: **`excludeLayers` afeta contatos, NÃO queries** — então ele
não esconde uma hurtbox de um `OverlapCircle`. Por isso os i-frames desligam o **colisor da
hurtbox**, não uma camada.

### As camadas do projeto

`Default(0)`, `TransparentFX(1)`, `Ignore Raycast(2)`, `AnomalyBarrier(3)`, `Water(4)`,
`UI(5)`, `Enemy(6)`, `Aliados(7)`, `Player(8)`, `Obstacle(9)`, `Pickup(10)`,
`PlayerHurtbox(13)`, `EnemyHurtbox(14)`.

**`PlayerHitbox(11)` e `EnemyHitbox(12)` foram removidas**, e o motivo é de desenho: a `Hitbox`
deste projeto **não tem colisor** — ela resolve por `Physics2D.OverlapCircle`. Uma camada só
existe para pôr num *colisor*. Se um dia a hitbox virar trigger de verdade, elas voltam.

---

## 3. Golpe e hurtbox: a pergunta certa

O corpo de um ator é **dois** colisores com funções separadas:

- **raiz**: `Rigidbody2D` + colisor **sólido** de movimento (a pegada no chão, achatada 2:1), na
  camada `Enemy`/`Player`;
- **filho `Hurtbox`**: colisor **trigger** cobrindo o corpo desenhado, na camada
  `EnemyHurtbox`/`PlayerHurtbox`.

A máscara do golpe é **só a camada de hurtbox**. Incluir `Enemy` responde a outra pergunta —
*"o que É um inimigo?"* — e traz o colisor da raiz, que não carrega `Hurtbox`.

**A cascata que isso já causou (2026-08-27):** a cena da Tumba tinha um override velho de
`camadaInimigos = 64` (só `Enemy`). Era inofensivo enquanto o golpe resolvia por
`GetComponentInParent<IDanificavel>()`. A migração para `Hitbox` transformou o override em
*"nada é atingível na Tumba"* — a hurtbox é **filha**, e `GetComponentInParent` sobe, nunca
desce. E o sintoma que apareceu **não foi esse**: foi *"o Abdul não invoca mais as Pedras de
Poder"*, porque sem levar dano ele nunca troca de fase.

Guardas: `GolpeAlcancaAHurtboxTests` — a máscara contém a camada da hurtbox, **não** contém a do
colisor de movimento, o código força a camada, a `Hitbox` denuncia alvo sem hurtbox, e **todo
`IDanificavel` garante a sua hurtbox** (o outro lado da máscara estreita: sem hurtbox, o alvo
fica intocável em silêncio).

---

## 4. Colisão de cenário

Toda parede de tilemap:

- camada **`Obstacle`**;
- `TilemapCollider2D` com `compositeOperation = Merge` e `extrusionFactor = 0,00001`;
- `CompositeCollider2D` em **`Outlines`**;
- `Rigidbody2D` **Static**, `gravityScale 0`.

**Por que Outlines.** A doc: *"This is usually the most efficient geometry to use as it produces
far less edges. Continuous edges do not cause unwanted collisions because all edges are
connected."* O preço é que *"nothing will collide in the interior of such geometry"* — quem já
estiver **dentro** da parede não é expulso. Não é problema aqui: o anel de colisão tem duas
células de espessura e todo ator que se move está em `CollisionDetectionMode2D.Continuous`.
`Polygons` é o que a doc chama de *"least efficient"* e serve para detectar interior — caso de
trigger, não de parede.

**Por que Static, explicitamente.** `CompositeCollider2D` tem
`[RequireComponent(typeof(Rigidbody2D))]`, e o corpo que a Unity cria junto nasce **Dynamic com
`gravityScale 1`**. Parede Dynamic é parede que o Damião empurra. Foi por causa dessa exigência
(sem o cuidado do bodyType) que a primeira tentativa de usar Composite, em 2026-08-13, foi
abandonada com um `MissingComponentException` e o comentário *"fica como polish futuro"*.

O ganho medido: **2 622 formas de colisão → 10 contornos** (1 708 no Castelo, 528 na Arena, 528
nos Portões, 270 nas Ruínas Pálidas, 116 no Santuário; dois contornos por cena, o perímetro
externo e o interno do anel).

Ferramenta: `Tools/FavelaAmarela/Física: consolidar a colisão dos tilemaps`.
Guarda: `ColisaoDeCenarioTests`.

---

## 5. Câmera e ordenação

- Ortográfica, **rotação `Quaternion.identity`**, `z = -10`.
- `orthographicSize` **derivado**, nunca escrito à mão: `EscalaDePixel.TamanhoOrtografico`.
  A 1080p e PPU 32 — 4× → referência 270 → **4,21875**; 3× (arenas) → 360 → **5,625**.
- `PixelPerfectCamera` (pacote `com.unity.2d.pixel-perfect`, já dependência via
  `com.unity.feature.2d`): PPU 32, referência 480×270 (640×360 nas arenas), **Pixel Snapping
  ligado**, Upscale e Crop desligados. Ele recalcula o tamanho a cada quadro para a tela real e
  alinha a posição da câmera à grade de pixels — o seguimento é por `SmoothDamp` e produz
  posição fracionária todo quadro.
- **`Transparency Sort Mode = Custom Axis (0,1,0)`** no `GraphicsSettings`, como o manual
  prescreve para isométrico.

**O eixo é desempate, não concorrente do `sortingOrder`.** A doc de
`Camera.transparencySortAxis`: *"This is used for sorting Renderer components when other,
higher priority, criterias fail to distinguish the render order."* `sortingLayer` e
`sortingOrder` mandam. E o desempate é necessário porque `DynamicYSort` escreve
`round(-y × 10)`, um `int`: resolução de 0,1 unidade, ou 3,2 px a PPU 32 — dois atores a menos
de 3 pixels de distância vertical empatam, e sem o eixo a ordem entre eles alterna de quadro em
quadro.

Guardas: `CameraPixelPerfectTests`, `OrdenacaoIsometricaTests`.

---

## 6. Física de impacto e legibilidade

A regra de ficção que governa o impacto: *em Carcosa, quanto mais uma coisa está impregnada,
menos ela se comporta como matéria.* O impacto vira **legibilidade** — o jogador descobre o que
uma coisa é pela forma como ela reage ao golpe, sem uma linha de diálogo.

`CorpoImpregnado.ResistenciaAImpulso` (0 = cede como gente, 1 = não cede):

| ator | resistência | leitura |
|---|---|---|
| Esqueleto Invocado | 0,10 | ossos montados às pressas, quase sem massa |
| Cultista | 0,15 | ainda é gente: leva o safanão e vai para trás |
| Cortesão Pálido | 0,45 | já foi gente: cambaleia, não voa |
| Coisa do Cemitério | 0,60 | caça pesada e ancorada, cambaleia pouco |
| Byakhee | 0,75 | criatura de fora, mas ainda presa a este plano |
| Espectro de Hali | 0,90 | quase não é corpo — o golpe atravessa mais do que empurra |
| Abdul, Rei em Amarelo, Pedra de Poder, Eco | 1,00 | não estão AQUI para serem empurrados |

Os quatro em 1,00 **não têm `Rigidbody2D`**: já são inamovíveis por construção. Marcá-los mesmo
assim é o que torna a imobilidade uma **decisão legível** em vez de a ausência de um componente
— olhando o prefab, *"não cede porque falta uma peça"* e *"não cede porque decidimos assim"*
eram indistinguíveis. Guarda: `HigieneDeFisicaTests.TodoAtorSemCorpo_DeclaraSuaImobilidade`.

---

## 7. Furtividade: o pilar, e como ele estava desligado

A percepção dos inimigos é **100% sonora**. `SomEmitido.RaioEfetivo` é o alcance do som;
`raioAudicao` é a acuidade do inimigo. O alcance real é o **menor dos dois**.

**O defeito (2026-08-27):** `EnemyPerception.HandleSomEmitido` comparava a distância apenas com
o próprio `raioAudicao` (10 no Cultista) e **descartava** `RaioEfetivo`. Agachado (2,0) e
correndo (8,5) eram ouvidos **exatamente igual**. Modo Furtivo, corrida e o abafamento da
tempestade não tinham efeito nenhum em jogo.

**E o código certo já existia, testado:** `CultistaFSM.ReceberEstimuloSonoro` sempre comparou
com `raioEfetivo` — mas ela só é instanciada em teste. O caminho vivo em produção é
`CultistaAI` + `EnemyPerception`. Um POCO testado e morto: o modo de falha mais caro desta casa.

A curva: Furtivo **2,0** · Andando **5,5** · Correndo **8,5**. A tempestade abafa, com um
**piso de 1,2**: sem ele, tempestade cheia levava o Furtivo a 0,8 — menos que a própria pegada
do Cultista — e seria preciso encostar nele para ser ouvido. *O piso é botão de balanceamento;
o playtest decide.* Guarda: `FurtividadeTests`.

---

## 8. A classe de defeito que domina este projeto

Sete dos nove achados de 2026-08-27 tinham a mesma forma: **a peça existe, não dá erro, e a
checagem não acontece**.

- **Máscara de camada em zero.** `ConeDeGelo.camadasQueBloqueiam` e o `layerObstaculos` das duas
  instâncias do Cortesão. Sintoma em jogo: *"atravessa parede"*.
- **Parede fora da camada.** Quatro ferramentas constroem o mesmo tilemap; três punham em
  `Obstacle` e a quarta nunca setou camada nenhuma. A parede continua barrando o jogador (a
  matriz deixa `Default × Player` colidir), então parece certa — o que some é a consulta.
- **Tilemap vazio com colisor.** Zero célula = zero forma. No Inspector, indistinguível de um
  gatilho funcionando.
- **Buffer de query cheio.** `OverlapCircle` enche 8 slots e descarta o resto **em ordem
  arbitrária**. Varrendo todas as camadas, o baú é o que sobra de fora: *"às vezes o E não faz
  nada"*.
- **Documento obsoleto.** A skill dizia que a ordenação estava em `Default`; estava em Custom
  Axis desde o commit `92410413`. A afirmação virou uma fase inteira de plano para uma mudança
  já feita.

**O antídoto, aplicado em todo lugar:** derivar em vez de listar, e quando a lista for
inevitável (porque é decisão de design), pôr um guarda que a confronta com o jogo.

---

## Referências

- `systems/renderizacao_isometrica.md` — profundidade, Y-sort e oclusão por dither
- `systems/combate.md` — hitbox, hurtbox e resolução de dano
- `systems/esquiva.md` — i-frames
- `unity64_gotchas/index.md` — APIs renomeadas na Unity 6.4
- skill `favela-isometric-standards` — as constantes fixas
