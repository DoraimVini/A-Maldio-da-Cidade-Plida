using System;
using UnityEngine;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Core.Player;
using FavelaAmarela.Runtime.Config;
namespace FavelaAmarela.Player
{
    /// <summary>
    /// MonoBehaviour Bridge connecting the POCO AnomalyPower to Unity.
    /// Broadcasts events for visual effects, audio, and physics systems to hook into.
    /// Follows the Open-Closed Principle (OCP).
    /// </summary>
    [AddComponentMenu("Favela Amarela/Anomaly Power Bridge")]
    public class AnomalyPowerBridge : MonoBehaviour
    {
        [Header("Configuração")]
        [Tooltip("Asset de tunagem do Salto (duração/cooldown/custo). Se vazio, usa os defaults do POCO.")]
        [SerializeField] private SaltoDimensionalConfig config;

        [Tooltip("Multiplicador de velocidade do dash — concern de feel do adaptador, não do POCO.")]
        [SerializeField] private float leapSpeedMultiplier = 3.5f;

        [Header("Progressão")]
        [Tooltip("Ligar só para testar o Salto isolado, sem o patuá. No jogo real, Damião começa sem o Salto — ele é destravado ao encontrar o patuá na Zona 5 (ver DesbloquearSalto).")]
        [SerializeField] private bool desbloqueadoNoInicio = false;

        private DimensionalLeap dimensionalLeap;
        private float lastUseTime = -999f;
        private bool _saltoDesbloqueado;

        private ResilienciaMental _resiliencia;
        private PlayerStateMachine _fsm;

        /// <summary>Se o Salto Dimensional já foi destravado (ver <see cref="DesbloquearSalto"/>).</summary>
        public bool SaltoDesbloqueado => _saltoDesbloqueado;

        /// <summary>
        /// Destrava o Salto Dimensional permanentemente. Chamado pelo pickup do
        /// patuá na Zona 5 — Damião não nasce com essa habilidade.
        /// </summary>
        public void DesbloquearSalto() => _saltoDesbloqueado = true;

        public void Bind(ResilienciaMental resiliencia)
        {
            _resiliencia = resiliencia;
        }

        /// <summary>Injeta a FSM de estado do jogador (chamado por <see cref="PlayerMovement"/> no Awake).</summary>
        public void BindStateMachine(PlayerStateMachine fsm) => _fsm = fsm;

        // Events that other components (like VFX, Audio, Physics) can subscribe to
        public event Action<Vector2, float, float> OnDimensionalLeapActivated; // direction, duration, speedMultiplier
        public event Action<float> OnResilienceConsumed;

        /// <summary>true enquanto a FSM do jogador estiver no estado Saltando (fonte única de verdade).</summary>
        public bool IsLeaping => _fsm != null && _fsm.CurrentState == PlayerState.Saltando;

        private void Awake()
        {
            if (config != null)
            {
                dimensionalLeap = new DimensionalLeap(config.LeapDuration, config.LeapCooldown, config.LeapResilienceCost);
            }
            else
            {
                Debug.LogWarning("[AnomalyPowerBridge] SaltoDimensionalConfig não atribuído; usando defaults do POCO.", this);
                dimensionalLeap = new DimensionalLeap();
            }

            _saltoDesbloqueado = desbloqueadoNoInicio;
        }

        public void TryActivateLeap(Vector2 direction)
        {
            if (!_saltoDesbloqueado) return;
            if (direction == Vector2.zero) return;
            if (_resiliencia == null) return;
            if (_fsm == null) return; // fallback seguro: sem FSM injetada, a ação não dispara
            if (!_fsm.EstaLivre) return; // portão barato antes do Execute (que é irreversível)

            if (!dimensionalLeap.CanActivate(_resiliencia.Atual, Time.time - lastUseTime)) return;

            var result = dimensionalLeap.Execute(_resiliencia.Atual);
            if (!result.Success) return;

            // Commit da exclusão mútua (revalida; em thread única o estado não mudou desde EstaLivre).
            if (!_fsm.TryEntrarAcao(PlayerState.Saltando, result.DurationSeconds)) return;

            lastUseTime = Time.time;
            _resiliencia.SofrerTrauma(result.ResilienceCost);

            // Broadcast events
            OnResilienceConsumed?.Invoke(result.ResilienceCost);
            OnDimensionalLeapActivated?.Invoke(direction, result.DurationSeconds, leapSpeedMultiplier);
        }
    }
}
