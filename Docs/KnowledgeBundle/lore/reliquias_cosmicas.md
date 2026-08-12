---
type: Lore Reference
title: As Quatro Relíquias Lendárias
description: Artefatos ancestrais do universo de Lovecraft e Chambers presentes em A Maldição da Cidade Pálida.
tags: [lore, items, artifacts, necronomicon, coroa-de-ossos]
timestamp: 2026-07-27T18:55:00Z
---

# As Quatro Relíquias Lendárias

## 1. O Necronomicon (*Al-Azif*)
* **Obtenção:** Recompensa de vitória contra o Miniboss Abdul Alhazred.
* **Efeito:** Permite a decodificação de inscrições rúnicas anciãs e aumenta o poder anômalo, exigindo controle rígido da Resiliência Mental.

## 2. O Anel do Sinal Amarelo
* **Obtenção:** Drop do Miniboss Byakhee nos Portões das Ruínas.
* **Efeito:** Gravação sacra que concede resguardo contra o pavor cósmico e reduz a detecção de sentinelas biológicas.

## 3. O Set Lendário de Set (4 peças)

O **Set Lendário de Set** é o conjunto de equipamento obtido no Templo da Serpente (Dungeon 2, opcional) e dentro do Castelo de Carcosa (Fase 2). O set completo é necessário para acessar a **sidequest do Avatar de Nyarlathotep**.

| Peça | Slot | Obtenção | Garantido? |
| :--- | :--- | :--- | :--- |
| **Elmo de Set** | Cabeça | Drop do Avatar de Set (Templo da Serpente) | ✅ Sempre |
| **Peitoral de Set** | Peito | Drop RNG do Avatar de Set, Naga (Templo), ou Castelo de Carcosa | ❌ RNG |
| **Grevas de Set** | Pernas | Drop RNG do Avatar de Set, Naga (Templo), ou Castelo de Carcosa | ❌ RNG |
| **Arma de Set** | Arma | Drop RNG do Avatar de Set, Naga (Templo), ou Castelo de Carcosa | ❌ RNG |

### Estado de implementação (2026-08-11)

Três das quatro peças existem como `ItemDef` em `Config/Resources/Itens/`:

| Peça | Id | Slot | Modificadores |
|---|---|---|---|
| **Elmo de Set** | `set_elmo` | Elmo | `DefesaFisica +5`, `VitMaxima +15` |
| **Peitoral de Set** | `set_peitoral` | Peitoral | `DefesaFisica +8`, `VitMaxima +25` |
| **Grevas de Set** | `set_grevas` | Grevas | `DefesaFisica +4`, `VitMaxima +10` |
| **Arma de Set** | — | Arma | **não criada** — forma pendente de decisão do Vini |

A escala segue a spec que já existia para o Elmo em [reliquias_de_hali.md](../systems/reliquias_de_hali.md),
com o Peitoral acima e as Grevas abaixo dele. Para comparação, as armaduras **Inerte** do
catálogo comum dão `DefesaFisica +1` — o Set é deliberadamente outra ordem de grandeza,
porque é armadura de um deus primordial e é o que destranca a sidequest.

**Sem bônus de conjunto por enquanto.** O "set completo" hoje é só um **gate de quest** (acesso
ao Avatar de Nyarlathotep), não um bônus mecânico de 2/4 peças à la ARPG — e não vai virar um
sem decisão explícita do Vini (`CLAUDE.md` §1). O gate também **não é verificável ainda**,
porque depende da Arma de Set, que não existe.

> **Correção de 2026-08-11 — a nota de 2026-07-28 estava errada e foi removida.** Ela dizia que
> *"o Elmo de Set é o item que antes se chamava Coroa de Ossos"*. **Não é.** O Vini confirmou em
> 2026-08-10 e 2026-08-11 que a **Coroa de Ossos é drop do Nagaraja** e é um **Artefato**
> (`ItemType.Artefato`, sem slot de corpo, com a habilidade "Sibilo de Yig" — ver
> [systems/artefatos.md](../systems/artefatos.md)). O **Elmo de Set** é peça de armadura do
> conjunto, drop garantido do Avatar de Set. São **dois itens distintos**, de naturezas
> distintas, que não competem por espaço no corpo. O que segue morto é só a entrada original
> "Coroa de Ossos do Rei em Amarelo", que era outra coisa ainda.

## 4. O Patuá (Patuá das Luas Gêmeas)
* **Obtenção:** Recompensa da conclusão da quest narrativa da **Rainha Cassilda** no Santuário de Yhtill. Não é um drop de combate — é a conclusão de um puzzle/diálogo com a Cassilda (decisão 2026-07-28).
* **Efeito:** Proteção abençoada pela Canção de Cassilda que desacelera o dreno de sanidade no escuro e acelera a regeneração de RM nos Postes de Luz.
* **Nota de nomeação (atualizada 2026-07-30):** O `PatuaPickup.cs` da Zona 5 é um item diferente desta relíquia. Ele **não destrava mais nada** — o Salto Dimensional foi removido do jogo — e seu novo efeito está **pendente de definição**. O rename antes proposto ("Fragmento de Hali do Salto") foi descartado junto com a habilidade.
