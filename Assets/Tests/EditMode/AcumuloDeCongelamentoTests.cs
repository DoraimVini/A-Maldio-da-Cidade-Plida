using NUnit.Framework;
using FavelaAmarela.Core.Combat;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Suite EditMode do acúmulo dos Cones de Gelo do Abdul: 3 acúmulos congelam Damião,
    /// e acúmulos expiram com o tempo (a mecânica é "não leve três seguidos", não uma
    /// punição inevitável).
    /// </summary>
    public class AcumuloDeCongelamentoTests
    {
        [Test]
        public void Inicia_SemAcumulosENaoCongelado()
        {
            var frio = new AcumuloDeCongelamento();
            Assert.AreEqual(0, frio.Acumulos);
            Assert.IsFalse(frio.EstaCongelado);
            Assert.AreEqual(3, frio.Limite);
        }

        [Test]
        public void DoisAcumulos_NaoCongelam()
        {
            var frio = new AcumuloDeCongelamento(limite: 3);
            frio.AplicarAcumulo();
            frio.AplicarAcumulo();

            Assert.AreEqual(2, frio.Acumulos);
            Assert.IsFalse(frio.EstaCongelado);
        }

        [Test]
        public void TerceiroAcumulo_Congela_EZeraOsAcumulos()
        {
            var frio = new AcumuloDeCongelamento(limite: 3, duracaoDoCongelamento: 1.5f);
            bool congelou = false;
            frio.OnCongelou += () => congelou = true;

            frio.AplicarAcumulo();
            frio.AplicarAcumulo();
            frio.AplicarAcumulo();

            Assert.IsTrue(congelou);
            Assert.IsTrue(frio.EstaCongelado);
            Assert.AreEqual(0, frio.Acumulos, "Ao congelar, o acúmulo zera.");
            Assert.AreEqual(1.5f, frio.TempoCongeladoRestante, 0.0001f);
        }

        [Test]
        public void Congelamento_Expira_EDisparaDescongelou()
        {
            var frio = new AcumuloDeCongelamento(limite: 1, duracaoDoCongelamento: 1f);
            bool descongelou = false;
            frio.OnDescongelou += () => descongelou = true;

            frio.AplicarAcumulo(); // congela imediatamente (limite 1)
            Assert.IsTrue(frio.EstaCongelado);

            frio.Tick(1f);

            Assert.IsFalse(frio.EstaCongelado);
            Assert.IsTrue(descongelou);
            Assert.AreEqual(0f, frio.TempoCongeladoRestante);
        }

        [Test]
        public void EnquantoCongelado_NovosAcumulosSaoIgnorados()
        {
            var frio = new AcumuloDeCongelamento(limite: 1, duracaoDoCongelamento: 2f);
            frio.AplicarAcumulo(); // congela

            frio.AplicarAcumulo(); // deve ser ignorado
            Assert.AreEqual(0, frio.Acumulos, "Congelado não acumula punição em cima.");
        }

        [Test]
        public void Acumulo_ExpiraComOTempo()
        {
            var frio = new AcumuloDeCongelamento(limite: 3, duracaoDoAcumulo: 5f);
            frio.AplicarAcumulo();
            frio.AplicarAcumulo();
            Assert.AreEqual(2, frio.Acumulos);

            frio.Tick(5f); // expira um
            Assert.AreEqual(1, frio.Acumulos);

            frio.Tick(5f); // expira o outro
            Assert.AreEqual(0, frio.Acumulos);
        }

        [Test]
        public void AcumulosNaoExpiram_AntesDoTempo()
        {
            var frio = new AcumuloDeCongelamento(limite: 3, duracaoDoAcumulo: 5f);
            frio.AplicarAcumulo();

            frio.Tick(4.9f);
            Assert.AreEqual(1, frio.Acumulos);
        }

        [Test]
        public void TresConesSeguidos_Congelam_MasEspacadosNao()
        {
            // O cenário de design: cones espaçados não devem congelar.
            var frio = new AcumuloDeCongelamento(limite: 3, duracaoDoAcumulo: 4f);

            frio.AplicarAcumulo();
            frio.Tick(4f); // expira
            frio.AplicarAcumulo();
            frio.Tick(4f); // expira
            frio.AplicarAcumulo();

            Assert.IsFalse(frio.EstaCongelado,
                "Cones espaçados não devem congelar — só três em sequência.");
        }

        [Test]
        public void Limpar_ZeraTudoEDescongela()
        {
            var frio = new AcumuloDeCongelamento(limite: 1, duracaoDoCongelamento: 3f);
            bool descongelou = false;
            frio.OnDescongelou += () => descongelou = true;

            frio.AplicarAcumulo(); // congela
            frio.Limpar();

            Assert.IsFalse(frio.EstaCongelado);
            Assert.AreEqual(0, frio.Acumulos);
            Assert.IsTrue(descongelou);
        }

        [Test]
        public void OnAcumulosMudaram_ReportaAContagem()
        {
            var frio = new AcumuloDeCongelamento(limite: 3);
            var vistos = new System.Collections.Generic.List<int>();
            frio.OnAcumulosMudaram += n => vistos.Add(n);

            frio.AplicarAcumulo();
            frio.AplicarAcumulo();
            frio.AplicarAcumulo(); // congela -> reporta 0

            Assert.AreEqual(new[] { 1, 2, 0 }, vistos.ToArray());
        }
    }
}
