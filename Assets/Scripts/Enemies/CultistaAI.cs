using UnityEngine;

namespace FavelaAmarela.Runtime.Enemies
{
    [RequireComponent(typeof(EnemyBase))]
    [RequireComponent(typeof(EnemyMovement))]
    [RequireComponent(typeof(EnemyCombat))]
    [RequireComponent(typeof(EnemyPerception))]
    [RequireComponent(typeof(EnemyStateMachine))]
    [RequireComponent(typeof(EnemyStatusEffects))]
    public class CultistaAI : MonoBehaviour
    {
        [Header("Cores por estado")]
        [SerializeField] private Color corErrante = Color.white;
        [SerializeField] private Color corAlerta = Color.yellow;
        [SerializeField] private Color corCaca = Color.red;
        [SerializeField] private Color corAtacar = new Color(0.75f, 0f, 0.2f);
        [SerializeField] private Color corHurt = Color.magenta;

        private EnemyStateMachine _fsm;
        private SpriteRenderer _sr;

        private void Awake()
        {
            _fsm = GetComponent<EnemyStateMachine>();
            _sr = GetComponent<SpriteRenderer>();
            _fsm.OnStateChanged += (prev, curr) => AtualizarCor(curr);
            AtualizarCor(_fsm.CurrentState);
        }

        private void AtualizarCor(EnemyState estado)
        {
            _sr.color = estado switch
            {
                EnemyState.Idle or EnemyState.Patrol => corErrante,
                EnemyState.Alert => corAlerta,
                EnemyState.Chase => corCaca,
                EnemyState.Attack => corAtacar,
                EnemyState.Hurt => corHurt,
                EnemyState.Dead => Color.gray,
                _ => _sr.color
            };
        }
    }
}
