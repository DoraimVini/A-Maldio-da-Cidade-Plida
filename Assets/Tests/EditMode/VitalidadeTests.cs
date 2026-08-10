using System;
using NUnit.Framework;
using FavelaAmarela.Core.Combat;

namespace FavelaAmarela.Tests.EditMode
{
    public class VitalidadeTests
    {
        [Test]
        public void Construtor_ComecaCheioENaoAbatido()
        {
            var vit = new Vitalidade(20f);
            Assert.AreEqual(20f, vit.Atual);
            Assert.AreEqual(1f, vit.Percentual);
            Assert.IsFalse(vit.EstaAbatido);
        }

        [Test]
        public void Construtor_MaxNaoPositivo_Lanca()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Vitalidade(0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Vitalidade(-5f));
        }

        [Test]
        public void Ferir_ReduzAtual()
        {
            var vit = new Vitalidade(20f);
            vit.Ferir(5f);
            Assert.AreEqual(15f, vit.Atual);
            Assert.IsFalse(vit.EstaAbatido);
        }

        [Test]
        public void Ferir_AbaixoDeZero_ClampaEAbate()
        {
            var vit = new Vitalidade(20f);
            vit.Ferir(999f);
            Assert.AreEqual(0f, vit.Atual);
            Assert.IsTrue(vit.EstaAbatido);
        }

        [Test]
        public void Ferir_NegativoNaLetal_DisparaAcabouDeAbaterUmaVezSo()
        {
            var vit = new Vitalidade(10f);
            int abateuCount = 0;
            vit.OnChanged += args => { if (args.AcabouDeAbater) abateuCount++; };

            vit.Ferir(6f);   // 10 -> 4, não abate
            vit.Ferir(6f);   // 4 -> 0, abate (1ª vez)
            vit.Ferir(6f);   // já em 0, nenhum evento

            Assert.AreEqual(1, abateuCount);
        }

        [Test]
        public void Ferir_ComAtualEmZero_NaoDisparaEvento()
        {
            var vit = new Vitalidade(10f);
            vit.Ferir(10f); // zera
            int chamadas = 0;
            vit.OnChanged += _ => chamadas++;

            vit.Ferir(5f); // já abatido, delta nulo após clamp
            Assert.AreEqual(0, chamadas);
        }

        [Test]
        public void Ferir_Negativo_Lanca()
        {
            var vit = new Vitalidade(10f);
            Assert.Throws<ArgumentOutOfRangeException>(() => vit.Ferir(-1f));
        }

        [Test]
        public void Curar_AumentaAtualMasNaoPassaDoMax()
        {
            var vit = new Vitalidade(20f);
            vit.Ferir(15f); // 5
            vit.Curar(3f);  // 8
            Assert.AreEqual(8f, vit.Atual);

            vit.Curar(999f); // clampa no teto
            Assert.AreEqual(20f, vit.Atual);
        }

        [Test]
        public void Restaurar_ClampaESincroniza()
        {
            var vit = new Vitalidade(20f);
            vit.Restaurar(7f);
            Assert.AreEqual(7f, vit.Atual);

            vit.Restaurar(-3f);
            Assert.AreEqual(0f, vit.Atual);
            Assert.IsTrue(vit.EstaAbatido);
        }

        [Test]
        public void OnChanged_ReportaDeltaEPercentual()
        {
            var vit = new Vitalidade(100f);
            VitalidadeChangedArgs capturado = default;
            vit.OnChanged += args => capturado = args;

            vit.Ferir(25f);

            Assert.AreEqual(-25f, capturado.Delta);
            Assert.AreEqual(75f, capturado.ValorAtual);
            Assert.AreEqual(0.75f, capturado.Percentual);
            Assert.IsFalse(capturado.AcabouDeAbater);
        }
    }
}
