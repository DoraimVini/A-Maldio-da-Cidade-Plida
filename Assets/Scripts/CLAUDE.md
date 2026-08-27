# Regras da camada Runtime (adaptadores MonoBehaviour)

Aplica-se a `Player/`, `Enemies/`, `GameLoop/`, `Camera/` e demais pastas com `MonoBehaviour` (fora de `Core/`).

- Um `MonoBehaviour` aqui só orquestra: instancia/injeta POCOs de `Core` (via métodos `.Bind()`, ver `GameManager.cs`), lê input, e sincroniza o resultado com o mundo visual. Regra de negócio (dano, transição de estado, detecção sonora) mora em `Core`, nunca aqui.
- Movimentação: `Rigidbody2D` com `gravityScale = 0` e `CollisionDetectionMode2D.Continuous`; movimento aplicado via `rb.linearVelocity` em `FixedUpdate` (convenção real do projeto, ver `PlayerMovement.cs`) — não usar `MovePosition` nem manipular `transform.position` diretamente.
- Valide toda referência vinda do Inspector em `Awake()`: se estiver nula, `Debug.LogError` e siga com um fallback seguro (não deixe estourar `NullReferenceException`).
- Proibido `GameObject.Find` / `FindObjectOfType` em código de produção.
- Prefira composição: vários componentes pequenos com responsabilidade única em vez de um script grande fazendo tudo.
- Câmera (`IsometricCameraController`) fica sempre com rotação `Quaternion.identity` — nunca tilte fisicamente a câmera (a "sensação" isométrica vem do Y-sorting e de `BaseIsometrica.ParaMundo`, não de rotação de câmera). Ao mexer em física, câmera, prefab de sala ou Rigidbody de inimigo/player, confira a skill `favela-isometric-standards`.
- Documentação XML em português nos membros públicos, mesmo padrão da camada Core.
- Antes de usar uma API da Unity fora do que já é convenção conhecida no projeto, confira a Script Reference da versão exata (6000.4): https://docs.unity3d.com/6000.4/Documentation/ScriptReference/index.html — não assuma nomes/assinaturas de versões antigas da Unity (ex.: `Rigidbody2D.velocity` foi renomeado para `linearVelocity` na Unity 6).
