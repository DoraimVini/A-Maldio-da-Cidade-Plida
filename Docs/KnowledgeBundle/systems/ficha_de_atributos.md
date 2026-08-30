---
type: Game System
title: Ficha de Atributos e Matemática do Combate
description: Atributos base de toda unidade (Vitalidade/Ataque/Defesa/Conjuração/Resistência) e a fórmula de mitigação de dano.
tags: [combat, attributes, balance]
---

# Ficha de Atributos e Matemática do Combate

**Toda unidade do jogo tem uma ficha** — Cultista Amarelo, Damião, Aparição Primordial,
Espectro. Decisão do Vini (2026-07-29): em vez de campos de vida/dano espalhados por cada
`MonoBehaviour`, os números vivem numa ficha autorada como asset.

## Os 6 atributos

| Atributo | Canal | O que faz |
|---|---|---|
| **VitalidadeMax** | — | Teto da [Vitalidade](vitalidade.md) corpórea |
| **ResilienciaMax** | Anômalo | Teto da [Resiliência Mental](resiliencia_mental.md). **0 = a unidade não tem mente a ferir** e ignora todo o canal anômalo |
| **Ataque** | Físico | Dano bruto do golpe corpo-a-corpo da unidade |
| **Defesa** | Físico | Mitiga o dano físico recebido |
| **Conjuração** | Anômalo | Dano bruto das magias (0 se a unidade não conjura) |
| **ResistenciaAnomala** | Anômalo | Mitiga o dano anômalo recebido (defesa mágica) |

Dois canais separados, deliberadamente:

- **Físico:** `Ataque` → mitigado por `Defesa` → fere a **Vitalidade**.
- **Anômalo:** `Conjuração` **ou `ArmaResult.TraumaAnomalia`** → mitigado por
  `ResistenciaAnomala` → drena a **[Resiliência Mental](resiliencia_mental.md)**.

### O canal anômalo por golpe corpo-a-corpo (2026-08-12)

Até aqui o canal anômalo era **só documentação**: `ArmaResult` carregava exclusivamente
dano físico, e `StatType.TraumaAnomalia`/`DefesaAnomalia` existiam no enum de inventário
sem uma única linha que os lesse. Nenhuma arma conseguia ferir uma mente.

Agora `ArmaResult` tem o campo `TraumaAnomalia`, e `EnemyBase` resolve os dois canais no
mesmo golpe. **A mente é um segundo vetor de derrota**: zerar a Resiliência Mental de uma
criatura a abate exatamente como zerar a Vitalidade — uma lâmina de Carcosa pode desfazer
o Byakhee muito antes de vencer os 500 de carne dele.

Quem *não* tem mente ignora o canal de graça, sem `if` por tipo de inimigo espalhado pelo
combate: com `ResilienciaMax` = 0, o `EnemyBase` simplesmente não instancia o objeto
`ResilienciaMental`. A carne é a regra; a mente é a exceção autorada por ficha.

> **O Rei em Amarelo está fora disto, e de propósito.** Ele não tem `EnemyBase`,
> `Vitalidade` nem `IDanificavel` — nenhum `ArmaResult` chega até ele. Ver
> [boss_rei_em_amarelo.md](boss_rei_em_amarelo.md): a punição de sanidade dele é morte
> instantânea por olhar, não uma barra. **Não tente validar o dano anômalo nele.**

> **Damião é um caso especial no `Ataque`:** o dano dele vem da **arma** equipada, não da
> ficha — e desde 2026-08-28 a arma não carrega mais um número, carrega uma **faixa** com
> crítico e precisão (ver *A matemática da arma*, abaixo). O `Ataque` da ficha do Damião é
> o **golpe desarmado**, que por decisão de design é **0** — o gesto de mão vazia existe
> (faz barulho, entra no estado Atacando) mas não mata. Para os inimigos, o `Ataque` da
> ficha **é** o dano do golpe.
>
> ⚠️ **Este bloco dizia "Cravo 40, Estilete 25, Alfanje 60" e os três estavam errados.** O
> Alfanje nunca valeu 60 (o asset dizia 45) e o Estilete nunca valeu 25 (dizia 30). Um
> teste — `LutaContraByakheeTests` — chegou a copiar o 25 daqui e passou meses verde
> defendendo um número que o jogo não usava.

## Fórmula de mitigação (subtrativa com piso)

```
danoFinal = max(danoBruto × 0,15 ,  danoBruto − defesa)
```

