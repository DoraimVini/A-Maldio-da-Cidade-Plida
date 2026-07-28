using System.Collections.Generic;
using NUnit.Framework;
using FavelaAmarela.Core.GameLoop;

namespace FavelaAmarela.Tests.EditMode
{
    public class FrasesDeColapsoTests
    {
        [Test]
        public void Sortear_AmostraZero_RetornaAlgumaFrase()
        {
            var frases = new FrasesDeColapso(() => 0.0);

            var frase = frases.Sortear();

            Assert.IsFalse(string.IsNullOrWhiteSpace(frase));
        }

        [Test]
        public void Sortear_AmostraQuaseUm_NaoEstoura()
        {
            var frases = new FrasesDeColapso(() => 0.999999);

            var frase = frases.Sortear();

            Assert.IsFalse(string.IsNullOrWhiteSpace(frase));
        }

        [Test]
        public void Sortear_VariandoAmostra_CobreFrasesDistintas()
        {
            var frases = new FrasesDeColapso();
            var vistas = new HashSet<string>();

            // Varre o intervalo [0,1) forçando cada índice do pool.
            for (int i = 0; i < frases.Quantidade; i++)
            {
                double amostra = (i + 0.5) / frases.Quantidade;
                var f = new FrasesDeColapso(() => amostra);
                vistas.Add(f.Sortear());
            }

            Assert.AreEqual(frases.Quantidade, vistas.Count,
                "Cada índice do pool deveria produzir uma frase distinta.");
        }
    }
}
