---
type: Cinematic Design
title: Cinemática de Abertura — O Deserto de Hali
description: Storyboard e design da cinemática de abertura da Fase 1, quando Damião adentra o Deserto de Hali pela Garganta de Pedra Pálida.
tags: [cinematica, abertura, deserto-de-hali, storyboard, narrativa, espectros, tempestade]
timestamp: 2026-07-28T13:30:00-03:00
status: Design aprovado, implementação pendente
---

# Cinemática de Abertura — O Deserto de Hali

Sequência não-interativa que antecede o controle do jogador. Dura aproximadamente **45 a 60 segundos**. Sem diálogos falados. Apenas som ambiente, música e legendas opcionais de um pensamento interno de Damião.

---

## DESIGN GERAL

- **Estilo de câmera:** Fixed isometric — a câmera não segue Damião como no gameplay. Cada shot é um tableau fixo com Damião se movendo dentro dele.
- **Sem HUD:** Nenhum elemento de interface durante a cinemática.
- **Controle:** O jogador **não tem controle** até o Shot 6 (fade in do gameplay).
- **Input para pular:** Qualquer botão pula para o Shot 6 com fade rápido.
- **Implementação Unity:** Série de shots controlados por `Animator` ou `Timeline`, com `TempestadeVisualOverlay` e `TempestadeAmbiente` já existentes rampando gradualmente.

---

## STORYBOARD — BEAT A BEAT

---

### SHOT 1 — NEGRO E VENTO (0s–4s)

**Visual:** Tela completamente preta.

**Áudio:**
- Silêncio absoluto por 1 segundo.
- Depois: o som de vento distante começa — fraco, quase inaudível. Uma frequência baixa, como pressão nos ouvidos.
- Aos 3s: uma rajada súbita de areia — o som cresce abruptamente e recua.

**Propósito:** Desorientação total. O jogador não sabe onde está, nem o que virá.

---

### SHOT 2 — OS PÉS DE DAMIÃO (4s–9s)

**Visual:** Close nos pés de Damião — sapatos desgastados, meias arrastando na areia de cinza. Ele caminha devagar, cada passo pesado e incerto. O chão é cinza pálido, seco, com rachaduras. Partículas de areia raspam o chão da esquerda para a direita.

**Câmera:** Ângulo baixo, quase rente ao chão. Damião se move da esquerda para o centro do frame.

**Áudio:** Passos abafados na cinza. O vento continua crescendo.

**Propósito:** Apresentar Damião como figura humana, frágil, sem revelar o mundo ainda.

---

### SHOT 3 — A GARGANTA (ESTABLISHING SHOT) (9s–18s)

**Visual:** Pull back para o establishing shot completo: Damião, pequeno e curvado, caminha pela Garganta de Pedra Pálida. As duas paredes de rocha branca se erguem massivas em ambos os lados — enormes, verticais, cobertas de veias escuras. O céu acima é uma fissura de cor âmbar doente entre as rochas.

Ao fundo do canyon, além de Damião, a saída: o deserto começa a aparecer — uma claridade estranha e opressora.

Um único **poste de luz rachado** na parede direita ainda emite um fio de luz amarela quente sobre a cinza do chão. Damião passa por ele sem parar.

**Câmera:** Wide shot estático, levemente elevado.

**Áudio:**
- Vento nos corredores da garganta — um uivo geométrico, quase mecânico.
- Uma nota grave de sintetizador distorcido surge no fundo — a assinatura sonora de Carcosa.

**Pensamento de Damião (legenda, opcional):**
> *"As inscrições diziam: quem lê a segunda parte da Peça não pode voltar atrás."*

**Propósito:** Estabelecer a escala. Damião é minúsculo. O mundo é hostil e antigo.

---

### SHOT 4 — A SAÍDA DA GARGANTA (18s–27s)

**Visual:** Damião sai da garganta e para abruptamente. Câmera na sua frente — vemos suas costas e, além dele, o Deserto de Hali se abre pela primeira vez. Vastidão de dunas cinza-amareladas sob um céu que queima âmbar-sujo.

Os **dois sóis gêmeos** são visíveis no horizonte: dois círculos pálidos sobrepostos, como olhos semicerrados, que não aquecem nada.

Ao longe, no centro-esquerdo: a superfície negra e perfeitamente imóvel do **Lago de Hali** — uma mancha de ausência num mundo de cinza.

**Câmera:** Câmera posicionada à frente de Damião, angle médio — vemos sua reação de parar, seus ombros caírem levemente.

**Áudio:**
- O vento para por exatamente 2 segundos — silêncio súbito e irreal.
- Depois: uma rajada violenta explode — a mais forte até agora.

---

### SHOT 5 — A RAJADA E OS ESPECTROS (27s–42s)

