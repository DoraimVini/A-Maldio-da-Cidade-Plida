using NUnit.Framework;
using UnityEngine;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Smoke test do recurso de fonte embutida usado pelo <c>DanoFlutuante</c>
    /// (números de dano em world space).
    ///
    /// Existe porque a Unity 6 <b>renomeou</b> a fonte built-in: o antigo
    /// <c>Arial.ttf</c> não existe mais, é <c>LegacyRuntime.ttf</c>. Se o nome estiver
    /// errado, <c>Resources.GetBuiltinResource</c> devolve null e os números de dano
    /// somem <b>silenciosamente</b> — este teste transforma essa falha silenciosa em
    /// falha de CI.
    /// </summary>
    public class FonteBuiltinTests
    {
        [Test]
        public void FonteLegacyRuntime_ExisteNaUnity6()
        {
            var fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Assert.IsNotNull(fonte,
                "A fonte built-in 'LegacyRuntime.ttf' não foi encontrada — o DanoFlutuante " +
                "não conseguirá renderizar os números de dano.");
        }

        [Test]
        public void FonteArialAntiga_LancaExcecao()
        {
            // Documenta o gotcha com precisão: a Unity 6 não devolve null para o nome
            // antigo — ela LANÇA ArgumentException ("Arial.ttf is no longer a valid
            // built in font. Please use LegacyRuntime.ttf"). Ou seja, usar o nome velho
            // quebraria em runtime, não degradaria silenciosamente.
            Assert.Throws<System.ArgumentException>(
                () => Resources.GetBuiltinResource<Font>("Arial.ttf"),
                "'Arial.ttf' parou de lançar — a Unity mudou de comportamento, reavaliar.");
        }
    }
}
