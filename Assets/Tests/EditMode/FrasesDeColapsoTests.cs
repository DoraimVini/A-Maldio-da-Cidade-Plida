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
        public void Sortear_SemArgumento_UsaPoolMental()
        {
            var frases = new FrasesDeColapso(() => 0.0);
            Assert.AreEqual(frases.Sortear(TipoDeDerrota.Mental), frases.Sortear(),
                "Sortear() sem argumento deve continuar sendo o Colapso Mental (compatibilidade).");
        }

        [Test]
        public void Sortear_Corporea_UsaPoolDiferenteDoMental()
        {
            var frases = new FrasesDeColapso(() => 0.0);

            var mental = frases.Sortear(TipoDeDerrota.Mental);
            var corporea = frases.Sortear(TipoDeDerrota.Corporea);

            Assert.AreNotEqual(mental, corporea,
                "Morte corpórea não deve reusar a frase de Colapso Mental.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(corporea));
        }

        [Test]
        public void Sortear_Corporea_CobreTodoOPoolSemEstourar()
        {
            var referencia = new FrasesDeColapso();
            int total = referencia.QuantidadePara(TipoDeDerrota.Corporea);
            Assert.Greater(total, 0, "O pool de morte corpórea não pode estar vazio.");

            var vistas = new HashSet<string>();
            for (int i = 0; i < total; i++)
            {
                double amostra = (i + 0.5) / total;
                var f = new FrasesDeColapso(() => amostra);
                vistas.Add(f.Sortear(TipoDeDerrota.Corporea));
            }

            Assert.AreEqual(total, vistas.Count,
                "Cada índice do pool corpóreo deveria produzir uma frase distinta.");
        }

        [Test]
        public void Sortear_Corporea_AmostraQuaseUm_NaoEstoura()
        {
            var frases = new FrasesDeColapso(() => 0.999999);
            Assert.IsFalse(string.IsNullOrWhiteSpace(frases.Sortear(TipoDeDerrota.Corporea)));
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
