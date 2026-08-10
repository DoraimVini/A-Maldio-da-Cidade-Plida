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
    }
}
