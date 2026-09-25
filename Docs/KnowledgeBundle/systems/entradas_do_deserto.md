---
type: Art Asset
title: Entradas das Localizações do Deserto de Hali
description: Os 5 dioramas isométricos que marcam as entradas das localizações do Deserto — fatiamento, escala e configuração de import.
tags: [arte, sprites, deserto, entradas, pixelart]
---

# Entradas das Localizações do Deserto de Hali

Cinco dioramas isométricos que marcam, no overworld do Deserto, a entrada de cada
localização. Fatiados de uma folha única 2048×2048 gerada por IA.

| Sprite | Localização | Tamanho | No mundo (PPU 32) |
|---|---|---|---|
| `Entrada_TumbaDeAlhazred` | Tumba de Abdul Alhazred (Dungeon 1) | 192×138 | 6,0 × 4,3 un |
| `Entrada_SantuarioDeYhtill` | Santuário de Yhtill | 192×162 | 6,0 × 5,1 un |
| `Entrada_LagoNegroDeHali` | Lago Negro de Hali | 192×140 | 6,0 × 4,4 un |
| `Entrada_TemploDoPovoSerpente` | Templo do Povo Serpente (Dungeon 2) | 192×196 | 6,0 × 6,1 un |
| `Entrada_PortoesDeCarcosa` | Portões de Carcosa (saída da Fase 1) | 192×198 | 6,0 × 6,2 un |

Todos em `Assets/FavelaAmarela/Art/Entradas/`.

> **A arte escreve "YTHIL" no rótulo — é typo.** O lore do projeto usa **Yhtill** (61
> ocorrências na base). O nome do asset segue o lore, não a arte. Se a folha for regerada,
> pedir a grafia correta.

> **Nome divergente nos Portões:** o objeto na cena chama-se `Portoes_DasRuinas`, a arte diz
> "Portões de Carcosa", e a mecânica do companheiro fala em "abrir os Portões de Carcosa".
> São o mesmo lugar (a saída da Fase 1) com dois nomes. Não renomeei o objeto de cena — é
> decisão do Vini qual grafia vira a oficial.

## Configuração de import (obrigatória)

Segue a skill `favela-pixelart-standards`, verificada no `.meta` após a importação:

- **PPU 32** — convenção real do projeto (Damião, Cultista, Abdul e os tiles de areia
  usam 32; os PPU 100/160 que aparecem em outros assets são exceções de concept art).
- **Filter Mode: Point** — nunca Bilinear em pixel art.
- **Compression: None** — compressão destrói as bordas e a paleta.
- **Pivot: Bottom-center** (`alignment: 7`, `spritePivot {0.5, 0}`) — necessário para o
  Y-sorting isométrico (`DynamicYSort` ordena por `-y`), igual aos personagens.

Os `.meta` usam **GUID determinístico** (MD5 do nome do asset): regerar o arquivo não
quebra referências já feitas em cenas ou prefabs.

## Como foram fatiados

O original tinha os 5 dioramas soltos sobre um fundo bege com grade, mais os rótulos em
texto. O fatiamento foi automatizado (não recorte manual) para ser reprodutível:

1. **Preenchimento a partir das bordas** por luminância, com tolerância que atravessa a
   grade do fundo mas **para nos contornos escuros** dos dioramas. Um limiar simples de cor
   não serviria: partes da arte (a pedra do Santuário, por exemplo) têm exatamente a mesma
   cor do fundo — o que as distingue é estarem *cercadas*, não a cor.
2. **Preenchimento de buracos** — regiões cor-de-fundo cercadas por arte voltam a ser arte.
3. **Componentes conexos** → os 5 dioramas emergem como os 5 maiores. Fragmentos soltos
   (arcos de sombra, detalhes finos) são reunidos ao diorama cuja caixa os contém.
4. **Rótulos excluídos automaticamente**: as letras caem fora da caixa de todos os
   dioramas, então nenhuma decisão manual foi necessária.

### Redução para escala de jogo

O original entregava ~1000 px por diorama — a PPU 32 isso seria **31 unidades de mundo**,
com o Damião medindo ~1–2. Reduzidos para 192 px de largura (~6 un), tamanho escolhido para
ler de longe sem dominar um setor onde os pontos ficam ~11–20 unidades um do outro.

Duas escolhas na redução:
- **LANCZOS**, não nearest: preserva melhor a silhueta nesta arte, que é *ilustração em
  estilo pixel art em alta resolução*, não pixel art nativa. O aspecto cru vem do Point
  filter na Unity.
- **Alpha binário** (cada pixel opaco ou transparente): meio-tom na borda vira franja clara
  sob Point filter.

> Os recortes em **resolução cheia** (~1000 px) ficaram preservados fora do projeto. Se a
> escala precisar mudar, regenerar a partir deles — não reamplie os de 192 px.

## Em cena (2026-08-01)

Os 5 sprites já estão atribuídos às localizações que **já existiam** no `Deserto_Hali`:

| Objeto de cena | Posição | Sprite |
|---|---|---|
| `Entrada_TumbaAlhazred` | (-17, -2) | `Entrada_TumbaDeAlhazred` |
| `Santuario_Yhtill` | (-15, 10) | `Entrada_SantuarioDeYhtill` |
| `Lago_De_Hali` | (4, 6) | `Entrada_LagoNegroDeHali` |
| `Entrada_TemploSerpente` | (18, 0) | `Entrada_TemploDoPovoSerpente` |
| `Portoes_DasRuinas` | (-4, 14) | `Entrada_PortoesDeCarcosa` |

Ferramenta: `Tools/FavelaAmarela/Montar Deserto de Hali` (idempotente).

> **Duas armadilhas encontradas ao ligar:**
> 1. As localizações **já tinham sprite** — o quadrado embutido da Unity como placeholder.
>    Uma guarda "só atribui se estiver vazio" não substituiria nada. A regra correta é
>    "substitui enquanto o sprite não vier da pasta `Entradas/`".
> 2. Os placeholders usavam **tint** para se diferenciar (Tumba vermelha, Lago preto,
>    Santuário amarelo). Trocar só o sprite deixaria a arte tingida — o tint precisa voltar
>    a branco junto.

Cada uma ganhou `DynamicYSort`, para o Damião passar por trás/na frente corretamente.

## Pendente

- Falta ligar 4 das 5 ao `PortalDeCena`: só a Tumba tem cena de destino. Santuário, Lago,
  Templo e Portões ainda não têm cena para onde levar.
