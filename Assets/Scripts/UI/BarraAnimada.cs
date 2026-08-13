using UnityEngine;
using UnityEngine.UI;

namespace FavelaAmarela.Runtime.UI
{
    /// <summary>
    /// Base comum das barras de recurso do HUD (<see cref="ResilienciaBar"/>,
    /// <see cref="VitalidadeBar"/>, <see cref="VigorBar"/>): trilho + preenchimento com Lerp
    /// suave, o ciclo <c>Bind</c>/<c>Unbind</c>, e a garantia de nunca deixar handler
    /// pendurado (<c>OnDisable</c> chama <c>Unbind</c>).
    ///
    /// <para><b>O que NÃO está aqui, de propósito:</b> a política de cor. Extraída em
    /// 2026-08-13 depois de medir a duplicação real entre as três (~35–40 linhas = 40–50%, não
    /// os ~80% que um roadmap externo alegava) — o miolo que sobra de fora, a cor, muda de
    /// gatilho em cada uma: flags de transição no payload do evento (Resiliência), limiar local
    /// comparado à mudança (Vitalidade), booleano de evento dedicado (Vigor). Tentar unificar
    /// isso teria produzido uma abstração pior que as três implementações separadas.</para>
    ///
    /// <para>Subclasses implementam quatro pontos: assinar/cancelar o(s) evento(s) da fonte,
    /// devolver o percentual corrente (para sincronizar o fill no <c>Bind</c>) e recalcular a
    /// cor a partir de <see cref="Fonte"/> — sempre lendo o estado <b>ao vivo</b> da fonte, não
    /// um booleano cacheado no payload do evento, para <c>AtualizarCor</c> poder ser chamado a
    /// qualquer momento (inclusive no <c>Bind</c>, antes do primeiro evento) sem duplicar
    /// estado.</para>
    /// </summary>
    /// <typeparam name="TFonte">POCO ou Bridge que esta barra observa.</typeparam>
    public abstract class BarraAnimada<TFonte> : MonoBehaviour where TFonte : class
    {
        [Header("Preenchimento")]
        [Tooltip("Image com Image.Type = Filled, Fill Method = Horizontal. [ASSET pixel art]")]
        [SerializeField] protected Image fillImage;

        [Tooltip("Image de fundo/trilho da barra. [ASSET pixel art]")]
        [SerializeField] protected Image backgroundImage;

        [Header("Transição visual")]
        [Tooltip("Velocidade de interpolação do fill (unidades de fill por segundo). 0 = instantâneo.")]
        [SerializeField] protected float velocidadeLerp = 2.5f;

        /// <summary>Fonte corrente, ou <c>null</c> antes do primeiro <see cref="Bind"/>.</summary>
        protected TFonte Fonte { get; private set; }

        /// <summary>Percentual (0..1) para onde o fill está interpolando.</summary>
        protected float FillAlvo { get; set; } = 1f;

        /// <summary>Se há uma fonte conectada agora.</summary>
        protected bool Bound { get; private set; }

        /// <summary>
        /// Conecta a barra à fonte. Chamado pelo <c>HUDController</c> quando a fonte é
        /// criada/injetada. Idempotente: um re-bind troca a fonte com segurança, sem nunca
        /// ficar escutando duas ao mesmo tempo.
        /// </summary>
        public void Bind(TFonte fonte)
        {
            if (fonte == null)
            {
                Debug.LogWarning($"[{GetType().Name}] Bind recebeu fonte nula.");
                return;
            }

            Unbind();

            Fonte = fonte;
            Inscrever(fonte);
            Bound = true;

            // Sincroniza o visual com o estado atual, sem esperar o primeiro evento.
            FillAlvo = PercentualAtual(fonte);
            AplicarFillImediato(FillAlvo);
            AtualizarCor();
        }

        /// <summary>Desconecta do(s) evento(s). Seguro chamar mesmo sem bind ativo.</summary>
        public void Unbind()
        {
            if (Fonte != null) Desinscrever(Fonte);
            Fonte = null;
            Bound = false;
        }

        private void OnDisable() => Unbind(); // nunca deixa handler pendurado

        private void Update()
        {
            if (!Bound || fillImage == null) return;

            float atual = fillImage.fillAmount;
            if (Mathf.Approximately(atual, FillAlvo)) return;

            fillImage.fillAmount = velocidadeLerp <= 0f
                ? FillAlvo
                : Mathf.MoveTowards(atual, FillAlvo, velocidadeLerp * Time.deltaTime);
        }

        protected void AplicarFillImediato(float valor)
        {
            if (fillImage != null) fillImage.fillAmount = Mathf.Clamp01(valor);
        }

        /// <summary>Assina o(s) evento(s) da fonte.</summary>
        protected abstract void Inscrever(TFonte fonte);

        /// <summary>Cancela a assinatura do(s) evento(s) da fonte.</summary>
        protected abstract void Desinscrever(TFonte fonte);

        /// <summary>Percentual (0..1) corrente da fonte — usado para sincronizar o fill no Bind.</summary>
        protected abstract float PercentualAtual(TFonte fonte);

        /// <summary>
        /// Recalcula e aplica a cor de <see cref="fillImage"/>. Deve ler o estado <b>ao vivo</b>
        /// de <see cref="Fonte"/> (não um valor cacheado), para funcionar tanto chamado pelo
        /// <see cref="Bind"/> quanto por um handler de evento.
        /// </summary>
        protected abstract void AtualizarCor();
    }
}
