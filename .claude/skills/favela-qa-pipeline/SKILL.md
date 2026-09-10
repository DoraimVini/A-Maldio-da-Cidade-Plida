---
name: favela-qa-pipeline
description: Mandatory QA pipeline for compiling and testing changes in the Favela Amarela project. Triggered after any code alteration.
---

# Favela QA Pipeline (Mandatory)

Whenever a C# file or asset is altered, you MUST execute the following pipeline strictly in order. Do NOT advance to any other task or module until this cycle closes with success.

## Caminho primário: Unity CLI + Pipeline (Editor aberto) — desde 2026-09-10

O fluxo real deste projeto tem o Unity Editor **aberto** durante o desenvolvimento. Desde
2026-09-10 o projeto carrega o pacote `com.unity.pipeline` (Unity 6+) e a máquina tem o
**Unity CLI** (`unity`, canal beta): o Editor aberto é **dirigido pelo terminal**, e a suíte
roda **sem fechar a Unity**. Medido no dia da instalação: 1155 testes EditMode em 113 s pelo
Editor vivo, resultado idêntico ao batch.

1. **SAVE**: garanta o arquivo salvo (inerente às ferramentas de escrita).
2. **STATUS**: `unity status --format json --no-banner` — o Editor tem de aparecer com
   `state: "ready"`. Se não aparecer, veja "Sem Editor" abaixo.
3. **COMPILE**: `unity command recompile` e depois `unity command recompile_status` até
   `status: completed` — atenção: `data.result` desse comando é uma **string JSON**
   (`"{\"status\":\"completed\",\"failed\":false,\"errors\":[]}"`), não um objeto; faça
   `json.loads` duas vezes. `failed: true` ou `errors` não vazio = FAILURE, passo 6.
4. **TEST**:
   ```
   unity command run_tests --mode editor --timeout 500 --format json --no-banner
   ```
   Leia `data.result.Summary` (`Total/Passed/Failed/Skipped`) e `data.result.Results[]`.
   Prefira a suíte inteira; `--filter <Classe>` casa substring do nome, serve para iterar.
   PlayMode: `--mode playmode`. **Cuidado:** os testes de cena fazem `OpenScene` e **trocam a
   cena aberta do Vini** — confira `unity command list_open_scenes` (`isDirty`) antes de rodar
   a suíte inteira, e avise se houver cena suja.
5. **SUCCESS -> REPORT**: 0 falhas → reporte as contagens. Commit só com aprovação (o Vini deu
   aprovação permanente para commit ao fim de cada rodada verde; `git push` não).
6. **FAILURE -> FIX**: corrija e volte ao passo 1.

### Ferramentas de cena pelo Editor vivo (em vez de patchar YAML)

Toda ferramenta `[MenuItem("Tools/FavelaAmarela/...")]` roda no Editor aberto:
```
unity command menu --path "Tools/FavelaAmarela/UI: achar rótulos que não cabem" --format json --no-banner --timeout 120
unity command console --format json --no-banner        # ler o que ela logou (marcador!)
```
E a **verdade da cena** vem do Editor, não do meu parse do YAML: `get_scene_hierarchy`,
`find_gameobjects`, `get_component_properties`, `set_transform`, `set_serialized_field`,
`save_scene`. Duas armadilhas que isto fecha por construção: posição **local ≠ mundo** (o
portão dos Portões ficou 5,5 un fora da tela por um dia por causa disso) e **prefab ≠
instância**. `unity command` (sem argumentos) lista os 149 comandos; `--query <termo>` filtra.

Regra da própria skill do CLI, que vale aqui: **nunca editar `.unity`/`.prefab`/`.asset` à mão
enquanto houver Editor alcançável** — e, quando não houver, dizer explicitamente que se está
editando o arquivo direto.

### Sem Editor (`STATUS_NO_INSTANCES`) — duas causas, duas saídas

- **Safe Mode**: erro de compilação faz o Editor abrir sem o Pipeline. `unity pipeline list`
  mostra `safeMode`. Corrija o C# e peça para reabrir a Unity — não caia para o batch.
- **Unity fechada**: use o caminho batch abaixo. É o único caso em que ele é o caminho.

### Render de verdade (URP)

Para validar **o que o jogador vê**, não use a captura de EditMode (`Camera.Render()` sem
quadros reais mentiu em 2026-09-10: material *lit* sem luz e atlas não reempacotado). Com o
Editor aberto: `unity command editor_play` → `unity command capture_game_view` →
`unity command editor_stop`. Sem Editor: PlayMode com GPU,
`.\Tools\run_qa_tests.ps1 -TestPlatform PlayMode -ComGraficos` (o `AArenaDosPortoesNaTelaTests`
é o modelo).

## Caminho alternativo: script batch (só com o Editor FECHADO)

> Antes de 2026-09-10 o caminho primário era o bridge de terceiros `mcp-unity`
> (`com.gamelovers.mcp-unity`), que raramente estava conectado e tinha flakiness
> documentada (timeouts, `0/0 passed`). O Unity CLI oficial o substituiu.

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

Use-o só quando a Unity estiver **fechada** (`unity status` → `STATUS_NO_INSTANCES` e
`unity pipeline list` → `isRunning: false`). Com o Editor aberto ele falha em `EDITOR ABERTO`
antes de rodar — e a resposta certa é o caminho primário acima, não pedir para fechar.

**Ferramenta que renderiza** (`CapturaDeCena`) precisa da Unity **sem `-nographics`**: com a
flag, `TilemapRendererGeometryJob` dá access violation ao desenhar o primeiro Tilemap.
