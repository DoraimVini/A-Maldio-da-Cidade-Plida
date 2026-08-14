---
type: Game System
title: Análise do Sistema de Inventário (2026-08-14)
description: Auditoria completa do inventário — o que existe, o que está ligado, o que não faz nada
---

# Análise do Sistema de Inventário

Auditoria pedida pelo Vini em 2026-08-14, a partir de três queixas de playtest. **As três
procedem**, mas nenhuma pela razão que aparentava. O sistema não está "quebrado" — está
**incompleto em pontos que não avisam**.

O padrão que se repete: quase tudo existe e está ligado; o que falta é a **última milha** — a
peça que torna o resultado visível ou efetivo. Um sistema que falha em silêncio parece um sistema
que não existe.

---

## Queixa 1 — "Apertar TAB é praticamente um pause, não tem UI de slots"

### O que eu esperava achar, e não achei
O padrão dominante do projeto seria `PainelDeInventario` não estar em cena. **Não é o caso.** Ele
está nas 4 cenas jogáveis, com `raizDoPainel` atribuída, **12** `slotsDaMochila` e **7**
`slotsDoCorpo`, cada entrada com `grupo`, `icone` e `quantidade` apontando para objetos reais.

### A causa real
`PainelDeInventario.Pintar()` desenha slot vazio assim:

```csharp
visual.grupo.alpha = cheio ? opacidadeCheio : opacidadeVazio;  // 0.25 quando vazio
visual.icone.enabled = cheio;                                   // desligado quando vazio
```

Com a mochila vazia, o painel abre, pausa o mundo e desenha **doze grupos a 25% de opacidade com
os ícones desligados**. Se o slot não tiver uma moldura própria (só o `Image` do ícone), não há
nada para ver. O painel estava funcionando — não havia conteúdo.

**E a mochila esteve vazia o tempo todo**, porque o baú da Tumba estava com a Tabela de Drop
desligada (corrigido na 28ª rodada). As duas queixas eram a mesma.

### O que continua faltando de verdade
Mesmo com itens, o painel é **somente leitura**. Não há `Button`, não há arrastar, não há clique
para equipar, largar ou usar. `PainelDeInventario` não tem nenhum caminho de entrada além de
abrir e fechar. Ou seja: é um **visualizador**, não uma tela de inventário.

Isso não é bug, é escopo não implementado — mas explica a sensação de "não tem UI": mesmo
funcionando, ele não deixa fazer nada.

---

## Queixa 2 — "Equipamentos caem na barra de ações"

Procede, e é **por desenho**, não por acidente.

`BarraDeItens` não é uma barra de consumíveis: é uma **janela sobre as 8 primeiras posições da
mochila**.

```csharp
for (int i = 0; i < slots.Length && i < 8; i++) {
    var item = invManager.Main.GetSlot(i);
    if (item.Def.Tipo == ItemType.Consumivel) ...
    else if (item.Def.Tipo == ItemType.Arma || Armadura || Amuleto) ...
}
```

A linha do `else if` mostra que equipamento aparecendo ali foi **previsto e tratado**. Qualquer
item que caia nas posições 0–7 da mochila ocupa a barra, seja poção ou peitoral.

Consequência prática: pegar duas armas empurra os consumíveis para fora das 8 primeiras posições
e eles somem da barra — sem aviso, e sem que o jogador tenha feito nada errado.

**Não há correção "de wiring" aqui.** É decisão de design: ou a barra passa a filtrar por tipo
(mostrando só `Consumivel`), ou ela deixa de espelhar a mochila e ganha atribuição própria de
slots. A segunda é o padrão de ARPG e a que combina com a barra ter significado; a primeira é uma
tarde de trabalho.

---

## Queixa 3 — "Não sabemos se os atributos dos itens influenciam os nossos"

A mais séria das três. A resposta é: **alguns influenciam, quase metade não, e há um bug que
apaga atributos.**

### 3a. Metade do vocabulário de atributos não faz nada

`StatType` declara **15** atributos. Rastreando todo consumidor de `GetBonus`:

| StatType | Consumido por | Efeito real |
|---|---|---|
| `VitMaxima` | `VitalidadeBridge` | ✅ |
| `DefesaFisica` | `VitalidadeBridge` | ✅ |
| `TraumaFisico` | `MaoFisicaBridge` | ✅ |
| `TraumaAnomalia` | `MaoFisicaBridge` | ✅ |
| `VigorMaximo` | `GerenciadorDeVigor` | ✅ |
| `RegeneracaoVigor` | `GerenciadorDeVigor` | ✅ |
| `CustoEsquivaVigor` | `GerenciadorDeVigor` | ✅ |
| `CustoCorridaVigor` | `GerenciadorDeVigor` | ✅ |
| `RMMaxima` | — | ❌ **nada** |
| `RCMaxima` | — | ❌ **nada** |
| `Velocidade` | — | ❌ **nada** |
| `Furtividade` | — | ❌ **nada** |
| `DefesaAnomalia` | — | ❌ **nada** |
| `RegenRM` | só lido no `Update`, ver 3c | ⚠️ |
| `DrenoRM` | só lido no `Update`, ver 3c | ⚠️ |

