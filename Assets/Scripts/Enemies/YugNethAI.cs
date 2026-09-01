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
        /// <summary>
        /// Contorno de obstáculos, quando este objeto tem um <c>SeguidorDeCaminho</c>.
        ///
        /// <para><b>Opcional de propósito (2026-09-01):</b> sem ele o movimento continua sendo o
        /// de sempre, em linha reta. Um prefab esquecido degrada para o comportamento antigo,
        /// não para unidade parada.</para>
        /// </summary>
        private FavelaAmarela.Runtime.Navegacao.SeguidorDeCaminho _seguidorDeCaminho;

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
            _seguidorDeCaminho = GetComponent<FavelaAmarela.Runtime.Navegacao.SeguidorDeCaminho>();

            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            _vitalidade = GetComponent<VitalidadeBridge>();
            _vitalidade.OnAbatido += HandleAbatido;

            // Barra flutuante sobre a cabeça, se o prefab tiver uma (montada por
            // Tools/FavelaAmarela/Montar aliados). É opcional de propósito: o companheiro
            // funciona sem ela, e um aliado futuro pode não querer barra nenhuma.
            var barra = GetComponentInChildren<Runtime.UI.BarraDeVidaFlutuante>(includeInactive: true);
            if (barra != null && _vitalidade.Vitalidade != null)
                barra.Bind(_vitalidade.Vitalidade);

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
        /// <para><b>O comentário anterior aqui estava errado desde 2026-08-11</b>, e valia a
        /// pena corrigir em vez de apagar: ele dizia que Yug-Neth <i>"está na camada Enemy"</i>
        /// e que criar uma camada "Aliado" mudaria a matriz de todo mundo. A camada
        /// <c>Aliados</c> (7) <b>foi criada</b> naquela data e o prefab está nela desde então —
        /// o argumento se referia a um mundo que deixou de existir.</para>
        ///
        /// <para><b>Por que continua sendo por par de colisores, e não por camada:</b> a matriz
        /// tem <c>Aliados × Player</c> LIGADO de propósito, porque um aliado deve barrar o
        /// cenário e ser barrado por ele. O que não se quer é ele empurrar o Damião — um caso
        /// específico entre dois objetos, não entre duas categorias.</para>
        ///
        /// <para><b>A alternativa documentada</b> seria <c>Collider2D.excludeLayers</c>, que é
        /// per-instância e não precisa reagir a colisores criados depois. Fica registrada como
        /// melhoria: <c>IgnoreCollision</c> resolve só os colisores que existiam no momento da
        /// chamada, então um colisor acrescentado ao Damião mais tarde voltaria a empurrá-lo.</para>
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

        /// <summary>
        /// Faz Yug-Neth parar de seguir e ficar onde está — ele deixa de ser companheiro e passa
        /// a ser NPC. Usado ao entrar no Castelo.
        ///
        /// <para>Basta zerar o alvo: o <c>Update</c> sai cedo quando <c>_alvo</c> é nulo. Zerar a
        /// velocidade junto é o que impede que ele siga deslizando pela inércia do último quadro
        /// em que ainda seguia.</para>
        /// </summary>
        public void TornarNpc()
        {
            _alvo = null;
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
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
                // O companheiro é quem mais custa perder atrás de um muro: ele some, o
                // jogador não entende por quê, e a barra dele fica lá acusando presença.
                Vector3 passo = _seguidorDeCaminho != null
                    ? _seguidorDeCaminho.ProximoPontoPara(_alvo.position)
                    : _alvo.position;

                _rb.linearVelocity = _seguidor.CalcularVelocidade(transform.position, passo);
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
