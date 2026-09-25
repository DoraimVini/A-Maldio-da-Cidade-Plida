---
type: Game System
title: Habilidades de Item — Arquitetura Data-Driven
description: Como parar de exigir uma classe C# nova para cada arma/artefato, mantendo a testabilidade POCO do CLAUDE.md.
tags: [arquitetura, habilidades, itens, armas, artefatos, poco]
---

# Habilidades de Item — Arquitetura Data-Driven

> **Status:** Design escrito em 2026-08-10. **Não implementado.** Nasce de uma pergunta do
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
- `EfeitoDeInterrupcao` — o que o `CravoDeAklo` já faz (ver `Docs/KnowledgeBundle`)
- `EfeitoDeDrenoDeRM` / `EfeitoDeCuraDeRM` — cobre a passiva do Patuá e possíveis Artefatos
- `EfeitoDeBonusTemporario(StatType, valor, duração)` — buff/debuff passageiro

Este catálogo **é** a mesma disciplina de escopo contido do `loot_e_drop.md`: efeito novo
ainda é decisão deliberada e rara (precisa de código), mas *combinação* de efeitos
existentes — que é 90% do que uma arma nova precisa — vira dado puro. O Alfanje, hoje 60
linhas de classe, reduz a **um `HabilidadeDef` com dois `EfeitoDeAtordoamento`+
`EfeitoDeRepulsao` configurados**.

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
   Cravo/Estilete/Alfanje (extrair, não reescrever — `Sangramento` e
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
