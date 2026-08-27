---
name: favela-qa-pipeline
description: Mandatory QA pipeline for compiling and testing changes in the Favela Amarela project. Triggered after any code alteration.
---

# Favela QA Pipeline (Mandatory)

Whenever a C# file or asset is altered, you MUST execute the following pipeline strictly in order. Do NOT advance to any other task or module until this cycle closes with success.

## Primary path: MCP `mcp-unity` (Editor aberto)

O fluxo real deste projeto assume o Unity Editor **aberto** durante o desenvolvimento — é o que o MCP `mcp-unity` exige para funcionar. Use-o como caminho padrão:

1. **SAVE**: Ensure the altered file is saved (handled inherently when writing tools succeed).
2. **COMPILE**: Chame `mcp__mcp-unity__recompile_scripts`. Erro de compilação retornado = FAILURE, vá para o passo 5.
3. **TEST**: Chame `mcp__mcp-unity__run_tests` (testMode `EditMode`). Prefira rodar a suíte inteira (sem `testFilter`) em vez de filtrar por classe — um filtro que não casa o nome completo (namespace + classe) às vezes devolve `0/0` mesmo com o teste existindo.
4. **SUCCESS -> REPORT**: Se os testes passarem (0 falhas), reporte o resultado ao usuário e aguarde pedido explícito para commitar. NUNCA rode `git add`/`git commit` automaticamente nesta etapa.
5. **FAILURE -> FIX**: Se houver falha de compilação ou de testes real (não flakiness — ver abaixo), corrija imediatamente o erro na respectiva classe e volte ao Passo 1.

### Flakiness conhecida do MCP `mcp-unity` — não é falha real

`recompile_scripts` e `run_tests` falham de forma intermitente e **não relacionada ao seu código**:
- Timeout de 120s sem resposta.
- `Connection failed: Unknown error`.
- Resultado ambíguo `0/0 passed - 0/0 failed` com `testCount` maior que zero (comum na 1ª chamada logo após uma recompilação).

Nesses três casos, **repita a mesma chamada uma ou duas vezes** antes de tratar como falha real. Se persistir após ~3 tentativas, informe ao usuário que o bridge MCP está instável (não que o teste falhou) e ofereça o caminho alternativo abaixo.

## Caminho alternativo: script batch (só com o Editor fechado)

`Tools\run_qa_tests.ps1` roda os testes em batch mode e é mais determinístico (sem flakiness de MCP), mas **exige o Editor fechado**.

**Reescrito em 2026-08-27.** Ele classifica o resultado em EXATAMENTE UM de quatro estados e diz qual:

| estado | o que significa |
|---|---|
| `EDITOR ABERTO` | detectado ANTES de rodar — nada foi executado, nada mudou |
| `ERRO DE COMPILAÇÃO` | vem com arquivo, linha e mensagem, já sem repetição |
| `TESTES FALHARAM` | vem com os nomes e as três primeiras linhas de cada mensagem |
| `VERDE` | com as contagens |

Antes disso os três primeiros caíam todos em "arquivo de resultados não encontrado", e a mensagem chutava entre duas causas. Pior: o log da Unity sai em **UTF-16** quando redirecionado pelo PowerShell, então um `grep "error CS"` **não achava nada e o silêncio parecia sucesso** — foi assim que uma compilação quebrada passou por compilada em 2026-08-27.

O ruído de boot (~20 mil linhas) vai para `TestResults/unity_EditMode.log`; só o diagnóstico é impresso.

### Ferramenta de Editor: `Tools\run_editor_tool.ps1`

Para rodar `-executeMethod`, **use este wrapper, não o Unity direto**:

```
.\Tools\run_editor_tool.ps1 -Metodo FavelaAmarela.EditorTools.MontarBasesDeArma.Executar -Marcador "[BasesDeArma]"
```

O Corolário 4 do COMMANDMENT diz que exit code não é evidência — e `-executeMethod` devolve exatamente isso. Método com nome errado, método que retornou cedo por uma guarda, e método que funcionou saem **todos com código 0**. O wrapper exige o `-Marcador` (o prefixo dos `Debug.Log` da ferramenta) e **reprova se o log não tiver nenhuma linha com ele** — porque aí a ferramenta não deixou rastro de ter feito nada. Verificado: rodar com marcador errado devolve `TOOL FAILED` mesmo com a Unity saindo em 0.

Isso não substitui conferir o disco. Substitui o passo anterior: saber se vale a pena olhar.

Use-o só quando:
- O usuário pedir explicitamente uma rodada de QA "limpa"/definitiva (ex.: antes de um commit importante), ou
- O MCP estiver genuinamente indisponível após as tentativas acima.

Nesses casos, peça ao usuário para fechar o Editor antes de rodar o script — não assuma que ele está fechado.
