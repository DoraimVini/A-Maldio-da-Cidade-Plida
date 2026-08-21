using FavelaAmarela.Core.Combat;
using UnityEngine;

namespace FavelaAmarela.Runtime.UI
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Barra da <b>Resiliência do Companheiro</b> — o quanto
    /// Yug-Neth aguenta antes de cair.
    ///
    /// <para><b>Por que ela nasce escondida:</b> Yug-Neth está em cena desde o começo, mas
    /// <b>cativo</b> — só vira companheiro quando o jogador o liberta do Abdul, no meio do jogo.
    /// Uma barra vazia no HUD desde o menu anunciaria um sistema que ainda não existe para o
    /// jogador, e pior, pareceria um recurso zerado. Quem a revela é o
    /// <c>HUDController.InjetarCompanheiro</c>, no instante do registro.</para>
    ///
    /// <para><b>Incapacitado não é morto.</b> A Vitalidade zerada de Yug-Neth o deixa caído até
    /// ser reanimado num Refúgio — decisão de 2026-07-31, que revogou a morte-fim-de-run estilo
    /// escolta. A barra reflete isso com uma cor apagada em vez de sumir: o jogador precisa
    /// enxergar que há alguém para reanimar.</para>
    ///
    /// Contrato de arquitetura: ver <see cref="BarraAnimada{TFonte}"/>.
    /// </summary>
    [AddComponentMenu("FavelaAmarela/UI/Companheiro Bar")]
    public sealed class CompanheiroBar : BarraAnimada<Vitalidade>
    {
        [Header("Cores por faixa")]
        [Tooltip("Cor normal — a bioluminescência dourada do Mi-Go.")]
        [SerializeField] private Color corNormal = new(0.85f, 0.72f, 0.25f, 1f);

        [Tooltip("Cor quando a Resiliência do Companheiro cruza o limiar crítico.")]
        [SerializeField] private Color corCritica = new(0.90f, 0.42f, 0.12f, 1f);

        [Tooltip("Cor com Yug-Neth caído — apagada, não ausente: ele espera reanimação.")]
        [SerializeField] private Color corIncapacitado = new(0.28f, 0.26f, 0.30f, 1f);

        [Header("Limiar crítico")]
        [Tooltip("Fração abaixo da qual a barra entra em cor crítica (0..1).")]
        [Range(0f, 0.99f)]
        [SerializeField] private float fracaoCritica = 0.35f;

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
                ? corIncapacitado
                : Fonte.Percentual <= fracaoCritica ? corCritica : corNormal;
        }
    }
}