A defesa subtrai um valor plano ("a armadura absorve X"), mas o **piso de 15%** garante
que nenhuma pilha de defesa deixe alguém invulnerável — sempre passa um mínimo. Escolhida
(2026-07-29) por ser intuitiva para o jogador e escalar com segurança conforme as
armaduras coletáveis previstas entrarem.

A fórmula é **simétrica**: a mesma função (`MitigacaoDeDano.Aplicar`) resolve o golpe do
Cultista no Damião e o golpe da arma do Damião no Cultista. Vive isolada no Core, testada
em `MitigacaoDeDanoTests`.

## Escala e balanceamento atual

**A escala não é 0–100.** Este documento afirmou isso por meses; o Byakhee tem 500 de
Vitalidade e sempre teve. A tropa comum vive na casa dos 100 e os chefes são múltiplos
dela — é uma escala de tropa, com chefes fora dela de propósito.

| Ficha | Vitalidade | Ataque | Defesa | Conjuração | Resist. Anômala |
|---|---|---|---|---|---|
| `Ficha_Damiao` | 100 | 0 *(desarmado)* | 6 | 0 | 0 |
| `Ficha_Cultista` | 100 | 20 | 5 | 0 | 0 |
| `Ficha_Abdul` | 300 | 8 | 5 | 25 | 20 |
| `Ficha_Byakhee` | 500 | 26 | 8 | 20 | 12 |
| `Ficha_YugNeth` | 40 | 0 | 0 | 0 | 0 |
| `Ficha_Sseth` | 120 | 20 | 6 | 0 | 0 |
| `Ficha_Nagaraja` | 220 | 35 | 7 | 0 | 10 |
| `Ficha_AvatarDeSet` | 450 | 80 | 10 | 0 | 25 |

As três últimas são o **Templo do Povo Serpente** (Dungeon 2), autoradas em 2026-08-29. O
`Ataque` das três **já existia**, hardcoded nos scripts (20, 35, 80), e foi preservado — a
mesma disciplina que corrigiu o Cultista para 20 em vez de deixar a unificação enfraquecê-lo.
Vitalidade e Defesa são novas, derivadas do elenco existente e **abertas a mudança**: são
decisão de design, e vivem no asset para serem mexidas sem tocar em código.

> ⚠️ **Os três não podiam ser abatidos.** `SsethFarejadorAI`, `NagarajaAI` e `AvatarDeSetAI`
> são `MonoBehaviour` puros — sem `EnemyBase`, sem `IDanificavel`, sem Vitalidade. Eles
> **causam dano e não podem receber**. Nunca apareceu em jogo porque não há prefab nem cena do
> Templo; apareceria no dia da montagem, como "o inimigo não morre". A ficha resolve metade:
> falta acrescentar `EnemyBase` aos prefabs quando eles existirem, apontando a ficha e
> definindo o `nivelDaUnidade` — sugestão **3**, o nível em que o jogador sai da Fase 1.

**A identidade de cada um**, para os números não parecerem arbitrários:

- **Sseth Farejador** — tropa. Bate igual ao Cultista (20) de propósito: ele não é mais forte,
  ele **caça por faro**. O que muda é o jogo de furtividade, não a conta de dano.
- **Nagaraja** — elite nomeado, fala Aklo, é `IInteragivel` (tem conversa antes da luta, como
  o Abdul). É o único do Templo com **mente** (Resiliência 60): dá para derrotá-lo pelo canal
  anômalo, o que é coerente com uma criatura que argumenta. Larga a Coroa de Ossos.
- **Avatar de Set** — chefe. Ataque **80**, o maior do jogo, com cadência de 2,0 s: o oposto do
  Byakhee — poucos golpes, cada um devastador. Vitalidade 450 fica **abaixo** das 500 do
  Byakhee de propósito, porque o Templo é conteúdo opcional e punir quem explora seria punir a
  curiosidade. Sem mente: é um avatar de deus, não há o que argumentar.

> ⚠️ **O `Ataque` do Cultista era 14 no asset e 20 em jogo.** Cada inimigo carregava
> **dois** números de dano — o da ficha e um campo serializado no `MonoBehaviour` — e só o
> segundo rodava. Em 2026-08-28 os dois foram unificados na ficha, e a ficha foi corrigida
> para **20**: o número que o jogo jogava é o que foi testado em playtest, e uma
> refatoração não pode enfraquecer um inimigo em 30% de passagem.
> `EncontrosCalibradosTests` guarda isso agora.

### A escala por nível (2026-08-28)

Cada ficha declara **quanto cresce por nível**, e a lei é uma só —
`Core.Progression.EscalaDeNivel`:

