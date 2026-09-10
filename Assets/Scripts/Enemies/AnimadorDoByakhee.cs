using UnityEngine;
using FavelaAmarela.Core.Enemies;

namespace FavelaAmarela.Runtime.Enemies
{
    /// <summary>
    /// Camada Runtime. Toca os quadros do Byakhee conforme o estado da
    /// <see cref="ByakheeFSM"/> — o spritesheet real tem 26 quadros já nomeados por estado
    /// (<c>espreita</c>, <c>rasante</c>, <c>garras</c>, <c>grito</c>, <c>dano</c>,
    /// <c>derrota</c>), então o mapeamento é direto.
    ///
    /// <para><b>Por que não um <c>AnimatorController</c>:</b> um Animator traria uma
    /// <b>segunda máquina de estados</b>, com transições próprias, que precisaria ser mantida
    /// em sincronia com a <c>ByakheeFSM</c> do Core. Isso é exatamente a duplicação de regra
    /// que a arquitetura do projeto proíbe (ver <c>Assets/Scripts/CLAUDE.md</c>: regra de
    /// negócio mora no Core, o <c>MonoBehaviour</c> só sincroniza o visual). Aqui a FSM
    /// continua sendo a única fonte da verdade e este componente só a lê.</para>
    ///
    /// <para><b>Substitui o tingimento por cor</b> que o <c>ByakheeAI</c> usava como
    /// placeholder enquanto não havia arte. Arte real não se tinge — mesma regra já aplicada
    /// à Cassilda e ao Rei em Amarelo.</para>
    ///
    /// <para><b>Dois defeitos medidos em 2026-09-10</b> (o Vini: <i>"a sprite da Byakhee está
    /// virando de um lado para outro"</i>):</para>
    /// <list type="number">
    /// <item><b>A inscrição na FSM não acontecia.</b> Ela era feita em <c>OnEnable</c>, lendo
    /// <c>_ai.Fsm</c> — que o <c>ByakheeAI</c> só cria no <c>Awake</c> dele. A Unity não garante
    /// que o Awake de um componente rode antes do OnEnable de outro no mesmo objeto; no Editor
    /// vivo, <c>OnStateChanged</c> tinha um único inscrito (o próprio AI), e a luta inteira era
    /// tocada com os quatro quadros da espreita. Agora a inscrição é idempotente e repetida no
    /// <c>Start</c>, quando todos os Awakes já rodaram.</item>
    /// <item><b>A folha alternava o lado a cada quadro.</b> Medido pelo olho (252,0,0) contra o
    /// centroide do corpo: espreita D-E-D-E, rasante D-E-D-E-D-E, e assim por diante — a folha
    /// foi gerada em pares (cada pose para cada lado) e fatiada como sequência. A 8 qps o bicho
    /// virava 4 vezes por segundo. Os quadros virados para a esquerda foram espelhados <b>na
    /// própria folha</b> (ver <c>PROCEDENCIA_Byakhee.txt</c>); quem vira agora é o
    /// <c>flipX</c>, pela velocidade, com zona morta — guarda: <c>AFolhaDoByakheeTests</c>.</item>
    /// </list>
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [AddComponentMenu("Favela Amarela/Enemies/Animador do Byakhee")]
    public sealed class AnimadorDoByakhee : MonoBehaviour
    {
        [Header("Quadros por estado (preenchidos por 'Montar Animação do Byakhee')")]
        [Tooltip("Pairando/espreita — também usado no pouso e ao circundar. [ASSET]")]
        [SerializeField] private Sprite[] espreita;

        [Tooltip("Voo em rasante — também usado no frenesi, mais rápido. [ASSET]")]
        [SerializeField] private Sprite[] rasante;

        [Tooltip("Mergulho de garras. [ASSET]")]
        [SerializeField] private Sprite[] garras;

        [Tooltip("Grito direcionado (cone de pressão sonora). [ASSET]")]
        [SerializeField] private Sprite[] grito;

        [Tooltip("Reação a dano — interrompe o ciclo por um instante. [ASSET]")]
        [SerializeField] private Sprite[] dano;

        [Tooltip("Derrota. Toca uma vez e para no último quadro. [ASSET]")]
        [SerializeField] private Sprite[] derrota;

        [Header("Cadência")]
        [Tooltip("Quadros por segundo do ciclo normal.")]
        [Min(1f)]
        [SerializeField] private float quadrosPorSegundo = 8f;

        [Tooltip("Multiplicador de cadência no Frenesi — a leitura de 'ele acelerou'.")]
        [Min(1f)]
        [SerializeField] private float aceleracaoDoFrenesi = 1.75f;

        [Tooltip("Segundos que a reação a dano ocupa antes de voltar ao ciclo do estado.")]
        [Min(0f)]
        [SerializeField] private float duracaoDaReacao = 0.18f;

        [Header("Lado")]
        [Tooltip("Velocidade horizontal (un/s) abaixo da qual o sprite NÃO vira. Pairando, a " +
                 "velocidade oscila em torno de zero — sem zona morta, o flip viraria de novo o " +
                 "que a folha corrigida deixou de virar.")]
        [Min(0f)]
        [SerializeField] private float velocidadeMinimaParaVirar = 0.5f;

        private SpriteRenderer _sprite;
        private Rigidbody2D _rb;
        private ByakheeAI _ai;
        private EnemyBase _enemyBase;
        private bool _inscritoNaFsm;

        private Sprite[] _cicloAtual;
        private float _relogio;
        private int _quadro;

        // Reação a dano e derrota não são estados da FSM: são interrupções visuais que se
        // sobrepõem ao ciclo. Guardadas à parte para não sujar o switch de estado.
        private float _reacaoRestante;
        private bool _derrotado;

