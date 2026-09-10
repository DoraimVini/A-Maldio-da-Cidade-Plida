using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// O pacote <c>com.unity.pipeline</c> (instalado em 2026-09-10) é o que deixa o Unity CLI e
    /// o Claude Code <b>dirigirem o Editor aberto</b> — rodar a suíte, executar ferramentas, ler
    /// a cena — sem fechar a Unity. Ele tem também uma parte de <b>runtime</b>: um servidor de
    /// comandos que pode ser compilado para dentro do jogador (<c>enableInBuilds</c>).
    ///
    /// <para>Num jogo entregue a um edital, um servidor de controle escutando numa porta é
    /// inaceitável. O padrão do pacote é <c>false</c> e nada está persistido; este guarda existe
    /// para que uma mudança dessa — um clique numa janela de configurações — não passe em
    /// silêncio.</para>
    /// </summary>
    public sealed class OPipelineNaoEntraNaBuildTests
    {
        [Test]
        public void ORuntimeDoPipeline_NaoEstaHabilitadoParaBuilds()
        {
            var culpados = Directory.GetFiles("Assets", "*.asset", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles("ProjectSettings", "*.asset"))
                .Where(f => Regex.IsMatch(File.ReadAllText(f), @"enableInBuilds:\s*(1|true)", RegexOptions.IgnoreCase))
                .ToArray();

            Assert.IsEmpty(culpados,
                "O runtime do com.unity.pipeline está habilitado para builds em:\n  " +
                string.Join("\n  ", culpados) +
                "\nIsso compila um servidor de comandos para dentro do jogo entregue. Desligue " +
                "(unity command set_runtime_pipeline_settings --enableInBuilds false --confirm true).");
        }

        [Test]
        public void OPacote_EstaNoManifesto()
        {
            string manifesto = File.ReadAllText("Packages/manifest.json");

            StringAssert.Contains("\"com.unity.pipeline\"", manifesto,
                "O com.unity.pipeline saiu do manifesto — sem ele o Unity CLI não dirige o Editor " +
                "aberto e a suíte volta a exigir a Unity fechada (ver favela-qa-pipeline).");
        }
    }
}
