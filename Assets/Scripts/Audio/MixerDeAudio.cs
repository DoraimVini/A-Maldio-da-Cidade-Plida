using UnityEngine;

namespace FavelaAmarela.Runtime.Audio
{
    /// <summary>
    /// Camada Runtime. Ponto único por onde todo som do jogo passa. Mantém um <b>pool fixo</b>
    /// de <c>AudioSource</c>, criado uma vez no <c>Awake</c> — tocar um som nunca instancia
    /// nada (Regra de Ouro 1: sem lixo em hot path).
    ///
    /// <para>Se o <see cref="BancoDeSons"/> não tiver clipe para o som pedido, cai na
    /// <see cref="SinteseDeSom"/>. Assim o jogo <b>soa desde já</b>, e passa a soar melhor
    /// sozinho conforme o áudio real for autorado.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Audio/Mixer de Áudio")]
    public sealed class MixerDeAudio : MonoBehaviour
    {
        /// <summary>Instância única. Nula fora de cena — todo chamador deve tolerar isso.</summary>
        public static MixerDeAudio Instancia { get; private set; }

        [Header("Catálogo")]
        [Tooltip("Banco de sons autorado. Vazio = tudo sintetizado. [ASSET]")]
        [SerializeField] private BancoDeSons banco;

        [Header("Pool")]
        [Tooltip("Quantos sons podem soar ao mesmo tempo. Excedente é descartado, não alocado.")]
        [Min(1)]
        [SerializeField] private int vozes = 12;

        [Header("Mistura")]
        [Range(0f, 1f)]
        [Tooltip("Volume geral do gameplay.")]
        [SerializeField] private float volumeGeral = 0.8f;

        [Tooltip("Distância a partir da qual o som começa a atenuar.")]
        [Min(0.1f)]
        [SerializeField] private float distanciaMinima = 3f;

        [Tooltip("Distância em que o som some de vez.")]
        [Min(1f)]
        [SerializeField] private float distanciaMaxima = 24f;

        private AudioSource[] _pool;
        private int _proxima;

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }

            Instancia = this;
            MontarPool();
        }

        private void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
        }

        private void MontarPool()
        {
            _pool = new AudioSource[vozes];

            for (int i = 0; i < vozes; i++)
            {
                var go = new GameObject($"Voz_{i}");
                go.transform.SetParent(transform, false);

                var fonte = go.AddComponent<AudioSource>();
                fonte.playOnAwake = false;
                fonte.spatialBlend = 1f;                 // 3D: som tem lugar no mundo
                fonte.rolloffMode = AudioRolloffMode.Linear;
                fonte.minDistance = distanciaMinima;
                fonte.maxDistance = distanciaMaxima;

                _pool[i] = fonte;
            }
        }

        /// <summary>
        /// Toca um som numa posição do mundo.
        /// </summary>
        /// <param name="som">Qual som.</param>
        /// <param name="posicao">Onde ele acontece.</param>
        /// <param name="escalaDeVolume">Multiplicador extra (ex.: o quanto o passo foi alto).</param>
        public void Tocar(SomDoJogo som, Vector3 posicao, float escalaDeVolume = 1f)
        {
            if (_pool == null || _pool.Length == 0) return;

            var entrada = banco != null ? banco.Buscar(som) : null;

            AudioClip clipe = EscolherClipe(entrada);
            if (clipe == null) clipe = SinteseDeSom.Obter(som);
            if (clipe == null) return;

            float volume = volumeGeral * escalaDeVolume * (entrada != null ? entrada.Volume : 1f);
            if (volume <= 0.001f) return;

            var fonte = ProximaVozLivre();
            fonte.transform.position = posicao;
            fonte.clip = clipe;
            fonte.volume = Mathf.Clamp01(volume);

            float variacao = entrada != null ? entrada.VariacaoDeTom : 0.08f;
            fonte.pitch = 1f + Random.Range(-variacao, variacao);

            fonte.Play();
        }

        private static AudioClip EscolherClipe(BancoDeSons.Entrada entrada)
        {
            if (entrada?.Clipes == null || entrada.Clipes.Length == 0) return null;

            // Mais de um clipe = variação, para dois disparos seguidos não soarem idênticos.
            return entrada.Clipes[Random.Range(0, entrada.Clipes.Length)];
        }

        /// <summary>
        /// Primeira voz parada; se todas estiverem ocupadas, reusa em rodízio — um som novo
        /// vale mais que um som velho terminando.
        /// </summary>
        private AudioSource ProximaVozLivre()
        {
            for (int i = 0; i < _pool.Length; i++)
                if (!_pool[i].isPlaying) return _pool[i];

            _proxima = (_proxima + 1) % _pool.Length;
            return _pool[_proxima];
        }
    }
}