```
valor(nível) = base × (1 + ganhoPorNível × (nível − 1))
```

Linear, e no **nível 1 devolve exatamente o valor autorado** — nenhum asset precisou ser
reescrito quando a escala entrou.

| Taxa | Padrão | Efeito no nível 3 |
|---|---|---|
| `VitalidadePorNivel` | 0,30 | Damião: 100 → **160** |
| `AtaquePorNivel` | 0,25 | Cultista: 20 → **30** |
| `DefesaPorNivel` | 0,15 | Damião: 6 → **7,8** |

O **inimigo** escala pelo `nivelDaUnidade` autorado no prefab (por instância, não por
ficha: o mesmo Cultista vale 1 no Deserto e mais no endgame). O **Damião** escala pelo
nível de Exposição — ver [progressao_e_ecos.md](progressao_e_ecos.md).

## A matemática da arma (2026-08-28)

Até esta data a arma não tinha dano nenhum: o número vivia num `HabilidadeDef`, **um asset
por família**. Todo Alfanje era idêntico e *um Alfanje melhor era inexprimível* — não havia
campo onde escrever que este é melhor que aquele. Num ARPG a arma é a fonte do dano e a
habilidade é um multiplicador dela; o projeto estava construído ao contrário.

O `BaseDeArma` passou a carregar o bloco de combate:

| Campo | Papel |
|---|---|
| `DanoMinBase` / `DanoMaxBase` | O **dano branco**, como faixa, no nível 1 |
| `ChanceCriticaBase` | Probabilidade de crítico |
| `MultiplicadorCritico` | Quanto o crítico multiplica |
| `PrecisaoBase` | Chance de acertar — **errar é dano zero** (modelo D2, decisão do Vini) |

E a habilidade passou a carregar um **percentual** (`TipoDeEfeito.DanoDaArma`): "Golpe do
Deserto: 100% da arma". Trocar de arma melhora todas as habilidades de uma vez, que é o
loop que faz o loot valer a pena.

**As três armas da Tumba**, calibradas com o *valor esperado inalterado* — nenhuma ficou
mais forte ou mais fraca na média; o que mudou foi a textura:

| Arma | Faixa (nv 1) | Crítico | Precisão | Esperado | Cadência |
|---|---|---|---|---|---|
| Alfanje de Alhazred | 40 – 61 | 5% × 2,0 | 85% | **45,1** | 0,70 s |
| Cravo de Aklo | 33 – 49 | 8% × 1,7 | 92% | **39,8** | 0,50 s |
| Estilete de Irem | 24 – 35 | 12% × 1,6 | 95% | **30,0** | 0,30 s |

O Alfanje é a arma que erra e explode; o Estilete quase nunca erra e quase nunca dói. É a
mesma identidade das três de sempre, agora expressa em mais de um número.

### A ordem em que o golpe fecha

`Core.Combat.ResolucaoDeGolpe` — POCO puro, aleatoriedade injetada, no molde do `Bloqueio`:

```
1. dano  = faixa rolada [DanoMin, DanoMax] × fator do nível do item
2. dano *= percentual da habilidade
3. dano += bônus planos de afixo
4. dano *= (1 + % de aumento de afixo)
5. acerto:  sorteio > precisão  → ERROU, dano 0
6. crítico: sorteio < chance    → dano × multiplicador
7. MitigacaoDeDano.Aplicar(dano, defesa)     ← INTACTA
```

**Contrato de sorteios**, que importa para teste determinístico: **2 números quando o golpe
erra** (faixa + acerto), **3 quando conecta** (faixa + acerto + crítico). O retorno
antecipado no erro é de propósito — um golpe que não aconteceu não critica.

**Sorteio por golpe, não por alvo:** uma pancada que varre três inimigos acerta ou erra os
três juntos. É a leitura de "você deu um golpe bom". Quando os inimigos ganharem Evasão
própria, a rolagem por alvo passa a valer a pena — hoje ninguém tem.

## O bug de serialização das fichas (corrigido em 2026-08-12)

Os `.asset` de ficha foram autorados quando os campos do `FichaAtributosConfig` eram
`camelCase` (`vitalidadeMax`, `ataque`, `defesa`, `resistenciaAnomala`…). Depois os campos
em C# viraram `PascalCase` **sem nenhuma migração** — sem `[FormerlySerializedAs]`, sem
reescrita dos assets.

