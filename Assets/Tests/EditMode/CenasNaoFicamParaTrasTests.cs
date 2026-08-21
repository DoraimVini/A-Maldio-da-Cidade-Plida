using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda o modo de falha mais reincidente do projeto: <b>listas de cenas escritas à mão que
    /// param no tempo</b>.
    ///
    /// <para><b>Por que existe (2026-08-20):</b> quando os Portões das Ruínas e o Castelo de
    /// Carcosa nasceram, <b>cinco</b> listas de cenas já existiam e nenhuma foi atualizada:
    /// <c>BuildHUDCompleto.CenasDeJogo</c>, <c>HudCompletoTests</c>,
    /// <c>PadronizarCanvasDasCenas.Cenas</c>, <c>LigarSistemasNovos.Cenas</c> e
    /// <c>BootstrapDeCenaTests.CenasJogaveis</c>. Cada uma falhou em silêncio: a ferramenta
    /// rodava, logava sucesso, e simplesmente não tocava nas cenas que não conhecia.</para>
    ///
    /// <para>A consequência jogável foi grave e passou despercebida até o playtest: sem
    /// <c>EstadoPersistenteDaProgressao</c> e <c>…DoInventario</c>, o jogador chegava ao Byakhee
    /// <b>sem inventário</b>, e no Castelo as relíquias do rito <b>não sobreviviam à entrada no
    /// Trono</b> — o que tornava o Rei em Amarelo invencível por montagem, não por desenho.</para>
    ///
    /// <para><b>O que torna este guarda diferente das cinco listas:</b> ele <b>varre a pasta</b>
    /// em vez de enumerar cenas. Uma cena nova entra na cobertura no instante em que o arquivo
    /// existe. Só sai de propósito, por entrada justificada em <see cref="ForaDoMundo"/> — e
    /// escrever essa justificativa é o momento em que alguém pensa no assunto.</para>
    /// </summary>
    public sealed class CenasNaoFicamParaTrasTests
    {
        private const string PastaDeCenas = "Assets/Scenes";

        /// <summary>
        /// Cenas que legitimamente não montam mundo. Toda entrada precisa de motivo: a alternativa
        /// é alguém calar uma falha real acrescentando um nome aqui.
        /// </summary>
        private static readonly Dictionary<string, string> ForaDoMundo =
            new Dictionary<string, string>
            {
                ["Cena_Menu"] = "Menu principal: não tem mundo, jogador nem estado a persistir.",
                ["cena_1"] = "Legado abandonado, anterior à Tumba. Já documentado em " +
                             "BootstrapDeCenaTests.Cena1_EhLegadoAbandonado_NaoEntraNoGuarda.",
                ["Cena_ArenaDeTestes"] = "Arena de calibragem de chefe, fora do caminho do " +
                                          "jogador. Nasce e morre numa sessão; persistir estado " +
                                          "dela poluiria o save de verdade.",
            };

        /// <summary>
        /// Componentes sem os quais uma cena de mundo perde estado do jogador na transição.
        /// </summary>
        private static readonly string[] Exigidos =
        {
            "EstadoPersistenteDosArtefatos",
            "EstadoPersistenteDoInventario",
            "EstadoPersistenteDaProgressao",
            "GameLoopBootstrap",
        };

        [Test]
        public void TodaCenaDeMundo_CarregaOsSistemasDePersistencia()
        {
            var faltando = new List<string>();

            foreach (var cena in CenasDeMundo())
            {
                string yaml = File.ReadAllText(cena);
                string nome = Path.GetFileNameWithoutExtension(cena);

                foreach (var componente in Exigidos)
                {
                    string guid = GuidDoScript(componente);
                    Assert.IsNotNull(guid, $"Não achei o .meta de {componente}.cs — " +
                                            "o guarda não consegue se verificar.");

                    if (!Regex.IsMatch(yaml, "guid: " + guid))
                        faltando.Add($"{nome} não tem {componente}");
                }
            }

            Assert.IsEmpty(faltando,
                "Cena(s) de mundo sem os sistemas de persistência:\n  " +
                string.Join("\n  ", faltando) +
                "\n\nSe a cena é nova, provavelmente ela precisa entrar em " +
                "LigarSistemasNovos.Cenas (e possivelmente em BuildHUDCompleto.CenasDeJogo e " +
                "PadronizarCanvasDasCenas.Cenas). Se ela legitimamente não monta mundo, " +
                "declare-a em ForaDoMundo com o motivo.");
        }

        /// <summary>
        /// Impede que <see cref="ForaDoMundo"/> vire depósito: uma cena listada ali que não
        /// existe mais é sinal de que a lista parou no tempo — exatamente o defeito que este
        /// arquivo guarda.
        /// </summary>
        [Test]
        public void AsExcecoes_AindaCorrespondemACenasReais()
        {
            var existentes = Directory.GetFiles(PastaDeCenas, "*.unity")
                                      .Select(Path.GetFileNameWithoutExtension)
                                      .ToList();

            var fantasmas = ForaDoMundo.Keys.Where(k => !existentes.Contains(k)).ToList();

            Assert.IsEmpty(fantasmas,
                "Exceções apontando para cenas que não existem mais: " +
                string.Join(", ", fantasmas));
        }

        private static IEnumerable<string> CenasDeMundo()
        {
            var cenas = Directory.GetFiles(PastaDeCenas, "*.unity")
                                 .Where(c => !ForaDoMundo.ContainsKey(
                                            Path.GetFileNameWithoutExtension(c)))
                                 .ToList();

            Assert.Greater(cenas.Count, 0, "Nenhuma cena de mundo encontrada — " +
                                            "o guarda passaria vazio e não provaria nada.");
            return cenas;
        }

        private static string GuidDoScript(string nome)
        {
            var meta = Directory.GetFiles("Assets/Scripts", nome + ".cs.meta",
                                          SearchOption.AllDirectories).FirstOrDefault();
            if (meta == null) return null;

            var m = Regex.Match(File.ReadAllText(meta), @"guid: ([0-9a-f]{32})");
            return m.Success ? m.Groups[1].Value : null;
        }
    }
}
