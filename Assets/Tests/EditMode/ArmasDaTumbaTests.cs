using NUnit.Framework;
using FavelaAmarela.Core.Abilities;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Suite EditMode das 3 armas da Tumba de Alhazred (Cravo de Aklo, Estilete de
    /// Irem, Alfanje de Alhazred). POCO puro — instancia direto, sem cena.
    ///
    /// Cobre: ataque básico aplica dano; cada habilidade tem seu efeito-assinatura
    /// (Cravo interrompe, Estilete sangra, Alfanje repele + atordoa); cooldown do
    /// básico e da habilidade são validados e independentes.
    /// </summary>
    [TestFixture]
    public class ArmasDaTumbaTests
    {
        // ── Ataque básico ────────────────────────────────────────────────────

        [Test]
        public void Basico_DasTresArmas_AplicaDanoESucesso()
        {
            foreach (IArmaComHabilidade arma in new IArmaComHabilidade[]
                     { new CravoDeAklo(), new EstileteDeIrem(), new AlfanjeDeAlhazred() })
            {
                var r = arma.Execute();
                Assert.IsTrue(r.Success, $"{arma.NomeDaArma}: básico deveria ter sucesso.");
                Assert.Greater(r.Dano, 0f, $"{arma.NomeDaArma}: básico deveria causar dano.");
            }
        }

        [Test]
        public void Basico_NaoCarregaEfeitosDeOutrasArmas()
        {
            // O básico do Estilete NÃO interrompe conjuração (Cravo) nem repele (Alfanje).
            var r = new EstileteDeIrem().Execute();
            Assert.IsFalse(r.InterrompeConjuracao);
            Assert.AreEqual(0f, r.ForcaRepulsao, 0.0001f);
        }

        [Test]
        public void EstileteBasico_AcumulaSangramento()
        {
            // Decisão de design (2026-07-31): o ataque básico do Estilete acumula 1 de
            // sangramento. É o que torna o teto de 10 acúmulos alcançável — a habilidade
            // sozinha (cooldown 5 s) levaria quase um minuto para chegar lá.
            var r = new EstileteDeIrem().Execute();
            Assert.AreEqual(1, r.AcumulosDeSangramento);
            Assert.Greater(r.SangramentoPorSegundo, 0f);
        }

        [Test]
        public void OutrasArmas_NaoAcumulamSangramento()
        {
            // Sangramento é a assinatura do Estilete — Cravo e Alfanje não o aplicam.
            Assert.AreEqual(0, new CravoDeAklo().Execute().AcumulosDeSangramento);
            Assert.AreEqual(0, new CravoDeAklo().ExecuteHabilidade().AcumulosDeSangramento);
            Assert.AreEqual(0, new AlfanjeDeAlhazred().Execute().AcumulosDeSangramento);
            Assert.AreEqual(0, new AlfanjeDeAlhazred().ExecuteHabilidade().AcumulosDeSangramento);
        }

        [Test]
        public void EstileteHabilidade_AcumulaMaisQueOBasico()
        {
            var arma = new EstileteDeIrem();
            Assert.Greater(arma.ExecuteHabilidade().AcumulosDeSangramento,
                           arma.Execute().AcumulosDeSangramento,
                           "A habilidade é o empurrão guardado para a janela de dano.");
        }

        // ── Habilidades-assinatura ───────────────────────────────────────────

        [Test]
        public void CravoDeAklo_Habilidade_InterrompeConjuracao()
        {
            var r = new CravoDeAklo().ExecuteHabilidade();
            Assert.IsTrue(r.Success);
            Assert.IsTrue(r.InterrompeConjuracao, "Fincar o Aklo deve interromper a conjuração.");
            Assert.Greater(r.Dano, 0f);
        }

        [Test]
        public void EstileteDeIrem_Habilidade_AplicaSangramento()
        {
            var r = new EstileteDeIrem().ExecuteHabilidade();
            Assert.IsTrue(r.Success);
            Assert.Greater(r.SangramentoPorSegundo, 0f, "Ferida de Aklo deve sangrar por segundo.");
            Assert.Greater(r.DuracaoSangramento, 0f, "Sangramento deve ter duração.");
            Assert.IsFalse(r.InterrompeConjuracao);
        }

        [Test]
        public void AlfanjeDeAlhazred_Habilidade_RepeleEAtordoa()
        {
            var r = new AlfanjeDeAlhazred().ExecuteHabilidade();
            Assert.IsTrue(r.Success);
            Assert.Greater(r.ForcaRepulsao, 0f, "Golpe do Deserto deve repelir.");
            Assert.IsTrue(r.Atordoou, "Golpe do Deserto deve atordoar brevemente.");
            Assert.Greater(r.DuracaoAtordoamento, 0f);
        }

        // ── Mão Vazia (golpe desarmado) ──────────────────────────────────────

        [Test]
        public void MaoVazia_Basico_TemSucessoMasDanoZero()
        {
            var r = new MaoVazia().Execute();
            Assert.IsTrue(r.Success, "O gesto desarmado deve executar (entra no estado Atacando).");
            Assert.AreEqual(0f, r.Dano, 0.0001f,
                "Mão Vazia causa dano ZERO por decisão de design — bater de mão vazia não mata.");
        }

        [Test]
        public void MaoVazia_NaoAtordoaNemRepeleNemInterrompe()
        {
            var r = new MaoVazia().Execute();
            Assert.IsFalse(r.Atordoou);
            Assert.IsFalse(r.InterrompeConjuracao);
            Assert.AreEqual(0f, r.ForcaRepulsao, 0.0001f);
            Assert.AreEqual(0f, r.SangramentoPorSegundo, 0.0001f);
        }

        [Test]
        public void MaoVazia_RespeitaCooldownProprio()
        {
            var mao = new MaoVazia(cooldown: 0.25f);
            Assert.IsFalse(mao.CanActivate(0.24f));
            Assert.IsTrue(mao.CanActivate(0.25f));
        }

        [Test]
        public void MaoVazia_NaoTemHabilidade()
        {
            // Sem arma não há habilidade em botão separado — contrato explícito.
            Assert.IsFalse(new MaoVazia() is IArmaComHabilidade,
                "MaoVazia não deve implementar IArmaComHabilidade.");
        }

        // ── Cooldowns ────────────────────────────────────────────────────────

        [Test]
        public void CooldownBasico_BloqueiaAntesELiberaDepois()
        {
            var arma = new CravoDeAklo(cooldownBasico: 0.5f);
            Assert.IsFalse(arma.CanActivate(0.49f), "Não deveria liberar antes do cooldown.");
            Assert.IsTrue(arma.CanActivate(0.5f), "Deveria liberar exatamente no cooldown.");
        }

        [Test]
        public void CooldownHabilidade_EIndependenteDoBasico()
        {
            // Básico com cooldown curto, habilidade com cooldown longo — validações separadas.
            var arma = new CravoDeAklo(cooldownBasico: 0.5f, cooldownHabilidade: 6f);

            // Passou tempo suficiente pro básico, mas não pra habilidade.
            Assert.IsTrue(arma.CanActivate(1f));
            Assert.IsFalse(arma.CanActivateHabilidade(1f), "Habilidade não deve liberar com o cooldown do básico.");
            Assert.IsTrue(arma.CanActivateHabilidade(6f), "Habilidade deve liberar no próprio cooldown.");
        }
    }
}
