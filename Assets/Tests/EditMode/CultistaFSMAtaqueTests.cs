using NUnit.Framework;
using UnityEngine;
using FavelaAmarela.Core.Enemies;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Suite EditMode do estado <see cref="CultistaState.Atacar"/> da FSM do Cultista:
    /// entra em Atacar por proximidade (não por som), desfere golpes por cadência,
    /// volta para Caça quando o alvo sai do corpo-a-corpo e é interrompível por atordoamento.
    /// </summary>
    public class CultistaFSMAtaqueTests
    {
        [Test]
        public void Caca_ComAlvoAoAlcance_TransicionaParaAtacar()
        {
            var fsm = new CultistaFSM(CultistaState.Caca);
            fsm.AtualizarAlcanceDoAlvo(true);
            fsm.Tick(0.01f);
            Assert.AreEqual(CultistaState.Atacar, fsm.CurrentState);
        }

        [Test]
        public void Atacar_ForaDoAlcance_VoltaParaCaca()
        {
            var fsm = new CultistaFSM(CultistaState.Caca);
            fsm.AtualizarAlcanceDoAlvo(true);
            fsm.Tick(0.01f); // -> Atacar

            fsm.AtualizarAlcanceDoAlvo(false);
            fsm.Tick(0.01f);
            Assert.AreEqual(CultistaState.Caca, fsm.CurrentState);
        }

        [Test]
        public void Atacar_PrimeiroGolpeSoAposCadenciaCompleta()
        {
            var fsm = new CultistaFSM(CultistaState.Caca, cadenciaDeAtaque: 1.0f);
            int golpes = 0;
            fsm.OnGolpeDesferido += () => golpes++;

            fsm.AtualizarAlcanceDoAlvo(true);
            fsm.Tick(0.01f); // entra em Atacar, timer zera, nenhum golpe imediato
            Assert.AreEqual(0, golpes, "Não deve golpear no frame em que entra em Atacar.");

            fsm.Tick(0.9f); // timer 0.9 < 1.0
            Assert.AreEqual(0, golpes, "Não deve golpear antes de completar a cadência.");

            fsm.Tick(0.1f); // timer 1.0 -> golpe
            Assert.AreEqual(1, golpes, "Deve golpear ao completar a cadência.");
        }

        [Test]
        public void Atacar_DesfereUmGolpePorCadencia()
        {
            var fsm = new CultistaFSM(CultistaState.Caca, cadenciaDeAtaque: 1.0f);
            int golpes = 0;
            fsm.OnGolpeDesferido += () => golpes++;

            fsm.AtualizarAlcanceDoAlvo(true);
            fsm.Tick(0.01f); // -> Atacar

            fsm.Tick(1.0f); // golpe 1
            fsm.Tick(1.0f); // golpe 2
            fsm.Tick(0.5f); // acumula, sem golpe
            Assert.AreEqual(2, golpes);
        }

        [Test]
        public void Atacar_Atordoamento_InterrompeEZeraOEstado()
        {
            var fsm = new CultistaFSM(CultistaState.Caca, cadenciaDeAtaque: 1.0f);
            int golpes = 0;
            fsm.OnGolpeDesferido += () => golpes++;

            fsm.AtualizarAlcanceDoAlvo(true);
            fsm.Tick(0.01f); // -> Atacar
            fsm.Tick(0.9f);  // timer 0.9, sem golpe ainda

            fsm.AtordoarPor(2f);
            Assert.AreEqual(CultistaState.Atordoado, fsm.CurrentState);

            // Mesmo com o alvo ainda ao alcance, atordoado não golpeia.
            fsm.Tick(0.5f);
            Assert.AreEqual(0, golpes);
        }

        [Test]
        public void Atacar_SaiuERetornou_TimerReiniciaDoZero()
        {
            var fsm = new CultistaFSM(CultistaState.Caca, cadenciaDeAtaque: 1.0f);
            int golpes = 0;
            fsm.OnGolpeDesferido += () => golpes++;

            fsm.AtualizarAlcanceDoAlvo(true);
            fsm.Tick(0.01f); // -> Atacar
            fsm.Tick(0.9f);  // timer 0.9

            fsm.AtualizarAlcanceDoAlvo(false);
            fsm.Tick(0.01f); // -> Caca (perde o acumulado)

            fsm.AtualizarAlcanceDoAlvo(true);
            fsm.Tick(0.01f); // -> Atacar de novo, timer zerado
            fsm.Tick(0.2f);  // timer 0.2 (não 1.1) -> sem golpe
            Assert.AreEqual(0, golpes, "O timer deve reiniciar ao reengajar, não somar o acúmulo antigo.");
        }
    }
}
