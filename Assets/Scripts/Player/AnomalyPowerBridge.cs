using System;
using UnityEngine;
using FavelaAmarela.Core.Abilities;

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

        private DimensionalLeap dimensionalLeap;
        private float lastUseTime = -999f;
        
        // Mock current resilience until we implement the Resilience system
        private float currentResilience = 100f; 

        // Events that other components (like VFX, Audio, Physics) can subscribe to
        public event Action<Vector2, float, float> OnDimensionalLeapActivated; // direction, duration, speedMultiplier
        public event Action<float> OnResilienceConsumed;

        public bool IsLeaping { get; private set; }

        private void Awake()
        {
            dimensionalLeap = new DimensionalLeap(leapDuration, leapCooldown, leapResilienceCost);
        }

        public void TryActivateLeap(Vector2 direction)
        {
            if (IsLeaping) return;
            if (direction == Vector2.zero) return;

            if (dimensionalLeap.CanActivate(currentResilience, Time.time - lastUseTime))
            {
                var result = dimensionalLeap.Execute(currentResilience);
                if (result.Success)
                {
                    lastUseTime = Time.time;
                    currentResilience -= result.ResilienceCost;
                    
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
