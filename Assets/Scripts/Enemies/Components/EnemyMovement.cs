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

        /// <summary>
        /// Se a IA quer estar se movendo agora. Separado de <see cref="_velocidadeAlvo"/>
        /// porque "quero parar" e "ainda não decidi" são coisas diferentes.
        /// </summary>
        private bool _querendoMover;

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

        /// <summary>
        /// <b>O único lugar do componente que escreve no corpo.</b>
        ///
        /// <para><b>Por que isto mudou em 2026-09-04.</b> O caminho sem aceleração escrevia
        /// <c>linearVelocity</c> dentro de <see cref="MoverPara"/> — que é chamado do
        /// <c>Update</c> pela <c>EnemyStateMachine</c>, pelo <c>AvatarDeSetAI</c> e pelo
        /// <c>SsethFarejadorAI</c>. Escrever velocidade em ritmo VARIÁVEL para ser consumida em
        /// ritmo FIXO (50 Hz) faz a mesma decisão valer por dois passos de física num quadro
        /// rápido, e duas decisões se atropelarem num quadro lento — a segunda escrita descarta
        /// a primeira antes de ela ter existido. Como este é o componente de movimento
        /// COMPARTILHADO, o defeito valia para boa parte do elenco de uma vez.</para>
        ///
        /// <para>A decisão continua no <c>Update</c>, que é onde a IA pensa; só a escrita
        /// desceu para cá. <c>MoverPara</c> passou a apenas <b>guardar</b> o que quer.</para>
        /// </summary>
        private void FixedUpdate()
        {
            if (!_querendoMover)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 desejada = _velocidadeAlvo * _multiplicadorAlvo;

            _rb.linearVelocity = usarAceleracao
                ? Vector2.MoveTowards(_rb.linearVelocity, desejada,
                                      aceleracao * Time.fixedDeltaTime)
                : desejada;
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

            // GUARDA, nao escreve. Quem escreve e o FixedUpdate -- ver o doc dele.
            _velocidadeAlvo = direcao;
            _multiplicadorAlvo = velocidade > 0f ? velocidade : velocidadeErrante;
            _querendoMover = true;

            // O flip e do RENDERER, nao do corpo: pode continuar aqui, no ritmo do quadro.
            if (direcao.x != 0f) _sr.flipX = direcao.x < 0f;
        }

        public void Parar()
        {
            _querendoMover = false;
            _velocidadeAlvo = Vector2.zero;

            // Zera TAMBEM aqui, e nao so no FixedUpdate: EnemyStateMachine.EnterState chama
            // Parar() ao entrar em Patrol justamente porque sair de Chase com a velocidade
            // cravada fazia o inimigo deslizar em linha reta para fora da cena. Esperar ate
            // 20 ms para zerar reintroduziria uma versao curta disso.
            _rb.linearVelocity = Vector2.zero;

            // Esquece o caminho junto: parar e depois retomar com um caminho velho faria a
            // unidade andar para onde o alvo ESTAVA.
            if (_seguidor != null) _seguidor.Limpar();
        }
    }
}