**Sete atributos são decorativos.** Um item com "+Furtividade" ou "+Resiliência Mental Máxima"
pode ser autorado, salvo, equipado — e não muda nada. Nada avisa: nem console, nem teste.

`Furtividade` e `DefesaAnomalia` doem particularmente, porque furtividade é pilar do jogo e dano
de anomalia é o que os chefes usam.

### 3b. Bug: trocar de equipamento apaga atributos da ficha

`VitalidadeBridge.AtualizarAtributosDeEquipamento()` reconstrói a ficha final:

```csharp
_atributosFinais = new FichaDeAtributos(
    vitalidadeMax: _atributosBase.VitalidadeMax + bonusVit,
    ataque:        _atributosBase.Ataque,
    defesa:        _atributosBase.Defesa + bonusDefesa
);
```

O construtor tem **10 parâmetros**; três são passados. Os outros **sete voltam ao default** a cada
troca de equipamento:

| parâmetro | vira |
|---|---|
| `conjuracao` | 0 |
| `resistenciaAnomala` | **0** |
| `velocidadeErrante` | 1.5 |
| `velocidadeCaca` | 3.5 |
| `alcanceDeGolpe` | 1.2 |
| `cadenciaDeAtaque` | 1.2 |
| `resilienciaMax` | **0** |

Ou seja: o que o `FichaAtributosConfig` do Damião definir além de vida/ataque/defesa é
**destruído no primeiro equipamento trocado**. `ResistenciaAnomala` zerada significa tomar dano
cósmico cheio — e a única pista seria morrer mais rápido sem motivo aparente.

É a mesma família dos bugs de reconstrução posicional que já apareceram no `ArmaResult`
(11 argumentos por posição, campos novos silenciosamente perdidos).

### 3c. `Update` varrendo o inventário inteiro, duas vezes por frame

`GerenciadorEfeitosPassivos.Update()` chama `GetBonus(RegenRM)` e `GetBonus(DrenoRM)` **todo
frame**. Cada chamada percorre: todos os slots de equipamento, **a mochila inteira**, os 4
artefatos e todos os Ecos desbloqueados. Dois varrimentos completos por frame.

Viola a Regra de Ouro 1 (`CLAUDE.md` §4). O custo cresce com o tamanho da mochila — justo o que se
espera que cresça.

### 3d. Duas assinaturas que nunca são desfeitas

```csharp
InventoryManager.Instance.Main.OnSlotChanged -= (i) => NotificarMudanca();
ProgressionManager.Instance.OnEcoDesbloqueado -= (eco) => NotificarMudanca();
```

`-=` com um **lambda novo** nunca casa com o registrado no `+=`. Como o
`GerenciadorEfeitosPassivos` vive no `Player_Damiao` (recriado a cada cena), cada troca de cena
deixa um assinante morto que continua recebendo eventos.

### 3e. Não existe ficha do personagem na UI

Correto: **nada lê `VitalidadeBridge.Atributos` para exibir**. A ficha final é calculada e usada
só internamente. Não há como o jogador — nem você, em playtest — ver o efeito de um item.

Isso é o que torna 3a e 3b invisíveis. Com um painel de ficha, "Resistência Anômala: 0" saltaria
aos olhos na primeira troca de equipamento.

---

## Ordem de correção sugerida

Por relação impacto/custo, não por gravidade:

1. **Bug 3b** (ficha reconstruída com 3 de 10 campos). Corrupção silenciosa de dados de combate,
   correção de poucas linhas. Fazer com que a reconstrução preserve todos os campos.
2. **Bug 3d** (assinaturas vazando). Poucas linhas, evita comportamento fantasma entre cenas.
3. **Painel de ficha** (3e). É o instrumento que torna todo o resto verificável — sem ele,
   qualquer correção de atributo continua não observável.
4. **Bug 3c** (`Update` varrendo tudo). Cachear o bônus e recalcular no `OnBonusChanged`, que já
   existe.
5. **Atributos mortos** (3a). Decidir caso a caso: implementar `Furtividade` e `DefesaAnomalia`,
   ou removê-los do enum. **Um enum que promete o que não cumpre é pior que um enum menor** —
   permite autorar itens que mentem.
6. **Barra de ações** (queixa 2). Decisão de design, não bug.
7. **Interação no painel** (queixa 1). Escopo novo — equipar/largar pela tela.

Os itens 1, 2 e 4 são correções de bug fechadas. O 3 é feature pequena com retorno alto de
diagnóstico. O 5, 6 e 7 precisam de decisão sua antes de código.

---

## O que este documento NÃO afirma

Nada aqui foi verificado em Play Mode. As conclusões vêm de leitura de código, YAML de cena e
rastreamento de chamadores — método que pega wiring ausente e caminhos mortos com confiança, mas
que **não prova comportamento em runtime**. Os itens 3b e 3d em particular merecem confirmação
com o jogo rodando antes de serem dados como fechados.
