using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda os ajustes de import da pixel art: <b>Point</b>, <b>sem compressão</b>, <b>PPU
    /// 32</b> — o que a skill <c>favela-pixelart-standards</c> exige.
    ///
    /// <para><b>Por que a skill não bastava (medido em 2026-09-09).</b> Ela existe desde o
    /// começo do projeto e mesmo assim <b>12 dos 176 PNGs a violavam</b>, com 1056 testes
    /// verdes por cima. Três eram ícones de item em <b>Bilinear</b> — Alfanje, Coroa de Ossos e
    /// Maça —, ou seja, <b>borrados no inventário e na barra de ações</b>. Uma skill é conselho
    /// que eu preciso lembrar de seguir; isto aqui é portão.</para>
    ///
    /// <para><b>E o <c>IconesDosItensTests</c> já varria essa pasta exata</b> — e passava, porque
    /// olhava outras coisas. Cobrir a pasta não é cobrir a regra.</para>
    ///
    /// <para><b>A regra não é uma só, e é por isso que ela é difícil de escrever à mão.</b>
    /// <c>filterMode</c> e compressão valem para <b>toda</b> arte: borrar é borrar, em canvas ou
    /// no mundo. Já <c>PPU</c> só governa <b>sprite de mundo</b> — um <c>UnityEngine.UI.Image</c>
    /// é esticado pelo <c>RectTransform</c> e não liga para PPU nenhum. Uma regra chapada
    /// reprovaria a HUD inteira sem que nada estivesse errado.</para>
    ///
    /// <para><b>Arte não referenciada fica de fora</b>, de propósito: concept art e folhas
    /// aposentadas não chegam à tela, e reprová-las obrigaria a apagar material de referência ou
    /// a inchar a lista de exceções. No instante em que alguém usar o arquivo, ele passa a ser
    /// cobrado.</para>
    /// </summary>
    public sealed class ImportacaoDaPixelArtTests
    {
        private const string PastaDaArte = "Assets/FavelaAmarela";

        /// <summary>
        /// A pasta de ícones tem <b>dono</b>: <c>IconesDosItensTests</c>.
        ///
        /// <para><b>E ele me corrigiu.</b> Eu "consertei" três ícones de Bilinear para Point —
        /// Alfanje, Coroa de Ossos e Maça — achando que eram pixel art borrada. Não são:
        /// <b>são pintura reduzida de ~485 px</b>, e Point num ícone pintado serrilha as bordas
        /// suaves e fica <i>pior</i> que a fonte. O guarda de lá mantém, item por item e com a
        /// procedência de cada leva escrita, quais ícones são pixel art autorada (Point) e
        /// quais são pintura (Bilinear).</para>
        ///
        /// <para>Duplicar aquela lista aqui faria dois testes brigarem pelo mesmo arquivo, e o
        /// vencedor seria quem editasse por último. A regra tem um lugar só.</para>
        /// </summary>
        private const string PastaComDonoProprio = "Assets/FavelaAmarela/Art/Items/Icones";

        /// <summary>PPU do projeto. Um só, senão dois sprites lado a lado têm pixels de tamanhos diferentes.</summary>
        private const string PpuDoProjeto = "32";

        /// <summary>
        /// Violações aceitas, e <b>por quê</b>. Sem a razão escrita, uma lista de exceções vira
        /// o lugar onde se esconde o defeito — que é como os três ícones borrados sobreviveram.
        /// </summary>
        private static readonly Dictionary<string, string> Excecoes =
            new Dictionary<string, string>
            {
                ["Art/UI/Areia_Tempestade.png"] =
                    "É um UnityEngine.UI.Image num Canvas (o Véu da Tempestade, tela cheia, " +
                    "alpha animado). PPU não se aplica a UI, e o Bilinear é o que faz a bruma " +
                    "ser bruma em vez de um quadriculado sobre o Deserto.",

                ["Art/UI/Sprites/bar_background.png"] = "UI em canvas — PPU não se aplica.",
                ["Art/UI/Sprites/bar_fill.png"] = "UI em canvas — PPU não se aplica.",
                ["Art/UI/Sprites/panic_overlay.png"] = "UI em canvas — PPU não se aplica.",

                ["Art/Enemies/CoisaDoCemiterio.png"] =
                    "PPU 16 num sprite de MUNDO, o que é irregular de verdade — mas corrigir " +
                    "para 32 REDUZ A CRIATURA À METADE: ela mede 2,31 unidades hoje (1,09× o " +
                    "Damião) e cairia para 1,16 (0,55×). Para um caçador que mata por contato, " +
                    "isso é decisão de design, não de import. Fica registrado aqui até o Vini " +
                    "decidir. Efeito colateral conhecido: os pixels dela têm o dobro do tamanho " +
                    "dos do resto do elenco.",
            };

        /// <summary>
        /// GUIDs de sprite que alguma cena, prefab ou asset referencia — a arte que chega à tela.
        /// </summary>
        private static HashSet<string> ArteEmUso()
        {
            var usados = new HashSet<string>();

            var consumidores = new[] { "*.prefab", "*.unity", "*.asset" }
                .SelectMany(p => Directory.EnumerateFiles("Assets", p, SearchOption.AllDirectories));

            foreach (var arquivo in consumidores)
            {
                foreach (Match m in Regex.Matches(File.ReadAllText(arquivo), @"guid: ([0-9a-f]{32})"))
                    usados.Add(m.Groups[1].Value);
            }

            return usados;
        }

        [Test]
        public void TodaArteEmUso_ImportaComoPixelArt()
        {
            Assert.IsTrue(Directory.Exists(PastaDaArte), $"Pasta de arte ausente: {PastaDaArte}");

            var emUso = ArteEmUso();
            var falhas = new List<string>();
            int conferidos = 0;

            foreach (var png in Directory.EnumerateFiles(PastaDaArte, "*.png", SearchOption.AllDirectories))
            {
                if (png.Replace('\\', '/').StartsWith(PastaComDonoProprio)) continue;

                string meta = png + ".meta";
                string relativo = png.Replace('\\', '/').Substring(PastaDaArte.Length + 1);

                if (!File.Exists(meta))
                {
                    falhas.Add($"{relativo}: sem .meta — a Unity vai gerar um GUID novo na " +
                               "próxima abertura e toda referência a este sprite se perde");
                    continue;
                }

                string t = File.ReadAllText(meta);

                var guid = Regex.Match(t, @"(?m)^guid: ([0-9a-f]{32})");
                if (!guid.Success || !emUso.Contains(guid.Groups[1].Value)) continue;

                conferidos++;

                string ppu = Campo(t, @"(?m)^  spritePixelsToUnits: (-?\d+)");
                string filtro = Campo(t, @"(?m)^    filterMode: (-?\d+)");
                string compressao = Campo(t, @"(?m)^    textureCompression: (-?\d+)");

                var problemas = new List<string>();

                // Valem para TODA arte: borrar e comprimir estragam pixel art em canvas
                // tanto quanto no mundo.
                if (filtro != "0")
                    problemas.Add($"filterMode {NomeDoFiltro(filtro)} — a arte sai BORRADA");
                if (compressao != "0")
                    problemas.Add("compressão ligada — come as bordas e suja a paleta");

                // Só para sprite de mundo. Ver a doc da classe.
                if (ppu != PpuDoProjeto)
                    problemas.Add($"PPU {ppu} em vez de {PpuDoProjeto} — os pixels deste sprite " +
                                  "ficam de um tamanho diferente do resto do elenco");

                if (problemas.Count == 0) continue;

                if (Excecoes.TryGetValue(relativo, out var razao))
                {
                    // Exceção conhecida: não reprova, mas continua visível no log.
                    TestContext.WriteLine($"[exceção] {relativo}: {string.Join("; ", problemas)}");
                    TestContext.WriteLine($"          {razao}");
                    continue;
                }

                falhas.Add($"{relativo}: {string.Join("; ", problemas)}");
            }

            Assert.Greater(conferidos, 0,
                "Nenhuma arte em uso foi conferida. Ou a pasta mudou de lugar, ou a varredura " +
                "de consumidores parou de achar cena e prefab — e aí este guarda não guarda nada.");

            Assert.IsEmpty(falhas,
                "Arte em uso importada fora do padrão de pixel art:\n  " +
                string.Join("\n  ", falhas) +
                "\n\nConserto: selecione o arquivo no Project, e no Inspector ponha Filter Mode " +
                "= Point (no filter), Compression = None e Pixels Per Unit = 32. Se a violação " +
                "for deliberada, acrescente o arquivo em ImportacaoDaPixelArtTests.Excecoes " +
                "COM A RAZÃO — uma exceção sem razão escrita é onde o próximo defeito se " +
                "esconde.");
        }

        /// <summary>
        /// Todo tile de <b>chão</b> tem de gerar malha <c>FullRect</c>.
        ///
        /// <para><b>O que <c>Tight</c> faz e por que aqui é errado.</b> A doc da Unity define os
        /// dois: <i>FullRect — malha retangular igual ao tamanho da sprite</i>;
        /// <i>Tight — malha ajustada aos valores de alfa do pixel, cortando o máximo de pixels
        /// excedentes</i>. Um tile de chão isométrico é um <b>losango dentro de um retângulo
        /// 32 × 16</b>: os quatro cantos são transparentes. Com <c>Tight</c>, a malha para de ser
        /// a célula e passa a ser o losango — e duas células vizinhas encostam malha com malha,
        /// onde antes encostavam retângulo com retângulo. É de lá que sai o fio de fundo entre
        /// tiles.</para>
        ///
        /// <para><b>Medido em 2026-09-09:</b> 3 dos 14 sprites de <c>Art/Tiles</c> estavam em
        /// <c>Tight</c>. Dois eram chão — <c>arena_piso_placeholder</c>, que pinta as 4 096
        /// células da Arena de Testes, e <c>santuario_piso_placeholder</c> — e foram corrigidos.
        /// O terceiro é o <c>wall_stone</c>, e ele é a <b>exceção legítima</b>: pivô
        /// <c>BottomCenter</c>, ou seja, é parede e não chão. Parede não ladrilha lado a lado no
        /// plano do piso, e recortar a malha ao alfa lá poupa overdraw sem abrir costura.</para>
        ///
        /// <para>Por isso o guarda discrimina pelo <b>pivô</b>, e não por nome de arquivo: pivô
        /// central é chão, <c>BottomCenter</c> é coisa que fica em pé. É a mesma distinção que o
        /// resto do projeto já usa, e ela não envelhece quando alguém acrescentar um tile
        /// novo.</para>
        /// </summary>
        [Test]
        public void TileDeChao_UsaMalhaFullRect()
        {
            const string pastaDeTiles = "Assets/FavelaAmarela/Art/Tiles";

            Assert.IsTrue(Directory.Exists(pastaDeTiles),
                $"Pasta de tiles ausente: {pastaDeTiles}");

            var falhas = new List<string>();
            int conferidos = 0;

            foreach (var png in Directory.EnumerateFiles(pastaDeTiles, "*.png",
                                                         SearchOption.AllDirectories))
            {
                string meta = png + ".meta";
                if (!File.Exists(meta)) continue;

                string t = File.ReadAllText(meta);

                // Pivô central = chão. BottomCenter = parede, e aí Tight é escolha legítima.
                if (Campo(t, @"(?m)^  alignment: (-?\d+)") != "0") continue;

                conferidos++;

                if (Campo(t, @"(?m)^  spriteMeshType: (-?\d+)") == "0") continue;

                falhas.Add($"{Path.GetFileName(png)}: malha Tight num tile de chão — a malha " +
                           "vira o losango em vez da célula, e os vizinhos deixam de encostar");
            }

            Assert.Greater(conferidos, 0,
                "Nenhum tile de chão foi conferido. Ou a pasta mudou de lugar, ou todos os " +
                "tiles passaram a ter pivô BottomCenter — e aí a distinção deste guarda parou " +
                "de valer.");

            Assert.IsEmpty(falhas,
                "Tile de chão com malha Tight:\n  " + string.Join("\n  ", falhas) +
                "\n\nConserto: selecione o arquivo no Project e ponha Mesh Type = Full Rect no " +
                "Inspector.");
        }

        private static string Campo(string texto, string padrao)
        {
            var m = Regex.Match(texto, padrao);
            return m.Success ? m.Groups[1].Value : "?";
        }

        private static string NomeDoFiltro(string valor)
            => valor == "1" ? "Bilinear" : valor == "2" ? "Trilinear" : valor;
    }
}