        private void Awake()
        {
            _sprite = GetComponent<SpriteRenderer>();
            _rb = GetComponent<Rigidbody2D>();
            _ai = GetComponent<ByakheeAI>();
            _enemyBase = GetComponent<EnemyBase>();

            if (_ai == null)
                Debug.LogError($"[AnimadorDoByakhee] '{name}' sem ByakheeAI — sem estado para ler.", this);

            // Arte real não se tinge: o ByakheeAI pintava o sprite por estado como placeholder.
            _sprite.color = Color.white;
        }

        private void OnEnable()
        {
            InscreverNaFsm();
            if (_enemyBase != null)
            {
                _enemyBase.OnDanoSofrido += HandleDano;
                _enemyBase.OnAbatido += HandleAbatido;
            }
        }

        private void OnDisable()
        {
            DesinscreverDaFsm();
            if (_enemyBase != null)
            {
                _enemyBase.OnDanoSofrido -= HandleDano;
                _enemyBase.OnAbatido -= HandleAbatido;
            }
        }

        private void Start()
        {
            // Segunda chance de inscrição: aqui TODOS os Awakes já rodaram, então a FSM existe.
            // No OnEnable ela pode não existir ainda — e foi assim que a luta inteira ficou nos
            // quadros da espreita sem ninguém notar (ver o resumo da classe).
            InscreverNaFsm();
            if (_ai?.Fsm != null) TrocarCiclo(CicloDe(_ai.Fsm.CurrentState), reiniciar: true);
        }

        /// <summary>Inscreve na FSM uma vez só, quando ela existir. Seguro chamar repetido.</summary>
        private void InscreverNaFsm()
        {
            if (_inscritoNaFsm || _ai?.Fsm == null) return;
            _ai.Fsm.OnStateChanged += HandleEstadoMudou;
            _inscritoNaFsm = true;
        }

        private void DesinscreverDaFsm()
        {
            if (!_inscritoNaFsm || _ai?.Fsm == null) return;
            _ai.Fsm.OnStateChanged -= HandleEstadoMudou;
            _inscritoNaFsm = false;
        }

        /// <summary>Se este animador está ouvindo a FSM do Byakhee. Para os guardas.</summary>
        public bool EstaInscritoNaFsm => _inscritoNaFsm;

        private void Update()
        {
            VirarParaOndeVoa();

            if (_cicloAtual == null || _cicloAtual.Length == 0) return;

            if (_reacaoRestante > 0f)
            {
                _reacaoRestante -= Time.deltaTime;
                if (_reacaoRestante <= 0f && !_derrotado && _ai?.Fsm != null)
                    TrocarCiclo(CicloDe(_ai.Fsm.CurrentState), reiniciar: false);
            }

            float fps = quadrosPorSegundo;
            if (!_derrotado && _ai?.Fsm != null && _ai.Fsm.CurrentState == ByakheeState.Frenesi)
                fps *= aceleracaoDoFrenesi;

            _relogio += Time.deltaTime * fps;
            if (_relogio < 1f) return;

            _relogio -= 1f;
            _quadro++;

            // A derrota trava no último quadro: o corpo não volta a bater asa.
            if (_derrotado && _quadro >= _cicloAtual.Length)
            {
                _quadro = _cicloAtual.Length - 1;
                return;
            }

            _quadro %= _cicloAtual.Length;
            _sprite.sprite = _cicloAtual[_quadro];
        }

        /// <summary>
        /// A folha inteira olha para a direita; o <c>flipX</c> vira o corpo para onde ele voa.
        /// Só decide acima da zona morta — pairando, a velocidade cruza zero o tempo todo, e
        /// virar a cada cruzamento traria de volta exatamente o defeito que a folha corrigida
        /// tirou. Abaixo dela o lado anterior fica.
        /// </summary>
        private void VirarParaOndeVoa()
        {
            if (_derrotado || _rb == null) return;

            float vx = _rb.linearVelocity.x;
            if (Mathf.Abs(vx) < velocidadeMinimaParaVirar) return;

            _sprite.flipX = vx < 0f;
        }

        private void HandleEstadoMudou(ByakheeState anterior, ByakheeState atual)
        {
            if (_derrotado) return;
            TrocarCiclo(CicloDe(atual), reiniciar: true);
        }

        private void HandleDano(float _)
        {
            if (_derrotado || dano == null || dano.Length == 0) return;

            TrocarCiclo(dano, reiniciar: true);
            _reacaoRestante = duracaoDaReacao;
        }

        private void HandleAbatido()
        {
            _derrotado = true;
            _reacaoRestante = 0f;
            TrocarCiclo(derrota, reiniciar: true);
        }

        /// <summary>
        /// Estado da FSM → ciclo de quadros. Sete estados para quatro ciclos de voo: pouso e
        /// circundar reaproveitam a espreita (é o mesmo corpo pairando ou em terra), e o
        /// frenesi reaproveita o rasante acelerado, porque é isso que ele é.
        /// </summary>
        private Sprite[] CicloDe(ByakheeState estado) => estado switch
        {
            ByakheeState.Rasante => rasante,
            ByakheeState.MergulhoDeGarras => garras,
            ByakheeState.GritoDirecionado => grito,
            ByakheeState.Frenesi => rasante,
            _ => espreita,
        };

        private void TrocarCiclo(Sprite[] novo, bool reiniciar)
        {
            if (novo == null || novo.Length == 0) return;
            if (!reiniciar && _cicloAtual == novo) return;

            _cicloAtual = novo;
            if (reiniciar)
            {
                _quadro = 0;
                _relogio = 0f;
            }
            _sprite.sprite = _cicloAtual[Mathf.Clamp(_quadro, 0, _cicloAtual.Length - 1)];
        }
    }
}
