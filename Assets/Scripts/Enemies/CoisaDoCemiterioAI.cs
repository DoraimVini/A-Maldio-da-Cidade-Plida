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

        private void OnEnable()
        {
            if (GameManager.Instance != null && GameManager.Instance.SoundBroadcaster != null)
            {
                GameManager.Instance.SoundBroadcaster.OnSomEmitido += HandleSomEmitido;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null && GameManager.Instance.SoundBroadcaster != null)
            {
                GameManager.Instance.SoundBroadcaster.OnSomEmitido -= HandleSomEmitido;
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
            if (GameManager.Instance == null || GameManager.Instance.Resiliencia == null) return;
            // Se o Damião está preso numa cutscene (ex.: queda Z4→Z5), só tensão, sem morte.
            if (GameManager.Instance.JogadorInvulneravel) return;

            GameManager.Instance.Resiliencia.ForcarColapso();
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
