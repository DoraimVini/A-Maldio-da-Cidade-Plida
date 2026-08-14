# Labirinto de Carcosa (Sistema de Progressão)

O sistema de progressão de Favela Amarela evita a tradicional "árvore de talentos" de ARPGs para manter o foco no Survival Horror. O XP (Pontos de Eco) é ganho primordialmente por exploração e eventos narrativos (Exposição), desincentivando o grind.

## 1. Moeda: Pontos de Eco
- O jogador ganha "Ecos" ao interagir com sussurros, documentos e memórias traumáticas.
- A curva de custo para o próximo nível é fechada, travando o Level Cap no nível 12.
- 1 Ponto de Eco permite ativar 1 nó do Labirinto de Carcosa.

## 2. A Árvore de Progressão (O Labirinto)
O Labirinto possui o formato do Símbolo Amarelo (3 braços ramificados a partir do centro):
- **Sobrevivente:** Foco em furtividade, mobilidade e escape.
- **Ocultista:** Foco em feitiçaria, resiliência mental e rituais do Necronomicon.
- **Protetor:** Foco em bloqueio, sinergia com o companheiro (Yug-Neth) e combate físico.

Existem 30 nós no total (10 por braço). Hierarquia de nós:
- **Menores:** +5% Vigor, +5% Resiliência.
- **Notáveis:** Alteram levemente a gameplay (ex: Dash que empurra inimigos).
- **Keystones:** (1 na ponta de cada braço). Mudam radicalmente o estilo de jogo (Ex: Sangue de Byakhee - roubo de vida através do Ocultismo).

## 3. Gastando Ecos (Santuários)
O jogador **não pode** gastar Ecos pelo menu a qualquer momento. Para respeitar o ritmo do Survival Horror, a progressão só acontece fisicamente dentro dos **Santuários de Carcosa** (checkpoints escassos nas fases). A decisão cria tensão, já que o jogador precisa sobreviver até o Santuário carregando seus Ecos sem morrer.

## 4. Engenharia (Arquitetura)
- **`ProgressionManager.cs`**: Calcula a XP, Ecos disponíveis e valida a compra de nós.
- **`EcoDef.cs`**: ScriptableObject que define o nó, os pré-requisitos e seu GUID único para serialização.
- **`GerenciadorEfeitosPassivos.cs`**: Aggregate Root que condensa os buffs de Ecos do Labirinto com os bônus de Relíquias da Tumba e Itens da Mochila. Repassa eventos `OnBonusChanged` para os consumidores sem acoplamento.
- **`ProgressionSaveData.cs`**: Entidade POCO injetada no Save System para persistir quais nós (GUIDs) foram desbloqueados e quantos Ecos não gastos o jogador tem.

---

## 5. Estado real vs. este documento (verificado em 2026-08-14)

> ⚠️ **Divergência doc↔código.** As seções 1–4 descrevem o sistema *como deveria ser*. O que
> existe hoje:
>
> | afirmação | realidade |
> |---|---|
> | "30 nós no total (10 por braço)" | **zero assets `EcoDef`** no projeto — a árvore não tem um único nó autorado |
> | Sistema funcionando | `ProgressionManager` **não está em cena nem prefab nenhum**; `Instance` é sempre `null` |
> | Buffs de Ecos chegam ao jogador | os 4 consumidores rodam permanentemente no *fallback* (nível 1, sem Ecos) |
>
> Ou seja: **o código está pronto, não está ligado, e não tem conteúdo.** Ligar o
> `ProgressionManager` é a Fase 3 da refatoração de managers (`log.md`, 27ª rodada). Autorar os
> 30 nós é trabalho de design ainda não começado.
>
> Consequência prática para qualquer proposta de expansão: **acrescentar recurso a uma árvore
> com zero nós é otimização prematura.** A ordem correta é ligar → autorar alguns nós → só então
> expandir a mecânica.

## 6. Proposta de adaptação (Vini, 2026-08-14) — FORA DO VERTICAL SLICE

Um documento de design propôs um sistema de progressão mais enxuto (3 atributos, recursos
Lucidez/Sinal, teia com nós ativos, Fragmentos de Identidade). Decisão do Vini: **adequar o que
transporta à realidade existente, sem mudar nada do que já está construído** — a narrativa
(Damião, Yug-Neth, Abdul, Byakhee, Rei em Amarelo) permanece intacta.

