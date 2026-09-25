---
type: Game System
title: Persistência — O Que Salva, Quando e Como
description: O ciclo completo do save, o padrão Observer do IPersistente, e o bug de 2026-08-11 em que o jogo gravava mas nunca lia de volta.
tags: [save, persistencia, inventario, progressao, artefatos]
---

# Persistência

## O ciclo, de ponta a ponta

```
Início do jogo
   └─ GerenciadorDeSave.Awake() → CarregarDoDisco()   ← lê o JSON para a memória
        └─ cada IPersistente, no Start(), se Registra e consulta o registro

Durante a partida
   └─ Refúgio de Luz  → CapturarTudo() + GravarEmDisco()   ← ponto de save
   └─ Troca de cena   → CapturarTudo()                     ← só memória, sem disco

Fim
   └─ o arquivo em disco só reflete o último Refúgio visitado
```

## O bug de 2026-08-11: gravava e nunca lia

`CarregarDoDisco()` existia desde o começo e **nunca era chamado por ninguém** em todo o
projeto. O efeito era traiçoeiro porque *parecia* funcionar:

- **Trocar de cena funcionava** — o registro vive em memória num objeto `DontDestroyOnLoad`,
  então a Tumba → Deserto preservava tudo.
- **Fechar e reabrir o jogo perdia tudo** — o arquivo estava lá, escrito, e ninguém o lia.

Sem erro, sem aviso. Corrigido chamando `CarregarDoDisco()` no `Awake` do gerenciador —
antes do `Start` dos objetos de cena, que é quando cada um se registra e consulta o registro.
O campo `carregarDoDiscoAoIniciar` permite desligar isso para depurar uma partida limpa **sem
apagar o arquivo**.

## O padrão: Observer, não auto-save

Ninguém salva o próprio arquivo. Cada `IPersistente` só sabe **ler e escrever o próprio
estado**; quem junta tudo e grava é o `GerenciadorDeSave`. Isso mantém **um arquivo só, um
formato só e um lugar só para depurar**.

```csharp
string ChaveDePersistencia { get; }   // de ChavesDeSave, ou do ObjetoPersistente
string CapturarEstado();              // serializa o próprio estado
void AplicarEstado(string estado);    // só é chamado se a chave existir no save
```

**Objeto sem chave no save mantém o estado padrão** — é o fallback gracioso que permite
adicionar conteúdo novo sem invalidar saves antigos.

## O que persiste hoje

| O quê | Ponte | Chave |
|---|---|---|
| Arma empunhada + Vitalidade | `EstadoPersistenteDoJogador` | `Jogador.ArmaEquipada` |
| Companheiro | `EstadoPersistenteDoCompanheiro` | `Companheiro.YugNeth.*` |
| **Mochila + equipamento** | `EstadoPersistenteDoInventario` | `Jogador.Inventario` |
| **Nível, Exposição e Ecos** | `EstadoPersistenteDaProgressao` | `Jogador.Progressao` |
| **Os 4 slots de Artefato** | `EstadoPersistenteDosArtefatos` | `Jogador.Artefatos` |
| Flags de mundo/quest | direto no registro | `Quest.*`, `Mundo.Abatido.*` |

As três em **negrito** entraram em 2026-08-11. Antes disso, `InventoryManager.GetSaveData()` e
`ProgressionManager.GetSaveData()` existiam e **nunca eram chamados** — mochila, equipamento,
nível e Ecos se perdiam a cada recarregamento, em silêncio. Ficou pior quando o nível passou a
**gatear o loot** (ver [loot_e_drop.md](loot_e_drop.md)): perder o nível passou a significar
perder acesso a itens.

### Decisões de formato
- **Inventário e progressão** viajam em **JSON** (`JsonUtility`), porque têm estrutura.
- **Artefatos** viajam como **ids separados por vírgula**, com slot vazio virando campo em
  branco (`"necronomicon,,coroa_de_ossos,"`). A **posição importa** — o jogador escolheu qual
  Artefato fica em qual tecla, e devolver tudo embaralhado seria quase tão ruim quanto perder.
- **JSON ilegível não derruba o load**: cai num aviso e mantém o estado padrão.

## Salvar é decisão de design, não automatismo
`gravarEmDiscoAoCapturar` vem **desligado**: gravar só nos **Refúgios de Luz**. Num jogo de
sobrevivência, poder salvar em qualquer lugar dissolve a tensão de decidir se vale a pena
avançar mais um cômodo.

## Pendências
- **Wiring de cena:** as três pontes novas precisam ser anexadas ao Damião.
- **Sem tela de save/load** e sem múltiplos slots de partida — depende do menu principal, que
  também não existe (ver [roadmap_vertical_slice.md](../roadmap_vertical_slice.md)).
- **Save não versionado:** não há campo de versão no `EstadoDeSave`, então uma mudança de
  formato futura não terá como migrar saves antigos.

## Relacionados
- [Loot e Drop](loot_e_drop.md) — o nível persistido gateia o drop
- [Artefatos](artefatos.md) — os 4 slots persistidos
