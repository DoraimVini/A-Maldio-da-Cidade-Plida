---
type: Briefing
title: Contexto Completo — Para Outros Agentes de IA (Antigravity, etc.)
description: Ponto de entrada único para qualquer agente que não acompanhou a sessão de 2026-08-11. O que existe, o que mudou, o que está pendente, e as armadilhas já pagas.
tags: [briefing, contexto, onboarding, antigravity, sessao]
timestamp: 2026-08-11T00:00:00Z
---

# Contexto Completo da Sessão de 2026-08-11

> **Para quem é este documento:** qualquer agente de IA (Antigravity, outra sessão do Claude
> Code, um humano voltando depois de dias) que precise entender **o que este projeto é agora**
> sem reconstruir o raciocínio do zero. Leia isto antes de tocar em qualquer sistema listado
> abaixo — vários têm decisão de design ou pendência que não é óbvia só olhando o código.

## 1. O que é este projeto, em 3 frases

Jogo de stealth/horror cósmico em Unity 6000.4.4f1, 2D isométrico, ambientado em Carcosa
(mito de Hastur/Chambers). Protagonista Damião, preso nas Ruínas Pálidas. Arquitetura
**estrita** POCO (`FavelaAmarela.Core.*`, C# puro testável) + adaptador MonoBehaviour
(`FavelaAmarela.Runtime.*`) — regra de ouro do `CLAUDE.md` §2, não é sugestão.

**Título oficial mostrado ao jogador: "Caminho para Carcosa"** — o repositório, a pasta e os
namespaces continuam com outros nomes por serem retrabalho sem ganho (ver `CLAUDE.md` topo).

## 2. Estado do git AGORA

- **Branch de trabalho:** `feat/fase1-deserto-hali` (não `main`, não `develop*`)
- Havia **três branches** (`develop`, `develop_items`, `develop_manager`) paradas num commit
  de semanas atrás. Uma troca acidental de branch (provavelmente por GUI de Git) já causou um
  susto grande nesta sessão — confirme sempre `git branch --show-current` antes de assumir
  que está no lugar certo.
- Todo commit desta sessão está pushado para `origin/feat/fase1-deserto-hali`.

## 3. Regra de trabalho não-negociável: QA antes de considerar pronto

A skill `favela-qa-pipeline` exige compilar + rodar os testes EditMode antes de qualquer
entrega. **O Unity Editor precisa estar FECHADO** para isso — o batch mode
(`Tools/run_qa_tests.ps1`) rejeita com "another Unity instance running" se houver instância
aberta. Isso já causou retrabalho várias vezes nesta sessão porque scripts foram editados com
o Play Mode rodando.

> **Ver [unity64_gotchas/domain_reload_em_play_mode.md](unity64_gotchas/domain_reload_em_play_mode.md):**
> editar script com o Editor aberto em Play causa domain reload, que zera todo POCO criado em
> `Awake()` sem recriar — produz uma cascata de `NullReferenceException` em scripts sem
> relação nenhuma entre si. Não é bug de código quando isso acontece; é sintoma de reload.

Também existe `Tools/FavelaAmarela/Rodar TODO o wiring (na ordem)` que executa as sete
ferramentas de montagem de cena **headless** via `-executeMethod`, sem abrir o Editor:
```
Unity.exe -batchmode -nographics -quit -projectPath . \
  -executeMethod FavelaAmarela.EditorTools.RodarTodoOWiring.Executar
```

## 4. O que foi construído nesta sessão (ordem cronológica)

Cada item tem doc próprio em `systems/` — isto é só o mapa. Total: **426 testes EditMode**,
todos verdes na última rodada.

| # | Sistema | Doc | Estado |
|---|---|---|---|
| 1 | Motor de loot (`SorteioDeDrop`, `TabelaDeDrop`, tiers por nível) | [loot_e_drop.md](systems/loot_e_drop.md) | Implementado, `BauDaTumba` migrado |
| 2 | Armaduras básicas + Set Lendário (Elmo/Peitoral/Grevas de Set) | [reliquias_de_hali.md](systems/reliquias_de_hali.md), [reliquias_cosmicas.md](lore/reliquias_cosmicas.md) | Assets criados; **Arma de Set não existe** |
| 3 | Artefatos (4 slots, Necronomicon/Patuá migrados de Chave/Amuleto) | [artefatos.md](systems/artefatos.md) | Implementado |
| 4 | Áudio (stealth audível, combate, resiliência) | [audio.md](systems/audio.md) | Implementado, sem clipes reais (síntese) |
| 5 | Persistência corrigida (bug: save nunca era lido de volta) | [persistencia.md](systems/persistencia.md) | Corrigido |
| 6 | Consumíveis (3 itens, molde do inventário de armas) | — | Implementado |
| 7 | Painel de inventário (Tab/I) | — | Implementado |
| 8 | Deserto povoado + Coisa do Cemitério | — | Implementado (densidade por tempestade) |
| 9 | Fluxo de jogo: menu virou **cena própria** (`Cena_Menu`, índice 0 do build) | — | Implementado |
| 10 | Layer `Aliados` + barra de vida flutuante (Yug-Neth agora é alvo válido) | — | Implementado |
| 11 | **Boss Byakhee** (Core + Runtime) | [boss_byakhee.md](systems/boss_byakhee.md) | **Ver §5 — desbalanceado, correção pendente** |
| 12 | `SampleScene` removida (blockout morto, zero referências) | — | Removida |

## 5. PENDÊNCIA ATIVA: Byakhee está matematicamente invencível

Isto é o que estava em discussão no momento em que este doc foi escrito — **qualquer agente
que continue a sessão deve resolver isto antes de seguir para o Rei em Amarelo.**

Com os números atuais (Vitalidade 420, pouso espontâneo da fase 3 a cada 30s), a luta leva
**~52 segundos** de execução perfeita, mas o grito infrassônico (2 RM/s passivo) drena **104
RM** nesse tempo — Damião só tem **100**. Ele colapsa mentalmente antes de vencer, mesmo
jogando sem erro.

**Bug real encontrado junto (corrigir sempre, independente de balance):** `GolpearComGarras()`
em `ByakheeAI.cs` fere o jogador **sem checar distância** — cada pouso causa 20 de dano mesmo
do outro lado da arena.

**Correções propostas, aguardando confirmação do Vini:**
1. Garras checarem alcance (~1,5 un.) — **isto é bug, aplicar sempre**
2. Pouso espontâneo da fase 3: 30s → 10s
3. Vitalidade: 420 → 300

**Pergunta de design em aberto:** o Byakhee deve ser vencível **sem** o Patuá/Necronomicon
equipados? O Patuá dá +1,5 RegenRM, que abateria quase todo o dreno passivo — se a resposta
for "artefatos são obrigatórios", os números atuais quase fecham, mas isso trava o chefe atrás
da quest da Cassilda (item de conteúdo secundário, não garantido no VS).

## 6. O buraco filosófico maior: curva de poder do Deserto

O Vini apontou (2026-08-11) que o jogo está **punitivo demais** na abertura: Damião começa
**desarmado** (Ataque 0 sem arma), o Deserto já está povoado com 11 Cultistas, e até este
boss ser criado **nenhum chefe largava equipamento** — só artefato. O Set Lendário (a melhor
armadura do jogo) foi autorado **sem nenhuma fonte de obtenção** (Templo da Serpente não tem
cena).

`Drop_Byakhee` (Anel do Sinal Amarelo garantido) é a primeira correção disso. Falta:
- Arma de Set (ainda não decidida)
- Fonte para o Set Lendário (Elmo/Peitoral/Grevas) — precisa de tabela de drop em algum chefe
  ou baú, ou da cena do Templo
- Decisão de design sobre a abertura desarmada (é intencional — a tempestade abafa o ruído
  de propósito para travessia furtiva — ou precisa de arma inicial?)

## 7. Ordem de trabalho combinada com o Vini

**Bosses primeiro, depois o Castelo.** Nesta ordem:
1. ~~Byakhee~~ — Core+Runtime prontos, **balanceamento pendente** (§5)
2. **Rei em Amarelo** — zero código ainda. Mecânica de "virar as costas" não existe em
   nenhuma forma. Design em `GDD_Mestre.md` (buscar seção do Rei em Amarelo).
3. **Castelo de Carcosa** — cena inexistente.

O **Templo do Povo Serpente** (item 14, "polimento", conteúdo opcional) foi discutido como
possível solução para a curva de poder, mas **decisão foi adiar** em favor dos bosses
obrigatórios — ele compete direto com o que sustenta a tese do Vertical Slice
("abertura + desfecho", ver `roadmap_vertical_slice.md`).

## 8. Armadilhas já pagas nesta sessão (não repetir)

1. **`GameObject.Find` só enxerga objetos ativos.** Uma ferramenta de wiring que criava telas
   desativadas e tentava achá-las de novo com `Find` duplicava a cada execução, mesmo se
   dizendo idempotente. Use `GetComponentsInChildren<Transform>(includeInactive: true)`.
2. **Recriar um `MainInventory`/`EquipmentInventory` com `new` órfã os inscritos em
   `OnSlotChanged`.** `LoadFromSaveData` mutava a instância inteira; corrigido para popular no
   lugar (`LimparTudo()` + repovoar) preservando quem já tinha assinado o evento.
3. **`SequenciaDeColapso.Awake()` desativa o próprio `painelColapso`.** Se o `CanvasGroup`
   estiver no mesmo GameObject que o componente, ele se desliga e nunca mais consegue rodar
   coroutine. O painel precisa ser um **filho**, não o mesmo objeto.
4. **`AddComponent<ColetavelDeItem>()` dispara `Awake()` antes de `Configurar()` rodar.**
   Monte o objeto **inativo**, configure, só então ative.
5. **A `Library/` não é dependência de nada** — é cache 100% regenerável
   (`.gitignore`, zero arquivos commitados). Não hesite em avaliar remoção de assets por medo
   dela; procure pelo GUID no `Assets/` e `ProjectSettings/` para saber de verdade.

## 9. Como validar qualquer mudança

```powershell
# 1. Feche o Unity Editor completamente
# 2. Rode:
Tools\run_qa_tests.ps1
# 3. Só commitar se "TESTS PASSED"
```

Para aplicar mudanças de cena/prefab sem abrir o Editor manualmente, use
`Tools/FavelaAmarela/Rodar TODO o wiring (na ordem)` (ver §3) — mas ele também exige o Editor
fechado, e produz efeitos em `Assets/Scenes/*.unity` que precisam ir para o commit.

## 10. Onde ler mais

- [roadmap_vertical_slice.md](roadmap_vertical_slice.md) — estado real de cada um dos 14 itens do edital
- [systems/index.md](systems/index.md) — catálogo completo de sistemas
- [log.md](log.md) — devlog cronológico detalhado, uma entrada por rodada de trabalho desta sessão
- `CLAUDE.md` (raiz do repo) — regras de arquitetura e preferências do Vini