`CLAUDE.md` §1.1 mantém "níveis de personagem" e "árvore de talentos" **fora do Vertical Slice**.
Esta seção é registro de decisão para depois do edital, não plano de execução imediata.

### 6.1 O que já existe com outro nome (transporta de graça)

| documento de design | já existe como | onde |
|---|---|---|
| **Lucidez** (sanidade, zera = fim) | `ResilienciaMental` — Trauma, Colapso, Ancoragem | Core, testado |
| XP por exploração | **Exposição** (`exposicaoAtual`), curva fechada, cap 12 | `ProgressionManager` |
| Teia de talentos, nós passivos | `EcoDef` com `PreRequisitos` + `Modificadores` | `ProgressionManager` |
| +1 talento por nível | `pontosDeEcoDisponiveis++` no level up | idem |
| Gastar pontos só em ponto seguro | já é regra: **Santuários de Carcosa** (seção 3) | idem |
| 4 slots de ação | `BarraDeAcoes` (arma + Q/E/R) e `BarraDeArtefatos` (F1–F4) | UI |
| Fragmentos de Identidade | mecanicamente idêntico a item/Artefato com `Modificadores` | `ArtefatoDef` |
| 3 eixos de build (Corpo/Mente/Alma) | **`CaminhoEco`**: Sobrevivente / Ocultista / Protetor | `EcoDef` |

O último item é o mais importante: a estrutura de três eixos que o documento propõe **já está
desenhada**, com vocabulário diegético próprio. Sobrevivente ≈ Corpo, Ocultista ≈ Alma,
Protetor ≈ o eixo de defesa/companheiro.

### 6.2 Colisões resolvidas

1. **"Ecos de Memória" como moeda de XP — rejeitado.** `EcoDef` já *é* o "Eco da Memória", e é o
   **nó de talento**, não a moeda. A moeda já se chama **Exposição**. Usar a mesma palavra para
   as duas coisas quebraria código, assets e docs existentes. **Fica como está.**
2. **Corpo/Mente/Alma como atributos numéricos novos — rejeitado como campo.** A ficha já tem 5
   atributos (`FichaDeAtributos`) e o `StatType` já tem 15 entradas, das quais 4 são decorativas.
   Acrescentar três campos numéricos aumentaria o problema que a auditoria do inventário
   documentou. Se forem adotados, devem ser **rótulos de UI agregando os stats existentes**, não
   dados novos.
3. **"Mente" não tem correspondente.** Corpo mapeia (Vitalidade/Defesa/Ataque) e Alma mapeia
   (Conjuração/Resistência Anômala), mas percepção/investigação **não são atributos do jogador**
   hoje — percepção é FSM de inimigo. Adotar "Mente" exigiria criar a estatística e os usos dela.

### 6.3 O que é genuinamente novo, e vale

**Sinal** — não existe nada parecido no projeto. A regra proposta (*cada ponto de Sinal reduz a
Lucidez máxima*) tem um encaixe notável: `StatType.RMMaxima` existe e está **morto como passiva**
(ver `inventario_analise.md`). O Sinal daria propósito a ele pelo canal negativo de
`FichaDeAtributos.ComBonus`, que já aceita bônus negativos com piso desde 2026-08-14.

É a peça de melhor relação custo/retorno de todo o documento — pequena, reaproveita infraestrutura
recém-construída e converte um atributo decorativo em mecânica.

**Nós ativos na teia** — `EcoDef` só carrega `Modificadores` (passivas). Desbloquear *habilidade
ativa* por nó exigiria campo novo no `EcoDef` e ligação com a `BarraDeAcoes`. É o maior bloco de
trabalho real da proposta, e depende de a árvore ter nós (ver seção 5).

### 6.4 Ordem sugerida, quando o VS liberar

1. **Ligar o `ProgressionManager`** (Fase 3 da refatoração de managers) — sem isso nada acima
   funciona, porque `Instance` é `null` e a progressão nunca é salva.
2. **Autorar um punhado de nós `EcoDef`** (não os 30 — o suficiente para validar o fluxo:
   ganhar Exposição → subir nível → gastar ponto no Santuário → sentir o efeito).
3. **Sinal**, com `RMMaxima` negativo.
4. **Nós ativos**, só depois de 1–3 estarem em pé.

Itens 1 e 2 são pré-requisitos duros. Começar por 3 ou 4 seria construir sobre sistema que não
roda.