**Visual:** A rajada de areia envolve Damião completamente — a tela fica quase opaca de partículas âmbar e brancas. Damião se curva, braço no rosto, resistindo.

Quando a rajada começa a dissipar levemente: **no céu ao fundo**, três formas emergem da tempestade.

**Os Espectros de Hali** — translúcidos, alongados, vagamente humanoides mas deformados pelo vento — derivam silenciosamente para cima na corrente do ar. Eles não se movem como seres vivos: se movem como a fumaça. Como se a tempestade os transportasse. Emitem uma luz pálida fraca — quase invisível.

Damião os vê. Sua postura muda — ele congela, encolhido.

Os espectros desaparecem atrás da próxima parede de areia.

A tempestade enfraquece levemente. O céu volta a ser visível — com o deserto se estendendo além.

**Câmera:** Wide shot — Damião pequeno no canto inferior esquerdo do frame. Os espectros dominam o céu no terço superior.

**Áudio:**
- Durante a rajada: ruído de areia + frequência de 7.83 Hz pulsando no fundo (frequência de Carcosa).
- Quando os espectros aparecem: silêncio dos outros sons. Só o 7.83 Hz e um tom agudo e fraco, quase um gemido.
- Quando desaparecem: o vento retorna gradualmente.

**Pensamento de Damião (legenda, opcional):**
> *"Eles não me viram. Ou viram, e não se importaram."*

**Propósito:** Estabelecer os espectros como ameaça real e alienígena. Mostrar que Damião está **em território de criaturas**, não de humanos.

---

### SHOT 6 — DAMIÃO AVANÇA (42s–52s)

**Visual:** Damião, ainda curvado, dá um passo para frente. Depois outro. Ele não corre — ele avança com a determinação hesitante de quem não tem escolha.

A câmera **recua lentamente** enquanto ele avança, revelando cada vez mais o deserto à sua frente — as dunas, as ruínas no horizonte, a escuridão do Lago de Hali.

Ele fica cada vez menor no frame. O deserto fica cada vez maior.

**Câmera:** Travelling back suave — efeito de Damião encolhendo contra o ambiente.

**Áudio:**
- A trilha do drone silencioso começa (estado musical de Exploração).
- Batidas industriais sutis e esporádicas — presença, não urgência.

---

### SHOT 7 — FADE E CONTROLE (52s–60s)

**Visual:** Um fade suave para o estado de gameplay — a câmera assume a posição padrão isométrica de follow, a tempestade está em intensidade "Entrada" (calma), e o jogador recebe controle de Damião.

Sem título na tela. Sem "Fase 1". Sem fanfarra.

O jogo simplesmente... começa.

**Áudio:** Crossfade suave do drone para o loop de Exploração padrão.

---

## DIAGRAMA DE SHOTS

```
[NEGRO + VENTO] → [PÉS DE DAMIÃO] → [GARGANTA (wide)] → [SAÍDA DO CANYON]
      1s–4s            4s–9s              9s–18s               18s–27s
                                                                    │
                                                                    ▼
                                               [RAJADA + ESPECTROS] → [DAMIÃO AVANÇA] → [FADE/GAMEPLAY]
                                                     27s–42s               42s–52s           52s–60s
```

---

## ELEMENTOS TÉCNICOS EXISTENTES QUE A CINEMÁTICA PODE REUSAR

| Elemento | Sistema existente | Como usar |
| :--- | :--- | :--- |
| Tempestade de areia | `TempestadeAmbiente` + `TempestadeVisualOverlay` | Rampar StormIntensity de 0 → 0.9 → 0.3 ao longo dos shots 3–6 |
| Espectros voando | `EspectroHali.prefab` + `EspectroAI` | Spawnar 2-3 espectros com física Kinematic somente para o shot 5, destruídos após |
| Damião | `Player_Damiao.prefab` | Mover via `rb.MovePosition` ou script de cinemática — Input desabilitado |
| Drone musical | `TempestadeAmbiente` | Activar o layer de Exploração no AudioMixer durante o fade final |
| Overlay de tela | `ScreenFader.cs` | Fade in/out no Shot 1 e Shot 7 |

---

## PENDÊNCIAS DE IMPLEMENTAÇÃO

- **Câmera de cinemática:** definir se usará `Cinemachine` com shots fixos ou câmera manual via `Timeline`.
- **Animação dos espectros no voo:** o sprite atual do Espectro tem Idle/Move/Attack/Death — verificar se Move serve para o voo horizontal.
- **Pensamentos de Damião:** decidir se as legendas internas são implementadas (texto UI) ou descartadas (cinemática completamente muda).
- **Script de controle da sequência:** criar `AberturaDesertoCinematica.cs` (Runtime) para orquestrar os shots, a rampa de tempestade e o handoff de controle ao jogador.
