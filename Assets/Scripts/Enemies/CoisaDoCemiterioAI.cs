using UnityEngine;
using FavelaAmarela.Core.Enemies;
using FavelaAmarela.Core.Stealth;
using FavelaAmarela.Runtime.GameLoop;

namespace FavelaAmarela.Runtime.Enemies
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour) da Coisa do Cemitério (bestiário item 5, ver
    /// <c>coisa_do_cemiterio.md</c>). Diferente do <see cref="CultistaAI"/>, não tem
    /// patrulha nem reage a golpe de arma física — a imunidade é "de graça": o
    /// resolvedor de golpe (<c>MaoFisicaBridge</c>) só procura por
    /// <see cref="CultistaAI"/> nos colisores atingidos, então esta classe nunca é
    /// encontrada ali. O toque no jogador (Collider2D marcado como "Is Trigger" no
    /// Inspector) força o Colapso instantâneo, reaproveitando o mesmo método usado
    /// por <c>ColapsoTrigger</c>.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(Rigidbody2D))]
    [AddComponentMenu("Favela Amarela/Enemies/Coisa Do Cemiterio AI")]
    public sealed class CoisaDoCemiterioAI : MonoBehaviour
    {
        private CoisaDoCemiterioFSM _fsm;
        private SpriteRenderer _spriteRenderer;
        private Rigidbody2D _rb;
        private SoundBroadcastService _soundBroadcaster;

        [Header("Configurações")]
        [SerializeField] private float velocidadeFarejando = 1.2f;
        [SerializeField] private float velocidadeAlvoPreciso = 2.5f;
        [SerializeField] private float duracaoAlvoPreciso = 6f;

        [Header("Feedback Visual")]
        [SerializeField] private Color corFarejando = Color.white;
        [SerializeField] private Color corAlvoPreciso = new Color(0.6f, 0.1f, 0.1f);

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            _fsm = new CoisaDoCemiterioFSM(duracaoAlvoPreciso);
            _fsm.OnStateChanged += HandleStateChanged;

            AtualizarVisual(_fsm.CurrentState);
        }

        private void FixedUpdate()
        {
            _fsm.Tick(Time.fixedDeltaTime);

            if (!_fsm.UltimaOrigemConhecida.HasValue)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            float velocidade = _fsm.CurrentState == CoisaDoCemiterioState.AlvoPreciso
                ? velocidadeAlvoPreciso
                : velocidadeFarejando;

            MoverEmDirecaoA(_fsm.UltimaOrigemConhecida.Value, velocidade);
        }

        private void MoverEmDirecaoA(Vector2 alvo, float velocidade)
        {
            Vector2 direcao = (alvo - (Vector2)transform.position).normalized;
            _rb.linearVelocity = direcao * velocidade;
        }

        /// <summary>
        /// Injeta o serviço de som (chamado por <c>GameManager.InjetarDependencias()</c>
        /// no bootstrap, antes do Awake/OnEnable deste componente — mesma garantia de
        /// ordem que já existe para <c>PlayerMovement.Bind()</c>). Substitui a busca por
        /// <c>GameManager.Instance</c> a cada (des)inscrição.
        /// </summary>
        public void Bind(SoundBroadcastService soundBroadcaster) => _soundBroadcaster = soundBroadcaster;

        private void OnEnable()
        {
            if (_soundBroadcaster != null)
            {
                _soundBroadcaster.OnSomEmitido += HandleSomEmitido;
            }
        }

        private void OnDisable()
        {
            if (_soundBroadcaster != null)
            {
                _soundBroadcaster.OnSomEmitido -= HandleSomEmitido;
            }
        }

        private void HandleSomEmitido(SomEmitido som)
        {
            float distancia = Vector2.Distance(transform.position, som.Origem);
            _fsm.ReceberEstimuloSonoro(som.Origem, distancia, som.RaioEfetivo);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collision.CompareTag("Player")) return;

            var mente = collision.GetComponentInParent<FavelaAmarela.Runtime.Combat.ResilienciaBridge>();
            if (mente == null) return;

            // A cutscene (ex.: queda Z4→Z5) é respeitada DENTRO da bridge: ela ignora Colapso
            // forçado enquanto IgnorarTrauma estiver ativo. Antes, este if vivia aqui e em mais
            // duas fontes de morte instantânea — três cópias da mesma regra.
            mente.ForcarColapso();
        }

        private void HandleStateChanged(CoisaDoCemiterioState anterior, CoisaDoCemiterioState atual)
        {
            AtualizarVisual(atual);
        }

        private void AtualizarVisual(CoisaDoCemiterioState estado)
        {
            _spriteRenderer.color = estado == CoisaDoCemiterioState.AlvoPreciso ? corAlvoPreciso : corFarejando;
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
