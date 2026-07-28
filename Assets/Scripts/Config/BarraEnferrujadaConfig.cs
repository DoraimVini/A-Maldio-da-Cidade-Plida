using UnityEngine;

namespace FavelaAmarela.Runtime.Config
{
    /// <summary>
    /// Config da Barra Enferrujada: parâmetros que alimentam o construtor do POCO
    /// <c>FavelaAmarela.Core.Abilities.BarraEnferrujada</c>. O alcance físico do golpe
    /// (usado no Overlap e no gizmo) NÃO mora aqui — é concern espacial do adaptador e
    /// continua no <c>MaoFisicaBridge</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "FavelaAmarela/Config/Barra Enferrujada", fileName = "BarraEnferrujadaConfig")]
    public sealed class BarraEnferrujadaConfig : ScriptableObject
    {
        [SerializeField] private float duration = 0.3f;
        [SerializeField] private float cooldown = 0.6f;
        [Range(0f, 1f)]
        [SerializeField] private float probabilidadeAtordoar = 0.35f;
        [SerializeField] private float duracaoAtordoamento = 2f;

        /// <summary>Duração do golpe, que trava Damião no lugar (s).</summary>
        public float Duration => duration;
        /// <summary>Cooldown entre golpes (s).</summary>
        public float Cooldown => cooldown;
        /// <summary>Chance (0..1) de o golpe atordoar o alvo.</summary>
        public float ProbabilidadeAtordoar => probabilidadeAtordoar;
        /// <summary>Duração do atordoamento aplicado, quando ocorre (s).</summary>
        public float DuracaoAtordoamento => duracaoAtordoamento;
    }
}
