using FavelaAmarela.Core.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace FavelaAmarela.Runtime.UI
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Barra de HUD que reflete a <see cref="Vitalidade"/>
    /// corpórea de Damião — a "carne", distinta da <see cref="ResilienciaMental"/> que a
    /// <see cref="ResilienciaBar"/> mostra. Duas barras, dois vetores de derrota.
    ///
    /// Contrato de arquitetura (mesmo da ResilienciaBar):
    ///   • NÃO faz polling. Reage exclusivamente ao evento OnChanged.
    ///   • NÃO contém regra de negócio — só traduz estado do Core em visual.
    ///   • É "burra": recebe a POCO por Bind() e não sabe de onde ela veio.
    ///
    /// Assets de sprite (pixel art, PPU 32, Point, sem compressão) e o layout do prefab
    /// são montados no editor da Unity. Pontos de asset marcados com [ASSET].
    /// </summary>
    [AddComponentMenu("FavelaAmarela/UI/Vitalidade Bar")]
    public sealed class VitalidadeBar : MonoBehaviour
    {
        [Header("Preenchimento")]
        [Tooltip("Image com Image.Type = Filled, Fill Method = Horizontal. [ASSET pixel art]")]
        [SerializeField] private Image fillImage;

        [Tooltip("Image de fundo/trilho da barra. [ASSET pixel art]")]
        [SerializeField] private Image backgroundImage;

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

        [Header("Transição visual")]
        [Tooltip("Velocidade de interpolação do fill (unidades de fill por segundo). 0 = instantâneo.")]
        [SerializeField] private float velocidadeLerp = 2.5f;

        private Vitalidade _fonte;
        private float _fillAlvo = 1f;
        private bool _bound;

        /// <summary>
        /// Conecta a barra a uma instância de <see cref="Vitalidade"/>. Chamado pelo
        /// <c>HUDController</c>. Idempotente: re-bind troca a fonte com segurança.
        /// </summary>
        public void Bind(Vitalidade fonte)
        {
            if (fonte == null)
            {
                Debug.LogWarning("[VitalidadeBar] Bind recebeu fonte nula.");
                return;
            }

            Unbind(); // garante que não fica escutando duas fontes

            _fonte = fonte;
            _fonte.OnChanged += HandleVitalidadeChanged;
            _bound = true;

            // Sincroniza o visual com o estado atual, sem esperar o primeiro evento.
            _fillAlvo = _fonte.Percentual;
            if (fillImage != null) fillImage.fillAmount = Mathf.Clamp01(_fillAlvo);
            AplicarCor(_fillAlvo, _fonte.EstaAbatido);
        }

        /// <summary>Desconecta do evento. Seguro chamar mesmo sem bind ativo.</summary>
        public void Unbind()
        {
            if (_fonte != null)
                _fonte.OnChanged -= HandleVitalidadeChanged;
            _fonte = null;
            _bound = false;
        }

        private void OnDisable() => Unbind(); // nunca deixa handler pendurado

        private void HandleVitalidadeChanged(VitalidadeChangedArgs args)
        {
            // O fill busca o novo alvo; a interpolação acontece no Update.
            _fillAlvo = args.Percentual;
            AplicarCor(args.Percentual, args.ValorAtual <= 0f);
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

        private void AplicarCor(float percentual, bool abatido)
        {
            if (fillImage == null) return;

            fillImage.color = abatido
                ? corAbatido
                : percentual <= fracaoCritica ? corCritica : corNormal;
        }
    }
}
