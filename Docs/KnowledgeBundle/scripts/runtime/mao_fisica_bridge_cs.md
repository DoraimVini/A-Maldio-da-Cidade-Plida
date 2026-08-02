---
type: C# Script
title: MaoFisicaBridge.cs
description: Adapter que conecta a arma física equipada (Barra Enferrujada) à Unity e resolve o alcance do golpe
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Player/MaoFisicaBridge.cs
tags: [runtime, player, combat]
timestamp: 2026-07-09T00:00:00Z
---

# MaoFisicaBridge

**Namespace:** `FavelaAmarela.Player`
**Tipo:** `public class` (herda de `MonoBehaviour`)

Adapter que conecta a arma equipada na Mão Física à Unity. A arma **não é fixa**: Damião começa desarmado (golpe de Mão Vazia, dano 0) e recebe uma das três armas da Tumba por `EquiparArma(IArmaComHabilidade)`, chamado pelo `BauDaTumba`. Espelha o `EsquivaBridge`: recebe a FSM por `BindStateMachine(PlayerStateMachine)` para exclusão mútua entre ações, expõe `TryAtacar()` (ataque básico) e `TryUsarHabilidade()` (habilidade da arma, cooldown próprio) pro `PlayerMovement` chamar, e resolve o golpe com buffer pré-alocado (`Physics2D.OverlapCircle` + `ContactFilter2D`) mirando qualquer `IDanificavel`.

## Responsabilidades
- `Awake()`: instancia `BarraEnferrujada` com os valores serializados; usa `LayerMask.GetMask("Enemy")` como fallback se `camadaInimigos` não foi setada no Inspector.
- `TryAtacar(Vector2 direcao)`: valida desbloqueio + cooldown, executa o golpe do POCO e resolve quem foi atingido.
- `ResolverGolpe(...)`: varre `Physics2D.OverlapCircleAll` no alcance configurado e chama `CultistaAI.ReceberGolpeFisico(resultado)` em cada alvo atingido.

## API Pública
- `OnAtaqueExecutado(Vector2, float)` (evento): direção e duração do golpe.
- `IsAtacando` (`bool`): se um golpe está em andamento (bloqueia reentrância via `Invoke(EndAtaque, ...)`).
- `ArmaDesbloqueada` (`bool`): se a arma já foi adquirida.
- `DesbloquearArma()`: chamado pelo pickup da arma na Zona 5 — Damião nasce **desarmado**.

## Progressão
`desbloqueadaNoInicio` no Inspector é só para testar combate isolado; no fluxo real do jogo a arma começa bloqueada.
