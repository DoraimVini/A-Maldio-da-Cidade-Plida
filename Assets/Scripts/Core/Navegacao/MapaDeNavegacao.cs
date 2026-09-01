using System;

namespace FavelaAmarela.Core.Navegacao
{
    /// <summary>
    /// Uma célula da grade de navegação — coordenada inteira, igual à do <c>Grid</c> da cena.
    ///
    /// <para><b>Struct própria em vez de <c>Vector2Int</c></b> porque a camada Core não depende
    /// de <c>UnityEngine</c> (regra do <c>Core/CLAUDE.md</c>). A conversão acontece na ponte,
    /// que é o trabalho de um adaptador.</para>
    /// </summary>
    public readonly struct Celula : IEquatable<Celula>
    {
        public readonly int X;
        public readonly int Y;

        public Celula(int x, int y) { X = x; Y = y; }

        public bool Equals(Celula outra) => X == outra.X && Y == outra.Y;
        public override bool Equals(object o) => o is Celula c && Equals(c);
        public override int GetHashCode() => unchecked(X * 397 ^ Y);
        public override string ToString() => $"({X},{Y})";

        public static bool operator ==(Celula a, Celula b) => a.Equals(b);
        public static bool operator !=(Celula a, Celula b) => !a.Equals(b);
    }

    /// <summary>
    /// O que a busca de caminho precisa saber sobre o mundo: <b>onde dá para pisar</b>.
    ///
    /// <para><b>Por que uma interface, e não o Tilemap direto.</b> Assim a busca é testável com
    /// um mapa desenhado à mão numa string, sem cena, sem Unity, sem Play Mode — que é a regra
    /// do <c>Core/</c> e, aqui, também a única forma de eu conseguir verificar qualquer coisa
    /// enquanto o Editor está aberto.</para>
    ///
    /// <para>Em jogo, quem implementa é a ponte que lê o tilemap <c>Colisao</c>: célula com tile
    /// de colisão é bloqueada, o resto é chão.</para>
    /// </summary>
    public interface IMapaDeNavegacao
    {
        /// <summary>Se um ator pode ocupar esta célula.</summary>
        bool EhCaminhavel(Celula c);
    }

    /// <summary>
    /// Mapa de navegação a partir de uma matriz literal — o que os testes usam, e o que serve
    /// de referência viva de como a busca se comporta.
    ///
    /// <para>Lê um desenho onde <c>#</c> é parede e qualquer outro caractere é chão. A primeira
    /// linha do desenho é <c>y</c> mais alto, como se lê um mapa no papel.</para>
    /// </summary>
    public sealed class MapaDesenhado : IMapaDeNavegacao
    {
        private readonly bool[,] _livre;

        /// <summary>Largura em células.</summary>
        public int Largura { get; }

        /// <summary>Altura em células.</summary>
        public int Altura { get; }

        public MapaDesenhado(params string[] linhas)
        {
            if (linhas == null || linhas.Length == 0)
                throw new ArgumentException("O desenho não pode ser vazio.", nameof(linhas));

            Altura = linhas.Length;
            Largura = linhas[0].Length;

            _livre = new bool[Largura, Altura];

            for (int y = 0; y < Altura; y++)
            {
                string linha = linhas[Altura - 1 - y];   // primeira linha desenhada = y maior

                if (linha.Length != Largura)
                    throw new ArgumentException(
                        $"Todas as linhas precisam ter a mesma largura ({Largura}); a linha " +
                        $"{Altura - 1 - y} tem {linha.Length}.", nameof(linhas));

                for (int x = 0; x < Largura; x++)
                    _livre[x, y] = linha[x] != '#';
            }
        }

        /// <summary>
        /// Fora dos limites é <b>bloqueado</b>. Num mapa desenhado o limite é a borda do
        /// desenho; em jogo, quem responde é o tilemap, e sair do mundo não é caminho.
        /// </summary>
        public bool EhCaminhavel(Celula c) =>
            c.X >= 0 && c.Y >= 0 && c.X < Largura && c.Y < Altura && _livre[c.X, c.Y];
    }
}
