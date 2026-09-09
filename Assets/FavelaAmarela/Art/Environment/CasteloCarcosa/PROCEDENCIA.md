# Castelo de Carcosa — Z2 e Z3 — procedência

**Origem:** desenhado pixel a pixel por script (`castelo_z2_z3.py`), não gerado por IA de
imagem e não extraído de pacote.

**O que isto substitui.** Até 2026-09-09 os **6 Nobres Fossilizados** do Salão do Banquete e os
**3 Espelhos de Aldebaran** da Biblioteca desenhavam o **sprite embutido da Unity**
(`fileID: 10905`, GUID de recursos internos) — nove quadrados brancos na última fase do jogo —
e nenhum deles tinha componente que trocasse isso em runtime.

**Paleta:** a do Castelo — "mármore negro com adornos de ouro manchado", do documento de level
design — mais o amarelo que já é do Sinal, do robe do Damião, da chama do Poste de Osso e do
Eco de Carcosa: `#C8C0B4`, `#A79E90`, `#7D7466`, `#574F45`, `#D6961E`, `#8A5F14`, `#181422`,
`#3A304A`, `#FFD64A`.

**Formato:** PPU 32, pivô no rodapé, `filterMode: Point`, `textureCompression: None`, conforme
a skill `favela-pixelart-standards`.

| peça | px | em mundo | por quê desse tamanho |
|---|---|---|---|
| `Nobre_Fossilizado_0..2` | 40 × 56 | 1,25 × 1,75 un | O `BoxCollider2D` deles na cena mede **1,20 × 1,00**: são **cobertura**. A altura fica um pouco abaixo do Damião (2,12) para ler como "dá para se agachar atrás" |
| `Espelho_De_Aldebaran_0..2` | 32 × 64 | 1 × 2 un | Não têm colisor: não são obstáculo, são presença |

**Três poses para seis Nobres.** As instâncias 0–5 recebem os quadros 0,1,2,0,1,2 — um brinde
que nunca terminou, um curvado com as mãos na beira da mesa, um recuando de braços abertos.
Seis desenhos únicos custariam muito mais do que o Salão devolve.

**A primeira versão saiu como peão de xadrez** e foi refeita. O erro foi construir o corpo como
uma interpolação única de largura do pescoço ao chão: sem ombro, sem cintura, e com os braços
*dentro* da silhueta, onde não aparecem. A versão que ficou tem plano de corpo — cabeça, ombros
largos, cintura estreita, saia que abre — e os braços saem para fora do contorno, que é o que
faz uma silhueta ler como gente a 56 px de altura.

**Os Espelhos não refletem a sala.** O vidro é uma fresta para outro lugar: uma faixa de luz
violácea que desce torta, uma trinca amarela contínua e um ponto de luz no fundo. É o corpo
visível da `PressaoPsiquicaZone` que fica em cima deles.
