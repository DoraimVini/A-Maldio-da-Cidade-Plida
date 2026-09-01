using System;

namespace FavelaAmarela.Core.Environment
{
    /// <summary>
    /// A regra que <b>esconde o Templo do Povo Serpente sob a tempestade</b>: quem entra na
    /// área sem a carta é arremessado para outro canto do Deserto.
    ///
    /// <para><b>A ideia é do Vini (2026-09-01):</b> <i>"vamos esconder o templo debaixo da
    /// tempestade — se não tiver o mapa, a tempestade joga o Damião para outro canto."</i></para>
    ///
    /// <para><b>Por que isto é bom para este jogo em particular.</b> A Tempestade de Memória já
    /// abafa som e já é o sistema que diferencia setores; usá-la também como <b>véu</b> dá a ela
    /// um segundo papel sem inventar mecânica nova. E transforma o Templo — que é conteúdo
    /// opcional, a Dungeon 2 — em algo que se <b>descobre</b> em vez de algo que está no mapa
    /// desde o começo.</para>
    ///
    /// <para><b>Manda para um canto DIFERENTE, e sempre o mais distante entre os candidatos.</b>
    /// Sortear qualquer canto poderia devolver o jogador a dois passos de onde estava, e aí a
    /// tempestade pareceria quebrada em vez de perigosa. O custo tem de ser sentido — mas
    /// sentido como <i>regra</i>, não como azar.</para>
    ///
    /// <para>POCO puro: dá para afirmar o destino sem cena, sem Play Mode e sem tempestade.</para>
    /// </summary>
    public sealed class DesorientacaoDaTempestade
    {
        /// <summary>Um ponto do mundo, em duas dimensões. Evita depender de <c>UnityEngine</c>.</summary>
        public readonly struct Ponto
        {
            public readonly float X;
            public readonly float Y;

            public Ponto(float x, float y) { X = x; Y = y; }

            public float DistanciaAte(Ponto outro)
            {
                float dx = X - outro.X;
                float dy = Y - outro.Y;
                return (float)Math.Sqrt(dx * dx + dy * dy);
            }

            public override string ToString() => $"({X:0.#}, {Y:0.#})";
        }

        private readonly Ponto[] _cantos;

        /// <summary>
        /// Distância abaixo da qual dois pontos contam como "o mesmo canto". Sem ela, um canto a
        /// um passo do jogador seria um destino legal, e a tempestade não teria consequência.
        /// </summary>
        public float RaioDoMesmoCanto { get; set; } = 12f;

        /// <param name="cantos">
        /// Os destinos possíveis, em coordenadas do mundo. Tipicamente os quatro cantos
        /// jogáveis do Deserto — mas a regra não sabe disso, e é o que a torna testável.
        /// </param>
        public DesorientacaoDaTempestade(params Ponto[] cantos)
        {
            if (cantos == null || cantos.Length < 2)
                throw new ArgumentException(
                    "São precisos ao menos dois cantos: com um só, a tempestade devolveria o " +
                    "jogador exatamente onde ele estava.", nameof(cantos));

            _cantos = (Ponto[])cantos.Clone();
        }

        /// <summary>
        /// Para onde a tempestade arremessa quem está em <paramref name="de"/>.
        ///
        /// <para>Devolve o canto <b>mais distante</b> dentre os que não são o próprio canto do
        /// jogador. É determinístico de propósito: um jogador que aprende a regra aprende que o
        /// preço de insistir sem a carta é a travessia inteira de volta — e aprender uma regra é
        /// jogo, enquanto sofrer um sorteio é frustração.</para>
        /// </summary>
        public Ponto Arremessar(Ponto de)
        {
            Ponto melhor = _cantos[0];
            float maior = -1f;

            for (int i = 0; i < _cantos.Length; i++)
            {
                float d = de.DistanciaAte(_cantos[i]);

                // O canto onde o jogador já está não é destino.
                if (d <= RaioDoMesmoCanto) continue;

                if (d > maior)
                {
                    maior = d;
                    melhor = _cantos[i];
                }
            }

            // Todos os cantos dentro do raio: mapa pequeno demais para a regra, ou raio grande
            // demais. Devolve o mais distante mesmo assim -- arremessar para perto é melhor que
            // não arremessar, porque a promessa feita ao jogador é que algo acontece.
            if (maior < 0f)
            {
                foreach (var c in _cantos)
                {
                    float d = de.DistanciaAte(c);
                    if (d > maior) { maior = d; melhor = c; }
                }
            }

            return melhor;
        }

        /// <summary>
        /// Se a tempestade deve agir. Separado de <see cref="Arremessar"/> para quem chama poder
        /// decidir <i>antes</i> de mexer no jogador — e para o teste afirmar a condição sem
        /// precisar de um destino.
        /// </summary>
        public static bool DeveArremessar(bool temACarta) => !temACarta;
    }
}
