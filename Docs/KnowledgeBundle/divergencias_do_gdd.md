---
type: Design Register
title: Divergências entre o GDD e o jogo implementado
description: Onde a implementação se afastou do GDD Mestre v1.3, e por quê — o GDD é a intenção, o código é a verdade
timestamp: 2026-08-21T12:00:00Z
---

# Divergências do GDD — registro consolidado

> **Regra de leitura** (`CLAUDE.md` §3.1, item 4): em conflito entre OKF e código, o
> **código-fonte é a verdade sobre como funciona**; o **OKF é a verdade sobre como deveria
> funcionar**. Este documento existe para que a divergência seja **explícita** em vez de
> descoberta por acidente meses depois.
>
> Referência: [GDD Mestre v1.3](GDD_Mestre.md) · [Roadmap do VS](roadmap_vertical_slice.md) ·
> [Plano da build](plano_da_build.md)

---

## A. Divergências que o GDD ainda afirma como verdade (corrigir no GDD)

### A1. Yug-Neth **não é mais** a chave dos Portões das Ruínas ⚠️

O changelog **v1.2** do GDD diz textualmente: *"Yug-Neth é a chave dos Portões das Ruínas."*

**Descartado em 2026-08-20.** No lugar, o fim da luta contra o Byakhee libera um **Poste de
Luz** — que já reanima o companheiro, ancora a Resiliência Mental, cura e grava a partida.
O papel de Yug-Neth mudou para outra coisa: ao entrar no **Castelo de Carcosa** ele **deixa de
ser companheiro** e vira o **NPC que ensina o artesanato**.

*Por que mudou:* uma "chave" que é um personagem vivo cria um estado de falha sem saída — se o
companheiro fosse incapacitado antes dos Portões, o jogador ficava trancado. O Poste de Luz
resolve o mesmo beat narrativo (recompensa por vencer o chefe) sem poder travar a partida.

### A2. A Resiliência Mental **não é mais** o único recurso ativo

§3.5 diz: *"Resiliência Mental (RM): O único recurso ativo (0.0 a 100.0)."*

Hoje são **três** recursos com barra própria no HUD:
- **Resiliência Mental** — a mente (o pilar original)
- **Vitalidade** — a carne (combate físico virou pilar sistêmico próprio, ver `CLAUDE.md` §1)
- **Vigor** — estamina da Esquiva

Mais a **Resiliência do Companheiro (RC)** quando Yug-Neth está ativo.

### A3. Consumíveis autorados são outros

§3.5 cita *"Chá Calmante que recupera 40 RM"*. Os três consumíveis que existem em jogo são:
**Água da Cacimba** (corpo), **Erva de Ancoragem** (mente) e **Raiz de Yhtill** (os dois).
Modelo: finitos e não-farmáveis, com o anti-*soft-lock* no `RefugioDeLuz` em vez de moeda ou
recarga.

### A4. Propagação sonora não usa ondas físicas

§3.6 descreve *"ondas circulares físicas na cena que alertam os cultistas se tocarem em seu raio
auditivo"*.

A implementação é **`SoundBroadcastService`** — um POCO em `Core.Stealth` que emite um evento
`SomEmitido` (posição + raio); o `CultistaFSM` compara distância. **Não há objeto físico de onda
na cena.** É mais barato, determinístico e testável sem Unity rodando — e foi o que permitiu os
testes EditMode do stealth.

### A5. Y-sorting não usa Custom Axis

§3.7 fala em *"Y-Sorting dinâmico por eixo customizado"*. O projeto está em **Transparency Sort
Mode `Default`**; a ordenação vem de **`sortingOrder = -y * 10`** explícito por sprite
(`DynamicYSort` em `LateUpdate`). Funciona porque o sort axis só desempata. Reconciliar quando o
pipeline for mexido (URP). Ver skill `favela-isometric-standards`, mandato 5.

---

## B. Decisões de design tomadas depois do GDD (não estão nele)

### B1. A Tumba de Alhazred é **obrigatória**

