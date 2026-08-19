using UnityEngine;

namespace FavelaAmarela.Runtime.Enemies
{
    /// <summary>
    /// Camada Runtime. Toca os quadros do Cultista conforme o <see cref="EnemyState"/> da
    /// <see cref="EnemyStateMachine"/> — mesmo padrão do <see cref="AnimadorDoByakhee"/>: lê a
    /// FSM do Core, escreve só o <c>SpriteRenderer</c>, nenhuma regra de negócio aqui.
    ///
    /// <para><b>Por que não um <c>AnimatorController</c>:</b> traria uma segunda máquina de
    /// estados a manter em sincronia com a <c>EnemyStateMachine</c> — a duplicação de regra que
    /// <c>Assets/Scripts/CLAUDE.md</c> proíbe (ver o mesmo argumento, mais detalhado, no XML doc
    /// do <see cref="AnimadorDoByakhee"/>).</para>
    ///
    /// <para><b>Não mexe na cor.</b> <see cref="CultistaAI"/> já tinge o sprite por estado
    /// (branco/amarelo/vermelho) como leitura de gameplay — não é placeholder de arte ausente,
    /// é sinalização deliberada, e os dois componentes escrevem em canais diferentes do mesmo
    /// <c>SpriteRenderer</c> (cor vs. sprite) sem conflito.</para>
    ///
    /// <para><b><c>Hurt</c> não tem quadros próprios</b> — a folha só tem idle/walk/attack/death.
    /// A reação a dano continua sendo só o flash magenta do <c>CultistaAI</c>; o ciclo de sprite
    /// não muda.</para>
    ///
    /// <para><b><c>Dead</c> tem visibilidade quase nula:</b> <c>EnemyBase</c> chama
    /// <c>Destroy(gameObject)</c> no mesmo método que dispara <c>OnAbatido</c>, então o quadro de
    /// morte aparece por, no máximo, um frame antes do objeto sumir. Preenchido mesmo assim —
    /// arte que existe e não se liga é o defeito mais comum deste projeto.</para>
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [AddComponentMenu("Favela Amarela/Enemies/Animador do Cultista")]
    public sealed class AnimadorDoCultista : MonoBehaviour
    {
        [Header("Quadros por ciclo (preenchidos por 'Montar Animação do Cultista')")]
        [Tooltip("Errante, Alerta, Patrulha. [ASSET]")]
        [SerializeField] private Sprite[] idle;

        [Tooltip("Caça — perseguindo o jogador. [ASSET]")]
        [SerializeField] private Sprite[] walk;

        [Tooltip("Golpe. [ASSET]")]
        [SerializeField] private Sprite[] attack;

        [Tooltip("Abatido. Visibilidade quase nula — ver XML doc da classe. [ASSET]")]
        [SerializeField] private Sprite[] death;

        [Header("Cadência")]
        [Min(1f)]
        [SerializeField] private float quadrosPorSegundo = 6f;

        private SpriteRenderer _sprite;
        private EnemyStateMachine _fsm;

        private Sprite[] _cicloAtual;
        private float _relogio;
        private int _quadro;
        private bool _travado; // true em Dead: ignora novas trocas de estado

        private void Awake()
        {
            _sprite = GetComponent<SpriteRenderer>();
            _fsm = GetComponent<EnemyStateMachine>();

            if (_fsm == null)
                Debug.LogError($"[AnimadorDoCultista] '{name}' sem EnemyStateMachine — sem " +
                               "estado para ler.", this);
        }

        private void OnEnable()
        {
            if (_fsm != null) _fsm.OnStateChanged += HandleEstadoMudou;
        }

        private void OnDisable()
        {
            if (_fsm != null) _fsm.OnStateChanged -= HandleEstadoMudou;
        }

        private void Start()
        {
            if (_fsm != null) TrocarCiclo(CicloDe(_fsm.CurrentState), reiniciar: true);
        }

        private void Update()
        {
            if (_travado || _cicloAtual == null || _cicloAtual.Length == 0) return;

            _relogio += Time.deltaTime * quadrosPorSegundo;
            if (_relogio < 1f) return;

            _relogio -= 1f;
            _quadro = (_quadro + 1) % _cicloAtual.Length;
            _sprite.sprite = _cicloAtual[_quadro];
        }

        private void HandleEstadoMudou(EnemyState anterior, EnemyState atual)
        {
            if (atual == EnemyState.Dead)
            {
                TrocarCiclo(death, reiniciar: true);
                _travado = true; // toca 1 quadro; Destroy(gameObject) encerra o resto
                return;
            }

            // Hurt não troca ciclo — só o flash de cor do CultistaAI reage a ele.
            if (atual == EnemyState.Hurt) return;

            TrocarCiclo(CicloDe(atual), reiniciar: false);
        }

        private Sprite[] CicloDe(EnemyState estado) => estado switch
        {
            EnemyState.Chase => walk,
            EnemyState.Attack => attack,
            EnemyState.Dead => death,
            _ => idle, // Idle, Patrol, Alert, Hurt (estado inicial antes do primeiro evento)
        };

        private void TrocarCiclo(Sprite[] ciclo, bool reiniciar)
        {
            if (ciclo == null || ciclo.Length == 0) return;
            if (!reiniciar && ciclo == _cicloAtual) return; // mesmo ciclo: não reinicia o quadro

            _cicloAtual = ciclo;
            _quadro = 0;
            _relogio = 0f;
            _sprite.sprite = ciclo[0];
        }
    }
}
