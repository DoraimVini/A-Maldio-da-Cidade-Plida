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
