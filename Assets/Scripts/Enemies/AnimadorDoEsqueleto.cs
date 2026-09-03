using UnityEngine;

namespace FavelaAmarela.Runtime.Enemies
{
    /// <summary>
    /// Camada Runtime. Toca os quadros do <see cref="EsqueletoInvocado"/> — mesmo padrão do
    /// <see cref="AnimadorDoCultista"/> e do <see cref="AnimadorDoEspectro"/>: escreve
    /// <c>SpriteRenderer.sprite</c> direto, sem <c>AnimatorController</c>.
    ///
    /// <para><b>Por que ele lê o Rigidbody, e não uma FSM.</b> O Esqueleto não tem FSM — é um
    /// <see cref="MonoBehaviour"/> simples que persegue, para ao chegar no alcance e golpeia em
    /// cadência. As três situações dele estão todas legíveis na velocidade do corpo: parado
    /// (colado no alvo, entre golpes) ou andando. O golpe é o único que precisa ser
    /// <b>avisado</b>, porque acontece num instante e não muda a velocidade.</para>
    ///
    /// <para><b>O espelhamento é obrigatório aqui, não enfeite (2026-09-03).</b> A arte anterior
    /// era uma pose frontal simétrica de 20×46 px, então nada no projeto precisava virar o
    /// Esqueleto. A folha nova olha para a <b>direita</b>: sem <c>flipX</c>, metade das
    /// perseguições seria o esqueleto andando de ré.</para>
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(Rigidbody2D))]
    [AddComponentMenu("Favela Amarela/Enemies/Animador do Esqueleto")]
    public sealed class AnimadorDoEsqueleto : MonoBehaviour
    {
        [Header("Quadros por ciclo")]
        [Tooltip("Colado no alvo, esperando a cadência do golpe. [ASSET pixel art]")]
        [SerializeField] private Sprite[] parado;

        [Tooltip("Perseguindo. [ASSET pixel art]")]
        [SerializeField] private Sprite[] andar;

        [Tooltip("O golpe, tocado uma vez e depois devolve o controle. [ASSET pixel art]")]
        [SerializeField] private Sprite[] golpe;

        [Header("Cadência")]
        [Min(1f)]
        [SerializeField] private float quadrosPorSegundo = 10f;

        [Tooltip("Abaixo desta velocidade ele conta como parado. Evita trocar de ciclo a cada " +
                 "frame quando a perseguição encosta no alvo e a velocidade oscila em torno de zero.")]
        [Min(0f)]
        [SerializeField] private float velocidadeMinimaParaAndar = 0.05f;

        private SpriteRenderer _sprite;
        private Rigidbody2D _rb;

        private Sprite[] _ciclo;
        private float _relogio;
        private int _quadro;

        /// <summary>Enquanto verdadeiro, o golpe manda e a velocidade não troca o ciclo.</summary>
        private bool _golpeando;

        private void Awake()
        {
            _sprite = GetComponent<SpriteRenderer>();
            _rb = GetComponent<Rigidbody2D>();
            _ciclo = parado;
        }

        /// <summary>
        /// Avisa que o Esqueleto desferiu um golpe. Chamado por
        /// <see cref="EsqueletoInvocado"/> no instante do dano.
        ///
        /// <para>O ciclo toca uma vez até o fim e só então devolve o controle à velocidade —
        /// um golpe cortado no meio por um passo do alvo não leria como golpe.</para>
        /// </summary>
        public void Golpear()
        {
            if (golpe == null || golpe.Length == 0) return;

            _golpeando = true;
            _ciclo = golpe;
            _quadro = 0;
            _relogio = 0f;
            _sprite.sprite = golpe[0];
        }

        private void Update()
        {
            // A arte olha para a direita. Só vira quando há intenção de movimento: parado, ele
            // mantém o lado em que estava, que é o lado do alvo de onde acabou de chegar.
            float vx = _rb.linearVelocity.x;
            if (Mathf.Abs(vx) > velocidadeMinimaParaAndar) _sprite.flipX = vx < 0f;

            if (!_golpeando) TrocarCiclo(EmMovimento() ? andar : parado);

            if (_ciclo == null || _ciclo.Length == 0) return;

            _relogio += Time.deltaTime * quadrosPorSegundo;
            if (_relogio < 1f) return;

            _relogio -= 1f;
            _quadro++;

            if (_golpeando && _quadro >= _ciclo.Length)
            {
                _golpeando = false;
                _quadro = 0;
                _ciclo = EmMovimento() ? andar : parado;
                if (_ciclo == null || _ciclo.Length == 0) return;
            }

            _quadro %= _ciclo.Length;
            _sprite.sprite = _ciclo[_quadro];
        }

        private bool EmMovimento()
            => _rb.linearVelocity.sqrMagnitude >
               velocidadeMinimaParaAndar * velocidadeMinimaParaAndar;

        private void TrocarCiclo(Sprite[] novo)
        {
            if (novo == null || novo.Length == 0 || ReferenceEquals(novo, _ciclo)) return;

            _ciclo = novo;
            _quadro = 0;
            _relogio = 0f;
            _sprite.sprite = novo[0];
        }
    }
}
