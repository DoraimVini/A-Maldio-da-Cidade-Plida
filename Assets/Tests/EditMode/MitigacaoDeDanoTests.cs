using NUnit.Framework;
using FavelaAmarela.Core.Combat;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Suite EditMode da fórmula subtrativa com piso (<see cref="MitigacaoDeDano"/>).
    /// Verifica a conta acordada: golpe do Cultista (24) contra a defesa base do
    /// Damião (4) resulta em 20 de dano líquido (5 golpes numa Vitalidade 100).
    /// </summary>
    public class MitigacaoDeDanoTests
    {
        [Test]
        public void GolpeCultista_ContraDefesaBase_Da20()
        {
            // 24 bruto − 4 defesa = 20; piso (24×0,15 = 3,6) não domina.
            Assert.AreEqual(20f, MitigacaoDeDano.Aplicar(24f, 4f), 0.0001f);
        }

        [Test]
        public void SemDefesa_DanoIntegral()
        {
            Assert.AreEqual(24f, MitigacaoDeDano.Aplicar(24f, 0f), 0.0001f);
        }

        [Test]
        public void DefesaAltissima_PisoGaranteMinimo()
        {
            // 24 − 100 seria negativo; o piso de 15% garante 3,6.
            Assert.AreEqual(3.6f, MitigacaoDeDano.Aplicar(24f, 100f), 0.0001f);
        }

        [Test]
        public void PisoDominaQuandoDefesaQuaseIgualAoGolpe()
        {
            // 10 − 9,5 = 0,5, mas o piso (10×0,15 = 1,5) é maior.
            Assert.AreEqual(1.5f, MitigacaoDeDano.Aplicar(10f, 9.5f), 0.0001f);
        }

        [Test]
        public void DanoBrutoNaoPositivo_Zero()
        {
            Assert.AreEqual(0f, MitigacaoDeDano.Aplicar(0f, 5f), 0.0001f);
            Assert.AreEqual(0f, MitigacaoDeDano.Aplicar(-10f, 5f), 0.0001f);
        }

        [Test]
        public void DefesaNegativa_TratadaComoZero()
        {
            Assert.AreEqual(24f, MitigacaoDeDano.Aplicar(24f, -5f), 0.0001f);
        }

        [Test]
        public void NuncaExcedeODanoBruto()
        {
            // pisoFracao acima de 1 é clampado; final não passa do bruto.
            Assert.AreEqual(24f, MitigacaoDeDano.Aplicar(24f, 0f, pisoFracao: 2f), 0.0001f);
        }

        [Test]
        public void PisoZero_PermiteDefesaAnularOGolpe()
        {
            // Com piso 0, defesa >= golpe zera o dano.
            Assert.AreEqual(0f, MitigacaoDeDano.Aplicar(24f, 30f, pisoFracao: 0f), 0.0001f);
        }
    }
}
