# Eco de Carcosa — procedência

**Origem:** desenhado pixel a pixel por script (`eco_de_carcosa.py`), não gerado por IA de
imagem e não extraído de pacote.

**Paleta:** a do Castelo de Carcosa — "mármore negro com adornos de ouro manchado", do
documento de level design — mais o amarelo que já é do Sinal, do robe do Damião e da chama do
Poste de Osso: `#0B0A10`, `#1E1A24`, `#3A3040`, `#54465C`, `#D6961E`, `#FFD64A`, `#FFF6C8`.

**Formato:** 32 × 64 px a PPU 32 = 1 × 2 unidades de mundo. Pivô no rodapé (0.5, 0), convenção
isométrica do projeto. `filterMode: Point`, `textureCompression: None`, conforme a skill
`favela-pixelart-standards`.

**Quadros:** `Eco_0..3.png`, a 6 quadros por segundo. O sopro move a barra esfarrapada e a
fumaça; o Sinal Amarelo pulsa por cima (`Eco_2` é o pico). Fogo e presença que só piscam de
brilho leem como lâmpada — o que muda de forma lê como coisa viva.

**Por que não tem rosto.** A boca do capuz é um buraco. Um rosto daria dono à aparição, e o
`EcoDeCarcosa` é o oposto disso: ele se manifesta **nas costas** de Damião quando o jogador
fica parado, e some quando ele volta a andar.

**Altura:** o corpo desenhado ocupa 1,69 unidade contra 2,12 do Damião. É de propósito — ele
não tem pé, se dissolve antes do chão, e fica **atrás** do jogador a 1,5 unidade.

**Para que serve:** é o corpo visível do anti-camping do Castelo. Até 2026-09-04 os dois
`Eco_De_Carcosa` da Biblioteca (Z3) não tinham `SpriteRenderer` **nem filho nenhum** — e
`AtivarEco()` liga filhos. O Eco se manifestava, drenava 3 de Resiliência Mental por segundo e
**nada aparecia na tela**: a sanidade caía sem causa visível, num jogo em que Resiliência
zerada é derrota.
