---
type: Game System
title: Dossiê — A luta contra o Rei em Amarelo
description: Tudo o que cerca o confronto final como ele existe em 2026-09-28 — caminho até o Trono, arena, fases, números, falas, arquitetura, testes, arte, histórico, o veredito do playtest e as restrições para o redesenho.
tags: [boss, rei-em-amarelo, castelo, final, dossie, redesenho]
---

# Dossiê — A luta contra o Rei em Amarelo

> **Estado em 2026-09-28:** jogável de ponta a ponta, vencível, coberto por testes — e
> **reprovado no playtest** pelo Vini em 2026-09-10: *"Eu detestei a luta contra o Rei, está
> muito repetitiva."* Item 12 do roadmap, ⚠️. Decisão dele: **não mexer até repensar a luta.**
>
> Este documento é a fotografia completa do que existe, para o redesenho partir de fatos. O
> código é a fonte da verdade de *como funciona*; onde este texto e o código divergirem, vale o
> código. Complementa [boss_rei_em_amarelo.md](boss_rei_em_amarelo.md), que tem trechos
> desatualizados (ver §13).

---

## 1. Em uma tela

| | |
|---|---|
| **Onde** | `Castelo_Carcosa.unity`, zona Z5 — o Trono de Aldebaran. Última cena do Vertical Slice. |
| **O que o jogador precisa trazer** | 3 relíquias **equipadas** nos slots de Artefato: Necronomicon, Patuá das Luas Gêmeas, Anel do Sinal Amarelo. |
| **Estrutura** | Ritual (sem pressão) → **3 ciclos de selamento** (sobreviver dentro do escudo) → **Confronto** (mesmo ciclo, mas o Rei sangra entre os desvelos) → Selado. |
| **Condição de vitória** | Vitalidade do Rei (1000) a zero, só possível no Confronto e só na calmaria. |
| **Condição de derrota** | Estar fora do escudo aceso quando o Rei se desvela → Colapso instantâneo. |
| **Duração típica** | ~45–60 s de luta, com **5 a 8 repetições da mesma ação**. |
| **Problema central** | É um laço só — *corre para o clarão, espera, sobrevive* — repetido. As três primeiras voltas não pedem nada além de andar até o altar que acendeu. |
| **Restrição que decide o desenho** | O Rei **não tem animação de ataque**. Cinco clipes: idle, selar, desvelo, dano, queda. |

---

## 2. O caminho até o Trono

A luta final é o fecho de uma cadeia que atravessa o jogo inteiro. Cada relíquia vem de um lugar:

| Relíquia (id) | De onde vem | Observação |
|---|---|---|
| **Necronomicon** (`necronomicon`) | Tumba de Alhazred — cai quando o **Abdul é derrotado** em combate | ⚠️ ver abaixo |
| **Patuá das Luas Gêmeas** (`patua_luas_gemeas`) | Santuário de Yhtill — quest "A Canção Incompleta" da Cassilda | Entregue ao concluir a quest |
| **Anel do Sinal Amarelo** (`anel_sinal_amarelo`) | Portões das Ruínas — **drop garantido do Byakhee** (`Drop_Byakhee`, grau 3) | Esteve desligado até 2026-08-19 (guarda `ReliquiasDoRitoTests`) |
| ~~Coroa de Ossos~~ | Seria drop do Nagaraja, no Templo da Serpente | **Sem fonte jogável** — o Templo não tem cena. Por isso o rito exige 3, não as 4 do design. |

**A relíquia tem de estar equipada, não só na mochila.** O `PontoFocalDeReliquia` checa
`ArtefatosBridge.Inventario.Contem(id)`; se ela estiver só na mochila, o altar responde
*"{nome} dorme na tua mochila. O ponto focal só responde ao que Damião traz em mãos."*

### ⚠️ Poupar o Abdul fecha o final (achado em 2026-09-28, ao montar este dossiê)

