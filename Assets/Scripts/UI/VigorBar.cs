using FavelaAmarela.Player;
using UnityEngine;

namespace FavelaAmarela.Runtime.UI
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Barra de HUD que reflete o
    /// <see cref="GerenciadorDeVigor"/> de Damião.
    ///
    /// Contrato de arquitetura: ver <see cref="BarraAnimada{TFonte}"/>.
    /// </summary>
    [AddComponentMenu("FavelaAmarela/UI/Vigor Bar")]
    public sealed class VigorBar : BarraAnimada<GerenciadorDeVigor>
    {
        [Header("Cores por estado (tint sobre o sprite)")]
        [SerializeField] private Color corNormal = new(0.35f, 0.70f, 0.25f, 1f); // Verde Vigor
        [SerializeField] private Color corExausto = new(0.40f, 0.40f, 0.40f, 1f); // Cinza apagado quando exausto

        protected override void Inscrever(GerenciadorDeVigor fonte)
        {
            fonte.OnVigorChanged += HandleVigorChanged;
            fonte.OnExaustaoChanged += HandleExaustaoChanged;
        }

        protected override void Desinscrever(GerenciadorDeVigor fonte)
        {
            fonte.OnVigorChanged -= HandleVigorChanged;
            fonte.OnExaustaoChanged -= HandleExaustaoChanged;
        }

        protected override float PercentualAtual(GerenciadorDeVigor fonte)
            => fonte.VigorAtual / Mathf.Max(1f, fonte.VigorMaximo);

        private void HandleVigorChanged(float atual, float maximo)
            => FillAlvo = atual / Mathf.Max(1f, maximo);

        private void HandleExaustaoChanged(bool estaExausto) => AtualizarCor();

        protected override void AtualizarCor()
        {
            if (fillImage == null || Fonte == null) return;
            fillImage.color = Fonte.EstaExausto ? corExausto : corNormal;
        }
    }
}
