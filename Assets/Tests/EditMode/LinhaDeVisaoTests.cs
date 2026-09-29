using FavelaAmarela.Core.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// A geometria da linha de visão do Rei, sem cena: o segmento Rei→Damião contra a pegada de
    /// um Nobre. O Rito do Olhar inteiro depende desta resposta estar certa nos cantos.
    /// </summary>
    public sealed class LinhaDeVisaoTests
    {
        private static readonly Vector2 Rei = new Vector2(0f, 10f);
        private static readonly CaixaDeCobertura Nobre =
            CaixaDeCobertura.DoCentro(new Vector2(0f, 5f), new Vector2(2f, 2f));

        [Test]
        public void AtrasDoNobre_ALinhaCruza()
            => Assert.IsTrue(LinhaDeVisao.Cruza(Rei, new Vector2(0f, 0f), Nobre));

        [Test]
        public void AoLadoDoNobre_ALinhaPassaLivre()
            => Assert.IsFalse(LinhaDeVisao.Cruza(Rei, new Vector2(4f, 0f), Nobre));

        [Test]
        public void NaFrenteDoNobre_ALinhaNaoChegaNele()
            => Assert.IsFalse(LinhaDeVisao.Cruza(Rei, new Vector2(0f, 7f), Nobre),
                "O Damião entre o Rei e o Nobre está exposto: o segmento acaba antes da caixa.");

        [Test]
        public void RasparNaQuina_ContaComoCoberto()
            => Assert.IsTrue(LinhaDeVisao.Cruza(new Vector2(-2f, 5f), new Vector2(0f, 7f), Nobre),
                "A diagonal que só encosta na quina (-1, 6) conta como tocar a caixa.");

        [Test]
        public void LinhaVertical_ParalelaAoLado_DentroEForaDaFaixa()
        {
            Assert.IsTrue(LinhaDeVisao.Cruza(new Vector2(1f, 10f), new Vector2(1f, 0f), Nobre),
                "Paralela ao eixo y, exatamente na borda: toca.");
            Assert.IsFalse(LinhaDeVisao.Cruza(new Vector2(1.01f, 10f), new Vector2(1.01f, 0f), Nobre));
        }

        [Test]
        public void Angulo_ZeroParaBaixo_PositivoADireita()
        {
            Assert.AreEqual(0f, LinhaDeVisao.AnguloAPartirDeBaixo(Rei, new Vector2(0f, 0f)), 1e-4f);
            Assert.AreEqual(45f, LinhaDeVisao.AnguloAPartirDeBaixo(Rei, new Vector2(5f, 5f)), 1e-3f);
            Assert.AreEqual(-90f, LinhaDeVisao.AnguloAPartirDeBaixo(Rei, new Vector2(-3f, 10f)), 1e-3f);
        }

        [Test]
        public void Caixa_AceitaCantosEmQualquerOrdem()
        {
            var c = new CaixaDeCobertura(new Vector2(3f, 1f), new Vector2(-1f, 4f));
            Assert.AreEqual(new Vector2(-1f, 1f), c.Min);
            Assert.AreEqual(new Vector2(3f, 4f), c.Max);
            Assert.IsTrue(c.Contem(new Vector2(0f, 2f)));
        }
    }
}
