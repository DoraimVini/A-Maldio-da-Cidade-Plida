# Regras de Resolução de Problemas e Uso de MCP/Skills

Cada tipo de problema deve ser atacado com o MCP ou Skill correto, evitando que o Antigravity tente resolver tudo sozinho de forma genérica. Use a tabela abaixo como diretriz mandatória de ação:

| Tipo de Erro | MCP / Skill a Usar | Exemplo de Ação Esperada |
| :--- | :--- | :--- |
| **Erro de compilação C#** | `claude-code` + `unity-qa` | Ler o arquivo com erro, encontrar a referência quebrada e corrigir estritamente a linha defeituosa. Em seguida, executar os testes (ex: `PlayerStealthStateTests`). |
| **Arte borrada ou filtrada** | `aseprite-bridge` + `favela-pixelart-standards` | Verificar a paleta e reimportar o asset com PPU=32 e Filter Mode=Point. |
| **Física quebrada (personagem voando)** | `favela-isometric-standards` | O Rigidbody2D do Player está com gravityScale diferente de 0. Corrigir as configurações para conformidade com a gravidade zero. |
| **Termo genérico no código (ex: health)** | `favela-lore-enforcer` | Substituir ocorrências de palavras proibidas (health, damage, etc.) pelos termos diegéticos corretos (Lucidez, Anomalia, etc.). |
| **Testes falhando** | `favela-qa-pipeline` | Corrigir a classe (POCO) responsável pela lógica testada, rodar os testes e reportar o resultado ao usuário. Commit só acontece mediante pedido explícito do usuário — NUNCA rode `git add`/`git commit` automaticamente. NUNCA prosseguir sem testes passando. |
| **GDD desatualizado** | `notion-mcp-server` | Atualizar a página do GDD no Notion refletindo as mecânicas implementadas (ex: Resiliência Mental). |

# Template Padrão de Scripts (ScriptTemplate)

Sempre que for solicitado a criar um novo script C#, **DEVO** usar estritamente o seguinte formato de template:

```csharp
using UnityEngine;

namespace FavelaAmarela.[Namespace]
{
    public class [ClassName] : [BaseClass]
    {
        [Fields]

        void Awake() { }

        [Methods]
    }
}
```

# Aliases de Comandos (Pseudo-comandos)

Quando o usuário utilizar os seguintes "comandos", devo interpretar como uma macro e executar a ação correspondente imediatamente:

- `unity.import_sprite(path, ppu, filter)` → Importar o PNG como sprite com as configurações dadas (criando e executando um script de Editor temporário para alterar com segurança).
- `unity.create_script(name, folder, baseClass)` → Criar o script C# na pasta especificada utilizando a estrutura do `ScriptTemplate`.
- `unity.build_dev(platform, outputPath)` → Executar o build de desenvolvimento via Unity Command Line (`run_command`) ou gerar o script de build.
- `unity.find_components(componentType, searchFolder)` → Fazer uma busca nos arquivos `.prefab` e `.unity` (cenas) dentro da pasta especificada usando `grep_search` para listar todos os GameObjects que possuem o componente.
