---
type: Game System
title: Habilidades de Item — Arquitetura Data-Driven
description: Como parar de exigir uma classe C# nova para cada arma/artefato, mantendo a testabilidade POCO do CLAUDE.md.
tags: [arquitetura, habilidades, itens, armas, artefatos, poco]
---

# Habilidades de Item — Arquitetura Data-Driven

> **Status:** Design escrito em 2026-08-10. **IMPLEMENTADO em 2026-08-27**, na branch
> `develop_items`. As três peças existem: `IEfeitoDeHabilidade` e `HabilidadeComposta` em
> `Assets/Scripts/Core/Abilities/`, `HabilidadeDef` em `Assets/Scripts/Inventario/`. O catálogo
> fechado de efeitos está em `Core/Abilities/Efeitos/` (Dano, TraumaAnomalia, Atordoamento,
> Repulsão, Sangramento, Interrupção).
>
> **Duas divergências do que está escrito abaixo, ambas deliberadas:**
>
> 1. **A assinatura do efeito.** O texto propõe `Aplicar(AlvoDeEfeito alvo)`, com o efeito
>    agindo direto no alvo. O implementado é `Aplicar(ConstrutorDeGolpe golpe)`: o efeito
>    **compõe um `ArmaResult`**. Motivo concreto: `ArmaResult` já é o valor que carrega efeito
>    por todo o pipeline (`Hurtbox`, `EnemyStatusEffects`, `RepulsaoDeImpacto`, `HitStop`).
>    Agir direto no alvo exigiria reescrever esse pipeline inteiro para ganhar a mesma coisa.
>    O `ConstrutorDeGolpe` existe porque `ArmaResult` é um `readonly struct` de **doze**
>    parâmetros, e compor criando um struct novo a cada efeito repetiria os doze argumentos
>    posicionais em cada um.
>
> 2. **O passo 3 do caminho de migração NÃO foi seguido.** Ele dizia que as 3 armas existentes
>    "continuam funcionando sem migrar — troca é opcional". O Vini decidiu o contrário
>    ("migram todas"), e as classes `MacaDeAklo`, `EstileteDeIrem` e `AlfanjeDeAlhazred` foram
>    **deletadas**, junto da `WeaponFactory`. A troca só aconteceu depois de
>    `EquivalenciaDaMigracaoTests` provar igualdade campo a campo nos doze campos do
>    `ArmaResult` e cadência em nove pontos de tempo.

> **Status original (preservado):** Design escrito em 2026-08-10. Não implementado. Nasce de uma pergunta do
> Vini sobre [loot_e_drop.md](loot_e_drop.md): o catálogo de itens vai crescer (árvore de
> tiers + drop em todo inimigo/baú + Artefatos) e o padrão atual de habilidade não escala
> junto sem virar trabalho de programador por item.

## O problema, no código de hoje

Cada arma com habilidade própria é uma classe C# escrita à mão:

```csharp
public sealed class AlfanjeDeAlhazred : IArmaComHabilidade
{
    public ArmaResult ExecuteHabilidade() => new ArmaResult(
        success: true, dano: danoHabilidade, atordoou: true,
        duracaoAtordoamento: duracaoAtordoamento, forcaRepulsao: forcaRepulsao);
}
```

E o `WeaponFactory` mapeia um `enum` fechado (`TipoArmaFisica`) para essas classes:

```csharp
{ TipoArmaFisica.AlfanjeDeAlhazred, () => new AlfanjeDeAlhazred() },
```

Isso funciona bem para 3 armas autoradas com cuidado — é código limpo, testável, cada uma
com sua própria personalidade. O problema é o **custo marginal**: toda entrada nova da
árvore de itens (`loot_e_drop.md`) que precise de habilidade exige, hoje: um valor de enum, um
arquivo de classe novo, e uma linha no `WeaponFactory`. Uma dungeon inteira de armas novas é
uma dungeon inteira de classes C# novas — e cada Artefato (Necronomicon, Coroa de Ossos, os
que vierem depois) precisaria do mesmo tratamento em duplicado, porque `IAnomalyPower` é uma
interface irmã e separada de `IArmaComHabilidade`, sem reaproveitamento entre as duas.

