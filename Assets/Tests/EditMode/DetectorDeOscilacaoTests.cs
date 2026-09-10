using FavelaAmarela.Runtime.Diagnostico;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// A contagem do <see cref="DetectorDeOscilacao"/> — a parte pura, sem cena. Um diagnóstico
    /// que conta errado é pior que nenhum: ele aponta para a família de causa errada.
    /// </summary>
    public sealed class DetectorDeOscilacaoTests
    {
        private static bool[] Bools(params int[] v)
        {
            var b = new bool[v.Length];
            for (int i = 0; i < v.Length; i++) b[i] = v[i] != 0;
            return b;
        }

        [Test]
        public void FlipPiscando_ContaCadaTroca_EDenunciaFacingDisputado()
        {
            var flip = Bools(0, 1, 0, 1, 0, 1);
            var zeros = new float[6];

            var r = DetectorDeOscilacao.Analisar(zeros, flip, zeros, zeros, zeros, 6);

            Assert.AreEqual(5, r.TrocasFlipX);
            Assert.IsTrue(r.FacingDisputado(2));
        }

        [Test]
        public void EscalaAlternandoSinal_Conta_EZerosNaoContam()
        {
            // 1, -1, 0, -1, 1: o zero no meio não é troca; -1 → -1 também não.
            var escala = new[] { 1f, -1f, 0f, -1f, 1f };
            var zeros = new float[5];

            var r = DetectorDeOscilacao.Analisar(escala, new bool[5], zeros, zeros, zeros, 5);

            Assert.AreEqual(2, r.TrocasSinalEscalaX);
            Assert.AreEqual(-1f, r.EscalaXMin);
            Assert.AreEqual(1f, r.EscalaXMax);
        }

        [Test]
        public void JitterDePosicao_ContaInversoesDeDirecao_EAmplitude()
        {
            // Vai e volta 0,05 a cada quadro em torno de 10: position.x nunca troca de sinal,
            // mas a direção inverte a cada passo — é o padrão do jitter de física.
            var pos = new[] { 10f, 10.05f, 10f, 10.05f, 10f, 10.05f };
            var zeros = new float[6];

            var r = DetectorDeOscilacao.Analisar(zeros, new bool[6], zeros, zeros, pos, 6);

            Assert.AreEqual(0, r.TrocasSinalPosX, "position.x é sempre positiva; não troca de sinal.");
            Assert.AreEqual(4, r.InversoesDeDirecaoX);
            Assert.AreEqual(0.05f, r.AmplitudePosX, 1e-4f);
            Assert.AreEqual(0.05f, r.MaiorPassoNumaInversao, 1e-4f);
        }

        [Test]
        public void MovimentoReto_NaoInverte_EFacingEstavel()
        {
            var pos = new[] { 0f, 1f, 2f, 3f, 4f };
            var vel = new[] { 5f, 5f, 5f, 5f, 5f };
            var escala = new[] { 2.4f, 2.4f, 2.4f, 2.4f, 2.4f };

            var r = DetectorDeOscilacao.Analisar(escala, new bool[5], vel, new float[5], pos, 5);

            Assert.AreEqual(0, r.InversoesDeDirecaoX);
            Assert.AreEqual(0, r.TrocasSinalVelX);
            Assert.AreEqual(0, r.TrocasFlipX);
            Assert.AreEqual(4f, r.AmplitudePosX);
            Assert.IsFalse(r.FacingDisputado(2));
        }

        [Test]
        public void PiscadaQueAcompanhaVirada_NaoEhOrfa_EPiscadaSemVirada_Eh()
        {
            // Quadros 0-1: velocidade +5 → -5 e o flip acompanha (virada real).
            // Quadros 3-4: o flip pisca com a velocidade constante (segunda fonte).
            var flip = Bools(0, 1, 1, 1, 0, 0);
            var vel = new[] { 5f, -5f, -5f, -5f, -5f, -5f };
            var zeros = new float[6];

            var r = DetectorDeOscilacao.Analisar(zeros, flip, vel, zeros, zeros, 6);

            Assert.AreEqual(2, r.TrocasFlipX);
            Assert.AreEqual(1, r.PiscadasOrfas, "Só a piscada sem virada de velocidade é órfã.");
        }

        [Test]
        public void SemAmostras_NaoEstoura()
        {
            var r = DetectorDeOscilacao.Analisar(new float[0], new bool[0], new float[0], new float[0], new float[0], 0);

            Assert.AreEqual(0, r.TrocasFlipX);
            Assert.AreEqual(0f, r.AmplitudePosX);
        }
    }
}
