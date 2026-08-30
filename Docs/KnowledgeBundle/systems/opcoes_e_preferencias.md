---
type: Game System
title: Opções e Preferências do Jogador
description: Volume, tela cheia e sincronização vertical — o que o jogador escolhe sobre o jogo, como é aplicado no motor e onde é guardado.
tags: [ui, audio, video, preferencias, shipping]
---

# Opções e Preferências do Jogador

**Criado em 2026-08-29.** Até esta data o projeto não tinha **controle de volume nem opção de
vídeo nenhuma** — nem no menu principal, nem na pausa. Para um jogo que vai ser vendido isso
não é polimento: é a primeira coisa que alguém procura quando o som está alto demais, e a
ausência vira análise negativa antes de qualquer julgamento sobre o jogo em si.

## As três peças

| Peça | Camada | Papel |
|---|---|---|
| `PreferenciasDoJogador` | **Core (POCO)** | O que o jogador escolheu. Valida, saneia e avisa quem escuta. Testável sem a Unity. |
| `PreferenciasBridge` | Runtime | Persiste em disco e **aplica no motor**. Nasce sozinha, antes de qualquer cena. |
| `PainelDeOpcoes` | Runtime (UI) | A tela. Persistente e independente do HUD. |

## O que é oferecido

| Opção | Padrão | Observação |
|---|---|---|
| **Volume geral** | 0,8 | Canal único — o jogo tem efeitos e **não tem música** |
| **Tela cheia** | ligada | |
| **Sincronização vertical** | **ligada** | Ver abaixo: é recomendação explícita da Unity |
| **Limite de quadros** | sem limite | **Só vale com VSync desligada** |

### Um canal de volume, não três

Um painel com "Música / Efeitos / Geral" mentiria sobre o que o jogador controla: não existe
sistema de música no projeto. Quando existir, o canal entra aqui e no `MixerDeAudio` junto.

### Por que VSync e não `targetFrameRate`

A primeira recomendação da auditoria era `Application.targetFrameRate`. A documentação da
Unity 6.4 é explícita contra:

> *"It's recommended to use `QualitySettings.vSyncCount` over `Application.targetFrameRate`
> because vSyncCount implements a hardware-based synchronization mechanism, whereas
> targetFrameRate is a software-based timing method and is subject to microstuttering."*

E, decisivo para a interface:

> *"If `vSyncCount != 0`, then `targetFrameRate` is ignored."*

Por isso o padrão é **VSync ligada**, e o limite de quadros existe só para quem a desligar. O
POCO expõe `LimiteEfetivoDeQuadros`, que devolve "sem limite" enquanto a VSync estiver ligada —
e o seletor da tela fica **desabilitado** nesse caso. Uma opção ativa mostrando "60" enquanto o
motor ignora aquele número é a interface descrevendo um estado que não existe.

## Onde fica guardado

`preferencias.json`, em `Application.persistentDataPath` — **ao lado do save, não dentro**.

Preferência não é progresso: começar uma peregrinação nova apaga o save e **não pode** zerar o
volume que a pessoa ajustou. Arquivo ilegível cai no padrão e avisa uma vez, em vez de impedir
o jogo de abrir.

## Como o volume chega ao som

O `MixerDeAudio` é o **ponto único** por onde todo som do jogo passa. Por isso é o único lugar
onde ler o volume faz o jogo inteiro obedecer:

```
PainelDeOpcoes  →  PreferenciasDoJogador.VolumeGeral
                        ↓
                   MixerDeAudio.VolumeGeral  (propriedade, não campo)
                        ↓
                   toda chamada de Tocar()
```

Sem `PreferenciasBridge` em cena — numa cena de teste, por exemplo — o campo do Inspector
responde. Degrada para o comportamento antigo em vez de emudecer o jogo.

## Decisões registradas

### A tela vive fora do HUD

O `HUDController` se oculta em toda cena sem `GameLoopBootstrap` — ou seja, no **menu
principal**, que é justamente onde se procura as opções antes de começar. Pendurar a tela ali
exigiria furar aquela regra, e a regra é boa (é ela que mantém o HUD fora do menu sem uma lista
de "cenas que têm HUD"). O painel é um Canvas próprio, persistente, `sortingOrder` 200.

### Resolução não é oferecida — ainda

Uma resolução mal escolhida pode deixar a interface fora da tela, e então o jogador **não
alcança mais a opção para desfazê-la**. Tela cheia cobre a necessidade comum sem esse risco.
Resolução entra quando houver uma confirmação com contagem regressiva que reverte sozinha.

### Só um dono para a configuração de vídeo

`QualitySettings.vSyncCount`, `Application.targetFrameRate` e `Screen.fullScreen` são escritos
**exclusivamente** pela `PreferenciasBridge`, e um teste guarda isso. Dois donos divergem, e o
motor fica num estado que a interface não descreve — é a mesma forma de defeito dos dois números
de dano por inimigo que a [ficha_de_atributos.md](ficha_de_atributos.md) documenta.

## O que os testes garantem

- **`PreferenciasDoJogadorTests`** — limites do volume, saneamento do limite de quadros (0 fps é
  um jogo congelado), a regra da VSync, evento único ao carregar do disco, e que o volume
  **chega ao `MixerDeAudio`**.
- **`OpcoesAlcancaveisTests`** — que a tela existe e está inteira, **e** que há botão ligado nos
  dois menus. As duas coisas, porque não são a mesma: este repositório coleciona peças que
  existem, compilam e ninguém consegue chamar.

## Relacionados

- [Ficha de Atributos](ficha_de_atributos.md) — o padrão "um dono por número"
- [Interface e HUD](../scripts/index.md) — por que o HUD é persistente e se oculta
