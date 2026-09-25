using NUnit.Framework;
using FavelaAmarela.Core.Player;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Suite EditMode de <see cref="PlayerStateMachine.ForcarEstado"/> — o caminho dos
    /// efeitos de controle impostos pelo inimigo (hoje, o congelamento pelos Cones de
    /// Gelo de Abdul), distinto das ações que o jogador escolhe.
    /// </summary>
    public class PlayerStateMachineForcarEstadoTests
    {
        [Test]
        public void ForcarEstado_EntraMesmoComAcaoEmCurso()
        {
            var fsm = new PlayerStateMachine();
            fsm.TryEntrarAcao(PlayerState.Esquivando, 5f);

            fsm.ForcarEstado(PlayerState.Congelado, 1.5f);

            Assert.AreEqual(PlayerState.Congelado, fsm.CurrentState,
                "Um atordoamento não pode falhar só porque o jogador estava esquivando — " +
                "seria a janela para ignorá-lo.");
        }

        [Test]
        public void ForcarEstado_APartirDeLivre_Funciona()
        {
            var fsm = new PlayerStateMachine();
            fsm.ForcarEstado(PlayerState.Congelado, 1.5f);
            Assert.AreEqual(PlayerState.Congelado, fsm.CurrentState);
            Assert.IsFalse(fsm.EstaLivre);
        }

        [Test]
        public void Congelado_ExpiraSozinhoEVoltaALivre()
        {
            var fsm = new PlayerStateMachine();
            fsm.ForcarEstado(PlayerState.Congelado, 1.5f);

            fsm.Tick(1.0f);
            Assert.AreEqual(PlayerState.Congelado, fsm.CurrentState);

            fsm.Tick(0.6f);
            Assert.AreEqual(PlayerState.Livre, fsm.CurrentState);
        }

        [Test]
        public void Congelado_BloqueiaNovasAcoes()
        {
            var fsm = new PlayerStateMachine();
            fsm.ForcarEstado(PlayerState.Congelado, 2f);

            bool entrou = fsm.TryEntrarAcao(PlayerState.Atacando, 0.3f);

            Assert.IsFalse(entrou, "Congelado não pode atacar.");
            Assert.AreEqual(PlayerState.Congelado, fsm.CurrentState);
        }

        [Test]
        public void ForcarEstado_DisparaOnStateChanged()
        {
            var fsm = new PlayerStateMachine();
            PlayerState anterior = PlayerState.Livre, novo = PlayerState.Livre;
            fsm.OnStateChanged += (a, n) => { anterior = a; novo = n; };

            fsm.TryEntrarAcao(PlayerState.Atacando, 1f);
            fsm.ForcarEstado(PlayerState.Congelado, 1.5f);

            Assert.AreEqual(PlayerState.Atacando, anterior);
            Assert.AreEqual(PlayerState.Congelado, novo);
        }

        [Test]
        public void ForcarEstado_Livre_NaoFazNada()
        {
            var fsm = new PlayerStateMachine();
            fsm.TryEntrarAcao(PlayerState.Atacando, 1f);

            fsm.ForcarEstado(PlayerState.Livre, 1f);

            Assert.AreEqual(PlayerState.Atacando, fsm.CurrentState,
                "Para liberar, o caminho é ForcarLivre().");
        }

        [Test]
        public void ForcarEstado_DuracaoInvalida_Ignorada()
        {
            var fsm = new PlayerStateMachine();
            fsm.ForcarEstado(PlayerState.Congelado, 0f);
            fsm.ForcarEstado(PlayerState.Congelado, -1f);
            Assert.AreEqual(PlayerState.Livre, fsm.CurrentState);
        }
    }
}
