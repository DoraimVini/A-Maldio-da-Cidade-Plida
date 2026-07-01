using FavelaAmarela.Core.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace FavelaAmarela.Runtime.UI
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Barra de HUD que reflete a
    /// <see cref="ResilienciaMental"/> de Damião.
    ///
    /// Contrato de arquitetura:
    ///   • NÃO faz polling. Reage exclusivamente ao evento OnChanged.
    ///   • NÃO contém regra de negócio — só traduz estado do Core em visual.
    ///   • É "burra": recebe a POCO por Bind() e não sabe de onde ela veio.
    ///
    /// Assets de sprite (pixel art, PPU 16, Point, sem compressão) e o layout
    /// do prefab são montados no editor da Unity — este script só dirige o
    /// preenchimento e as trocas de estado. Pontos de asset marcados com
    /// [ASSET] no Inspector.
    /// </summary>
    [AddComponentMenu("FavelaAmarela/UI/Resiliencia Bar")]
    public sealed class ResilienciaBar : MonoBehaviour
    {
        // ── Referências de cena (atribuídas no editor) ───────────────────────

        [Header("Preenchimento")]
        [Tooltip("Image com Image.Type = Filled, Fill Method = Horizontal. [ASSET pixel art]")]
        [SerializeField] private Image fillImage;

        [Tooltip("Image de fundo/trilho da barra. [ASSET pixel art]")]
        [SerializeField] private Image backgroundImage;

        [Header("Cores por estado (tint sobre o sprite pixel art)")]
        [SerializeField] private Color corNormal   = new(0.85f, 0.78f, 0.30f, 1f); // amarelo Carcosa
        [SerializeField] private Color corPanico    = new(0.80f, 0.20f, 0.15f, 1f); // vermelho trauma
        [SerializeField] private Color corColapso   = new(0.15f, 0.15f, 0.15f, 1f); // quase preto

        [Header("Transição visual")]
        [Tooltip("Velocidade de interpolação do fill (unidades de fill por segundo). 0 = instantâneo.")]
        [SerializeField] private float velocidadeLerp = 2.5f;

        [Header("Efeitos de estado (opcionais)")]
        [Tooltip("GameObject ligado enquanto em Pânico. Ex: vinheta pulsante. [ASSET]")]
        [SerializeField] private GameObject overlayPanico;

        [Tooltip("Disparado uma vez ao entrar em Colapso. Ex: rachadura na tela. [ASSET]")]
        [SerializeField] private Animator colapsoAnimator;
        [SerializeField] private string colapsoTrigger = "Colapso";

        // ── Estado interno ───────────────────────────────────────────────────

        private ResilienciaMental _fonte;
        private float _fillAlvo = 1f;    // 0..1, destino da interpolação
        private bool _bound;

        // ── Ciclo de vida / binding ──────────────────────────────────────────

        /// <summary>
        /// Conecta a barra a uma instância de ResilienciaMental.
        /// Chamado pelo HUDController quando a POCO de Damião é criada.
        /// Idempotente: re-bind troca a fonte com segurança.
        /// </summary>
        public void Bind(ResilienciaMental fonte)
        {
            if (fonte == null)
            {
                Debug.LogWarning("[ResilienciaBar] Bind recebeu fonte nula.");
                return;
            }

            Unbind(); // garante que não fica escutando duas fontes

            _fonte = fonte;
            _fonte.OnChanged += HandleResilienciaChanged;
            _bound = true;

            // Sincroniza o visual com o estado atual, sem esperar o primeiro evento
            _fillAlvo = _fonte.Percentual;
            AplicarFillImediato(_fillAlvo);
            AplicarEstadoVisual(_fonte.IsPanico, _fonte.IsColapso, forcar: true);
        }

        /// <summary>Desconecta do evento. Seguro chamar mesmo sem bind ativo.</summary>
        public void Unbind()
        {
            if (_fonte != null)
                _fonte.OnChanged -= HandleResilienciaChanged;
            _fonte = null;
            _bound = false;
        }

        private void OnDisable() => Unbind(); // nunca deixa handler pendurado

        // ── Reação ao evento (sem polling) ───────────────────────────────────

        private void HandleResilienciaChanged(ResilienciaChangedArgs args)
        {
            // O fill busca o novo alvo; a interpolação acontece no Update.
            _fillAlvo = args.Percentual;

            // Transições de estado vêm prontas no payload — sem recalcular nada.
            if (args.EntrouEmPanico) EntrarPanico();
            if (args.SaiuDoPanico)   SairPanico();
            if (args.EntrouEmColapso) EntrarColapso();
        }

        // ── Interpolação suave do preenchimento ──────────────────────────────

        private void Update()
        {
            if (!_bound || fillImage == null) return;

            float atual = fillImage.fillAmount;
            if (Mathf.Approximately(atual, _fillAlvo)) return;

            fillImage.fillAmount = velocidadeLerp <= 0f
                ? _fillAlvo
                : Mathf.MoveTowards(atual, _fillAlvo, velocidadeLerp * Time.deltaTime);
        }

        // ── Aplicação visual ─────────────────────────────────────────────────

        private void AplicarFillImediato(float valor)
        {
            if (fillImage != null) fillImage.fillAmount = Mathf.Clamp01(valor);
        }

        private void EntrarPanico()  => AplicarEstadoVisual(panico: true,  colapso: false);
        private void SairPanico()    => AplicarEstadoVisual(panico: false, colapso: false);

        private void EntrarColapso()
        {
            AplicarEstadoVisual(panico: false, colapso: true);
            if (colapsoAnimator != null && !string.IsNullOrEmpty(colapsoTrigger))
                colapsoAnimator.SetTrigger(colapsoTrigger);
        }

        private void AplicarEstadoVisual(bool panico, bool colapso, bool forcar = false)
        {
            if (fillImage != null)
            {
                Color alvo = colapso ? corColapso : panico ? corPanico : corNormal;
                fillImage.color = alvo;
            }

            if (overlayPanico != null)
                overlayPanico.SetActive(panico && !colapso);
        }
    }
}
