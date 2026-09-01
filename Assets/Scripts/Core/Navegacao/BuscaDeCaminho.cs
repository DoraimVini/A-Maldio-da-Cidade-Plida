using System;
using System.Collections.Generic;

namespace FavelaAmarela.Core.Navegacao
{
    /// <summary>
    /// Busca de caminho A* sobre a grade do mundo.
    ///
    /// <para><b>Por que existe (2026-08-31).</b> Nenhuma unidade do jogo contorna nada: o
    /// <c>EnemyMovement</c> escreve <c>linearVelocity</c> na direção do alvo e vai em linha
    /// reta. Isso nunca incomodou porque o Deserto de Hali é um <b>plano aberto com um lago</b>
    /// — não há o que contornar. No instante em que o mapa ganhar geometria, todo perseguidor
    /// encosta na parede e fica lá, e o companheiro Yug-Neth some atrás do primeiro muro.</para>
    ///
    /// <para><b>Por que A* na grade, e não NavMesh.</b> O mundo <b>já é</b> uma grade — a
    /// caminhabilidade é literalmente "o tilemap de colisão tem tile nesta célula?". Um NavMesh
    /// exigiria pacote novo, é 3D por construção, e produziria uma segunda representação do
    /// mundo para divergir da primeira. Este repositório já tem cicatrizes de duas fontes da
    /// verdade para a mesma coisa.</para>
    ///
    /// <para><b>POCO puro</b>, sem <c>UnityEngine</c>: dá para afirmar o caminho inteiro com um
    /// mapa desenhado numa string, sem cena e sem Play Mode.</para>
    /// </summary>
    public sealed class BuscaDeCaminho
    {
        /// <summary>
        /// Custo de um passo reto. Inteiros em vez de <c>float</c> de propósito: com custos
        /// inteiros a comparação de prioridade é exata, e dois caminhos de mesmo custo não
        /// alternam por erro de ponto flutuante — o que em jogo apareceria como o inimigo
        /// "tremendo" entre duas rotas.
        /// </summary>
        private const int CustoReto = 10;

        /// <summary>Diagonal ≈ √2 × reto, arredondado. O par 10/14 é o padrão da literatura.</summary>
        private const int CustoDiagonal = 14;

        /// <summary>
        /// Teto de células examinadas. Sem ele, um alvo inalcançável faz a busca varrer o mapa
        /// inteiro — e com onze Cultistas perseguindo ao mesmo tempo isso é um travamento, não
        /// uma lentidão.
        /// </summary>
        public int TetoDeNos { get; set; } = 4000;

        private readonly List<No> _abertos = new List<No>(256);
        private readonly Dictionary<Celula, No> _porCelula = new Dictionary<Celula, No>(256);
        private readonly HashSet<Celula> _fechados = new HashSet<Celula>();
        private readonly List<Celula> _caminho = new List<Celula>(64);

        private sealed class No
        {
            public Celula Onde;
            public No Veio;
            public int G;     // custo desde a origem
            public int F;     // G + heurística
            public bool Aberto;
        }

