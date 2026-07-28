using UnityEngine;

namespace FavelaAmarela.Runtime.Config
{
    /// <summary>
    /// Config da Esquiva: parâmetros que alimentam o construtor do POCO
    /// <c>FavelaAmarela.Core.Abilities.Esquiva</c>. Centraliza a tunagem num asset.
    /// </summary>
    [CreateAssetMenu(menuName = "FavelaAmarela/Config/Esquiva", fileName = "EsquivaConfig")]
    public sealed class EsquivaConfig : ScriptableObject
    {
        [SerializeField] private float duration = 0.15f;
        [SerializeField] private float cooldown = 0.8f;
        [SerializeField] private float speedMultiplier = 2.5f;

        /// <summary>Duração da esquiva (s).</summary>
        public float Duration => duration;
        /// <summary>Cooldown entre esquivas (s).</summary>
        public float Cooldown => cooldown;
        /// <summary>Multiplicador de velocidade durante a esquiva.</summary>
        public float SpeedMultiplier => speedMultiplier;
    }
}
