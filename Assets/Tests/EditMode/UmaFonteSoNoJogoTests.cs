using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda que <b>todo texto do jogo usa a mesma fonte</b>, e que essa fonte não é a embutida
    /// da Unity.
    ///
    /// <para><b>O que motivou (2026-09-02).</b> O Vini reclamou da fonte olhando um print da barra
    /// de interação. Medido, o problema não era a face escolhida — era não haver <b>uma</b>
    /// escolha:</para>
    ///
    /// <code>
    /// Cena_Menu, Castelo_Carcosa   9 Text  ->  Kenney Pixel
    /// HUD_Gameplay, Painel_Opcoes 78 Text  ->  Arial embutida (fileID 10102)
    /// </code>
    ///
    /// <para>O menu já tinha sido migrado para a pixel font; o HUD ficou para trás e ninguém
    /// percebeu, porque nenhum teste comparava os dois. O jogo abria numa tipografia e jogava em
    /// outra.</para>
    ///
    /// <para><b>Por que este é um teste de disco, e isso está certo aqui.</b> A regra é sobre uma
    /// <b>referência serializada</b> — qual asset o campo aponta. Não é geometria nem layout: é
    /// exatamente o tipo de fato que o YAML guarda inteiro. Medir isto em PlayMode não acrescenta
    /// nada e custa uma cena.</para>
    /// </summary>
    public sealed class UmaFonteSoNoJogoTests
    {
        /// <summary>
        /// A fonte embutida da Unity (<c>LegacyRuntime.ttf</c>, a Arial). É o <i>default</i> que
        /// todo <c>Text</c> novo herda sem ninguém escolher — e por isso o sinal de que ninguém
        /// escolheu.
        /// </summary>
        private const string Embutida = "m_Font: {fileID: 10102";

        private static readonly Regex Referencia =
            new Regex(@"m_Font: \{fileID: (\d+), guid: ([a-f0-9]{32})", RegexOptions.Compiled);

        private static IEnumerable<string> Arquivos()
        {
            foreach (var pasta in new[] { "Assets/Scenes", "Assets/FavelaAmarela/Resources" })
            {
                if (!Directory.Exists(pasta)) continue;
                foreach (var f in Directory.EnumerateFiles(pasta, "*.*", SearchOption.AllDirectories)
                             .Where(f => f.EndsWith(".unity") || f.EndsWith(".prefab")))
                    yield return f;
            }
        }

        [Test]
        public void NenhumTextoUsaAFonteEmbutidaDaUnity()
        {
            var culpados = Arquivos()
                .Select(f => (Arquivo: f, Quantos: Contar(File.ReadAllText(f), Embutida)))
                .Where(p => p.Quantos > 0)
                .Select(p => $"  {Path.GetFileName(p.Arquivo)}: {p.Quantos} Text")
                .ToArray();

            Assert.IsEmpty(culpados,
                "Text(s) na fonte EMBUTIDA da Unity (Arial). É o default que todo Text novo " +
                "herda — quem está aqui não teve a fonte escolhida:" + System.Environment.NewLine +
                string.Join(System.Environment.NewLine, culpados));
        }

        [Test]
        public void OJogoInteiroUsaUmaFonteSo()
        {
            var porFonte = new Dictionary<string, List<string>>();
            int total = 0;

            foreach (var f in Arquivos())
            {
                foreach (Match m in Referencia.Matches(File.ReadAllText(f)))
                {
                    string chave = $"{m.Groups[1].Value}/{m.Groups[2].Value}";
                    if (!porFonte.TryGetValue(chave, out var lista))
                        porFonte[chave] = lista = new List<string>();
                    if (!lista.Contains(Path.GetFileName(f))) lista.Add(Path.GetFileName(f));
                    total++;
                }
            }

            // REGRA DURA: zero referências significaria que este teste não achou nada para medir.
            Assert.Greater(total, 50,
                $"Só achei {total} referência(s) de fonte. Este teste não está lendo os prefabs " +
                "do HUD — passar verde aqui não afirmaria nada.");

            Assert.AreEqual(1, porFonte.Count,
                $"O jogo usa {porFonte.Count} fontes diferentes. O jogador abre o menu numa " +
                "tipografia e joga em outra:" + System.Environment.NewLine +
                string.Join(System.Environment.NewLine,
                    porFonte.Select(p => $"  {p.Key} em {string.Join(", ", p.Value)}")));
        }

        private static int Contar(string texto, string agulha)
        {
            int n = 0, i = 0;
            while ((i = texto.IndexOf(agulha, i, System.StringComparison.Ordinal)) >= 0)
            {
                n++;
                i += agulha.Length;
            }
            return n;
        }
    }
}
