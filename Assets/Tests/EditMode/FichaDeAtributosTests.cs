using System;
using NUnit.Framework;
using FavelaAmarela.Core.Combat;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Suite EditMode da <see cref="FichaDeAtributos"/> — o POCO de atributos base que
    /// toda unidade tem. Verifica round-trip dos 5 atributos, defaults de conjuração e
    /// as validações de construção.
    /// </summary>
    public class FichaDeAtributosTests
    {
        [Test]
        public void Construtor_ExpoeOsCincoAtributos()
        {
            var ficha = new FichaDeAtributos(
                vitalidadeMax: 100f, ataque: 24f, defesa: 4f,
                conjuracao: 30f, resistenciaAnomala: 8f);

            Assert.AreEqual(100f, ficha.VitalidadeMax);
            Assert.AreEqual(24f, ficha.Ataque);
            Assert.AreEqual(4f, ficha.Defesa);
            Assert.AreEqual(30f, ficha.Conjuracao);
            Assert.AreEqual(8f, ficha.ResistenciaAnomala);
        }

        [Test]
        public void Construtor_ConjuracaoEResistencia_DefaultZero()
        {
            // A maioria das unidades não conjura (ex.: Cultista comum).
            var ficha = new FichaDeAtributos(vitalidadeMax: 100f, ataque: 24f, defesa: 4f);
            Assert.AreEqual(0f, ficha.Conjuracao);
            Assert.AreEqual(0f, ficha.ResistenciaAnomala);
        }

        [Test]
        public void Construtor_VitalidadeNaoPositiva_Lanca()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new FichaDeAtributos(0f, 10f, 5f));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new FichaDeAtributos(-1f, 10f, 5f));
        }

        [Test]
        public void Construtor_AtributoNegativo_Lanca()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new FichaDeAtributos(100f, -1f, 5f));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new FichaDeAtributos(100f, 10f, -5f));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new FichaDeAtributos(100f, 10f, 5f, conjuracao: -3f));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new FichaDeAtributos(100f, 10f, 5f, resistenciaAnomala: -2f));
        }

        [Test]
        public void Ficha_AlimentaMitigacao_ContaFisicaFecha()
        {
            // Integração leve: golpe do Cultista (Ataque) vs Defesa do Damião = 20.
            var cultista = new FichaDeAtributos(vitalidadeMax: 100f, ataque: 24f, defesa: 0f);
            var damiao = new FichaDeAtributos(vitalidadeMax: 100f, ataque: 0f, defesa: 4f);

            float danoRecebido = MitigacaoDeDano.Aplicar(cultista.Ataque, damiao.Defesa);
            Assert.AreEqual(20f, danoRecebido, 0.0001f);
        }

        // ── ComBonus: o antídoto para a reconstrução posicional ──────────────

        /// <summary>
        /// O teste que importa. O <c>VitalidadeBridge</c> recalculava a ficha chamando o
        /// construtor com 3 dos 10 parâmetros; os outros sete voltavam ao default a cada troca de
        /// equipamento. <c>ResistenciaAnomala</c> ia a zero — o defensor perdia toda mitigação
        /// anômala e o único sintoma era morrer mais rápido sem explicação.
        /// </summary>
        [Test]
        public void ComBonus_PreservaTodoCampoQueNaoRecebeBonus()
        {
            var original = new FichaDeAtributos(
                vitalidadeMax: 120f,
                ataque: 14f,
                defesa: 6f,
                conjuracao: 9f,
                resistenciaAnomala: 7f,
                velocidadeErrante: 2.2f,
                velocidadeCaca: 4.4f,
                alcanceDeGolpe: 1.8f,
                cadenciaDeAtaque: 0.9f,
                resilienciaMax: 80f);

            var comBonus = original.ComBonus(bonusVitalidade: 30f, bonusDefesa: 5f);

            // O que recebeu bônus
            Assert.AreEqual(150f, comBonus.VitalidadeMax, 1e-4f);
            Assert.AreEqual(11f, comBonus.Defesa, 1e-4f);

            // O que NÃO recebeu tem de sobreviver intacto — era exatamente isto que se perdia.
            Assert.AreEqual(original.Ataque, comBonus.Ataque, 1e-4f, "Ataque foi alterado.");
            Assert.AreEqual(original.Conjuracao, comBonus.Conjuracao, 1e-4f, "Conjuração foi alterada.");
            Assert.AreEqual(original.ResistenciaAnomala, comBonus.ResistenciaAnomala, 1e-4f,
                "ResistenciaAnomala foi zerada — é o bug que este método existe para impedir.");
            Assert.AreEqual(original.VelocidadeErrante, comBonus.VelocidadeErrante, 1e-4f);
            Assert.AreEqual(original.VelocidadeCaca, comBonus.VelocidadeCaca, 1e-4f);
            Assert.AreEqual(original.AlcanceDeGolpe, comBonus.AlcanceDeGolpe, 1e-4f);
            Assert.AreEqual(original.CadenciaDeAtaque, comBonus.CadenciaDeAtaque, 1e-4f);
            Assert.AreEqual(original.ResilienciaMax, comBonus.ResilienciaMax, 1e-4f,
                "ResilienciaMax foi zerada.");
        }

        /// <summary>
        /// Sem argumento nenhum, <c>ComBonus</c> tem de ser cópia fiel. Serve de rede contra
        /// alguém acrescentar um campo à ficha e esquecer de copiá-lo aqui: o campo novo apareceria
        /// como divergência já nesta chamada trivial.
        /// </summary>
        [Test]
        public void ComBonus_SemArgumentos_EhCopiaFiel()
        {
            var original = new FichaDeAtributos(
                vitalidadeMax: 55f, ataque: 3f, defesa: 2f,
                conjuracao: 1f, resistenciaAnomala: 4f,
                velocidadeErrante: 1.1f, velocidadeCaca: 2.3f,
                alcanceDeGolpe: 1.4f, cadenciaDeAtaque: 1.7f, resilienciaMax: 12f);

            var copia = original.ComBonus();

            Assert.AreEqual(original.VitalidadeMax, copia.VitalidadeMax, 1e-4f);
            Assert.AreEqual(original.Ataque, copia.Ataque, 1e-4f);
            Assert.AreEqual(original.Defesa, copia.Defesa, 1e-4f);
            Assert.AreEqual(original.Conjuracao, copia.Conjuracao, 1e-4f);
            Assert.AreEqual(original.ResistenciaAnomala, copia.ResistenciaAnomala, 1e-4f);
            Assert.AreEqual(original.VelocidadeErrante, copia.VelocidadeErrante, 1e-4f);
            Assert.AreEqual(original.VelocidadeCaca, copia.VelocidadeCaca, 1e-4f);
            Assert.AreEqual(original.AlcanceDeGolpe, copia.AlcanceDeGolpe, 1e-4f);
            Assert.AreEqual(original.CadenciaDeAtaque, copia.CadenciaDeAtaque, 1e-4f);
            Assert.AreEqual(original.ResilienciaMax, copia.ResilienciaMax, 1e-4f);
        }

        /// <summary>
        /// Bônus negativo (item amaldiçoado) não pode lançar. O construtor rejeita negativos, e
        /// o recálculo de equipamento roda dentro de um handler de evento — uma exceção ali
        /// mataria a atualização de atributos inteira, não só a linha que estourou.
        /// </summary>
        [Test]
        public void ComBonus_ComBonusNegativo_NaoLanca_EFazPisoEmZero()
        {
            var f = new FichaDeAtributos(100f, 10f, 6f, resistenciaAnomala: 2f, resilienciaMax: 30f);

            FichaDeAtributos r = null;
            Assert.DoesNotThrow(() => r = f.ComBonus(
                bonusVitalidade: -500f,
                bonusDefesa: -8f,
                bonusAtaque: -50f,
                bonusResistenciaAnomala: -9f,
                bonusResilienciaMax: -99f));

            Assert.AreEqual(1f, r.VitalidadeMax, 1e-4f, "Vitalidade tem piso 1 — zero lançaria.");
            Assert.AreEqual(0f, r.Defesa, 1e-4f);
            Assert.AreEqual(0f, r.Ataque, 1e-4f);
            Assert.AreEqual(0f, r.ResistenciaAnomala, 1e-4f);
            Assert.AreEqual(0f, r.ResilienciaMax, 1e-4f);
        }

        [Test]
        public void ComBonus_SomaEmTodosOsCanaisQueAceitamBonus()
        {
            var f = new FichaDeAtributos(100f, 10f, 5f, resistenciaAnomala: 2f, resilienciaMax: 50f);

            var r = f.ComBonus(bonusVitalidade: 10f, bonusDefesa: 1f, bonusAtaque: 4f,
                               bonusResistenciaAnomala: 3f, bonusResilienciaMax: 25f);

            Assert.AreEqual(110f, r.VitalidadeMax, 1e-4f);
            Assert.AreEqual(14f, r.Ataque, 1e-4f);
            Assert.AreEqual(6f, r.Defesa, 1e-4f);
            Assert.AreEqual(5f, r.ResistenciaAnomala, 1e-4f);
            Assert.AreEqual(75f, r.ResilienciaMax, 1e-4f);
        }
    }
}
