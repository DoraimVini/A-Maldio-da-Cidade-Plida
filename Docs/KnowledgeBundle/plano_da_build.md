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
Cena_Menu → Deserto_Hali ⇄ Tumba_De_Alhazred
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
2. Tumba (`Tumba_De_Alhazred`): pegar arma, resolver Abdul, **libertar Yug-Neth**.
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

---

## Bloco 6 — Pós-build: o HUD deve ser **persistente**, não por cena

**Não fazer antes do build de hoje sair.** É refatoração de arquitetura.

### O problema, encontrado em 2026-08-21

`BuildHUDCompleto.ObterOuCriarCanvas()` acha **qualquer** `HUDController` na cena e usa o
`GameObject` dele como raiz — sem checar se aquele objeto é a raiz certa. Na prática o
`HUDController` costuma estar dentro de **`HUD_ResilienciaBar.prefab`**, um prefab **nomeado por
uma única barra**, e as outras nove peças (Vitalidade, Vigor, Companheiro, Ações, Artefatos,
Itens, Painel de Inventário, Ficha, caixa de diálogo) são penduradas nele em tempo de Editor via
C# — `new GameObject`, `AddComponent`, matemática de `RectTransform` na mão. Em cena onde nada é
encontrado, a ferramenta cria uma raiz **diferente** (`HUD_Gameplay`, solta, não-prefab).

Ou seja: **o HUD não tem uma forma única.** Tem duas, dependendo do histórico da cena. Essa
ambiguidade causou diretamente um falso alarme grave nesta sessão — uma verificação por regex
leu uma referência de prefab válida (`stripped`, apontando para dentro do
`HUD_ResilienciaBar.prefab`) como corrompida, porque não havia forma canônica contra a qual
comparar.

### A direção certa (e por que mudou desde o primeiro rascunho)

O primeiro rascunho deste bloco propunha "um prefab autoral instanciado em cada cena". Está
**superado**. O problema de fundo não é o prefab — é o HUD ser **por cena**.

O modo de falha mais repetido deste projeto é *"N lugares ficaram fora de sincronia"*. Já foram
encontradas **seis** listas de cenas escritas à mão que envelheceram (`BuildHUDCompleto`,
`HudCompletoTests`, `PadronizarCanvasDasCenas`, `LigarSistemasNovos`, `BootstrapDeCenaTests`, e
a do próprio `CenasNaoFicamParaTrasTests`, que existe só para pegar as outras). Enquanto o HUD
for por cena, ele **continua sendo mais uma dessas listas**, mesmo virando prefab: instância por
cena aceita *override* por instância, e um ajuste feito numa cena diverge das outras quatro em
silêncio.

**O HUD não muda entre cenas.** Ele não tem por que nascer e morrer cinco vezes.

### O padrão já existe neste projeto — não é arquitetura nova

`CLAUDE.md` §2 manda seguir exemplo canônico em vez de inventar padrão. Este já está
implementado e funcionando aqui:

| | como nasce |
|---|---|
| `InventoryManager` | `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` → `Resources.Load<GameObject>("InventoryManager")` → `DontDestroyOnLoad` + guarda de singleton |
| `GerenciadorDeSave` | mesmo, criando o `GameObject` em código |
| `ProgressionBridge` | mesmo (ver `CLAUDE.md` §1) |

O HUD passa a ser o quarto: **`Assets/FavelaAmarela/Resources/HUD_Gameplay.prefab`**, contendo o
`Canvas`, `CanvasScaler` (1920×1080, match 0.5), `GraphicRaycaster`, `HUDController` e as nove
peças **já ligadas no Inspector**. Nasce uma vez, sobrevive às trocas de cena.

### O que isso apaga

- **`BuildHUDCompleto` deixa de existir.** Não encolhe — some, junto com a lista de cenas dele.
  Não há mais o que ficar desatualizado.
