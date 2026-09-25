using NUnit.Framework;
using FavelaAmarela.Core.Quests;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Testes da quest "A Canção Incompleta" (Cassilda). Cobrem principalmente as regras
    /// que impedem o jogador de ganhar o Patuá cedo demais ou de perder progresso.
    /// </summary>
    public sealed class CancaoIncompletaTests
    {
        [Test]
        public void QuestNova_ComecaNaoIniciada()
        {
            var q = new CancaoIncompleta();

            Assert.AreEqual(EstadoDaQuest.NaoIniciada, q.Estado);
            Assert.AreEqual(0, q.Entregues);
            Assert.AreEqual(3, q.Total, "escopo reduzido para 3 fragmentos");
            Assert.AreEqual(3, q.Restantes);
        }

        [Test]
        public void Iniciar_ColocaEmAndamento()
        {
            var q = new CancaoIncompleta();
            q.Iniciar();

            Assert.AreEqual(EstadoDaQuest.EmAndamento, q.Estado);
        }

        [Test]
        public void Iniciar_DuasVezes_NaoReinicia()
        {
            var q = new CancaoIncompleta();
            q.Iniciar();
            q.Entregar(0);
            q.Iniciar();   // falar com Cassilda de novo

            Assert.AreEqual(1, q.Entregues, "conversar de novo não pode zerar o progresso");
        }

        [Test]
        public void Entregar_ContaEAvisa()
        {
            var q = new CancaoIncompleta();
            int avisado = -1;
            q.OnFragmentoEntregue += i => avisado = i;

            Assert.IsTrue(q.Entregar(1));
            Assert.AreEqual(1, q.Entregues);
            Assert.AreEqual(1, avisado);
            Assert.IsTrue(q.FoiEntregue(1));
            Assert.IsFalse(q.FoiEntregue(0));
        }

        [Test]
        public void Entregar_OMesmoDuasVezes_NaoContaDeNovo()
        {
            // Sem isto, um duplo-clique ou bug de UI inflaria o progresso e daria o Patuá cedo.
            var q = new CancaoIncompleta();

            Assert.IsTrue(q.Entregar(0));
            Assert.IsFalse(q.Entregar(0));
            Assert.AreEqual(1, q.Entregues);
        }

        [Test]
        public void Entregar_IniciaAQuestImplicitamente()
        {
            // O jogador pode achar um fragmento antes de falar com a rainha.
            var q = new CancaoIncompleta();
            q.Entregar(0);

            Assert.AreEqual(EstadoDaQuest.EmAndamento, q.Estado);
        }

        [Test]
        public void Entregar_IndiceForaDaFaixa_NaoQuebra()
        {
            var q = new CancaoIncompleta();

            Assert.IsFalse(q.Entregar(-1));
            Assert.IsFalse(q.Entregar(99));
            Assert.AreEqual(0, q.Entregues);
            Assert.IsFalse(q.FoiEntregue(99));
        }

        [Test]
        public void Concluir_ExigeTodosOsFragmentos()
        {
            var q = new CancaoIncompleta();
            q.Entregar(0);
            q.Entregar(1);

            Assert.IsFalse(q.Concluir(), "a rainha não adianta a recompensa");
            Assert.AreEqual(EstadoDaQuest.EmAndamento, q.Estado);
        }

        [Test]
        public void Concluir_ComTodos_FechaEAvisa()
        {
            var q = new CancaoIncompleta();
            bool concluiu = false;
            q.OnConcluida += () => concluiu = true;

            q.Entregar(0); q.Entregar(1); q.Entregar(2);

            Assert.IsTrue(q.TodosEntregues);
            Assert.IsTrue(q.Concluir());
            Assert.AreEqual(EstadoDaQuest.Concluida, q.Estado);
            Assert.IsTrue(concluiu);
        }

        [Test]
        public void Concluir_DuasVezes_NaoDaOPatuaDeNovo()
        {
            var q = new CancaoIncompleta();
            q.Entregar(0); q.Entregar(1); q.Entregar(2);
            q.Concluir();

            int vezes = 0;
            q.OnConcluida += () => vezes++;

            Assert.IsFalse(q.Concluir());
            Assert.AreEqual(0, vezes);
        }

        [Test]
        public void Entregar_DepoisDeConcluida_NaoConta()
        {
            var q = new CancaoIncompleta(2);
            q.Entregar(0); q.Entregar(1);
            q.Concluir();

            Assert.IsFalse(q.Entregar(0));
            Assert.AreEqual(2, q.Entregues);
        }

        [Test]
        public void Restantes_AcompanhaAsEntregas()
        {
            var q = new CancaoIncompleta();

            Assert.AreEqual(3, q.Restantes);
            q.Entregar(0);
            Assert.AreEqual(2, q.Restantes);
            q.Entregar(1);
            q.Entregar(2);
            Assert.AreEqual(0, q.Restantes);
        }

        [Test]
        public void TotalInvalido_ViraUm()
        {
            Assert.AreEqual(1, new CancaoIncompleta(0).Total);
            Assert.AreEqual(1, new CancaoIncompleta(-5).Total);
        }

        // ── Recital (estrofes finais) ────────────────────────────────────────

        [Test]
        public void SemEstrofesAutoradas_TodosEntregues_FicaEmAndamentoAteConcluirExplicito()
        {
            // Compatibilidade com o comportamento antigo: sem estrofes no construtor, o
            // recital nasce completo, mas Entregar() nunca fechou a quest sozinho — isso
            // sempre foi um passo à parte (Concluir(), chamado pelo CassildaNPC).
            var q = new CancaoIncompleta();
            q.Entregar(0); q.Entregar(1); q.Entregar(2);

            Assert.AreEqual(EstadoDaQuest.EmAndamento, q.Estado);
            Assert.IsTrue(q.Recital.Completo);
            Assert.IsTrue(q.Concluir());
            Assert.AreEqual(EstadoDaQuest.Concluida, q.Estado);
        }

        [Test]
        public void ComEstrofesAutoradas_TodosEntregues_AbreRecitalSemConcluir()
        {
            const int certaDaTerceira = 1;
            const int certaDaQuarta = 2;
            var q = new CancaoIncompleta(3, certaDaTerceira, certaDaQuarta);
            q.Entregar(0); q.Entregar(1); q.Entregar(2);

            Assert.AreEqual(EstadoDaQuest.Recitando, q.Estado,
                "ter todos os fragmentos não é ter a canção completa");
            Assert.IsFalse(q.Concluir(), "a rainha não dá o Patuá enquanto faltar estrofe");
        }

        [Test]
        public void Responder_ForaDoRecitando_NaoConta()
        {
            const int certaDaTerceira = 1;
            var q = new CancaoIncompleta(3, certaDaTerceira, 2);

            // Ainda em andamento (nem todos os fragmentos entregues): não há o que responder.
            q.Entregar(0);
            Assert.IsFalse(q.Responder(certaDaTerceira));
        }

        [Test]
        public void Responder_TodasCertas_LiberaConcluir()
        {
            const int certaDaTerceira = 1;
            const int certaDaQuarta = 2;
            var q = new CancaoIncompleta(3, certaDaTerceira, certaDaQuarta);
            q.Entregar(0); q.Entregar(1); q.Entregar(2);

            Assert.IsTrue(q.Responder(certaDaTerceira));
            Assert.IsTrue(q.Responder(certaDaQuarta));
            Assert.IsTrue(q.Recital.Completo);

            bool concluiu = false;
            q.OnConcluida += () => concluiu = true;

            Assert.IsTrue(q.Concluir());
            Assert.AreEqual(EstadoDaQuest.Concluida, q.Estado);
            Assert.IsTrue(concluiu);
        }

        [Test]
        public void Responder_Errado_NaoAvancaEstadoNemBloqueiaRetry()
        {
            const int certaDaTerceira = 1;
            var q = new CancaoIncompleta(3, certaDaTerceira, 2);
            q.Entregar(0); q.Entregar(1); q.Entregar(2);

            Assert.IsFalse(q.Responder(certaDaTerceira + 1));
            Assert.AreEqual(EstadoDaQuest.Recitando, q.Estado, "errar não empurra pra fora do recital");
            Assert.AreEqual(0, q.Recital.EstrofeAtual, "e não avança a estrofe");

            // Retry livre: sem custo, a resposta certa logo depois funciona.
            Assert.IsTrue(q.Responder(certaDaTerceira));
        }
    }
}
