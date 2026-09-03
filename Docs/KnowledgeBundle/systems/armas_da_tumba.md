---
type: Game System
title: As Três Armas da Tumba
description: Maça de Aklo, Estilete de Irem e Alfanje de Alhazred — ataque básico, habilidade própria e o sangramento por acúmulo.
tags: [combat, weapons, tumba, sangramento]
---

# As Três Armas da Tumba


> ⚠️ **Os números de dano deste documento estão DEFASADOS, e a divergência é conhecida.** A
> tabela de panorama abaixo diz **40/25/60** (Maça/Estilete/Alfanje) para o dano básico; o
> código sempre disse **40/30/45**. O `CLAUDE.md` §3.1 regra 4 manda seguir o código para "como
> funciona", então a migração para dado de 2026-08-27 preservou **40/30/45** — os valores estão
> agora fixados em `EquivalenciaDaMigracaoTests`, e mexer neles é decisão de balanceamento
> deliberada, não efeito colateral de editar um asset. **Se 40/25/60 for a intenção de design,
> é mudança a fazer de propósito, no asset e no teste juntos.**
>
> **As armas deixaram de ser classes C#** em 2026-08-27. Vivem em
> `Config/Habilidades/Habilidade_*.asset` (o que o golpe faz) e `Config/Armas/BaseArma_*.asset`
> (alcance, raio e janela — a *família*). Ver [habilidades_de_item.md](habilidades_de_item.md).

O baú da Câmara do Baú (Zona 6b) **sorteia uma** das três armas seladas — não é escolha, é
RNG, e é o que faz a build variar entre partidas. Cada arma tem **ataque básico + uma
habilidade em botão separado**, com cooldown próprio.

Como o sorteio é aleatório, a regra de ouro do balanceamento é: **a Tumba tem de ser
vencível com qualquer uma das três.** Nenhuma pode ser obrigatória, e nenhuma pode ser
"a errada".

## Panorama

| Arma | Básico | Habilidade | Papel |
|---|---|---|---|
| **Maça de Aklo** | 40 | *Calar o Aklo* — **interrompe conjuração** | Ferramenta anti-mago |
| **Estilete de Irem** | 25 | *Ferida de Aklo* — **+3 acúmulos de sangramento** | Dano por permanência |
| **Alfanje de Alhazred** | 60 | *Golpe do Deserto* — **repele + atordoa** | Força bruta e espaço |

Damião começa **desarmado**: o golpe de Mão Vazia existe (faz barulho, entra no estado
Atacando) mas causa **dano 0** — bater de mão limpa não mata.

## O sangramento por acúmulo (Estilete de Irem)

O Estilete tem o **menor dano do baú** (25 contra os 60 do Alfanje). Numa disputa de
dano-por-janela ele perde sempre — então ele precisa de um **eixo diferente de força**.
A resposta é converter *permanência* em *burst*:

| Fonte | Acúmulos |
|---|---|
| Ataque básico (cooldown 0,3 s) | **+1** |
| Habilidade *Ferida de Aklo* (cooldown 5 s) | **+3** |

Ao chegar a **10 acúmulos**, as feridas **estouram automaticamente** e a contagem zera.

> **Por que o básico também acumula** (decisão do Vini, 2026-07-31): se só a habilidade
> acumulasse, com cooldown de 5 s chegar a 10 levaria quase um minuto. Com o básico
> somando, acumular vira consequência de **ficar em cima do alvo** — o que casa com a
> ideia de lâmina rápida e recompensa o jogador agressivo.

### Dano do estouro

| Alvo | Dano |
|---|---|
| **Aparição Primordial** (boss) | 10% da Vitalidade máxima, **teto de 60** |
| Inimigo comum | **40 fixo** |

Duas travas deliberadas:

- **Teto de 60** — sem ele, um chefe futuro com 2000 de vida levaria 200 num estouro, o que
  viraria um "delete boss".
- **Fixo contra comuns** — 10% de um Cultista (100 de Vitalidade) seriam 10 de dano,
  irrelevante; o jogador concluiria que a mecânica "não funciona" fora da luta de boss.

### Regras finas

- **Acumular renova a duração.** Parar de bater deixa a ferida estancar — acumular exige
  manter a pressão, não aplicar uma vez e esperar.
- **Um golpe fraco não rebaixa uma ferida grave**: fica valendo o maior dano-por-segundo.
- Enquanto acumula, corre um **DoT pequeno** (4/s por acúmulo). Ele existe para dar
  feedback — a barra do alvo se mexe, o jogador vê que algo está acontecendo — não como
  fonte principal de dano. O dano de verdade é o estouro.
