using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda o <b>HUD completo</b>: o <see cref="FavelaAmarela.Runtime.UI.HUDController"/> tem
    /// que ter as seis views ligadas em toda cena de jogo, e nenhum script de
    /// <c>Assets/Scripts/UI/</c> pode ficar <b>órfão</b> (existir no C#, não existir em cena ou
    /// prefab nenhum).
    ///
    /// <para><b>O que motivou (2026-08-13):</b> até aqui cada peça do HUD vinha de uma ferramenta
    /// de Editor com lista de cenas própria. O resultado: nenhuma cena tinha HUD completo — o
    /// Deserto e o Santuário não mostravam a arma empunhada nem os artefatos F1–F4, e a
    /// <c>VigorBar</c> nunca foi instanciada em cena ou prefab algum (0 em ambos). O
    /// <c>HUDController.InjetarVigor</c> ligava em <c>null</c> sem avisar — era o único
    /// <c>Injetar*</c> sem <c>Debug.LogError</c>, então nada no console apontava a causa.</para>
    ///
    /// <para><b>Por que a resolução de campo é mais que um grep</b> (achado escrevendo este
    /// guarda): quando o <c>HUDController</c> vem de uma instância do prefab
    /// <c>HUD_ResilienciaBar</c> <b>sem nenhum campo sobrescrito</b>, a Unity não duplica o
    /// bloco do componente na cena — os valores moram só no <c>.prefab</c>. Um campo
    /// sobrescrito vira uma entrada em <c>m_Modifications</c> referenciando o componente pelo
    /// <c>fileID</c> <i>dentro do prefab</i>, não pelo guid do script. Um grep simples pelo guid
    /// do <c>HUDController</c> não encontra nada em três das quatro cenas — não porque o
    /// componente não exista, mas porque o formato de override da Unity é outro. O valor efetivo
    /// de um campo é: <b>o valor no prefab, substituído pelo override da cena se houver um</b>.</para>
    ///
    /// <para>Lê o YAML de cenas e prefabs em vez de abrir a cena no Editor — mesma técnica de
    /// <c>FichaAtributosAssetsTests</c>: um teste EditMode que chama <c>OpenScene</c> mexe no
    /// estado do Editor de quem roda a suíte.</para>
    /// </summary>
    public sealed class HudCompletoTests
    {
        private const string PastaDeCenas = "Assets/Scenes/";
        private const string PastaDeScriptsUI = "Assets/Scripts/UI/";

        /// <summary>
        /// Cenas que precisam de HUD completo — as três fases jogáveis do Vertical Slice mais a
        /// Arena de Testes (onde os chefes são calibrados).
        /// </summary>
        private static readonly string[] CenasComHud =
        {
            PastaDeCenas + "Deserto_Hali.unity",
            PastaDeCenas + "Playtest_RuinasPalidas.unity",
            PastaDeCenas + "Santuario_Yhtill.unity",
            PastaDeCenas + "Cena_ArenaDeTestes.unity",
            // Acrescentadas em 2026-08-20. A ausência delas escondia um defeito real: as duas
            // cenas de chefe do Vertical Slice tinham DUAS das seis views do HUD.
            PastaDeCenas + "Portoes_Das_Ruinas.unity",
            PastaDeCenas + "Castelo_Carcosa.unity",
        };

        /// <summary>
        /// Os seis campos declarados em <c>HUDController</c>. Mudar esta lista também exige
        /// mudar <c>HUDController.cs</c> e (se for view nova) <c>BuildHUDCompleto</c> — os três
        /// devem andar juntos.
        /// </summary>
        private static readonly string[] CamposDoHud =
        {
            "resilienciaBar", "vigorBar", "vitalidadeBar",
            "barraDeAcoes", "barraDeItens", "barraDeArtefatos",
        };

        /// <summary>
        /// Scripts de <c>Assets/Scripts/UI/</c> que legitimamente não aparecem em cena nem
        /// prefab nenhum, com o motivo. Um script novo cai fora desta lista por padrão — é o
        /// que torna o teste um guarda, não uma lista para manter manualmente a cada UI nova.
        /// </summary>
        private static readonly Dictionary<string, string> OrfasConhecidas = new Dictionary<string, string>
        {
            ["ScreenFader"] = "Dormente: os dois consumidores (AberturaDesertoCinematica, " +
                               "QuedaZ4Z5Trigger) também não estão instanciados em cena nenhuma. " +
                               "Sem o gatilho em cena, não há onde a Fader precisar existir ainda.",

            ["BarraAnimada"] = "Classe base ABSTRATA das barras de recurso — nunca é anexada a " +
                                "um GameObject. Quem aparece em cena são as concretas " +
                                "(ResilienciaBar, VitalidadeBar, VigorBar), verificadas nos " +
                                "casos de HUDController_TemAsSeisViewsLigadas.",

            ["PadraoDeTextoDeDialogo"] = "Classe ESTÁTICA de constantes, não componente: guarda " +
                                "os limites do Best Fit do texto de diálogo (24–44) num lugar só, " +
                                "lidos pela ferramenta de Editor e pelo TipografiaDeDialogoTests. " +
                                "Não tem o que anexar a um GameObject.",
        };

        // ── HUDController: os 6 campos ligados em toda cena de jogo ──────────

                // MIGRADO EM 2026-08-22 (Bloco 6): o HUD deixou de ser montado por cena e passou
        // a viver em Resources/HUD_Gameplay.prefab, carregado por
        // HUDController.GarantirInstancia com DontDestroyOnLoad. Verificar a presenca
        // dele nas cenas passou a ser verificar o vazio. A cobertura NAO sumiu: mudou de
        // alvo para HudPersistenteTests, que checa o prefab.
        [Ignore("Contrato mudou: ver HudPersistenteTests")]
        [TestCaseSource(nameof(CenasComHud))]
        public void HUDController_TemAsSeisViewsLigadas(string caminhoDaCena)
        {
            Assert.IsTrue(File.Exists(caminhoDaCena), $"Cena não encontrada: {caminhoDaCena}");

            var campos = ResolverCamposDoHud(caminhoDaCena);

            Assert.IsNotNull(campos,
                $"{Path.GetFileName(caminhoDaCena)}: nenhum HUDController na cena (nem direto, " +
                "nem via prefab). Rode 'Tools/FavelaAmarela/Build HUD Completo em todas as " +
                "cenas de jogo'.");

            var vazios = new List<string>();
            foreach (var campo in CamposDoHud)
            {
                if (!campos.TryGetValue(campo, out var valor) || valor == "0") vazios.Add(campo);
            }

            Assert.IsEmpty(vazios,
                $"{Path.GetFileName(caminhoDaCena)}: HUDController com campo(s) vazio(s) " +
                $"(fileID: 0): {string.Join(", ", vazios)}. Rode " +
                "'Tools/FavelaAmarela/Build HUD Completo (cena aberta)' nesta cena.");
        }

        // ── Nenhum script de UI fica órfão ───────────────────────────────────

        [Test]
        public void NenhumScriptDeUI_FicaOrfaoSemMotivoDocumentado()
        {
            var scripts = Directory.GetFiles(PastaDeScriptsUI, "*.cs");
            Assert.IsNotEmpty(scripts, $"Nenhum script encontrado em {PastaDeScriptsUI}.");

            var todosOsArquivos = new List<string>();
            todosOsArquivos.AddRange(Directory.GetFiles("Assets/Scenes", "*.unity", SearchOption.AllDirectories));
            todosOsArquivos.AddRange(Directory.GetFiles("Assets", "*.prefab", SearchOption.AllDirectories));

            var problemas = new List<string>();

            foreach (var caminhoScript in scripts)
            {
                string nome = Path.GetFileNameWithoutExtension(caminhoScript);
                if (OrfasConhecidas.ContainsKey(nome)) continue;

                string guid = GuidDoMeta(caminhoScript + ".meta");
                if (string.IsNullOrEmpty(guid))
                {
                    problemas.Add($"{nome}: sem .meta ou sem guid — não dá para verificar uso.");
                    continue;
                }

                bool usado = false;
                foreach (var arquivo in todosOsArquivos)
                {
                    if (ArquivoContemTexto(arquivo, guid)) { usado = true; break; }
                }

                if (!usado)
                {
                    problemas.Add($"{nome}: 0 ocorrências em cenas ou prefabs. Se for " +
                        "intencional (feature dormente, aguardando consumidor), acrescente a " +
                        "'OrfasConhecidas' em HudCompletoTests.cs com o motivo.");
                }
            }

            Assert.IsEmpty(problemas, "Script(s) de UI órfão(s):\n" + string.Join("\n", problemas));
        }

        // ── Resolução de campo: direto na cena, OU herdado de prefab + override ─

        /// <summary>
        /// Valor efetivo dos seis campos do <c>HUDController</c> nesta cena, ou <c>null</c> se
        /// não houver nenhum. Cobre os dois formatos que a Unity usa: componente serializado
        /// por inteiro na própria cena, ou instância de prefab cujo baseline vem do
        /// <c>.prefab</c> e é sobrescrito por <c>m_Modifications</c>.
        /// </summary>
        private static Dictionary<string, string> ResolverCamposDoHud(string caminhoDaCena)
        {
            string guidHud = GuidDoScript("HUDController.cs");

            // Caso A: o componente está serializado por inteiro na própria cena — objeto que
            // não veio de prefab, ou prefab instance com esse componente já destacado.
            var blocosNaCena = LerBlocos(caminhoDaCena);
            var direto = AcharBlocoPorScript(blocosNaCena, guidHud);
            if (direto.HasValue)
            {
                var dict = new Dictionary<string, string>();
                foreach (var campo in CamposDoHud)
                    dict[campo] = ValorDoCampo(direto.Value.Linhas, campo) ?? "0";
                return dict;
            }

            // Caso B: vem de um PrefabInstance. Acha, entre os prefabs do projeto, qual tem um
            // HUDController — e se a cena de fato instancia esse prefab.
            foreach (var caminhoPrefab in Directory.GetFiles("Assets", "*.prefab", SearchOption.AllDirectories))
            {
                var blocosDoPrefab = LerBlocos(caminhoPrefab);
                var blocoNoPrefab = AcharBlocoPorScript(blocosDoPrefab, guidHud);
                if (!blocoNoPrefab.HasValue) continue;

                string guidDoPrefab = GuidDoMeta(caminhoPrefab + ".meta");
                if (string.IsNullOrEmpty(guidDoPrefab)) continue;

                string textoDaCena = File.ReadAllText(caminhoDaCena);
                if (!textoDaCena.Contains(guidDoPrefab)) continue; // cena não usa este prefab

                var baseline = new Dictionary<string, string>();
                foreach (var campo in CamposDoHud)
                    baseline[campo] = ValorDoCampo(blocoNoPrefab.Value.Linhas, campo) ?? "0";

                AplicarOverridesDaCena(caminhoDaCena, blocoNoPrefab.Value.FileId, guidDoPrefab, baseline);
                return baseline;
            }

            return null;
        }

        /// <summary>
        /// Aplica, por cima do <paramref name="baseline"/> (valores do prefab), qualquer
        /// override que a cena tenha para o componente de fileID <paramref name="fileIdNoPrefab"/>.
        /// Formato do bloco de override (<c>PrefabInstance.m_Modification.m_Modifications</c>):
        /// <code>
        /// - target: {fileID: N, guid: G, type: 3}
        ///   propertyPath: nomeDoCampo
        ///   value:
        ///   objectReference: {fileID: M}
        /// </code>
        /// </summary>
        private static void AplicarOverridesDaCena(string caminhoDaCena, long fileIdNoPrefab,
            string guidDoPrefab, Dictionary<string, string> baseline)
        {
            var linhas = File.ReadAllLines(caminhoDaCena);
            string prefixoAlvo = $"target: {{fileID: {fileIdNoPrefab}, guid: {guidDoPrefab}";

            for (int i = 0; i < linhas.Length; i++)
            {
                if (!linhas[i].TrimStart().StartsWith("- " + prefixoAlvo)) continue;

                string propertyPath = null, objRef = null;
                for (int j = i + 1; j < linhas.Length && j < i + 5; j++)
                {
                    if (linhas[j].TrimStart().StartsWith("- target:")) break; // próxima entrada

                    var pm = Regex.Match(linhas[j], @"propertyPath:\s*(\S+)");
                    if (pm.Success) propertyPath = pm.Groups[1].Value;

                    var om = Regex.Match(linhas[j], @"objectReference:\s*\{fileID:\s*(-?\d+)");
                    if (om.Success) objRef = om.Groups[1].Value;
                }

                if (propertyPath != null && objRef != null && baseline.ContainsKey(propertyPath))
                    baseline[propertyPath] = objRef;
            }
        }

        // ── Apoio: blocos YAML ────────────────────────────────────────────────

        private readonly struct Bloco
        {
            public readonly long FileId;
            public readonly List<string> Linhas;
            public Bloco(long fileId, List<string> linhas) { FileId = fileId; Linhas = linhas; }
        }

        private static List<Bloco> LerBlocos(string caminho)
        {
            var separador = new Regex(@"^--- !u!\d+ &(\d+)$");
            var blocos = new List<Bloco>();
            List<string> atual = null;

            foreach (var linha in File.ReadAllLines(caminho))
            {
                var m = separador.Match(linha);
                if (m.Success)
                {
                    atual = new List<string>();
                    blocos.Add(new Bloco(long.Parse(m.Groups[1].Value), atual));
                    continue;
                }
                atual?.Add(linha);
            }
            return blocos;
        }

        private static Bloco? AcharBlocoPorScript(List<Bloco> blocos, string guidDoScript)
        {
            foreach (var bloco in blocos)
                foreach (var linha in bloco.Linhas)
                    if (linha.Contains("m_Script:") && linha.Contains(guidDoScript))
                        return bloco;
            return null;
        }

        /// <summary>Valor de <c>fileID</c> de um campo <c>nome: {fileID: N}</c> dentro do bloco.</summary>
        private static string ValorDoCampo(List<string> bloco, string nomeDoCampo)
        {
            var padrao = new Regex($@"^  {Regex.Escape(nomeDoCampo)}: \{{fileID: (-?\d+)");
            foreach (var linha in bloco)
            {
                var m = padrao.Match(linha);
                if (m.Success) return m.Groups[1].Value;
            }
            return null;
        }

        // ── Apoio: guids ─────────────────────────────────────────────────────

        private static readonly Dictionary<string, string> CacheDeGuids = new Dictionary<string, string>();

        private static string GuidDoScript(string nomeDoArquivo)
            => GuidDoMeta(PastaDeScriptsUI + nomeDoArquivo + ".meta");

        private static string GuidDoMeta(string caminhoDoMeta)
        {
            if (CacheDeGuids.TryGetValue(caminhoDoMeta, out var cacheado)) return cacheado;
            if (!File.Exists(caminhoDoMeta)) return null;

            var m = Regex.Match(File.ReadAllText(caminhoDoMeta), @"guid:\s*([0-9a-f]{32})");
            string guid = m.Success ? m.Groups[1].Value : null;

            CacheDeGuids[caminhoDoMeta] = guid;
            return guid;
        }

        private static bool ArquivoContemTexto(string caminho, string texto)
        {
            foreach (var linha in File.ReadLines(caminho))
                if (linha.Contains(texto)) return true;
            return false;
        }
    }
}
