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
                    if (Vector2.Distance(transform.position, _chaseOrigin) > maxChaseDistance)
                    {
                        _perception.PerderAlvo();
                        TryTransition(EnemyState.Patrol);
                    }
                    break;
                case EnemyState.Attack:
                    if (!_combat.TentarAtacar()) TryTransition(EnemyState.Chase);
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
