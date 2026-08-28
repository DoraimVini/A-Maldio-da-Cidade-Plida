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

        [Header("Escala por nível")]
        [Tooltip("Quanto a Vitalidade cresce por nível, em fração do valor base. 0,30 = +30% " +
                 "do valor de nível 1 a cada nível. Nível 1 vale exatamente o número acima.")]
        [Min(0f)]
        public float VitalidadePorNivel = 0.30f;

        [Tooltip("Quanto o Ataque cresce por nível, em fração do valor base.")]
        [Min(0f)]
        public float AtaquePorNivel = 0.25f;

        [Tooltip("Quanto a Defesa cresce por nível. Cresce MAIS DEVAGAR que o ataque de " +
                 "propósito: defesa subtrai e ataque multiplica, então casá-las no mesmo passo " +
                 "faria o combate travar no meio da curva.")]
        [Min(0f)]
        public float DefesaPorNivel = 0.15f;

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
        public FichaDeAtributos CriarFicha() => CriarFicha(1);

        /// <summary>
        /// Cria a ficha <b>no nível pedido</b>, aplicando a lei única de
        /// <see cref="FavelaAmarela.Core.Progression.EscalaDeNivel"/>.
        ///
        /// <para><b>Por que existe (2026-08-28).</b> O Vini pediu que a escala cresça com o jogo
        /// e com o personagem — "saber que ele no nível 2 está mais forte e com mais defesa".
        /// Sem uma lei única cada sistema inventaria a sua, e as duas divergiriam em silêncio.</para>
        ///
        /// <para><b>Nível 1 devolve exatamente o autorado</b>, o que permitiu ligar a escala sem
        /// reescrever um único <c>.asset</c> nem rebalancear o jogo inteiro junto.</para>
        ///
        /// <para>Conjuração e Resistência Anômala <b>não escalam</b> por enquanto: o canal
        /// anômalo é do desenho de cada chefe (o Abdul conjura 25, o Byakhee 20) e escalá-lo
        /// junto tornaria o Trauma incontrolável antes de existir defesa contra ele no jogador —
        /// que só passou a existir hoje, com a <c>DefesaAnomalia</c>.</para>
        /// </summary>
        public FichaDeAtributos CriarFicha(int nivel)
        {
            var escala = new System.Func<float, float, float>(
                (valor, ganho) =>
                    FavelaAmarela.Core.Progression.EscalaDeNivel.Valor(valor, ganho, nivel));

            return new FichaDeAtributos(
                vitalidadeMax: escala(VitalidadeMax, VitalidadePorNivel),
                ataque: escala(Ataque, AtaquePorNivel),
                defesa: escala(Defesa, DefesaPorNivel),
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
