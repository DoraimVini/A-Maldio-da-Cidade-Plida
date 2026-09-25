using UnityEngine;

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
        }

        private void FixedUpdate()
        {
            if (!usarAceleracao || _velocidadeAlvo == Vector2.zero) return;
            _rb.linearVelocity = Vector2.MoveTowards(_rb.linearVelocity,
                _velocidadeAlvo * _multiplicadorAlvo, aceleracao * Time.fixedDeltaTime);
        }

        public void MoverPara(Vector2 alvo, float velocidade = -1f)
        {
            Vector2 direcao = alvo - (Vector2)transform.position;
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
        }
    }
}
