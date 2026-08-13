using FavelaAmarela.Core.Combat;
using UnityEngine;

namespace FavelaAmarela.Runtime.UI
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Barra de HUD que reflete a <see cref="Vitalidade"/>
    /// corpórea de Damião — a "carne", distinta da <see cref="ResilienciaMental"/> que a
    /// <see cref="ResilienciaBar"/> mostra. Duas barras, dois vetores de derrota.
    ///
    /// Contrato de arquitetura: ver <see cref="BarraAnimada{TFonte}"/>.
    ///
    /// Assets de sprite (pixel art, PPU 32, Point, sem compressão) e o layout do prefab
    /// são montados no editor da Unity. Pontos de asset marcados com [ASSET].
    /// </summary>
    [AddComponentMenu("FavelaAmarela/UI/Vitalidade Bar")]
    public sealed class VitalidadeBar : BarraAnimada<Vitalidade>
    {
        [Header("Cores por faixa (tint sobre o sprite pixel art)")]
        [Tooltip("Cor normal — carne sã.")]
        [SerializeField] private Color corNormal = new(0.72f, 0.18f, 0.18f, 1f); // vermelho-carne

        [Tooltip("Cor quando a Vitalidade cruza para baixo do limiar crítico.")]
        [SerializeField] private Color corCritica = new(0.95f, 0.45f, 0.10f, 1f); // alerta

        [Tooltip("Cor ao ser abatido (Vitalidade zerada).")]
        [SerializeField] private Color corAbatido = new(0.15f, 0.15f, 0.15f, 1f);

        [Header("Limiar crítico")]
        [Tooltip("Fração da Vitalidade abaixo da qual a barra entra em cor crítica (0..1).")]
        [Range(0f, 0.99f)]
        [SerializeField] private float fracaoCritica = 0.3f;

        protected override void Inscrever(Vitalidade fonte)
            => fonte.OnChanged += HandleVitalidadeChanged;

        protected override void Desinscrever(Vitalidade fonte)
            => fonte.OnChanged -= HandleVitalidadeChanged;

        protected override float PercentualAtual(Vitalidade fonte) => fonte.Percentual;

        private void HandleVitalidadeChanged(VitalidadeChangedArgs args)
        {
            FillAlvo = args.Percentual;
            AtualizarCor();
        }

        protected override void AtualizarCor()
        {
            if (fillImage == null || Fonte == null) return;

            fillImage.color = Fonte.EstaAbatido
                ? corAbatido
                : Fonte.Percentual <= fracaoCritica ? corCritica : corNormal;
        }
    }
}