- **`MontarBarraDeItens` / `MontarPainelDeInventario` / `BuildPainelDeFicha` /
  `MontarCaixaDeDialogo`** deixam de precisar do modo "montar na cena aberta": viram edição de
  prefab.
- **Um `PrefabInstance` por cena** deixa de ser uma coisa a verificar. O guarda vira: "o prefab
  existe em `Resources/` e tem as nove peças ligadas" — um teste, não cinco.
- **Zero divergência possível entre cenas**, porque não há cinco cópias.

### O que isso custa (honesto)

1. **Perde-se o preview no Editor.** Ao abrir `Deserto_Hali`, o HUD não aparece mais enquadrando
   a tela — ele só existe em Play Mode. Para trabalho visual de UI isso se resolve abrindo o
   prefab direto (que é *melhor* que hoje, onde é preciso rodar uma ferramenta batch para ver o
   efeito de mudar uma constante em C#). Para posicionar inimigos ou pintar chão, não faz falta.
2. **Precisa de guarda de duplicata.** `DontDestroyOnLoad` + recarregar a mesma cena = dois HUDs,
   se o singleton não barrar. Copiar exatamente o guarda do `InventoryManager` (`if (Instance
   == null) ... else Destroy(gameObject)`).
3. **Precisa esconder no menu.** `Cena_Menu` não deve mostrar HUD. Resolve-se com o HUD ouvindo
   `SceneManager.sceneLoaded` e se escondendo quando não há jogador/`GameLoopBootstrap` na cena.
4. **Rebind por cena.** O `GameLoopBootstrap` já faz esse trabalho hoje (`InjetarMaoFisica`,
   `InjetarCompanheiro`, `Bind` das barras); passa a rebindar um HUD que já existe em vez de um
   recém-criado. É a mesma quantidade de trabalho, no mesmo lugar.

### Alternativa, se o preview no Editor for inegociável

Manter instância por cena, mas escrever um teste que falhe se **qualquer** instância tiver
*override* (`m_Modifications` não-vazio nas propriedades de layout). Mantém o WYSIWYG e pega a
divergência. É mais código de teste e não elimina a lista de cenas — por isso é a segunda opção,
não a primeira.

### Passos, em ordem

1. Numa cena com HUD montado, extrair a raiz para
   `Assets/FavelaAmarela/Resources/HUD_Gameplay.prefab`, com as nove peças dentro, ligadas no
   Inspector.
2. Dar ao `HUDController` o bootstrap `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` +
   `Resources.Load` + `DontDestroyOnLoad` + guarda de singleton, **copiando** o de
   `InventoryManager`.
3. Fazer o HUD se esconder quando a cena carregada não tem jogador (o menu).
4. Ajustar `GameLoopBootstrap` para rebindar o HUD persistente a cada `sceneLoaded`.
5. Remover as instâncias de HUD das 5 cenas e **apagar `BuildHUDCompleto`**.
6. Substituir `HudCompletoTests` por um guarda do prefab (as nove peças ligadas), não das cenas.
7. Suíte completa antes e depois.

### Ganho imediato que isto destrava

A **caixa de diálogo pequena demais** (relatada pelo Vini em 2026-08-21, ao escolher fala de NPC)
hoje exige achar e corrigir a caixa em cinco cenas, ou rodar `MontarCaixaDeDialogo` e torcer.
Com o HUD persistente, é **uma edição no prefab** — fonte e tamanho, vistos na hora, valendo para
o jogo inteiro.

### Onde isto entra na fila (recomendação)

Depois do build, mas **provavelmente não antes de hitbox/hurtbox**. Este bloco é ganho de
*manutenção e de velocidade de iteração* — melhora a vida de quem desenvolve. Hitbox/hurtbox é
ganho de *sensação de combate* — melhora a vida de quem **joga**, que é o que decide se o jogo é
divertido e retém jogador. Ordem sugerida: (1) build sai, (2) hitbox/hurtbox + caixa de diálogo,
(3) este bloco, (4) Templo do Povo-Serpente.
