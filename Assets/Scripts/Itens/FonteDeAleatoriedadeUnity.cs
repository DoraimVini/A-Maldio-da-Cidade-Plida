using FavelaAmarela.Core.Loot;

namespace FavelaAmarela.Runtime.Itens
{
    /// <summary>
    /// Implementação de produção da <see cref="IFonteDeAleatoriedade"/>, sobre
    /// <c>System.Random</c>. Não usa <c>UnityEngine.Random</c> de propósito: aquele é
    /// estático e global, e uma instância própria permite semear uma partida inteira
    /// (ou um teste) sem interferir no resto do jogo.
    /// </summary>
    public sealed class FonteDeAleatoriedadeUnity : IFonteDeAleatoriedade
    {
        private readonly System.Random _random;

        /// <summary>Cria uma fonte com semente arbitrária.</summary>
        public FonteDeAleatoriedadeUnity() => _random = new System.Random();

        /// <summary>Cria uma fonte com semente fixa — mesma semente, mesma sequência.</summary>
        public FonteDeAleatoriedadeUnity(int semente) => _random = new System.Random(semente);

        /// <inheritdoc />
        public float ProximoValor() => (float)_random.NextDouble();

        /// <inheritdoc />
        public int ProximoInteiro(int minInclusivo, int maxExclusivo)
        {
            if (maxExclusivo <= minInclusivo) return minInclusivo;
            return _random.Next(minInclusivo, maxExclusivo);
        }
    }
}
