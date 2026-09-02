using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda que <b>toda fala tem onde aparecer</b>.
    ///
    /// <para><b>O que motivou (2026-09-02).</b> O Vini relatou <i>"tem diálogos que estão sem
    /// caixa de texto da UI escolhida"</i>. Medido: dos 17 scripts que mostram fala pelo
    /// <c>TutorialHintUI</c>, <b>catorze</b> caíam para a instância global do HUD quando o campo
    /// do Inspector estava vazio — e <b>três não</b>: <c>ColetavelDeItem</c>,
    /// <c>FragmentoDeYhtill</c> e <c>YugNethArtesao</c>.</para>
    ///
    /// <para>Os campos desses três estavam <c>{fileID: 0}</c> em <b>100% das instâncias em
    /// disco</b> — 18 só no <c>Deserto_Hali</c>, mais 3 prefabs de item. Na prática: <b>pegar
    /// qualquer item no Deserto não mostrava caixa nenhuma</b>, e os Fragmentos de Yhtill não
    /// falavam. Sem erro, sem aviso: o <c>if (campo != null)</c> simplesmente não entrava.</para>
    ///
    /// <para>É o modo de falha assinado deste repositório — a peça existe, compila, e a ligação
    /// não acontece — na sua forma mais barata de evitar: uma linha por consumidor.</para>
    /// </summary>
    public sealed class FalaSempreTemCaixaTests
    {
        private const string Scripts = "Assets/Scripts";

        /// <summary>
        /// Quem tem campo de caixa e <b>não</b> precisa cair para a global, com a razão.
        /// </summary>
        private static readonly (string Arquivo, string Porque)[] Dispensados =
        {
            ("TutorialHintUI.cs", "é a própria caixa"),
        };

        [Test]
        public void TodoQuemMostraFala_CaiParaACaixaGlobal()
        {
            Assert.IsTrue(Directory.Exists(Scripts), $"Pasta ausente: {Scripts}");

            var semQueda = Directory
                .GetFiles(Scripts, "*.cs", SearchOption.AllDirectories)
                .Select(caminho => new { Caminho = caminho, Fonte = File.ReadAllText(caminho) })
                // Só quem GUARDA uma caixa: um campo do tipo TutorialHintUI.
                .Where(a => Regex.IsMatch(a.Fonte, @"\bTutorialHintUI\s+\w+\s*;"))
                .Where(a => !Dispensados.Any(d => Path.GetFileName(a.Caminho) == d.Arquivo))
                // A queda: qualquer menção a TutorialHintUI.Instancia no arquivo.
                .Where(a => !a.Fonte.Contains("TutorialHintUI.Instancia"))
                .Select(a => Path.GetFileName(a.Caminho))
                .ToList();

            Assert.IsEmpty(semQueda,
                "Script(s) que guardam uma caixa de fala e NÃO caem para a global: " +
                string.Join(", ", semQueda) + Environment.NewLine +
                "Quando o campo do Inspector está vazio — que é o caso normal, porque a caixa " +
                "vive num prefab-asset e não dá para arrastar — a fala é descartada em " +
                "SILÊNCIO." + Environment.NewLine +
                "Conserto: 'campo != null ? campo : TutorialHintUI.Instancia', resolvido no " +
                "momento do uso (a Instancia só existe depois do OnEnable do HUD).");
        }

        /// <summary>
        /// O <b>prompt de interação</b> mora no HUD persistente, e <b>em cena nenhuma</b>.
        ///
        /// <para><b>Medido em 2026-09-02:</b> ele existia em <b>uma cena das seis</b> do build.
        /// Nas outras cinco o jogador nunca via "E — Abrir o baú" — o objeto era interagível e
        /// não anunciava isso, incluindo o Baú de Yhtill que eu tinha acabado de pôr no
        /// Santuário.</para>
        ///
        /// <para>E <b>nenhum em cena</b>, não só "pelo menos um no HUD": dois prompts inscritos
        /// no mesmo detector escrevem a frase duas vezes.</para>
        /// </summary>
        /// <summary>
        /// Painéis de fala que <b>têm de morar no HUD persistente, e em cena nenhuma</b>, com o
        /// que cada um custava quando faltava.
        /// </summary>
        private static readonly (string Script, string Custo)[] SoNoHud =
        {
            ("Assets/Scripts/UI/PromptDeInteracao.cs",
             "existia em UMA cena das seis: nas outras cinco o jogador nunca via 'E — ...', e " +
             "nada anunciava que baú, item ou NPC eram interagíveis"),

            ("Assets/Scripts/UI/PainelDeEscolha.cs",
             "existia em DUAS cenas das seis, e o CassildaNPC pula a ramificação EM SILÊNCIO " +
             "quando ele falta — a conversa acontece pela metade e nada reclama"),
        };

        [Test]
        public void OsPaineisDeFala_MoramNoHudEEmCenaNenhuma()
        {
            const string Hud = "Assets/FavelaAmarela/Resources/HUD_Gameplay.prefab";
            Assert.IsTrue(File.Exists(Hud), $"HUD ausente: {Hud}");

            string hud = File.ReadAllText(Hud);
            var cenas = Directory.GetFiles("Assets/Scenes", "*.unity", SearchOption.AllDirectories);

            var problemas = new System.Collections.Generic.List<string>();

            foreach (var (script, custo) in SoNoHud)
            {
                Assert.IsTrue(File.Exists(script), $"Script ausente: {script}");

                var guid = Regex.Match(File.ReadAllText(script + ".meta"), @"guid: ([0-9a-f]{32})");
                Assert.IsTrue(guid.Success, $"{Path.GetFileName(script)}.meta sem guid.");

                string marca = "guid: " + guid.Groups[1].Value;
                string nome = Path.GetFileNameWithoutExtension(script);

                if (!hud.Contains(marca))
                    problemas.Add($"{nome} NÃO está no HUD persistente — {custo}");

                var emCena = cenas.Where(c => File.ReadAllText(c).Contains(marca))
                                  .Select(Path.GetFileName)
                                  .ToList();

                if (emCena.Count > 0)
                    problemas.Add($"{nome} ainda está em cena ({string.Join(", ", emCena)}) — " +
                                  "dois deles disputam a mesma Instancia e o jogador vê a coisa " +
                                  "duas vezes");
            }

            Assert.IsEmpty(problemas,
                "Painel(is) de fala fora do lugar:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", problemas) + Environment.NewLine +
                "Um prefab-asset não referencia objeto de cena: quem entrega as referências do " +
                "jogador é o GameLoopBootstrap, por Bind().");
        }

        /// <summary>
        /// O outro lado: um dispensado que deixou de guardar caixa some da lista, senão ela vira
        /// ficção e o próximo a ler acredita.
        /// </summary>
        [Test]
        public void NenhumDispensado_DeixouDeGuardarCaixa()
        {
            var obsoletos = Dispensados
                .Where(d =>
                {
                    var achados = Directory.GetFiles(Scripts, d.Arquivo, SearchOption.AllDirectories);
                    if (achados.Length == 0) return true;
                    return !Regex.IsMatch(File.ReadAllText(achados[0]),
                                          @"\bTutorialHintUI\s+\w+\s*;|class TutorialHintUI");
                })
                .Select(d => d.Arquivo)
                .ToList();

            Assert.IsEmpty(obsoletos,
                "Dispensado(s) que não guardam mais caixa: " + string.Join(", ", obsoletos) +
                ". Remova de FalaSempreTemCaixaTests.Dispensados.");
        }
    }
}
