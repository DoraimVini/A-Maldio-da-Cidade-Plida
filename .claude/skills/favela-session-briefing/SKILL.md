---
name: favela-session-briefing
description: Triggers at the start of a work session on this project — phrases like "vamos começar", "bora trabalhar no jogo", "onde paramos", "o que fazer agora", "retomar o projeto". Gathers git state, last devlog entry and the next roadmap item before any coding starts.
---

# Favela Amarela - Session Briefing

## Objective
Before doing any work in a fresh session, get oriented: what state is the repo in, what
happened last session, and what's the next actionable item on the roadmap. Do this BEFORE
proposing or writing any code.

## Execution Rules

1. **Git state:** run `git status` and `git log --oneline -10` in the game repo
   (`C:\Users\Vini\Desktop\Peregrino_Amarelo\Peregrino_Amarelo`). Confirm current branch,
   any uncommitted changes, and the last few commits.
2. **Last session recap:** read only the **top 1-2 dated entries** of
   `Docs\KnowledgeBundle\log.md` — not the whole file. That's what happened last time.
3. **Next actionable item:** read `Docs\KnowledgeBundle\roadmap_vertical_slice.md` —
   specifically the "Prioridade 1" table and the first item still marked ❌ or ⚠️. That's
   the next real thing to build. Do not re-derive priority order from scratch; the roadmap
   already has it, plus a "Próximos passos recomendados" note in the Studio Knowledge Base
   mirror (`Projeto_Amarelo/sistemas_implementados.md`) if it's been kept in sync.
4. **MCP Unity check:** verify whether `mcp__mcp-unity__*` tools are available/connected.
   If not, say so upfront — don't propose gameplay changes that need compiling/testing
   until the Vini reconnects the Editor bridge.
5. **Summarize and confirm:** in a few lines, tell the Vini: current branch/git state, what
   was done last session, and the recommended next item — then ask if that's what he wants
   to tackle before writing any code. Do not start implementing until he confirms or
   redirects.

## Key Directories
- `Docs\KnowledgeBundle\roadmap_vertical_slice.md` — estado real item-a-item do Vertical Slice.
- `Docs\KnowledgeBundle\log.md` — devlog técnico, entradas mais recentes no topo.
- `C:\Users\Vini\Desktop\Studio_Knowledge_Base\Projeto_Amarelo\` — cofre Obsidian do
  projeto: referência de produto/narrativa complementar (não técnica). Útil para contexto
  de design, não para estado de implementação (esse vem do roadmap acima).
