using System.Collections.Generic;
using NUnit.Framework;
using FavelaAmarela.Core.Abilities;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Suite EditMode do sorteio do baú da Tumba. O ponto crítico de design coberto aqui:
    /// o sorteio é <b>uniforme e cobre as três armas</b> — nenhuma build sai favorecida, e
    /// nenhuma arma fica inalcançável (o Abdul tem de ser vencível com qualquer uma).
    /// </summary>
    public class SorteioDeArmaDaTumbaTests
    {
        [Test]
        public void Pool_TemAsTresArmas()
        {
            Assert.AreEqual(3, new SorteioDeArmaDaTumba().Quantidade);
        }

        [Test]
        public void Sortear_CobreAsTresArmasSemRepetir()
        {
            var referencia = new SorteioDeArmaDaTumba();
            var vistas = new HashSet<ArmaDaTumba>();

            for (int i = 0; i < referencia.Quantidade; i++)
            {
                double amostra = (i + 0.5) / referencia.Quantidade;
                var s = new SorteioDeArmaDaTumba(() => amostra);
                vistas.Add(s.Sortear());
            }

            Assert.AreEqual(3, vistas.Count,
                "Cada faixa do intervalo [0,1) deve produzir uma arma distinta — sorteio uniforme.");
            Assert.Contains(ArmaDaTumba.CravoDeAklo, new List<ArmaDaTumba>(vistas));
            Assert.Contains(ArmaDaTumba.EstileteDeIrem, new List<ArmaDaTumba>(vistas));
            Assert.Contains(ArmaDaTumba.AlfanjeDeAlhazred, new List<ArmaDaTumba>(vistas));
        }

        [Test]
        public void Sortear_AmostraZero_NaoEstoura()
        {
            Assert.DoesNotThrow(() => new SorteioDeArmaDaTumba(() => 0.0).Sortear());
        }

        [Test]
        public void Sortear_AmostraQuaseUm_NaoEstoura()
        {
            var s = new SorteioDeArmaDaTumba(() => 0.999999);
            Assert.AreEqual(ArmaDaTumba.AlfanjeDeAlhazred, s.Sortear());
        }

        [Test]
        public void Criar_CadaTipo_InstanciaArmaCorretaComHabilidade()
        {
            var cravo = SorteioDeArmaDaTumba.Criar(ArmaDaTumba.CravoDeAklo);
            var estilete = SorteioDeArmaDaTumba.Criar(ArmaDaTumba.EstileteDeIrem);
            var alfanje = SorteioDeArmaDaTumba.Criar(ArmaDaTumba.AlfanjeDeAlhazred);

            Assert.IsInstanceOf<CravoDeAklo>(cravo);
            Assert.IsInstanceOf<EstileteDeIrem>(estilete);
            Assert.IsInstanceOf<AlfanjeDeAlhazred>(alfanje);

            // Toda arma do baú tem habilidade em botão separado.
            foreach (var arma in new[] { cravo, estilete, alfanje })
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(arma.NomeDaArma));
                Assert.IsFalse(string.IsNullOrWhiteSpace(arma.NomeHabilidade));
                Assert.Greater(arma.Execute().Dano, 0f);
            }
        }

        [Test]
        public void SortearECriar_DevolveArmaUsavel()
        {
            var arma = new SorteioDeArmaDaTumba(() => 0.5).SortearECriar();
            Assert.IsNotNull(arma);
            Assert.IsTrue(arma.Execute().Success);
        }

        [Test]
        public void TodasAsArmas_ServemContraBoss_TemDanoNaHabilidade()
        {
            // Nenhuma arma pode ser "inútil" contra a Aparição Primordial: o baú é RNG,
            // então as três precisam ter uma habilidade que contribua na luta.
            foreach (ArmaDaTumba tipo in System.Enum.GetValues(typeof(ArmaDaTumba)))
            {
                var r = SorteioDeArmaDaTumba.Criar(tipo).ExecuteHabilidade();
                bool contribui = r.Dano > 0f || r.SangramentoPorSegundo > 0f
                                 || r.InterrompeConjuracao || r.Atordoou || r.ForcaRepulsao > 0f;
                Assert.IsTrue(contribui, $"{tipo}: a habilidade precisa ter efeito útil contra boss.");
            }
        }
    }
}
