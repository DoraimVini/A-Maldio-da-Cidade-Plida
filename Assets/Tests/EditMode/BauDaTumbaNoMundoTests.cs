using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda os baús do mundo: todo <c>BauDaTumba</c> em cena precisa ter <b>alguma</b> fonte de
    /// arma ligada.
    ///
    /// <para><b>Por que existe (2026-08-14):</b> o baú de <c>Playtest_RuinasPalidas</c> estava com
    /// <c>tabela</c> em <c>fileID: 0</c>, sem <c>forcarArma</c>. O código estava correto, o asset
    /// <c>Drop_BauDaTumba</c> existia e era válido — só ninguém tinha ligado os dois. Abrir o baú
    /// gastava a interação, registrava erro no console e não entregava nada; para quem joga, "a
    /// arma e o inventário estão quebrados".</para>
    ///
    /// <para>É a nona ocorrência catalogada do modo de falha dominante deste projeto: código que
    /// existe e não está ligado. <c>SorteioDeDropTests</c> e <c>TabelaDeDropAssetsTests</c> já
    /// cobriam o sorteio e os assets — nenhum dos dois olhava a <b>cena</b>, que era exatamente
    /// onde o elo faltava.</para>
    /// </summary>
    public sealed class BauDaTumbaNoMundoTests
    {
        private const string PastaDeCenas = "Assets/Scenes/";

        [Test]
        public void TodoBauEmCena_TemFonteDeArmaLigada()
        {
            string guidDoBau = GuidDoScript("BauDaTumba.cs");
            Assert.IsNotNull(guidDoBau, "BauDaTumba.cs sem .meta — o guarda não tem o que procurar.");

            var quebrados = new List<string>();
            int bausEncontrados = 0;

            foreach (var cena in Directory.GetFiles(PastaDeCenas, "*.unity"))
            {
                foreach (var bloco in BlocosDoScript(cena, guidDoBau))
                {
                    bausEncontrados++;

                    bool temTabela = ReferenciaDe(bloco, "tabela") != "0";
                    bool forcando = ValorInteiro(bloco, "forcarArma") == 1
                                    && ReferenciaDe(bloco, "armaForcada") != "0";

                    if (!temTabela && !forcando)
                        quebrados.Add($"{Path.GetFileName(cena)}: baú sem 'tabela' e sem " +
                                      "'forcarArma' + 'armaForcada'");
                }
            }

            Assert.IsEmpty(quebrados,
                "Baú sem fonte de arma. Abrir gasta a interação, registra erro no console e não " +
                "entrega nada — para quem joga, a arma e o inventário parecem quebrados. Ligue " +
                "'Assets/FavelaAmarela/Config/Drops/Drop_BauDaTumba.asset' no campo Tabela, ou " +
                "marque Forçar Arma com um ItemDef:\n" + string.Join("\n", quebrados));

            // Sem isto o teste passaria vazio caso o baú saísse de todas as cenas por engano —
            // verde por ausência é o tipo de garantia que não garante nada.
            Assert.Greater(bausEncontrados, 0,
                "Nenhum BauDaTumba em cena alguma. Se o baú foi removido de propósito, apague " +
                "este guarda; se não, a arma inicial da Tumba deixou de existir no mundo.");
        }

        /// <summary>
        /// Cenas jogáveis não podem começar com arma na mão. <c>Cena_ArenaDeTestes</c> fica de
        /// fora: lá o override existe para calibrar chefe, é o uso legítimo do campo.
        /// </summary>
        private static readonly string[] CenasQueComecamDesarmadas =
        {
            PastaDeCenas + "Deserto_Hali.unity",
            PastaDeCenas + "Playtest_RuinasPalidas.unity",
            PastaDeCenas + "Santuario_Yhtill.unity",
        };

        [TestCaseSource(nameof(CenasQueComecamDesarmadas))]
        public void CenaJogavel_NaoComecaComArmaDeTeste(string caminhoDaCena)
        {
            if (!File.Exists(caminhoDaCena)) Assert.Ignore("cena ausente");

            string conteudo = File.ReadAllText(caminhoDaCena);

            // Instância de prefab: o valor mora em m_Modifications, não num bloco de componente.
            var override_ = Regex.Match(conteudo,
                @"propertyPath:\s*armaInicialParaTeste\s*\r?\n\s*value:\s*(\d+)");

            int valor = override_.Success ? int.Parse(override_.Groups[1].Value) : 0;

            // E o caso de estar direto na cena, sem prefab.
            if (!override_.Success)
            {
                var direto = Regex.Match(conteudo, @"^\s*armaInicialParaTeste:\s*(\d+)",
                    RegexOptions.Multiline);
                if (direto.Success) valor = int.Parse(direto.Groups[1].Value);
            }

            Assert.AreEqual(0, valor,
                $"{Path.GetFileName(caminhoDaCena)}: 'armaInicialParaTeste' está em {valor}, não " +
                "em Nenhuma. Damião nasce armado sem ter aberto o baú, e essa arma é equipada " +
                "direto no Awake — ela NÃO entra no inventário, então some na troca de cena e " +
                "parece que 'a arma sumiu'. Override de teste esquecido: rode " +
                "'Tools/FavelaAmarela/Reparar Arma da Tumba'.");
        }

        // ── Apoio ────────────────────────────────────────────────────────────

        private static string GuidDoScript(string nomeDoArquivo)
        {
            var metas = Directory.GetFiles("Assets/Scripts", nomeDoArquivo + ".meta",
                SearchOption.AllDirectories);
            if (metas.Length == 0) return null;

            var m = Regex.Match(File.ReadAllText(metas[0]), @"guid:\s*([0-9a-f]{32})");
            return m.Success ? m.Groups[1].Value : null;
        }

        /// <summary>Todos os blocos YAML da cena cujo <c>m_Script</c> aponta para o GUID dado.</summary>
        private static IEnumerable<List<string>> BlocosDoScript(string caminhoDaCena, string guid)
        {
            var separador = new Regex(@"^--- !u!\d+ &\d+");
            List<string> atual = null;
            var blocos = new List<List<string>>();

            foreach (var linha in File.ReadAllLines(caminhoDaCena))
            {
                if (separador.IsMatch(linha))
                {
                    atual = new List<string>();
                    blocos.Add(atual);
                    continue;
                }
                atual?.Add(linha);
            }

            foreach (var bloco in blocos)
            {
                foreach (var linha in bloco)
                {
                    if (linha.Contains("m_Script:") && linha.Contains(guid))
                    {
                        yield return bloco;
                        break;
                    }
                }
            }
        }

        /// <summary><c>fileID</c> do campo, ou <c>"0"</c> quando desligado ou ausente.</summary>
        private static string ReferenciaDe(List<string> bloco, string campo)
        {
            var padrao = new Regex($@"^  {Regex.Escape(campo)}: \{{fileID: (-?\d+)");
            foreach (var linha in bloco)
            {
                var m = padrao.Match(linha);
                if (m.Success) return m.Groups[1].Value;
            }
            return "0";
        }

        private static int ValorInteiro(List<string> bloco, string campo)
        {
            var padrao = new Regex($@"^  {Regex.Escape(campo)}: (-?\d+)");
            foreach (var linha in bloco)
            {
                var m = padrao.Match(linha);
                if (m.Success && int.TryParse(m.Groups[1].Value, out int v)) return v;
            }
            return 0;
        }
    }
}