O portal do Deserto para os **Portões das Ruínas** é travado pela chave de save
`Quest.Tumba.AbdulResolvido`. Sem resolver Abdul (vencendo **ou** poupando), o portal recusa
passagem com uma linha diegética.

*Por quê:* é na Tumba que Yug-Neth é libertado. Quem fosse direto chegava ao Byakhee **sem arma
e sem companheiro**, e depois ao Castelo sem o NPC de artesanato. Decisão do Vini (2026-08-21):
manter obrigatório para simplificar o Vertical Slice. Guarda: `TumbaObrigatoriaTests`.

### B2. O atalho Santuário → Castelo foi **removido**

Existia porque o Castelo nasceu como cena solta. Pulava o Byakhee — a **única fonte do Anel do
Sinal Amarelo** — e levava ao Rei sem o necessário para o rito.

### B3. Abater o Byakhee **destranca**; quem **abre** é o jogador

Os Portões não abrem sozinhos no instante do abate. Ficam destrancados, e o jogador abre
interagindo (`PortaoDosPortoes`, um `IInteragivel`).

*Por quê:* abrir sozinho rouba o gesto do jogador e joga a transição de fase por cima da
animação de morte do chefe. Assim a luta termina, o mundo respira, e a passagem é escolha.

### B4. A Tempestade de Memória **não** afeta a velocidade do jogador

Decisão do Vini: a tempestade atrapalha **só os inimigos**. O efeito dela sobre o jogador é
abafar o próprio ruído (`PlayerStealthState.AplicarAbafamentoTempestade`) — o que **inverte** o
stealth: tempestade forte = mais seguro para se mover.

### B5. Luz é segurança (stealth invertido)

`RefugioDeLuz` faz da luz o lugar seguro, ao contrário do stealth clássico onde a sombra
protege. A detecção é **100% sonora** — não existe detecção por luz/sombra nem percepção
graduada (esta última está fora do VS).

### B6. A Coroa de Ossos **não** é exigida pelo rito

O Rei em Amarelo pede **3 relíquias**, não 4. A Coroa só faz falta para o Set Lendário 4/4, que
abre a Z4 (Observatório) — dungeon opcional, fora do VS.

### B7. Título visível ao jogador: **"Caminho para Carcosa"**

O repositório (`A Maldição da Cidade Pálida`), a pasta local (`Peregrino Amarelo`) e os
namespaces (`FavelaAmarela.*`) mantêm nomes históricos. Todo texto novo mostrado ao jogador usa
**Caminho para Carcosa** — e o `productName` da build foi corrigido para isso em 2026-08-21
(estava saindo com o nome do repositório).

### B8. Áudio é **sintetizado**, não arquivos

`SinteseDeSom` gera as formas de onda em runtime. **Zero arquivos `.wav` no projeto é o
esperado, não um sintoma** — uma auditoria anterior concluiu erradamente que "não havia áudio".
`MixerDeAudio`, `AudioDeStealth` e `AudioDeResiliencia` estão nas cinco cenas de gameplay.

### B9. Progressão por nível fica **fora** do VS

`Progressao` + `ProgressionBridge` existem e funcionam, mas **ninguém concede Exposição no mundo
e não há um único `EcoDef` autorado**. Consequência esperada (não bug): com o nível travado em
1, o loot só entrega tier 1.

### B10. Cinemática de abertura **adiada**

Sem artista e sem ferramenta definida. Sai do caminho crítico da build; o design continua em
`systems/cinematica_abertura_deserto.md`.

---

## C. Padrões de engenharia estabelecidos (o GDD §3.7 é curto demais)

§3.7 só diz *"`Rigidbody2D` com `gravityScale = 0`"*. A auditoria de física de **2026-08-21**
estabeleceu o resto do contrato, agora registrado na skill `favela-isometric-standards`:

