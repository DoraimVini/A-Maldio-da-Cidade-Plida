---
type: Game System
title: Sistema de Furtividade (Stealth)
description: Detalhes das regras de detecção acústica (implementada) e detecção visual por cone de visão (planejada)
tags: [stealth, design, sound-propagation, vision-cone, cultista]
timestamp: 2026-07-10T15:00:00Z
---

# Sistema de Furtividade (Stealth)

O stealth é a mecânica de sobrevivência primária de Damião. O ambiente de Carcosa pune o barulho e a exposição.

---

## 1. Detecção Sonora (Acústica — Implementada)

A detecção por som baseia-se em um modelo físico e geométrico de propagação de ruído na cena.

### 1.1 O Vetor do Som (`SoundBroadcastService.cs`)
Qualquer ação de Damião que gere ruído (caminhar rápido, correr, esquivar ou teleportar com o Salto) emite um sinal circular no mundo:
*   **Origem:** A coordenada `Vector2` onde o som foi emitido.
*   **Intensidade (Raio Efetivo):** A distância em metros que o som se propaga antes de desaparecer.
    *   *Agachado:* Raio = $0.0$ metros (silêncio total).
    *   *Caminhando:* Raio = $2.0$ metros.
    *   *Correndo:* Raio = $6.0$ metros.
    *   *Salto Dimensional (Estática):* Raio = $8.0$ metros na origem e no destino.

### 1.2 Reação da IA (`CultistaFSM.cs`)
Os cultistas possuem uma assinatura auditiva. O método `ReceberEstimuloSonoro(origemSom, distanciaAoJogador, raioEfetivo)` é disparado:
1.  Se a distância física entre o Cultista e o jogador for menor ou igual ao `raioEfetivo`, o cultista registra o estímulo.
2.  A IA armazena a `UltimaOrigemConhecida = origemSom`.
3.  **Transição de Estado:**
    *   Se no estado `Errante` $\rightarrow$ Muda imediatamente para `Alerta`.
    *   No estado `Alerta`, o cultista pausa por $1.5$ segundos (telegrafando a suspeita para o jogador) e caminha até o ponto do som.
    *   Se ele alcançar a origem do som e ouvir outro ruído em até $1.5$ segundos, muda para `Caca` (Perseguição ativa).

---

## 2. Detecção Visual (Cone de Visão — Planejada)

Diferente do som (que é instantâneo e circular), a visão é direcional, contínua e bloqueada por obstáculos físicos do cenário.

### 2.1 Métricas do Cone de Visão Isométrico
Os inimigos (Cultistas e Sentinelas Byakhee) terão cones de detecção visual projetados no plano 2D (XY):
*   **Alcance Máximo ($R$):** $6.0$ unidades.
*   **Ângulo de Abertura ($\theta$):** $90^\circ$ ($45^\circ$ para a esquerda e para a direita em relação ao vetor de direção do inimigo).
*   **Mapeamento de Eixo:** Como o jogo é isométrico, a direção visual do inimigo é mapeada em 8 direções discretas baseadas no seu vetor de patrulha.

### 2.2 Algoritmo de Oclusão (Raycasting)
Para simular a ocultação física atrás de paredes ou casas:
1.  **Filtro de Ângulo e Distância:** Verifica se a posição de Damião está dentro de $R$ e se o ângulo entre a frente do inimigo e a direção do jogador é menor que $\theta/2$.
2.  **Linha de Visão (Raycast):** Se passar no filtro, o motor dispara um Raycast físico no plano 2D direcionado a Damião.
    *   Se o raio colidir com um objeto contendo `BoxCollider2D` marcado como parede (`WallData`) antes de colidir com Damião, a linha de visão é **bloqueada** (jogador permanece oculto).
    *   Se o raio colidir com Damião primeiro, ele é **visto**.

### 2.3 Barra de Alerta (Detection Meter)
A detecção visual não causa alerta instantâneo:
*   Ao entrar na linha de visão do cultista, uma **barra de suspeita** sobre a cabeça do inimigo começa a encher.
*   **Velocidade de Acúmulo:** Depende da distância de Damião:
    *   *Perto (< 2m):* Enche 100% em $0.5$ segundos.
    *   *Longe (> 4m):* Enche em $2.0$ segundos.
*   Se a barra atingir $100\%$, o cultista entra no estado `Caca` imediatamente. Se Damião sair de vista antes de encher, a barra drena gradualmente.

---

## 3. Matriz de Detecção de Ameaças

| Tipo de Inimigo | Sensibilidade Sonora | Sensibilidade Visual | Comportamento Principal |
| :--- | :--- | :--- | :--- |
| **Cultista Amarelo** | Alta (Raio 100%) | Média ($90^\circ$ / 6m) | Patrulha ativa, investiga sons rapidamente, corre em sua direção. |
| **Byakhee (Sentinela)** | Nula (Ignora som) | Altíssima ($120^\circ$ / 12m) | Fixo no topo. Grita ao avistar o jogador, alertando todos os cultistas próximos para `Caca`. |
| **Coisa do Cemitério** | Média (Ignora alertas) | Nula (Cego) | **Faro ativo.** Segue a trilha olfativa de Damião, movendo-se na sua direção geral sem precisar de som ou visão. |
