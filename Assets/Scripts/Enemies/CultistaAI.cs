using UnityEngine;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Core.Enemies;
using FavelaAmarela.Core.Stealth;

namespace FavelaAmarela.Runtime.Enemies
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Rigidbody2D))]
    public class CultistaAI : MonoBehaviour, IDanificavel
    {
        private CultistaFSM _fsm;
        private SpriteRenderer _spriteRenderer;
        private Rigidbody2D _rb;
        private PatrolRoute _patrolRoute;
        private SoundBroadcastService _soundBroadcaster;

        [Header("Patrulha")]
        [SerializeField] private Transform[] waypoints;

        [Header("Configurações")]
        [SerializeField] private float velocidadeErrante = 1.0f;
        [SerializeField] private float velocidadeCaca = 3.5f;
        [SerializeField] private Color corErrante = Color.white;
        [SerializeField] private Color corAlerta = Color.yellow;
        [SerializeField] private Color corCaca = Color.red;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            _fsm = new CultistaFSM(CultistaState.Errante);
            _fsm.OnStateChanged += HandleStateChanged;

            if (waypoints != null && waypoints.Length > 0)
            {
                var poses = new Vector2[waypoints.Length];
                for (int i = 0; i < waypoints.Length; i++)
                {
                    if (waypoints[i] == null)
                    {
                        Debug.LogError($"[CultistaAI] Waypoint {i} não está atribuído no Inspector. Usando posição atual como fallback.", this);
                        poses[i] = transform.position;
                        continue;
                    }
                    poses[i] = waypoints[i].position;
                }

                _patrolRoute = new PatrolRoute(poses, loop: true);
            }
            else
            {
                _patrolRoute = new PatrolRoute(new[] { (Vector2)transform.position });
            }

            AtualizarVisual(_fsm.CurrentState);
        }

        private void FixedUpdate()
        {
            _fsm.Tick(Time.fixedDeltaTime);

            switch (_fsm.CurrentState)
            {
                case CultistaState.Errante:
                    if (_patrolRoute != null)
                    {
                        MoverEmDirecaoA(_patrolRoute.AlvoAtual, velocidadeErrante);
                        _patrolRoute.AtualizarChegada(transform.position, 0.3f);
                    }
                    else
                    {
                        _rb.linearVelocity = Vector2.zero;
                    }
                    break;
                case CultistaState.Alerta:
                    // Parado (Pausa telegrafada antes de caçar)
                    // Poderia apenas rotacionar em direção a _fsm.UltimaOrigemConhecida se desejado
                    _rb.linearVelocity = Vector2.zero;
                    break;
                case CultistaState.Caca:
                    if (_fsm.UltimaOrigemConhecida.HasValue)
                    {
                        MoverEmDirecaoA(_fsm.UltimaOrigemConhecida.Value, velocidadeCaca);
                    }
                    else
                    {
                        _rb.linearVelocity = Vector2.zero;
                    }
                    break;
            }
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

        /// <summary>Cultista Amarelo não é boss — leva crítico de furtividade normalmente.</summary>
        public bool EhAparicaoPrimordial => false;

        /// <summary>
        /// Recebe o resultado de um golpe de arma física (via <c>MaoFisicaBridge</c>).
        /// O Cultista não tem barra de vida: só reage ao atordoamento do golpe — o dano
        /// bruto (<see cref="ArmaResult.Dano"/>) é ignorado aqui, importa para alvos com
        /// vida (ex.: uma Aparição Primordial).
        /// </summary>
        public void ReceberGolpe(ArmaResult resultado)
        {
            if (resultado.Atordoou)
            {
                _fsm.AtordoarPor(resultado.DuracaoAtordoamento);
            }
        }

        private void HandleStateChanged(CultistaState anterior, CultistaState atual)
        {
            AtualizarVisual(atual);
        }

        private void AtualizarVisual(CultistaState estado)
        {
            switch (estado)
            {
                case CultistaState.Errante:
                    _spriteRenderer.color = corErrante;
                    break;
                case CultistaState.Alerta:
                    _spriteRenderer.color = corAlerta;
                    break;
                case CultistaState.Caca:
                    _spriteRenderer.color = corCaca;
                    break;
            }
        }

        private void OnDestroy()
        {
            if (_fsm != null)
            {
                _fsm.OnStateChanged -= HandleStateChanged;
            }
        }

        private void OnDrawGizmos()
        {
            if (_fsm == null) return;

            Gizmos.color = _spriteRenderer != null ? _spriteRenderer.color : Color.white;
            Gizmos.DrawWireSphere(transform.position, 1.5f);
            
            // Desenha raio de vibração
            Gizmos.color = new Color(0.5f, 0, 0.5f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, 3f);
        }
    }
}
