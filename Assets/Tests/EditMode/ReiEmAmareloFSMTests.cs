using System.Collections.Generic;
using NUnit.Framework;
using FavelaAmarela.Core.Enemies;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Trava as regras do confronto final: o ritual das relíquias (metade sem pressão) e o
    /// selamento (metade de reação pura, sem barra de vida — errar mata na hora).
    /// </summary>
    public class ReiEmAmareloFSMTests
    {
        private static readonly string[] TresReliquias = { "necronomicon", "patua_luas_gemeas", "anel_sinal_amarelo" };

        private static ReiEmAmareloFSM CriarEmRitual(int ciclos = 3, float janela = 1.5f, float intervalo = 6f)
        {
            var fsm = new ReiEmAmareloFSM(TresReliquias, ciclos, janela, intervalo);
            fsm.Iniciar();
            return fsm;
        }

        // ── Ritual das relíquias ─────────────────────────────────────────────

        [Test]
        public void Iniciar_VaiParaAtivandoReliquias()
        {
            var fsm = CriarEmRitual();
            Assert.AreEqual(ReiEmAmareloState.AtivandoReliquias, fsm.CurrentState);
        }

        [Test]
        public void AtivarReliquiaNaoExigida_NaoConta()
        {
            var fsm = CriarEmRitual();
            bool ativou = fsm.AtivarReliquia("coroa_de_ossos"); // fora da lista de 3

            Assert.IsFalse(ativou);
            Assert.AreEqual(0, fsm.ReliquiasAtivas);
        }

        [Test]
        public void AtivarMesmaReliquiaDuasVezes_SoContaUma()
        {
            var fsm = CriarEmRitual();
            fsm.AtivarReliquia("necronomicon");
            fsm.AtivarReliquia("necronomicon");

            Assert.AreEqual(1, fsm.ReliquiasAtivas);
        }

        [Test]
        public void AtivarTodasAsReliquias_IniciaOSelamento()
        {
            var fsm = CriarEmRitual();

            foreach (var id in TresReliquias) fsm.AtivarReliquia(id);

            Assert.IsTrue(fsm.TodasAsReliquiasAtivas);
            Assert.AreEqual(ReiEmAmareloState.Selando, fsm.CurrentState);
        }

        [Test]
        public void FaltandoUmaReliquia_NaoIniciaOSelamento()
        {
            var fsm = CriarEmRitual();

            fsm.AtivarReliquia(TresReliquias[0]);
            fsm.AtivarReliquia(TresReliquias[1]);

            Assert.AreEqual(ReiEmAmareloState.AtivandoReliquias, fsm.CurrentState);
        }

        [Test]
        public void AtivarReliquia_ComOSelamentoEmCurso_NaoTemEfeito()
        {
            var fsm = CriarEmRitual();
            foreach (var id in TresReliquias) fsm.AtivarReliquia(id);

            // Já está em Selando; reativar não deveria significar nada.
            bool efeito = fsm.AtivarReliquia(TresReliquias[0]);

            Assert.IsFalse(efeito);
        }

        // ── Selamento: janela de reação ──────────────────────────────────────

        private static ReiEmAmareloFSM CriarNoInicioDoSelamento(int ciclos = 3, float janela = 1.5f, float intervalo = 6f)
        {
            var fsm = CriarEmRitual(ciclos, janela, intervalo);
            foreach (var id in TresReliquias) fsm.AtivarReliquia(id);
            return fsm;
        }

        [Test]
        public void Selando_AposOIntervalo_AbreADesvelacao()
        {
            var fsm = CriarNoInicioDoSelamento(intervalo: 6f);

            fsm.Tick(6.1f, jogadorEstaDeCostas: false);

            Assert.AreEqual(ReiEmAmareloState.Desvelado, fsm.CurrentState);
        }

        [Test]
        public void Desvelado_DeCostasATempo_Sobrevive()
        {
            var fsm = CriarNoInicioDoSelamento(intervalo: 1f, janela: 1.5f);

            fsm.Tick(1.1f, jogadorEstaDeCostas: false); // abre a janela
            Assert.AreEqual(ReiEmAmareloState.Desvelado, fsm.CurrentState);

            fsm.Tick(0.5f, jogadorEstaDeCostas: true); // reage dentro do 1,5 s

            Assert.AreEqual(1, fsm.CiclosSobrevividos);
            Assert.AreNotEqual(ReiEmAmareloState.Colapso, fsm.CurrentState);
        }

        [Test]
        public void Desvelado_DeFrenteAteOFimDaJanela_Colapsa()
        {
            var fsm = CriarNoInicioDoSelamento(intervalo: 1f, janela: 1.5f);

            fsm.Tick(1.1f, jogadorEstaDeCostas: false); // abre a janela
            fsm.Tick(1.6f, jogadorEstaDeCostas: false); // nunca reage, janela estoura

            Assert.AreEqual(ReiEmAmareloState.Colapso, fsm.CurrentState);
        }

        [Test]
        public void Desvelado_ReagirNoUltimoInstante_AindaSalva()
        {
            // A janela é de reação, não de antecipação: reagir a 0,01s do fim ainda conta.
            var fsm = CriarNoInicioDoSelamento(intervalo: 1f, janela: 1.5f);

            fsm.Tick(1.1f, jogadorEstaDeCostas: false);
            fsm.Tick(1.49f, jogadorEstaDeCostas: false);
            fsm.Tick(0.005f, jogadorEstaDeCostas: true);

            Assert.AreEqual(1, fsm.CiclosSobrevividos);
        }

        [Test]
        public void SobreviverTodosOsCiclos_Sela()
        {
            var fsm = CriarNoInicioDoSelamento(ciclos: 2, intervalo: 1f, janela: 1.5f);

            for (int i = 0; i < 2; i++)
            {
                fsm.Tick(1.1f, jogadorEstaDeCostas: false); // abre
                fsm.Tick(0.1f, jogadorEstaDeCostas: true);  // sobrevive
            }

            Assert.AreEqual(ReiEmAmareloState.Selado, fsm.CurrentState);
            Assert.AreEqual(2, fsm.CiclosSobrevividos);
        }

        [Test]
        public void Colapso_DisparaUmaVezSo()
        {
            var fsm = CriarNoInicioDoSelamento(intervalo: 1f, janela: 1.5f);
            int vezes = 0;
            fsm.OnColapso += () => vezes++;

            fsm.Tick(1.1f, jogadorEstaDeCostas: false);
            fsm.Tick(1.6f, jogadorEstaDeCostas: false);
            fsm.Tick(5f, jogadorEstaDeCostas: false); // continua chamando Tick depois de morto

            Assert.AreEqual(1, vezes);
        }

        [Test]
        public void ListaDeReliquiasVazia_LancaExcecao()
        {
            Assert.Throws<System.ArgumentException>(() => new ReiEmAmareloFSM(new List<string>()));
        }
    }
}
