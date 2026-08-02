using System;

namespace FavelaAmarela.Core.Combat
{
    /// <summary>
    /// Camada POCO — sem dependências de Unity. Testável via NUnit puro em EditMode.
    ///
    /// Modela a <b>vitalidade corpórea</b> (a "carne") de um ator — distinta da
    /// <see cref="ResilienciaMental"/>, que é a sanidade. Um Cultista Amarelo, uma
    /// Aparição Primordial ou o próprio Damião podem ter vitalidade: zerá-la significa
    /// ser <b>abatido</b> (morte física), enquanto zerar a Resiliência Mental é o
    /// Colapso psicológico. Duas barras, dois vetores de derrota.
    ///
    /// Segue o mesmo contrato da <see cref="ResilienciaMental"/>: estado exposto só por
    /// propriedades de leitura, mutação só por métodos explícitos (<see cref="Ferir"/>,
    /// <see cref="Curar"/>) e o evento <see cref="OnChanged"/> como única superfície de
    /// saída (a UI e o adaptador de morte observam, sem polling).
    /// </summary>
    public sealed class Vitalidade
    {
        private float _atual;

        /// <summary>Teto máximo de vitalidade. Imutável após construção.</summary>
        public float Max { get; }

        /// <summary>Valor corrente de vitalidade (0 … Max).</summary>
        public float Atual => _atual;

        /// <summary>Percentual de vitalidade (0.0 … 1.0), útil para barras de UI.</summary>
        public float Percentual => Max > 0f ? _atual / Max : 0f;

        /// <summary>
        /// Abatido — a vitalidade chegou a zero (morte física). Para um Cultista,
        /// dispara sua saída de cena; para o Damião, a tela de derrota corpórea.
        /// </summary>
        public bool EstaAbatido => _atual <= 0f;

        /// <summary>
        /// Disparado sempre que a vitalidade muda de fato. Não dispara se o valor
        /// tentado não alterar o estado real (ex.: ferir um alvo já abatido).
        /// </summary>
        public event Action<VitalidadeChangedArgs> OnChanged;

        /// <param name="max">Vitalidade máxima. Deve ser maior que zero.</param>
        public Vitalidade(float max)
        {
            if (max <= 0f)
                throw new ArgumentOutOfRangeException(nameof(max),
                    "Vitalidade máxima deve ser maior que zero.");

            Max = max;
            _atual = max; // começa cheio
        }

        /// <summary>
        /// Aplica dano físico (equivalente diegético de TakeDamage para a carne).
        /// Reduz Atual, clampado a zero. Não dispara evento se já estiver abatido.
        /// </summary>
        /// <param name="valor">Magnitude positiva do ferimento.</param>
        public void Ferir(float valor)
        {
            if (valor < 0f)
                throw new ArgumentOutOfRangeException(nameof(valor),
                    "Ferimento deve ser um valor positivo.");
            Alterar(-valor);
        }

        /// <summary>
        /// Restaura vitalidade corpórea (cura física). Aumenta Atual, clampado ao
        /// máximo. Não dispara evento se já estiver cheio.
        /// </summary>
        /// <param name="valor">Magnitude positiva da cura.</param>
        public void Curar(float valor)
        {
            if (valor < 0f)
                throw new ArgumentOutOfRangeException(nameof(valor),
                    "Cura deve ser um valor positivo.");
            Alterar(valor);
        }

        /// <summary>
        /// Restaura a vitalidade a um valor absoluto salvo (carregamento de save).
        /// Não é uma mudança diegética (dano/cura) — é reconstrução de estado a partir
        /// do disco. O valor é clampado a [0, <see cref="Max"/>] e dispara
        /// <see cref="OnChanged"/> para a UI ressincronizar.
        /// </summary>
        /// <param name="valor">Valor absoluto a restaurar (será clampado).</param>
        public void Restaurar(float valor)
        {
            float alvo = Clamp(valor, 0f, Max);
            Alterar(alvo - _atual);
        }

        private void Alterar(float delta)
        {
            if (delta == 0f) return;

            float anterior = _atual;
            bool estavaAbatido = EstaAbatido;

            _atual = Clamp(_atual + delta, 0f, Max);

            // Clamp absorveu o delta inteiro — nenhuma mudança real.
            if (Math.Abs(_atual - anterior) < 1e-6f) return;

            OnChanged?.Invoke(new VitalidadeChangedArgs(
                valorAnterior: anterior,
                valorAtual: _atual,
                max: Max,
                acabouDeAbater: !estavaAbatido && EstaAbatido));
        }

        // Clamp puro — sem System.Math.Clamp, para manter o POCO 100% portável
        // para qualquer ambiente de teste headless (mesmo motivo de ResilienciaMental).
        private static float Clamp(float v, float min, float max)
            => v < min ? min : v > max ? max : v;
    }

    /// <summary>
    /// Payload imutável do evento <see cref="Vitalidade.OnChanged"/>.
    /// <c>readonly struct</c> evita alocação de heap em hot path de combate
    /// (Regra de Ouro 1). Observadores (UI, adaptador de morte) recebem tudo que
    /// precisam aqui — sem manter referência ao objeto <see cref="Vitalidade"/>.
    /// </summary>
    public readonly struct VitalidadeChangedArgs
    {
        /// <summary>Valor imediatamente antes da mudança.</summary>
        public readonly float ValorAnterior;

        /// <summary>Valor após a mudança (já clampado).</summary>
        public readonly float ValorAtual;

        /// <summary>Teto máximo no momento do evento.</summary>
        public readonly float Max;

        /// <summary>
        /// True somente no evento em que Atual chegou a zero (cruzou para abatido).
        /// Use para disparar a saída de cena do inimigo ou a derrota do jogador.
        /// </summary>
        public readonly bool AcabouDeAbater;

        /// <summary>Percentual corrente (0.0 … 1.0). Útil para barras de UI.</summary>
        public float Percentual => Max > 0f ? ValorAtual / Max : 0f;

        /// <summary>Delta desta mudança (positivo = cura, negativo = dano).</summary>
        public float Delta => ValorAtual - ValorAnterior;

        public VitalidadeChangedArgs(float valorAnterior, float valorAtual, float max, bool acabouDeAbater)
        {
            ValorAnterior = valorAnterior;
            ValorAtual = valorAtual;
            Max = max;
            AcabouDeAbater = acabouDeAbater;
        }
    }
}
