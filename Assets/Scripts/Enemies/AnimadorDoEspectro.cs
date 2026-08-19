using UnityEngine;
using FavelaAmarela.Core.Enemies;

namespace FavelaAmarela.Runtime.Enemies
{
    /// <summary>
    /// Camada Runtime. Toca os quadros do Espectro de Hali conforme a <see cref="EspectroFSM"/>
    /// — mesmo padrão do <see cref="AnimadorDoByakhee"/>.
    ///
    /// <para><b>Só dois ciclos, não quatro.</b> A folha
    /// (<c>EspectroHali_Spritesheet_24x48</c>) tem <c>idle/move/attack/death</c>, mas o
    /// Espectro não é <c>IDanificavel</c> nem ataca — é encenado por um diretor de cutscene
    /// (<see cref="EspectroAI.Manifestar"/> / <see cref="EspectroAI.IniciarCerco"/>), sem estado
    /// de golpe ou morte na <see cref="EspectroFSM"/>. Os quadros de <c>attack</c>/<c>death</c>
    /// não têm gatilho nenhum para tocar — ligá-los seria inventar um estado que a FSM não tem.
    /// <c>Latente</c> e <c>Manifestando</c> usam <c>idle</c> (ele ainda não avançou);
    /// <c>Cercando</c> usa <c>move</c>.</para>
    ///
    /// <para><b>Não mexe na cor/alfa</b> — isso continua com <c>EspectroAI.AtualizarVisual</c>,
    /// que decide a transparência do fade de materialização. Este componente só troca o
    /// <c>sprite</c>.</para>
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(EspectroAI))]
    [AddComponentMenu("Favela Amarela/Enemies/Animador do Espectro")]
    public sealed class AnimadorDoEspectro : MonoBehaviour
    {
        [Header("Quadros por ciclo (preenchidos por 'Montar Animação do Espectro')")]
        [Tooltip("Latente e Manifestando — ele ainda não avançou. [ASSET]")]
        [SerializeField] private Sprite[] idle;

        [Tooltip("Cercando — avançando em direção ao alvo do cerco. [ASSET]")]
        [SerializeField] private Sprite[] mover;

        [Header("Cadência")]
        [Min(1f)]
        [SerializeField] private float quadrosPorSegundo = 6f;

        private SpriteRenderer _sprite;
        private EspectroAI _ai;

        private Sprite[] _cicloAtual;
        private float _relogio;
        private int _quadro;

        private void Awake()
        {
            _sprite = GetComponent<SpriteRenderer>();
            _ai = GetComponent<EspectroAI>();
        }

        private void OnEnable()
        {
            if (_ai.Fsm != null) _ai.Fsm.OnStateChanged += HandleEstadoMudou;
        }

        private void OnDisable()
        {
            if (_ai.Fsm != null) _ai.Fsm.OnStateChanged -= HandleEstadoMudou;
        }

        private void Start()
        {
            if (_ai.Fsm != null) TrocarCiclo(CicloDe(_ai.Fsm.CurrentState));
        }

        private void Update()
        {
            if (_cicloAtual == null || _cicloAtual.Length == 0) return;

            _relogio += Time.deltaTime * quadrosPorSegundo;
            if (_relogio < 1f) return;

            _relogio -= 1f;
            _quadro = (_quadro + 1) % _cicloAtual.Length;
            _sprite.sprite = _cicloAtual[_quadro];
        }

        private void HandleEstadoMudou(EspectroState anterior, EspectroState atual)
            => TrocarCiclo(CicloDe(atual));

        private Sprite[] CicloDe(EspectroState estado) => estado switch
        {
            EspectroState.Cercando => mover,
            _ => idle, // Latente, Manifestando
        };

        private void TrocarCiclo(Sprite[] ciclo)
        {
            if (ciclo == null || ciclo.Length == 0 || ciclo == _cicloAtual) return;

            _cicloAtual = ciclo;
            _quadro = 0;
            _relogio = 0f;
            _sprite.sprite = ciclo[0];
        }
    }
}
