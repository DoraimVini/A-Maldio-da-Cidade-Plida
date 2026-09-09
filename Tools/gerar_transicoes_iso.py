# -*- coding: utf-8 -*-
"""
Gera o conjunto de 47 tiles de transicao entre dois terrenos, para grid ISOMETRICO.

    python Tools/gerar_transicoes_iso.py <base.png> <vizinho.png> <pasta_de_saida> [--banda N]

DE ONDE VEM O 47, e por que vale aqui
-------------------------------------
E o "blob tileset" de 8 vizinhos. Das 256 combinacoes possiveis, so 47 sao distintas, porque
um CANTO so muda o desenho quando as DUAS arestas vizinhas a ele estao presentes -- se falta
uma aresta, aquele canto ja esta coberto pela borda da aresta.

    arestas presentes | mascaras | configuracoes
            0         |     1    |      1
            1         |     4    |      4
            2         |     6    |     10
            3         |     4    |     16
            4         |     1    |     16
                                    ----
                                      47

A derivacao e TOPOLOGICA: depende so de haver 4 arestas e 4 cantos alternando num ciclo. O
losango isometrico tem exatamente isso, entao o 47 vale igual ao retangular. O que MUDA e
qual vizinho e qual.

A VIZINHANCA DO LOSANGO (medida, nao suposta)
---------------------------------------------
A conversao do projeto e `BaseIsometrica.ParaMundo`: x = dx - dy, y = (dx + dy) * 0,5. Dela
sai que os quatro vizinhos que ENCOSTAM DE ARESTA sao as diagonais da tela, e os que tocam so
num VERTICE sao as cardinais -- o inverso da intuicao retangular:

                       Cima (+1,+1)
              NO (0,+1)         NE (+1,0)
    Esquerda                              Direita
     (-1,+1)        [ este tile ]         (+1,-1)
              SO (-1,0)         SE (0,-1)
                       Baixo (-1,-1)

O QUE ESTE SCRIPT ENTREGA, E O QUE NAO
--------------------------------------
Entrega os 47 quadros com a borda do vizinho aplicada por mascara dithered, mais um manifesto
que diz qual configuracao cada arquivo atende -- de forma que autorar o Rule Tile vire trabalho
mecanico em vez de adivinhacao.

NAO entrega arte final. Medido em 2026-09-09 com sand_01 (brilho 154) e arena_piso_01 (92):
com banda de 4 px a transicao SOME -- o losango tem meia-altura de 8 e as pontas leste/oeste
tem 1 px, entao a borda vira franja no contorno. Com banda proporcional (85% da meia-altura,
6,8 px a 32x16) ela le, mas como CHECKERBOARD Bayer, que a essa escala parece mistura 50/50 e
nao borda de terreno. Serve de placeholder; transicao desenhada a mao ganha sempre.
Isto existe para DESTRAVAR level design -- pintar um limite de bioma e ver se ele funciona --
nao para ficar.

E, no dia em que este script for usado: **hoje nao existe uma unica celula neste projeto onde
dois terrenos se encontram**. Cada cena usa uma familia de chao so. Auditado em 2026-09-09.
"""
import argparse
import io
import os
import sys
from itertools import product

try:
    from PIL import Image
except ImportError:
    sys.exit("PIL nao encontrado. Instale com: pip install pillow")


# ── a vizinhanca, na ordem do ciclo ────────────────────────────────────────
ARESTAS = [
    ("NE", (1, 0), "nordeste  -- encosta de aresta"),
    ("NO", (0, 1), "noroeste  -- encosta de aresta"),
    ("SO", (-1, 0), "sudoeste  -- encosta de aresta"),
    ("SE", (0, -1), "sudeste   -- encosta de aresta"),
]

# Cada canto fica ENTRE duas arestas do ciclo, e so importa quando as duas existem.
CANTOS = [
    ("Cima", ("NE", "NO"), (1, 1)),
    ("Esquerda", ("NO", "SO"), (-1, 1)),
    ("Baixo", ("SO", "SE"), (-1, -1)),
    ("Direita", ("SE", "NE"), (1, -1)),
]   # o terceiro campo e o offset de celula do canto, mantido para leitura do mapa acima

# Bayer 4x4: o dither de pixel art. Uma borda com transicao suave vira cinza; com dither,
# continua sendo dois tons -- que e o que mantem a peca legivel a 32 x 16.
BAYER = [
    [0, 8, 2, 10],
    [12, 4, 14, 6],
    [3, 11, 1, 9],
    [15, 7, 13, 5],
]


def configuracoes():
    """As 47, em ordem estavel. Cada uma: (arestas presentes, cantos presentes)."""
    saida = []
    for mask in product([1, 0], repeat=4):
        arestas = {nome: v for (nome, _, _), v in zip(ARESTAS, mask)}

        livres = [c for c in CANTOS if arestas[c[1][0]] and arestas[c[1][1]]]

        for combo in product([1, 0], repeat=len(livres)):
            cantos = {c[0]: v for c, v in zip(livres, combo)}
            saida.append((arestas, cantos))

    return saida


def nome_do_arquivo(arestas, cantos):
    """Nome que carrega a configuracao inteira -- autorar a regra vira leitura, nao busca."""
    a = "".join(n if arestas[n] else "-" for n, _, _ in [(x[0], x[1], x[2]) for x in ARESTAS])
    c = "".join((n[0] if cantos.get(n, 1) else "-") for n, _, _ in CANTOS)
    return f"a{a or '----'}_c{c or '----'}"


