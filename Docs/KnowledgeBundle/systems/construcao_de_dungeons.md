# Padrões de Construção de Cenas e Dungeons

Favela Amarela possui diretrizes rigorosas para a arquitetura de cena que previnem Z-Fighting, bugs de perspectiva e peso de performance.

## 1. Hierarquia Obrigatória
Todas as cenas aditivas (Dungeons) usam a mesma estrutura fixa:
```text
[NomeDaDungeon] (Scene root)
├── Grid (Grid: Isometric Z as Y)
│   ├── Ground_Base (Tilemap, Sorting Layer "Ground", Order 0)
│   ├── Ground_Decals (Tilemap, Sorting Layer "Ground", Order 1)
│   ├── Walls (GameObject vazio, pai de todos os prefabs de parede)
│   └── Ceilings (GameObject vazio, pai dos tetos e coberturas)
├── Entities
│   ├── Enemies
│   └── NPCs
├── Triggers
│   ├── Trigger_Transicao_Entrada
│   ├── Trigger_Transicao_Saida
│   └── Trigger_Chefe
└── SceneSetup (Componente de inicialização e script)
```
- Existe uma ferramenta na Engine (`Favela Amarela > Dungeons > Gerar Templo Povo Serpente`) que gera e valida esse esqueleto inteiro com apenas 1 clique.

## 2. Otimização e Colisão
As paredes **não usam Tilemaps**. Elas são instanciadas como prefabs independentes (ex: `Wall_Stone_Straight`) anexadas sob o parent `Walls`.
- Para evitar peso de física absurdo em milhares de blocos, o GameObject `Walls` possui um `CompositeCollider2D` e um `Rigidbody2D (Static)`. 
- Os `BoxCollider2D` dos filhos estão marcados como `Used By Composite`, unificando a malha da dungeon num polígono único.

## 3. Renderização e Sorting (O Coração Isométrico)
A prioridade final de renderização se apoia unicamente em:
1. **Sorting Layers**:
   - `Background` -> `Ground` -> `Shadows` -> `Gameplay` -> `Foreground` -> `UI`
   - O segredo da ilusão: **Paredes, Monstros e o Jogador VIVEM TODOS na mesma Sorting Layer (`Gameplay`).** Se estiverem separados, o jogador nunca passará atrás de uma parede.
2. **Y-Sorting**:
   - A fórmula `-Y * 10` (no PPU 32) dá a ordem de impressão dentro da layer.
   - Objetos estáticos calculam no `Start`.
   - Elementos móveis usam o script `DynamicYSort` (namespace `FavelaAmarela.Runtime.Rendering`) para atualizar o Order a cada LateUpdate. O SceneSetup faz o safety check defensivo disto!
3. **Oclusão**:
   - Objetos altos recebem `OcclusaoDitherFade` e shader especial. Em vez de transparência Alpha, eles mostram dithering (pixels intercalados) revelando Damião por trás, mantendo a estética retro fiel.
