using System;
using System.Collections.Generic;
using UnityEngine;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Abilities.Efeitos;

namespace FavelaAmarela.Inventario
{
    /// <summary>
    /// Que efeitos um golpe carrega. É o catálogo fechado de
    /// <c>habilidades_de_item.md</c>, exposto no Inspector.
    ///
    /// <para><b>Valor novo entra sempre no FIM</b> — este enum é serializado por índice nos
    /// <c>.asset</c>, e inserir no meio remapearia silenciosamente toda habilidade já autorada
    /// (um sangramento viraria repulsão sem ninguém notar). Mesma regra de <c>ItemType</c> e
    /// <c>EquipmentSlot</c>.</para>
    /// </summary>
    public enum TipoDeEfeito
    {
        /// <summary>Trauma físico direto.</summary>
        Dano,

        /// <summary>Estática cósmica — mitigada por Resistência Anômala, não por Defesa.</summary>
        TraumaAnomalia,

        /// <summary>Trava o alvo por um tempo.</summary>
        Atordoamento,

        /// <summary>Empurra o corpo, modulado por <c>CorpoImpregnado</c>.</summary>
        Repulsao,

        /// <summary>Abre acúmulos de sangramento (dano por permanência).</summary>
        Sangramento,

        /// <summary>Corta a conjuração de quem estiver conjurando.</summary>
        Interrupcao,

        /// <summary>
        /// Dano como <b>percentual do dano branco da arma</b> (Valor 1,0 = 100%).
        ///
        /// <para>Acrescentado no FIM em 2026-08-28, junto com o bloco de combate da
        /// <c>BaseDeArma</c>. É o efeito que faz a habilidade escalar com o equipamento em vez
        /// de ter número próprio — trocar de arma passa a melhorar todas as habilidades de uma
        /// vez, que é o loop que faz o loot valer a pena.</para>
        ///
        /// <para><c>Dano</c> plano continua existindo e continua legítimo: golpe de inimigo e
        /// habilidade de valor fixo não têm arma de onde escalar.</para>
        /// </summary>
        DanoDaArma,
    }

    /// <summary>Um efeito autorado no Inspector, com os números que ele carrega.</summary>
    [Serializable]
    public struct EfeitoAutorado
    {
        /// <summary>Qual efeito do catálogo.</summary>
        public TipoDeEfeito Tipo;

        /// <summary>
        /// O número principal: dano, trauma, segundos de atordoamento, força de repulsão, ou
        /// dano por segundo do sangramento. Ignorado por <c>Interrupcao</c>.
        /// </summary>
        public float Valor;

        [Tooltip("Só para Sangramento: quanto tempo cada acúmulo dura.")]
        public float Duracao;

        [Tooltip("Só para Sangramento: quantos acúmulos este golpe abre.")]
        public int Acumulos;
    }

    /// <summary>
    /// Monta uma <see cref="HabilidadeComposta"/> a partir de dados — a peça que faz arma nova
    /// deixar de custar uma classe C#.
    ///
    /// <para><b>O problema, no código de antes.</b> Cada arma com habilidade própria era uma
    /// classe escrita à mão, mais um valor no enum <c>TipoArmaFisica</c>, mais uma linha no
    /// dicionário da <c>WeaponFactory</c>. Como <c>habilidades_de_item.md</c> resumiu em
    /// 2026-08-10: <i>"uma dungeon inteira de armas novas é uma dungeon inteira de classes C#
    /// novas."</i></para>
    ///
    /// <para><b>A régua que continua valendo:</b> se o efeito é dano ou status com número
    /// configurável, é dado. Se tem lógica condicional própria — estado, contador, gatilho por
    /// fase de luta —, continua sendo código. O Escudo Mágico do Abdul merece classe; um
    /// alfanje que atordoa e repele, não.</para>
    /// </summary>
    [CreateAssetMenu(menuName = "Favela Amarela/Habilidade", fileName = "Habilidade_")]
    public sealed class HabilidadeDef : ScriptableObject
    {
        [Header("Identidade (visível ao jogador)")]
        [Tooltip("Nome diegético da arma. Segue o vocabulário do lore-enforcer.")]
        public string NomeDaArma;