## A saída: efeitos como dado, orquestração como POCO fixo

Composição sobre herança já é a regra de ouro do projeto (`CLAUDE.md` §4.3, `IAnomalyPower`
existe exatamente por isso). A extensão natural é **descer um nível**: em vez de compor
*armas* a partir de classes, compor *habilidades* a partir de **efeitos** pequenos e
reutilizáveis.

### As três peças novas

| Peça | Camada | Papel |
|---|---|---|
| `IEfeitoDeHabilidade` | Core (POCO) | Contrato mínimo: `Aplicar(AlvoDeEfeito alvo)`. Cada implementação é um efeito atômico. |
| `HabilidadeComposta` | Core (POCO) | Implementa `IArmaComHabilidade` **e** `IAnomalyPower`. Guarda uma lista ordenada de `IEfeitoDeHabilidade` + timing (cooldown/duração/custo de RM). Ao executar, aplica os efeitos em ordem. |
| `HabilidadeDef` | Data (ScriptableObject) | Monta uma `HabilidadeComposta` a partir de dados: qual efeito, com quais números. É o que o designer edita no Inspector — **sem abrir o editor de código**. |

### O catálogo de efeitos (fechado, pequeno, cresce devagar)

Um número pequeno e cuidadosamente autorado de efeitos primitivos cobre o que as 3 armas já
fazem e o que as passivas dos Artefatos precisam:

- `EfeitoDeDano` (direto, já existe como conceito em `ArmaResult.Dano`)
- `EfeitoDeAtordoamento` (duração) — o que o Alfanje já faz
- `EfeitoDeRepulsao` (força) — idem
- `EfeitoDeSangramento` (acúmulo, duração) — o que o Estilete já faz (`Sangramento`,
  `ExplosaoDeSangramento` já são POCOs prontos, viram efeito sem reescrever nada)
- `EfeitoDeInterrupcao` — o que o `MacaDeAklo` já faz (ver `Docs/KnowledgeBundle`)
- `EfeitoDeDrenoDeRM` / `EfeitoDeCuraDeRM` — cobre a passiva do Patuá e possíveis Artefatos
- `EfeitoDeBonusTemporario(StatType, valor, duração)` — buff/debuff passageiro

Este catálogo **é** a mesma disciplina de escopo contido do `loot_e_drop.md`: efeito novo
ainda é decisão deliberada e rara (precisa de código), mas *combinação* de efeitos
existentes — que é 90% do que uma arma nova precisa — vira dado puro. O Alfanje, hoje 60
linhas de classe, reduz a **um `HabilidadeDef` com dois `EfeitoDeAtordoamento`+
`EfeitoDeRepulsao` configurados**.

### O efeito que mudou tudo: `DanoDaArma` (2026-08-28)

O catálogo acima abriu com `EfeitoDeDano` — **dano plano, autorado na habilidade**. Foi assim
que as 3 armas nasceram, e foi o defeito estrutural do sistema de itens inteiro.

`Habilidade_AlfanjeDeAlhazred` é **um asset só, pendurado na família**. Todo Alfanje que existir
aponta para ele. Logo: **dois Alfanjes são sempre idênticos, e um Alfanje melhor é
inexprimível** — não há campo onde escrever que este é melhor que aquele. A Forja do Carcosa
Debugger não conseguia criar uma arma mais forte porque o número não estava no item.

Num ARPG a **arma é a fonte do dano** e a **habilidade é um multiplicador dela**. É isso que faz
trocar de arma melhorar todas as habilidades de uma vez, e é isso que dá sentido ao tier.

