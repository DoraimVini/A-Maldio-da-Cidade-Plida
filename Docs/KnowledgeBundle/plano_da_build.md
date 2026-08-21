---
type: Build Plan
title: Plano da build do Vertical Slice
description: O que falta para gerar o executável entregável, em ordem de risco
status: em execução
data: 2026-08-20
---

# Plano da build — Vertical Slice

> **Prazo: a build sai amanhã (2026-08-21), decisão do Vini.**
> Este documento é a lista de verificação, não um relatório. Ordem = risco decrescente.

## Estado de partida (medido em 2026-08-20)

As **6 cenas do caminho** estão no Build Settings e ligadas ponta a ponta:

```
Cena_Menu → Deserto_Hali ⇄ Playtest_RuinasPalidas
                         ⇄ Santuario_Yhtill
                         → Portoes_Das_Ruinas → Castelo_Carcosa
```

`Cena_Menu` está no índice 0, que é o correto — a build arranca no menu.
`Cena_ArenaDeTestes` segue fora, de propósito.

Os 14 itens do VS estão fechados. **O que separa isto de uma build entregue é playtest**, mais
os itens abaixo.

---

## Bloco 1 — Identidade da build (barato, alto constrangimento se sair errado)

| # | item | estado medido | por que importa |
|---|---|---|---|
| 1.1 | `productName` | **`"A Maldição da Cidade Pálida"`** | ❌ **Errado.** O título oficial visível ao jogador é **"Caminho para Carcosa"** (decisão do Vini, 2026-08-11, registrada no topo do `CLAUDE.md`). O nome atual é o do **repositório**, que ficou por razões históricas. Hoje a **janela do jogo e o nome do executável** sairiam com o título errado — num envio de edital. |
| 1.2 | `companyName` | **`DefaultCompany`** | ❌ Vai para o caminho de dados (`%APPDATA%/DefaultCompany/...`) e para as propriedades do `.exe`. Lê como projeto não configurado. |
| 1.3 | Ícone | **nenhum definido** | ❌ O executável sai com o ícone padrão da Unity. |
| 1.4 | `bundleVersion` | `1.0` | ⚠️ Decidir se a entrega é `1.0` ou `0.1` — é um Vertical Slice, não um lançamento. |

Resolução: `Tools/FavelaAmarela/Build: preparar identidade` (a criar) ou à mão no Player
Settings. **Exige a Unity fechada** se for por ferramenta em batch.

---

## Bloco 2 — Correções que já estão em código e faltam aplicar

Tudo abaixo está **escrito e commitável**, mas depende de rodar ferramenta de Editor com a
Unity fechada. Nenhuma foi aplicada ainda.

| # | ferramenta | o que conserta |
|---|---|---|
| 2.1 | `Tools/FavelaAmarela/Colisores: revisar as pegadas` | **O Byakhee não tem colisor nenhum** — o chefe é impossível de acertar (`OverlapCircle` não encontra nada). É a causa do "o Damião não causou dano na Byakhee". **Bloqueia a build**: o VS termina num chefe invencível. Também normaliza as pegadas do elenco (Damião ia a 1,467 contra 0,576 do Cultista). |
| 2.2 | `Tools/FavelaAmarela/Áudio: ligar o som do combate` | Golpe e habilidade de Damião eram mudos, e o Byakhee estava sem `AudioDeCombate`. Metade do "combate sem feel". |
| 2.3 | `Tools/FavelaAmarela/Montar Animação do Cultista` | Aplica a escala corrigida (Cultista e Damião com a mesma altura de figura) e o pivô na linha do chão. |

---

## Bloco 3 — Playtest de ponta a ponta

**Ninguém jogou o caminho crítico inteiro ainda.** É o maior risco não coberto por teste
automatizado, e nenhuma suíte substitui.

Roteiro mínimo:

1. Menu → Deserto. Damião anda, faz barulho, é caçado por Cultista.
2. Tumba (`Playtest_RuinasPalidas`): pegar arma, resolver Abdul, **libertar Yug-Neth**.
3. Voltar ao Deserto. Yug-Neth segue. Santuário: quest da Cassilda, Patuá.
4. Portões: o portal **exige a Tumba resolvida** (trava nova) — conferir que a linha de recusa
   aparece se tentar antes.
5. Arena do Byakhee: a luta começa por gatilho, **a saída tranca**, o chefe é acertável,
   morre, destranca os Portões e acende o Poste.
6. Interagir no portão → Castelo. Yug-Neth entra e **vira NPC de artesanato**.
7. Castelo: Z1→Z2→Z3→Z5. Rito das 3 relíquias. Selar o Rei → `SequenciaDeSelamento`.

Cada passo que falhar volta como item aqui.

---

## Bloco 4 — Textos provisórios escritos por mim

Duas falas visíveis ao jogador estão com **placeholder que eu escrevi** e precisam do Vini:

| onde | campo | situação |
|---|---|---|
| `SequenciaDeSelamento` | `linhaDoDesfecho` | A **última fala do jogo**. Provisória. |
| `Entrada_DosPortoes` (Deserto) | `linhaSeTrancado` | Fala de recusa do portal trancado. Provisória. |

Ambas são `[SerializeField]` — dá para trocar no Inspector sem tocar em código.

---

## Bloco 5 — Dívida conhecida que **não** bloqueia a build

Registrado para não ser redescoberto como surpresa:

- **Arte adiada por decisão do Vini (2026-08-20).** Ícones de armadura, sprite do Byakhee
  (o arquivo novo é gerado por IA e **não tem canal alpha** — o xadrez está pintado em pixels),
  Cassilda e fragmentos com placeholder, Rei em Amarelo com sprite emprestado.
- **Hitbox/hurtbox não existem.** Cada personagem tem **um** colisor fazendo três trabalhos.
  As camadas para separar já estão declaradas no projeto e **não são usadas por nada**:
  `PlayerHitbox` (11), `EnemyHitbox` (12), `PlayerHurtbox` (13), `EnemyHurtbox` (14).
  É a próxima melhoria real de combate, e é refatoração de pipeline de dano.
- **Golpear não emite ruído de stealth.** Só `PlayerMovement` chama `SoundBroadcastService.Emitir`,
  então dar uma espadada não atrai ninguém num jogo cuja percepção é 100% sonora. Ligar isso
  muda o equilíbrio da furtividade — é decisão de design, não conserto.
- **`AudioDeCombate` só existe para quem tem `EnemyBase`** (Byakhee e Cultista). Abdul usa
  `IDanificavel` sem `EnemyBase`; Espectro, Esqueleto e a Coisa têm caminhos próprios. Eles
  seguem mudos no combate.
- **`ItemRecolhido` e `ArtefatoInvocado`** continuam sem disparo: não há evento de "peguei do
  chão" nem de "invoquei" para assinar. Exige evento novo.
- **Coroa de Ossos sem fonte jogável** — não é exigida pelo rito (o Rei pede 3 relíquias),
  só pelo Set Lendário 4/4, que abre a Z4 opcional, fora do VS.
