using UnityEngine;

namespace FavelaAmarela.Runtime.Enemies
{
    public enum EnemyState { Idle, Patrol, Alert, Chase, Attack, Hurt, Dead }

    [RequireComponent(typeof(EnemyMovement), typeof(EnemyCombat), typeof(EnemyPerception))]
    public class EnemyStateMachine : MonoBehaviour
    {
        [Header("Configuração")]
        [SerializeField] private EnemyState initialState = EnemyState.Idle;
        [SerializeField] private float alertDuration = 2.0f;
        [SerializeField] private float maxChaseDistance = 20f;
        [SerializeField] private float defaultHurtDuration = 0.3f;

        [Header("Diagnóstico")]
        [Tooltip("Loga cada transição de estado deste inimigo. Serve para ver se ele está " +
                 "perseguindo, perdendo o alvo ou oscilando. Desligue quando a IA estabilizar.")]
        [SerializeField] private bool logarTransicoes = false;

        private EnemyMovement _movement;
        private EnemyCombat _combat;
        private EnemyPerception _perception;
        private EnemyBase _enemyBase;

        private EnemyState _currentState;
        private float _stateTimer;
        private float _hurtDuration;
        private Vector2 _chaseOrigin;

        public event System.Action<EnemyState, EnemyState> OnStateChanged;
        public EnemyState CurrentState => _currentState;

        private void Awake()
        {
            _movement = GetComponent<EnemyMovement>();
            _combat = GetComponent<EnemyCombat>();
            _perception = GetComponent<EnemyPerception>();
            _enemyBase = GetComponent<EnemyBase>();

            if (_perception != null)
            {
                _perception.OnEntrouAlerta += () => TryTransition(EnemyState.Alert);
                _perception.OnEntrouCaca += () => TryTransition(EnemyState.Chase);
                _perception.OnPerdeuAlvo += () => TryTransition(EnemyState.Patrol);
            }
            if (_enemyBase != null)
            {
                _enemyBase.OnAbatido += () => TryTransition(EnemyState.Dead);

                // LEVAR UM GOLPE PASSA A CONTAR. Até 2026-09-04 OnGolpeRecebido não tinha
                // ouvinte nenhum na IA: o inimigo reagia a passo e ignorava facada. Quem
                // decide o que fazer é a percepção, não a FSM -- ela sobe a suspeita e dispara
                // OnEntrouCaca, que já está ligado acima. Assim há UM caminho para Chase, e
                // não dois que podem divergir.
                if (_perception != null)
                    _enemyBase.OnGolpeRecebido += _ => _perception.NotarAgressao();
            }
        }

        private void Start()
        {
            _currentState = initialState;
            EnterState(_currentState);
        }

        private void Update()
        {
            _stateTimer += Time.deltaTime;
            UpdateState();
        }

        public bool TryTransition(EnemyState newState)
        {
            if (newState == _currentState) return false;
            if (_currentState == EnemyState.Dead) return false;
            if (!IsValidTransition(_currentState, newState)) return false;

            ExitState(_currentState);
            var previous = _currentState;
            _currentState = newState;
            EnterState(_currentState);
            OnStateChanged?.Invoke(previous, newState);

            if (logarTransicoes)
                Debug.Log($"[IA:{name}] {previous} -> {newState} " +
                          $"(suspeita={_perception.Suspeita:0.00})", this);

            return true;
        }

        private bool IsValidTransition(EnemyState from, EnemyState to) => to switch
        {
            EnemyState.Dead => true,
            EnemyState.Hurt => from != EnemyState.Dead,
            EnemyState.Idle => true,
            EnemyState.Patrol => from != EnemyState.Dead,
            EnemyState.Alert => from is EnemyState.Idle or EnemyState.Patrol or EnemyState.Chase,
            EnemyState.Chase => from is EnemyState.Idle or EnemyState.Patrol or EnemyState.Alert or EnemyState.Attack or EnemyState.Hurt,
            EnemyState.Attack => from == EnemyState.Chase,
            _ => false
        };

        private void EnterState(EnemyState state)
        {
            _stateTimer = 0f;
            switch (state)
            {
                case EnemyState.Idle:
                case EnemyState.Alert:
                case EnemyState.Attack:
                // Patrol precisa parar explicitamente: MoverPara deixa a linearVelocity
                // cravada no Rigidbody, e sair de Chase sem zerá-la fazia o inimigo deslizar
                // em linha reta para fora da cena (parecia estar "fugindo" do jogador).
                case EnemyState.Patrol:
                    _movement.Parar();
                    break;
                case EnemyState.Hurt:
                    if (_hurtDuration <= 0f) _hurtDuration = defaultHurtDuration;
                    _movement.Parar();
                    break;
                case EnemyState.Chase:
                    _chaseOrigin = transform.position;
                    break;
                case EnemyState.Dead:
                    _movement.Parar();
                    break;
            }
        }

        private void ExitState(EnemyState state) { }

        private void UpdateState()
        {
            switch (_currentState)
            {
                case EnemyState.Idle:
                    if (_stateTimer > 1f) TryTransition(EnemyState.Patrol);
                    break;
                case EnemyState.Alert:
                    if (_stateTimer > alertDuration) TryTransition(EnemyState.Patrol);
                    break;
                case EnemyState.Chase:
                    if (_perception.UltimaOrigemConhecida.HasValue)
                    {
                        _movement.MoverPara(_perception.UltimaOrigemConhecida.Value, _movement.VelocidadeCaca);
                        if (_combat.AlvoEstaAoAlcance()) TryTransition(EnemyState.Attack);
                    }
                    else
                    {
                        // Chase SEM destino era um no-op silencioso: o inimigo ficava parado,
                        // em estado de caça, para sempre -- nem perseguia nem voltava a
                        // patrulhar, porque a distância até _chaseOrigin nunca crescia. Voltar
                        // a Patrol é o comportamento honesto: sem alvo não há caça.
                        TryTransition(EnemyState.Patrol);
                        break;
                    }
                    if (Vector2.Distance(transform.position, _chaseOrigin) > maxChaseDistance)
                    {
                        _perception.PerderAlvo();
                        TryTransition(EnemyState.Patrol);
                    }
                    break;
                case EnemyState.Attack:
                    // Só volta a perseguir quando o alvo de fato escapa do alcance.
                    // Antes bastava TentarAtacar() devolver false — o que acontece durante
                    // toda a recarga — e o inimigo era jogado de volta para Chase todo
                    // frame, oscilando entre parar (EnterState de Attack) e avançar.
                    if (!_combat.AlvoEstaAoAlcance()) TryTransition(EnemyState.Chase);
                    else _combat.TentarAtacar();
                    break;
                case EnemyState.Hurt:
                    if (_stateTimer > _hurtDuration) TryTransition(EnemyState.Chase);
                    break;
                case EnemyState.Dead: break;
            }
        }

        public void Atordoar(float duracao)
        {
            _hurtDuration = duracao;
            TryTransition(EnemyState.Hurt);
        }
    }
}
