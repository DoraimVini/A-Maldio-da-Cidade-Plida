using System;
using System.Linq;
using NUnit.Framework;
using FavelaAmarela.Core.Progression;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Testes do POCO <see cref="Progressao"/> — o Labirinto de Carcosa sem Unity.
    ///
    /// <para>Antes da Fase 3 esta lógica vivia num <c>MonoBehaviour</c> e <b>não tinha teste
    /// nenhum</b>: era o maior risco não coberto da refatoração de managers.</para>
    /// </summary>
    public sealed class ProgressaoTests
    {
        /// <summary>A curva real do jogo, para os testes exercitarem os números de produção.</summary>
        private static int[] CurvaDoJogo => new[]
        {
            0, 100, 300, 600, 1000, 1500, 2100, 2800, 3600, 4500, 5500, 6600
        };

        private static Progressao Nova() => new Progressao(CurvaDoJogo);

        // ── Construção ───────────────────────────────────────────────────────

        [Test]
        public void Nasce_NoNivelUm_SemExposicaoESemPontos()
        {
            var p = Nova();

            Assert.AreEqual(1, p.NivelAtual);
            Assert.AreEqual(0, p.ExposicaoAtual);
            Assert.AreEqual(0, p.PontosDeEcoDisponiveis);
            Assert.IsEmpty(p.EcosDesbloqueados);
        }

        [Test]
        public void CurvaVazia_Lanca()
        {
            Assert.Throws<ArgumentException>(() => new Progressao(null));
            Assert.Throws<ArgumentException>(() => new Progressao(new int[0]));
        }

        [Test]
        public void CurvaEhCopiada_MutarOVetorDeFontesNaoAfetaAProgressao()
        {
            var curva = CurvaDoJogo;
            var p = new Progressao(curva);

            curva[1] = 999999; // se a curva fosse compartilhada, subir de nível quebraria

            p.AdicionarExposicao(100);
            Assert.AreEqual(2, p.NivelAtual, "A curva deve ser copiada na construção.");
        }

        // ── Exposição e níveis ───────────────────────────────────────────────

        [Test]
        public void Exposicao_AbaixoDoLimiar_NaoSobeDeNivel()
        {
            var p = Nova();
            p.AdicionarExposicao(99);

            Assert.AreEqual(1, p.NivelAtual);
            Assert.AreEqual(99, p.ExposicaoAtual);
            Assert.AreEqual(0, p.PontosDeEcoDisponiveis);
        }

        [Test]
        public void Exposicao_NoLimiar_SobeUmNivel_EConcedeUmPonto()
        {
            var p = Nova();
            p.AdicionarExposicao(100);

            Assert.AreEqual(2, p.NivelAtual);
            Assert.AreEqual(1, p.PontosDeEcoDisponiveis);
        }

        /// <summary>
        /// Um único ganho grande deve atravessar vários limiares de uma vez — matar um chefe não
        /// pode deixar níveis "presos" esperando o próximo ganho.
        /// </summary>
        [Test]
        public void Exposicao_GanhoGrande_SobeVariosNiveisDeUmaVez()
        {
            var p = Nova();
            p.AdicionarExposicao(1000); // limiar do nível 5

            Assert.AreEqual(5, p.NivelAtual);
            Assert.AreEqual(4, p.PontosDeEcoDisponiveis, "Um ponto por nível ganho.");
        }

        [Test]
        public void Exposicao_AcumulaAoLongoDeVariasChamadas()
        {
            var p = Nova();
            p.AdicionarExposicao(50);
            p.AdicionarExposicao(50);

            Assert.AreEqual(2, p.NivelAtual, "50 + 50 alcança o limiar de 100.");
        }

        [Test]
        public void CurvaCompleta_LevaAoTetoComOnzePontos()
        {
            var p = Nova();
            p.AdicionarExposicao(6600);

            Assert.AreEqual(12, p.NivelAtual);
            Assert.IsTrue(p.NoTeto);
            // 12 níveis, mas se começa no 1 — logo 11 ganhos.
            Assert.AreEqual(11, p.PontosDeEcoDisponiveis,
                "Uma run completa dá 11 Pontos de Eco. É o orçamento da árvore inteira.");
        }

        [Test]
        public void NoTeto_ExposicaoNovaEhIgnorada()
        {
            var p = Nova();
            p.AdicionarExposicao(6600);
            int exposicaoNoTeto = p.ExposicaoAtual;

            p.AdicionarExposicao(5000);

            Assert.AreEqual(exposicaoNoTeto, p.ExposicaoAtual,
                "No teto nem a Exposição é somada — comportamento herdado do ProgressionManager.");
            Assert.AreEqual(12, p.NivelAtual);
        }

        // ── Eventos ──────────────────────────────────────────────────────────

        [Test]
        public void Exposicao_DisparaOnExposicaoGanha_MesmoSemSubirDeNivel()
        {
            var p = Nova();
            int vezes = 0;
            p.OnExposicaoGanha += () => vezes++;

            p.AdicionarExposicao(10);

            Assert.AreEqual(1, vezes);
        }

        [Test]
        public void LevelUp_DisparaUmaVezPorNivel_ComONivelNovo()
        {
            var p = Nova();
            var niveis = new System.Collections.Generic.List<int>();
            p.OnLevelUp += n => niveis.Add(n);

            p.AdicionarExposicao(600); // até o nível 4

            CollectionAssert.AreEqual(new[] { 2, 3, 4 }, niveis);
        }

        [Test]
        public void NoTeto_NaoDisparaEventoAlgum()
        {
            var p = Nova();
            p.AdicionarExposicao(6600);

            bool disparou = false;
            p.OnExposicaoGanha += () => disparou = true;
            p.OnLevelUp += _ => disparou = true;

            p.AdicionarExposicao(100);

            Assert.IsFalse(disparou);
        }

        // ── Ecos ─────────────────────────────────────────────────────────────

        [Test]
        public void Eco_SemPontos_NaoDesbloqueia()
        {
            var p = Nova();

            bool ok = p.TryDesbloquearEco("eco-a", null, out string motivo);

            Assert.IsFalse(ok);
            StringAssert.Contains("Pontos de Eco", motivo);
            Assert.IsEmpty(p.EcosDesbloqueados);
        }

        [Test]
        public void Eco_ComPonto_DesbloqueiaEGastaOPonto()
        {
            var p = Nova();
            p.AdicionarExposicao(100); // 1 ponto

            bool ok = p.TryDesbloquearEco("eco-a", null, out string motivo);

            Assert.IsTrue(ok, motivo);
            Assert.AreEqual(0, p.PontosDeEcoDisponiveis);
            Assert.IsTrue(p.Possui("eco-a"));
        }

        [Test]
        public void Eco_JaDesbloqueado_NaoGastaPontoDeNovo()
        {
            var p = Nova();
            p.AdicionarExposicao(300); // 2 pontos
            p.TryDesbloquearEco("eco-a", null, out _);

            bool ok = p.TryDesbloquearEco("eco-a", null, out string motivo);

            Assert.IsFalse(ok);
            StringAssert.Contains("já desbloqueado", motivo);
            Assert.AreEqual(1, p.PontosDeEcoDisponiveis, "O ponto não pode ser consumido em vão.");
        }

        [Test]
        public void Eco_ComPreRequisitoNaoAtendido_NaoDesbloqueia()
        {
            var p = Nova();
            p.AdicionarExposicao(100);

            bool ok = p.TryDesbloquearEco("eco-b", new[] { "eco-a" }, out string motivo);

            Assert.IsFalse(ok);
            StringAssert.Contains("Pré-requisitos", motivo);
            Assert.AreEqual(1, p.PontosDeEcoDisponiveis);
        }

        [Test]
        public void Eco_ComPreRequisitoAtendido_Desbloqueia()
        {
            var p = Nova();
            p.AdicionarExposicao(300); // 2 pontos
            p.TryDesbloquearEco("eco-a", null, out _);

            bool ok = p.TryDesbloquearEco("eco-b", new[] { "eco-a" }, out string motivo);

            Assert.IsTrue(ok, motivo);
        }

        /// <summary>
        /// Pré-requisito é <b>OU</b>, não E. A árvore tem nós-Ponte que ligam braços diferentes;
        /// exigir todos os pré-requisitos os tornaria inalcançáveis.
        /// </summary>
        [Test]
        public void Eco_PreRequisitoEhOu_BastaUmDosListados()
        {
            var p = Nova();
            p.AdicionarExposicao(300);
            p.TryDesbloquearEco("sobrevivente-3", null, out _);

            bool ok = p.TryDesbloquearEco("ponte",
                new[] { "sobrevivente-3", "ocultista-3" }, out string motivo);

            Assert.IsTrue(ok, motivo);
        }

        [Test]
        public void Eco_SemId_NaoDesbloqueia()
        {
            var p = Nova();
            p.AdicionarExposicao(100);

            Assert.IsFalse(p.TryDesbloquearEco(null, null, out _));
            Assert.IsFalse(p.TryDesbloquearEco("", null, out _));
            Assert.AreEqual(1, p.PontosDeEcoDisponiveis);
        }

        [Test]
        public void Eco_DisparaOnEcoDesbloqueado_ComOId()
        {
            var p = Nova();
            p.AdicionarExposicao(100);

            string recebido = null;
            p.OnEcoDesbloqueado += id => recebido = id;

            p.TryDesbloquearEco("eco-a", null, out _);

            Assert.AreEqual("eco-a", recebido);
        }

        // ── Restauração ──────────────────────────────────────────────────────

        [Test]
        public void Restaurar_ReponhaEstadoCompleto()
        {
            var p = Nova();
            p.Restaurar(7, 2500, 3, new[] { "eco-a", "eco-b" });

            Assert.AreEqual(7, p.NivelAtual);
            Assert.AreEqual(2500, p.ExposicaoAtual);
            Assert.AreEqual(3, p.PontosDeEcoDisponiveis);
            Assert.AreEqual(2, p.EcosDesbloqueados.Count);
            Assert.IsTrue(p.Possui("eco-a"));
        }

        /// <summary>
        /// Restaurar não é progredir. Disparar <c>OnLevelUp</c> ao carregar um save faria a UI de
        /// subida de nível piscar a cada troca de cena.
        /// </summary>
        [Test]
        public void Restaurar_NaoDisparaEventos()
        {
            var p = Nova();
            bool disparou = false;
            p.OnLevelUp += _ => disparou = true;
            p.OnExposicaoGanha += () => disparou = true;
            p.OnEcoDesbloqueado += _ => disparou = true;

            p.Restaurar(9, 3600, 5, new[] { "eco-a" });

            Assert.IsFalse(disparou);
        }

        [Test]
        public void Restaurar_SubstituiOsEcosAnteriores_NaoAcumula()
        {
            var p = Nova();
            p.AdicionarExposicao(300);
            p.TryDesbloquearEco("antigo", null, out _);

            p.Restaurar(3, 300, 1, new[] { "novo" });

            Assert.IsFalse(p.Possui("antigo"), "Restaurar deve substituir, não somar.");
            Assert.IsTrue(p.Possui("novo"));
        }

        [Test]
        public void Restaurar_ValoresInvalidos_SaoClampados()
        {
            var p = Nova();
            p.Restaurar(-5, -100, -3, null);

            Assert.AreEqual(1, p.NivelAtual, "Nível nunca abaixo de 1.");
            Assert.AreEqual(0, p.ExposicaoAtual);
            Assert.AreEqual(0, p.PontosDeEcoDisponiveis);
            Assert.IsEmpty(p.EcosDesbloqueados);
        }

        [Test]
        public void Restaurar_IgnoraIdsVaziosOuNulos()
        {
            var p = Nova();
            p.Restaurar(2, 100, 0, new[] { "eco-a", null, "", "eco-b" });

            Assert.AreEqual(2, p.EcosDesbloqueados.Count);
        }

        // ── Ciclo completo ───────────────────────────────────────────────────

        /// <summary>
        /// O ciclo que o jogador vive: ganhar Exposição explorando, subir de nível, gastar o
        /// ponto num Santuário, e o progresso sobreviver ao save.
        /// </summary>
        [Test]
        public void CicloCompleto_GanharGastarSalvarRestaurar()
        {
            var p = Nova();

            p.AdicionarExposicao(600);
            Assert.AreEqual(4, p.NivelAtual);
            Assert.AreEqual(3, p.PontosDeEcoDisponiveis);

            Assert.IsTrue(p.TryDesbloquearEco("sobrevivente-1", null, out _));
            Assert.IsTrue(p.TryDesbloquearEco("sobrevivente-2",
                new[] { "sobrevivente-1" }, out _));
            Assert.AreEqual(1, p.PontosDeEcoDisponiveis);

            // Save → restauração numa instância nova (simula recarregar o jogo).
            var ids = p.EcosDesbloqueados.ToArray();
            var depoisDoLoad = Nova();
            depoisDoLoad.Restaurar(p.NivelAtual, p.ExposicaoAtual, p.PontosDeEcoDisponiveis, ids);

            Assert.AreEqual(4, depoisDoLoad.NivelAtual);
            Assert.AreEqual(1, depoisDoLoad.PontosDeEcoDisponiveis);
            Assert.IsTrue(depoisDoLoad.Possui("sobrevivente-2"));

            // E continua progredindo a partir do estado restaurado.
            depoisDoLoad.AdicionarExposicao(400); // 600 + 400 = 1000, limiar do nível 5
            Assert.AreEqual(5, depoisDoLoad.NivelAtual);
            Assert.AreEqual(2, depoisDoLoad.PontosDeEcoDisponiveis);
        }
    }
}
