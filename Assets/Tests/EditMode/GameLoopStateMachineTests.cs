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
    }
}
