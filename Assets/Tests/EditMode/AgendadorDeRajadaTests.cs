using NUnit.Framework;
using FavelaAmarela.Core.Environment;

namespace FavelaAmarela.Tests.EditMode
{
    public class AgendadorDeRajadaTests
    {
        [Test]
        public void Tick_AntesDoIntervalo_NaoEstaEmRajada()
        {
            var agendador = new AgendadorDeRajada(intervaloMinimo: 5f, intervaloMaximo: 5f, duracaoRajada: 3f, amostraAleatoria: () => 0.0);

            agendador.Tick(4.9f);

            Assert.IsFalse(agendador.EstaEmRajada);
        }

        [Test]
        public void Tick_AoAtingirOIntervalo_EntraEmRajada()
        {
            var agendador = new AgendadorDeRajada(intervaloMinimo: 5f, intervaloMaximo: 5f, duracaoRajada: 3f, amostraAleatoria: () => 0.0);

            agendador.Tick(5f);

            Assert.IsTrue(agendador.EstaEmRajada);
        }

        [Test]
        public void Tick_AposDuracaoDaRajada_VoltaACalmaria()
        {
            var agendador = new AgendadorDeRajada(intervaloMinimo: 5f, intervaloMaximo: 5f, duracaoRajada: 3f, amostraAleatoria: () => 0.0);

            agendador.Tick(5f);
            Assert.IsTrue(agendador.EstaEmRajada);

            agendador.Tick(3f);

            Assert.IsFalse(agendador.EstaEmRajada);
        }

        [Test]
        public void Tick_AposCalmariaSemGatilho_PermaneceCalmo()
        {
            var agendador = new AgendadorDeRajada(intervaloMinimo: 10f, intervaloMaximo: 10f, duracaoRajada: 3f, amostraAleatoria: () => 0.0);

            for (int i = 0; i < 20; i++)
            {
                agendador.Tick(0.1f);
            }

            Assert.IsFalse(agendador.EstaEmRajada);
        }

        [Test]
        public void Tick_SorteiaNovoIntervaloAposRajada_ESeguePeriodico()
        {
            var agendador = new AgendadorDeRajada(intervaloMinimo: 5f, intervaloMaximo: 5f, duracaoRajada: 3f, amostraAleatoria: () => 0.0);

            // Primeira rajada
            agendador.Tick(5f);
            Assert.IsTrue(agendador.EstaEmRajada);
            agendador.Tick(3f);
            Assert.IsFalse(agendador.EstaEmRajada);

            // Segunda rajada, mesmo intervalo (amostra fixa)
            agendador.Tick(4.9f);
            Assert.IsFalse(agendador.EstaEmRajada);
            agendador.Tick(0.1f);
            Assert.IsTrue(agendador.EstaEmRajada);
        }

        [Test]
        public void Construtor_AmostraNoMaximo_SorteiaIntervaloMaximo()
        {
            var agendador = new AgendadorDeRajada(intervaloMinimo: 5f, intervaloMaximo: 12f, duracaoRajada: 3f, amostraAleatoria: () => 0.999999);

            agendador.Tick(6.9f);
            Assert.IsFalse(agendador.EstaEmRajada, "6.9s ainda não deveria alcançar um intervalo próximo de 12s");

            agendador.Tick(5.2f);
            Assert.IsTrue(agendador.EstaEmRajada);
        }
    }
}
