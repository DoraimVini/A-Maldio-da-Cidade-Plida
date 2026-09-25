using NUnit.Framework;
using FavelaAmarela.Core.Combat;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Suite EditMode do <see cref="Sangramento"/> (acúmulos + estouro) e da conta do
    /// estouro (<see cref="ExplosaoDeSangramento"/>) — a Ferida de Aklo do Estilete de
    /// Irem. POCO puro, sem cena.
    /// </summary>
    public class SangramentoTests
    {
        // ── Estado inicial ───────────────────────────────────────────────────

        [Test]
        public void NasceSemAcumulos()
        {
            var s = new Sangramento();
            Assert.IsFalse(s.Ativo);
            Assert.AreEqual(0, s.Acumulos);
            Assert.AreEqual(0f, s.DanoPorSegundo);
        }

        [Test]
        public void TickSemFerida_NaoCausaDanoNemExplode()
        {
            var s = new Sangramento();
            var tick = s.Tick(1f);
            Assert.AreEqual(0f, tick.DanoContinuo, 0.0001f);
            Assert.IsFalse(tick.Explodiu);
        }

        // ── Acúmulo ──────────────────────────────────────────────────────────

        [Test]
        public void Aplicar_SomaAcumulos()
        {
            var s = new Sangramento();
            s.Aplicar(1, 4f, 5f);
            s.Aplicar(3, 4f, 5f);
            Assert.AreEqual(4, s.Acumulos);
        }

        [Test]
        public void DanoPorSegundo_EscalaComOsAcumulos()
        {
            var s = new Sangramento();
            s.Aplicar(3, 4f, 5f);
            Assert.AreEqual(12f, s.DanoPorSegundo, 0.0001f, "3 acúmulos × 4/s = 12/s.");

            // 1s a 12/s = 12 de dano contínuo.
            Assert.AreEqual(12f, s.Tick(1f).DanoContinuo, 0.0001f);
        }

        [Test]
        public void Acumulos_NaoPassamDoTeto()
        {
            var s = new Sangramento(limiteDeAcumulos: 10);
            s.Aplicar(50, 4f, 5f);
            Assert.AreEqual(10, s.Acumulos);
        }

        [Test]
        public void Aplicar_RenovaADuracao()
        {
            var s = new Sangramento();
            s.Aplicar(1, 4f, 5f);
            s.Tick(4f);              // sobra 1s
            s.Aplicar(1, 4f, 5f);    // renova
            Assert.AreEqual(5f, s.TempoRestante, 0.0001f);
        }

        [Test]
        public void Aplicar_MaisFraco_NaoRebaixaAFeridaGrave()
        {
            var s = new Sangramento();
            s.Aplicar(1, 20f, 6f);
            s.Aplicar(1, 4f, 1f);
            Assert.AreEqual(40f, s.DanoPorSegundo, 0.0001f, "2 acúmulos × 20/s (o mais forte).");
        }

        [Test]
        public void Aplicar_ValoresInvalidos_Ignorados()
        {
            var s = new Sangramento();
            s.Aplicar(0, 4f, 5f);
            s.Aplicar(-2, 4f, 5f);
            s.Aplicar(1, 0f, 5f);
            s.Aplicar(1, 4f, 0f);
            Assert.AreEqual(0, s.Acumulos);
        }

        // ── Estouro ──────────────────────────────────────────────────────────

        [Test]
        public void AoAtingirOTeto_Explode_EZeraOsAcumulos()
        {
            var s = new Sangramento(limiteDeAcumulos: 10);
            s.Aplicar(10, 4f, 5f);

            var tick = s.Tick(0.1f);

            Assert.IsTrue(tick.Explodiu);
            Assert.AreEqual(0, s.Acumulos, "O estouro zera a contagem.");
            Assert.IsFalse(s.Ativo);
        }

        [Test]
        public void AbaixoDoTeto_NaoExplode()
        {
            var s = new Sangramento(limiteDeAcumulos: 10);
            s.Aplicar(9, 4f, 5f);
            Assert.IsFalse(s.Tick(0.1f).Explodiu);
        }

        [Test]
        public void Explode_UmaVezSo()
        {
            var s = new Sangramento(limiteDeAcumulos: 10);
            int explosoes = 0;
            s.OnExplodiu += () => explosoes++;

            s.Aplicar(10, 4f, 5f);
            s.Tick(0.1f);
            s.Tick(0.1f);
            s.Tick(0.1f);

            Assert.AreEqual(1, explosoes);
        }

        [Test]
        public void EstouroTemPrioridadeSobreAExpiracao()
        {
            // Chegou ao teto no mesmo tick em que a duração acabaria: deve estourar,
            // não estancar em silêncio.
            var s = new Sangramento(limiteDeAcumulos: 10);
            s.Aplicar(10, 4f, 0.1f);
            Assert.IsTrue(s.Tick(5f).Explodiu);
        }

        // ── Expiração ────────────────────────────────────────────────────────

        [Test]
        public void SemNovosGolpes_AFeridaEstanca()
        {
            var s = new Sangramento();
            s.Aplicar(2, 4f, 3f);

            s.Tick(5f);

            Assert.IsFalse(s.Ativo, "Parar de bater deixa a ferida estancar.");
            Assert.AreEqual(0, s.Acumulos);
        }

        [Test]
        public void UltimoTick_NaoEntregaDanoAlemDaDuracao()
        {
            var s = new Sangramento();
            s.Aplicar(1, 8f, 0.25f);   // 8/s por 0,25s = 2 de dano no total
            Assert.AreEqual(2f, s.Tick(1f).DanoContinuo, 0.0001f);
        }

        [Test]
        public void Limpar_EstancaTudo()
        {
            var s = new Sangramento();
            int terminou = 0;
            s.OnTerminou += () => terminou++;

            s.Aplicar(5, 4f, 5f);
            s.Limpar();

            Assert.IsFalse(s.Ativo);
            Assert.AreEqual(0, s.Acumulos);
            Assert.AreEqual(1, terminou);
        }

        [Test]
        public void Limpar_SemFerida_NaoDisparaEvento()
        {
            var s = new Sangramento();
            int terminou = 0;
            s.OnTerminou += () => terminou++;
            s.Limpar();
            Assert.AreEqual(0, terminou);
        }

        [Test]
        public void OnAcumulosMudaram_ReportaAContagem()
        {
            var s = new Sangramento();
            int ultimo = -1;
            s.OnAcumulosMudaram += n => ultimo = n;

            s.Aplicar(3, 4f, 5f);
            Assert.AreEqual(3, ultimo);

            s.Limpar();
            Assert.AreEqual(0, ultimo);
        }
    }

    /// <summary>
    /// Suite EditMode da conta do estouro — percentual contra boss, fixo contra comuns.
    /// </summary>
    public class ExplosaoDeSangramentoTests
    {
        [Test]
        public void ContraBoss_UsaPercentualDaVidaMaxima()
        {
            // 10% de 300 = 30 (abaixo do teto de 60).
            Assert.AreEqual(30f,
                ExplosaoDeSangramento.Calcular(300f, ehAparicaoPrimordial: true), 0.0001f);
        }

        [Test]
        public void ContraBoss_RespeitaOTeto()
        {
            // 10% de 2000 seria 200 — o teto impede o "delete boss".
            Assert.AreEqual(ExplosaoDeSangramento.TetoDeDano,
                ExplosaoDeSangramento.Calcular(2000f, ehAparicaoPrimordial: true), 0.0001f);
        }

        [Test]
        public void ContraInimigoComum_UsaDanoFixo()
        {
            // 10% de um Cultista (100) seriam 10 — irrelevante. O fixo mantém o efeito útil.
            Assert.AreEqual(ExplosaoDeSangramento.DanoContraComuns,
                ExplosaoDeSangramento.Calcular(100f, ehAparicaoPrimordial: false), 0.0001f);
        }

        [Test]
        public void ContraInimigoComum_IgnoraAVidaMaxima()
        {
            Assert.AreEqual(
                ExplosaoDeSangramento.Calcular(50f, false),
                ExplosaoDeSangramento.Calcular(5000f, false), 0.0001f);
        }

        [Test]
        public void VidaMaximaInvalida_NaoCausaDanoNoBoss()
        {
            Assert.AreEqual(0f, ExplosaoDeSangramento.Calcular(0f, true), 0.0001f);
            Assert.AreEqual(0f, ExplosaoDeSangramento.Calcular(-10f, true), 0.0001f);
        }
    }
}
