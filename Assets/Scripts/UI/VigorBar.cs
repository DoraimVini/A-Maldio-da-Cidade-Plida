using FavelaAmarela.Player;
using UnityEngine;
using UnityEngine.UI;

namespace FavelaAmarela.Runtime.UI
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Barra de HUD que reflete o
    /// <see cref="GerenciadorDeVigor"/> de Damião.
    ///
    /// Contrato de arquitetura:
    ///   • NÃO faz polling. Reage exclusivamente aos eventos OnVigorChanged e OnExaustaoChanged.
    ///   • NÃO contém regra de negócio — só traduz estado do Player em visual.
    ///   • É "burra": recebe a fonte por Bind() e não sabe de onde ela veio.
    /// </summary>
    [AddComponentMenu("FavelaAmarela/UI/Vigor Bar")]
    public sealed class VigorBar : MonoBehaviour
    {
        [Header("Preenchimento")]
        [Tooltip("Image com Image.Type = Filled, Fill Method = Horizontal. Use a barGreen_horizontalMid aqui.")]
        [SerializeField] private Image fillImage;

        [Tooltip("Image de fundo/trilho da barra. Use barBack_horizontalMid aqui.")]
        [SerializeField] private Image backgroundImage;

        [Header("Cores por estado (tint sobre o sprite)")]
        [SerializeField] private Color corNormal = new(0.35f, 0.70f, 0.25f, 1f); // Verde Vigor
        [SerializeField] private Color corExausto = new(0.40f, 0.40f, 0.40f, 1f); // Cinza apagado quando exausto

        [Header("Transição visual")]
        [Tooltip("Velocidade de interpolação do fill (unidades de fill por segundo). 0 = instantâneo.")]
        [SerializeField] private float velocidadeLerp = 4f;

        private GerenciadorDeVigor _fonte;
        private float _fillAlvo = 1f;
        private bool _bound;

        public void Bind(GerenciadorDeVigor fonte)
        {
            if (fonte == null) return;
            Unbind();

            _fonte = fonte;
            _fonte.OnVigorChanged += HandleVigorChanged;
            _fonte.OnExaustaoChanged += HandleExaustaoChanged;
            _bound = true;

            // Sincroniza estado inicial
            _fillAlvo = _fonte.VigorAtual / Mathf.Max(1f, _fonte.VigorMaximo);
            AplicarFillImediato(_fillAlvo);
            AplicarEstadoVisual(_fonte.EstaExausto);
        }

        public void Unbind()
        {
            if (_fonte != null)
            {
                _fonte.OnVigorChanged -= HandleVigorChanged;
                _fonte.OnExaustaoChanged -= HandleExaustaoChanged;
            }
            _fonte = null;
            _bound = false;
        }

        private void OnDisable() => Unbind();

        private void HandleVigorChanged(float atual, float maximo)
        {
            _fillAlvo = atual / Mathf.Max(1f, maximo);
        }

        private void HandleExaustaoChanged(bool estaExausto)
        {
            AplicarEstadoVisual(estaExausto);
        }

        private void Update()
        {
            if (!_bound || fillImage == null) return;

            float atual = fillImage.fillAmount;
            if (Mathf.Approximately(atual, _fillAlvo)) return;

            fillImage.fillAmount = velocidadeLerp <= 0f
                ? _fillAlvo
                : Mathf.MoveTowards(atual, _fillAlvo, velocidadeLerp * Time.deltaTime);
        }

        private void AplicarFillImediato(float valor)
        {
            if (fillImage != null) fillImage.fillAmount = Mathf.Clamp01(valor);
        }

        private void AplicarEstadoVisual(bool estaExausto)
        {
            if (fillImage != null)
            {
                fillImage.color = estaExausto ? corExausto : corNormal;
            }
        }
    }
}
