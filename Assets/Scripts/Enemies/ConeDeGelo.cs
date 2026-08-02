using UnityEngine;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Player;

namespace FavelaAmarela.Runtime.Enemies
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Projétil <b>Cone de Gelo</b> conjurado por Abdul na
    /// Fase 2. Viaja em linha reta e, ao acertar Damião, causa <b>dano anômalo</b> na
    /// Resiliência Mental e aplica um <b>acúmulo de frio</b> — três acúmulos congelam.
    ///
    /// <para>Segue o canal anômalo da <see cref="FavelaAmarela.Core.Combat.FichaDeAtributos"/>:
    /// <c>Conjuracao</c> do lançador mitigada pela <c>ResistenciaAnomala</c> do alvo, ferindo
    /// a <b>Resiliência Mental</b> (não a Vitalidade) — é magia, não pancada.</para>
    ///
    /// <para>Autodestrói ao acertar, ao bater em obstáculo ou por tempo de vida, para não
    /// vazar objetos numa luta longa.</para>
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    [AddComponentMenu("Favela Amarela/Enemies/Cone de Gelo")]
    public sealed class ConeDeGelo : MonoBehaviour
    {
        [Header("Movimento")]
        [Tooltip("Velocidade do projétil em unidades por segundo.")]
        [SerializeField] private float velocidade = 6f;

        [Tooltip("Segundos até sumir sozinho, se não acertar nada.")]
        [SerializeField] private float tempoDeVida = 4f;

        [Header("Efeito")]
        [Tooltip("Dano anômalo bruto (vem da Conjuração da ficha de quem lançou).")]
        [SerializeField] private float danoAnomalo = 25f;

        [Tooltip("Camadas que bloqueiam o projétil (paredes). O alvo é achado por componente.")]
        [SerializeField] private LayerMask camadasQueBloqueiam;

        private Rigidbody2D _rb;
        private Vector2 _direcao;
        private float _tempoRestante;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = GetComponent<Collider2D>();
            col.isTrigger = true; // atravessa até achar o alvo; paredes tratadas abaixo

            _tempoRestante = tempoDeVida;
        }

        /// <summary>
        /// Configura o projétil no instante da conjuração: direção e dano vindos de quem
        /// lançou. Chamado por <c>AbdulAlhazredAI</c> logo após instanciar.
        /// </summary>
        /// <param name="direcao">Direção de viagem (será normalizada).</param>
        /// <param name="danoAnomaloBruto">Conjuração da ficha do lançador.</param>
        public void Lancar(Vector2 direcao, float danoAnomaloBruto)
        {
            _direcao = direcao.sqrMagnitude > 0.0001f ? direcao.normalized : Vector2.right;
            if (danoAnomaloBruto > 0f) danoAnomalo = danoAnomaloBruto;

            // Gira o sprite na direção de viagem (o cone aponta para onde vai).
            float angulo = Mathf.Atan2(_direcao.y, _direcao.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angulo);
        }

        private void FixedUpdate()
        {
            _rb.linearVelocity = _direcao * velocidade;

            _tempoRestante -= Time.fixedDeltaTime;
            if (_tempoRestante <= 0f) Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Parede: o cone se desfaz sem efeito.
            if ((camadasQueBloqueiam.value & (1 << other.gameObject.layer)) != 0)
            {
                Destroy(gameObject);
                return;
            }

            if (!other.CompareTag("Player")) return;

            AplicarNoDamiao(other);
            Destroy(gameObject);
        }

        private void AplicarNoDamiao(Collider2D alvo)
        {
            // 1. Acúmulo de frio — 3 congelam.
            var congelamento = alvo.GetComponentInParent<CongelamentoBridge>();
            if (congelamento != null) congelamento.AplicarAcumulo();

            // 2. Dano anômalo na Resiliência Mental, mitigado pela Resistência Anômala.
            //    O GameManager é dono da ResilienciaMental (POCO), então o dreno passa por ele.
            var gm = FavelaAmarela.Runtime.GameLoop.GameManager.Instance;
            if (gm == null || gm.Resiliencia == null) return;

            float resistencia = ObterResistenciaAnomala(alvo);
            float danoFinal = MitigacaoDeDano.Aplicar(danoAnomalo, resistencia);
            if (danoFinal > 0f) gm.Resiliencia.SofrerTrauma(danoFinal);
        }

        private static float ObterResistenciaAnomala(Collider2D alvo)
        {
            var vitalidade = alvo.GetComponentInParent<FavelaAmarela.Runtime.Combat.VitalidadeBridge>();
            return vitalidade?.Atributos?.ResistenciaAnomala ?? 0f;
        }
    }
}
