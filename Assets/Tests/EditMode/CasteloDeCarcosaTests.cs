using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda o <b>Castelo de Carcosa</b> — a última fase do Vertical Slice.
    ///
    /// <para><b>O que motivou:</b> o levantamento de 2026-08-19 mostrou o Castelo no estado
    /// assinatura deste projeto — <c>PressaoPsiquicaZone</c>, <c>CortesaoPalido</c>,
    /// <c>EcoDeCarcosa</c>, <c>PontoFocalDeReliquia</c> e <c>DetectorDeCostas</c> todos
    /// escritos e <b>em cena nenhuma</b>. Estes testes existem para a fase não voltar a ser
    /// código sem mundo.</para>
    ///
    /// <para><b>E para o log não mentir de novo:</b> na primeira execução da ferramenta,
    /// <c>EditorSceneManager.SaveScene</c> falhou em silêncio, o método seguiu adiante e o log
    /// anunciou "Cena montada" — com o Build Settings e o portal do Santuário já apontando para
    /// um arquivo que não existia. O primeiro teste abaixo é exatamente esse par: a cena no
    /// disco <b>e</b> quem aponta para ela.</para>
    /// </summary>
    public sealed class CasteloDeCarcosaTests
    {
        private const string Cena = "Assets/Scenes/Castelo_Carcosa.unity";
        private const string CenaSantuario = "Assets/Scenes/Santuario_Yhtill.unity";
        private const string BuildSettings = "ProjectSettings/EditorBuildSettings.asset";

        [Test]
        public void ACena_ExisteEstaNaBuildEAlcancavelPeloSantuario()
        {
            Assert.IsTrue(File.Exists(Cena),
                "Castelo_Carcosa.unity não existe. Rode 'Tools/FavelaAmarela/Montar Castelo de " +
                "Carcosa' — e confira o disco, não o log: a ferramenta já reportou sucesso uma " +
                "vez sem ter salvado nada.");

            Assert.IsTrue(File.ReadAllText(BuildSettings).Contains("Castelo_Carcosa.unity"),
                "O Castelo não está no Build Settings — a cena existiria mas nenhuma build a " +
                "carregaria.");

            // O portal do Santuário é o único caminho até o Castelo. Sem ele, a fase final só
            // seria alcançável abrindo a cena no Editor.
            var portais = Regex.Matches(File.ReadAllText(CenaSantuario), @"cenaDestino:\s*(\S+)")
                               .Cast<Match>()
                               .Select(m => m.Groups[1].Value)
                               .ToList();

            CollectionAssert.Contains(portais, "Castelo_Carcosa",
                "Nenhum PortalDeCena do Santuário aponta para Castelo_Carcosa — o Castelo " +
                "ficaria inalcançável em jogo.");
        }

        [Test]
        public void AsQuatroZonasDoCaminhoCritico_EstaoNaCena()
        {
            string txt = File.ReadAllText(Cena);

            // Z4 (Observatório) fica de fora por design — é dungeon opcional, aberta só com o
            // Set Lendário 4/4 (level_design_castelo_carcosa.md §3, Z4).
            foreach (var zona in new[] { "Z1_PortoesInternos", "Z2_SalaoDoBanquete",
                                          "Z3_BibliotecaEsquecida", "Z5_TronoDeAldebaran" })
            {
                Assert.IsTrue(Regex.IsMatch(txt, $@"(?m)^\s+m_Name:\s*{zona}\s*$"),
                    $"Zona '{zona}' ausente do Castelo.");
            }
        }

        [Test]
        public void OsSistemasDoCastelo_EstaoInstanciados()
        {
            string txt = File.ReadAllText(Cena);

            // Mínimos por sistema, do design: 2 Cortesãos patrulhando o Salão, 3 Espelhos com
            // Pressão Psíquica na Biblioteca, 2 Ecos, e uma marca de zona por sala.
            var esperado = new Dictionary<string, int>
            {
                { "CasteloDeCarcosaZone", 4 },
                { "PressaoPsiquicaZone", 3 },
                { "EcoDeCarcosa", 2 },
                { "CortesaoPalido", 2 },
                { "ReiEmAmareloAI", 1 },
                { "PontoDeChegada", 1 },
                { "RefugioDeLuz", 1 },
            };

            var falhas = new List<string>();

            foreach (var par in esperado)
            {
                string guid = GuidDoScript(par.Key);
                if (guid == null) { falhas.Add($"{par.Key}: script não encontrado"); continue; }

                int n = Regex.Matches(txt, Regex.Escape(guid)).Count;
                if (n < par.Value)
                    falhas.Add($"{par.Key}: {n} instância(s), esperado ao menos {par.Value}");
            }

            Assert.IsEmpty(falhas,
                "Sistemas do Castelo faltando na cena — código escrito e mundo vazio é o modo " +
                "de falha que este guarda existe para pegar:\n  " + string.Join("\n  ", falhas));
        }

        /// <summary>
        /// Um ponto focal por relíquia que o <b>Rei</b> exige — ele é a fonte da verdade do
        /// rito. Faltando um, o selamento nunca completa e o chefe fica invencível.
        ///
        /// <para><b>Divergência conhecida entre design e código:</b> o documento de level design
        /// fala de <b>4</b> relíquias (Anel, Coroa, Patuá, Necronomicon), mas
        /// <c>ReiEmAmareloAI</c> exige <b>3</b> — a Coroa de Ossos está de fora porque não tem
        /// fonte jogável (o roadmap já registrava isso). Este teste segue o <b>código</b>, como
        /// manda o <c>CLAUDE.md</c> §3.1 regra 4, e a divergência fica sinalizada aqui.</para>
        /// </summary>
        [Test]
        public void CadaReliquiaExigidaPeloRei_TemSeuPontoFocal()
        {
            string prefabDoRei = "Assets/FavelaAmarela/Art/Enemies/ReiEmAmarelo.prefab";
            Assert.IsTrue(File.Exists(prefabDoRei), $"Prefab ausente: {prefabDoRei}");

            var bloco = Regex.Match(File.ReadAllText(prefabDoRei),
                                    @"(?ms)idsDasReliquiasExigidas:\s*(.*?)(?=^\s{2}\w)");
            Assert.IsTrue(bloco.Success, "O prefab do Rei não serializa idsDasReliquiasExigidas.");

            var ids = Regex.Matches(bloco.Groups[1].Value, @"(?m)^\s*-\s*(\S+)\s*$")
                           .Cast<Match>()
                           .Select(m => m.Groups[1].Value)
                           .ToList();

            Assert.IsNotEmpty(ids, "O Rei não exige relíquia nenhuma — o rito não teria como começar.");

            string txt = File.ReadAllText(Cena);
            var faltando = ids
                .Where(id => !Regex.IsMatch(txt, $@"(?m)^\s+m_Name:\s*Ponto_Focal_{Regex.Escape(id)}\s*$"))
                .ToList();

            Assert.IsEmpty(faltando,
                $"Relíquias exigidas pelo Rei sem ponto focal no Trono: {string.Join(", ", faltando)}. " +
                "Sem todos, o rito de selamento nunca completa e o chefe fica invencível.");
        }

        private static string GuidDoScript(string nome)
        {
            var arquivo = Directory
                .EnumerateFiles("Assets/Scripts", nome + ".cs", SearchOption.AllDirectories)
                .FirstOrDefault();

            if (arquivo == null || !File.Exists(arquivo + ".meta")) return null;

            var m = Regex.Match(File.ReadAllText(arquivo + ".meta"), @"(?m)^guid:\s*([0-9a-f]{32})");
            return m.Success ? m.Groups[1].Value : null;
        }
    }
}
