using System;
using System.Collections.Generic;
using FavelaAmarela.Core.Combat;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Suite NUnit (EditMode) para <see cref="ResilienciaMental"/>.
    /// Roda sem cena, sem Play Mode, sem qualquer API de Unity — POCO puro.
    ///
    /// Cobertura:
    ///   A. Construção e validação de argumentos
    ///   B. Estado inicial
    ///   C. SofrerTrauma (dano / descida)
    ///   D. Ancorar (cura / subida)
    ///   E. Helpers narrativos (EstabilizarCompletamente, ForcarColapso)
    ///   F. Transições de estado (Normal ↔ Pânico ↔ Colapso)
    ///   G. Evento OnChanged — payload e supressão
    ///   H. Factory ComThresholdFracional
    /// </summary>
    [TestFixture]
    public class ResilienciaMentalTests
    {
        // Fixture padrão reutilizado em todos os testes:
        //   Max = 100, ThresholdPanico = 25 (25%)
        private ResilienciaMental _rm;

        [SetUp]
        public void SetUp() => _rm = new ResilienciaMental(max: 100f, thresholdPanico: 25f);

        // ════════════════════════════════════════════════════════════════════
        // A. Construção e validação de argumentos
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void Construtor_MaxZero_DeveLancarArgumentOutOfRange()
            => Assert.Throws<ArgumentOutOfRangeException>(() => new ResilienciaMental(0f, 10f));

        [Test]
        public void Construtor_MaxNegativo_DeveLancarArgumentOutOfRange()
            => Assert.Throws<ArgumentOutOfRangeException>(() => new ResilienciaMental(-1f, 10f));

        [Test]
        public void Construtor_ThresholdNegativo_DeveLancarArgumentOutOfRange()
            => Assert.Throws<ArgumentOutOfRangeException>(() => new ResilienciaMental(100f, -1f));

        [Test]
        public void Construtor_ThresholdIgualAoMax_DeveLancarArgumentOutOfRange()
            => Assert.Throws<ArgumentOutOfRangeException>(() => new ResilienciaMental(100f, 100f));

        [Test]
        public void Construtor_ThresholdMaiorQueMax_DeveLancarArgumentOutOfRange()
            => Assert.Throws<ArgumentOutOfRangeException>(() => new ResilienciaMental(100f, 150f));

        [Test]
        public void Construtor_ThresholdZero_DeveSerValido()
        {
            // Threshold = 0 significa que só entra em Pânico chegando ao colapso —
            // design válido para chefes ou Damião com equipamento lendário.
            Assert.DoesNotThrow(() => new ResilienciaMental(100f, 0f));
        }

        // ════════════════════════════════════════════════════════════════════
        // B. Estado inicial
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void EstadoInicial_AtualDeveSerIgualAoMax()
            => Assert.AreEqual(100f, _rm.Atual, 1e-5f);

        [Test]
        public void EstadoInicial_PercentualDeveSer1()
            => Assert.AreEqual(1f, _rm.Percentual, 1e-5f);

        [Test]
        public void EstadoInicial_NaoDevEstarEmPanico()
            => Assert.IsFalse(_rm.IsPanico);

        [Test]
        public void EstadoInicial_NaoDeveEstarEmColapso()
            => Assert.IsFalse(_rm.IsColapso);

        // ════════════════════════════════════════════════════════════════════
        // C. SofrerTrauma
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void SofrerTrauma_ValorNegativo_DeveLancarArgumentOutOfRange()
            => Assert.Throws<ArgumentOutOfRangeException>(() => _rm.SofrerTrauma(-10f));

        [Test]
        public void SofrerTrauma_ValorNormal_DeveReduzirAtual()
        {
            _rm.SofrerTrauma(30f);
            Assert.AreEqual(70f, _rm.Atual, 1e-5f);
        }

        [Test]
        public void SofrerTrauma_AlemDoLimite_DeveClampearEmZero()
        {
            _rm.SofrerTrauma(999f);
            Assert.AreEqual(0f, _rm.Atual, 1e-5f);
        }

        [Test]
        public void SofrerTrauma_ComAtualEmZero_NaoDeveDispararEvento()
        {
            _rm.ForcarColapso();
            int contagem = 0;
            _rm.OnChanged += _ => contagem++;
            _rm.SofrerTrauma(10f); // sem efeito real
            Assert.AreEqual(0, contagem);
        }

        [Test]
        public void SofrerTrauma_Zero_NaoDeveDispararEvento()
        {
            int contagem = 0;
            _rm.OnChanged += _ => contagem++;
            _rm.SofrerTrauma(0f);
            Assert.AreEqual(0, contagem);
        }

        // ════════════════════════════════════════════════════════════════════
        // D. Ancorar
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void Ancorar_ValorNegativo_DeveLancarArgumentOutOfRange()
            => Assert.Throws<ArgumentOutOfRangeException>(() => _rm.Ancorar(-5f));

        [Test]
        public void Ancorar_ValorNormal_DeveAumentarAtual()
        {
            _rm.SofrerTrauma(50f);
            _rm.Ancorar(20f);
            Assert.AreEqual(70f, _rm.Atual, 1e-5f);
        }

        [Test]
        public void Ancorar_AlemDoMaximo_DeveClampearNoMax()
        {
            _rm.SofrerTrauma(10f);
            _rm.Ancorar(999f);
            Assert.AreEqual(100f, _rm.Atual, 1e-5f);
        }

        [Test]
        public void Ancorar_ComAtualNoMaximo_NaoDeveDispararEvento()
        {
            int contagem = 0;
            _rm.OnChanged += _ => contagem++;
            _rm.Ancorar(50f); // já está no máximo
            Assert.AreEqual(0, contagem);
        }

        // ════════════════════════════════════════════════════════════════════
        // E. Helpers narrativos
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void EstabilizarCompletamente_DeveRestaurarAoMax()
        {
            _rm.SofrerTrauma(60f);
            _rm.EstabilizarCompletamente();
            Assert.AreEqual(100f, _rm.Atual, 1e-5f);
        }

        [Test]
        public void EstabilizarCompletamente_SeJaNoMax_NaoDeveDispararEvento()
        {
            int contagem = 0;
            _rm.OnChanged += _ => contagem++;
            _rm.EstabilizarCompletamente();
            Assert.AreEqual(0, contagem);
        }

        [Test]
        public void ForcarColapso_DeveLevarAtualAZero()
        {
            _rm.ForcarColapso();
            Assert.AreEqual(0f, _rm.Atual, 1e-5f);
        }

        [Test]
        public void ForcarColapso_DeveDispararEntrouEmColapso()
        {
            ResilienciaChangedArgs? capturado = null;
            _rm.OnChanged += args => capturado = args;
            _rm.ForcarColapso();
            Assert.IsTrue(capturado?.EntrouEmColapso);
        }

        [Test]
        public void ForcarColapso_SeJaEmColapso_NaoDeveDispararEvento()
        {
            _rm.ForcarColapso();
            int contagem = 0;
            _rm.OnChanged += _ => contagem++;
            _rm.ForcarColapso();
            Assert.AreEqual(0, contagem);
        }

        // ════════════════════════════════════════════════════════════════════
        // F. Transições de estado
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void Estado_AcimaDoThreshold_NaoDevEstarEmPanico()
        {
            _rm.SofrerTrauma(70f); // atual = 30, threshold = 25
            Assert.IsFalse(_rm.IsPanico);
        }

        [Test]
        public void Estado_AbaixoDoThreshold_DeveEstarEmPanico()
        {
            _rm.SofrerTrauma(80f); // atual = 20, threshold = 25
            Assert.IsTrue(_rm.IsPanico);
        }

        [Test]
        public void Estado_ExatamenteNoThreshold_DeveEstarEmPanico()
        {
            _rm.SofrerTrauma(75f); // atual = 25, threshold = 25 → 25 ≤ 25 → pânico
            Assert.IsTrue(_rm.IsPanico);
        }

        [Test]
        public void Estado_EmZero_NaoDeveEstarEmPanico_MasSimEmColapso()
        {
            _rm.ForcarColapso();
            Assert.IsFalse(_rm.IsPanico,  "IsPanico deve ser false em colapso total.");
            Assert.IsTrue(_rm.IsColapso, "IsColapso deve ser true quando atual = 0.");
        }

        [Test]
        public void MargemAtePanico_AcimaDoThreshold_DeveSerPositiva()
        {
            _rm.SofrerTrauma(40f); // atual = 60, margem = 60 - 25 = 35
            Assert.AreEqual(35f, _rm.MargemAtePanico, 1e-5f);
        }

        [Test]
        public void MargemAtePanico_EmPanico_DeveSer_Zero()
        {
            _rm.SofrerTrauma(90f); // atual = 10, já em pânico
            Assert.AreEqual(0f, _rm.MargemAtePanico, 1e-5f);
        }

        // ════════════════════════════════════════════════════════════════════
        // G. Evento OnChanged — payload e supressão
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void OnChanged_SofrerTrauma_PayloadCorreto()
        {
            ResilienciaChangedArgs? capturado = null;
            _rm.OnChanged += args => capturado = args;
            _rm.SofrerTrauma(30f);

            Assert.IsNotNull(capturado);
            Assert.AreEqual(100f, capturado!.Value.ValorAnterior, 1e-5f, "ValorAnterior incorreto.");
            Assert.AreEqual(70f,  capturado!.Value.ValorAtual,    1e-5f, "ValorAtual incorreto.");
            Assert.AreEqual(100f, capturado!.Value.Max,           1e-5f, "Max incorreto.");
            Assert.AreEqual(25f,  capturado!.Value.ThresholdPanico, 1e-5f, "ThresholdPanico incorreto.");
            Assert.AreEqual(-30f, capturado!.Value.Delta,         1e-5f, "Delta incorreto.");
        }

        [Test]
        public void OnChanged_Ancorar_PercentualCorreto()
        {
            _rm.SofrerTrauma(50f);
            ResilienciaChangedArgs? capturado = null;
            _rm.OnChanged += args => capturado = args;
            _rm.Ancorar(25f); // 50 → 75

            Assert.AreEqual(0.75f, capturado!.Value.Percentual, 1e-5f);
        }

        [Test]
        public void OnChanged_EntradaEmPanico_EntrouEmPanicoTrue()
        {
            _rm.SofrerTrauma(70f); // atual = 30, acima do threshold
            ResilienciaChangedArgs? capturado = null;
            _rm.OnChanged += args => capturado = args;
            _rm.SofrerTrauma(10f); // 30 → 20, cruza threshold (25) descendo

            Assert.IsTrue(capturado!.Value.EntrouEmPanico,  "EntrouEmPanico deve ser true.");
            Assert.IsFalse(capturado!.Value.SaiuDoPanico,   "SaiuDoPanico deve ser false.");
            Assert.IsFalse(capturado!.Value.EntrouEmColapso,"EntrouEmColapso deve ser false.");
        }

        [Test]
        public void OnChanged_JaEmPanico_SofrerMaisTrauma_NaoDeveMarcarEntrouEmPanico()
        {
            _rm.SofrerTrauma(80f); // atual = 20, já em pânico
            ResilienciaChangedArgs? capturado = null;
            _rm.OnChanged += args => capturado = args;
            _rm.SofrerTrauma(5f); // 20 → 15, continua em pânico

            Assert.IsFalse(capturado!.Value.EntrouEmPanico,
                "EntrouEmPanico não deve ser true quando já estava em pânico.");
        }

        [Test]
        public void OnChanged_SaidaDoPanico_SaiuDoPanicoTrue()
        {
            _rm.SofrerTrauma(80f); // atual = 20, em pânico
            ResilienciaChangedArgs? capturado = null;
            _rm.OnChanged += args => capturado = args;
            _rm.Ancorar(10f); // 20 → 30, cruza threshold (25) subindo

            Assert.IsTrue(capturado!.Value.SaiuDoPanico,    "SaiuDoPanico deve ser true.");
            Assert.IsFalse(capturado!.Value.EntrouEmPanico, "EntrouEmPanico deve ser false.");
        }

        [Test]
        public void OnChanged_Colapso_EntrouEmColapsoTrue()
        {
            _rm.SofrerTrauma(70f); // atual = 30
            ResilienciaChangedArgs? capturado = null;
            _rm.OnChanged += args => capturado = args;
            _rm.SofrerTrauma(999f); // atual → 0

            Assert.IsTrue(capturado!.Value.EntrouEmColapso, "EntrouEmColapso deve ser true.");
        }

        [Test]
        public void OnChanged_MultiplasMudancas_SeqCorreta()
        {
            // Verifica que múltiplos eventos em sequência carregam os valores
            // corretos — não reutilizam estado de evento anterior.
            var historico = new List<float>();
            _rm.OnChanged += args => historico.Add(args.ValorAtual);

            _rm.SofrerTrauma(20f); // 100 → 80
            _rm.SofrerTrauma(20f); // 80 → 60
            _rm.Ancorar(10f);      // 60 → 70

            Assert.AreEqual(3, historico.Count);
            Assert.AreEqual(80f, historico[0], 1e-5f);
            Assert.AreEqual(60f, historico[1], 1e-5f);
            Assert.AreEqual(70f, historico[2], 1e-5f);
        }

        // ════════════════════════════════════════════════════════════════════
        // H. Factory ComThresholdFracional
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void Factory_FracaoValida_DeveCalcularThresholdCorreto()
        {
            var rm = ResilienciaMental.ComThresholdFracional(200f, 0.25f);
            Assert.AreEqual(50f, rm.ThresholdPanico, 1e-5f); // 25% de 200
        }

        [Test]
        public void Factory_FracaoZero_DeveSerValida()
        {
            // Threshold = 0 → pânico só com colapso total
            Assert.DoesNotThrow(() => ResilienciaMental.ComThresholdFracional(100f, 0f));
        }

        [Test]
        public void Factory_FracaoUm_DeveLancarArgumentOutOfRange()
            => Assert.Throws<ArgumentOutOfRangeException>(
                () => ResilienciaMental.ComThresholdFracional(100f, 1f));

        [Test]
        public void Factory_FracaoNegativa_DeveLancarArgumentOutOfRange()
            => Assert.Throws<ArgumentOutOfRangeException>(
                () => ResilienciaMental.ComThresholdFracional(100f, -0.1f));
    }
}
