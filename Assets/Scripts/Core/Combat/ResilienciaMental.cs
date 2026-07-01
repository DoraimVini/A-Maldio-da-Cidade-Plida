using System;

namespace FavelaAmarela.Core.Combat
{
    /// <summary>
    /// Camada POCO — sem dependências de Unity. Testável via NUnit puro em EditMode.
    ///
    /// Modela a sanidade/vitalidade de Damião segundo o vocabulário diegético
    /// do lore-enforcer (favela-lore-enforcer SKILL):
    ///   HP         → Resiliência Mental
    ///   TakeDamage → SofrerTrauma
    ///   Heal       → Ancorar  (estabilização psicológica)
    ///
    /// Três estados internos:
    ///   Normal  — Atual > ThresholdPanico
    ///   Pânico  — 0 &lt; Atual ≤ ThresholdPanico  (mecânicas de horror ativadas)
    ///   Colapso — Atual ≤ 0                      (game over / cutscene narrativa)
    ///
    /// O evento OnChanged é a única superfície de saída — toda a camada de
    /// UI (ResilienciaBar), áudio (DynamicMusicController) e câmera
    /// (CameraShake) observa este evento, sem polling nem referência ao
    /// objeto de jogo.
    /// </summary>
    public sealed class ResilienciaMental
    {
        // ── Estado privado ───────────────────────────────────────────────────

        private float _atual;

        // ── Propriedades públicas (somente leitura) ──────────────────────────

        /// <summary>Teto máximo de resiliência. Imutável após construção.</summary>
        public float Max { get; }

        /// <summary>
        /// Valor absoluto abaixo do qual o estado de Pânico é ativado.
        /// Imutável após construção. Definir como fração: use o factory
        /// <see cref="ComThresholdFracional"/>.
        /// </summary>
        public float ThresholdPanico { get; }

        /// <summary>Valor corrente de resiliência (0 … Max).</summary>
        public float Atual => _atual;

        /// <summary>Percentual de resiliência (0.0 … 1.0).</summary>
        public float Percentual => Max > 0f ? _atual / Max : 0f;

        /// <summary>
        /// Pânico ativo quando há vida mas ela está abaixo do threshold.
        /// Neste estado, o jogo habilita: câmera trêmula, música de terror,
        /// distorção de shader e percepção de Espectros sem o Amuleto.
        /// </summary>
        public bool IsPanico => _atual > 0f && _atual <= ThresholdPanico;

        /// <summary>
        /// Colapso total — Damião perdeu toda a resiliência.
        /// Dispara cutscene ou tela de "Perdido em Carcosa".
        /// </summary>
        public bool IsColapso => _atual <= 0f;

        /// <summary>Quantos pontos faltam para cruzar o threshold de pânico.</summary>
        public float MargemAtePanico => Math.Max(0f, _atual - ThresholdPanico);

        // ── Evento ───────────────────────────────────────────────────────────

        /// <summary>
        /// Disparado toda vez que o valor de resiliência muda de fato.
        /// Não dispara se o valor tentado não alterar o estado real
        /// (ex: trauma aplicado com Atual já em zero).
        /// </summary>
        public event Action<ResilienciaChangedArgs> OnChanged;

        // ── Construção ───────────────────────────────────────────────────────

        /// <param name="max">Resiliência máxima. Deve ser maior que zero.</param>
        /// <param name="thresholdPanico">
        /// Valor absoluto do limiar de pânico. Deve ser ≥ 0 e menor que max.
        /// </param>
        public ResilienciaMental(float max, float thresholdPanico)
        {
            if (max <= 0f)
                throw new ArgumentOutOfRangeException(nameof(max),
                    "Resiliência máxima deve ser maior que zero.");
            if (thresholdPanico < 0f)
                throw new ArgumentOutOfRangeException(nameof(thresholdPanico),
                    "Threshold de pânico não pode ser negativo.");
            if (thresholdPanico >= max)
                throw new ArgumentOutOfRangeException(nameof(thresholdPanico),
                    "Threshold de pânico deve ser menor que o máximo.");

            Max             = max;
            ThresholdPanico = thresholdPanico;
            _atual          = max; // começa cheio
        }

        /// <summary>
        /// Factory: define o threshold como fração do máximo.
        /// Ex: <c>ComThresholdFracional(100f, 0.25f)</c> → threshold em 25.
        /// </summary>
        /// <param name="fracao">Entre 0 (inclusive) e 1 (exclusive).</param>
        public static ResilienciaMental ComThresholdFracional(float max, float fracao)
        {
            if (fracao < 0f || fracao >= 1f)
                throw new ArgumentOutOfRangeException(nameof(fracao),
                    "Fração deve estar no intervalo [0, 1).");
            return new ResilienciaMental(max, max * fracao);
        }

        // ── API pública — vocabulário diegético ─────────────────────────────

        /// <summary>
        /// Aplica trauma psicológico (equivalente diegético de TakeDamage).
        /// Reduz Atual; clampado a zero. Não dispara evento se já estiver em zero.
        /// </summary>
        /// <param name="valor">Magnitude positiva do trauma.</param>
        public void SofrerTrauma(float valor)
        {
            if (valor < 0f)
                throw new ArgumentOutOfRangeException(nameof(valor),
                    "Trauma deve ser um valor positivo.");
            Alterar(-valor);
        }

