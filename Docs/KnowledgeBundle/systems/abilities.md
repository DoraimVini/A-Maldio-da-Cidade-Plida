---
type: Game System
title: Sistema de Habilidades Anômalas e Equipamentos
description: Lógica de poderes baseados em IAnomalyPower e armas físicas baseadas em IArma (Sistema de 2 Slots)
tags: [abilities, powers, combat, weapons, design-pattern, poco]
timestamp: 2026-07-10T15:00:00Z
---

# Sistema de Habilidades e Equipamentos (2 Slots)

Para manter a interface minimalista de horror e seguir a filosofia arquitetural do projeto (composição sobre herança via C# puro), o sistema de equipamentos e progressão de Damião baseia-se em **dois slots de ação ativos** e **itens consumíveis rápidos**.

---

## 1. O Sistema de 2 Slots (Físico vs. Anômalo)

Inspirado em *Source of Madness*, Damião pode carregar exatamente dois itens ou habilidades ativas de cada vez (um em cada mão). Esta decisão elimina grades de inventário complexas.

```
                  ┌──────────────────────────────┐
                  │      DAMIÃO (Equipamentos)   │
                  └──────────────┬───────────────┘
                                 │
        ┌────────────────────────┴────────────────────────┐
        ▼                                                 ▼
   SLOT 1: MÃO FÍSICA                               SLOT 2: MÃO ANÔMALA
   - Interface: IArma                               - Interface: IAnomalyPower
   - Categoria: Mundana                             - Categoria: Sobrenatural
   - Consumo: Apenas Cooldown                       - Consumo: Resiliência Mental + Cooldown
   - Exemplos: Barra Enferrujada, Lâminas           - Exemplos: Salto Dimensional, Talismãs
```

### 1.1 Mão Física (`IArma.cs`)
Reservada para ferramentas físicas e armas brancas. O jogador pode usá-las livremente, dependendo apenas do tempo de recarga da própria arma.
*   **Contrato POCO:**
    ```csharp
    public interface IArma
    {
        string NomeDaArma { get; }
        bool CanActivate(float tempoDesdeUltimoUso);
        ArmaResult Execute();
    }
    ```
*   **Armas Cadastradas:**
    *   **Barra Enferrujada (Implementada):** Causa dano físico leve. Possui $35\%$ de chance de infligir estado `Atordoado` no cultista por $2.0$ segundos (injetável via gerador de números aleatórios para testes).
    *   **Lâmina do Sinal (Planejada):** Causa dano físico moderado. Se o ataque atingir o inimigo por trás enquanto o jogador estiver em stealth (`PlayerStealthState.Agachado`), inflige dano crítico e atordoamento de $4.0$ segundos.

### 1.2 Mão Anômala (`IAnomalyPower.cs`)
Reservada para manifestações dimensionais de Carcosa. Distorcer a realidade consome a estabilidade neural de Damião, drenando sanidade a cada uso.
*   **Contrato POCO:**
    ```csharp
    public interface IAnomalyPower
    {
        string PowerName { get; }
        bool CanActivate(float resilienciaAtual, float tempoDesdeUltimoUso);
        PowerResult Execute(float resilienciaAtual);
    }
    ```
*   **Poderes Cadastrados:**
    *   **Salto Dimensional (Implementado):** Teleporta Damião no plano XY, atravessando colisões. Custa $15.0$ de Resiliência Mental por uso e emite um pulso de som de $8.0$ metros de raio.
    *   **Talismã do Vento Negro (Planejado):** Cria um cone de vento que empurra cultistas leves para trás e apaga postes de luz no raio do efeito. Custa $20.0$ de Resiliência Mental.

---

## 2. Barra de Ações Rápidas (Itens Consumíveis)

Além dos dois slots ativos, Damião tem acesso a uma hotbar de consumíveis rápidos no HUD:
*   **Funcionamento:** Itens são empilháveis até um limite rígido de 3 unidades por tipo. O uso é instantâneo e serve para gestão de sanidade emergencial em combate ou escuridão.
*   **Exemplos de Itens:**
    *   *Chá Calmante:* Restaura $40.0$ de Resiliência Mental. Tempo de consumo: $1.0$ segundo (durante o qual Damião fica lento).
    *   *Sino de Estática:* Emite um ruído estridente de $15.0$ metros no ponto de impacto ao ser lançado, servindo para atrair cultistas para longe do caminho seguro.

---

## 3. Progressão de Habilidades (Sem Árvores de RPG)

Para evitar árvores de habilidades genéricas que quebram o tom de horror, a progressão baseia-se em **Composição Dinâmica**:
1.  **Desbloqueio de Módulos:** Damião encontra "Fragmentos de Hali" ou patuás esculpidos pelo cenário. Cada patuá é um script POCO que herda de `IAnomalyPower` ou `IArma`.
2.  **Troca nos Refúgios:** A substituição das habilidades equipadas nas mãos **só pode ser feita fisicamente sob a luz de um poste de luz seguro** (Refúgio). Isso impede a troca rápida de poderes no meio de perseguições, exigindo planejamento.
3.  **Gating Inicial:** O jogador começa o jogo sem arma física e sem o Salto Dimensional (`MaoFisicaBridge.desbloqueadaNoInicio == false`). O desbloqueio de ambos ocorre de forma dramática ao cair no subterrâneo (Zona 5), alterando instantaneamente a forma como o jogador lida com os cultistas.

### Decisão de nomenclatura (2026-07-28)
Resolvido: o pickup existente na Zona 5 (`PatuaPickup.cs`), que destrava o Salto Dimensional e colidia de nome com a nova relíquia lendária **Patuá das Luas Gêmeas** (recompensa da quest da Rainha Cassilda, item diferente), passa a se chamar **"Fragmento de Hali do Salto"**. Renomeação de código (classe/prefab/textos de UI) pendente da Fatia 3 do roadmap (pickup genérico de relíquia).
