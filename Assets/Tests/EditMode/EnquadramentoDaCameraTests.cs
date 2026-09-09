using NUnit.Framework;
using UnityEngine;
using FavelaAmarela.Core.Camera;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// A aritmética do enquadramento: travar a câmera nos limites do mapa e o tremor por trauma.
    ///
    /// <para>São contas puras, e por isso vivem em <c>Core</c> e são testadas aqui — a versão
    /// anterior do tremor morava dentro de um <c>LateUpdate</c>, onde nenhum teste alcança, e
    /// carregava um defeito que sobreviveu meses justamente por isso.</para>
    /// </summary>
    public sealed class EnquadramentoDaCameraTests
    {
        // ── travar nos limites ───────────────────────────────────────────────

        /// <summary>
        /// O caso que motivou: no Deserto de Hali a câmera enxerga <b>7,50 unidades</b> para
        /// cada lado (orthographicSize 4,21875 a 16:9), e o jogador alcança as paredes de
        /// limite. Sem travar, ela revelava o vazio além da borda.
        /// </summary>
        [Test]
        public void NaBordaDoMapa_ACameraParaAntesDeMostrarOVazio()
        {
            var minimo = new Vector2(-44f, -32f);
            var maximo = new Vector2(44f, 32f);
            const float meiaLargura = 7.5f, meiaAltura = 4.21875f;

            var presa = EnquadramentoDaCamera.Prender(
                new Vector2(44f, 32f), minimo, maximo, meiaLargura, meiaAltura);

            Assert.AreEqual(44f - meiaLargura, presa.x, 0.001f,
                "A câmera passou da borda LESTE. Ela enxerga 7,5 unidades para o lado, então " +
                "o centro dela não pode chegar a menos disso do limite — senão mostra o que " +
                "existe além da parede, que é nada.");

            Assert.AreEqual(32f - meiaAltura, presa.y, 0.001f,
                "A câmera passou da borda NORTE.");
        }

        [Test]
        public void NoMeioDoMapa_ACameraNaoEMexida()
        {
            var desejada = new Vector2(3f, -7f);

            var presa = EnquadramentoDaCamera.Prender(
                desejada, new Vector2(-44f, -32f), new Vector2(44f, 32f), 7.5f, 4.21875f);

            Assert.AreEqual(desejada, presa,
                "Longe das bordas o travamento não deve mexer em nada — se mexe, ele está " +
                "brigando com o amortecedor no meio do mapa.");
        }

        /// <summary>
        /// Sala menor que a vista: centraliza em vez de travar.
        ///
        /// <para>Travar com <c>mínimo &gt; máximo</c> daria um <c>Clamp</c> invertido e a câmera
        /// saltaria entre as duas bordas a cada quadro. Mostrar um pouco de vazio é ruim;
        /// piscar é pior.</para>
        /// </summary>
        [Test]
        public void SalaMenorQueAVista_Centraliza()
        {
            var presa = EnquadramentoDaCamera.Prender(
                new Vector2(100f, -100f), new Vector2(-4f, -3f), new Vector2(4f, 3f),
                7.5f, 4.21875f);

            Assert.AreEqual(0f, presa.x, 0.001f, "Deveria centralizar em X.");
            Assert.AreEqual(0f, presa.y, 0.001f, "Deveria centralizar em Y.");
        }

        // ── tremor por trauma ────────────────────────────────────────────────

        /// <summary>
        /// Dois golpes seguidos <b>somam</b> trauma.
        ///
        /// <para>A versão anterior guardava duração e magnitude e <b>reiniciava</b> a cada
        /// chamada: o segundo golpe apagava o primeiro em vez de somar, e uma sequência de
        /// acertos sacudia menos que um acerto só.</para>
        /// </summary>
        [Test]
        public void DoisGolpes_SomamTrauma()
        {
            float t = EnquadramentoDaCamera.Acumular(0f, 0.3f);
            t = EnquadramentoDaCamera.Acumular(t, 0.3f);

            Assert.AreEqual(0.6f, t, 0.0001f,
                "Dois golpes deveriam somar. Se o segundo substitui o primeiro, uma sequência " +
                "de acertos sacode menos que um acerto sozinho.");
        }

        [Test]
        public void OTrauma_NaoPassaDoTeto()
        {
            float t = EnquadramentoDaCamera.Acumular(0.9f, 5f);

            Assert.AreEqual(EnquadramentoDaCamera.TraumaMaximo, t, 0.0001f,
                "Sem teto, um crítico arrancaria a tela do lugar.");
        }

        [Test]
        public void OTrauma_DecaiAteZeroENaoAbaixo()
        {
            float t = EnquadramentoDaCamera.Decair(0.5f, 1f, 1.6f);

            Assert.AreEqual(0f, t,
                "O trauma passou de zero e ficou negativo — o tremor voltaria ao contrário.");
        }

        /// <summary>
        /// O trauma entra <b>ao quadrado</b>, e é o que separa tremor legível de chiado
        /// constante.
        /// </summary>
        [Test]
        public void OTremor_CresceAoQuadradoDoTrauma()
        {
            var ruido = new Vector2(1f, 0f);

            float fraco = EnquadramentoDaCamera.Deslocamento(0.2f, 1f, ruido).x;
            float cheio = EnquadramentoDaCamera.Deslocamento(1.0f, 1f, ruido).x;

            Assert.AreEqual(0.04f, fraco, 0.0001f,
                "Trauma 0,2 deveria deslocar 4% do máximo (0,2²), não 20%. Linear, a tela " +
                "treme o tempo todo numa luta e o tremor deixa de dizer quanto doeu.");

            Assert.AreEqual(1f, cheio, 0.0001f, "Trauma cheio deveria dar o deslocamento cheio.");
        }

        [Test]
        public void SemTrauma_NaoHaDeslocamento()
        {
            Assert.AreEqual(Vector2.zero,
                EnquadramentoDaCamera.Deslocamento(0f, 1f, new Vector2(1f, 1f)));
        }

        /// <summary>
        /// O tremor escala com o zoom: as cenas rodam em <c>orthographicSize</c> 4,21875 e
        /// 5,625, 33% de diferença. Sem o fator, o tremor ocuparia menos tela na cena mais
        /// afastada.
        /// </summary>
        [Test]
        public void OTremor_EscalaComOZoom()
        {
            var ruido = new Vector2(1f, 0f);
            float fator = 5.625f / 4.21875f;

            float perto = EnquadramentoDaCamera.Deslocamento(1f, 0.35f, ruido).x;
            float longe = EnquadramentoDaCamera.Deslocamento(1f, 0.35f, ruido, fator).x;

            Assert.Greater(longe, perto,
                "Na cena mais afastada o tremor precisa de MAIS unidades de mundo para ocupar " +
                "a mesma fração da tela.");

            Assert.AreEqual(perto * fator, longe, 0.0001f);
        }

        // ── zona morta ───────────────────────────────────────────────────────

        [Test]
        public void DentroDaZonaMorta_ACameraNaoSeMexe()
        {
            var camera = new Vector2(10f, 5f);
            var alvo = camera + new Vector2(0.4f, 0.3f);   // 0,5 de distância

            Assert.AreEqual(camera,
                EnquadramentoDaCamera.AlvoComZonaMorta(alvo, camera, 0.75f),
                "Passo curto dentro da zona morta arrastou a câmera. É o balanço de fundo " +
                "constante que a zona existe para evitar.");
        }

        /// <summary>
        /// Fora da zona, o alvo devolvido é o ponto <b>na borda</b> dela — não o jogador.
        /// Devolver o jogador faria a câmera centralizá-lo e reabrir a folga do outro lado, e a
        /// zona morta oscilaria em vez de segurar.
        /// </summary>
        [Test]
        public void ForaDaZonaMorta_OAlvoEABordaDaZona_NaoOJogador()
        {
            var camera = Vector2.zero;
            var alvo = new Vector2(3f, 0f);

            var efetivo = EnquadramentoDaCamera.AlvoComZonaMorta(alvo, camera, 0.75f);

            Assert.AreEqual(2.25f, efetivo.x, 0.0001f,
                "Deveria parar a 0,75 do jogador (3 − 0,75), e não em cima dele.");
            Assert.AreEqual(0f, efetivo.y, 0.0001f);
        }

        [Test]
        public void ZonaMortaZero_NaoMudaNada()
        {
            var alvo = new Vector2(3f, -2f);

            Assert.AreEqual(alvo,
                EnquadramentoDaCamera.AlvoComZonaMorta(alvo, Vector2.zero, 0f));
        }

        // ── antecipação ──────────────────────────────────────────────────────

        [Test]
        public void AlvoParado_NaoAntecipa()
        {
            Assert.AreEqual(Vector2.zero,
                EnquadramentoDaCamera.AntecipacaoDesejada(Vector2.zero, 1.8f, 7.5f),
                "Parado, a câmera não tem para onde olhar à frente — e antecipar aqui faria " +
                "a câmera derivar sozinha com o jogador imóvel.");
        }

        [Test]
        public void AAntecipacao_CresceComAVelocidadeEsatura()
        {
            var meia = EnquadramentoDaCamera.AntecipacaoDesejada(
                new Vector2(3.75f, 0f), 1.8f, 7.5f);
            var cheia = EnquadramentoDaCamera.AntecipacaoDesejada(
                new Vector2(7.5f, 0f), 1.8f, 7.5f);
            var acima = EnquadramentoDaCamera.AntecipacaoDesejada(
                new Vector2(30f, 0f), 1.8f, 7.5f);

            Assert.AreEqual(0.9f, meia.x, 0.0001f, "Meia velocidade, meia antecipação.");
            Assert.AreEqual(1.8f, cheia.x, 0.0001f, "Na velocidade de corrida, antecipação cheia.");
            Assert.AreEqual(1.8f, acima.x, 0.0001f,
                "Acima da referência ela precisa SATURAR. Sem teto, o arremesso da Tempestade " +
                "jogaria a câmera para fora do mapa.");
        }

        // ── ruído do tremor ──────────────────────────────────────────────────

        /// <summary>
        /// O ruído tem de ser <b>determinístico</b> e <b>contínuo</b>.
        ///
        /// <para>A primeira versão do tremor por trauma — minha, do mesmo dia — sorteava
        /// <c>Random.insideUnitCircle</c> a cada quadro. Sorteio independente por quadro não é
        /// tremor, é chiado: a câmera salta para um ponto sem relação com o anterior.</para>
        /// </summary>
        [Test]
        public void ORuido_EDeterministicoEContinuo()
        {
            Assert.AreEqual(EnquadramentoDaCamera.Ruido(1.5f, 22f),
                            EnquadramentoDaCamera.Ruido(1.5f, 22f),
                            "O mesmo tempo tem de dar o mesmo ruído.");

            var a = EnquadramentoDaCamera.Ruido(1.000f, 22f);
            var b = EnquadramentoDaCamera.Ruido(1.002f, 22f);

            Assert.Less(Vector2.Distance(a, b), 0.3f,
                "Dois instantes vizinhos deram valores distantes — o ruído não é contínuo, e " +
                "o tremor volta a parecer sorteio por quadro.");
        }

        [Test]
        public void ORuido_FicaNaFaixaEsperada()
        {
            for (float t = 0f; t < 5f; t += 0.037f)
            {
                var r = EnquadramentoDaCamera.Ruido(t, 22f);
                Assert.GreaterOrEqual(r.x, -1.001f); Assert.LessOrEqual(r.x, 1.001f);
                Assert.GreaterOrEqual(r.y, -1.001f); Assert.LessOrEqual(r.y, 1.001f);
            }
        }

        /// <summary>
        /// X e Y precisam ser <b>independentes</b>. Amostrar o Perlin no mesmo ponto para os
        /// dois eixos faria os dois lerem o mesmo valor, e o tremor sairia sempre na diagonal.
        /// </summary>
        [Test]
        public void ORuido_NaoSaiSempreNaDiagonal()
        {
            int iguais = 0, total = 0;

            for (float t = 0f; t < 8f; t += 0.05f)
            {
                var r = EnquadramentoDaCamera.Ruido(t, 22f);
                total++;
                if (Mathf.Abs(r.x - r.y) < 0.1f) iguais++;
            }

            Assert.Less(iguais / (float)total, 0.35f,
                $"{iguais} de {total} amostras tiveram X e Y quase iguais. Os dois eixos estão " +
                "amarrados e o tremor sai sempre na mesma diagonal.");
        }

        /// <summary>
        /// O dano vira trauma proporcionalmente, e satura no teto por golpe — abaixo de 1, para
        /// dois golpes somarem em vez de o primeiro já estourar.
        /// </summary>
        [Test]
        public void ODano_ViraTraumaProporcional_ESatura()
        {
            const float referencia = 40f, teto = 0.45f;

            Assert.AreEqual(teto * 0.5f,
                EnquadramentoDaCamera.TraumaDeUmGolpe(20f, referencia, teto), 0.0001f,
                "Meio golpe deveria dar meio trauma.");

            Assert.AreEqual(teto,
                EnquadramentoDaCamera.TraumaDeUmGolpe(400f, referencia, teto), 0.0001f,
                "Dez vezes o golpe de referência deveria saturar no teto, não decuplicar.");

            Assert.AreEqual(0f,
                EnquadramentoDaCamera.TraumaDeUmGolpe(0f, referencia, teto), 0.0001f,
                "Golpe de dano zero — a mão vazia deste jogo — não pode sacudir a tela.");
        }
    }
}
