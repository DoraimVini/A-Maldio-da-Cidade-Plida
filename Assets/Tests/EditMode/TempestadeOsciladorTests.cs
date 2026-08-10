using NUnit.Framework;
using FavelaAmarela.Core.Environment;

namespace FavelaAmarela.Tests.EditMode
{
    public class TempestadeOsciladorTests
    {
        [Test]
        public void Tick_NoInicio_RetornaOPontoMedioDaFaixa()
        {
            var oscilador = new TempestadeOscilador(minimo: 0.2f, maximo: 0.6f, velocidadeCiclo: 1f);

            float valor = oscilador.Tick(0f);

            Assert.AreEqual(0.4f, valor, 0.0001f);
        }

        [Test]
        public void Tick_NuncaSaiDaFaixaMinMax()
        {
            var oscilador = new TempestadeOscilador(minimo: 0.2f, maximo: 0.6f, velocidadeCiclo: 2.5f);

            for (int i = 0; i < 200; i++)
            {
                float valor = oscilador.Tick(0.1f);
                Assert.GreaterOrEqual(valor, 0.2f - 0.0001f);
                Assert.LessOrEqual(valor, 0.6f + 0.0001f);
            }
        }

        [Test]
        public void DefinirFaixa_AceitaMinMaxInvertidos()
        {
            var oscilador = new TempestadeOscilador(minimo: 0.8f, maximo: 0.1f, velocidadeCiclo: 1f);

            float valor = oscilador.Tick(0f);

            // Ponto médio da faixa correta (0.1 a 0.8), não da ordem passada
            Assert.AreEqual(0.45f, valor, 0.0001f);
        }

        [Test]
        public void DefinirFaixa_ClampeiaForaDeZeroUm()
        {
            var oscilador = new TempestadeOscilador(minimo: -0.5f, maximo: 1.5f, velocidadeCiclo: 1f);

            float valor = oscilador.Tick(0f);

            Assert.AreEqual(0.5f, valor, 0.0001f);
        }

        [Test]
        public void DefinirFaixa_MudaAFaixaUsadaEmTicksSeguintes()
        {
            var oscilador = new TempestadeOscilador(minimo: 0.2f, maximo: 0.6f, velocidadeCiclo: 1f);
            oscilador.Tick(0f);

            oscilador.DefinirFaixa(0.5f, 0.9f);
            float valor = oscilador.Tick(0f);

            Assert.GreaterOrEqual(valor, 0.5f - 0.0001f);
            Assert.LessOrEqual(valor, 0.9f + 0.0001f);
        }
    }
}
