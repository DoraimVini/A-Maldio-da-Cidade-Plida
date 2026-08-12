using NUnit.Framework;
using UnityEngine;
using FavelaAmarela.Core.Enemies;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Trava a geometria da mecânica da Máscara Pálida — "de costas" precisa de definição
    /// exata, porque o erro custa a luta inteira (Colapso instantâneo).
    /// </summary>
    public class DetectorDeCostasTests
    {
        // Rei parado na origem, em todos os casos.
        private static readonly Vector2 PosicaoDoRei = Vector2.zero;

        [Test]
        public void OlharDiretamenteParaLonge_EhDeCostas()
        {
            var jogador = new Vector2(5f, 0f); // a leste do Rei
            var olhar = Vector2.right;         // olhando ainda mais para leste — para longe

            Assert.IsTrue(DetectorDeCostas.EstaDeCostas(jogador, olhar, PosicaoDoRei));
        }

        [Test]
        public void OlharDiretamenteParaORei_NaoEhDeCostas()
        {
            var jogador = new Vector2(5f, 0f);
            var olhar = Vector2.left; // olhando na direção do Rei

            Assert.IsFalse(DetectorDeCostas.EstaDeCostas(jogador, olhar, PosicaoDoRei));
        }

        [Test]
        public void PerfilDeLado_NaoContaComoDeCostas()
        {
            // A 90° do Rei: nem de frente, nem de costas — não pode salvar por acidente.
            var jogador = new Vector2(5f, 0f);
            var olhar = Vector2.up;

            Assert.IsFalse(DetectorDeCostas.EstaDeCostas(jogador, olhar, PosicaoDoRei));
        }

        [Test]
        public void QuaseDeCostas_DentroDoLimiarPadrao_Salva()
        {
            // ~120° de desvio do "para longe" perfeito ainda deveria contar — folga real de
            // input, não exigir precisão de laboratório.
            var jogador = new Vector2(5f, 0f);
            var olhar = new Vector2(Mathf.Cos(Mathf.Deg2Rad * 30f), Mathf.Sin(Mathf.Deg2Rad * 30f));

            Assert.IsTrue(DetectorDeCostas.EstaDeCostas(jogador, olhar, PosicaoDoRei));
        }

        [Test]
        public void LimiarMaisRigoroso_RejeitaOMesmoAngulo()
        {
            var jogador = new Vector2(5f, 0f);
            var olhar = new Vector2(Mathf.Cos(Mathf.Deg2Rad * 30f), Mathf.Sin(Mathf.Deg2Rad * 30f));

            Assert.IsFalse(DetectorDeCostas.EstaDeCostas(jogador, olhar, PosicaoDoRei, limiar: -0.95f));
        }

        [Test]
        public void SemDirecaoDeOlhar_NuncaSalva()
        {
            // Vector2.zero como "olhar" não pode ser interpretado como "de costas" por acaso.
            var jogador = new Vector2(5f, 0f);

            Assert.IsFalse(DetectorDeCostas.EstaDeCostas(jogador, Vector2.zero, PosicaoDoRei));
        }

        [Test]
        public void EmCimaDoRei_NuncaEhDeCostas()
        {
            // Sem vetor "para o alvo" definido, não há geometria — não arrisca dar como salvo.
            Assert.IsFalse(DetectorDeCostas.EstaDeCostas(PosicaoDoRei, Vector2.right, PosicaoDoRei));
        }
    }
}
