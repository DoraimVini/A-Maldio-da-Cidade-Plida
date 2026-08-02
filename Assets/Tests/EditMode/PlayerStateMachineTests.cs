using NUnit.Framework;
using FavelaAmarela.Core.Player;

namespace FavelaAmarela.Tests.EditMode
{
    public class PlayerStateMachineTests
    {
        private PlayerStateMachine fsm;

        [SetUp]
        public void Setup()
        {
            fsm = new PlayerStateMachine();
        }

        [Test]
        public void ComecaLivre()
        {
            Assert.AreEqual(PlayerState.Livre, fsm.CurrentState);
            Assert.IsTrue(fsm.EstaLivre);
        }

        [Test]
        public void TryEntrarAcao_APartirDeLivre_Entra()
        {
            bool ok = fsm.TryEntrarAcao(PlayerState.Esquivando, 0.2f);

            Assert.IsTrue(ok);
            Assert.AreEqual(PlayerState.Esquivando, fsm.CurrentState);
            Assert.AreEqual(0.2f, fsm.TempoRestante, 0.0001f);
        }

        [Test]
        public void TryEntrarAcao_ComAcaoEmCurso_Rejeita()
        {
            fsm.TryEntrarAcao(PlayerState.Esquivando, 0.15f);

            bool ok = fsm.TryEntrarAcao(PlayerState.Atacando, 0.3f);

            Assert.IsFalse(ok);
            Assert.AreEqual(PlayerState.Esquivando, fsm.CurrentState); // não trocou de ação
        }

        [Test]
        public void TryEntrarAcao_PedindoLivre_Rejeita()
        {
            bool ok = fsm.TryEntrarAcao(PlayerState.Livre, 1f);

            Assert.IsFalse(ok);
            Assert.AreEqual(PlayerState.Livre, fsm.CurrentState);
        }

        [Test]
        public void Tick_AoEsgotarDuracao_VoltaParaLivre()
        {
            fsm.TryEntrarAcao(PlayerState.Atacando, 0.3f);

            fsm.Tick(0.2f);
            Assert.AreEqual(PlayerState.Atacando, fsm.CurrentState); // ainda dentro da duração

            fsm.Tick(0.15f); // total 0.35 > 0.3
            Assert.AreEqual(PlayerState.Livre, fsm.CurrentState);
        }

        [Test]
        public void ForcarLivre_InterrompeAcao()
        {
            fsm.TryEntrarAcao(PlayerState.Esquivando, 5f);

            fsm.ForcarLivre();

            Assert.AreEqual(PlayerState.Livre, fsm.CurrentState);
            Assert.AreEqual(0f, fsm.TempoRestante, 0.0001f);
        }

        [Test]
        public void OnStateChanged_DisparaNaTransicao()
        {
            PlayerState? antigo = null;
            PlayerState? novo = null;
            int contador = 0;
            fsm.OnStateChanged += (a, n) => { antigo = a; novo = n; contador++; };

            fsm.TryEntrarAcao(PlayerState.Esquivando, 0.15f);

            Assert.AreEqual(1, contador);
            Assert.AreEqual(PlayerState.Livre, antigo);
            Assert.AreEqual(PlayerState.Esquivando, novo);
        }

        [Test]
        public void TimeInState_ReiniciaNaTransicao()
        {
            fsm.TryEntrarAcao(PlayerState.Esquivando, 10f);
            fsm.Tick(0.5f);
            Assert.AreEqual(0.5f, fsm.TimeInState, 0.0001f);

            fsm.ForcarLivre();
            Assert.AreEqual(0f, fsm.TimeInState, 0.0001f); // resetou ao virar Livre
        }
    }
}
