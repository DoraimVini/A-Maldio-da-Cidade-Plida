# Regras de Resolução de Problemas e Uso de MCP/Skills

Cada tipo de problema deve ser atacado com o MCP ou Skill correto, evitando que o Antigravity tente resolver tudo sozinho de forma genérica. Use a tabela abaixo como diretriz mandatória de ação:

| Tipo de Erro | MCP / Skill a Usar | Exemplo de Ação Esperada |
| :--- | :--- | :--- |
| **Erro de compilação C#** | `claude-code` + `unity-qa` | Ler o arquivo com erro, encontrar a referência quebrada e corrigir estritamente a linha defeituosa. Em seguida, executar os testes (ex: `PlayerStealthStateTests`). |
| **Arte borrada ou filtrada** | `aseprite-bridge` + `favela-pixelart-standards` | Verificar a paleta e reimportar o asset com PPU=16 e Filter Mode=Point. |
| **Física quebrada (personagem voando)** | `favela-isometric-standards` | O Rigidbody2D do Player está com gravityScale diferente de 0. Corrigir as configurações para conformidade com a gravidade zero. |
| **Termo genérico no código (ex: health)** | `favela-lore-enforcer` | Substituir ocorrências de palavras proibidas (health, damage, etc.) pelos termos diegéticos corretos (Lucidez, Anomalia, etc.). |
| **Testes falhando** | `favela-qa-pipeline` | Corrigir a classe (POCO) responsável pela lógica testada, rodar os testes e reportar o resultado ao usuário. Commit só acontece mediante pedido explícito do usuário — NUNCA rode `git add`/`git commit` automaticamente. NUNCA prosseguir sem testes passando. |
| **GDD desatualizado** | `notion-mcp-server` | Atualizar a página do GDD no Notion refletindo as mecânicas implementadas (ex: Resiliência Mental). |
