---
type: C# Script
title: AbdulFSM.cs
description: Máquina de estados da luta contra Abdul Alhazred (fases + Escudo Mágico)
resource: file:///C:/Users/Vini/Desktop/projeto_amarelo/A%20Maldi%C3%A7%C3%A3o%20da%20Cidade%20P%C3%A1lida/Assets/Scripts/Core/Enemies/AbdulFSM.cs
tags: [core, enemies, fsm, boss]
timestamp: 2026-07-30T00:00:00Z
---

# AbdulFSM

**Namespace:** `FavelaAmarela.Core.Enemies`
**Tipo:** `public sealed class` (POCO puro)

Regras da luta do boss. Design completo em [Luta contra Abdul](../../systems/boss_abdul.md).

**Estados** (`AbdulState`): `Transe` → `Fase1` → `Fase2` ⇄ `Exausto` → `Derrotado`.

## API Pública
- `PodeReceberDano` — **o coração da luta**: só é true com escudo baixo e luta em andamento
- `EscudoAtivo`, `PedrasQuebradas`, `MagiasNoCiclo` — estado observável
- `IniciarLuta()` — tira do Transe (chamado pela escolha "Lutar" ou pela traição da trégua)
- `QuebrarPedraDePoder()` — **só tem efeito na Fase 1**; na Fase 2 o escudo não depende mais das Pedras
- `AtualizarFracaoDeVida(fracao)` — dispara virada de fase e derrota
- `Tick(dt)` — relógio da luta
- Eventos: `OnStateChanged`, `OnEscudoMudou`, `OnInvocarEsqueletos`, `OnConjurarConeDeGelo`, `OnDerrotado`

**A FSM não guarda a vida** — quem tem a `Vitalidade` é o adaptador, que informa a fração
restante. Mesma divisão de `CultistaFSM`/`CultistaAI`. 13 testes EditMode.
