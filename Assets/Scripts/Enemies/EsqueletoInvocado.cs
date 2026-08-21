using UnityEngine;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Core.Companion;
using FavelaAmarela.Runtime.Combat;

namespace FavelaAmarela.Runtime.Enemies
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). <b>Esqueleto invocado</b> por Abdul durante a luta.
    /// Não é um Cultista: não tem stealth, não caça por som, não patrulha — ele já nasce
    /// sabendo onde Damião está e vai direto nele. O papel dele é <b>pressão</b>, para o
    /// jogador não conseguir procurar Pedras de Poder com calma.
    ///
    /// <para>Frágil de propósito (poucos golpes o derrubam) e <b>expira sozinho</b>: sem
    /// tempo de vida, uma luta longa acumularia dezenas deles e viraria uma multidão
    /// impossível em vez de pressão pontual.</para>
    ///
    /// <para>Reaproveita <see cref="SeguidorDeAlvo"/> (mesma peça do companheiro Yug-Neth) —
    /// a diferença é a distância de conforto zero: ele quer encostar, não acompanhar.</para>
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(Collider2D))]
    [AddComponentMenu("Favela Amarela/Enemies/Esqueleto Invocado")]
    public sealed class EsqueletoInvocado : MonoBehaviour, IDanificavel
    {
        [Header("Atributos")]
        [Tooltip("Vitalidade do esqueleto — frágil de propósito.")]
        [SerializeField] private float vitalidadeMax = 30f;

        [Tooltip("Dano por golpe no Damião.")]
        [SerializeField] private float ataque = 12f;

        [Tooltip("Defesa (mitiga golpes recebidos). 0 = cai com qualquer arma.")]
        [SerializeField] private float defesa = 0f;

        [Header("Perseguição")]
        [Tooltip("Velocidade de perseguição.")]
        [SerializeField] private float velocidade = 2.2f;

        [Tooltip("Distância para acertar o Damião.")]
        [SerializeField] private float alcanceDeGolpe = 0.9f;

        [Tooltip("Segundos entre golpes.")]
        [SerializeField] private float cadenciaDeAtaque = 1.5f;

        [Header("Tempo de vida")]
        [Tooltip("Segundos até virar pó sozinho. Impede que a arena encha de esqueletos numa luta longa.")]
        [SerializeField] private float tempoDeVida = 20f;

        [Header("Feedback")]
        [SerializeField] private bool mostrarNumerosDeDano = true;
        [SerializeField] private Color corDoDano = new Color(0.9f, 0.9f, 0.85f);

        private Rigidbody2D _rb;
        private Vitalidade _vitalidade;
        private SeguidorDeAlvo _perseguidor;
        private Transform _alvo;
        private VitalidadeBridge _vitalidadeDoAlvo;
        private float _tempoDesdeUltimoGolpe;
        private float _tempoRestante;

        /// <summary>Esqueleto comum, não é boss — leva crítico furtivo normalmente.</summary>
        public bool EhAparicaoPrimordial => false;

        private void Awake()
        {
            // Área atingível derivada do sprite — invocado em runtime: sem isto nasceria sem área atingível.
            // A garantia vive aqui, no código, e não numa lista de prefabs: listas
            // escritas à mão são o modo de falha mais repetido deste projeto.
            FavelaAmarela.Runtime.Combat.Hurtbox.GarantirPara(gameObject, "EnemyHurtbox");

            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            _vitalidade = new Vitalidade(vitalidadeMax > 0f ? vitalidadeMax : 30f);
            // Distância de conforto zero: ele quer encostar, não manter distância.
            _perseguidor = new SeguidorDeAlvo(distanciaDeConforto: 0f, velocidade: velocidade);
            _tempoRestante = tempoDeVida;
        }

        /// <summary>
        /// Injeta quem perseguir (chamado por <c>AbdulAlhazredAI</c> ao invocar). Sem alvo,
        /// o esqueleto fica parado — nunca busca por tag, seguindo a convenção do Runtime.
        /// </summary>
        public void Bind(Transform alvo)
        {
            _alvo = alvo;
            if (alvo != null) _vitalidadeDoAlvo = alvo.GetComponentInParent<VitalidadeBridge>();
        }

        private void FixedUpdate()
        {
            _tempoRestante -= Time.fixedDeltaTime;
            if (_tempoRestante <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            if (_alvo == null)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            float distancia = Vector2.Distance(transform.position, _alvo.position);

            if (distancia <= alcanceDeGolpe)
            {
                _rb.linearVelocity = Vector2.zero;
                TentarGolpear(Time.fixedDeltaTime);
                return;
            }

            _tempoDesdeUltimoGolpe = 0f;
            _rb.linearVelocity = _perseguidor.CalcularVelocidade(transform.position, _alvo.position);
        }

        private void TentarGolpear(float dt)
        {
            _tempoDesdeUltimoGolpe += dt;
            if (_tempoDesdeUltimoGolpe < cadenciaDeAtaque) return;

            _tempoDesdeUltimoGolpe = 0f;
            // A mitigação pela Defesa do alvo acontece dentro do VitalidadeBridge.
            _vitalidadeDoAlvo?.ReceberDanoFisico(ataque);
        }

        /// <inheritdoc />
        public void ReceberGolpe(ArmaResult resultado)
        {
            if (_vitalidade.EstaAbatido) return;
            if (resultado.Dano <= 0f) return;

            float danoFinal = MitigacaoDeDano.Aplicar(resultado.Dano, defesa);
            if (danoFinal <= 0f) return;

            _vitalidade.Ferir(danoFinal);

            if (mostrarNumerosDeDano)
                DanoFlutuante.Mostrar(transform.position, danoFinal, corDoDano);

            if (_vitalidade.EstaAbatido) Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.9f, 0.9f, 0.85f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, alcanceDeGolpe);
        }
    }
}
