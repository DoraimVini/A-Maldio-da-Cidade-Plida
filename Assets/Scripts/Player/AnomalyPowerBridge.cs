using System;
using UnityEngine;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Combat;
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
        [Header("Ability Settings")]
        [SerializeField] private float leapDuration = 0.2f;
        [SerializeField] private float leapCooldown = 1.0f;
        [SerializeField] private float leapResilienceCost = 10f;
        [SerializeField] private float leapSpeedMultiplier = 3.5f;

        [Header("Progressão")]
        [Tooltip("Ligar só para testar o Salto isolado, sem o patuá. No jogo real, Damião começa sem o Salto — ele é destravado ao encontrar o patuá na Zona 5 (ver DesbloquearSalto).")]
        [SerializeField] private bool desbloqueadoNoInicio = false;

        private DimensionalLeap dimensionalLeap;
        private float lastUseTime = -999f;
        private bool _saltoDesbloqueado;

        private ResilienciaMental _resiliencia;

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

        // Events that other components (like VFX, Audio, Physics) can subscribe to
        public event Action<Vector2, float, float> OnDimensionalLeapActivated; // direction, duration, speedMultiplier
        public event Action<float> OnResilienceConsumed;

        public bool IsLeaping { get; private set; }

        private void Awake()
        {
            dimensionalLeap = new DimensionalLeap(leapDuration, leapCooldown, leapResilienceCost);
            _saltoDesbloqueado = desbloqueadoNoInicio;
        }

        public void TryActivateLeap(Vector2 direction)
        {
            if (!_saltoDesbloqueado) return;
            if (IsLeaping) return;
            if (direction == Vector2.zero) return;

            if (_resiliencia == null) return;

            if (dimensionalLeap.CanActivate(_resiliencia.Atual, Time.time - lastUseTime))
            {
                var result = dimensionalLeap.Execute(_resiliencia.Atual);
                if (result.Success)
                {
                    lastUseTime = Time.time;
                    _resiliencia.SofrerTrauma(result.ResilienceCost);
                    
                    IsLeaping = true;
                    
                    // Broadcast events
                    OnResilienceConsumed?.Invoke(result.ResilienceCost);
                    OnDimensionalLeapActivated?.Invoke(direction, result.DurationSeconds, leapSpeedMultiplier);

                    // End leap after duration
                    Invoke(nameof(EndLeap), result.DurationSeconds);
                }
            }
        }

        private void EndLeap()
        {
            IsLeaping = false;
        }
    }
}