        /// <summary>
        /// Ancora a mente de Damião (equivalente diegético de Heal).
        /// Aumenta Atual; clampado ao máximo. Não dispara evento se já estiver cheio.
        /// </summary>
        /// <param name="valor">Magnitude positiva da ancoragem.</param>
        public void Ancorar(float valor)
        {
            if (valor < 0f)
                throw new ArgumentOutOfRangeException(nameof(valor),
                    "Ancoragem deve ser um valor positivo.");
            Alterar(valor);
        }

        /// <summary>
        /// Estabilização completa — restaura ao máximo de uma vez.
        /// Equivale a uma cutscene de recuperação. Não dispara evento se já no teto.
        /// </summary>
        public void EstabilizarCompletamente() => Alterar(Max - _atual);

        /// <summary>
        /// Força colapso imediato — útil para eventos narrativos (emboscada,
        /// visão de Hastur) que devem ignorar a resiliência atual.
        /// Dispara OnChanged com <see cref="ResilienciaChangedArgs.EntrouEmColapso"/> = true.
        /// </summary>
        public void ForcarColapso() => Alterar(-_atual);

        // ── Núcleo privado ───────────────────────────────────────────────────

        private void Alterar(float delta)
        {
            if (delta == 0f) return;

            float anterior    = _atual;
            bool estavaPanico = IsPanico;
            bool estaColapso  = IsColapso;

            _atual = Clamp(_atual + delta, 0f, Max);

            // Clamp absorveu o delta inteiro — nenhuma mudança real
            if (Math.Abs(_atual - anterior) < 1e-6f) return;

            bool agoraPanico  = IsPanico;
            bool agoraColapso = IsColapso;

            OnChanged?.Invoke(new ResilienciaChangedArgs(
                valorAnterior:   anterior,
                valorAtual:      _atual,
                max:             Max,
                thresholdPanico: ThresholdPanico,
                entrouEmPanico:  !estavaPanico && agoraPanico,
                saiuDoPanico:    estavaPanico   && !agoraPanico,
                entrouEmColapso: !estaColapso   && agoraColapso
            ));
        }

        // Clamp puro — sem System.Math.Clamp (disponível só em .NET Standard 2.1+,
        // garantido no Unity 6, mas escrito explicitamente para deixar o POCO
        // 100% portável para qualquer ambiente de teste headless).
        private static float Clamp(float v, float min, float max)
            => v < min ? min : v > max ? max : v;
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Payload imutável do evento <see cref="ResilienciaMental.OnChanged"/>.
    ///
    /// Struct readonly evita alocação de heap em hot path (cada frame de combate
    /// pode disparar múltiplos eventos).
    ///
    /// Observadores (UI, áudio, câmera) recebem tudo que precisam aqui —
    /// sem precisar manter referência ao objeto ResilienciaMental.
    /// </summary>
    public readonly struct ResilienciaChangedArgs
    {
        // ── Valores do momento da mudança ────────────────────────────────────

        /// <summary>Valor imediatamente antes da mudança.</summary>
        public readonly float ValorAnterior;

        /// <summary>Valor após a mudança (já clampado).</summary>
        public readonly float ValorAtual;

        /// <summary>Teto máximo no momento do evento.</summary>
        public readonly float Max;

        /// <summary>Threshold de pânico no momento do evento.</summary>
        public readonly float ThresholdPanico;

        // ── Sinalizadores de transição de estado ─────────────────────────────

        /// <summary>
        /// True somente no frame em que Damião cruza o threshold DESCENDO
        /// (Normal → Pânico). Use para ativar efeitos de entrada no horror.
        /// </summary>
        public readonly bool EntrouEmPanico;

        /// <summary>
        /// True somente no frame em que Damião cruza o threshold SUBINDO
        /// (Pânico → Normal). Use para desativar efeitos de horror.
        /// </summary>
        public readonly bool SaiuDoPanico;

        /// <summary>
        /// True somente no frame em que Atual chegou a zero.
        /// Use para disparar cutscene de colapso ou tela de morte.
        /// </summary>
        public readonly bool EntrouEmColapso;

        // ── Propriedades calculadas ───────────────────────────────────────────

        /// <summary>Percentual corrente (0.0 … 1.0). Útil para barras de UI.</summary>
        public float Percentual     => Max > 0f ? ValorAtual / Max : 0f;

        /// <summary>Estado de pânico após a mudança.</summary>
        public bool EstaPanico      => ValorAtual > 0f && ValorAtual <= ThresholdPanico;

        /// <summary>Estado de colapso após a mudança.</summary>
        public bool EstaEmColapso   => ValorAtual <= 0f;

        /// <summary>Delta desta mudança (positivo = cura, negativo = dano).</summary>
        public float Delta          => ValorAtual - ValorAnterior;

        // ── Construtor ───────────────────────────────────────────────────────

        public ResilienciaChangedArgs(
            float valorAnterior,
            float valorAtual,
            float max,
            float thresholdPanico,
            bool  entrouEmPanico,
            bool  saiuDoPanico,
            bool  entrouEmColapso)
        {
            ValorAnterior   = valorAnterior;
            ValorAtual      = valorAtual;
            Max             = max;
            ThresholdPanico = thresholdPanico;
            EntrouEmPanico  = entrouEmPanico;
            SaiuDoPanico    = saiuDoPanico;
            EntrouEmColapso = entrouEmColapso;
        }
    }
}
