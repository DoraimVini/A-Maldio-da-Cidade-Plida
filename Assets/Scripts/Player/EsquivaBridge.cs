using System;
using UnityEngine;
using FavelaAmarela.Core.Abilities;

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
        [Header("Ability Settings")]
        [SerializeField] private float duration = 0.15f;
        [SerializeField] private float cooldown = 0.8f;
        [SerializeField] private float speedMultiplier = 2.5f;

        private Esquiva esquiva;
        private float lastUseTime = -999f;

        /// <summary>Direção, duração e multiplicador de velocidade da esquiva ativada.</summary>
        public event Action<Vector2, float, float> OnEsquivaActivada;

        public bool IsEsquivando { get; private set; }

        private void Awake()
        {
            esquiva = new Esquiva(duration, cooldown, speedMultiplier);
        }

        public void TryActivateEsquiva(Vector2 direction)
        {
            if (IsEsquivando) return;
            if (direction == Vector2.zero) return;

            if (!esquiva.CanActivate(Time.time - lastUseTime)) return;

            var result = esquiva.Execute();
            lastUseTime = Time.time;
            IsEsquivando = true;

            OnEsquivaActivada?.Invoke(direction, result.DurationSeconds, result.SpeedMultiplier);

            Invoke(nameof(EndEsquiva), result.DurationSeconds);
        }

        private void EndEsquiva()
        {
            IsEsquivando = false;
        }
    }
}
