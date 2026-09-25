---
type: C# Script
title: CameraController.cs (classe IsometricCameraController)
description: Controlador de câmera ortográfica 2D isométrica — segue o alvo e expõe shake/zoom
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Camera/CameraController.cs
tags: [runtime, camera]
timestamp: 2026-07-09T00:00:00Z
---

# IsometricCameraController

**Namespace:** `FavelaAmarela.CameraSystem`
**Tipo:** `public class` (herda de `MonoBehaviour`)

> Nota: o arquivo se chama `CameraController.cs`, mas a classe pública dentro dele é `IsometricCameraController` — use esse nome ao procurar a referência no código ou no Inspector.

Controlador de câmera ortográfica 2D para a visão isométrica top-down. Anexado à Main Camera; segue um alvo (`target`) com `Vector3.SmoothDamp`. Sempre mantém `cam.orthographic = true` e nunca tilta a câmera fisicamente — a "sensação" isométrica vem do Y-sorting e de `PlayerMovement.ConvertToIsometric`, não de rotação de câmera (ver skill `favela-isometric-standards`).

## API Pública
- `Shake(float duration, float magnitude)`: sacode a câmera por `duration` segundos com deslocamento aleatório de até `magnitude` unidades por frame (`LateUpdate`). Reaproveitável por qualquer evento de impacto (ex.: chão desmoronando na queda Z4→Z5).
- `SetZoom(float newSize)`: atualiza `orthographicSize` em runtime (ex.: aproximar na arena durante a luta do Abdul).
- `SetTarget(Transform newTarget)`: troca o alvo seguido.

## Robustez
`Awake()` valida a presença do componente `Camera` e loga aviso se nenhum `target` foi atribuído — segue sem quebrar (câmera fica parada).
