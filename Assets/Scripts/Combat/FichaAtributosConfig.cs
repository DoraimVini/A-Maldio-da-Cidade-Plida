using UnityEngine;
using FavelaAmarela.Core.Combat;

namespace FavelaAmarela.Core.Combat
{
    /// <summary>
    /// ScriptableObject que define os atributos base de uma unidade (jogador ou inimigo).
    /// Usado por VitalidadeBridge e EnemyBase para criar a FichaDeAtributos.
    /// </summary>
    [CreateAssetMenu(menuName = "Favela Amarela/Ficha de Atributos", fileName = "Ficha_")]
    public class FichaAtributosConfig : ScriptableObject
    {
        [Header("Atributos Primários")]
        [Tooltip("Vitalidade Corpórea máxima (HP).")]
        public float VitalidadeMax = 100f;

        [Tooltip("Resiliência Mental máxima (sanidade). Apenas para o jogador.")]
        public float ResilienciaMax = 100f;

        [Header("Combate")]
        [Tooltip("Dano físico base (usado por inimigos para calcular dano causado).")]
        public float Ataque = 24f;

        [Tooltip("Defesa física (reduz dano recebido).")]
        public float Defesa = 5f;

        [Tooltip("Conjuração (dano anômalo base, usado por inimigos que lançam magias).")]
        public float Conjuracao = 0f;

        [Tooltip("Resistência a dano anômalo.")]
        public float ResistenciaAnomala = 0f;

        [Header("Movimento (inimigos)")]
        [Tooltip("Velocidade de patrulha (errante).")]
        public float VelocidadeErrante = 1.5f;

        [Tooltip("Velocidade de caça (perseguição).")]
        public float VelocidadeCaca = 3.5f;

        [Tooltip("Alcance do golpe corpo-a-corpo.")]
        public float AlcanceDeGolpe = 1.2f;

        [Tooltip("Cadência de ataque em segundos.")]
        public float CadenciaDeAtaque = 1.2f;

        /// <summary>
        /// Cria uma FichaDeAtributos a partir dos valores configurados.
        /// </summary>
        public FichaDeAtributos CriarFicha()
        {
            return new FichaDeAtributos(
                vitalidadeMax: VitalidadeMax,
                ataque: Ataque,
                defesa: Defesa,
                conjuracao: Conjuracao,
                resistenciaAnomala: ResistenciaAnomala,
                velocidadeErrante: VelocidadeErrante,
                velocidadeCaca: VelocidadeCaca,
                alcanceDeGolpe: AlcanceDeGolpe,
                cadenciaDeAtaque: CadenciaDeAtaque
            );
        }
    }
}
