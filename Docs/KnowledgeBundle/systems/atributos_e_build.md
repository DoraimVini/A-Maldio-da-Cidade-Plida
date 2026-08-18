---
type: Game System
title: Atributos, Níveis e Build — estado consolidado
description: Todos os números, recursos e sistemas de progressão do jogo num lugar só, para discussão de design
---

# Atributos, Níveis e Build

Documento de **discussão**, montado em 2026-08-14 a partir do código e dos assets reais — não do
que a documentação afirmava. Junta num lugar só tudo que hoje define o personagem: atributos,
recursos, equipamento, progressão e o que já está autorado.

Onde houver divergência entre este documento e outro, **este foi verificado contra o repositório**.

---

## 1. Os dois sistemas de atributos que coexistem

O jogo tem **dois vocabulários de atributo** vivendo ao mesmo tempo. Entender a diferença é
pré-requisito para qualquer discussão de build.

### 1.1 `FichaDeAtributos` — a ficha de combate de **toda unidade**

POCO puro (`Core/Combat`). Toda unidade tem uma: Damião, Cultista, Byakhee, Yug-Neth, Abdul.
Autorada como asset `FichaAtributosConfig`.

| campo | função | default |
|---|---|---|
| `VitalidadeMax` | teto da carne; zerar = abatido | obrigatório > 0 |
| `Ataque` | dano físico bruto do golpe da unidade | 0 |
| `Defesa` | mitigação física | 0 |
| `Conjuracao` | dano anômalo bruto | 0 |
| `ResistenciaAnomala` | mitigação anômala | 0 |
| `ResilienciaMax` | teto da mente — **0 = não tem mente a ferir**, imune ao canal anômalo | 0 |
| `VelocidadeErrante` | velocidade de patrulha | 1.5 |
| `VelocidadeCaca` | velocidade de perseguição | 3.5 |
| `AlcanceDeGolpe` | alcance do corpo-a-corpo | 1.2 |
| `CadenciaDeAtaque` | cadência | 1.2 |

**Dois canais de dano separados**, e essa é a regra central do combate:
- Físico: `Ataque` do atacante, mitigado por `Defesa` do defensor → **Vitalidade**
- Anômalo: `Conjuracao` do atacante, mitigado por `ResistenciaAnomala` → **Resiliência Mental**

### 1.2 `StatType` — o vocabulário dos **modificadores de item**

Enum de 15 entradas (`Inventario/ItemEnums.cs`). É o que um item, Eco ou Artefato pode conceder.
**Não é o mesmo conjunto da ficha** — e essa assimetria é a origem de vários problemas.

| `StatType` | quem consome | vivo? |
|---|---|---|
| `VitMaxima` | `VitalidadeBridge` | ✅ |
| `DefesaFisica` | `VitalidadeBridge` | ✅ |
| `TraumaFisico` | `MaoFisicaBridge` (soma ao dano da arma) | ✅ |
| `TraumaAnomalia` | `MaoFisicaBridge` | ✅ |
| `VigorMaximo` | `GerenciadorDeVigor` | ✅ |
| `RegeneracaoVigor` | `GerenciadorDeVigor` | ✅ |
| `CustoEsquivaVigor` | `GerenciadorDeVigor` | ✅ |
| `CustoCorridaVigor` | `GerenciadorDeVigor` | ✅ |
| `RegenRM` | `GerenciadorEfeitosPassivos.Update` → `Ancorar` | ✅ |
| `DrenoRM` | `GerenciadorEfeitosPassivos.Update` → `SofrerTrauma` | ✅ |
| `RMMaxima` | `VitalidadeBridge.AplicarEfeitoConsumivel` → `Ancorar` | ⚠️ **só como consumível**, nulo como passiva |
| `RCMaxima` | — | ❌ decorativo |
| `Velocidade` | — | ❌ decorativo |
| `Furtividade` | — | ❌ decorativo |
| `DefesaAnomalia` | — | ❌ decorativo |

**Quatro atributos não fazem nada**, e um quinto só funciona em consumível. Um item com
"+Furtividade" pode ser autorado, salvo e equipado sem efeito algum. `Furtividade` e
`DefesaAnomalia` doem mais: furtividade é pilar do jogo, e dano anômalo é o que os chefes usam.

> Desde 2026-08-14, o `PainelDeFicha` marca **`SEM EFEITO PASSIVO`** ao lado de bônus nessas
> entradas, e `AtributosConsumidosTests` impede que essa lista divirja do código.

---

## 2. Os três recursos do jogador

