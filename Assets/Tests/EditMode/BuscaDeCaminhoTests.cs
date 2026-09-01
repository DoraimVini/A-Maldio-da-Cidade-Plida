using System.Linq;
using NUnit.Framework;
using FavelaAmarela.Core.Navegacao;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a <b>busca de caminho</b> — a peça que faz uma unidade contornar em vez de
    /// encostar na parede.
    ///
    /// <para><b>O estado que ela conserta (2026-08-31).</b> Nenhuma das nove unidades que se
    /// movem contorna nada: todas escrevem velocidade na direção do alvo. Isso nunca incomodou
    /// porque o Deserto de Hali é um plano aberto com um lago — não há o que contornar. É
    /// dívida invisível que vence no dia em que o mapa ganhar geometria.</para>
    ///
    /// <para>Os mapas destes testes são <b>desenhados</b>: <c>#</c> é parede, o resto é chão, e
    /// a primeira linha é o <c>y</c> mais alto — como se lê um mapa no papel. Ler o teste é ver
    /// o problema.</para>
    /// </summary>
    public sealed class BuscaDeCaminhoTests
    {
        private static readonly BuscaDeCaminho Busca = new BuscaDeCaminho();

        // ── O básico ──────────────────────────────────────────────────────────

        [Test]
        public void EmCampoAberto_VaiEmLinhaReta()
        {
            var mapa = new MapaDesenhado(
                ".....",
                ".....",
                ".....");

            var caminho = Busca.Encontrar(mapa, new Celula(0, 1), new Celula(4, 1)).ToList();

            Assert.AreEqual(4, caminho.Count,
                "Sem obstáculo, quatro passos para andar quatro células.");

            Assert.AreEqual(new Celula(4, 1), caminho[^1]);
        }

        [Test]
        public void AOrigem_NaoEntraNoCaminho()
        {
            var mapa = new MapaDesenhado("...");
            var caminho = Busca.Encontrar(mapa, new Celula(0, 0), new Celula(2, 0));

            CollectionAssert.DoesNotContain(caminho, new Celula(0, 0),
                "O caminho é 'os próximos lugares onde pisar' — quem anda já está na origem.");
        }

        /// <summary>
        /// <b>O caso que motivou tudo isto.</b> Uma parede entre perseguidor e alvo: em linha
        /// reta o inimigo encosta e trava; com busca, ele dá a volta.
        /// </summary>
        [Test]
        public void ComParedeNoMeio_ContornaEmVezDeTravar()
        {
            var mapa = new MapaDesenhado(
                ".....",
                ".###.",
                ".....",
                ".###.",
                ".....");

            var caminho = Busca.Encontrar(mapa, new Celula(2, 0), new Celula(2, 4)).ToList();

            Assert.IsNotEmpty(caminho,
                "Há caminho pelas laterais — devolver vazio faria o inimigo desistir de um alvo " +
                "alcançável.");

            Assert.AreEqual(new Celula(2, 4), caminho[^1]);

            foreach (var c in caminho)
                Assert.IsTrue(mapa.EhCaminhavel(c), $"O caminho passou por dentro da parede em {c}.");
        }

        [Test]
        public void SemCaminho_DevolveVazio()
        {
            var mapa = new MapaDesenhado(
                "..#..",
                "..#..",
                "..#..");

            var caminho = Busca.Encontrar(mapa, new Celula(0, 1), new Celula(4, 1));

            Assert.IsEmpty(caminho,
                "Alvo emparedado tem de devolver vazio — e quem chama precisa tratar isso, em " +
                "vez de andar contra a parede.");
        }

        // ── Geometria: os erros que só aparecem em movimento ──────────────────

        /// <summary>
        /// <b>Cortar quina é o defeito mais traiçoeiro da busca em grade.</b> O caminho parece
        /// certo no papel — as duas células são livres e adjacentes na diagonal — mas o ator
        /// tem largura, e ao atravessar o vértice ele encosta no canto e trava. O sintoma se lê
        /// como "a IA travou", não como "o caminho estava errado".
        /// </summary>
        [Test]
        public void NuncaCortaQuina()
        {
            // A primeira linha desenhada é o y MAIOR, então este desenho dá:
            //   (0,1) parede   (1,1) livre   <- linha "#."
            //   (0,0) livre    (1,0) parede  <- linha ".#"
            // Ir de (0,0) a (1,1) na diagonal atravessaria o vértice entre as duas paredes.
            var mapa = new MapaDesenhado(
                "#.",
                ".#");

            var caminho = Busca.Encontrar(mapa, new Celula(0, 0), new Celula(1, 1));

            Assert.IsEmpty(caminho,
                "A diagonal entre duas paredes foi aceita. O caminho parece válido e o ator " +
                "trava no canto — o pior tipo de bug, porque a evidência aponta para o lugar " +
                "errado.");
        }

        [Test]
        public void ADiagonal_EhPreferidaQuandoLivre()
        {
            var mapa = new MapaDesenhado(
                "...",
                "...",
                "...");

            var caminho = Busca.Encontrar(mapa, new Celula(0, 0), new Celula(2, 2)).ToList();

            Assert.AreEqual(2, caminho.Count,
                "Em campo aberto a diagonal custa menos que dois retos: dois passos, não quatro.");
        }

        // ── Os casos de jogo ──────────────────────────────────────────────────

        /// <summary>
        /// O alvo encostado numa parede cai numa célula bloqueada por arredondamento. Desistir
        /// aí faria o inimigo parar de perseguir alguém que está a um passo — que é
        /// indistinguível de IA quebrada.
        /// </summary>
        [Test]
        public void DestinoBloqueado_UsaAVizinhaLivreMaisProxima()
        {
            var mapa = new MapaDesenhado(
                ".....",
                "..#..",
                ".....");

            var caminho = Busca.Encontrar(mapa, new Celula(0, 1), new Celula(2, 1)).ToList();

            Assert.IsNotEmpty(caminho,
                "O alvo está 'dentro' da parede por arredondamento; o perseguidor tem de chegar " +
                "ao lado dele, não desistir.");

            Assert.IsTrue(mapa.EhCaminhavel(caminho[^1]),
                "O último passo tem de ser numa célula onde dá para pisar.");
        }

        [Test]
        public void OrigemIgualAoDestino_NaoProduzPasso()
        {
            var mapa = new MapaDesenhado("...");

            Assert.IsEmpty(Busca.Encontrar(mapa, new Celula(1, 0), new Celula(1, 0)),
                "Já chegou: zero passos.");
        }

        [Test]
        public void ForaDoMapa_NaoEhCaminho()
        {
            var mapa = new MapaDesenhado("...");

            Assert.IsEmpty(Busca.Encontrar(mapa, new Celula(1, 0), new Celula(99, 99)),
                "Sair do mundo não é caminho.");
        }

        /// <summary>
        /// <b>Onze Cultistas perseguindo ao mesmo tempo.</b> Sem teto, um alvo inalcançável faz
        /// cada um varrer o mapa inteiro — e isso não é lentidão, é travamento. O teto troca
        /// "caminho ótimo sempre" por "o jogo não congela", que é a troca certa.
        /// </summary>
        [Test]
        public void OTetoDeNos_ImpedeVarreduraDoMapaInteiro()
        {
            // Mapa grande com o alvo emparedado no canto oposto.
            var linhas = new string[60];
            for (int i = 0; i < 60; i++) linhas[i] = new string('.', 60);
            linhas[30] = new string('#', 60);   // parede completa: não há caminho

            var mapa = new MapaDesenhado(linhas);

            var busca = new BuscaDeCaminho { TetoDeNos = 200 };
            var caminho = busca.Encontrar(mapa, new Celula(0, 0), new Celula(59, 59));

            Assert.IsEmpty(caminho, "Sem caminho, o resultado é vazio.");
        }

        /// <summary>
        /// A lista é reaproveitada entre chamadas para não alocar em hot path. Isso é uma
        /// armadilha para quem a guardar sem copiar — o teste existe para o contrato ficar
        /// escrito, não porque o comportamento seja desejável.
        /// </summary>
        [Test]
        public void ALista_EhReaproveitadaEntreChamadas()
        {
            var mapa = new MapaDesenhado(".....");
            var busca = new BuscaDeCaminho();

            var primeira = busca.Encontrar(mapa, new Celula(0, 0), new Celula(4, 0));
            int antes = primeira.Count;

            busca.Encontrar(mapa, new Celula(0, 0), new Celula(2, 0));

            Assert.AreNotEqual(antes, primeira.Count,
                "A mesma instância é devolvida: quem precisar guardar o caminho deve COPIAR. " +
                "Se isto falhar, alguém passou a alocar por chamada — e aí o teste é que muda.");
        }
    }
}
