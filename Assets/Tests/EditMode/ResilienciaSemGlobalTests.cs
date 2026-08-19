using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a rodada final da refatoração de managers: <b>ninguém alcança a Resiliência Mental
    /// por um global</b>.
    ///
    /// <para><b>A causa raiz dos 19 call-sites</b> (achada em 2026-08-18): a Vitalidade tem uma
    /// <c>VitalidadeBridge</c> no Damião, então quem o atinge faz
    /// <c>GetComponentInParent&lt;VitalidadeBridge&gt;()</c>. A Resiliência <b>não tinha bridge
    /// nenhuma</b> — <c>GameManager.Instance.Resiliencia</c> era a única porta. Não foi descuido
    /// de quem escreveu; era o único caminho existente.</para>
    ///
    /// <para><b>Escritos ANTES da migração</b>, a pedido do plano: dois dos consumidores chamam
    /// dentro do <c>Update</c> (<c>GerenciadorEfeitosPassivos</c> e <c>PressaoPsiquicaZone</c>) e
    /// falham em <b>silêncio</b> se a ordem de bind sair errada — não estouram, só param de
    /// drenar. Sem rede, a regressão só apareceria em playtest, como "a Resiliência não cai
    /// mais".</para>
    /// </summary>
    public sealed class ResilienciaSemGlobalTests
    {
        private const string PastaDeScripts = "Assets/Scripts";

        private const string PrefabDoDamiao =
            "Assets/FavelaAmarela/Art/Characters/Damiao/Player_Damiao.prefab";

        /// <summary>
        /// Arquivos que podem citar <c>GameManager</c> legitimamente: a própria casca, e os
        /// utilitários de Editor que a manipulam por nome.
        /// </summary>
        private static readonly HashSet<string> Isentos = new HashSet<string>
        {
            "GameManager.cs",
        };

        // ── A bridge que faltava ─────────────────────────────────────────────

        [Test]
        public void ExisteUmaResilienciaBridge()
        {
            var achados = Directory.GetFiles(PastaDeScripts, "ResilienciaBridge.cs",
                SearchOption.AllDirectories);

            Assert.IsNotEmpty(achados,
                "Não existe ResilienciaBridge. É a peça que falta para a Resiliência ter o mesmo " +
                "tratamento da Vitalidade: um componente no Damião que qualquer atacante alcança " +
                "por GetComponentInParent, em vez de um singleton global.");
        }

        [Test]
        public void PlayerDamiao_TemResilienciaBridge()
        {
            Assert.IsTrue(File.Exists(PrefabDoDamiao), $"Prefab ausente: {PrefabDoDamiao}");

            string guid = GuidDoScript("ResilienciaBridge.cs");
            Assert.IsNotNull(guid, "ResilienciaBridge.cs ainda não existe.");

            string prefab = File.ReadAllText(PrefabDoDamiao);

            Assert.IsTrue(prefab.Contains(guid),
                "Player_Damiao.prefab está sem ResilienciaBridge. Sem ela, tudo que fere a mente " +
                "de Damião (Cone de Gelo, Coisa do Cemitério, Byakhee, zonas de pressão) volta a " +
                "precisar de um global. Mesmo modo de falha do Vigor, que só existia na Arena.");
        }

        // ── O global tem de sumir do código de produção ──────────────────────

        [Test]
        public void NenhumCodigoDeProducao_AlcancaGameManagerInstance()
        {
            var infratores = new List<string>();

            foreach (var arquivo in Directory.GetFiles(PastaDeScripts, "*.cs",
                         SearchOption.AllDirectories))
            {
                string nome = Path.GetFileName(arquivo);
                if (Isentos.Contains(nome)) continue;

                string codigo = SemComentarios(File.ReadAllText(arquivo));
                var achados = Regex.Matches(codigo, @"GameManager\s*\.\s*Instance");

                if (achados.Count > 0)
                    infratores.Add($"{nome} ({achados.Count}x)");
            }

            Assert.IsEmpty(infratores,
                "Código de produção ainda alcançando GameManager.Instance:\n  " +
                string.Join("\n  ", infratores) +
                "\n\nA Resiliência deve chegar por ResilienciaBridge (em quem atinge Damião, via " +
                "GetComponentInParent) ou por injeção do GameLoopBootstrap (em objeto de cena).");
        }

        /// <summary>
        /// A casca só pode sobreviver se ainda tiver função. Com zero consumidores, ela vira
        /// código morto num GameObject de 5 cenas — exatamente o tipo de coisa que esta
        /// refatoração existe para remover.
        /// </summary>
        [Test]
        public void GameManager_NaoTemMaisEncaminhamentosObsoletos()
        {
            var achados = Directory.GetFiles(PastaDeScripts, "GameManager.cs",
                SearchOption.AllDirectories);

            if (achados.Length == 0) Assert.Pass("GameManager já foi removido — fim da linha.");

            string codigo = SemComentarios(File.ReadAllText(achados[0]));
            var obsoletos = Regex.Matches(codigo, @"\[Obsolete");

            Assert.AreEqual(0, obsoletos.Count,
                $"GameManager ainda tem {obsoletos.Count} encaminhamento(s) [Obsolete]. " +
                "Com todos os consumidores migrados, eles não têm mais função — e a casca " +
                "inteira passa a ser candidata a remoção das 5 cenas.");
        }

        // ── Os dois consumidores de Update, que falham calados ───────────────

        /// <summary>
        /// <c>GerenciadorEfeitosPassivos</c> e <c>PressaoPsiquicaZone</c> drenam ou restauram
        /// Resiliência <b>a cada frame</b>. Se a fonte vier nula, os dois simplesmente não fazem
        /// nada — sem erro. Este teste exige que ambos resolvam a fonte <b>uma vez</b>, fora do
        /// <c>Update</c>, para que uma fonte ausente seja detectável no bind e não vire silêncio.
        /// </summary>
        [TestCase("GerenciadorEfeitosPassivos.cs")]
        [TestCase("PressaoPsiquicaZone.cs")]
        public void ConsumidorDeUpdate_NaoResolveFonteDentroDoUpdate(string nomeDoArquivo)
        {
            var achados = Directory.GetFiles(PastaDeScripts, nomeDoArquivo,
                SearchOption.AllDirectories);
            Assert.IsNotEmpty(achados, $"{nomeDoArquivo} não encontrado.");

            string fonte = File.ReadAllText(achados[0]);
            int inicio = fonte.IndexOf("void Update()");
            if (inicio < 0) Assert.Pass($"{nomeDoArquivo} não tem Update — nada a checar.");

            string corpo = SemComentarios(CorpoDoMetodo(fonte, inicio));

            foreach (var proibido in new[] { "GameManager.Instance", "GetComponentInParent",
                                             "FindAnyObjectByType", "FindObjectsByType" })
            {
                StringAssert.DoesNotContain(proibido, corpo,
                    $"'{proibido}' dentro do Update de {nomeDoArquivo}. Resolver a fonte por " +
                    "frame viola a Regra de Ouro 1 e transforma fonte ausente em silêncio — " +
                    "cacheie no Bind/Awake, onde dá para avisar.");
            }
        }

        // ── Apoio ────────────────────────────────────────────────────────────

        /// <summary>Do início do método até fechar as chaves, contando aninhamento.</summary>
        private static string CorpoDoMetodo(string fonte, int inicio)
        {
            int abre = fonte.IndexOf('{', inicio);
            if (abre < 0) return "";

            int nivel = 0;
            for (int i = abre; i < fonte.Length; i++)
            {
                if (fonte[i] == '{') nivel++;
                else if (fonte[i] == '}')
                {
                    nivel--;
                    if (nivel == 0) return fonte.Substring(inicio, i - inicio + 1);
                }
            }

            return fonte.Substring(inicio);
        }

        /// <summary>
        /// Remove linhas de comentário antes de procurar padrões proibidos — senão o guarda
        /// proíbe <b>documentar</b> o defeito que ele vigia. Lição de 2026-08-18: a primeira
        /// versão de <c>BarraDeItensInjetadaTests</c> falhou contra o código já corrigido porque
        /// o XML doc explicava o problema antigo.
        /// </summary>
        private static string SemComentarios(string fonte)
        {
            var sb = new System.Text.StringBuilder(fonte.Length);

            foreach (var linha in fonte.Split('\n'))
            {
                string t = linha.TrimStart();
                if (t.StartsWith("//") || t.StartsWith("*") || t.StartsWith("/*")) continue;
                sb.Append(linha).Append('\n');
            }

            return sb.ToString();
        }

        private static string GuidDoScript(string nomeDoArquivo)
        {
            var metas = Directory.GetFiles(PastaDeScripts, nomeDoArquivo + ".meta",
                SearchOption.AllDirectories);
            if (metas.Length == 0) return null;

            var m = Regex.Match(File.ReadAllText(metas[0]), @"guid:\s*([0-9a-f]{32})");
            return m.Success ? m.Groups[1].Value : null;
        }
    }
}
