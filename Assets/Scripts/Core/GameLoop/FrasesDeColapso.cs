using System;

namespace FavelaAmarela.Core.GameLoop
{
    /// <summary>
    /// Fornece uma frase diegética de fim de jogo (Colapso Mental — o "Game Over"
    /// diegético, ver favela-lore-enforcer), sorteada de um pool no vocabulário do
    /// lore (Hastur, Rei em Amarelo, Carcosa, Cultista Amarelo). POCO puro, com RNG
    /// injetável (mesmo padrão de <c>BarraEnferrujada</c>) — testável sem Unity.
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

        /// <summary>Total de frases no pool.</summary>
        public int Quantidade => _pool.Length;

        /// <summary>Sorteia uma frase de Colapso do pool.</summary>
        public string Sortear()
        {
            int i = (int)(amostraAleatoria() * _pool.Length);
            if (i < 0) i = 0;
            if (i >= _pool.Length) i = _pool.Length - 1;
            return _pool[i];
        }
    }
}
