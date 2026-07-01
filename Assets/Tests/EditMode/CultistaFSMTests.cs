using NUnit.Framework;
using FavelaAmarela.Core.AI;

namespace FavelaAmarela.Tests.EditMode
{
    public class CultistaFSMTests
    {
        private CultistaFSM fsm;

        [SetUp]
        public void Setup()
        {
            fsm = new CultistaFSM();
        }

        [Test]
        public void Inicializa_Em_EstadoErrante()
        {
            Assert.AreEqual(CultistaState.Errante, fsm.CurrentState);
        }

        [Test]
        public void ReceberEstimuloSonoro_DistanteNãoAlerta()
        {
            // Jogador andando a 10m (o limite é 8m)
            fsm.ReceberEstimuloSonoro(10f, jogadorCorrendo: false);
            Assert.AreEqual(CultistaState.Errante, fsm.CurrentState);
        }

        [Test]
        public void ReceberEstimuloSonoro_AndandoPerto_Alerta()
        {
            // Jogador andando a 7m (limite 8m)
            fsm.ReceberEstimuloSonoro(7f, jogadorCorrendo: false);
            Assert.AreEqual(CultistaState.Alerta, fsm.CurrentState);
            Assert.AreEqual(0f, fsm.TimeSinceLastStimulus);
        }

        [Test]
        public void ReceberEstimuloSonoro_CorrendoLonge_Alerta()
        {
            // Jogador correndo a 13m (limite 14m)
            fsm.ReceberEstimuloSonoro(13f, jogadorCorrendo: true);
            Assert.AreEqual(CultistaState.Alerta, fsm.CurrentState);
        }

        [Test]
        public void Alerta_VoltaParaErrante_SeNaoHouverEstimuloPor4s()
        {
            fsm.ReceberEstimuloSonoro(7f, false);
            Assert.AreEqual(CultistaState.Alerta, fsm.CurrentState);

            // Simula passagem de tempo sem ouvir nada
            fsm.Tick(4.1f);
            Assert.AreEqual(CultistaState.Errante, fsm.CurrentState);
        }

        [Test]
        public void Alerta_TransicionaParaCaca_AposPausaTelegrafada()
        {
            // O jogador faz barulho
            fsm.ReceberEstimuloSonoro(5f, false);
            Assert.AreEqual(CultistaState.Alerta, fsm.CurrentState);

            // Passa 1.0s (ainda em alerta, não terminou os 1.5s)
            fsm.Tick(1.0f);
            Assert.AreEqual(CultistaState.Alerta, fsm.CurrentState);

            // O jogador faz barulho DE NOVO (o que valida a caça pois o TimeSinceLastStimulus zera)
            fsm.ReceberEstimuloSonoro(5f, false);
            
            // Passa mais 0.6s (total = 1.6s em Alerta)
            fsm.Tick(0.6f);
            
            Assert.AreEqual(CultistaState.Caca, fsm.CurrentState);
        }

        [Test]
        public void Caca_VoltaParaErrante_SePerderRastroPor5s()
        {
            // Força ida para caça
            fsm.ReceberEstimuloSonoro(5f, false); // Alerta
            fsm.Tick(1.0f);
            fsm.ReceberEstimuloSonoro(5f, false);
            fsm.Tick(0.6f); // Caça
            
            Assert.AreEqual(CultistaState.Caca, fsm.CurrentState);

            // Tempo passa sem ouvir o jogador
            fsm.Tick(5.1f);

            Assert.AreEqual(CultistaState.Errante, fsm.CurrentState);
        }
        
        [Test]
        public void Tick_NaoAcumulaLixo_SemFazerTransicoesInvalidas()
        {
            fsm.Tick(100f);
            // Continua em Errante sem bugar
            Assert.AreEqual(CultistaState.Errante, fsm.CurrentState);
        }
    }
}
