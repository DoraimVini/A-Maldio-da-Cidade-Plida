using FavelaAmarela.Core.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Geometria do abrigo que cada relíquia ergue no rito do Rei em Amarelo — a resposta da
    /// luta a partir de 2026-09-10, no lugar de dar as costas.
    ///
    /// <para><b>O que estes testes protegem de verdade:</b> que a zona testada seja a zona
    /// desenhada. <i>"Eu estava dentro e morri"</i> é o pior desfecho possível numa mecânica de
    /// abrigo — pior que um abrigo generoso demais, porque destrói a confiança do jogador no
    /// que a tela mostra.</para>
    /// </summary>
    public sealed class AbrigoDeReliquiaTests
    {
        private static readonly Vector2 Altar = new Vector2(-10f, 61f);

        [Test]
        public void EmCimaDoAltar_EstaAbrigado()
        {
            Assert.IsTrue(AbrigoDeReliquia.EstaAbrigado(Altar, Altar));
        }

        /// <summary>
        /// O abrigo é uma <b>elipse</b>, não um círculo: a vista do jogo é 20 × 11,25 unidades,
        /// e espaço de jogo redondo num quadro largo lê torto. É a mesma lição da arena da
        /// Byakhee e da zona morta da câmera.
        /// </summary>
        [Test]
        public void OAbrigoEhElitico_LargoENaoRedondo()
        {
            // 1,5 un a leste: dentro (a meia-largura é 2).
            Assert.IsTrue(AbrigoDeReliquia.EstaAbrigado(Altar + new Vector2(1.5f, 0f), Altar),
                "1,5 un na horizontal tem de estar dentro — a meia-largura é 2.");

            // 1,5 un ao norte: FORA (a meia-altura é 1).
            Assert.IsFalse(AbrigoDeReliquia.EstaAbrigado(Altar + new Vector2(0f, 1.5f), Altar),
                "1,5 un na vertical tem de estar fora — a meia-altura é 1. Se este passar, o " +
                "abrigo virou círculo e não bate mais com a cúpula desenhada.");
        }

        [Test]
        public void NaBorda_AindaAbriga()
        {
            Assert.IsTrue(AbrigoDeReliquia.EstaAbrigado(Altar + new Vector2(2f, 0f), Altar),
                "A borda conta como dentro: quem parou em cima da linha vê a cúpula à volta.");
        }

        [Test]
        public void ForaDaElipse_NaoAbriga()
        {
            Assert.IsFalse(AbrigoDeReliquia.EstaAbrigado(Altar + new Vector2(2.1f, 0f), Altar));
            Assert.IsFalse(AbrigoDeReliquia.EstaAbrigado(Altar + new Vector2(0f, -1.1f), Altar));
        }

        /// <summary>
        /// A diagonal é o caso que um teste de eixos não pega: dentro nos dois eixos
        /// separadamente pode estar fora da elipse.
        /// </summary>
        [Test]
        public void DentroNosDoisEixos_PodeEstarForaDaElipse()
        {
            var p = Altar + new Vector2(1.8f, 0.9f); // x < 2 e y < 1, mas (0,9)² + (0,9)² > 1
            Assert.IsFalse(AbrigoDeReliquia.EstaAbrigado(p, Altar),
                "(1,8 ; 0,9) está dentro de cada eixo e fora da elipse — é por isso que a " +
                "conta é (x/a)² + (y/b)² e não duas comparações.");
        }

        /// <summary>
        /// Semi-eixo zerado no Inspector não pode abrigar o mundo inteiro. Falhar visível é
        /// melhor que abrigar em silêncio.
        /// </summary>
        [Test]
        public void SemiEixoZerado_NaoAbrigaNinguem()
        {
            Assert.IsFalse(AbrigoDeReliquia.EstaAbrigado(Altar, Altar, 0f, 1f));
            Assert.IsFalse(AbrigoDeReliquia.EstaAbrigado(Altar, Altar, 2f, 0f));
        }

        // ── A travessia ──────────────────────────────────────────────────────

        [Test]
        public void JaDentro_LevaZeroSegundos()
        {
            Assert.AreEqual(0f,
                AbrigoDeReliquia.SegundosParaAlcancar(Altar, Altar, 4.5f), 0.0001f);
        }

        /// <summary>
        /// A conta mede até a <b>borda</b>, não até o centro: entrar basta, parar em cima do
        /// altar não é exigido.
        /// </summary>
        [Test]
        public void MedeAteABorda_NaoAteOCentro()
        {
            var origem = Altar + new Vector2(10f, 0f);

            float t = AbrigoDeReliquia.SegundosParaAlcancar(origem, Altar, 5f);

            Assert.AreEqual((10f - 2f) / 5f, t, 0.001f,
                "10 un de distância menos os 2 de meia-largura, a 5 u/s.");
        }

        /// <summary>
        /// Aproximando pelo eixo curto, a borda está mais perto do centro — a conta tem de usar
        /// o raio <b>na direção da aproximação</b>, não o semi-eixo maior.
        /// </summary>
        [Test]
        public void PeloEixoCurto_ABordaEstaMaisPerto()
        {
            var pelaLargura = Altar + new Vector2(10f, 0f);
            var pelaAltura = Altar + new Vector2(0f, 10f);

            float largura = AbrigoDeReliquia.SegundosParaAlcancar(pelaLargura, Altar, 5f);
            float altura = AbrigoDeReliquia.SegundosParaAlcancar(pelaAltura, Altar, 5f);

            Assert.Greater(altura, largura,
                "Pela vertical o abrigo é metade da largura, então a travessia demora mais.");
            Assert.AreEqual((10f - 1f) / 5f, altura, 0.001f);
        }

        [Test]
        public void VelocidadeZero_NuncaChega()
        {
            Assert.AreEqual(float.PositiveInfinity,
                AbrigoDeReliquia.SegundosParaAlcancar(Altar + Vector2.right * 5f, Altar, 0f));
        }
    }
}