No `AbdulAlhazredAI`, o Necronomicon só é instanciado em `HandleDerrotado`. Escolher
**concordar** na conversa (`ResolverEscolha` → `_poupado = true`) liberta o Yug-Neth e **não
entrega o tomo** — o que o `companheiro_mi_go.md` e o GDD registram como intencional (*"poupa
Abdul, sem Necronomicon"*). Mas o `ReiEmAmareloAI` exige `necronomicon`. Consequência: **quem
faz a escolha pacífica chega ao Trono sem poder completar o rito.** A única saída no código é a
"traição da trégua" (`IniciarLuta` ainda aceita o Abdul em `Transe`), que exige voltar à Tumba e
atacá-lo — **não verifiquei se algo no jogo diz isso ao jogador.** É uma decisão de design do
Vini: ou o rito aceita outra relíquia no lugar, ou a escolha pacífica precisa custar outra coisa,
ou o jogo avisa.

### Nível e arma ao chegar

Somada a Exposição das cenas (11 Cultistas × 25, Abdul 150, 2 Cultistas da Tumba × 25, Byakhee
200 = **675**), o jogador chega ao Trono no **nível 4**. Armas de referência usadas na
calibração: Alfanje das Ruínas Pálidas do baú (o piso) e um Alfanje T2 (o teto).

---

## 3. A arena — Z5, o Trono de Aldebaran

| | |
|---|---|
| Forma | Losango isométrico de **30 × 15 un** (Tilemap), fechado pelo Tilemap `Colisao` |
| Câmera | Ortográfica, mostra **20 × 11,25 un** — a sala é mais larga que a tela |
| Pontos focais | 3 altares, cada um a ~11 un do trono; os das pontas a **20 un um do outro** — de um não se vê o outro |
| Arte dos altares | Base `Ruin_3` (CraftPix Undead) + feixe amarelo de 10 quadros (`AnimadorDeAltarDeReliquia`), desde 2026-09-03 |
| Escudos | `Escudo_Magico` do Abdul (12 quadros, 64 × 96 px, alpha médio 20%) à escala 2,5 → cúpula de 5 × 7,5 un, camada `Frente` |
| Rei | Prefab `ReiEmAmarelo.prefab`, **escala ≈ 3,7 na cena → 15,2 un de altura** (escolha do Vini, 2026-09-10: *"Eu quis o Rei com uma escala maior, mesmo"*). Pés dentro da sala, cabeça acima da parede do fundo (61 % do corpo dentro). |
| Antes da luta | Refúgio na Z1 do Castelo — é para lá que o Colapso devolve, se o jogador descansou nele |

**Por que a cúpula é alta:** como a sala tem 30 un e a câmera 20, um terço dos ciclos começaria
com o abrigo fora da tela. Os 7,5 un de cúpula aparecem por cima da borda do quadro antes do
altar, e o Rei **anuncia pelo nome** qual relíquia acendeu (§6).

---

## 4. A luta, fase por fase

A máquina é a `ReiEmAmareloFSM` (Core, POCO). Estados: `Aguardando → AtivandoReliquias →
Selando ⇄ Desvelado → … → Selado` (vitória) ou `Colapso` (derrota).

### Fase 0 — Ritual das relíquias (sem relógio)

- O rito começa sozinho no `Start` do Rei (`IniciarRitual` → `AtivandoReliquias`). Até
  2026-09-02 só o Carcosa Debugger chamava isso, e os altares recusavam toda relíquia em
  silêncio.
- O jogador vai a cada altar e aperta **E**. O altar ativa, o feixe acende, o Rei toca o clipe
  `dano` (recua) e a caixa diz *"O ponto focal desperta. Ainda faltam N."*
- **Não há pressão nenhuma** — o `Tick` nem avança neste estado.
- A ordem em que o jogador ativa os altares **define a ordem dos escudos** na fase seguinte.

### Fases 1–3 — Selamento (os três ciclos obrigatórios)

Cada ciclo:

1. **Selando (calmaria), 6 s.** No **primeiro quadro** da calmaria, o escudo da relíquia do ciclo
   acende e os outros apagam. O jogador tem os 6 s para correr até ele. A travessia mais longa
   (20 un) leva 4,44 s andando — cabe.
2. **Desvelado, janela de 1,5 s.** O Rei fica vermelho, toca `desvelo` e o som de pânico.
   **Estar dentro do escudo em qualquer momento da janela salva** — não precisa se manter.
   Fora dele ao fim de 1,5 s → Colapso.
3. Sobreviveu → *"O selo aperta. Ainda faltam N."* → próximo ciclo.

Três relíquias, três ciclos: cada artefato abriga exatamente uma vez.

### Fase 4 — Confronto (a quarta fase, desde 2026-09-10)

Sobrevivido o terceiro ciclo, o rito **não sela: desmascara.** `EmConfronto = true`, a barra de
vida do Rei é ligada, e a caixa diz por 5 s: *"A Máscara Pálida cai. Ele sangra entre os
desvelos — fere-o, e volta ao clarão antes que ele te encare."*

A partir daí o ciclo continua igual (os escudos seguem se revezando, dando a volta na lista), com
duas diferenças:

- a calmaria cai de 6 para **5 s**;
- durante a calmaria **o Rei pode ser ferido** (`PodeReceberDano = EmConfronto && Selando`).
  Durante o desvelo ele é imune — quem está batendo nele está fora do abrigo, e o rito já cobra
  isso.

O jogador tem de sair do escudo, correr até o Rei, bater, e voltar ao escudo aceso antes do
desvelo. Golpe recusado não faz som nem mostra número: silêncio é a informação de que nada
entrou.

### Fim

| | |
|---|---|
| **Vitória** | Vitalidade zero → `Abater()` → `Selado` → Rei toca `queda`, escudos apagam, cai o espólio, `SequenciaDeSelamento` escurece a tela, escreve *"O Sinal se fecha. Carcosa cala — por ora."* (linha **provisória**) e volta ao Menu após 5 s. |
| **Derrota** | Fora do escudo no desvelo → `Colapso` → `ResilienciaBridge.ForcarColapso()` (mesmo mecanismo do `ColapsoTrigger` e da Coisa do Cemitério) → tela de Colapso → renascer no último Refúgio. O Rei fica em `idle`, de pé. |

**Espólio do Rei** (`Drop_ReiEmAmarelo`, todos garantidos, grau 2): Alfanje do Rei, Maça do Sinal
Amarelo, Estilete da Máscara Pálida. ⚠️ A `SequenciaDeSelamento` leva ao Menu 5 s depois — **o
jogador nunca usa essas armas.** Ou o espólio sai, ou o desfecho deixa o jogador de pé no Trono.

---

## 5. Os números

| Parâmetro | Valor | Onde vive |
|---|---|---|
| Relíquias exigidas | 3 (`necronomicon`, `patua_luas_gemeas`, `anel_sinal_amarelo`) | `ReiEmAmareloAI.idsDasReliquiasExigidas` (prefab) |
| Ciclos de selamento | **3** | `ciclosDeSelamento` (prefab) |
| Calmaria do selamento | **6 s** | `intervaloEntreCiclos` (prefab) |
| Calmaria do Confronto | **5 s** | `intervaloNoConfronto` (prefab) |
| Janela do desvelo | **1,5 s** — único número que o design doc especifica | `duracaoDaJanela` (prefab) |
| Vitalidade | **1000** | `Ficha_Rei.asset` |
| Defesa / Res. Anômala | **12 / 30** | `Ficha_Rei.asset` |
| Ataque | **0** — afirmação, não omissão: o Rei não golpeia | `Ficha_Rei.asset`; `OConfrontoDoReiTests` recusa Ataque > 0 |
| Abrigo | elipse **4 × 2 un** (semieixos 2 × 1), igual à bolha desenhada | `AbrigoDeReliquia`, `EscudoDeReliquia` |
| Velocidade do Damião | 4,5 un/s andando, 7,5 correndo | usada nos guardas de geometria |

### A calibração do Confronto (2026-09-10)

| | |
|---|---|
| Alfanje do baú, nível 2 (piso) | 63 brutos → 51 após Defesa → **20 golpes → 7 ciclos** a 3 golpes/ciclo |
| Alfanje T2, nível 4 (teto) | 128 brutos → 116 → **8,6 golpes** |
| Ida e volta do abrigo mais próximo, correndo | 2,4 s (Anel) · 2,56 s (Necronomicon, Patuá) |
| Golpes por ciclo a 5 s | até 5 |

A 6 s a T2 matava o Rei num ciclo só; por isso a calmaria do Confronto é 5. O guarda que media
isso (`ComUmaArmaT2NoNivelDeChegada_ORei_NaoCaiNumCicloSo`) está **pausado** (`Assert.Ignore`
com a razão) desde que a luta foi parada — no nível 4 ele reprova: a calmaria comporta 9 golpes
e a T2 precisa de 8,6.

### Quanto a luta se repete

```
Ritual         3 altares, sem relógio                      ~10–20 s
Selamento      3 × (6 s de calmaria + desvelo)             ~20 s    ← 3 repetições obrigatórias
Confronto      N × (5 s de calmaria + desvelo)             ~12–35 s ← 2 a 5 repetições
                 T2 bem jogado: 2–3 ciclos
                 arma do baú:   4–7 ciclos
───────────────────────────────────────────────────────────────────
Total          5 a 8 repetições da mesma ação, ~45–60 s de relógio
```

**Nenhuma repetição muda a pergunta.** O ciclo 1 ensina o abrigo; os ciclos 2 e 3 repetem a
lição; o Confronto acrescenta "bata nele" mas mantém o mesmo relógio. É o que a memória de
trabalho registrou como *laço repetido não é luta*.

---

## 6. O que o jogador vê e ouve

### Falas (caixa de diálogo do HUD, `TutorialHintUI`)

| Quando | Texto |
|---|---|
| Altar sem a relíquia | *"O ponto focal não responde. {nome} não está contigo."* |
| Relíquia na mochila, não equipada | *"{nome} dorme na tua mochila. O ponto focal só responde ao que Damião traz em mãos."* |
| Altar ativado | *"O ponto focal desperta. Ainda faltam N."* / *"O último ponto focal desperta. O rito de selamento começa."* |
| Primeiro escudo acende | *"{nome} ergue um clarão. Não o encares — abriga-te dentro antes que ele se desvele."* (4 s) |
| Escudos seguintes | *"{nome} arde. Corre para o clarão."* (4 s) |
| Ciclo sobrevivido (selamento) | *"O selo aperta. Ainda faltam N."* |
| Começa o Confronto | *"A Máscara Pálida cai. Ele sangra entre os desvelos — fere-o, e volta ao clarão antes que ele te encare."* (5 s) |
| Vitória | *"O Sinal se fecha. Carcosa cala — por ora."* — **provisória**, serializada no Inspector |

### Sinais

| Sinal | Estado |
|---|---|
| Cor do Rei | roxo no ritual, amarelo na calmaria, **vermelho no desvelo**, pálido quando selado — **cores provisórias**, tingimento sobre a arte |
| Animação | `selar` na calmaria, `desvelo` no desvelo, `dano` ao ativar altar ou ser ferido, `queda` ao ser selado |
| Som | `SomDoJogo.EntrouEmPanico` no começo de cada desvelo — o único sinal que atravessa "não olhe para ele" |
| Escudo | cúpula translúcida animada, uma acesa por vez, desenhada por cima dos atores |
| Barra de vida | `BarraDeVidaFlutuante` (a do Yug-Neth), ligada no Confronto; aparece quando o Rei é ferido e some cheia |

### ⚠️ O aviso do Confronto não chega

Em 2026-09-10, depois de jogar a luta, o Vini disse: *"Ah tá, eu não tinha entendido que você já
tinha implementado que dava para bater nele."* A mudança de regra mais importante da luta é
comunicada por **uma frase de 5 s** e por uma barra que só aparece depois do primeiro golpe —
numa luta que passou três ciclos ensinando o jogador a **correr para longe do Rei**. A
vulnerabilidade precisa ser visível **no corpo dele** (a máscara caindo no sprite, um clarão nele
durante a calmaria, uma mudança de pose), não num texto.

---

## 7. Arquitetura

```
PontoFocalDeReliquia (×3, IInteragivel) ──AtivarReliquia──▶ ReiEmAmareloAI ──▶ ReiEmAmareloFSM (Core)
        │ irmão                                               │  Update: Tick(dt, abrigado)
        ▼                                                     │  IDanificavel: ReceberGolpe → Vitalidade
EscudoDeReliquia (×3) ◀──Acender/Apagar (OnEscudoAceso)───────┤
        │ Protege(posição) ── AbrigoDeReliquia (Core) ─────────┘
        ▼
  OnColapso ──▶ ResilienciaBridge.ForcarColapso ──▶ SequenciaDeColapso ──▶ Refúgio
  OnSelado  ──▶ OnVitoria ──▶ SequenciaDeSelamento ──▶ Menu
            └─▶ OnAbatido ──▶ DropAoAbater (Drop_ReiEmAmarelo)
```

| Peça | Camada | Papel |
|---|---|---|
| `Core/Enemies/ReiEmAmareloFSM.cs` | POCO | Toda a regra: ritual, ciclos, `ReliquiaDoCiclo`, `EmConfronto`, `PodeReceberDano`, `Abater()`. Eventos: `OnReliquiaAtivada`, `OnEscudoAceso`, `OnComecouADesvelar`, `OnCicloSobrevivido`, `OnComecouOConfronto`, `OnSelado`, `OnColapso`. |
| `Core/Enemies/AbrigoDeReliquia.cs` | POCO | Geometria da elipse: `EstaAbrigado`, `SegundosParaAlcancar`. |
| `Core/.../DetectorDeCostas.cs` | POCO | Mecânica antiga ("dê as costas"). **Aposentado do rito** em 2026-09-10; mantido com 7 testes. |
| `Enemies/ReiEmAmareloAI.cs` | Runtime | Adaptador. **Sem `EnemyBase`** (como o Abdul): implementa `IDanificavel` e `IFonteDeEspolio` direto — um `EnemyBase` seria uma segunda fonte de espólio. Ficha, `Vitalidade`, `Hurtbox.GarantirPara`, falas, cores, som, animação. |
| `Itens/PontoFocalDeReliquia.cs` | Runtime | O altar: checa posse **equipada**, ativa, acende o feixe, fala. |
| `Itens/EscudoDeReliquia.cs` | Runtime | A cúpula: acende, apaga, responde "está dentro?" lendo o id do altar irmão (sem cópia própria). |
| `GameLoop/SequenciaDeSelamento.cs` | Runtime | O desfecho: escurece, linha final, volta ao Menu. |
| `ReiEmAmarelo.prefab` | asset | Parâmetros da luta (§5), ficha, barra, `ReiEmAmarelo_AC`. |
| `Ficha_Rei.asset`, `Drop_ReiEmAmarelo.asset` | asset | Carne e espólio. |

---

## 8. Testes

| Arquivo | Testes | O que guarda |
|---|---|---|
| `ReiEmAmareloFSMTests` | 26 | Toda a máquina: ritual, ordem dos escudos, janela, Confronto, imunidade no desvelo, `Abater` |
| `AbrigoDeReliquiaTests` | 10 | A elipse e o tempo de alcance |
| `DetectorDeCostasTests` | 7 | A mecânica aposentada |
| `OTronoCabeNaSalaTests` | 7 | Rei com chão sob os pés, ≥ 50 % dentro da sala, altura < duas telas, cada relíquia com abrigo, abrigo sobre chão pintado, **travessia mais longa cabe na calmaria** |
| `OConfrontoDoReiTests` | 7 | Carne (ficha, Ataque 0), geometria abrigo→Rei→abrigo, duração com piso e teto (1 pausado), nível de chegada |
| `AnimacaoDoReiEmAmareloTests` | 6 | Os 5 clipes e o controller |
| `CasteloDeCarcosaTests` | 7 | Sistemas do Castelo em cena; **vencer o Rei tem consequência** |
| `ReliquiasDoRitoTests` | 3 | Toda relíquia exigida tem fonte no jogo (o Anel no Byakhee) |
| `AltaresRespondemNaTelaTests` | 2 | Ativar um altar muda a imagem |
| `OAltarResponde` (PlayMode) | 3 | O altar aceita a relíquia equipada, recusa a da mochila |
| `PosseDeArtefatosTests` | 12 | Equipado × possuído |

Ferramentas de Editor que montam a luta: `Trono: montar os abrigos das relíquias`,
`Trono: dar carne ao Rei`, `Montar Prefab do Rei em Amarelo`, e o **Carcosa Debugger**
(concede as relíquias, invoca o Rei, mostra a FSM ao vivo). A `Cena_ArenaDeTestes` existe fora do
Build Settings para testar o Rei isolado.

---

## 9. Arte e licença

- **Sprites:** "Moonstone Keeper – Eldermoon Grove", por **Sucart** (sucart.itch.io). Cinco folhas
  de quadros 88 × 129 px — **idle, selar, desvelo, dano, queda** — e o `ReiEmAmarelo_AC` que as
  toca. O Animator **não tem teia de transições**: quem manda é a FSM (`animator.Play` por
  estado), para não haver uma segunda máquina de estados.
- **O que falta no pacote:** a página lista também *Walk, Run, Jump, Dodge (WIP)* e **ATTACK
  (WIP)**. Na captura de 2026-09-10 o ataque não estava publicado. **Checar de novo antes do
  redesenho** — se saiu, a luta ganha um golpe sem trocar de arte.
- **Licença:** *"You are free to use this asset pack in any project that you make, as long as you
  credit me using the name Sucart."* Crédito presente na tela de Créditos desde 2026-09-10,
  guardado por `CreditosTests`. Texto integral em `Art/Enemies/ReiEmAmarelo/LICENCA_Sucart.txt`.
- **Pendências de arte:** as cores de estado são tingimento provisório; a Máscara Pálida não
  existe como elemento visual separado.

---

## 10. Histórico

| Data | O que aconteceu |
|---|---|
| 2026-08-11/12 | Core e Runtime escritos. Design original: sem barra de vida, 4 relíquias, **dar as costas** ao Rei em 1,5 s (`DetectorDeCostas`). Coroa de Ossos sem fonte → rito com 3. Carcosa Debugger e Arena de Testes criados. |
| 2026-08-12 | Prefab com sprite emprestado ("Necromancer" da Inbox). |
| 2026-08-19 | Castelo existe; Z5 em cena. Atalho Santuário→Castelo removido (pulava o Byakhee, fonte do Anel). `Drop_Byakhee` ligado — antes o Anel não caía e o rito era impossível. |
| 2026-08-20 | `OnVitoria` tinha **zero assinantes**; ligado à `SequenciaDeSelamento`. |
| 2026-09-02 | Relato: *"os altares não estão funcionando"*. O rito nunca começava (`IniciarRitual` só no Debugger). Passou para o `Start`. Som do desvelo ligado (o evento também tinha zero assinantes). |
| 2026-09-03 | Altares vestidos (feixe amarelo). Arte do Sucart entra. |
| 2026-09-10 | Relato: *"Não tem como evitar o ataque do Rei, nem de costas."* `LookDirection` só atualizava andando → quem parava para ler o aviso morria. Mecânica trocada pelo **abrigo** (pedido do Vini: *"cada artefato gera um escudo por vez"*). Rei devolvido para dentro da sala (override de escala 2,9041). |
| 2026-09-10 | Pedido do Vini: *"depois dos três escudos, abre-se uma fase de combate"* → **Confronto**, Rei com ficha e Vitalidade, calmaria de 5 s. |
| 2026-09-10 | Playtest: *"detestei, está muito repetitiva"*. Item 12 volta a ⚠️. Luta parada. |
| 2026-09-10 | Vini aumenta o Rei para escala ≈ 3,7 de propósito; guardas passam a medir a decisão dele. Contado: 5–8 repetições por luta. *"Porra, quantas vezes essa luta se repete? Isso tá muito chato."* Depois: *"eu não tinha entendido que dava para bater nele."* |
| 2026-09-28 | Este dossiê. Achado: poupar o Abdul deixa o jogador sem Necronomicon. |

---

## 11. Design original × implementado

| Design (`level_design_castelo_carcosa.md` §Z5) | Implementado |
|---|---|
| Não há barra de vida | Há, desde o Confronto (1000) |
| 4 relíquias (Anel, Coroa, Patuá, Necronomicon) | 3 — a Coroa não tem fonte |
| Dar as costas ao Rei em 1,5 s | Estar dentro do escudo aceso em 1,5 s |
| UI emite "Pressão Psíquica Extrema" no desvelo | Cor vermelha + som de pânico; sem efeito de UI de pressão |
| O Rei "não pode ser enfrentado fisicamente" (bestiário) | Pode, no Confronto |
| Varanda sem bordas para o espaço, luas gêmeas no chão | Losango de pedra com parede; sem luas refletidas |

---

## 12. Problemas conhecidos, por gravidade

1. **Repetitiva** — a mesma ação 5 a 8 vezes, ~50 s. A crítica do playtest. Design, não bug.
2. **Poupar o Abdul fecha o final** — sem Necronomicon, o rito não completa (§2).
3. **A regra do Confronto não é percebida** — uma frase de 5 s e uma barra que surge tarde (§6).
4. **Espólio inútil** — três armas garantidas e o jogo volta ao Menu 5 s depois (§4).
5. **Ciclos 2 e 3 do selamento são a mesma pergunta que o 1** — só muda o altar.
6. **Cores de estado provisórias** e **linha do desfecho provisória**.
7. **Coroa de Ossos sem fonte** — por escopo (Templo fora do VS), não por defeito.
8. **Guarda de balanceamento pausado** — volta quando a luta for refeita.

---

## 13. Divergências com `boss_rei_em_amarelo.md`

Aquele documento ainda diz: *"Falta prefab, arte, o Trono de Aldebaran em cena de verdade"* e
*"o Castelo (item 11) ainda não existe"* — tudo isso existe desde agosto. A descrição do
frontmatter ainda fala em "sem barra de vida". A seção "Sobreviver todos os ciclos … sela o Rei"
descreve a versão anterior ao Confronto. Os números e a arquitetura das seções novas estão certos.

---

## 14. Para o redesenho: o que está fixo e o que é alavanca

### Restrições reais

- **Sem clipe de ataque.** Qualquer luta em que o Rei agride fisicamente depende do *Attack (WIP)*
  do Sucart, de arte nova, ou de o ataque ser um **efeito** (projétil, área no chão, onda) e não
  um movimento do corpo.
- **A sala é mais larga que a câmera** (30 × 20). Tudo que o jogador precisa ver precisa aparecer
  por cima da borda ou ser anunciado.
- **O prazo é de edital** e o Rei é metade da tese do VS — não é candidato a corte.
- **A skill `favela-lore-enforcer`** vale para toda fala nova.

### O que já está pronto para ser reaproveitado

A FSM com fases separáveis (`EmConfronto`, `PodeReceberDano`, `Abater`), o Rei como
`IDanificavel` com ficha e barra, três abrigos independentes que acendem e apagam por evento, a
geometria da sala medida e guardada, falas por relíquia, o som de desvelo, o Carcosa Debugger.

### Alavancas baratas (números, sem código)

| Mudança | De → para | Efeito |
|---|---|---|
| `ciclosDeSelamento` | 3 → **1** | um ciclo ensina o abrigo; o Confronto abre logo |
| Vitalidade (`Ficha_Rei`) | 1000 → **400** | 3–4 golpes: o Rei cai no 1º ou 2º ciclo do Confronto |
| Resultado | | **2 a 3 laços, ~20 s** — curta, não boa |

### Perguntas que mudam o desenho (decisões do Vini)

A lição do playtest é que **escalar precisa mudar a pergunta feita ao jogador**, não só o relógio
ou a quantidade. Direções possíveis, sem ordem de preferência:

- **Cada relíquia muda a regra do ciclo em vez de só mudar o altar** — o escudo do Patuá, do Anel
  e do Necronomicon com comportamentos diferentes (um que se move, um que encolhe, um que exige
  ficar de costas *dentro* dele).
- **Dois escudos acesos, um falso** — escolha em vez de corrida.
- **O Rei como terreno** — o desvelo varre a sala em faixas ou ondas, e o abrigo é onde a onda
  não passa, mudando a cada ciclo.
- **A vulnerabilidade no corpo** — o Confronto começa com a máscara caindo no sprite e o Rei
  mudando de pose, e ele só sangra num ponto específico (a máscara no chão, um altar que
  "segura" o Rei enquanto o jogador bate).
- **Menos Rei, mais Castelo** — a luta como o fim de uma subida pelas zonas, com os ciclos
  espalhados pelo caminho.

Qualquer uma delas precisa responder primeiro: *quantas vezes o jogador faz a mesma ação antes de
algo mudar?* Se a resposta for "todas", é metrônomo de novo.

---

## Relacionados

- [boss_rei_em_amarelo.md](boss_rei_em_amarelo.md) — documento de sistema (ver §13 para o que está desatualizado)
- [level_design_castelo_carcosa.md](level_design_castelo_carcosa.md) — o design original da Z5
- [artefatos.md](artefatos.md) — slots de Artefato, equipado × possuído
- [reliquias_de_hali.md](reliquias_de_hali.md) e [../lore/reliquias_cosmicas.md](../lore/reliquias_cosmicas.md) — as relíquias
- [companheiro_mi_go.md](companheiro_mi_go.md) — a escolha do Abdul e o Necronomicon
- [boss_byakhee.md](boss_byakhee.md) — o outro chefe do VS; fonte do Anel
- [../roadmap_vertical_slice.md](../roadmap_vertical_slice.md) — item 12