        /// <summary>
        /// Acha um caminho de <paramref name="origem"/> a <paramref name="destino"/>.
        ///
        /// <para>Devolve a lista de células <b>excluindo a origem</b> e incluindo o destino —
        /// é o que um seguidor de caminho consome: "os próximos lugares onde pisar". Lista
        /// vazia significa <b>não há caminho</b>, e quem chama precisa tratar isso: seguir em
        /// linha reta contra uma parede é exatamente o defeito que esta classe existe para
        /// acabar.</para>
        ///
        /// <para><b>A lista devolvida é reaproveitada</b> entre chamadas, para a busca não
        /// alocar em hot path (Regra de Ouro 1). Quem precisar guardá-la deve copiar.</para>
        /// </summary>
        public IReadOnlyList<Celula> Encontrar(IMapaDeNavegacao mapa, Celula origem, Celula destino)
        {
            _caminho.Clear();

            if (mapa == null) return _caminho;
            if (origem == destino) return _caminho;

            // Destino bloqueado é o caso mais comum em jogo: o alvo está "dentro" de uma parede
            // por arredondamento de célula. Devolver vazio aqui faria o inimigo desistir de
            // perseguir alguém que está a um passo -- então buscamos a célula livre mais
            // próxima do destino antes de desistir.
            if (!mapa.EhCaminhavel(destino))
            {
                if (!TentarVizinhaLivre(mapa, ref destino)) return _caminho;
            }

            if (!mapa.EhCaminhavel(origem)) return _caminho;

            _abertos.Clear();
            _porCelula.Clear();
            _fechados.Clear();

            var inicio = new No
            {
                Onde = origem, Veio = null, G = 0,
                F = Heuristica(origem, destino), Aberto = true,
            };

            _abertos.Add(inicio);
            _porCelula[origem] = inicio;

            int examinados = 0;

            while (_abertos.Count > 0)
            {
                if (++examinados > TetoDeNos) return _caminho;   // desiste: sem caminho viável

                var atual = RemoverMenorF();
                atual.Aberto = false;
                _fechados.Add(atual.Onde);

                if (atual.Onde == destino) return Reconstruir(atual);

                for (int i = 0; i < 8; i++)
                {
                    int dx = Dx[i];
                    int dy = Dy[i];

                    var vizinha = new Celula(atual.Onde.X + dx, atual.Onde.Y + dy);

                    if (_fechados.Contains(vizinha)) continue;
                    if (!mapa.EhCaminhavel(vizinha)) continue;

                    // Diagonal só passa se os DOIS ortogonais adjacentes estiverem livres.
                    // Sem isto o caminho corta quinas — e o ator, que tem largura, encosta no
                    // canto e trava. O caminho pareceria certo e o movimento estaria errado.
                    if (dx != 0 && dy != 0)
                    {
                        if (!mapa.EhCaminhavel(new Celula(atual.Onde.X + dx, atual.Onde.Y))) continue;
                        if (!mapa.EhCaminhavel(new Celula(atual.Onde.X, atual.Onde.Y + dy))) continue;
                    }

                    int g = atual.G + (dx != 0 && dy != 0 ? CustoDiagonal : CustoReto);

                    if (_porCelula.TryGetValue(vizinha, out var no))
                    {
                        if (g >= no.G) continue;   // já conhecemos rota igual ou melhor

                        no.G = g;
                        no.F = g + Heuristica(vizinha, destino);
                        no.Veio = atual;

                        if (!no.Aberto) { no.Aberto = true; _abertos.Add(no); }
                    }
                    else
                    {
                        no = new No
                        {
                            Onde = vizinha, Veio = atual, G = g,
                            F = g + Heuristica(vizinha, destino), Aberto = true,
                        };

                        _porCelula[vizinha] = no;
                        _abertos.Add(no);
                    }
                }
            }

            return _caminho;   // vazio: não há caminho
        }

        // Ordem: os quatro retos primeiro. Com custos iguais, examinar retos antes de diagonais
        // produz caminhos que parecem mais naturais e evita ziguezague desnecessário.
        private static readonly int[] Dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
        private static readonly int[] Dy = { 0, 0, 1, -1, 1, -1, 1, -1 };

        /// <summary>
        /// Distância de Chebyshev ponderada — a heurística <b>admissível</b> para movimento em
        /// 8 direções com estes custos. Admissível quer dizer que ela nunca superestima, que é
        /// o que garante que o caminho devolvido é o mais curto e não só "um caminho".
        /// </summary>
        private static int Heuristica(Celula a, Celula b)
        {
            int dx = a.X > b.X ? a.X - b.X : b.X - a.X;
            int dy = a.Y > b.Y ? a.Y - b.Y : b.Y - a.Y;

            int menor = dx < dy ? dx : dy;
            int maior = dx < dy ? dy : dx;

            return CustoDiagonal * menor + CustoReto * (maior - menor);
        }

        /// <summary>
        /// Menor F da lista aberta. Varredura linear em vez de heap: com o teto de nós acima, a
        /// lista fica pequena, e uma heap seria mais código para manter por um ganho que este
        /// jogo não sente. Se o Profiler apontar para cá, é aqui que se troca.
        /// </summary>
        private No RemoverMenorF()
        {
            int melhor = 0;

            for (int i = 1; i < _abertos.Count; i++)
                if (_abertos[i].F < _abertos[melhor].F) melhor = i;

            var no = _abertos[melhor];
            _abertos.RemoveAt(melhor);
            return no;
        }

        private IReadOnlyList<Celula> Reconstruir(No fim)
        {
            for (var no = fim; no != null && no.Veio != null; no = no.Veio)
                _caminho.Add(no.Onde);

            _caminho.Reverse();   // da origem para o destino
            return _caminho;
        }

        /// <summary>
        /// Troca um destino bloqueado pela célula livre adjacente mais próxima. É o que faz um
        /// perseguidor continuar perseguindo quando o alvo encosta numa parede.
        /// </summary>
        private static bool TentarVizinhaLivre(IMapaDeNavegacao mapa, ref Celula destino)
        {
            for (int i = 0; i < 8; i++)
            {
                var c = new Celula(destino.X + Dx[i], destino.Y + Dy[i]);
                if (!mapa.EhCaminhavel(c)) continue;

                destino = c;
                return true;
            }

            return false;
        }
    }
}
