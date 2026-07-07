---
name: query-knowledge-bundle
description: Triggers whenever the user asks about game mechanics, lore, enemies, stealth rules, or architecture. Instructs the agent to actively search and read the Open Knowledge Format (OKF) documentation before coding.
---

# Favela Amarela - Knowledge Bundle Router (Second Brain)

## Objective
This project uses an Open Knowledge Format (OKF) database located in `Docs/KnowledgeBundle/`. It acts as the ultimate source of truth for Game Design, Architecture, Lore, and Systems. 

You must NOT hallucinate or guess how a system (like Stealth, AI, Mental Resilience, or Lore) works. You must query the Knowledge Bundle.

## Execution Rules
Whenever you are asked to implement, fix, or discuss a system, you MUST perform the following steps before writing any code:

1. **Search:** Use your file search or grep capabilities to search inside `Docs/KnowledgeBundle/` for keywords related to the user's prompt (e.g., `grep -i "cultista" Docs/KnowledgeBundle/systems/`).
2. **Read:** Once you find the relevant `.md` files (e.g., `cultista_ai.md`, `bestiary.md`, `resiliencia_mental.md`), use your file reading tool to read their full contents.
3. **Apply:** Write your code or response strictly adhering to the rules, edge cases, and architectures defined in those documents.
4. **Update (If needed):** If you create a brand new mechanic or significantly alter an existing one during the conversation, you MUST update the relevant `.md` file in the Knowledge Bundle to reflect the new reality, keeping the Second Brain up to date.

## Key Directories
- `Docs/KnowledgeBundle/systems/` - AI behaviors, FSMs, Combat, Stealth rules.
- `Docs/KnowledgeBundle/lore/` - The Bestiary, World Rules, and Glossary.
- `Docs/KnowledgeBundle/architecture/` - C# standards and POCO rules.
