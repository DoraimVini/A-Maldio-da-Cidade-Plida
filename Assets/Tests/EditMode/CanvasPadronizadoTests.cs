using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a padronização do <c>CanvasScaler</c> em cenas e prefabs.
    ///
    /// <para><b>O bug que motivou (2026-08-19):</b> a UI saía do enquadramento em jogo. As cinco
    /// cenas foram montadas por ferramentas diferentes e nunca tiveram o Canvas padronizado —
    /// <c>Deserto_Hali</c> estava em <c>ConstantPixelSize</c> (a UI não acompanha o viewport),
    /// duas cenas não tinham <c>CanvasScaler</c> nenhum, e a Arena usava referência 640×360
    /// contra 1920×1080 do menu.</para>
    ///
    /// <para><b>E a causa mais escondida:</b> o <c>HUD_ResilienciaBar.prefab</c> carrega
    /// <c>Canvas</c> próprio, com <c>ConstantPixelSize</c> a 800×600 <b>dentro do prefab</b>.
    /// Ele é instanciado nas três cenas jogáveis, então corrigir só o Canvas da cena deixava o
    /// sintoma igual — e teria dado a impressão de estar resolvido. Por isso este guarda cobre
    /// prefab, não só cena.</para>
    /// </summary>
    public sealed class CanvasPadronizadoTests
    {
        private const int UiScaleModeScaleWithScreenSize = 1;

        private static readonly string[] Cenas =
        {
            "Assets/Scenes/Cena_Menu.unity",
            "Assets/Scenes/Deserto_Hali.unity",
            "Assets/Scenes/Tumba_De_Alhazred.unity",
            "Assets/Scenes/Santuario_Yhtill.unity",
            "Assets/Scenes/Cena_ArenaDeTestes.unity",
        };

        private static readonly string[] PrefabsComCanvas =
        {
            "Assets/FavelaAmarela/Art/UI/HUD_ResilienciaBar.prefab",
        };

        /// <summary>
        /// Um <c>CanvasScaler</c> num arquivo YAML é o bloco que contém <c>m_UiScaleMode</c>.
        /// Blocos <c>PrefabInstance</c> podem <i>mencionar</i> nomes de campo em
        /// <c>m_Modifications</c> sem serem scalers — por isso o filtro é pelo campo real.
        /// </summary>
        private static IEnumerable<string> BlocosDeScaler(string conteudo) =>
            Regex.Split(conteudo, @"(?m)^--- ")
                 .Where(d => Regex.IsMatch(d, @"(?m)^\s+m_UiScaleMode:\s*\d+"));

        /// <param name="exigirScaler">
        /// Só para prefabs. <b>Cena sem CanvasScaler não é defeito por si só</b>:
        /// <c>Tumba_De_Alhazred</c> e <c>Santuario_Yhtill</c> não têm <c>Canvas</c> próprio —
        /// toda a UI delas vem do <c>HUD_ResilienciaBar.prefab</c>, que carrega o seu. Uma versão
        /// anterior deste teste exigia scaler em toda cena e reprovava as duas por algo que não
        /// deve existir; o que importa é que <b>todo scaler que existir</b> esteja no padrão.
        /// </param>
        private static void ConferirArquivo(string caminho, List<string> falhas, bool exigirScaler)
        {
            if (!File.Exists(caminho)) { falhas.Add($"{caminho}: ausente"); return; }

            string nome = Path.GetFileNameWithoutExtension(caminho);
            var blocos = BlocosDeScaler(File.ReadAllText(caminho)).ToList();

            if (blocos.Count == 0)
            {
                if (exigirScaler)
                    falhas.Add($"{nome}: nenhum CanvasScaler — a UI não vai escalar com a tela");
                return;
            }

            foreach (var b in blocos)
            {
                int modo = int.Parse(Regex.Match(b, @"m_UiScaleMode:\s*(\d+)").Groups[1].Value);
                if (modo != UiScaleModeScaleWithScreenSize)
                {
                    falhas.Add($"{nome}: uiScaleMode={modo} (esperado 1 = ScaleWithScreenSize). " +
                               "Em ConstantPixelSize a UI é desenhada em pixels fixos e estoura " +
                               "a borda em resoluções diferentes da de autoria.");
                    continue;
                }

                var rr = Regex.Match(b, @"m_ReferenceResolution:\s*\{x:\s*([\d.]+),\s*y:\s*([\d.]+)\}");
                if (!rr.Success || rr.Groups[1].Value != "1920" || rr.Groups[2].Value != "1080")
                    falhas.Add($"{nome}: referenceResolution=({rr.Groups[1].Value}," +
                               $"{rr.Groups[2].Value}), esperado (1920,1080) — telas em escalas " +
                               "diferentes fazem a mesma UI aparecer com tamanhos diferentes.");
            }
        }

        [Test]
        public void TodoCanvasScalerDeCena_EstaNoPadrao()
        {
            var falhas = new List<string>();
            foreach (var c in Cenas) ConferirArquivo(c, falhas, exigirScaler: false);

            Assert.IsEmpty(falhas,
                "Canvas fora do padrão. Rode 'Tools/FavelaAmarela/Padronizar Canvas e moldura " +
                "do menu'.\n  " + string.Join("\n  ", falhas));
        }

        /// <summary>
        /// Toda cena jogável precisa ter UI escalável <b>por algum caminho</b> — Canvas próprio
        /// ou o prefab do HUD. Uma cena sem nenhum dos dois não teria interface nenhuma.
        /// </summary>
        [Test]
        public void TodaCenaJogavel_TemUiEscalavel()
        {
            const string guidDoHud = "49c4f983dce7b5949ae8dc6113737890";

            var jogaveis = new[]
            {
                "Assets/Scenes/Deserto_Hali.unity",
                "Assets/Scenes/Tumba_De_Alhazred.unity",
                "Assets/Scenes/Santuario_Yhtill.unity",
            };

            var falhas = new List<string>();

            foreach (var caminho in jogaveis)
            {
                if (!File.Exists(caminho)) { falhas.Add($"{caminho}: ausente"); continue; }

                string txt = File.ReadAllText(caminho);
                bool temScalerProprio = BlocosDeScaler(txt).Any();
                bool temHud = txt.Contains(guidDoHud);

                if (!temScalerProprio && !temHud)
                    falhas.Add($"{Path.GetFileNameWithoutExtension(caminho)}: sem CanvasScaler " +
                               "próprio e sem o prefab do HUD — a cena não teria UI escalável.");
            }

            Assert.IsEmpty(falhas, string.Join("\n  ", falhas));
        }

        /// <summary>
        /// O prefab é o caso que engana: a cena pode estar certa e a UI continuar quebrada,
        /// porque o HUD traz o próprio Canvas junto.
        /// </summary>
        [Test]
        public void PrefabsComCanvas_TambemEstaoPadronizados()
        {
            var falhas = new List<string>();
            foreach (var p in PrefabsComCanvas) ConferirArquivo(p, falhas, exigirScaler: true);

            Assert.IsEmpty(falhas,
                "Prefab com Canvas fora do padrão — a cena pode estar correta e a UI continuar " +
                "estourando a borda por causa dele.\n  " + string.Join("\n  ", falhas));
        }

        /// <summary>
        /// O menu ficou de fora da passada de UI porque <c>AplicarCaraDaInterface</c> escolhe
        /// painéis por <b>nome</b>, e nenhum nome do menu estava na lista dela. O log daquela
        /// rodada registrou "Cena_Menu: 0 painel(is)" e ninguém leu.
        /// </summary>
        [Test]
        public void CenaMenu_UsaAMolduraDoDarkAgesUI()
        {
            const string cena = "Assets/Scenes/Cena_Menu.unity";
            const string tilesheet =
                "Assets/ThirdParty/DarkAgesUI/DarkAgesUi_v1.0/32x32-Tilesheet.png.meta";

            Assert.IsTrue(File.Exists(cena), $"Cena ausente: {cena}");
            Assert.IsTrue(File.Exists(tilesheet), $"Tilesheet ausente: {tilesheet}");

            string guid = Regex.Match(File.ReadAllText(tilesheet),
                                      @"(?m)^guid:\s*([0-9a-f]{32})").Groups[1].Value;

            Assert.IsTrue(File.ReadAllText(cena).Contains(guid),
                "Cena_Menu não referencia o tilesheet do Dark Ages UI — os botões e painéis " +
                "voltaram a ser retângulos chapados com o sprite embutido da Unity.");
        }
    }
}
