using System;
using UnityEngine;
using FavelaAmarela.Core.Companion;
using FavelaAmarela.Core.Enemies;
using FavelaAmarela.Runtime.Combat;

namespace FavelaAmarela.Runtime.Enemies
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). <b>Yug-Neth</b>, o filhote Mi-Go acorrentado por
    /// Abdul Alhazred — companion obrigatório para abrir os Portões de Carcosa (decisão
    /// dos diretores, 2026-07-30). Frágil e passivo: não ataca, não conjura. Toda a
    /// mecânica é <b>proteção</b> — o jogador é quem precisa mantê-lo vivo depois de
    /// libertado.
    ///
    /// <para><b>Três momentos de vida:</b></para>
    /// <list type="bullet">
    ///   <item><b>Cativo</b> (padrão, ao nascer na cena): anda de um lado para o outro
    ///   perto de onde Abdul o prendeu — reaproveita <see cref="PatrolRoute"/> com
    ///   <c>loop: false</c> (ping-pong), mesma peça já usada pelo Cultista em Errante. Não
    ///   segue ninguém, não é alvo de nada durante a luta (ele ainda está sob controle de
    ///   Abdul).</item>
    ///   <item><b>Livre</b>: a partir do instante em que <see cref="Bind"/> é chamado (por
    ///   <c>AbdulAlhazredAI.LibertarYugNeth</c>, nos dois caminhos da conversa — concordar
    ///   ou vencer a luta), passa a seguir quem o libertou via <see cref="SeguidorDeAlvo"/>.</item>
    ///   <item><b>Incapacitado</b>: a Vitalidade dele chegou a zero enquanto livre. Ele
    ///   <b>não morre</b> — cai e fica inerte exatamente onde estava, bloqueando os Portões
    ///   de Carcosa até ser reanimado num <c>RefugioDeLuz</c> (decisão do Vini, 2026-07-31,
    ///   que revoga a regra anterior de "sem resgate, run acaba na hora").</item>
    /// </list>
    ///
    /// <para>Reaproveita <see cref="VitalidadeBridge"/> para vitalidade e dano — mesma
    /// peça que o Damião usa.</para>
    ///
    /// <para><b>Pendência conhecida:</b> uma vez livre, nenhum inimigo mira nele ainda — a
    /// detecção de alvo do Cultista (<c>CultistaAI.DetectarAlvoAoAlcance</c>) só considera
    /// a camada Player. Fazer inimigos escolherem entre Damião e o companheiro é IA de
    /// alvo nova, fora desta fatia — então hoje ele só cai por dano de área/ambiente, não
    /// por ataque direto.</para>
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(VitalidadeBridge))]
    [RequireComponent(typeof(Aliado))]
    [AddComponentMenu("Favela Amarela/Enemies/Yug-Neth")]
    public sealed class YugNethAI : MonoBehaviour
    {
        [Header("Cativeiro (antes de ser libertado)")]
        [Tooltip("Meia-distância do vaivém em torno da posição inicial (anda para um lado, depois para o outro).")]
        [SerializeField] private float raioDeCativeiro = 1.2f;

        [Tooltip("Velocidade do vaivém enquanto cativo.")]
        [SerializeField] private float velocidadeDeCativeiro = 1f;

        [Tooltip("Distância para considerar que chegou numa ponta do vaivém.")]
        [SerializeField] private float raioDeChegada = 0.15f;

        [Header("Seguir quem libertou (depois de livre)")]
        [Tooltip("Distância que o companheiro mantém sem se mover, uma vez livre.")]
        [SerializeField] private float distanciaDeConforto = 1.8f;

        [Tooltip("Velocidade ao se deslocar para alcançar quem o libertou.")]
        [SerializeField] private float velocidadeDeSeguimento = 4f;

        [Header("Incapacitado")]
        [Tooltip("Sprite do Yug-Neth, tingido de cinza enquanto incapacitado. Opcional.")]
        [SerializeField] private SpriteRenderer spriteDoYugNeth;

        [Tooltip("Cor aplicada enquanto incapacitado (inerte, esperando reanimação).")]
        [SerializeField] private Color corIncapacitado = new Color(0.5f, 0.5f, 0.5f, 0.85f);

        private Rigidbody2D _rb;
        private SeguidorDeAlvo _seguidor;
        private PatrolRoute _cativeiro;
        private VitalidadeBridge _vitalidade;
        private Transform _alvo;
        private bool _liberado;
        private bool _incapacitado;
        private Color _corOriginal = Color.white;

        /// <summary>Vitalidade corpórea do companheiro (para HUD/observadores externos).</summary>
        public VitalidadeBridge Vitalidade => _vitalidade;

        /// <summary>Se já foi libertado (segue alguém) ou ainda está cativo (vaivém).</summary>
        public bool Liberado => _liberado;

        /// <summary>
        /// Se está incapacitado (caído, aguardando reanimação num Refúgio). Enquanto true,
        /// os Portões de Carcosa não podem ser atravessados — ele é a chave dimensional.
        /// </summary>
        public bool EstaIncapacitado => _incapacitado;

        /// <summary>
        /// Disparado no instante em que ele cai (Vitalidade zerada). Não é mais game over —
        /// é o gatilho para a UI avisar "leve Yug-Neth a um Refúgio".
        /// </summary>
        public event Action OnIncapacitado;

        /// <summary>Disparado quando ele é reanimado num Refúgio e volta a seguir Damião.</summary>
        public event Action OnReanimado;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            _vitalidade = GetComponent<VitalidadeBridge>();
            _vitalidade.OnAbatido += HandleAbatido;

            // Cativo = intocável. Durante a luta do Abdul ele ainda está sob controle dele
            // e não pode levar dano de fonte nenhuma (decisão do Vini, 2026-07-31): não é
            // só que os inimigos não miram nele — um Cone de Gelo perdido ou uma área de
            // efeito também não podem feri-lo. A vulnerabilidade só liga ao ser libertado,
            // que é quando a mecânica de incapacitação passa a fazer sentido.
            _vitalidade.IgnorarDano = true;

            _seguidor = new SeguidorDeAlvo(distanciaDeConforto, velocidadeDeSeguimento);

            // Vaivém entre dois pontos ao redor da posição de nascimento — mesmo padrão
            // ping-pong do CultistaAI.Errante, só que sem laço (loop: false).
            Vector2 posicaoInicial = transform.position;
            var pontos = new[]
            {
                posicaoInicial + new Vector2(-raioDeCativeiro, 0f),
                posicaoInicial + new Vector2(raioDeCativeiro, 0f),
            };
            _cativeiro = new PatrolRoute(pontos, loop: false);

            if (spriteDoYugNeth == null) spriteDoYugNeth = GetComponent<SpriteRenderer>();
            if (spriteDoYugNeth != null) _corOriginal = spriteDoYugNeth.color;
        }

        private void OnDestroy()
        {
            if (_vitalidade != null)
                _vitalidade.OnAbatido -= HandleAbatido;
        }

        /// <summary>
        /// Liberta Yug-Neth: a partir de agora ele segue <paramref name="alvoASeguir"/> em
        /// vez de vaguear. Chamado por quem o solta (hoje, <c>AbdulAlhazredAI</c>, nos
        /// dois caminhos da conversa — concordar ou vencer a luta) — nunca por busca de
        /// tag na cena, mesmo padrão de injeção (<c>.Bind()</c>) já usado no Runtime.
        /// </summary>
        /// <summary>
        /// Faz o corpo de Yug-Neth deixar de barrar fisicamente um outro colisor — na
        /// prática, o de Damião. Chamado pelo <c>GameManager</c> no bootstrap.
        ///
        /// <para><b>Por que não resolver por layer:</b> ele está na camada <c>Enemy</c>, e a
        /// taxonomia de layers do projeto é um conjunto fechado — criar uma camada "Aliado"
        /// mudaria a matriz de colisão de todo mundo. Ignorar o par de colisores atinge
        /// exatamente o problema (ele parar de te empurrar) sem efeito colateral: ele
        /// continua colidindo com paredes, então segue Damião sem atravessar o cenário.</para>
        /// </summary>
        public void IgnorarColisaoCom(Collider2D outro)
        {
            if (outro == null) return;

            var meus = GetComponents<Collider2D>();
            for (int i = 0; i < meus.Length; i++)
            {
                if (meus[i] == null || meus[i].isTrigger) continue;
                Physics2D.IgnoreCollision(meus[i], outro, true);
            }
        }

        public void Bind(Transform alvoASeguir)
        {
            _alvo = alvoASeguir;
            _liberado = true;

            // Livre = vulnerável. É aqui que a proteção de "protegê-lo" vira mecânica de
            // verdade: a partir de agora ele pode cair e precisar de um Refúgio. O golpe do
            // jogador continua nunca o atingindo — isso é responsabilidade do marcador
            // `Aliado`, não deste estado, porque vale para sempre.
            if (_vitalidade != null) _vitalidade.IgnorarDano = false;
        }

        /// <summary>
        /// Reanima Yug-Neth num Refúgio: cura a Vitalidade ao máximo e ele volta a seguir
        /// Damião. Chamado por <c>RefugioDeLuz</c>. Sem efeito se ele não estiver
        /// incapacitado (idempotente — pisar em vários Refúgios seguidos não faz nada extra).
        /// </summary>
        public void Reanimar()
        {
            if (!_incapacitado) return;

            _incapacitado = false;
            if (_vitalidade?.Vitalidade != null && _vitalidade.Atributos != null)
                _vitalidade.Vitalidade.Curar(_vitalidade.Atributos.VitalidadeMax);

            AplicarCor(_corOriginal);
            OnReanimado?.Invoke();
        }

        private void FixedUpdate()
        {
            if (_incapacitado)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            if (_liberado)
            {
                if (_alvo == null) return;
                _rb.linearVelocity = _seguidor.CalcularVelocidade(transform.position, _alvo.position);
                return;
            }

            MoverNoCativeiro();
        }

        private void MoverNoCativeiro()
        {
            Vector2 posicaoAtual = transform.position;
            Vector2 direcao = (_cativeiro.AlvoAtual - posicaoAtual).normalized;
            _rb.linearVelocity = direcao * velocidadeDeCativeiro;
            _cativeiro.AtualizarChegada(posicaoAtual, raioDeChegada);
        }

        private void HandleAbatido()
        {
            // Não morre: cai no lugar. Ver EstaIncapacitado.
            _incapacitado = true;
            AplicarCor(corIncapacitado);
            OnIncapacitado?.Invoke();
        }

        private void AplicarCor(Color cor)
        {
            if (spriteDoYugNeth != null) spriteDoYugNeth.color = cor;
        }
    }
}