A Unity casa dado serializado com campo **por nome exato**. Resultado: durante um período
indeterminado, **toda ficha do projeto ignorou silenciosamente os valores do disco** e caiu
nos defaults da classe C# (`100 / 24 / 5 / 0`). Nada disso aparecia no console, e os
`.asset` continuavam mostrando os valores certos no Inspector.

O caso mais grave era o **Byakhee**, um dos dois bosses do Vertical Slice:

| Campo | Autorado no asset | O que rodava de fato |
|---|---|---|
| `vitalidadeMax` | 500 | **100** |
| `ataque` | 26 | **24** |
| `defesa` | 8 | **5** |
| `resistenciaAnomala` | 12 | **0** |

Ou seja: o boss lutava com **um quinto** da vitalidade projetada e sem nenhuma resistência
anômala. Qualquer balanceamento feito por playtest antes desta data foi feito contra
números que não eram os da ficha.

**Correção:** cada campo do `FichaAtributosConfig` ganhou
`[FormerlySerializedAs("<nomeAntigo>")]`, que faz a Unity remapear o dado antigo na carga.
Não remova esses atributos sem antes reescrever os `.asset`.

**Consequência prática:** o balanceamento de todos os encontros precisa ser reavaliado em
playtest, porque os números reais mudaram de verdade agora — em especial o Byakhee, que
ficou 5× mais resistente do que estava na prática.

**Contas que fecham esses números** (refeitas em 2026-08-28 — as anteriores usavam uma
Defesa de 4 que o Damião não tem desde 2026-08-12, e os danos de arma errados acima):

- Golpe do Cultista no Damião, nível 1: `max(20×0,15 ; 20−6)` = **14** → **8 golpes** até o
  Colapso. Punitivo o suficiente para empurrar ao stealth, com janela de fuga. No nível 3 o
  Damião aguenta **12** — a progressão se sente contra a tropa que ele já conhece.
- Armas contra o Cultista (defesa 5), no valor esperado e no nível 1: Alfanje `45,1−5=40,1`
  → **3 golpes**; Cravo `39,8−5=34,8` → **3 golpes**; Estilete `30,0−5=25,0` → **4 golpes**,
  mas batendo 2,3× mais rápido que o Alfanje.
- **Byakhee, o encontro que estava quebrado.** O Vini jogou e relatou: *"não tem como ganhar
  da Byakhee, os itens são fracos demais."* Estava certo, e a causa não era o número do
  chefe — era o Baú da Tumba entregar a arma travada no **nível 1** para sempre. Com a arma
  no nível 3, que é onde a Exposição põe o jogador ao chegar nos Portões:

  | Arma (nv 3) | Faixa | Esperado | vs Defesa 8 | Golpes |
  |---|---|---|---|---|
  | Alfanje | 60,0 – 91,5 | 67,6 | 59,6 | **9** |
  | Cravo | 49,5 – 73,5 | 59,8 | 51,8 | **10** |
  | Estilete | 36,0 – 52,5 | 45,1 | 37,1 | **14** *(cadência 0,30 s)* |

  E o Byakhee precisa de **9** garradas contra o Damião nível 3 (26 → 18,2 contra Defesa
  7,8, sobre 160 de Vitalidade). **Era 14 contra 5.** `LutaContraByakheeTests` roda a luta
  com os POCOs reais sobre cinco sementes fixas e afirma que as três vencem.

Para re-balancear, edite os assets em `Assets/FavelaAmarela/Config/` — **sem tocar código**.

## Arquitetura

- POCO `FavelaAmarela.Core.Combat.FichaDeAtributos` — imutável, valida na construção.
- `FichaAtributosConfig` (`ScriptableObject`, Runtime) autora os valores no Inspector e
  produz o POCO via `CriarFicha()`. Um asset por tipo de unidade.
- O Core **não** conhece `ScriptableObject`: o SO nasce por cima do POCO, nunca o contrário.

## Feedback visual (provisório)

Enquanto não há animações de golpe/impacto, todo dano aplicado spawna um **número
flutuante** (`DanoFlutuante`) na posição do alvo — pedido explícito do Vini como forma de
verificar que dano, mitigação e cadência funcionam. Cores distintas: dano no Cultista em
amarelo-pálido, dano no Damião em vermelho. É um **diagnóstico**, substituível por VFX
diegético depois sem tocar o Core.

O **Trauma de Anomalia** sai num número de cor própria (amarelo pálido de Carcosa,
`corDoTraumaAnomalo` no `EnemyBase`), para o jogador distinguir os dois canais de relance
enquanto não há VFX. Um golpe que fere carne e mente ao mesmo tempo spawna dois números.
