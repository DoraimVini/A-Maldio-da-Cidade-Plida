using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda o <b>grafo de navegação</b> entre as cenas: todo portal leva a uma cena que existe,
    /// está na build, e deposita o jogador num ponto que existe lá.
    ///
    /// <para><b>O bug que motivou:</b> a volta dos Portões para o Deserto pedia
    /// <c>chegarEm: "PortoesDasRuinas"</c>, e esse identificador <b>não existe</b> no Deserto — os
    /// ids de lá são outros. O efeito é mudo: <c>PortalDeCena</c> escreve
    /// <c>PontoDeChegada.Pendente</c>, nenhum ponto o consome, e o jogador aparece na posição
    /// autorada da cena — a entrada do deserto, longe dos Portões. Sem exceção, sem aviso, sem
    /// nada no console. Só se descobre andando.</para>
    ///
    /// <para><b>Por que os testes por cena não pegavam:</b> cada um olhava a sua cena. Este erro
    /// só existe <b>entre</b> duas — o portal está numa, o ponto que falta está na outra. Nenhum
    /// guarda olhava o par.</para>
    ///
    /// <para><c>chegarEm</c> <b>vazio é legítimo</b> e não falha aqui: significa "não reposicione,
    /// use a posição autorada". <c>PortalDeCena</c> trata o vazio explicitamente
    /// (<c>Pendente = null</c>), e a entrada da Tumba usa esse caminho de propósito. O defeito é
    /// pedir um ponto <b>nomeado</b> que não existe.</para>
    /// </summary>
    public sealed class NavegacaoEntreCenasTests
    {
        private const string PastaDeCenas = "Assets/Scenes";
        private const string BuildSettings = "ProjectSettings/EditorBuildSettings.asset";

        /// <summary>
        /// Cenas de desenvolvimento, fora do grafo do jogo. <c>Cena_ArenaDeTestes</c> é a arena do
        /// Carcosa Debugger e <c>cena_1</c> é legado — nenhuma das duas entra em build.
        /// </summary>
        private static readonly string[] ForaDoJogo = { "Cena_ArenaDeTestes", "cena_1" };

        [Test]
        public void TodoPortal_LevaAUmaCenaQueExisteEEstaNaBuild()
        {
            var naBuild = new HashSet<string>(
                Regex.Matches(File.ReadAllText(BuildSettings), @"path:\s*Assets/Scenes/(\S+)\.unity")
                     .Cast<Match>()
                     .Select(m => m.Groups[1].Value));

            var falhas = new List<string>();

            foreach (var (cena, destino, _) in TodosOsPortais())
            {
                if (!File.Exists($"{PastaDeCenas}/{destino}.unity"))
                {
                    falhas.Add($"{cena} → '{destino}': a cena de destino não existe no disco");
                    continue;
                }

                if (!naBuild.Contains(destino))
                    falhas.Add($"{cena} → '{destino}': destino fora do Build Settings — " +
                               "SceneManager.LoadScene falharia em runtime");
            }

            Assert.IsEmpty(falhas,
                "Portal apontando para o vazio:\n  " + string.Join("\n  ", falhas));
        }

        [Test]
        public void TodoChegarEmNomeado_TemPontoDeChegadaNoDestino()
        {
            var falhas = new List<string>();

            foreach (var (cena, destino, chegarEm) in TodosOsPortais())
            {
                // Vazio = "não reposicione". Caminho legítimo e usado.
                if (string.IsNullOrWhiteSpace(chegarEm)) continue;

                string arquivo = $"{PastaDeCenas}/{destino}.unity";
                if (!File.Exists(arquivo)) continue; // já cobrado pelo teste acima

                var ids = IdentificadoresDe(arquivo);

                if (!ids.Contains(chegarEm))
                    falhas.Add($"{cena} → {destino}: chegarEm '{chegarEm}' não existe lá. " +
                               $"Ids disponíveis: {(ids.Count == 0 ? "(nenhum)" : string.Join(", ", ids.OrderBy(i => i)))}");
            }

            Assert.IsEmpty(falhas,
                "Portal pedindo um ponto de chegada que não existe no destino. O jogador cai na " +
                "posição autorada da cena, sem erro no console:\n  " + string.Join("\n  ", falhas));
        }

        // ── Leitura das cenas ─────────────────────────────────────────────────

        /// <summary>
        /// Todos os portais do jogo como (cena de origem, destino, chegarEm).
        ///
        /// <para>O par é lido junto porque <c>PortalDeCena</c> serializa <c>cenaDestino</c> e
        /// <c>chegarEm</c> em sequência. <b>O <c>chegarEm</c> pode vir vazio</b>, e o padrão
        /// precisa aceitar isso sem engolir a linha seguinte — a primeira versão desta regex
        /// capturava <c>"carenciaAoCarregar:"</c> como se fosse o identificador.</para>
        /// </summary>
        private static IEnumerable<(string Cena, string Destino, string ChegarEm)> TodosOsPortais()
        {
            foreach (var arquivo in Directory.EnumerateFiles(PastaDeCenas, "*.unity"))
            {
                string cena = Path.GetFileNameWithoutExtension(arquivo);
                if (ForaDoJogo.Contains(cena)) continue;

                foreach (Match m in Regex.Matches(File.ReadAllText(arquivo),
                             @"(?m)^\s*cenaDestino:\s*(\S*)[ \t]*\r?\n\s*chegarEm:[ \t]*(\S*)[ \t]*$"))
                {
                    string destino = m.Groups[1].Value;
                    if (string.IsNullOrWhiteSpace(destino)) continue;

                    yield return (cena, destino, m.Groups[2].Value);
                }
            }
        }

        private static HashSet<string> IdentificadoresDe(string arquivo)
            => new HashSet<string>(
                Regex.Matches(File.ReadAllText(arquivo), @"(?m)^\s*identificador:\s*(\S+)[ \t]*$")
                     .Cast<Match>()
                     .Select(m => m.Groups[1].Value));
    }
}
