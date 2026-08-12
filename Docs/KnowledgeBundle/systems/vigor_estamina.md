# Sistema de Vigor (Estamina)

O Vigor em *Favela Amarela* é um recurso tático de curto prazo usado para forçar a tomada de decisão no combate e na fuga (Survival Horror). Não deve punir a exploração leve.

## 1. Regras de Design
- **Capacidade e Regeneração:** O Vigor base é 100. Regenera rapidamente, exceto quando o jogador chega ao estado de **Exaustão**.
- **Consumo de Corrida (Sprint):** Custa Vigor ao longo do tempo. Se exaurido, Damião reverte para caminhada (Walk).
- **Consumo de Esquiva (Dash):** Custa Vigor em taxa fixa (ex: 25). Se não houver vigor suficiente, a esquiva falha e as i-frames não disparam.
- **Limiar de Exaustão:** Se o vigor chegar a 0, Damião entra em Exaustão. Nesse estado, ele não pode correr nem esquivar até que a barra recupere até o Limiar (30). A taxa de regeneração durante a exaustão é mais lenta.

## 2. Arquitetura
- **`GerenciadorDeVigor.cs`:** É a Fonte Única da Verdade. Atuando como um MonoBehaviour no Player, concentra todo o estado atual e as regras de regeneração e exaustão. Dispara os eventos `OnVigorChanged` e `OnExaustaoChanged`.
- Integra perfeitamente com o `GerenciadorEfeitosPassivos`, buscando ativamente buffs de `VigorMaximo`, `RegeneracaoVigor`, `CustoEsquivaVigor` e `CustoCorridaVigor`.
- **`PlayerMovement.cs` e `EsquivaBridge.cs`:** Consumidores do Vigor. Eles verificam com o Gerenciador se a ação está autorizada, e aplicam o bloqueio em caso negativo.
- **`VigorBar.cs`:** (UI) Assina os eventos do Gerenciador sem polling e interpola suavemente a barra de estamina verde, que fica cinza ao entrar em exaustão. É acoplada através do `HUDController`.
- **Save/Load:** O GameManager restaura a estamina na ida ao Menu ou transições, e a `ProgressionSaveData` foi estendida para garantir fluidez.
