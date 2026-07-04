# Regras da camada UI

Além das regras gerais de adaptador em `Assets/Scripts/CLAUDE.md`:

- UI só lê estado de POCOs e reage a eventos C# (ex.: `ResilienciaMental.OnChanged`, ver `ResilienciaBar.cs`). Nada de polling em `Update`, salvo animação visual (ex.: Lerp de barra de progresso).
- Proibido `GameObject.Find` / `FindObjectOfType` para localizar elementos de UI — resolva por referência serializada.
- Campos serializados relevantes devem ter `[Tooltip("...")]`.
- Qualquer texto visível ao jogador (label, tooltip, nome de habilidade, mensagem de HUD) deve passar pela skill `favela-lore-enforcer` antes de ser escrito.