| recurso | teto | onde vive | esgotar = |
|---|---|---|---|
| **Vitalidade** (carne) | 100 (Damião) | `Vitalidade` POCO, via `VitalidadeBridge` | Colapso corpóreo |
| **Resiliência Mental** (mente) | 100 | `ResilienciaMental` POCO, criado pelo `GameLoopBootstrap` | Colapso mental |
| **Vigor** (fôlego) | 100 | `GerenciadorDeVigor` | exaustão (não mata) |

**Resiliência Mental** tem limiar de Pânico em **25%** (`fracaoPanico = 0.25`), que dispara
câmera, áudio e shader. É o recurso mais central do jogo — é a "Lucidez" em tudo menos no nome.

**Vigor** (valores base): corrida **12/s**, esquiva **25** por uso, regeneração **25/s**
(**15/s** exausto), limiar de exaustão em **30**.

> ⚠️ **Assimetria importante:** a Resiliência Mental do jogador **não vem da ficha**. Ela é criada
> pelo `GameLoopBootstrap` a partir de `maxResiliencia = 100`. O campo `ResilienciaMax` da
> `Ficha_Damiao` vale 0. São dois caminhos diferentes para o mesmo conceito — por isso o
> `PainelDeFicha` não exibe `ResilienciaMax`: mostraria 0 contradizendo a barra do HUD.

---

## 3. As cinco fichas autoradas (números reais)

| unidade | Vitalidade | Ataque | Defesa | Conjuração | Resist. Anômala | Resiliência |
|---|---|---|---|---|---|---|
| **Damião** | 100 | **0** | 6 | 0 | **0** | — (vem do bootstrap: 100) |
| **Cultista** | 100 | 14 | 5 | 0 | 0 | 0 |
| **Yug-Neth** | 40 | 0 | 0 | 0 | 0 | 0 |
| **Abdul** | 300 | 8 | 5 | 25 | 20 | 0 |
| **Byakhee** | 500 | 26 | 8 | 20 | 12 | **120** |

Leituras que saltam:

- **Damião tem `Ataque: 0`.** Todo o dano dele vem da **arma equipada**, não da ficha. A ficha do
  jogador serve só para Vitalidade e Defesa.
- **Damião tem `ResistenciaAnomala: 0`** — nenhuma mitigação contra o canal anômalo. Abdul
  conjura a 25 e o Byakhee a 20, direto na Resiliência.
- **Só o Byakhee tem mente a ferir** (`ResilienciaMax: 120`). É o único inimigo que pode ser
  desfeito pelo canal anômalo; todos os outros o ignoram silenciosamente.
- **Yug-Neth é frágil de propósito** (40 de vida, zero defesa) — ele cai e precisa ser reanimado.

### As três armas (o `Ataque` real do jogador)

| arma | dano básico | cooldown | dano da habilidade | cooldown hab. |
|---|---|---|---|---|
| **Cravo de Aklo** | 40 | 0.5s | 30 | 6s |
| **Estilete de Irem** | 30 | 0.3s | 15 | 5s |
| **Alfanje de Alhazred** | 45 | 0.7s | 40 | 5s |

Perfil claro: Estilete rápido e fraco, Alfanje lento e forte, Cravo no meio. O `StatType.TraumaFisico`
de itens **soma** a esses valores.

---

## 4. Equipamento e inventário

- **Mochila:** 12 posições (`DefaultCapacidadeSurvivalHorror`).
- **Corpo:** 7 slots — `Arma`, `Elmo`, `Peitoral`, `Grevas`, `Amuleto`, `Anel`, `MaoSecundaria`.
  A Mão Secundária trava quando a principal empunha arma de duas mãos.
- **Artefatos:** inventário separado; **posse ilimitada, porte de 4** (barra F1–F4). Só valem
  enquanto equipados. Cada um tem passiva + habilidade própria.
- **Barra de ações:** arma + habilidade da arma (Q) + slots E/R aguardando poderes anômalos.

> ⚠️ A barra de itens é uma **janela sobre as 8 primeiras posições da mochila**, não uma barra de
> consumíveis. Equipamento aparece nela; e pegar duas armas empurra consumíveis para fora da
> barra. Decisão de design pendente.

---

## 5. Progressão: níveis e a árvore

### 5.1 Curva de Exposição (XP)

Cap no **nível 12**. Ganha-se Exposição por exploração e eventos narrativos — nunca por grind.

| nível | Exposição acumulada | | nível | Exposição acumulada |
|---|---|---|---|---|
| 1 | 0 | | 7 | 2100 |
| 2 | 100 | | 8 | 2800 |
| 3 | 300 | | 9 | 3600 |
| 4 | 600 | | 10 | 4500 |
| 5 | 1000 | | 11 | 5500 |
| 6 | 1500 | | 12 | 6600 (cap) |

