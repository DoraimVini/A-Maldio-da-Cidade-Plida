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
        private const string CenaPortoes = "Assets/Scenes/Portoes_Das_Ruinas.unity";
        private const string BuildSettings = "ProjectSettings/EditorBuildSettings.asset";

        /// <summary>
        /// O Castelo tem que ser alcançável <b>pelos Portões das Ruínas</b>, e por mais nenhum
        /// caminho.
        ///
        /// <para><b>Este teste já exigiu o contrário.</b> Até 2026-08-19 ele cobrava um portal
        /// Santuário → Castelo, porque o Castelo era cena solta e um atalho direto era melhor que
        /// nada. Com os Portões em cena, o atalho virou defeito: ele pula o Byakhee, que é a
        /// <b>única fonte do Anel do Sinal Amarelo</b> — uma das três relíquias do rito. Um
        /// caminho que leva ao chefe final sem o que é preciso para vencê-lo, e sem avisar
        /// disso. Por isso o teste agora <b>proíbe</b> o que antes exigia.</para>
        /// </summary>
        [Test]
        public void ACena_ExisteEstaNaBuildEAlcancavelSoPelosPortoes()
        {
            Assert.IsTrue(File.Exists(Cena),
                "Castelo_Carcosa.unity não existe. Rode 'Tools/FavelaAmarela/Montar Castelo de " +
                "Carcosa' — e confira o disco, não o log: a ferramenta já reportou sucesso uma " +
                "vez sem ter salvado nada.");

            Assert.IsTrue(File.ReadAllText(BuildSettings).Contains("Castelo_Carcosa.unity"),
                "O Castelo não está no Build Settings — a cena existiria mas nenhuma build a " +
                "carregaria.");

            CollectionAssert.Contains(DestinosDe(CenaPortoes), "Castelo_Carcosa",
                "Nenhum PortalDeCena dos Portões das Ruínas leva ao Castelo — a fase final " +
                "ficaria inalcançável em jogo.");

            CollectionAssert.DoesNotContain(DestinosDe(CenaSantuario), "Castelo_Carcosa",
                "O Santuário ainda tem o atalho para o Castelo. Ele pula o Byakhee, que é a " +
                "única fonte do Anel do Sinal Amarelo — o jogador chegaria ao Rei sem poder " +
                "selá-lo. Rode 'Tools/FavelaAmarela/Montar Castelo de Carcosa', que remove.");
        }

        private static List<string> DestinosDe(string cena)
        {
            Assert.IsTrue(File.Exists(cena), $"Cena ausente: {cena}");

            return Regex.Matches(File.ReadAllText(cena), @"cenaDestino:\s*(\S+)")
                        .Cast<Match>()
                        .Select(m => m.Groups[1].Value)
                        .ToList();
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

        /// <summary>
        /// O chão do Castelo é <b>isométrico</b>, como o do resto do jogo.
        ///
        /// <para><b>O defeito que motivou:</b> a primeira versão desenhava cada sala como um
        /// <c>SpriteRenderer</c> retangular em espaço de mundo — o Castelo era <b>top-down</b>
        /// enquanto Deserto, Santuário e Portões usam <c>Grid</c> isométrico com
        /// <c>cellSize (1, 0.5)</c>. Relatado pelo Vini olhando a cena; nenhum teste percebia,
        /// porque todos verificavam <i>o que existe</i> e nenhum verificava <i>em que projeção
        /// está desenhado</i>.</para>
        ///
        /// <para><c>m_CellLayout: 2</c> é <c>GridLayout.CellLayout.Isometric</c> (Rectangle é 0).
        /// Testar o número, e não só a presença do <c>Grid</c>, é o que separa "tem grid" de
        /// "tem grid isométrico" — um <c>Grid</c> retangular passaria no primeiro.</para>
        /// </summary>
        [Test]
        public void OChaoDoCastelo_EIsometricoComoOResto()
        {
            string txt = File.ReadAllText(Cena);

            Assert.IsTrue(Regex.IsMatch(txt, @"(?m)^Grid:\s*$"),
                "O Castelo não tem componente Grid — o chão voltou a ser SpriteRenderer " +
                "retangular, ou seja, top-down. Rode 'Tools/FavelaAmarela/Montar Castelo de " +
                "Carcosa'.");

            Assert.IsTrue(Regex.IsMatch(txt, @"m_CellLayout:\s*2"),
                "O Grid do Castelo não está em cellLayout Isometric (2). Um Grid retangular " +
                "desenha o mesmo mundo em projeção errada.");

            Assert.IsTrue(Regex.IsMatch(txt, @"m_CellSize:\s*\{x:\s*1,\s*y:\s*0\.5"),
                "cellSize do Castelo não é (1, 0.5) — a proporção 2:1 é o que faz o losango " +
                "isométrico do projeto (skill favela-isometric-standards).");

            // A colisão vem de um TilemapCollider2D sobre as células de borda. Sem ele o
            // jogador anda para fora do chão — e sem as células, o colisor não gera geometria
            // nenhuma (a armadilha do colliderType, paga com um playtest na Arena de Testes).
            Assert.IsTrue(txt.Contains("TilemapCollider2D"),
                "O Castelo não tem TilemapCollider2D — o chão existiria sem nada segurando o " +
                "jogador dentro dele.");
        }

        /// <summary>
        /// Vencer o Rei tem que <b>fazer alguma coisa</b>.
        ///
        /// <para><b>O buraco que motivou:</b> <c>ReiEmAmareloAI.OnVitoria</c> passou a existir
        /// com o comentário "quem monta a cena decide o que fazer com isso" — e ninguém decidia.
        /// O evento tinha <b>zero assinantes</b>. Completar o rito, o clímax do Vertical Slice,
        /// só repintava o Rei; o jogo seguia rodando, indiferente.</para>
        ///
        /// <para><b>Por que nenhum outro teste pegava:</b> um evento C# sem assinante é
        /// perfeitamente válido. Não há exceção, não há aviso, não há linha no console. Compila,
        /// roda, e não acontece nada — a forma mais silenciosa do modo de falha assinatura deste
        /// projeto, na última cena do jogo.</para>
        /// </summary>
        [Test]
        public void VencerORei_TemConsequencia()
        {
            string guid = GuidDoScript("SequenciaDeSelamento");
            Assert.IsNotNull(guid,
                "Script SequenciaDeSelamento não existe — ninguém consome ReiEmAmareloAI.OnVitoria.");

            string txt = File.ReadAllText(Cena);

            Assert.IsTrue(txt.Contains(guid),
                "O Castelo não tem SequenciaDeSelamento. O evento OnVitoria do Rei ficaria sem " +
                "assinante e selá-lo não faria nada. Rode 'Tools/FavelaAmarela/Montar Castelo " +
                "de Carcosa'.");

            // Presença não basta: o componente precisa apontar para o Rei e para o painel.
            // Um SequenciaDeSelamento com campos nulos loga erro em Awake e, no melhor caso,
            // some do console no meio de um playtest.
            var doc = Regex.Match(txt,
                $@"---\s*!u!114\s*&-?\d+\r?\n(?:(?!^---)[\s\S])*?{guid}(?:(?!^---)[\s\S])*",
                RegexOptions.Multiline);

            Assert.IsTrue(doc.Success, "Componente SequenciaDeSelamento ilegível no YAML.");

            var falhas = new List<string>();
            foreach (var campo in new[] { "rei", "painel", "texto" })
            {
                var m = Regex.Match(doc.Value, $@"(?m)^\s*{campo}:\s*\{{fileID:\s*(-?\d+)");

                if (!m.Success) falhas.Add($"{campo}: ausente do YAML");
                else if (m.Groups[1].Value == "0") falhas.Add($"{campo}: nulo");
            }

            Assert.IsEmpty(falhas,
                "SequenciaDeSelamento com referência solta — o desfecho existiria pela metade:\n  " +
                string.Join("\n  ", falhas));
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
