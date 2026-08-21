using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda o que a build mostra para fora.
    ///
    /// <para><b>Por que existe (2026-08-20):</b> o <c>productName</c> era "A Maldição da Cidade
    /// Pálida" — o nome do <b>repositório</b>. O título oficial visível ao jogador é
    /// <b>"Caminho para Carcosa"</b>, decisão do Vini registrada no topo do <c>CLAUDE.md</c>.
    /// A janela do jogo e o executável entregues ao edital sairiam com o nome errado, e nenhum
    /// teste olhava para isso.</para>
    ///
    /// <para>Lê o YAML de <c>ProjectSettings.asset</c> em vez de usar <c>PlayerSettings</c>:
    /// assim o guarda vale igual rodando em batch, e mede o que está <b>gravado</b>, não o que
    /// a API devolve em memória.</para>
    /// </summary>
    public sealed class IdentidadeDaBuildTests
    {
        private const string Settings = "ProjectSettings/ProjectSettings.asset";
        private const string TituloOficial = "Caminho para Carcosa";

        [Test]
        public void OTituloDaBuild_EONomeOficialDoJogo()
        {
            Assert.IsTrue(File.Exists(Settings), $"{Settings} não encontrado.");

            var m = Regex.Match(File.ReadAllText(Settings), @"(?m)^\s*productName:\s*(.+)$");
            Assert.IsTrue(m.Success, "productName não encontrado em ProjectSettings.asset.");

            string valor = m.Groups[1].Value.Trim().Trim('"');

            Assert.AreEqual(TituloOficial, valor,
                $"A build sairia com o título '{valor}'. O nome oficial visível ao jogador é " +
                $"'{TituloOficial}' (CLAUDE.md, primeira seção) — os outros nomes " +
                "(repositório, pasta, namespaces) são históricos e não vão para a tela. " +
                "Conserto: 'Tools/FavelaAmarela/Build: preparar identidade'.");
        }

        [Test]
        public void OEstudio_NaoEOPadraoDaUnity()
        {
            var m = Regex.Match(File.ReadAllText(Settings), @"(?m)^\s*companyName:\s*(.+)$");
            Assert.IsTrue(m.Success, "companyName não encontrado.");

            string valor = m.Groups[1].Value.Trim().Trim('"');

            Assert.AreNotEqual("DefaultCompany", valor,
                "companyName está no padrão da Unity. Ele vai para o caminho de save do " +
                "jogador (%APPDATA%/DefaultCompany/...) e para as propriedades do executável.");
        }

        /// <summary>
        /// A cena de índice 0 é a que a build abre. Se não for o menu, o jogo arranca no meio
        /// do mundo — sem estado, sem save carregado, provavelmente sem jogador.
        /// </summary>
        [Test]
        public void APrimeiraCenaDaBuild_EOMenu()
        {
            const string arquivo = "ProjectSettings/EditorBuildSettings.asset";
            Assert.IsTrue(File.Exists(arquivo), $"{arquivo} não encontrado.");

            var primeira = Regex.Match(File.ReadAllText(arquivo),
                                       @"enabled:\s*1[\s\S]{0,120}?path:\s*(\S+)");

            Assert.IsTrue(primeira.Success, "Nenhuma cena habilitada no Build Settings.");

            StringAssert.Contains("Cena_Menu.unity", primeira.Groups[1].Value,
                $"A build abriria em '{primeira.Groups[1].Value}' em vez do menu.");
        }
    }
}
