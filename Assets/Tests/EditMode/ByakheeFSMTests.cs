using NUnit.Framework;
using FavelaAmarela.Core.Enemies;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Trava as regras da luta do Byakhee (<c>lore/cassilda_e_byakhee.md</c> §IV).
    ///
    /// <para>O que estes testes protegem é a <b>inversão</b> que define a luta: ele é imune no
    /// ar e vulnerável só no chão. Se alguém "consertar" isso deixando-o sempre atacável, a
    /// luta vira um saco de pancadas e o design morre sem ninguém notar.</para>
    /// </summary>
    public class ByakheeFSMTests
    {
        private static ByakheeFSM CriarEmCombate()
        {
            var fsm = new ByakheeFSM();
            fsm.IniciarLuta();
            return fsm;
        }

        [Test]
        public void Espreita_NaoPodeReceberDano()
        {
            var fsm = new ByakheeFSM();

            Assert.AreEqual(ByakheeState.Espreita, fsm.CurrentState);
            Assert.IsFalse(fsm.PodeReceberDano, "Antes do grito de abertura ele é intocável.");
        }

        [Test]
        public void NoAr_EhImune()
        {
            var fsm = CriarEmCombate();

            Assert.AreEqual(ByakheeState.Rasante, fsm.CurrentState);
            Assert.IsFalse(fsm.PodeReceberDano, "No ar o Byakhee não pode ser ferido.");
        }

        [Test]
        public void Pousado_AbreAJanelaDeDano()
        {
            var fsm = CriarEmCombate();

            // Rasante → mergulho → pouso: o primeiro ciclo completo da fase 1.
            fsm.Tick(2.1f);   // fim do rasante
            fsm.Tick(1.3f);   // fim do mergulho

            Assert.AreEqual(ByakheeState.Pousado, fsm.CurrentState);
            Assert.IsTrue(fsm.PodeReceberDano, "O pouso é a única janela de dano.");
        }

        /// <summary>
        /// Avança em passos pequenos até o Byakhee pousar, sem presumir a ordem do ciclo — que
        /// alterna e muda por fase. Devolve o FSM já na janela, com o relógio dela zerado.
        /// </summary>
        private static void AvancarAtePousar(ByakheeFSM fsm)
        {
            for (int i = 0; i < 1000 && fsm.CurrentState != ByakheeState.Pousado; i++)
                fsm.Tick(0.05f);

            Assert.AreEqual(ByakheeState.Pousado, fsm.CurrentState,
                "Não chegou a pousar — a luta ficaria sem nenhuma janela de dano.");
        }

        [Test]
        public void JanelaDeDano_EncurtaDaFase1ParaAFase2()
        {
            // O aperto da fase 2 vem de janela MENOR, não de dano maior — se alguém trocar
            // isso por "mais dano", a luta muda de caráter e este teste cai.
            var fase1 = CriarEmCombate();
            fase1.AtualizarFracaoDeVida(1f);
            AvancarAtePousar(fase1);

            fase1.Tick(1.6f);
            Assert.AreEqual(ByakheeState.Pousado, fase1.CurrentState,
                "Na fase 1 a janela dura 2 s — 1,6 s ainda está dentro.");

            var fase2 = CriarEmCombate();
            fase2.AtualizarFracaoDeVida(0.5f);
            AvancarAtePousar(fase2);

            fase2.Tick(1.6f);
            Assert.AreNotEqual(ByakheeState.Pousado, fase2.CurrentState,
                "Na fase 2 a janela dura 1,5 s — 1,6 s já a fechou.");
        }

        [Test]
        public void Fase3_CircundaEPousaSozinhoDepoisDoIntervalo()
        {
            // A válvula que impede impasse para quem não tem a Lâmina do Sinal.
            var fsm = new ByakheeFSM(intervaloPousoEspontaneo: 5f);
            fsm.IniciarLuta();
            fsm.AtualizarFracaoDeVida(0.2f);   // fase 3

            fsm.Tick(2.1f);
            Assert.AreEqual(ByakheeState.Circundando, fsm.CurrentState);

            fsm.Tick(4.9f);
            Assert.AreEqual(ByakheeState.Circundando, fsm.CurrentState, "Ainda não deu o tempo.");

            fsm.Tick(0.2f);
            Assert.AreEqual(ByakheeState.Pousado, fsm.CurrentState,
                "Sem a Lâmina, o pouso espontâneo é a única saída da fase 3.");
        }

        [Test]
        public void CortarAsa_SoFuncionaNaFase3EEmVoo()
        {
            var fase1 = CriarEmCombate();
            Assert.IsFalse(fase1.CortarAsa(), "Na fase 1 a asa não é alvo.");

            var fase3 = CriarEmCombate();
            fase3.AtualizarFracaoDeVida(0.2f);
            Assert.IsTrue(fase3.CortarAsa(), "Em voo na fase 3, o corte força o pouso.");
            Assert.AreEqual(ByakheeState.Pousado, fase3.CurrentState);
        }

        [Test]
        public void Frenesi_DisparaAbaixoDe10PorCentoESoSaiPorGolpe()
        {
            var fsm = CriarEmCombate();
            fsm.AtualizarFracaoDeVida(0.08f);

            Assert.AreEqual(ByakheeState.Frenesi, fsm.CurrentState);

            // Não sai sozinho: o relógio corre contra o jogador.
            fsm.Tick(30f);
            Assert.AreEqual(ByakheeState.Frenesi, fsm.CurrentState);

            Assert.IsTrue(fsm.InterromperFrenesi());
            Assert.AreEqual(ByakheeState.Pousado, fsm.CurrentState,
                "Interromper derruba a criatura — a recompensa é a janela de dano.");
        }

        [Test]
        public void Frenesi_DrenaMaisResilienciaQueOGritoPassivo()
        {
            var fsm = CriarEmCombate();
            float passivo = fsm.DrenoDeResilienciaPorSegundo;

            fsm.AtualizarFracaoDeVida(0.05f);

            Assert.Greater(fsm.DrenoDeResilienciaPorSegundo, passivo,
                "O frenesi existe para apertar o relógio da sanidade.");
        }

        [Test]
        public void Espreita_ENaoDerrotado_NaoDrenamResiliencia()
        {
            var antes = new ByakheeFSM();
            Assert.AreEqual(0f, antes.DrenoDeResilienciaPorSegundo,
                "O grito só começa quando a luta começa.");

            var depois = CriarEmCombate();
            depois.AtualizarFracaoDeVida(0f);
            Assert.AreEqual(ByakheeState.Derrotado, depois.CurrentState);
            Assert.AreEqual(0f, depois.DrenoDeResilienciaPorSegundo,
                "Morto, o silêncio é físico — o dreno para.");
        }

        [Test]
        public void VidaZerada_DisparaDerrotadoUmaVezSo()
        {
            var fsm = CriarEmCombate();

            int vezes = 0;
            fsm.OnDerrotado += () => vezes++;

            fsm.AtualizarFracaoDeVida(0f);
            fsm.AtualizarFracaoDeVida(0f);
            fsm.Tick(5f);

            Assert.AreEqual(1, vezes, "Derrotar duas vezes dropava o Anel duas vezes.");
        }
    }
}
