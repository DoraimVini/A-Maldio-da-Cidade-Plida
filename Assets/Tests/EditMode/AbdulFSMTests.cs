using NUnit.Framework;
using FavelaAmarela.Core.Enemies;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Suite EditMode da luta contra Abdul Alhazred. Cobre as regras que fazem a luta ser
    /// uma luta: escudo bloqueando dano, Pedras de Poder como única abertura na Fase 1,
    /// escudo permanente na Fase 2, e a janela de exaustão como único momento de matar.
    /// </summary>
    public class AbdulFSMTests
    {
        [Test]
        public void Inicia_EmTranse_Intocavel()
        {
            var fsm = new AbdulFSM();
            Assert.AreEqual(AbdulState.Transe, fsm.CurrentState);
            Assert.IsFalse(fsm.PodeReceberDano, "Em transe (pré-luta) ele não pode ser ferido.");
        }

        [Test]
        public void IniciarLuta_VaiParaFase1_ComEscudoDePe()
        {
            var fsm = new AbdulFSM();
            fsm.IniciarLuta();

            Assert.AreEqual(AbdulState.Fase1, fsm.CurrentState);
            Assert.IsTrue(fsm.EscudoAtivo);
            Assert.IsFalse(fsm.PodeReceberDano, "Escudo de pé = imune.");
        }

        [Test]
        public void Fase1_QuebrarPedra_AbreJanelaDeDano()
        {
            var fsm = new AbdulFSM(duracaoEscudoQuebrado: 5f);
            fsm.IniciarLuta();

            fsm.QuebrarPedraDePoder();

            Assert.IsFalse(fsm.EscudoAtivo);
            Assert.IsTrue(fsm.PodeReceberDano, "Pedra quebrada é a única abertura da Fase 1.");
            Assert.AreEqual(1, fsm.PedrasQuebradas);
        }

        // ── A última Pedra derruba o escudo de vez (fix do softlock, 2026-08-01) ─────
        //
        // O bug: cada Pedra abria só uma janela temporária e o escudo sempre voltava.
        // Quebrada a última, não havia mais o que quebrar e o escudo nunca mais caía —
        // se Damião não tivesse levado Abdul abaixo do limiar da Fase 2 até ali, a luta
        // ficava **invencível**. Relatado pelo Vini jogando: "depois da quarta pedra não
        // dá para matá-lo".

        [Test]
        public void UltimaPedra_DerrubaOEscudoDeVez()
        {
            var fsm = new AbdulFSM(duracaoEscudoQuebrado: 3f, intervaloDeConjuracao: 99f);
            fsm.IniciarLuta();
            fsm.DefinirTotalDePedras(4);

            for (int i = 0; i < 4; i++) fsm.QuebrarPedraDePoder();

            Assert.IsTrue(fsm.EscudoDestruido);

            fsm.Tick(60f);   // muito além da janela de qualquer Pedra

            Assert.IsFalse(fsm.EscudoAtivo, "Sem Pedras de pé, nada sustenta o escudo.");
            Assert.IsTrue(fsm.PodeReceberDano, "A luta não pode virar invencível.");
        }

        [Test]
        public void PedrasParciais_EscudoAindaVolta()
        {
            // Com Pedras sobrando, a janela continua temporária — a tensão da Fase 1 depende
            // disso. Só a última muda a regra.
            var fsm = new AbdulFSM(duracaoEscudoQuebrado: 3f, intervaloDeConjuracao: 99f);
            fsm.IniciarLuta();
            fsm.DefinirTotalDePedras(4);

            fsm.QuebrarPedraDePoder();
            fsm.QuebrarPedraDePoder();

            Assert.IsFalse(fsm.EscudoDestruido);

            fsm.Tick(4f);
            Assert.IsTrue(fsm.EscudoAtivo, "Ainda há Pedras de pé: o escudo volta.");
        }

        [Test]
        public void SemTotalInformado_MantemOComportamentoAntigo()
        {
            // Degradação graciosa: se ninguém informar quantas Pedras existem, a FSM não
            // pode concluir que "todas caíram" e derrubar o escudo cedo demais.
            var fsm = new AbdulFSM(duracaoEscudoQuebrado: 3f, intervaloDeConjuracao: 99f);
            fsm.IniciarLuta();

            fsm.QuebrarPedraDePoder();
            Assert.IsFalse(fsm.EscudoDestruido);

            fsm.Tick(4f);
            Assert.IsTrue(fsm.EscudoAtivo);
        }

        [Test]
        public void Fase1_EscudoVoltaDepoisDoTempo()
        {
            var fsm = new AbdulFSM(duracaoEscudoQuebrado: 3f, intervaloDeConjuracao: 99f);
            fsm.IniciarLuta();
            fsm.QuebrarPedraDePoder();

            fsm.Tick(2.9f);
            Assert.IsTrue(fsm.PodeReceberDano, "Ainda dentro da janela.");

            fsm.Tick(0.2f);
            Assert.IsTrue(fsm.EscudoAtivo, "Passada a janela, ele reconjura o escudo.");
            Assert.IsFalse(fsm.PodeReceberDano);
        }

        [Test]
        public void Fase1_InvocaEsqueletosEmCadencia()
        {
            var fsm = new AbdulFSM(intervaloDeConjuracao: 2f);
            fsm.IniciarLuta();

            int invocacoes = 0;
            fsm.OnInvocarEsqueletos += () => invocacoes++;

            fsm.Tick(2f);
            fsm.Tick(2f);
            Assert.AreEqual(2, invocacoes);
        }

        [Test]
        public void VidaAbaixoDoLimiar_ViraFase2_ComEscudoPermanente()
        {
            var fsm = new AbdulFSM(fracaoParaFase2: 0.35f, duracaoEscudoQuebrado: 5f);
            fsm.IniciarLuta();
            fsm.QuebrarPedraDePoder();          // escudo baixo na Fase 1
            fsm.AtualizarFracaoDeVida(0.30f);   // cruza o limiar

            Assert.AreEqual(AbdulState.Fase2, fsm.CurrentState);
            Assert.IsTrue(fsm.EscudoAtivo, "Na Fase 2 o escudo sobe e passa a ser permanente.");
            Assert.IsFalse(fsm.PodeReceberDano);
        }

        [Test]
        public void Fase2_QuebrarPedra_NaoTemMaisEfeito()
        {
            var fsm = new AbdulFSM();
            fsm.IniciarLuta();
            fsm.AtualizarFracaoDeVida(0.2f); // -> Fase 2

            fsm.QuebrarPedraDePoder();

            Assert.IsTrue(fsm.EscudoAtivo,
                "Na Fase 2 as Pedras não derrubam mais o escudo — o plano do jogador tem de mudar.");
            Assert.IsFalse(fsm.PodeReceberDano);
        }

        [Test]
        public void Fase2_AposCicloDeMagias_FicaExaustoEVulneravel()
        {
            var fsm = new AbdulFSM(magiasPorCiclo: 3, intervaloDeConjuracao: 1f, duracaoExaustao: 4f);
            fsm.IniciarLuta();
            fsm.AtualizarFracaoDeVida(0.2f); // -> Fase 2

            int cones = 0, esqueletos = 0;
            fsm.OnConjurarConeDeGelo += () => cones++;
            fsm.OnInvocarEsqueletos += () => esqueletos++;

            fsm.Tick(1f);
            fsm.Tick(1f);
            Assert.AreEqual(AbdulState.Fase2, fsm.CurrentState, "Duas magias ainda não esgotam a mana.");

            fsm.Tick(1f); // terceira magia
            Assert.AreEqual(AbdulState.Exausto, fsm.CurrentState);
            Assert.IsFalse(fsm.EscudoAtivo);
            Assert.IsTrue(fsm.PodeReceberDano, "A exaustão é a janela do golpe de misericórdia.");
            Assert.AreEqual(3, cones + esqueletos);
        }

        [Test]
        public void Exausto_RecuperaMana_EVoltaParaFase2ComEscudo()
        {
            var fsm = new AbdulFSM(magiasPorCiclo: 1, intervaloDeConjuracao: 1f, duracaoExaustao: 2f);
            fsm.IniciarLuta();
            fsm.AtualizarFracaoDeVida(0.2f);
            fsm.Tick(1f); // 1 magia -> Exausto

            Assert.AreEqual(AbdulState.Exausto, fsm.CurrentState);

            fsm.Tick(2f); // exaustão acaba

            Assert.AreEqual(AbdulState.Fase2, fsm.CurrentState);
            Assert.IsTrue(fsm.EscudoAtivo, "Recuperada a mana, o escudo permanente volta.");
            Assert.AreEqual(0, fsm.MagiasNoCiclo, "O ciclo de mana reinicia.");
        }

        [Test]
        public void VidaZero_Derrota_EDisparaDropDoNecronomicon()
        {
            var fsm = new AbdulFSM();
            fsm.IniciarLuta();

            bool derrotou = false;
            fsm.OnDerrotado += () => derrotou = true;

            fsm.AtualizarFracaoDeVida(0f);

            Assert.AreEqual(AbdulState.Derrotado, fsm.CurrentState);
            Assert.IsTrue(derrotou, "A derrota dispara o evento que dropa o Necronomicon.");
            Assert.IsFalse(fsm.PodeReceberDano, "Já abatido não recebe mais dano.");
        }

        [Test]
        public void EmTranse_AtualizarVida_NaoFazNada()
        {
            var fsm = new AbdulFSM();
            fsm.AtualizarFracaoDeVida(0f); // sem IniciarLuta

            Assert.AreEqual(AbdulState.Transe, fsm.CurrentState,
                "Antes da luta começar, vida não é processada.");
        }

        [Test]
        public void OnEscudoMudou_DisparaNasTransicoesDeEscudo()
        {
            var fsm = new AbdulFSM(duracaoEscudoQuebrado: 2f, intervaloDeConjuracao: 99f);
            var eventos = new System.Collections.Generic.List<bool>();
            fsm.OnEscudoMudou += ativo => eventos.Add(ativo);

            fsm.IniciarLuta();          // escudo sobe (já começa true → sem evento)
            fsm.QuebrarPedraDePoder();  // cai  -> false
            fsm.Tick(2f);               // sobe -> true

            Assert.AreEqual(new[] { false, true }, eventos.ToArray());
        }

        [Test]
        public void IniciarLuta_DuasVezes_Ignorado()
        {
            var fsm = new AbdulFSM();
            fsm.IniciarLuta();
            fsm.AtualizarFracaoDeVida(0.2f); // Fase 2

            fsm.IniciarLuta(); // não deve resetar a luta

            Assert.AreEqual(AbdulState.Fase2, fsm.CurrentState);
        }
    }
}
