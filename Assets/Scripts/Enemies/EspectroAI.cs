using UnityEngine;
using FavelaAmarela.Core.Enemies;

namespace FavelaAmarela.Runtime.Enemies
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Adaptador do Espectro: injeta a
    /// <see cref="EspectroFSM"/> e traduz seus estados em visual e movimento.
    /// Diferente da <see cref="CultistaAI"/> (que reage a som sozinha), esta
    /// classe é dirigida externamente — um diretor de cutscene (ex.: o cerco
    /// da Zona 4) chama <see cref="Manifestar"/> e <see cref="IniciarCerco"/>
    /// na hora certa.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(Rigidbody2D))]
    public sealed class EspectroAI : MonoBehaviour
    {
        /// <summary>
        /// Contorno de obstáculos, quando este objeto tem um <c>SeguidorDeCaminho</c>.
        ///
        /// <para><b>Opcional de propósito (2026-09-01):</b> sem ele o movimento continua sendo o
        /// de sempre, em linha reta. Um prefab esquecido degrada para o comportamento antigo,
        /// não para unidade parada.</para>
        /// </summary>
        private FavelaAmarela.Runtime.Navegacao.SeguidorDeCaminho _seguidorDeCaminho;

        [Header("Configurações")]
        [SerializeField] private float velocidadeCerco = 2.0f;
        [SerializeField] private float distanciaParada = 0.15f;
        [SerializeField] private Color corLatente = new Color(1f, 1f, 1f, 0f);
        [SerializeField] private Color corManifestando = new Color(0.9f, 0.85f, 0.3f, 0.85f);

        private EspectroFSM _fsm;
        private SpriteRenderer _spriteRenderer;
        private Rigidbody2D _rb;
        private Vector2 _alvoCerco;

        /// <summary>A FSM do Espectro, para o <see cref="AnimadorDoEspectro"/> observar.</summary>
        public EspectroFSM Fsm => _fsm;

        private void Awake()
        {
            _seguidorDeCaminho = GetComponent<FavelaAmarela.Runtime.Navegacao.SeguidorDeCaminho>();

            _spriteRenderer = GetComponent<SpriteRenderer>();
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Fantasma atravessa geometria. Kinematic movido por velocidade passa por
            // colliders estáticos (paredes, barreira de anomalia) sem resposta de colisão
            // — mais robusto que excludeLayers, que as paredes anulam via ForceReceiveLayers.
            _rb.bodyType = RigidbodyType2D.Kinematic;

            _fsm = new EspectroFSM(EspectroState.Latente);
            _fsm.OnStateChanged += HandleStateChanged;

            AtualizarVisual(_fsm.CurrentState);
        }

        /// <summary>Faz o Espectro materializar visualmente (Latente → Manifestando).</summary>
        public void Manifestar()
        {
            _fsm.TryTransition(EspectroState.Manifestando);
        }

        /// <summary>Inicia o avanço em direção a <paramref name="alvo"/> (Manifestando → Cercando).</summary>
        public void IniciarCerco(Vector2 alvo)
        {
            _alvoCerco = alvo;
            _fsm.TryTransition(EspectroState.Cercando);
        }

        private void FixedUpdate()
        {
            if (_fsm.CurrentState != EspectroState.Cercando) return;

            Vector2 posicaoAtual = _rb.position;
            if (Vector2.Distance(posicaoAtual, _alvoCerco) <= distanciaParada)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 direcao = _seguidorDeCaminho != null
                ? _seguidorDeCaminho.DirecaoPara(_alvoCerco)
                : (_alvoCerco - posicaoAtual).normalized;
            _rb.linearVelocity = direcao * velocidadeCerco;
        }

        private void HandleStateChanged(EspectroState anterior, EspectroState atual)
        {
            AtualizarVisual(atual);
        }

        private void AtualizarVisual(EspectroState estado)
        {
            _spriteRenderer.color = estado switch
            {
                EspectroState.Latente => corLatente,
                _ => corManifestando,
            };
        }

        private void OnDestroy()
        {
            if (_fsm != null)
            {
                _fsm.OnStateChanged -= HandleStateChanged;
            }
        }
    }
}