**Cada nível concede exatamente 1 Ponto de Eco.** Logo, uma run completa dá **11 pontos** para
gastar na árvore — número pequeno de propósito, para cada escolha pesar.

### 5.2 A árvore (Labirinto de Carcosa)

Formato do Símbolo Amarelo: **três braços** a partir do centro.

| braço (`CaminhoEco`) | foco |
|---|---|
| **Sobrevivente** | furtividade, mobilidade, escape |
| **Ocultista** | feitiçaria, resiliência mental, rituais |
| **Protetor** | bloqueio, sinergia com Yug-Neth, combate físico |

Hierarquia de nós (`TipoEco`): `Menor` → `Notavel` → `Keystone` (1 na ponta de cada braço) →
`Ponte`.

Um nó (`EcoDef`) carrega: `Id`, nome, descrição, ícone, tipo, caminho, **pré-requisitos** e uma
lista de **`ModificadorFixo`** (o mesmo `StatType` da seção 1.2).

**Gastar pontos só acontece dentro dos Santuários de Carcosa** — não pelo menu. A tensão é
sobreviver até o Santuário carregando os pontos.

### 5.3 ⚠️ O estado real da progressão

| a documentação diz | a verificação mostra |
|---|---|
| "30 nós no total (10 por braço)" | **zero assets `EcoDef`** — nenhum nó autorado |
| ~~sistema funcionando~~ | ✅ **CORRIGIDO em 2026-08-18 (Fase 3):** `ProgressionBridge` auto-instanciado antes de qualquer cena |
| buffs chegam ao jogador | os 4 consumidores rodam permanentemente no fallback: nível 1, sem Ecos |

**A progressão inteira está inerte.** O código existe e é testável; falta ligar (Fase 3 da
refatoração de managers) e falta conteúdo (autorar os nós).

Consequência para o loot: `TabelaDeDrop` libera tiers por `NivelMinimo` comparado ao
`ProgressionBridge.Instancia.NivelAtual`. **Antes da Fase 3 (2026-08-18) isso era sempre 1 e nenhum tier acima do 1 podia cair** — agora o nível sobe de verdade, mas continua faltando quem chame `AdicionarExposicao` no mundo.

---

## 6. Regra que impede explosão de build

Invariante do sistema de loot, que vale registrar aqui porque limita o espaço de build:
**o sorteio escolhe *qual* `ItemDef` cai, mas nunca gera atributos.** Não há rolagem de
propriedades — todo item é autorado à mão. Isso mantém o jogo longe de Path of Exile / Last
Epoch por decisão explícita, e é o que torna 12 níveis suficientes.

---

## 7. Perguntas abertas para a discussão

1. **Os 4 atributos decorativos** (`RCMaxima`, `Velocidade`, `Furtividade`, `DefesaAnomalia`):
   implementar ou remover do enum? Um enum que promete o que não cumpre permite autorar itens que
   mentem. `Furtividade` toca o pilar de stealth.
2. **`Ataque` do Damião é 0 por desenho?** Se o dano sempre vem da arma, o campo é morto para o
   jogador — ou deveria virar um bônus somado ao da arma?
3. **`ResistenciaAnomala: 0` no Damião** é escolha de balanceamento ou lacuna? Hoje ele não tem
   como se defender do canal que os dois chefes usam.
4. **`ResilienciaMax` da ficha vs. `maxResiliencia` do bootstrap** — dois caminhos para o mesmo
   conceito. Unificar?
5. **11 pontos de Eco para 30 nós** — a proporção está certa? Significa tocar ~1/3 da árvore por
   run, o que favorece rejogabilidade mas pode frustrar quem quer um Keystone distante.
6. **Barra de ações espelhando a mochila** — filtrar por tipo, ou dar slots próprios?

---

## Fontes

Verificado em 2026-08-14 contra: `FichaDeAtributos.cs`, `ItemEnums.cs`, `Core/Progression/Progressao.cs`, `ProgressionBridge.cs`,
`EcoDef.cs`, `GerenciadorDeVigor.cs`, `GerenciadorEfeitosPassivos.cs`, `InventoryManager.cs`,
`EquipmentInventory.cs`, `MainInventory.cs`, os 5 assets `Ficha_*.asset` e as 3 armas em
`Core/Abilities`. Análises relacionadas: [inventario_analise.md](inventario_analise.md),
[progressao_labirinto_carcosa.md](progressao_labirinto_carcosa.md),
[ficha_de_atributos.md](ficha_de_atributos.md).
