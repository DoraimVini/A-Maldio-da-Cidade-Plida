# Procedência — Fragmentos de Yhtill

## `Fragmento_Yhtill_0` · `_1` · `_2`

**Desenhados por script em 2026-09-09.** Não vieram de pack nenhum: são pixel art gerada
proceduralmente, 24 × 24, PPU 32, Point, sem compressão, Full Rect.

### Por que existiram

Os três eram o item mais grave da lista congelada em `ArteNasCenasTests`: **objetos que a
Canção Incompleta manda o jogador ACHAR**, desenhados com o quadrado branco embutido da Unity,
tingidos de creme. Um item de missão que precisa ser notado à distância, sem forma própria.

### A ideia

**Páginas rasgadas de um mesmo caderno** — a canção de Cassilda, partida em três. Por isso os
rasgos são complementares:

| | quebra |
|---|---|
| `_0` | à **direita** |
| `_1` | dos **dois** lados |
| `_2` | à **esquerda** |

Encostadas, leem como uma folha só. É a leitura que a quest quer: juntar as três é remontar a
estrofe.

> **Foram cacos de pedra antes (2026-09-09).** O Vini perguntou *"não tem nenhum livro?"* e a
> pergunta corrigiu o desenho: **fragmento de uma canção é página, não pedra.** Uma canção é
> escrita, e escrita mora em papel.

### A paleta

**Extraída do `Necronomicon.png`**, que já existe no projeto como objeto de mundo — página
`(206,191,215)`, tinta `(46,34,47)`, ouro `(184,143,72)`, borda `(58,40,28)`. Não é paleta
escolhida a olho: é a do livro que o jogo já mostra, para as páginas pertencerem à mesma família
visual.

Cinco linhas de tinta e uma **inicial iluminada em ouro** na primeira, desenhadas **antes** do
contorno para o contorno vencer nas bordas.

O creme `(0.95, 0.93, 0.82)` que a cena autorava como *tinta* virou a cor da *arte*, e o
`m_Color` do renderer voltou ao branco — cor de item mora no item.

### Conferido

Renderizados sobre o chão real do Deserto (`sand_03`) antes de entrar, soltos e encostados.
