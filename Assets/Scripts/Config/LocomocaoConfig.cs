using UnityEngine;

namespace FavelaAmarela.Runtime.Config
{
    /// <summary>
    /// Config de locomoção de Damião: velocidades e raios de ruído por modo furtivo.
    /// Antes esses seis valores viviam presos no construtor de <c>PlayerStealthState</c>
    /// (POCO), sem qualquer exposição no Inspector — este ScriptableObject os torna
    /// ajustáveis num asset compartilhado, sem tocar no Core.
    /// </summary>
    [CreateAssetMenu(menuName = "FavelaAmarela/Config/Locomoção", fileName = "LocomocaoConfig")]
    public sealed class LocomocaoConfig : ScriptableObject
    {
        [Header("Furtivo (Sneaking)")]
        [SerializeField] private float sneakSpeed = 2.0f;
        [SerializeField] private float sneakNoise = 2.0f;

        [Header("Andando (Walking)")]
        [SerializeField] private float walkSpeed = 4.5f;
        [SerializeField] private float walkNoise = 5.5f;

        [Header("Correndo (Running)")]
        [SerializeField] private float runSpeed = 7.5f;
        [SerializeField] private float runNoise = 8.5f;

        /// <summary>Velocidade em modo Furtivo.</summary>
        public float SneakSpeed => sneakSpeed;
        /// <summary>Raio de ruído em modo Furtivo.</summary>
        public float SneakNoise => sneakNoise;
        /// <summary>Velocidade andando.</summary>
        public float WalkSpeed => walkSpeed;
        /// <summary>Raio de ruído andando.</summary>
        public float WalkNoise => walkNoise;
        /// <summary>Velocidade correndo.</summary>
        public float RunSpeed => runSpeed;
        /// <summary>Raio de ruído correndo.</summary>
        public float RunNoise => runNoise;
    }
}
