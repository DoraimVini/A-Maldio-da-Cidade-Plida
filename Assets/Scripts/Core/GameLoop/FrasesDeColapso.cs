using System;

namespace FavelaAmarela.Core.GameLoop
{
    /// <summary>
    /// Como Damião foi derrotado. Os dois caminhos terminam no mesmo fim de jogo
    /// (<see cref="GameState.Colapso"/>), mas a frase final difere — morrer de porrada
    /// não é "abraçar Hastur".
    ///
    /// <para><b>Nota histórica (2026-07-31):</b> havia um terceiro valor,
    /// <c>EscoltaPerdida</c> — Yug-Neth morto encerrava a run na hora, sem resgate
    /// (estilo Ashley/RE4). Revogado: a morte dele agora é <b>incapacitação recuperável</b>
    /// (cai no lugar, bloqueia os Portões de Carcosa até ser reanimado num Refúgio), não
    /// mais fim de jogo. Ver <c>YugNethAI.EstaIncapacitado</c> e <c>RefugioDeLuz</c>.</para>
    /// </summary>
    public enum TipoDeDerrota
    {
        /// <summary>Resiliência Mental a zero — a Cidade Pálida tomou a mente.</summary>
        Mental,
        /// <summary>Vitalidade corpórea a zero — o corpo cedeu (golpes, garras, lâminas).</summary>
        Corporea,
    }

    /// <summary>
    /// Fornece uma frase diegética de fim de jogo (o "Game Over" diegético, ver
    /// favela-lore-enforcer), sorteada de um pool no vocabulário do lore (Hastur, Rei em
    /// Amarelo, Carcosa, Cultista Amarelo). POCO puro, com RNG injetável (RNG
    /// determinístico para testes) — testável sem Unity.
    ///
    /// Há um pool por <see cref="TipoDeDerrota"/>: o Colapso Mental fala de lucidez
    /// dissolvida; a morte corpórea fala do corpo caído na areia. Ambos evitam
    /// vocabulário genérico ("You died", "HP zerado").
    /// </summary>
    public sealed class FrasesDeColapso
    {
        private static readonly string[] _pool =
        {
            "Você abraçou Hastur.",
            "A loucura de Carcosa tomou sua mente.",
            "Você se tornou mais um Cultista Amarelo.",
            "O Rei em Amarelo reclamou o que era dele.",
            "A Máscara Pálida agora é o seu rosto.",
            "Sua lucidez se dissolveu na Cidade Pálida.",
        };

        private static readonly string[] _poolCorporea =
        {
            "Seu corpo cedeu sob a Máscara de Gesso.",
            "A areia de Hali bebeu o que restou de você.",
            "A Tumba ganhou mais um osso.",
            "Sua carne falhou antes da sua mente.",
            "Você caiu, e Carcosa nem notou.",
            "O deserto fechou-se sobre Damião.",
        };

        private readonly Func<double> amostraAleatoria;
        private static readonly Random _randomPadrao = new Random();

        /// <param name="amostraAleatoria">
        /// Fonte de números em [0, 1) para sortear a frase. Injetável para testes
        /// determinísticos. Usa <see cref="Random"/> padrão se omitido.
        /// </param>
        public FrasesDeColapso(Func<double> amostraAleatoria = null)
        {
            this.amostraAleatoria = amostraAleatoria ?? (() => _randomPadrao.NextDouble());
        }

        /// <summary>Total de frases no pool de Colapso Mental.</summary>
        public int Quantidade => _pool.Length;

        /// <summary>Total de frases no pool do tipo de derrota indicado.</summary>
        public int QuantidadePara(TipoDeDerrota tipo) => PoolDe(tipo).Length;

        /// <summary>Sorteia uma frase de Colapso Mental (derrota por Resiliência a zero).</summary>
        public string Sortear() => Sortear(TipoDeDerrota.Mental);

        /// <summary>Sorteia uma frase do pool correspondente ao tipo de derrota.</summary>
        public string Sortear(TipoDeDerrota tipo)
        {
            var pool = PoolDe(tipo);
            int i = (int)(amostraAleatoria() * pool.Length);
            if (i < 0) i = 0;
            if (i >= pool.Length) i = pool.Length - 1;
            return pool[i];
        }

        private static string[] PoolDe(TipoDeDerrota tipo) => tipo switch
        {
            TipoDeDerrota.Corporea => _poolCorporea,
            _ => _pool,
        };
    }
}
