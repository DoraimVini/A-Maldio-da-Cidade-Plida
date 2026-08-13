using FavelaAmarela.Core.Combat;
using UnityEngine;

namespace FavelaAmarela.Runtime.UI
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Barra de HUD que reflete a
    /// <see cref="ResilienciaMental"/> de Damião.
    ///
    /// Contrato de arquitetura (herdado de <see cref="BarraAnimada{TFonte}"/>):
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
    public sealed class ResilienciaBar : BarraAnimada<ResilienciaMental>
    {
        [Header("Cores por estado (tint sobre o sprite pixel art)")]
        [SerializeField] private Color corNormal   = new(0.85f, 0.78f, 0.30f, 1f); // amarelo Carcosa
        [SerializeField] private Color corPanico    = new(0.80f, 0.20f, 0.15f, 1f); // vermelho trauma
        [SerializeField] private Color corColapso   = new(0.15f, 0.15f, 0.15f, 1f); // quase preto

        [Header("Efeitos de estado (opcionais)")]
        [Tooltip("GameObject ligado enquanto em Pânico. Ex: vinheta pulsante. [ASSET]")]
        [SerializeField] private GameObject overlayPanico;

        [Tooltip("Disparado uma vez ao entrar em Colapso. Ex: rachadura na tela. [ASSET]")]
        [SerializeField] private Animator colapsoAnimator;
        [SerializeField] private string colapsoTrigger = "Colapso";

        protected override void Inscrever(ResilienciaMental fonte)
            => fonte.OnChanged += HandleResilienciaChanged;

        protected override void Desinscrever(ResilienciaMental fonte)
            => fonte.OnChanged -= HandleResilienciaChanged;

        protected override float PercentualAtual(ResilienciaMental fonte) => fonte.Percentual;

        private void HandleResilienciaChanged(ResilienciaChangedArgs args)
        {
            // O fill busca o novo alvo; a interpolação acontece no Update da base.
            FillAlvo = args.Percentual;

            if (args.EntrouEmColapso && colapsoAnimator != null && !string.IsNullOrEmpty(colapsoTrigger))
                colapsoAnimator.SetTrigger(colapsoTrigger);

            AtualizarCor();
        }

        /// <summary>
        /// Lê Pânico/Colapso ao vivo de <see cref="BarraAnimada{TFonte}.Fonte"/> em vez de um
        /// booleano cacheado — assim funciona igual chamado pelo <c>Bind</c> (antes do primeiro
        /// evento) ou por <see cref="HandleResilienciaChanged"/>.
        /// </summary>
        protected override void AtualizarCor()
        {
            if (fillImage == null || Fonte == null) return;

            bool colapso = Fonte.IsColapso;
            bool panico = Fonte.IsPanico;

            fillImage.color = colapso ? corColapso : panico ? corPanico : corNormal;

            if (overlayPanico != null)
                overlayPanico.SetActive(panico && !colapso);
        }
    }
}
