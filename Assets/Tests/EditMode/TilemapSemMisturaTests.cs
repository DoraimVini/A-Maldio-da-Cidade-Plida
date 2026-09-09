using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda contra pintar o <b>mesmo tilemap</b> com um Rule Tile <b>e</b> com as sprites que
    /// aquele mesmo Rule Tile já sorteia.
    ///
    /// <para><b>Por que isso é uma armadilha e não só desarrumação.</b> Um
    /// <c>IsometricRuleTile</c> decide o que desenhar olhando os vizinhos e perguntando "este
    /// vizinho sou eu?". Uma célula pintada com <c>sand_02</c> cru <b>não é</b> a
    /// <c>RuleTile_Areia</c>, mesmo desenhando um pixel idêntico. Hoje isso não aparece, porque
    /// a regra da areia não tem nenhuma condição de vizinhança — tem uma regra só, com saída
    /// aleatória sobre 5 sprites.</para>
    ///
    /// <para><b>No dia em que alguém acrescentar uma regra de borda</b> — que é exatamente o que
    /// se quer de um Rule Tile — cada célula crua vira um buraco no terreno aos olhos da regra:
    /// as vizinhas dela passam a desenhar borda para dentro, contornando pedaços de chão que
    /// visualmente são chão. É a costura que o Rule Tile existia para evitar, fabricada pela
    /// mistura.</para>
    ///
    /// <para><b>O estado que originou (2026-09-09).</b> O <c>DesertFloor</c> do Deserto de Hali
    /// tinha 25 252 células: <b>19 512 (77,3%) pela <c>RuleTile_Areia</c> e 5 740 (22,7%) com
    /// <c>sand_01/02/03/pebbles/crack</c> cruas</b> — e a regra sorteia exatamente essas cinco
    /// sprites, então as duas metades desenhavam a mesma coisa. A Tumba pintava 1 111 células só
    /// com areia crua, sem Rule Tile nenhum.</para>
    ///
    /// <para><b>Consolidado no mesmo dia</b>, com autorização do Vini, por
    /// <c>ConsolidarOChaoDeAreia</c>: 6 851 células passaram para a regra e os dois pisos foram
    /// conferidos no disco em 100%. A conversão muda <b>qual</b> das cinco sprites cai em cada
    /// célula — a paleta é a mesma, o arranjo não —, e por isso era decisão dele.</para>
    ///
    /// <para><b>Este teste congela a lista, não proíbe.</b> Hoje ela está vazia. O que o guarda
    /// impede é a mistura <b>voltar</b>: um tilemap novo nessa condição reprova, e uma entrada
    /// que já foi consolidada e ficou na lista também reprova, cobrando a saída dela.</para>
    /// </summary>
    public sealed class TilemapSemMisturaTests
    {
        /// <summary>
        /// Tilemaps que hoje misturam Rule Tile com as sprites dele, e o custo de cada um.
        /// A lista existe para <b>encolher</b>.
        /// </summary>
        private static readonly Dictionary<string, string> MisturaConhecida =
            new Dictionary<string, string>();
        // VAZIA, e assim deve continuar. Ela nasceu com uma entrada — o DesertFloor do Deserto,
        // 77,3% RuleTile_Areia e 22,7% areia crua no mesmo tilemap — e o Vini autorizou a
        // consolidação no mesmo dia: 6 851 células (5 740 no Deserto, 1 111 na Tumba) passaram
        // para a regra, e os dois pisos foram conferidos no disco em 100%.
        //
        // Se algo voltar para cá, escreva a razão junto. Uma exceção sem razão escrita é onde o
        // próximo defeito se esconde.

        [Test]
        public void NenhumTilemapNovo_MisturaRuleTileComAsSpritesDele()
        {
            var poolPorRuleTile = PoolsDeRuleTile();

            Assert.IsNotEmpty(poolPorRuleTile,
                "Nenhum Rule Tile encontrado. Ou eles sumiram do projeto, ou a leitura do asset " +
                "quebrou — e sem ela este guarda não guarda nada.");

            var spriteDoTile = SpritePorTileSimples();
            var misturaAgora = new Dictionary<string, string>();

            foreach (var cena in CenasDoBuild())
            {
                string nomeDaCena = Path.GetFileNameWithoutExtension(cena);
                string yaml = File.ReadAllText(cena);
                var blocos = yaml.Split(new[] { "--- " }, System.StringSplitOptions.None);

                var nomes = new Dictionary<string, string>();
                foreach (var bloco in blocos)
                {
                    var id = Regex.Match(bloco, @"^!u!1 &(\d+)");
                    if (!id.Success) continue;
                    var nome = Regex.Match(bloco, @"m_Name: (.+)");
                    if (nome.Success) nomes[id.Groups[1].Value] = nome.Groups[1].Value.Trim();
                }

                foreach (var bloco in blocos)
                {
                    if (!Regex.IsMatch(bloco, @"^!u!1839735485 &\d+")) continue;

                    int inicio = bloco.IndexOf("m_TileAssetArray", System.StringComparison.Ordinal);
                    int fim = bloco.IndexOf("m_TileSpriteArray", System.StringComparison.Ordinal);
                    if (inicio < 0 || fim <= inicio) continue;

                    // O ARRAY não é o uso. Unity mantém a entrada em m_TileAssetArray depois
                    // que a última célula daquele tile é repintada — com refcount zero, até
                    // alguém compactar o tilemap. A primeira versão deste teste lia só o array e
                    // acusou os dois DesertFloor de misturar 5 tiles crus MINUTOS DEPOIS de a
                    // consolidação os deixar em 100% RuleTile_Areia, conferido no disco.
                    //
                    // Quem diz uso é m_TileIndex, que é o índice DENTRO do array gravado por
                    // célula pintada. Só entra na conta quem tem pelo menos uma célula.
                    var array = Regex.Matches(bloco.Substring(inicio, fim - inicio),
                                              @"m_Data: \{fileID: -?\d+, guid: ([0-9a-f]{32})")
                        .Cast<Match>().Select(m => m.Groups[1].Value).ToList();

                    var comCelula = new HashSet<int>(
                        Regex.Matches(bloco, @"m_TileIndex: (\d+)")
                            .Cast<Match>().Select(m => int.Parse(m.Groups[1].Value)));

                    var usados = comCelula
                        .Where(i => i < array.Count)
                        .Select(i => array[i])
                        .ToList();

                    // Que sprites os Rule Tiles deste tilemap reivindicam?
                    var reivindicadas = new HashSet<string>();
                    foreach (var g in usados)
                        if (poolPorRuleTile.TryGetValue(g, out var pool))
                            reivindicadas.UnionWith(pool);

                    if (reivindicadas.Count == 0) continue;

                    var colidem = usados
                        .Where(g => spriteDoTile.TryGetValue(g, out var s) && reivindicadas.Contains(s))
                        .ToList();

                    if (colidem.Count == 0) continue;

                    var dono = Regex.Match(bloco, @"m_GameObject: \{fileID: (\d+)\}");
                    string nomeDoMapa = dono.Success && nomes.TryGetValue(dono.Groups[1].Value, out var n)
                        ? n : "(?)";

                    misturaAgora[$"{nomeDaCena}/{nomeDoMapa}"] =
                        $"{colidem.Count} tile(s) cru(s) desenhando sprite que um Rule Tile do " +
                        "mesmo tilemap já sorteia";
                }
            }

            var novos = misturaAgora.Keys.Where(k => !MisturaConhecida.ContainsKey(k)).ToList();
            var resolvidos = MisturaConhecida.Keys.Where(k => !misturaAgora.ContainsKey(k)).ToList();

            var falhas = new List<string>();

            foreach (var k in novos)
                falhas.Add($"NOVA mistura: {k} — {misturaAgora[k]}");

            foreach (var k in resolvidos)
                falhas.Add($"JÁ CONSOLIDADO, mas continua na lista: {k} — tire-o de " +
                           "MisturaConhecida, senão ela vira depósito");

            Assert.IsEmpty(falhas,
                string.Join("\n  ", falhas) +
                "\n\nUma célula pintada com a sprite crua NÃO é o Rule Tile, por mais idêntica " +
                "que pareça. Enquanto a regra não tem condição de vizinhança isso não aparece; " +
                "no dia em que tiver, cada célula crua vira um buraco no terreno aos olhos dela.");
        }

        private static IEnumerable<string> CenasDoBuild()
        {
            string build = File.ReadAllText("ProjectSettings/EditorBuildSettings.asset");

            foreach (Match m in Regex.Matches(build, @"path: (Assets/Scenes/[^\s]+\.unity)"))
                if (File.Exists(m.Groups[1].Value))
                    yield return m.Groups[1].Value;
        }

        /// <summary>GUID do Rule Tile → GUIDs das sprites que ele pode desenhar.</summary>
        private static Dictionary<string, HashSet<string>> PoolsDeRuleTile()
        {
            var pools = new Dictionary<string, HashSet<string>>();

            foreach (var asset in Directory.EnumerateFiles("Assets", "*.asset",
                                                           SearchOption.AllDirectories))
            {
                string t = File.ReadAllText(asset);
                if (!t.Contains("RuleTile")) continue;
                if (!File.Exists(asset + ".meta")) continue;

                var guid = Regex.Match(File.ReadAllText(asset + ".meta"),
                                       @"(?m)^guid: ([0-9a-f]{32})");
                if (!guid.Success) continue;

                var sprites = Regex.Matches(t, @"guid: ([0-9a-f]{32}), type: 3")
                    .Cast<Match>().Select(m => m.Groups[1].Value).ToHashSet();

                if (sprites.Count > 0) pools[guid.Groups[1].Value] = sprites;
            }

            return pools;
        }

        /// <summary>GUID de um <c>Tile</c> comum → GUID da sprite única que ele desenha.</summary>
        private static Dictionary<string, string> SpritePorTileSimples()
        {
            var mapa = new Dictionary<string, string>();

            foreach (var asset in Directory.EnumerateFiles("Assets", "*.asset",
                                                           SearchOption.AllDirectories))
            {
                string t = File.ReadAllText(asset);
                if (t.Contains("RuleTile")) continue;

                var sprite = Regex.Match(t, @"m_Sprite: \{fileID: -?\d+, guid: ([0-9a-f]{32})");
                if (!sprite.Success || !File.Exists(asset + ".meta")) continue;

                var guid = Regex.Match(File.ReadAllText(asset + ".meta"),
                                       @"(?m)^guid: ([0-9a-f]{32})");
                if (guid.Success) mapa[guid.Groups[1].Value] = sprite.Groups[1].Value;
            }

            return mapa;
        }
    }
}