def quadrante(dx, dy):
    """Em que aresta do losango este pixel cai. dy positivo = para CIMA na tela."""
    if dx >= 0 and dy >= 0:
        return "NE"
    if dx < 0 and dy >= 0:
        return "NO"
    if dx < 0 and dy < 0:
        return "SO"
    return "SE"


def gerar(base, vizinho, arestas, cantos, banda):
    """Um quadro: `base` no miolo, `vizinho` nas bordas viradas para quem nao e igual."""
    L, A = base.size
    hw, hh = L / 2.0, A / 2.0

    saida = Image.new("RGBA", (L, A), (0, 0, 0, 0))
    pb, pv, ps = base.load(), vizinho.load(), saida.load()

    for y in range(A):
        for x in range(L):
            dx = (x + 0.5) - hw
            dy = hh - (y + 0.5)          # positivo = para cima

            # Dentro do losango? |dx|/hw + |dy|/hh <= 1
            t = abs(dx) / hw + abs(dy) / hh
            if t > 1.0:
                continue

            profundidade = (1.0 - t) * hh      # pixels do pixel ate a borda do losango

            usar_vizinho = False

            # Borda contra a aresta em que este pixel cai, se aquele vizinho for outro.
            q = quadrante(dx, dy)
            if not arestas[q] and profundidade < banda:
                usar_vizinho = True

            # Entalhe no vertice, quando o canto e outro mas as duas arestas sao iguais.
            for nome, (a1, a2), (cx, cy) in CANTOS:
                if cantos.get(nome, 1):
                    continue
                # vertice: Cima=(0,+hh) Baixo=(0,-hh) Direita=(+hw,0) Esquerda=(-hw,0)
                alvo = {
                    "Cima": (0.0, hh),
                    "Baixo": (0.0, -hh),
                    "Direita": (hw, 0.0),
                    "Esquerda": (-hw, 0.0),
                }[nome]
                d = ((dx - alvo[0]) / hw) ** 2 + ((dy - alvo[1]) / hh) ** 2
                if d < (banda / hh) ** 2:
                    usar_vizinho = True

            if usar_vizinho:
                # Dither na fronteira: quanto mais fundo, menor a chance de virar vizinho.
                limiar = BAYER[y % 4][x % 4] / 16.0
                if profundidade / max(banda, 1e-6) > limiar:
                    usar_vizinho = False

            ps[x, y] = pv[x, y] if usar_vizinho else pb[x, y]

    return saida


def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("base", help="PNG do terreno que ESTE tile e")
    ap.add_argument("vizinho", help="PNG do terreno vizinho, que aparece nas bordas")
    ap.add_argument("saida", help="pasta de saida")
    ap.add_argument("--banda", type=float, default=None,
                    help="espessura da borda em pixels. O padrao e 85%% da meia-altura do "
                         "tile, que para 32x16 da 6,8 px -- medido: banda fixa de 4 px some "
                         "num losango de meia-altura 8, porque as pontas leste e oeste tem "
                         "1 px de altura e a borda vira franja.")
    args = ap.parse_args()

    base = Image.open(args.base).convert("RGBA")
    vizinho = Image.open(args.vizinho).convert("RGBA")

    if base.size != vizinho.size:
        sys.exit(f"As duas texturas tem tamanhos diferentes: {base.size} e {vizinho.size}. "
                 "Transicao so faz sentido entre tiles da mesma celula.")

    # Proporcional ao tile, e nao um numero fixo: o que cabe num losango de 32 x 16 nao e o
    # mesmo que cabe num de 64 x 32, e um padrao que produz saida invisivel e um padrao ruim.
    banda = args.banda if args.banda is not None else (base.size[1] / 2.0) * 0.85

    os.makedirs(args.saida, exist_ok=True)

    nb = os.path.splitext(os.path.basename(args.base))[0]
    nv = os.path.splitext(os.path.basename(args.vizinho))[0]

    linhas = []
    for arestas, cantos in configuracoes():
        nome = f"{nb}_para_{nv}_{nome_do_arquivo(arestas, cantos)}.png"
        gerar(base, vizinho, arestas, cantos, banda).save(os.path.join(args.saida, nome))

        iguais = [n for n, _, _ in ARESTAS if arestas[n]] + \
                 [n for n, _, _ in CANTOS if cantos.get(n, 0)]
        linhas.append((nome, ", ".join(iguais) or "(nenhum vizinho igual)"))

    with io.open(os.path.join(args.saida, "MANIFESTO.md"), "w",
                 encoding="utf-8", newline="\n") as f:
        f.write(f"# Transicoes {nb} -> {nv}\n\n")
        f.write(f"{len(linhas)} quadros, banda de {banda:g} px. Gerado por "
                "`Tools/gerar_transicoes_iso.py`.\n\n")
        f.write("No Rule Tile, cada linha vira uma regra: os vizinhos listados sao **This**, "
                "e os que faltam sao **NotThis**.\n\n")
        f.write("| arquivo | vizinhos que sao IGUAIS a este tile |\n|---|---|\n")
        for nome, iguais in linhas:
            f.write(f"| `{nome}` | {iguais} |\n")

    print(f"  {len(linhas)} quadros em {args.saida} (banda {banda:g} px)")
    print(f"  manifesto: {os.path.join(args.saida, 'MANIFESTO.md')}")


if __name__ == "__main__":
    main()
