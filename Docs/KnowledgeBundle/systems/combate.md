---
type: System
title: Sistema de Combate Pálido (Hardcore Tático)
description: Sistema de combate focado em armas e tipos de dano (Priming), adaptado ao lore de Favela Amarela.
---

# Sistema de Combate: Arsenal de Carcosa (Hardcore Tático)

Este sistema funciona como uma alternativa tática para quando o Stealth falha. Ele baseia-se em tipos de dano nativos (Famílias de Armas), _Priming_ (preparação de status para combos) e quebra de defesas em camadas.

> **Importante:** Como o Favela Amarela foca primariamente em furtividade, este combate denso existe para encontros que escalam para o confronto aberto, ou para situações onde o jogador decide enfrentar perigos para garantir recompensas específicas (armas, afixos ou recursos).

## Taxonomia do Arsenal Pálido

Cada arma pertence a uma "Família" e tem um tipo de Dano Nativo. Isso define seu moveset, cadência e os status que aplica.

| Família | Dano Nativo | Comportamento Tático | Efeito Implícito Único (Lore) |
|---|---|---|---|
| **Lâminas Enferrujadas** | **Tétano (Corte)** | Ataques rápidos, permite esquiva ágil. | *Sede de Sangue:* +15% de crítico contra alvos Sangrando. |
| **Ferramentas Pesadas** | **Trauma (Impacto)** | Lentas, super armadura, quebra defesas/postura. | *Esmagamento Ósseo:* Cada 3º golpe estilhaça a armadura em 30% por 5s. |
| **Armas de Gambiarra** | **Pólvora (Fogo)** | Lentidão na recarga, atrai mais inimigos (som). | *Combustão Suja:* Tiros deixam nuvem ardente (DoT e corta visão). |
| **Químicos de Viela** | **Tóxico (Ácido)** | Curto alcance, longo DoT. | *Alucinação Química:* Inimigos mortos explodem em nuvem tóxica. |
| **Insígnias do Rei** | **Pálido (Arcano)** | Consome **Resiliência Mental** (sanidade). | *Olhar de Hastur:* Atravessa matéria física e dissipa buffs. |

## Matriz de Priming (Efeitos de Combo)

Recompensa trocar de armas ou ataques variados para executar um efeito massivo. Funciona através de `Status Aplicado (Preparation)` -> `Ataque Seguidor (Detonation)`.

- **Sangramento** (Tétano): Causa dano físico. *Preparado:* Golpe de **Trauma** causa **Mutilação** (Stun longo e grande dano de postura).
- **Osso Fraturado** (Trauma): Reduz armadura. *Preparado:* Tiro de **Pólvora** causa **Estilhaço** (Dano em explosão ao redor do alvo).
- **Queimadura** (Pólvora): Fumaça sufocante. *Preparado:* Magia **Pálida** causa **Vislumbre de Carcosa** (Colapso mental inimigo; ataca aliados ou fica aterrorizado).
- **Colapso/Loucura** (Pálido): Lerdeza e vulnerabilidade. *Preparado:* Ataque **Tóxico** causa **Anomalia** (Dano massivo e espalha Loucura).
- **Delírio** (Tóxico): Erros de ataque no inimigo. *Preparado:* Golpe de **Tétano** causa **Choque Anafilático** (Morte instantânea de mobs menores, hemorragia gigante em maiores).

## Defesas em Camadas (Inimigos)

- **Manto/Máscara de Gesso (Cultistas):** Alta proteção mágica/anômala. Fraco contra **Trauma** (quebre a máscara primeiro).
- **Carne Distorcida (Mutantes/Aberrações):** Altíssima defesa física (Corte/Trauma). Fraco contra **Pólvora** e **Tóxico**. *Nota: A Coisa do Cemitério possui defesas específicas além dessas.*
- **Armadura Balística (Milícia/Policiais):** Imune a Tétano/Pólvora ligeira. Quebrado por **Trauma** ou contornado por **Pálido**.
- **Sombra/Espectro de Areia:** Sem forma física. Recebe dano integral apenas de **Pálido**.

## Sistema de Hibridização e Loot

Armas únicas podem conter afixos raros de Hibridização, que convertem até **50%** do dano de uma família em outro tipo (ex: *Marreta de Motor* -> Trauma com 40% de conversão em Pólvora/Fogo). Isso altera a forma como o *Priming* pode ser ativado, otimizando _builds_ e incentivando a exploração por "Loot fora da curva" pós-Tempestade de Areia ou em covis secretos.

## Briga de Rua (A Fundação Desarmada)

Em *Favela Amarela*, o combate desarmado não é uma arte marcial, mas instinto de sobrevivência puro. Serve para ensinar as regras do jogo e impor respeito.

- **Família Oculta:** Mãos Nuas / Briga de Rua.
- **Dano Nativo:** **Trauma Leve** (Não quebra armaduras/máscaras, apenas empurra).
- **Velocidade:** Rápida, golpes curtos, permitindo empurrar o inimigo e correr.
- **Efeito Implícito (Lore):** *Instinto de Rato* — Quando a Vida ou a Resiliência Mental caem abaixo de 30%, a velocidade dos socos aumenta em 20%. Reflete o desespero de um morador acuado.
- **Limitações:** Não quebra a *Máscara de Gesso* (causa 0 de dano na postura) e não afeta *Sombras da Tempestade*.

## O Tutorial Pálido (Curva de Aprendizado)

O jogo ensinará a matriz de combate pela dor, usando o design de níveis:

1. **Fase 0 (Desarmado):** Enfrentando *Infectados Iniciais* (viciados pela névoa), o jogador aprende a rolar, bater e recuar.
2. **A Parede Intransponível:** Um *Cultista do Véu* com Máscara de Gesso bloqueia o caminho. Socos são inúteis. O jogador deve pegar um **Pedaço de Cano (Trauma)** próximo para quebrar a máscara.
3. **O Primeiro Priming:** Enfrentando um mutante de carne, o jogador usa uma *Navalha (Tétano)* para causar sangramento e finaliza com o *Cano (Trauma)*, gerando a *Mutilação*.



## Integração com Código (Diretrizes para Scripts/POCOs)

- As Famílias são cadastradas como `ScriptableObjects` (ex: `ItemConfig` com o campo `NativeDamageType`).
- O Dano "Pálido" (Insígnias do Rei) consome sanidade; conecte os golpes ao `ResilienciaMental.Consumir()`.
- O `DamageManager` atua checando a `DefenseLayer` do inimigo e a matriz de *Priming* no momento do hit para resolver bônus massivos (ex: aplicando a Anomalia).