| regra | valor | por quê |
|---|---|---|
| `gravityScale` | `0` | mandato original do GDD |
| **`constraints`** | **`FreezeRotation`** | corpo dinâmico que leva impulso fora do centro ganha velocidade angular; o `transform` roda e **o sprite gira junto**, destruindo a ilusão isométrica e girando o colisor. **4 corpos estavam sem isto** (Byakhee + 2 Cortesãos Pálidos + Damião da `cena_1`) |
| **`collisionDetectionMode`** | **`Continuous`** | `Discrete` deixa ator rápido atravessar parede fina entre dois `FixedUpdate`. **7 de 9 estavam em `Discrete`**, o Damião incluído |
| **`Physics2D.simulationMode`** | **`FixedUpdate`** | estava em `Update`/`Script`, contradizendo todo o código de movimento, que escreve `linearVelocity` dentro de `FixedUpdate` |
| **`interpolation`** | **`Interpolate`** | exigido pela doc da Unity quando em modo `FixedUpdate`: *"Unity may render multiple frames between simulation updates"* |
| **matriz de colisão** | **`Player` ✗ `Enemy`** | os dois eram `Dynamic` e não-trigger, então se **empurravam** fisicamente. Dano é resolvido por `Vector2.Distance` + `IDanificavel` — a colisão não tinha função de jogo, só produzia tranco e torque |

### C1. Movimento é `linearVelocity`, não força

Atribuído em `FixedUpdate`. **Não** `MovePosition`, **não** `AddForce`. Velocidade atribuída
ignora massa e inércia: o personagem para no mesmo quadro em que a tecla é solta. É controle
responsivo, não simulação.

### C2. Pegada de colisão: **0,60 × 0,30**, achatada 2:1

As pegadas nunca tinham sido calibradas entre si (Damião **1,467** contra Cultista 0,576 — dois
humanos do mesmo rig; o colisor do Damião era mais largo que a própria figura desenhada).

**Sobre cápsula:** cápsula *em pé* é forma de plataforma lateral, onde o eixo alto é altura
real. No isométrico o Y da tela é **profundidade** — a pegada é área no chão, e o que importa é
ser achatada na proporção 2:1 da grade.

Exceções legítimas e testadas: `YugNeth` e `EsqueletoInvocado` mantêm pegada própria derivada da
arte (`ArteDosPlaceholdersTests`).

### C3. O colisor do Byakhee é **trigger**, não sólido

O filtro do golpe usa `useTriggers = true`, então trigger basta para ser acertado — e um chefe
voador sólido empurraria o jogador e enroscaria nas paredes da arena.

### C4. Damião e Cultista medem **o mesmo**

Correção do Vini (2026-08-20): os dois são humanos e vieram do mesmo rig. A comparação é pela
**figura** (sem a elipse de sombra), porque as folhas têm margens diferentes — igualar altura de
imagem deixaria os corpos desiguais.

### C5. Hitbox/hurtbox **não existem** (dívida conhecida)

Cada personagem tem **um** colisor fazendo três trabalhos: barrar movimento, receber dano, ser
detectado. As quatro camadas para separar já estão declaradas e **não são usadas por nada**:
`PlayerHitbox` (11), `EnemyHitbox` (12), `PlayerHurtbox` (13), `EnemyHurtbox` (14).

---

## D. Direção de arquitetura decidida, ainda não executada

### D1. O HUD deve ser **persistente**, não por cena

Ver [Bloco 6 do plano da build](plano_da_build.md). Resumo: o HUD não muda entre cenas e não tem
por que nascer cinco vezes. Deve seguir o padrão que **já existe neste projeto**
(`InventoryManager`, `GerenciadorDeSave`, `ProgressionBridge`): `Resources.Load` +
`DontDestroyOnLoad` + guarda de singleton, bootstrapped em `BeforeSceneLoad`.

Isso **apaga** `BuildHUDCompleto` e a lista de cenas dele — o modo de falha mais repetido do
projeto (**seis** listas de cenas escritas à mão já envelheceram).

---

## Como manter este documento

Toda vez que uma decisão contrariar o GDD, ela entra aqui **no mesmo commit** que a implementa.
O GDD Mestre não é reescrito a cada mudança — ele guarda a intenção de design; este registro
guarda a diferença. Quando o GDD ganhar uma v1.4, a seção **A** deve ser absorvida por ele e
esvaziada aqui.
