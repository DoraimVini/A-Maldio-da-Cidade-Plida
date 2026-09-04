# Poste de Osso — procedência

**Origem:** desenhado pixel a pixel por script (`poste_de_osso.py`), não gerado por IA de
imagem e não extraído de pacote.

**Paleta:** medida de `Assets/ThirdParty/CraftPix/CursedLand/Objetos/Bones_3.png`, que é o
pacote que faz o chão do Deserto de Hali — `#EDEBDB`, `#D2CEBC`, `#ACA492`, `#8E8475`,
`#57493B`, `#443528`, mais `#6F1919` de sangue seco. A peça precisa parecer do mesmo mundo em
que é fincada.

**Formato:** 32 × 64 px a PPU 32 = 1 × 2 unidades de mundo. Pivô no rodapé (0.5, 0), convenção
isométrica do projeto. `filterMode: Point`, `textureCompression: None`, conforme a skill
`favela-pixelart-standards`.

**Quadros:**

| arquivo | o que é |
|---|---|
| `Poste_Apagado.png` | brasa morta na bacia do crânio |
| `Poste_Aceso_0..3.png` | ciclo da chama, a 8 quadros por segundo |

A chama usa o mesmo amarelo do robe do Damião e do Sinal (`#FFF6C8`, `#FFD64A`, `#D6961E`).

**Para que serve:** é o corpo visível do `RefugioDeLuz` — o único ponto de save do jogo, que
até 2026-09-04 era um `CircleCollider2D` invisível no chão.
