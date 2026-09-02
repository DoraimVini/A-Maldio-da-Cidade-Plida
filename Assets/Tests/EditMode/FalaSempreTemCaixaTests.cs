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
        [Test]
        public void OPromptDeInteracao_MoraNoHudEEmCenaNenhuma()
        {
            const string Hud = "Assets/FavelaAmarela/Resources/HUD_Gameplay.prefab";
            const string Script = "Assets/Scripts/UI/PromptDeInteracao.cs";

            Assert.IsTrue(File.Exists(Script), $"Script ausente: {Script}");

            var guid = Regex.Match(File.ReadAllText(Script + ".meta"), @"guid: ([0-9a-f]{32})");
            Assert.IsTrue(guid.Success, "PromptDeInteracao.cs.meta sem guid.");

            string marca = "guid: " + guid.Groups[1].Value;

            Assert.IsTrue(File.Exists(Hud), $"HUD ausente: {Hud}");
            StringAssert.Contains(marca, File.ReadAllText(Hud),
                "O HUD persistente não tem PromptDeInteracao — em cena nenhuma o jogador vê " +
                "'E — ...', e nada anuncia que baú, item ou NPC são interagíveis.");

            var emCena = Directory
                .GetFiles("Assets/Scenes", "*.unity", SearchOption.AllDirectories)
                .Where(c => File.ReadAllText(c).Contains(marca))
                .Select(Path.GetFileName)
                .ToList();

            Assert.IsEmpty(emCena,
                "Prompt de interação encontrado em cena: " + string.Join(", ", emCena) +
                Environment.NewLine +
                "Quem anuncia é o HUD persistente. Dois inscritos no mesmo detector escrevem a " +
                "mesma frase duas vezes, uma por cima da outra.");
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
