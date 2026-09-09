# Procedência — tiles de chão do Deserto

## `sand_01` · `sand_02` · `sand_03` · `sand_crack` · `sand_pebbles`

**Origem:** pack de licença aberta baixado, autorizado pelo Vini para uso no projeto.

**Modificados em 2026-09-09**, e o que segue é o registro do que foi feito e por quê.

### O problema

O Vini relatou querer *"o mapa inteiriço, sem o espaço entre as tiles"*. Medido, **não havia
espaço nenhum**: empilhados na malha isométrica da Unity, os losangos cobrem o plano com
**0,0% de buraco e 0,0% de sobreposição**. O import também já estava correto — Full Rect,
Point, sem compressão, PPU 32, 32×16 px = exatamente o `cellSize` de 1 × 0,5.

O que se via era **desenho**, não vão:

| causa | medida |
|---|---|
| Contorno de 1 px | **−86** de luminância, 60 dos 256 px opacos (23% do tile) |
| Gradiente interno | **−93** do topo à base, idêntico nos cinco |

E havia uma armadilha: **o contorno não era o defeito, era o disfarce.** Com ele, todo pixel de
borda encontrava outro pixel escuro igual e a costura media **0,0**. Removê-lo sozinho expunha
uma descontinuidade de **63,9** contra uma variação interna de 4,8 — treze vezes pior.

### O que foi feito

O losango de 256 px é **domínio fundamental exato** da malha gerada por (16, 8) e (−16, 8) —
verificado: toda vizinhança de todo pixel tem representante. Então o tile já é, topologicamente,
uma textura em toro; faltava ser **contínua** ao dar a volta.

1. **Contorno removido** e **gradiente vertical achatado**, com o tom-base dos cinco igualado.
2. **Solução de Poisson no toro da malha**: os gradientes originais do miolo preservados, com o
   salto da costura redistribuído pelo tile inteiro.
3. **Borda comum às cinco variantes.** Os 60 px de borda caem em **14 órbitas do wrap**, e cada
   órbita recebeu uma cor só — assim qualquer variante encosta em qualquer outra.

### O resultado, medido nos arquivos deste diretório

```
costura ......................... 0,00 nos cinco tiles
desacordo entre variantes ....... 0,0000 na borda
variação interna preservada ..... 2,4 a 11,0
```

### As invariantes que isto NÃO pode quebrar

- **Máscara alfa idêntica** nos cinco, 256 px opacos, zero alfa parcial — é o que garante a
  tesselação exata. Conferido depois de gravar.
- **Nenhuma cor inventada**: tudo veio da paleta de 18 cores do próprio pack.
- Os `.meta` **não foram tocados** — os GUIDs seguem os mesmos, e a `RuleTile_Areia` continua
  ligada sem rewiring.

### Como reverter

`git checkout <commit anterior> -- Assets/FavelaAmarela/Art/Tiles/sand_*.png`

Os originais estão no histórico. Nenhum arquivo foi apagado.

### O que ficou de fora, de propósito

O **gradiente interno** foi atenuado, não eliminado: achatá-lo por completo troca a grade de
bevel por um mosaico de manchas, porque os cinco tiles têm tons-base diferentes. O fantasma de
losango que sobra é isso. Quem realmente o mata são **decalques que atravessam a fronteira** da
célula — nenhum tile sozinho consegue esconder que é um tile.
