# Regras da camada Core (POCOs puros)

Aplica-se a `Core/Enemies`, `Core/Combat`, `Core/Stealth`, `Core/Environment`, `Core/GameLoop`, `Core/Abilities`.

- Zero dependência de `UnityEngine` além de `Vector2`, `Vector3` e `Mathf` quando estritamente necessário para cálculo. Nada de `MonoBehaviour`, `ScriptableObject`, `GameObject`, `Transform`.
- Classe `sealed` por padrão, a menos que exista uma razão concreta para permitir herança.
- Estado exposto via propriedades somente-leitura; mutação só através de métodos explícitos (ver `ResilienciaMental.SofrerTrauma`/`Ancorar`).
- Eventos via `event Action` / `event Action<T>`. Para hot paths, use `readonly struct` nos argumentos do evento (padrão de `SomEmitido`, `ResilienciaChangedArgs`) para evitar alocação.
- FSMs seguem o padrão `enum` + propriedade `CurrentState` + método privado de transição + evento `OnStateChanged` (ver `CultistaFSM`/`CultistaState`). Não instancie objetos de estado por frame.
- Toda classe nova aqui precisa ser testável sem a Unity rodando e ganhar um teste NUnit EditMode correspondente em `Assets/Tests/EditMode`, instanciando o POCO diretamente (`new MinhaClasse()`), sem cena nem `MonoBehaviour`.
- Documentação XML (`/// <summary>`) obrigatória em todo membro público, em português, seguindo o estilo já usado (explicar o "porquê" e a correspondência com o vocabulário diegético quando fizer sentido).
- Nomes de conceitos de domínio novos (habilidades, estados, recursos) devem seguir a skill `favela-lore-enforcer` — não usar termos genéricos como Health, Damage, Mana, Enemy.
- Aqui não há API de Unity a consultar (é C# puro). Na dúvida sobre um recurso de linguagem, use a documentação oficial da Microsoft (learn.microsoft.com/dotnet/csharp), nunca suposição. Ver seção 3 do `CLAUDE.md` raiz.
