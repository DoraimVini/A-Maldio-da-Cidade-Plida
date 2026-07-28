using UnityEngine;

namespace FavelaAmarela.Runtime.Config
{
    /// <summary>
    /// Config do Salto Dimensional: parâmetros que alimentam o construtor do POCO
    /// <c>FavelaAmarela.Core.Abilities.DimensionalLeap</c>. O multiplicador de velocidade
    /// do dash NÃO mora aqui — é concern do adaptador e continua no <c>AnomalyPowerBridge</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "FavelaAmarela/Config/Salto Dimensional", fileName = "SaltoDimensionalConfig")]
    public sealed class SaltoDimensionalConfig : ScriptableObject
    {
        [SerializeField] private float leapDuration = 0.2f;
        [SerializeField] private float leapCooldown = 1.0f;
        [SerializeField] private float leapResilienceCost = 10f;

        /// <summary>Duração do dash (s).</summary>
        public float LeapDuration => leapDuration;
        /// <summary>Cooldown entre saltos (s).</summary>
        public float LeapCooldown => leapCooldown;
        /// <summary>Custo de Resiliência Mental por salto.</summary>
        public float LeapResilienceCost => leapResilienceCost;
    }
}
