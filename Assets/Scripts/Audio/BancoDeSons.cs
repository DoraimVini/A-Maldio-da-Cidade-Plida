using System;
using System.Collections.Generic;
using UnityEngine;

namespace FavelaAmarela.Runtime.Audio
{
    /// <summary>
    /// Catálogo autorado que liga cada <see cref="SomDoJogo"/> a um clipe. Som novo entra
    /// como <b>asset</b>, sem tocar em código — mesma disciplina da <c>TabelaDeDrop</c>.
    ///
    /// <para>Entrada sem clipe não é erro: o <see cref="MixerDeAudio"/> cai numa síntese
    /// procedural, para o jogo ter retorno sonoro <b>antes</b> de existir arte de áudio.</para>
    /// </summary>
    [CreateAssetMenu(menuName = "Favela Amarela/Banco de Sons", fileName = "BancoDeSons")]
    public sealed class BancoDeSons : ScriptableObject
    {
        /// <summary>Uma linha do catálogo: qual som, quais clipes, com que variação.</summary>
        [Serializable]
        public sealed class Entrada
        {
            public SomDoJogo Som;

            [Tooltip("Clipes possíveis. Mais de um = variação a cada disparo, para não enjoar. [ASSET]")]
            public AudioClip[] Clipes;

            [Range(0f, 1f)]
            [Tooltip("Volume base deste som.")]
            public float Volume = 1f;

            [Range(0f, 0.5f)]
            [Tooltip("Variação aleatória de tom, para dois disparos seguidos não soarem idênticos.")]
            public float VariacaoDeTom = 0.08f;
        }

        [Tooltip("O catálogo. Entradas sem clipe caem na síntese procedural.")]
        [SerializeField] private List<Entrada> entradas = new List<Entrada>();

        private Dictionary<SomDoJogo, Entrada> _porSom;

        /// <summary>Devolve a entrada autorada para este som, ou <c>null</c> se não houver.</summary>
        public Entrada Buscar(SomDoJogo som)
        {
            if (_porSom == null)
            {
                _porSom = new Dictionary<SomDoJogo, Entrada>();
                foreach (var e in entradas)
                    if (e != null) _porSom[e.Som] = e;
            }

            return _porSom.TryGetValue(som, out var achado) ? achado : null;
        }
    }
}
