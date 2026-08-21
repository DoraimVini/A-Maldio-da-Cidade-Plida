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

        /// <summary>
        /// Nenhum arquivo do projeto pode <b>pedir</b> a fonte pelo nome antigo.
        ///
        /// <para><b>Por que os dois testes acima não bastavam:</b> eles medem o comportamento da
        /// <i>Unity</i> — que a fonte nova existe e que a velha lança. Nenhum dos dois olha para o
        /// nosso código. Em 2026-08-20 escrevi <c>GetBuiltinExtraResource&lt;Font&gt;("Arial.ttf")</c>
        /// num montador novo; a suíte inteira passou verde e a ferramenta só estourou ao rodar em
        /// batch, no meio de uma cadeia de montagem. O gotcha estava documentado em <b>seis</b>
        /// arquivos e ainda assim foi digitado errado — a documentação não impede o erro, o
        /// guarda impede.</para>
        ///
        /// <para>Ignora as próprias menções em comentário/mensagem: o que se proíbe é a
        /// <b>chamada</b>, não falar do nome antigo ao explicar a armadilha.</para>
        /// </summary>
        [Test]
        public void NenhumCodigo_PedeAFonteAntiga()
        {
            var culpados = new System.Collections.Generic.List<string>();

            foreach (var arquivo in System.IO.Directory.EnumerateFiles(
                         "Assets", "*.cs", System.IO.SearchOption.AllDirectories))
            {
                var linhas = System.IO.File.ReadAllLines(arquivo);

                for (int i = 0; i < linhas.Length; i++)
                {
                    // Corta no "//" antes de testar. Sem isto o guarda acusa os COMENTÁRIOS que
                    // explicam a armadilha — que citam a chamada e o nome antigo na mesma linha,
                    // justamente para avisar. Validado por mutação: sem o corte, três arquivos
                    // corretos eram reprovados. Um guarda que rejeita dado certo é pior que
                    // guarda nenhum, porque ensina a desligá-lo.
                    string linha = linhas[i];
                    int comentario = linha.IndexOf("//", System.StringComparison.Ordinal);
                    if (comentario >= 0) linha = linha.Substring(0, comentario);

                    // A chamada de verdade: o nome antigo entre aspas dentro de um
                    // GetBuiltinResource/GetBuiltinExtraResource, na mesma linha.
                    if (!linha.Contains("\"Arial.ttf\"")) continue;
                    if (!linha.Contains("GetBuiltin")) continue;

                    // O teste vizinho chama de propósito, para documentar que lança.
                    if (arquivo.EndsWith("FonteBuiltinTests.cs")) continue;

                    culpados.Add($"{arquivo}:{i + 1}");
                }
            }

            Assert.IsEmpty(culpados,
                "Fonte built-in pedida pelo nome antigo — isso LANÇA ArgumentException na Unity 6 " +
                "e derruba a ferramenta inteira. Use \"LegacyRuntime.ttf\", por " +
                "Resources.GetBuiltinResource:\n  " + string.Join("\n  ", culpados));
        }
    }
}
