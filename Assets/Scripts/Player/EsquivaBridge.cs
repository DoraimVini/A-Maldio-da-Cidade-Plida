using System;
using UnityEngine;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Player;
using FavelaAmarela.Runtime.Config;

namespace FavelaAmarela.Player
{
    /// <summary>
    /// MonoBehaviour Bridge conectando o POCO <see cref="Esquiva"/> à Unity.
    /// Espelha o papel de <see cref="AnomalyPowerBridge"/>, mas para um movimento
    /// físico comum — sem custo de Resiliência Mental e sem tornar Damião intangível
    /// (a Esquiva colide com paredes normalmente; só o Salto Dimensional atravessa
    /// barreiras anômalas).
    /// </summary>
    [AddComponentMenu("Favela Amarela/Esquiva Bridge")]
    public class EsquivaBridge : MonoBehaviour
    {
        [Header("Configuração")]
        [Tooltip("Asset de tunagem da Esquiva. Se vazio, usa os defaults do POCO.")]
        [SerializeField] private EsquivaConfig config;

        private Esquiva esquiva;
        private float lastUseTime = -999f;
        private PlayerStateMachine _fsm;

        /// <summary>Direção, duração e multiplicador de velocidade da esquiva ativada.</summary>
        public event Action<Vector2, float, float> OnEsquivaActivada;

        /// <summary>true enquanto a FSM do jogador estiver no estado Esquivando (fonte única de verdade).</summary>
        public bool IsEsquivando => _fsm != null && _fsm.CurrentState == PlayerState.Esquivando;

        /// <summary>Injeta a FSM de estado do jogador (chamado por <see cref="PlayerMovement"/> no Awake).</summary>
        public void BindStateMachine(PlayerStateMachine fsm) => _fsm = fsm;

        private void Awake()
        {
            if (config != null)
            {
                esquiva = new Esquiva(config.Duration, config.Cooldown, config.SpeedMultiplier);
            }
            else
            {
                Debug.LogWarning("[EsquivaBridge] EsquivaConfig não atribuído; usando defaults do POCO.", this);
                esquiva = new Esquiva();
            }
        }

        public void TryActivateEsquiva(Vector2 direction)
        {
            if (direction == Vector2.zero) return;
            if (_fsm == null) return; // fallback seguro: sem FSM injetada, a ação não dispara
            if (!_fsm.EstaLivre) return; // portão barato antes do Execute (que é irreversível)
            if (!esquiva.CanActivate(Time.time - lastUseTime)) return;

            var result = esquiva.Execute();

            // Commit da exclusão mútua (revalida; em thread única o estado não mudou desde EstaLivre).
            if (!_fsm.TryEntrarAcao(PlayerState.Esquivando, result.DurationSeconds)) return;

            lastUseTime = Time.time;
            OnEsquivaActivada?.Invoke(direction, result.DurationSeconds, result.SpeedMultiplier);
        }
    }
}
