---
type: C# Script
title: HUDController.cs
description: Controlador central da interface do jogador
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/UI/HUDController.cs
tags: [runtime, ui, hud]
timestamp: 2026-07-07T11:00:00Z
---

# HUDController

**Namespace:** `FavelaAmarela.UI` (deduzido)  
**Tipo:** `public class` (herda de `MonoBehaviour`)

Script responsável por orquestrar a Heads Up Display (HUD). 

## Responsabilidades
Como um adapter visual, ele não mantém estado próprio de game design. Ele observa os eventos dos POCOs da [camada Core](../core/index.md) (como `ResilienciaMental.OnChanged`) para atualizar os elementos da tela.

*(Nota: Referência preliminar; veja o código-fonte para APIs exatas do adapter).*