- O escoamento **não passa pela Defesa**: ela já mitigou o golpe que abriu a ferida.

### Contra o Abdul: atravessa o Escudo Mágico

Este é o ponto que torna o Estilete viável no clímax. A ferida é aberta na janela de
vulnerabilidade e **continua sangrando (e acumulando rumo ao estouro) mesmo depois de o
Escudo Mágico voltar**.

Sem essa regra, o Estilete competiria por dano numa janela curta contra o Alfanje e
perderia — e a premissa "vencível com qualquer arma" cairia. Com ela, ele **cobra durante
a espera**: enquanto o Alfanje precisa da janela aberta, o Estilete trabalha o tempo todo.
Ver [Luta contra Abdul](boss_abdul.md).

## Arquitetura

- `IArma` / `IArmaComHabilidade` + `ArmaResult` (`Core.Abilities`) — contrato e resultado
  imutável de um golpe. `ArmaResult` carrega os efeitos (`AcumulosDeSangramento`,
  `InterrompeConjuracao`, `ForcaRepulsao`, `Atordoou`…); cada arma preenche só o que usa.
- `Sangramento` + `ExplosaoDeSangramento` (`Core.Combat`) — POCOs puros com a regra de
  acúmulo/estouro e a conta do dano percentual. Testados sem Unity.
- `MaoFisicaBridge` (Runtime) — equipa a arma (`EquiparArma`, chamado pelo `BauDaTumba`) e
  resolve o golpe contra qualquer `IDanificavel`.
- Quem recebe o golpe (`CultistaAI`, `AbdulAlhazredAI`) possui a própria instância de
  `Sangramento` e escoa a ferida por frame.

> **Pendência conhecida:** `EsqueletoInvocado` ainda **não** sangra — ele é frágil e expira
> sozinho, então o acúmulo dificilmente chegaria ao teto; mas por consistência vale ligar
> quando houver folga.

---

## Guardado para a expansão: o Cravo de Aklo

**Decisão do Vini, 2026-09-02.** A família do meio virou **Maça** (morning star). A ideia
anterior — o **Cravo** — **não foi descartada**: fica reservada para a expansão de itens.

### O que era, e por que vale guardar

O *cravo* é um espigão. Em português, **cravar é fincar** — e a habilidade se chamava
literalmente **"Fincar o Aklo"**. A arma era um espigão **gravado com glifos de Aklo**, a
língua pré-humana que o Necronomicon traduz, que Aldaron era o único a ler, e em que está
escrita a última linha indecifrável da carta da Cassilda.

Fincá-lo num conjurador **interrompia a canalização**. Ou seja: a arma anti-mago do jogo
funcionava **escrevendo no inimigo**. Era a única das três cuja força não era corte nem
sangue, e sim linguagem — o que, num jogo sobre uma peça de teatro que enlouquece quem a
lê, é bom demais para se perder num rename.

### Por que saiu agora

Duas razões, ambas medidas:

1. **O nome era um fóssil.** Ele nasceu na v1.0 do GDD como *"Cravo de Ferro"*, quando a
   arma era de **mitigação pesada** (Defesa 30, um espigão grosso com que se apara pancada).
   A habilidade depois virou interrupção de conjuração, e o nome ficou — carregando um
   sentido que a mecânica já não tinha.
2. **Não se distinguia no inventário.** As três famílias eram Alfanje (corte curvo), Cravo
   (estocada) e Lâmina fina (picada): duas delas lâmina diagonal. Com maça, a tríade vira
   **corte / impacto / perfuração**, que se lê de relance.

### Como ele voltaria

Como **quarta família**, e não como substituto — a geometria dele já está definida e é
distinta da maça:

| | Alcance | Raio | Janela |
|---|---|---|---|
| Alfanje | 1,6 | 0,85 | 0,15 |
| Maça | 1,2 | 0,6 | 0,1 |
| Lâmina fina | 0,95 | 0,42 | 0,07 |
| **Cravo (futuro)** | ~1,2 | ~0,35 | ~0,08 | estocada estreita, mais rápida que a maça |

A habilidade **"Fincar o Aklo"** volta com ele. O que a maça herdou foi só o *efeito* de
interromper conjuração, com nome de impacto (**"Calar o Aklo"** — silenciar a boca que
conjura). Se o Cravo voltar, vale decidir se os dois dividem o papel anti-mago ou se a maça
troca de efeito.
