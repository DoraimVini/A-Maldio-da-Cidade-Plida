---
type: C# Script
title: NavegadorDeOpcoes.cs
description: Cursor puro de uma escolha de diálogo (navegar + confirmar)
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Core/Dialogo/NavegadorDeOpcoes.cs
tags: [core, dialogo]
timestamp: 2026-07-30T00:00:00Z
---

# NavegadorDeOpcoes

**Namespace:** `FavelaAmarela.Core.Dialogo`
**Tipo:** `public sealed class` (POCO puro)

Regra de navegação de uma escolha de N opções — usado pelo `PainelDeEscolha` (Runtime/UI)
na conversa com Abdul, mas **genérico**: qualquer diálogo ramificado futuro reusa a mesma peça.

Mantém o índice selecionado, trata mover para cima/baixo (com ou sem wrap) e confirmar.
`OpcaoDeDialogo` é o par texto + id que o chamador usa para saber o que foi escolhido.
