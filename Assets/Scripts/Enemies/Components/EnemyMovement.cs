using UnityEngine;
using FavelaAmarela.Runtime.Navegacao;

namespace FavelaAmarela.Runtime.Enemies
{
    [RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
    public class EnemyMovement : MonoBehaviour
    {
        [Header("Velocidades")]
        [SerializeField] private float velocidadeErrante = 1.5f;
        [SerializeField] private float velocidadeCaca = 3.5f;
        [SerializeField] private bool usarAceleracao = false;
        [SerializeField] private float aceleracao = 10f;

        private Rigidbody2D _rb;
        private SpriteRenderer _sr;
        private SeguidorDeCaminho _seguidor;
        private Vector2 _velocidadeAlvo;
        private float _multiplicadorAlvo;

        public float VelocidadeErrante => velocidadeErrante;
        public float VelocidadeCaca => velocidadeCaca;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _sr = GetComponent<SpriteRenderer>();
            _rb.gravityScale = 0f;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Opcional de propósito: sem ele o movimento continua sendo o de sempre, em linha
            // reta. Assim esta peça entra sem exigir que todo prefab de inimigo seja tocado no
            // mesmo commit -- e um prefab esquecido degrada para o comportamento antigo, não
            // para inimigo parado.
            _seguidor = GetComponent<SeguidorDeCaminho>();
        }

        private void FixedUpdate()
        {
            if (!usarAceleracao || _velocidadeAlvo == Vector2.zero) return;
            _rb.linearVelocity = Vector2.MoveTowards(_rb.linearVelocity,
                _velocidadeAlvo * _multiplicadorAlvo, aceleracao * Time.fixedDeltaTime);
        }

        /// <summary>
        /// Anda em direção a <paramref name="alvo"/>, <b>contornando</b> o que houver no
        /// caminho quando há um <see cref="SeguidorDeCaminho"/> neste objeto.
        ///
        /// <para><b>O que isto conserta (2026-09-01).</b> A direção era sempre a reta até o
        /// alvo. Num plano aberto — que é o Deserto de Hali hoje — funciona; com qualquer
        /// geometria, o perseguidor encosta na parede e fica lá empurrando, o que em playtest
        /// se lê como "a IA travou".</para>
        /// </summary>
        public void MoverPara(Vector2 alvo, float velocidade = -1f)
        {
            Vector2 direcao = _seguidor != null
                ? _seguidor.DirecaoPara(alvo)
                : alvo - (Vector2)transform.position;

            if (direcao.sqrMagnitude < 0.0001f) { Parar(); return; }
            direcao.Normalize();

            float vel = velocidade > 0f ? velocidade : velocidadeErrante;
            if (usarAceleracao)
            {
                _velocidadeAlvo = direcao;
                _multiplicadorAlvo = vel;
            }
            else _rb.linearVelocity = direcao * vel;

            if (direcao.x != 0f) _sr.flipX = direcao.x < 0f;
        }

        public void Parar()
        {
            _rb.linearVelocity = Vector2.zero;
            _velocidadeAlvo = Vector2.zero;

            // Esquece o caminho junto: parar e depois retomar com um caminho velho faria a
            // unidade andar para onde o alvo ESTAVA.
            if (_seguidor != null) _seguidor.Limpar();
        }
    }
}
