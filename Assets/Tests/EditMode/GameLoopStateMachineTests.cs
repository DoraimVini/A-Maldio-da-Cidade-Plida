using NUnit.Framework;
using FavelaAmarela.Core.GameLoop;

namespace FavelaAmarela.Tests.EditMode
{
    public class GameLoopStateMachineTests
    {
        [Test]
        public void InitialState_ShouldBeMenu()
        {
            var sm = new GameLoopStateMachine();
            Assert.AreEqual(GameState.Menu, sm.CurrentState);
        }

        [Test]
        public void ValidTransition_ShouldChangeStateAndReturnTrue()
        {
            var sm = new GameLoopStateMachine(GameState.Menu);
            bool success = sm.TryTransition(GameState.Gameplay);

            Assert.IsTrue(success);
            Assert.AreEqual(GameState.Gameplay, sm.CurrentState);
        }

        [Test]
        public void InvalidTransition_ShouldNotChangeStateAndReturnFalse()
        {
            var sm = new GameLoopStateMachine(GameState.Menu);
            bool success = sm.TryTransition(GameState.Colapso);

            Assert.IsFalse(success);
            Assert.AreEqual(GameState.Menu, sm.CurrentState);
        }

        [Test]
        public void ValidTransition_ShouldFireEvent()
        {
            var sm = new GameLoopStateMachine(GameState.Menu);
            bool eventFired = false;

            sm.OnStateChanged += (de, para) =>
            {
                Assert.AreEqual(GameState.Menu, de);
                Assert.AreEqual(GameState.Gameplay, para);
                eventFired = true;
            };

            sm.TryTransition(GameState.Gameplay);
            Assert.IsTrue(eventFired);
        }

        [Test]
        public void InvalidTransition_ShouldNotFireEvent()
        {
            var sm = new GameLoopStateMachine(GameState.Menu);
            bool eventFired = false;

            sm.OnStateChanged += (de, para) => eventFired = true;

            sm.TryTransition(GameState.Colapso);
            Assert.IsFalse(eventFired);
        }

        [Test]
        public void Gameplay_ToTransicaoDeFase_ShouldSucceed()
        {
            var sm = new GameLoopStateMachine(GameState.Gameplay);
            bool success = sm.TryTransition(GameState.TransicaoDeFase);

            Assert.IsTrue(success);
            Assert.AreEqual(GameState.TransicaoDeFase, sm.CurrentState);
        }

        [Test]
        public void TransicaoDeFase_ToMenu_ShouldSucceed()
        {
            var sm = new GameLoopStateMachine(GameState.TransicaoDeFase);
            bool success = sm.TryTransition(GameState.Menu);

            Assert.IsTrue(success);
            Assert.AreEqual(GameState.Menu, sm.CurrentState);
        }

        [Test]
        public void TransicaoDeFase_ToGameplay_ShouldBeRejected()
        {
            var sm = new GameLoopStateMachine(GameState.TransicaoDeFase);
            bool success = sm.TryTransition(GameState.Gameplay);

            Assert.IsFalse(success);
            Assert.AreEqual(GameState.TransicaoDeFase, sm.CurrentState);
        }

        // ── MundoCongelado: a regra que o GameStatePresenter traduz em Time.timeScale ──

        [TestCase(GameState.Pausado)]
        [TestCase(GameState.TransicaoDeFase)]
        public void MundoCongelado_VerdadeiroNosEstadosQueParamOTempo(GameState estado)
        {
            var sm = new GameLoopStateMachine(estado);
            Assert.IsTrue(sm.MundoCongelado);
        }

        [TestCase(GameState.Menu)]
        [TestCase(GameState.Gameplay)]
        public void MundoCongelado_FalsoNosEstadosQueDeixamOTempoCorrer(GameState estado)
        {
            var sm = new GameLoopStateMachine(estado);
            Assert.IsFalse(sm.MundoCongelado);
        }

        [Test]
        public void MundoCongelado_FalsoNoColapso_ParaASequenciaDeMorteTocar()
        {
            // A dissolução do Colapso é animada: congelar o tempo aqui deixaria a tela de morte
            // parada para sempre.
            var sm = new GameLoopStateMachine(GameState.Colapso);
            Assert.IsFalse(sm.MundoCongelado);
        }

        [Test]
        public void MundoCongelado_AcompanhaATransicao()
        {
            var sm = new GameLoopStateMachine(GameState.Gameplay);
            Assert.IsFalse(sm.MundoCongelado);

            sm.TryTransition(GameState.Pausado);
            Assert.IsTrue(sm.MundoCongelado, "Pausar tem de congelar.");

            sm.TryTransition(GameState.Gameplay);
            Assert.IsFalse(sm.MundoCongelado, "Despausar tem de soltar.");
        }
    }
}
