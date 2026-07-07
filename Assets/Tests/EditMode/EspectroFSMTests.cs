using NUnit.Framework;
using FavelaAmarela.Core.Enemies;

namespace FavelaAmarela.Tests.EditMode
{
    public class EspectroFSMTests
    {
        private EspectroFSM fsm;

        [SetUp]
        public void Setup()
        {
            fsm = new EspectroFSM();
        }

        [Test]
        public void Inicializa_Em_EstadoLatente()
        {
            Assert.AreEqual(EspectroState.Latente, fsm.CurrentState);
        }

        [Test]
        public void TryTransition_LatenteParaManifestando_Sucesso()
        {
            bool resultado = fsm.TryTransition(EspectroState.Manifestando);

            Assert.IsTrue(resultado);
            Assert.AreEqual(EspectroState.Manifestando, fsm.CurrentState);
        }

        [Test]
        public void TryTransition_ManifestandoParaCercando_Sucesso()
        {
            fsm.TryTransition(EspectroState.Manifestando);

            bool resultado = fsm.TryTransition(EspectroState.Cercando);

            Assert.IsTrue(resultado);
            Assert.AreEqual(EspectroState.Cercando, fsm.CurrentState);
        }

        [Test]
        public void TryTransition_PulandoManifestando_Rejeitada()
        {
            bool resultado = fsm.TryTransition(EspectroState.Cercando);

            Assert.IsFalse(resultado);
            Assert.AreEqual(EspectroState.Latente, fsm.CurrentState);
        }

        [Test]
        public void TryTransition_Retroceder_Rejeitada()
        {
            fsm.TryTransition(EspectroState.Manifestando);
            fsm.TryTransition(EspectroState.Cercando);

            bool resultado = fsm.TryTransition(EspectroState.Latente);

            Assert.IsFalse(resultado);
            Assert.AreEqual(EspectroState.Cercando, fsm.CurrentState);
        }

        [Test]
        public void TryTransition_Valida_DisparaOnStateChanged()
        {
            EspectroState? anteriorRecebido = null;
            EspectroState? atualRecebido = null;
            fsm.OnStateChanged += (anterior, atual) =>
            {
                anteriorRecebido = anterior;
                atualRecebido = atual;
            };

            fsm.TryTransition(EspectroState.Manifestando);

            Assert.AreEqual(EspectroState.Latente, anteriorRecebido);
            Assert.AreEqual(EspectroState.Manifestando, atualRecebido);
        }
    }
}