O catálogo ganhou `TipoDeEfeito.DanoDaArma`, cujo `Valor` é um **percentual**:

| Antes | Depois |
|---|---|
| `Golpe do Deserto: Dano 45` | `Golpe do Deserto: DanoDaArma 100%` |
| Melhora quando alguém edita o asset da família | Melhora sozinho toda vez que a arma melhora |

> **Por que um tipo novo em vez de reinterpretar o `Dano`.** `Valor: 45` viraria 45% em silêncio
> — o mesmo asset, lido de outro jeito, sem nenhum erro. E o dano plano continua legítimo: golpe
> de inimigo e habilidade de valor fixo usam-no, e `ResolucaoDeGolpe` os deixa passar intactos
> (`PercentualDoDanoDaArma <= 0` retorna cedo). Os 3 assets de arma migraram explicitamente,
> por ferramenta de Editor, com o **valor esperado preservado**.

> ⚠️ **`TipoDeEfeito` é serializado por índice.** `DanoDaArma` entrou **no fim** do enum. Inserir
> um valor no meio remapearia silenciosamente todo efeito já autorado — a Impregnação viraria
> Sangramento e ninguém veria um erro.

**A calibração dos percentuais foi derivada, não chutada:** `percentual = danoAntigoDaHabilidade
÷ valorEsperadoDaArma`. O ataque básico das três ficou em 100%; as habilidades especiais
guardam a proporção que tinham. A primeira tentativa dividiu pela **média da faixa** em vez do
**valor esperado** (que inclui precisão e crítico) e errou o Golpe do Deserto em 10% — 36,06 no
lugar de 40. O teste pegou.

### Quando ainda escrever uma classe

Chefes e Relíquias com mecânica de verdade única (o Escudo Mágico do Abdul, os Hieróglifos do
Avatar de Set) **continuam merecendo classe própria** — `HabilidadeComposta` é para o caso
comum (a maioria das armas, a maioria dos Artefatos), não uma regra sem exceção. A régua:
se o efeito é "dano/status com número configurável", é dado; se tem lógica condicional própria
(estado, contador, gatilho por fase de luta), é código. Isso é a mesma linha que já separa
`Core/Enemies` (regra pura) de `Runtime/Enemies` (adaptador) no resto do projeto — não é
padrão novo, é o padrão de sempre aplicado aqui.

## Testabilidade

Nada muda na exigência do `CLAUDE.md` §2 Regra 6: `HabilidadeComposta` e cada
`IEfeitoDeHabilidade` são POCOs, testáveis com `new HabilidadeComposta(...)` sem cena nem
Unity rodando. O ganho de teste é inclusive maior — um efeito testado uma vez (`
EfeitoDeAtordoamento` aplica N segundos de atordoamento, correto) vale para toda arma futura
que o use, em vez de recontar a mesma asserção em cada classe de arma.

## Caminho de migração (quando a implementação começar)

Não é ruptura — é extração incremental:

1. Escrever `IEfeitoDeHabilidade` + os efeitos que já existem em forma de lógica dentro do
   Maça/Estilete/Alfanje (extrair, não reescrever — `Sangramento` e
   `ExplosaoDeSangramento` já são reaproveitáveis como estão).
2. Escrever `HabilidadeComposta` implementando as duas interfaces atuais.
3. As 3 armas existentes **continuam funcionando sem migrar** — troca é opcional, não
   obrigatória, para não arriscar o que já está testado e jogável.
4. Todo item novo da árvore de tiers passa a nascer como `HabilidadeDef`, não classe.

## Relacionados

- [Loot e Drop](loot_e_drop.md) — o motivo desta arquitetura existir
- [As Três Armas da Tumba](armas_da_tumba.md) — o padrão atual, que continua válido para os 3
- [Habilidades Anômalas](abilities.md) — `IAnomalyPower`, a metade que este documento propõe
  unificar com `IArmaComHabilidade`