        [Tooltip("Nome diegético da habilidade.")]
        public string NomeDaHabilidade;

        /// <summary>
        /// Ícone da habilidade, mostrado na barra de ações.
        ///
        /// <para><b>Faltava até 2026-09-02</b> — o campo não existia, então a barra de ações
        /// não tinha o que desenhar no slot da habilidade da arma. É a última lacuna de ícone
        /// do projeto: os 25 <c>ItemDef</c> e os 4 <c>ArtefatoDef</c> já têm o seu.</para>
        ///
        /// <para>Por padrão recebe o ícone da <b>família</b> da arma (o Alfanje do Rei usa o
        /// mesmo do Alfanje de Alhazred). Isso é decisão, não preguiça: a habilidade É o gesto
        /// da família, e os três tiers de um Alfanje golpeiam igual — o que muda é o número.</para>
        /// </summary>
        public Sprite Icone;

        [Header("Ataque básico")]
        [Tooltip("Quanto tempo o golpe prende o ator na ação.")]
        [Min(0.05f)]
        public float DuracaoBasico = 0.3f;

        [Tooltip("Cadência. É consultada de verdade desde 2026-08-27 — antes disso " +
                 "IArma.CanActivate não era chamado por ninguém e este número não fazia nada.")]
        [Min(0f)]
        public float CooldownBasico = 0.4f;

        public List<EfeitoAutorado> EfeitosDoBasico = new List<EfeitoAutorado>();

        [Header("Habilidade (botão separado)")]
        [Min(0.05f)]
        public float DuracaoHabilidade = 0.4f;

        [Min(0f)]
        public float CooldownHabilidade = 5f;

        public List<EfeitoAutorado> EfeitosDaHabilidade = new List<EfeitoAutorado>();

        /// <summary>
        /// Constrói o POCO de combate. Chamado pela <c>WeaponFactory</c> ao equipar.
        /// </summary>
        public HabilidadeComposta Construir(PerfilDeArma perfil = default) =>
            new HabilidadeComposta(
                NomeDaArma, NomeDaHabilidade,
                Traduzir(EfeitosDoBasico), Traduzir(EfeitosDaHabilidade),
                DuracaoBasico, CooldownBasico,
                DuracaoHabilidade, CooldownHabilidade,
                perfil);

        /// <summary>
        /// Dado autorado → efeitos POCO. Um <c>Tipo</c> desconhecido é <b>ignorado com aviso</b>
        /// em vez de estourar: um asset autorado por uma versão mais nova do jogo não pode
        /// derrubar a partida, e um golpe a menos é degradação melhor que uma exceção.
        /// </summary>
        private static IReadOnlyList<IEfeitoDeHabilidade> Traduzir(List<EfeitoAutorado> autorados)
        {
            var efeitos = new List<IEfeitoDeHabilidade>();
            if (autorados == null) return efeitos;

            foreach (var a in autorados)
            {
                switch (a.Tipo)
                {
                    case TipoDeEfeito.Dano:
                        efeitos.Add(new EfeitoDeDano(a.Valor));
                        break;
                    case TipoDeEfeito.TraumaAnomalia:
                        efeitos.Add(new EfeitoDeTraumaAnomalia(a.Valor));
                        break;
                    case TipoDeEfeito.Atordoamento:
                        efeitos.Add(new EfeitoDeAtordoamento(a.Valor));
                        break;
                    case TipoDeEfeito.Repulsao:
                        efeitos.Add(new EfeitoDeRepulsao(a.Valor));
                        break;
                    case TipoDeEfeito.Sangramento:
                        efeitos.Add(new EfeitoDeSangramento(a.Acumulos, a.Valor, a.Duracao));
                        break;
                    case TipoDeEfeito.Interrupcao:
                        efeitos.Add(new EfeitoDeInterrupcao());
                        break;
                    case TipoDeEfeito.DanoDaArma:
                        efeitos.Add(new EfeitoDeDanoDaArma(a.Valor));
                        break;
                    default:
                        Debug.LogWarning($"[HabilidadeDef] Efeito desconhecido '{a.Tipo}' — " +
                                         "ignorado. O golpe sai sem ele.");
                        break;
                }
            }

            return efeitos;
        }
    }
}
