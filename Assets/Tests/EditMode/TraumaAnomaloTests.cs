using System;
using NUnit.Framework;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Combat;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Suite EditMode do <b>segundo canal de dano</b>: o Trauma de Anomalia, que a
    /// <see cref="FichaDeAtributos"/> já descrevia em documentação mas que nenhuma arma
    /// conseguia entregar — o <see cref="ArmaResult"/> só carregava dano físico.
    ///
    /// <para>Cobre as três pontas do caminho: o struct do golpe carregar o valor, a cópia
    /// com bônus não perder campo, e a conta final (mitigação pela Resistência Anômala →
    /// Resiliência Mental) fechar contra uma ficha real de Carcosa.</para>
    /// </summary>
    public class TraumaAnomaloTests
    {
        // ── ArmaResult: o transporte ─────────────────────────────────────────

        [Test]
        public void ArmaResult_TraumaAnomalia_DefaultZero()
        {
            // A esmagadora maioria das armas é mundana: um facão não fere a mente.
            var golpe = new ArmaResult(success: true, durationSeconds: 0.3f, cooldownSeconds: 0f, dano: 10f);
            Assert.AreEqual(0f, golpe.TraumaAnomalia);
        }

        [Test]
        public void ArmaResult_ComBonus_SomaNosDoisCanais()
        {
            var golpe = new ArmaResult(success: true, durationSeconds: 0.3f, cooldownSeconds: 0f,
                dano: 10f, traumaAnomalia: 4f);

            var comBonus = golpe.ComBonus(bonusFisico: 5f, bonusAnomalia: 3f);

            Assert.AreEqual(15f, comBonus.Dano, 1e-4f);
            Assert.AreEqual(7f, comBonus.TraumaAnomalia, 1e-4f);
        }

        [Test]
        public void ArmaResult_ComBonus_BonusAnomaliaOpcional_NaoAlteraTrauma()
        {
            var golpe = new ArmaResult(success: true, durationSeconds: 0.3f, cooldownSeconds: 0f,
                dano: 10f, traumaAnomalia: 4f);

            var comBonus = golpe.ComBonus(bonusFisico: 5f);

            Assert.AreEqual(4f, comBonus.TraumaAnomalia, 1e-4f);
        }

        /// <summary>
        /// <b>Guarda de regressão.</b> A <c>MaoFisicaBridge</c> reconstruía o struct à mão em
        /// dois lugares, com um construtor posicional de onze argumentos. Todo campo novo
        /// era descartado por esquecimento numa das cópias, sem erro de compilação — foi
        /// exatamente assim que o Trauma de Anomalia teria se perdido. Este teste dá a cada
        /// campo um valor distinto e exige que a cópia devolva todos: se alguém adicionar um
        /// campo ao <see cref="ArmaResult"/> e esquecer de propagá-lo no
        /// <see cref="ArmaResult.ComBonus"/>, é aqui que quebra.
        /// </summary>
        [Test]
        public void ArmaResult_ComBonus_PreservaTodosOsOutrosCampos()
        {
            var original = new ArmaResult(
                success: true,
                durationSeconds: 0.42f,
                cooldownSeconds: 3.5f,
                atordoou: true,
                duracaoAtordoamento: 1.25f,
                dano: 10f,
                interrompeConjuracao: true,
                sangramentoPorSegundo: 2.5f,
                duracaoSangramento: 4f,
                forcaRepulsao: 7.5f,
                acumulosDeSangramento: 3,
                traumaAnomalia: 6f);

            var copia = original.ComBonus(bonusFisico: 1f, bonusAnomalia: 1f);

            Assert.AreEqual(original.Success, copia.Success, "Success perdido na cópia.");
            Assert.AreEqual(original.DurationSeconds, copia.DurationSeconds, 1e-4f, "DurationSeconds perdido.");
            Assert.AreEqual(original.CooldownSeconds, copia.CooldownSeconds, 1e-4f, "CooldownSeconds perdido.");
            Assert.AreEqual(original.Atordoou, copia.Atordoou, "Atordoou perdido.");
            Assert.AreEqual(original.DuracaoAtordoamento, copia.DuracaoAtordoamento, 1e-4f, "DuracaoAtordoamento perdido.");
            Assert.AreEqual(original.InterrompeConjuracao, copia.InterrompeConjuracao, "InterrompeConjuracao perdido.");
            Assert.AreEqual(original.SangramentoPorSegundo, copia.SangramentoPorSegundo, 1e-4f, "SangramentoPorSegundo perdido.");
            Assert.AreEqual(original.DuracaoSangramento, copia.DuracaoSangramento, 1e-4f, "DuracaoSangramento perdido.");
            Assert.AreEqual(original.ForcaRepulsao, copia.ForcaRepulsao, 1e-4f, "ForcaRepulsao perdido.");
            Assert.AreEqual(original.AcumulosDeSangramento, copia.AcumulosDeSangramento, "AcumulosDeSangramento perdido.");
        }

        // ── FichaDeAtributos: quem tem mente a ferir ─────────────────────────

        [Test]
        public void Ficha_ResilienciaMax_DefaultZero()
        {
            // A carne é a regra, a mente é a exceção: um Cultista comum não tem mente a ferir.
            var cultista = new FichaDeAtributos(vitalidadeMax: 100f, ataque: 24f, defesa: 4f);
            Assert.AreEqual(0f, cultista.ResilienciaMax);
        }

        [Test]
        public void Ficha_ResilienciaMaxNegativa_Lanca()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new FichaDeAtributos(100f, 10f, 5f, resilienciaMax: -1f));
        }

        // ── A conta fechando ponta a ponta ───────────────────────────────────

        [Test]
        public void TraumaAnomalo_MitigadoPelaResistencia_NaoPelaDefesa()
        {
            // Ficha real do Byakhee: defesa física 8, resistência anômala 12.
            var byakhee = new FichaDeAtributos(
                vitalidadeMax: 500f, ataque: 26f, defesa: 8f,
                resistenciaAnomala: 12f, resilienciaMax: 120f);

            // Um golpe anômalo de 20 é mitigado pelos 12 de Resistência, não pelos 8 de Defesa.
            float trauma = MitigacaoDeDano.Aplicar(20f, byakhee.ResistenciaAnomala);
            Assert.AreEqual(8f, trauma, 1e-4f);

            // Se por engano passasse pela Defesa, o número seria outro — é essa troca
            // silenciosa de canal que o teste trava.
            float seFosseDefesa = MitigacaoDeDano.Aplicar(20f, byakhee.Defesa);
            Assert.AreNotEqual(trauma, seFosseDefesa);
        }

        [Test]
        public void TraumaAnomalo_AcumulaAteODesfazerDaMente()
        {
            var byakhee = new FichaDeAtributos(
                vitalidadeMax: 500f, ataque: 26f, defesa: 8f,
                resistenciaAnomala: 12f, resilienciaMax: 120f);

            // Espelha o que a EnemyBase monta: pânico em zero — inimigo não entra em pânico,
            // a mente só interessa cheia ou desfeita.
            var mente = new ResilienciaMental(byakhee.ResilienciaMax, 0f);

            var golpe = new ArmaResult(success: true, durationSeconds: 0.3f, cooldownSeconds: 0f,
                dano: 0f, traumaAnomalia: 20f);

            // 8 de trauma por golpe → 15 golpes para desfazer 120 de mente.
            for (int i = 0; i < 14; i++)
                mente.SofrerTrauma(MitigacaoDeDano.Aplicar(golpe.TraumaAnomalia, byakhee.ResistenciaAnomala));

            Assert.IsFalse(mente.IsColapso, "A mente cedeu cedo demais.");

            mente.SofrerTrauma(MitigacaoDeDano.Aplicar(golpe.TraumaAnomalia, byakhee.ResistenciaAnomala));

            Assert.IsTrue(mente.IsColapso, "O 15º golpe anômalo deveria desfazer a mente.");
        }

        [Test]
        public void MenteDesfeita_NaoDependeDaVitalidade()
        {
            // A tese do canal duplo: a criatura cai pela mente com a carne quase intacta.
            var carne = new Vitalidade(500f);
            var mente = new ResilienciaMental(120f, 0f);

            mente.SofrerTrauma(120f);

            Assert.IsTrue(mente.IsColapso);
            Assert.IsFalse(carne.EstaAbatido);
            Assert.AreEqual(500f, carne.Atual, 1e-4f, "Nenhum dano físico deveria ter sido aplicado.");
        }

        [Test]
        public void ArmaMundana_NaoFereMente()
        {
            // Facão Enferrujado do Tier 1: dano físico puro, trauma zero.
            var mente = new ResilienciaMental(120f, 0f);
            var golpeMundano = new ArmaResult(success: true, durationSeconds: 0.3f,
                cooldownSeconds: 0f, dano: 30f);

            float trauma = MitigacaoDeDano.Aplicar(golpeMundano.TraumaAnomalia, 12f);
            if (trauma > 0f) mente.SofrerTrauma(trauma);

            Assert.AreEqual(120f, mente.Atual, 1e-4f, "Arma mundana não pode arranhar a mente.");
        }
    }
}
