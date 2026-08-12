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

> **Damião é um caso especial no `Ataque`:** o dano dele vem da **arma** equipada
> (Cravo 40, Estilete 25, Alfanje 60), não da ficha. O `Ataque` da ficha do Damião é o
> **golpe desarmado**, que por decisão de design é **0** — o gesto de mão vazia existe
> (faz barulho, entra no estado Atacando) mas não mata. Para os inimigos, o `Ataque` da
> ficha **é** o dano do golpe.

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

**Escala unificada 0–100** para todos os atores — barra lê-se como porcentagem e o tuning
é fino.

| Ficha | Vitalidade | Ataque | Defesa |
|---|---|---|---|
| `Ficha_Damiao` | 100 | 0 *(desarmado)* | 6 |
| `Ficha_Cultista` | 100 | 24 | 5 |

> ⚠️ **Nenhum destes números estava valendo até 2026-08-12** — ver a seção seguinte. A
> tabela acima agora reflete os `.asset` de verdade (`Ficha_Damiao` tem defesa **6**, não
> 4 como esta tabela dizia antes).

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

**Contas que fecham esses números:**

- Golpe do Cultista no Damião: `max(24×0,15 ; 24−4)` = **20** → **5 golpes** para derrubar
  Damião (~6 s no corpo-a-corpo, com cadência de 1,2 s). Punitivo o suficiente para
  empurrar ao stealth, com janela de fuga.
- Armas contra o Cultista (defesa 5): Alfanje `60−5=55` → **2 golpes**; Cravo `40−5=35` →
  **3 golpes**; Estilete `25−5=20` → **5 golpes**.

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
