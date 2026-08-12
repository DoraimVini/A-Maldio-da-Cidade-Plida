using UnityEngine;
using UnityEngine.Serialization;
using FavelaAmarela.Core.Combat;

namespace FavelaAmarela.Core.Combat
{
    /// <summary>
    /// ScriptableObject que define os atributos base de uma unidade (jogador ou inimigo).
    /// Usado por VitalidadeBridge e EnemyBase para criar a FichaDeAtributos.
    ///
    /// <para><b>Sobre os <c>[FormerlySerializedAs]</c>:</b> os <c>.asset</c> deste tipo foram
    /// autorados quando os campos eram <c>camelCase</c> (<c>vitalidadeMax</c>, <c>ataque</c>…),
    /// e os campos em C# viraram <c>PascalCase</c> depois, sem migração. A Unity casa dado
    /// serializado com campo <b>por nome exato</b>: sem estes atributos, toda ficha do projeto
    /// ignorava silenciosamente os valores do disco e caía nos defaults desta classe — o
    /// Byakhee lutava com 100 de Vitalidade em vez dos 500 autorados, e com 0 de Resistência
    /// Anômala em vez de 12. Nada disso aparecia no console. Não remova os atributos sem antes
    /// reescrever os <c>.asset</c>.</para>
    /// </summary>
    [CreateAssetMenu(menuName = "Favela Amarela/Ficha de Atributos", fileName = "Ficha_")]
    public class FichaAtributosConfig : ScriptableObject
    {
        [Header("Atributos Primários")]
        [Tooltip("Vitalidade Corpórea máxima (a 'carne').")]
        [FormerlySerializedAs("vitalidadeMax")]
        public float VitalidadeMax = 100f;

        [Tooltip("Resiliência Mental máxima (a 'mente') — teto do canal anômalo. " +
                 "0 = a unidade não tem mente a ferir e ignora todo Trauma de Anomalia. " +
                 "A Resiliência de Damião NÃO vem daqui: ela é criada pelo GameManager.")]
        [FormerlySerializedAs("resilienciaMax")]
        public float ResilienciaMax = 0f;

        [Header("Combate")]
        [Tooltip("Dano físico base (usado por inimigos para calcular dano causado).")]
        [FormerlySerializedAs("ataque")]
        public float Ataque = 24f;

        [Tooltip("Defesa física (reduz dano recebido).")]
        [FormerlySerializedAs("defesa")]
        public float Defesa = 5f;

        [Tooltip("Conjuração (dano anômalo base, usado por inimigos que lançam magias).")]
        [FormerlySerializedAs("conjuracao")]
        public float Conjuracao = 0f;

        [Tooltip("Resistência a dano anômalo (mitiga o Trauma de Anomalia recebido).")]
        [FormerlySerializedAs("resistenciaAnomala")]
        public float ResistenciaAnomala = 0f;

        [Header("Movimento (inimigos)")]
        [Tooltip("Velocidade de patrulha (errante).")]
        [FormerlySerializedAs("velocidadeErrante")]
        public float VelocidadeErrante = 1.5f;

        [Tooltip("Velocidade de caça (perseguição).")]
        [FormerlySerializedAs("velocidadeCaca")]
        public float VelocidadeCaca = 3.5f;

        [Tooltip("Alcance do golpe corpo-a-corpo.")]
        [FormerlySerializedAs("alcanceDeGolpe")]
        public float AlcanceDeGolpe = 1.2f;

        [Tooltip("Cadência de ataque em segundos.")]
        [FormerlySerializedAs("cadenciaDeAtaque")]
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
                cadenciaDeAtaque: CadenciaDeAtaque,
                resilienciaMax: ResilienciaMax
            );
        }
    }
}
