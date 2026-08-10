using NUnit.Framework;
using UnityEngine;
using FavelaAmarela.Core.Enemies;

namespace FavelaAmarela.Tests.EditMode
{
    public class CoisaDoCemiterioFSMTests
    {
        private CoisaDoCemiterioFSM fsm;

        [SetUp]
        public void Setup()
        {
            fsm = new CoisaDoCemiterioFSM(duracaoAlvoPreciso: 6f);
        }

        [Test]
        public void Inicializa_Em_EstadoFarejando()
        {
            Assert.AreEqual(CoisaDoCemiterioState.Farejando, fsm.CurrentState);
        }

        [Test]
        public void ReceberEstimuloSonoro_RaioZero_NenhumaTransicao()
        {
            fsm.ReceberEstimuloSonoro(Vector2.zero, 1f, raioEfetivo: 0f);
            Assert.AreEqual(CoisaDoCemiterioState.Farejando, fsm.CurrentState);
        }

        [Test]
        public void ReceberEstimuloSonoro_ForaDoRaio_NenhumaTransicao()
        {
            fsm.ReceberEstimuloSonoro(Vector2.zero, 5f, raioEfetivo: 4f);
            Assert.AreEqual(CoisaDoCemiterioState.Farejando, fsm.CurrentState);
        }

        [Test]
        public void ReceberEstimuloSonoro_DentroDoRaio_FarejandoParaAlvoPreciso()
        {
            fsm.ReceberEstimuloSonoro(Vector2.zero, 7f, raioEfetivo: 8.5f);
            Assert.AreEqual(CoisaDoCemiterioState.AlvoPreciso, fsm.CurrentState);
            Assert.AreEqual(0f, fsm.TimeSinceLastStimulus);
        }

        [Test]
        public void ReceberEstimuloSonoro_GravaOrigemConhecida()
        {
            var origem = new Vector2(3f, -2f);
            fsm.ReceberEstimuloSonoro(origem, 5f, 10f);
            Assert.AreEqual(origem, fsm.UltimaOrigemConhecida);
        }

        [Test]
        public void AlvoPreciso_VoltaParaFarejando_AposDuracaoSemEstimulo()
        {
            fsm.ReceberEstimuloSonoro(Vector2.zero, 5f, raioEfetivo: 8.5f);
            Assert.AreEqual(CoisaDoCemiterioState.AlvoPreciso, fsm.CurrentState);

            fsm.Tick(5.9f);
            Assert.AreEqual(CoisaDoCemiterioState.AlvoPreciso, fsm.CurrentState);

            fsm.Tick(0.2f);
            Assert.AreEqual(CoisaDoCemiterioState.Farejando, fsm.CurrentState);
        }

        [Test]
        public void ReceberEstimuloSonoro_RepetidoDentroDoRaio_ResetaTempoEMantemAlvoPreciso()
        {
            fsm.ReceberEstimuloSonoro(Vector2.zero, 5f, raioEfetivo: 8.5f);
            fsm.Tick(5f);
            Assert.AreEqual(CoisaDoCemiterioState.AlvoPreciso, fsm.CurrentState);

            fsm.ReceberEstimuloSonoro(new Vector2(1f, 1f), 5f, raioEfetivo: 8.5f);
            Assert.AreEqual(0f, fsm.TimeSinceLastStimulus);
            Assert.AreEqual(CoisaDoCemiterioState.AlvoPreciso, fsm.CurrentState);

            fsm.Tick(5.9f);
            Assert.AreEqual(CoisaDoCemiterioState.AlvoPreciso, fsm.CurrentState);
        }

        [Test]
        public void NenhumEstimulo_OrigemConhecida_E_Null()
        {
            Assert.IsNull(fsm.UltimaOrigemConhecida);
        }

        [Test]
        public void Tick_SemEstimulo_NaoFazTransicaoInvalida()
        {
            fsm.Tick(100f);
            Assert.AreEqual(CoisaDoCemiterioState.Farejando, fsm.CurrentState);
        }
    }
}
