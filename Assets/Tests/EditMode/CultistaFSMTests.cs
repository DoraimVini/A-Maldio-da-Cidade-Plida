using NUnit.Framework;
using UnityEngine;
using FavelaAmarela.Core.Enemies;

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
        public void ReceberEstimuloSonoro_RaioZero_NenhumaTransicao()
        {
            fsm.ReceberEstimuloSonoro(Vector2.zero, 1f, raioEfetivo: 0f);
            Assert.AreEqual(CultistaState.Errante, fsm.CurrentState);
        }

        [Test]
        public void ReceberEstimuloSonoro_ForaDoRaio_NenhumaTransicao()
        {
            fsm.ReceberEstimuloSonoro(Vector2.zero, 5f, raioEfetivo: 4f);
            Assert.AreEqual(CultistaState.Errante, fsm.CurrentState);
        }

        [Test]
        public void ReceberEstimuloSonoro_DentroDoRaio_ErranteParaAlerta()
        {
            fsm.ReceberEstimuloSonoro(Vector2.zero, 7f, raioEfetivo: 8.5f);
            Assert.AreEqual(CultistaState.Alerta, fsm.CurrentState);
            Assert.AreEqual(0f, fsm.TimeSinceLastStimulus);
        }

        [Test]
        public void ReceberEstimuloSonoro_RepetidoDentroDoRaio_ResetaTempo()
        {
            fsm.ReceberEstimuloSonoro(Vector2.zero, 5f, raioEfetivo: 8.5f);
            Assert.AreEqual(CultistaState.Alerta, fsm.CurrentState);
            
            fsm.Tick(1.0f);
            Assert.AreEqual(1.0f, fsm.TimeSinceLastStimulus);
            
            fsm.ReceberEstimuloSonoro(Vector2.zero, 5f, raioEfetivo: 8.5f);
            Assert.AreEqual(0f, fsm.TimeSinceLastStimulus);
            Assert.AreEqual(CultistaState.Alerta, fsm.CurrentState);
        }

        [Test]
        public void Alerta_VoltaParaErrante_SeNaoHouverEstimuloPor8s()
        {
            fsm.ReceberEstimuloSonoro(Vector2.zero, 7f, raioEfetivo: 8.5f);
            Assert.AreEqual(CultistaState.Alerta, fsm.CurrentState);

            fsm.Tick(8.1f);
            Assert.AreEqual(CultistaState.Errante, fsm.CurrentState);
        }

        [Test]
        public void Alerta_TransicionaParaCaca_AposPausaTelegrafada()
        {
            fsm.ReceberEstimuloSonoro(Vector2.zero, 5f, raioEfetivo: 8.5f);
            Assert.AreEqual(CultistaState.Alerta, fsm.CurrentState);

            fsm.Tick(1.0f);
            Assert.AreEqual(CultistaState.Alerta, fsm.CurrentState);

            fsm.ReceberEstimuloSonoro(Vector2.zero, 5f, raioEfetivo: 8.5f);
            
            fsm.Tick(0.6f);
            
            Assert.AreEqual(CultistaState.Caca, fsm.CurrentState);
        }

        [Test]
        public void Caca_VoltaParaErrante_SePerderRastroPor10s()
        {
            fsm.ReceberEstimuloSonoro(Vector2.zero, 5f, raioEfetivo: 8.5f);
            fsm.Tick(1.0f);
            fsm.ReceberEstimuloSonoro(Vector2.zero, 5f, raioEfetivo: 8.5f);
            fsm.Tick(0.6f);

            Assert.AreEqual(CultistaState.Caca, fsm.CurrentState);

            fsm.Tick(10.1f);

            Assert.AreEqual(CultistaState.Errante, fsm.CurrentState);
        }
        
        [Test]
        public void Tick_NaoAcumulaLixo_SemFazerTransicoesInvalidas()
        {
            fsm.Tick(100f);
            Assert.AreEqual(CultistaState.Errante, fsm.CurrentState);
        }

        [Test]
        public void ReceberEstimulo_Valido_GravaOrigemConhecida()
        {
            var origem = new Vector2(10f, 5f);
            fsm.ReceberEstimuloSonoro(origem, 5f, 10f);
            Assert.AreEqual(origem, fsm.UltimaOrigemConhecida);
        }

        [Test]
        public void NenhumEstimulo_OrigemConhecida_E_Null()
        {
            Assert.IsNull(fsm.UltimaOrigemConhecida);
        }

        [Test]
        public void EstimuloInvalido_NaoSobreescreve_OrigemConhecida()
        {
            var origemValida = new Vector2(10f, 5f);
            fsm.ReceberEstimuloSonoro(origemValida, 5f, 10f);
            Assert.AreEqual(origemValida, fsm.UltimaOrigemConhecida);

            // Fora do raio
            fsm.ReceberEstimuloSonoro(new Vector2(20f, 20f), 15f, 10f);
            Assert.AreEqual(origemValida, fsm.UltimaOrigemConhecida);

            // Raio zero
            fsm.ReceberEstimuloSonoro(new Vector2(30f, 30f), 1f, 0f);
            Assert.AreEqual(origemValida, fsm.UltimaOrigemConhecida);
        }
    }
}
