using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda que todo item autorado tenha ícone.
    ///
    /// <para><b>O buraco que motivou (2026-08-19):</b> <b>18 dos 20</b> <c>ItemDef</c> estavam
    /// com <c>Icone: {fileID: 0}</c> — toda arma, armadura, consumível e artefato aparecia em
    /// branco na mochila e na barra de ações. Não dá erro, não quebra compilação, não aparece
    /// no Inspector como problema: só se vê jogando. É a mesma família dos outros defeitos
    /// desta base — a peça existe e não está ligada.</para>
    /// </summary>
    public sealed class IconesDosItensTests
    {
        /// <summary>
        /// Assets de item/artefato do projeto. Filtra pelo <b>campo</b> <c>Icone</c> em vez de
        /// por pasta: assim um item novo criado em outro lugar também entra no guarda.
        /// </summary>
        private static IEnumerable<string> AssetsComCampoIcone()
        {
            return Directory
                .EnumerateFiles("Assets", "*.asset", SearchOption.AllDirectories)
                .Where(p => File.ReadAllText(p).Contains("Icone:"));
        }

        [Test]
        public void NenhumItemAutorado_FicaSemIcone()
        {
            var semIcone = new List<string>();

            foreach (var caminho in AssetsComCampoIcone())
            {
                string txt = File.ReadAllText(caminho);

                var m = Regex.Match(txt, @"Icone:\s*\{fileID:\s*(-?\d+)");
                if (!m.Success) continue;

                if (m.Groups[1].Value == "0")
                    semIcone.Add(Path.GetFileNameWithoutExtension(caminho));
            }

            Assert.IsEmpty(semIcone,
                "Itens sem ícone — aparecem em branco na mochila e na barra de ações, sem " +
                "erro nenhum no console. Rode 'Tools/FavelaAmarela/Ligar icones dos itens'.\n  " +
                string.Join("\n  ", semIcone));
        }

        /// <summary>
        /// Os ícones pintados foram reduzidos de ~485 px e vão a <b>Bilinear</b>; os de pixel
        /// art vão a <b>Point</b>. Point num ícone pintado reduzido serrilha as bordas suaves e
        /// fica pior que a fonte — por isso o filtro não é único aqui, ao contrário do resto do
        /// projeto.
        /// </summary>
        [Test]
        public void Icones_TemFiltroCoerenteComAOrigem()
        {
            const string pasta = "Assets/FavelaAmarela/Art/Items/Icones";

            var pixelArt = new HashSet<string>
            {
                "Icone_AguaDaCacimba", "Icone_ErvaDeAncoragem", "Icone_RaizDeYhtill",
                "Icone_CapuzDeFarrapos", "Icone_ColeteDeSucata", "Icone_CaneleirasDeFerro",
                "Icone_ElmoDeSet", "Icone_PeitoralDeSet", "Icone_GrevasDeSet",
                "Icone_Necronomicon", "Icone_AnelDoSinalAmarelo", "Icone_Estilete",
            };

            Assert.IsTrue(Directory.Exists(pasta), $"Pasta de ícones ausente: {pasta}");

            var falhas = new List<string>();

            foreach (var png in Directory.EnumerateFiles(pasta, "*.png"))
            {
                string meta = png + ".meta";
                if (!File.Exists(meta)) { falhas.Add($"{Path.GetFileName(png)}: sem .meta"); continue; }

                string nome = Path.GetFileNameWithoutExtension(png);
                string txt = File.ReadAllText(meta);

                var filtro = Regex.Match(txt, @"filterMode:\s*(-?\d+)");
                if (!filtro.Success) { falhas.Add($"{nome}: sem filterMode"); continue; }

                string esperado = pixelArt.Contains(nome) ? "0" : "1";   // 0 = Point, 1 = Bilinear
                if (filtro.Groups[1].Value != esperado)
                    falhas.Add($"{nome}: filterMode={filtro.Groups[1].Value}, esperado {esperado} " +
                               (pixelArt.Contains(nome) ? "(Point — é pixel art)"
                                                        : "(Bilinear — é pintura reduzida)"));

                if (!Regex.IsMatch(txt, @"spritePixelsToUnits:\s*32\b"))
                    falhas.Add($"{nome}: PPU != 32");
            }

            Assert.IsEmpty(falhas, "Import de ícone fora do esperado:\n  " + string.Join("\n  ", falhas));
        }
    }
}
