using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda o <b>bootstrap de cena</b>: o componente que monta o grafo de dependências existe
    /// nas cenas jogáveis, com os campos que importam ligados e a configuração de Resiliência
    /// consistente entre fases.
    ///
    /// <para><b>Por que existe (2026-08-13):</b> escrito <b>antes</b> da refatoração que quebra o
    /// <c>GameManager</c> em componentes focados. Nenhum teste cobria <c>GameManager</c>,
    /// <c>ProgressionManager</c> ou <c>BarraDeItens</c> — mover 12 passos de injeção sem rede
    /// seria refatorar às cegas. Este guarda registra o contrato de wiring <b>de antes</b>, para
    /// que qualquer coisa perdida no caminho apareça como falha em vez de como bug silencioso em
    /// Play Mode.</para>
    ///
    /// <para>Lê o YAML das cenas em vez de abri-las no Editor — mesma técnica de
    /// <c>HudCompletoTests</c> e <c>FichaAtributosAssetsTests</c>. O componente de bootstrap está
    /// serializado <b>direto</b> nas cenas (não vem de prefab), então aqui não é preciso resolver
    /// override de prefab como o guarda do HUD faz.</para>
    /// </summary>
    public sealed class BootstrapDeCenaTests
    {
        private const string PastaDeCenas = "Assets/Scenes/";

        /// <summary>
        /// Cenas jogáveis do Build Settings. <c>Cena_Menu</c> fica de fora de propósito: é só
        /// menu, não tem mundo para montar.
        /// </summary>
        private static readonly string[] CenasJogaveis =
        {
            PastaDeCenas + "Deserto_Hali.unity",
            PastaDeCenas + "Tumba_De_Alhazred.unity",
            PastaDeCenas + "Santuario_Yhtill.unity",
        };

        /// <summary>A Arena de dev entra separada: precisa do bootstrap, mas não das telas de fase.</summary>
        private const string CenaDaArena = PastaDeCenas + "Cena_ArenaDeTestes.unity";

        /// <summary>
        /// Campos <b>mortos</b> do bootstrap antigo: em <c>fileID: 0</c> nas cinco cenas, e sem
        /// uso futuro previsto. Eram da cena antiga, de antes dela virar a Tumba (confirmado
        /// pelo Vini em 2026-08-13) — não são wiring esquecido nem feature dormente.
        ///
        /// <para>Um guarda que exigisse "todos os seis campos ligados" estaria errado. Este faz
        /// o oposto: garante que continuem <b>desligados</b>, para ninguém religar por engano um
        /// campo cujo <c>SetActive</c> nunca fez nada. O <c>GameStatePresenter</c> novo já nasce
        /// sem eles; eles morrem de vez quando o <c>GameManager</c> sair na Fase 2.</para>
        /// </summary>
        private static readonly Dictionary<string, string> CamposMortos = new Dictionary<string, string>
        {
            ["telaTransicaoDeFase"] = "Tela da cena antiga, anterior à Tumba. O estado " +
                                       "TransicaoDeFase continua existindo e congelando o mundo — " +
                                       "o que não existe mais é uma tela para ele.",

            ["gameplayRoot"] = "Raiz do mundo da cena antiga, anterior à Tumba. Nenhuma cena atual " +
                                "agrupa o jogável sob uma raiz única, e o SetActive por estado " +
                                "nunca chegou a rodar.",
        };

        /// <summary>Resiliência máxima esperada em toda cena jogável.</summary>
        private const float MaxResilienciaEsperada = 100f;

        /// <summary>Fração de pânico esperada em toda cena jogável.</summary>
        private const float FracaoPanicoEsperada = 0.25f;

        // ── O bootstrap existe onde precisa existir ──────────────────────────

        [TestCaseSource(nameof(CenasJogaveis))]
        public void CenaJogavel_TemOBootstrap(string caminhoDaCena)
        {
            var campos = CamposDoBootstrap(caminhoDaCena);

            Assert.IsNotNull(campos,
                $"{Path.GetFileName(caminhoDaCena)}: sem componente de bootstrap na cena. " +
                "Sem ele, nenhum POCO é criado nem injetado — combate, stealth e HUD ficam mudos.");
        }

        [Test]
        public void Arena_TemOBootstrap()
        {
            var campos = CamposDoBootstrap(CenaDaArena);

            Assert.IsNotNull(campos,
                "Cena_ArenaDeTestes sem bootstrap: é a cena onde os chefes são calibrados, " +
                "e sem ele o Carcosa Debugger não tem grafo de dependências para trabalhar.");
        }

        // ── Configuração de Resiliência consistente entre fases ──────────────

        [TestCaseSource(nameof(CenasJogaveis))]
        public void CenaJogavel_TemAConfiguracaoDeResilienciaPadrao(string caminhoDaCena)
        {
            var campos = CamposDoBootstrap(caminhoDaCena);
            Assert.IsNotNull(campos, $"{Path.GetFileName(caminhoDaCena)}: sem bootstrap.");

            // Divergir aqui muda a dificuldade de uma fase sem ninguém notar: a barra continua
            // parecendo cheia, mas o Colapso chega antes (ou nunca).
            Assert.AreEqual(MaxResilienciaEsperada, ValorNumerico(campos, "maxResiliencia"), 1e-4f,
                $"{Path.GetFileName(caminhoDaCena)}: maxResiliencia divergente das outras fases.");

            Assert.AreEqual(FracaoPanicoEsperada, ValorNumerico(campos, "fracaoPanico"), 1e-4f,
                $"{Path.GetFileName(caminhoDaCena)}: fracaoPanico divergente — o limiar de " +
                "Pânico (câmera, áudio, shader) dispararia em outro ponto nesta fase.");
        }

        // ── Os campos que a fase precisa de verdade ──────────────────────────

                // MIGRADO EM 2026-08-22 (Bloco 6): o HUD deixou de ser montado por cena e passou
        // a viver em Resources/HUD_Gameplay.prefab, carregado por
        // HUDController.GarantirInstancia com DontDestroyOnLoad. Verificar a presenca
        // dele nas cenas passou a ser verificar o vazio. A cobertura NAO sumiu: mudou de
        // alvo para HudPersistenteTests, que checa o prefab.
        [Ignore("Contrato mudou: ver HudPersistenteTests")]
        [TestCaseSource(nameof(CenasJogaveis))]
        public void CenaJogavel_TemSequenciaDeColapso_NoControladorDeMorte(string caminhoDaCena)
        {
            var campos = CamposDe(caminhoDaCena, "PlayerDeathController.cs", "GameManager.cs");
            Assert.IsNotNull(campos,
                $"{Path.GetFileName(caminhoDaCena)}: sem PlayerDeathController nem GameManager.");

            Assert.AreNotEqual("0", ReferenciaDe(campos, "sequenciaColapso"),
                $"{Path.GetFileName(caminhoDaCena)}: sem SequenciaDeColapso ligada. Morrer nesta " +
                "fase não tocaria a dissolução nem a frase final — o Colapso aconteceria em " +
                "silêncio.");
        }

                // MIGRADO EM 2026-08-22 (Bloco 6): o HUD deixou de ser montado por cena e passou
        // a viver em Resources/HUD_Gameplay.prefab, carregado por
        // HUDController.GarantirInstancia com DontDestroyOnLoad. Verificar a presenca
        // dele nas cenas passou a ser verificar o vazio. A cobertura NAO sumiu: mudou de
        // alvo para HudPersistenteTests, que checa o prefab.
        [Ignore("Contrato mudou: ver HudPersistenteTests")]
        [TestCaseSource(nameof(CenasJogaveis))]
        public void CenaJogavel_TemTelaDePause_NoPresenter(string caminhoDaCena)
        {
            var campos = CamposDe(caminhoDaCena, "GameStatePresenter.cs", "GameManager.cs");
            Assert.IsNotNull(campos,
                $"{Path.GetFileName(caminhoDaCena)}: sem GameStatePresenter nem GameManager.");

            Assert.AreNotEqual("0", ReferenciaDe(campos, "telaPause"),
                $"{Path.GetFileName(caminhoDaCena)}: sem tela de pause ligada. O Esc congelaria " +
                "o mundo sem mostrar nada — o jogo pareceria travado.");
        }

        // ── Os campos mortos continuam mortos ────────────────────────────────

        [Test]
        public void CamposMortos_ContinuamDesligadosEmTodaCena()
        {
            var religados = new List<string>();

            foreach (var caminho in TodasAsCenasComBootstrap())
            {
                var campos = CamposDoBootstrap(caminho);
                if (campos == null) continue;

                foreach (var campo in CamposMortos.Keys)
                {
                    if (ReferenciaDe(campos, campo) != "0")
                        religados.Add($"{Path.GetFileName(caminho)} → {campo}");
                }
            }

            Assert.IsEmpty(religados,
                "Campo(s) morto(s) do bootstrap antigo foram religados. Eles são da cena " +
                "anterior à Tumba e não têm uso no jogo — o SetActive deles nunca rodou. Se o " +
                "uso voltou de verdade, tire de 'CamposMortos' em BootstrapDeCenaTests.cs e " +
                "traga o campo de volta ao GameStatePresenter:\n" + string.Join("\n", religados));
        }

        // A cena_1 foi apagada em 2026-09-04. O teste que a guardava
        // (Cena1_EhLegadoAbandonado_NaoEntraNoGuarda) saiu junto: ele começava com
        // "if (!File.Exists(cena1)) return;", então sem o arquivo passaria a afirmar NADA --
        // um teste verde que não mede coisa alguma é pior que teste nenhum, porque conta como
        // cobertura.
        //
        // Quem cobre o bootstrap é CenaJogavel_TemOBootstrap, por TestCaseSource sobre
        // CenasJogaveis -- uma lista ESCRITA À MÃO, não uma varredura do Build Settings. Uma
        // cena nova que entre na build e não nesta lista não é coberta por ele; quem varre a
        // pasta inteira e obriga a justificar cada ausência é CenasNaoFicamParaTrasTests.

        // ── Apoio ────────────────────────────────────────────────────────────

        private static IEnumerable<string> TodasAsCenasComBootstrap()
        {
            foreach (var c in CenasJogaveis) yield return c;
            yield return CenaDaArena;
        }

        /// <summary>
        /// Linhas do bloco YAML do componente de bootstrap, ou <c>null</c> se a cena não o tem.
        ///
        /// <para>Procura por <b>qualquer</b> dos GUIDs conhecidos de bootstrap. Durante a
        /// refatoração de 2026-08-13 o papel migra de <c>GameManager</c> para
        /// <c>GameLoopBootstrap</c>; aceitar os dois deixa este guarda válido dos dois lados da
        /// mudança — que é justamente o que o torna útil como rede.</para>
        /// </summary>
        private static List<string> CamposDoBootstrap(string caminhoDaCena)
            => CamposDe(caminhoDaCena, "GameLoopBootstrap.cs", "GameManager.cs");

        /// <summary>
        /// Linhas do bloco YAML do <b>primeiro</b> script da lista que estiver na cena, ou
        /// <c>null</c> se nenhum estiver.
        ///
        /// <para>A lista é uma cadeia de fallback ordenada do dono <b>novo</b> para o
        /// <b>antigo</b>. Ela existe porque a Fase 2 não só troca o componente de bootstrap: ela
        /// redistribui campos entre componentes — <c>telaPause</c> passa a morar no
        /// <c>GameStatePresenter</c> (quem o liga/desliga) e <c>sequenciaColapso</c> no
        /// <c>PlayerDeathController</c> (quem conhece a causa da morte). Procurar todos eles no
        /// bootstrap encontraria o lugar errado depois da migração.</para>
        /// </summary>
        private static List<string> CamposDe(string caminhoDaCena, params string[] scriptsEmOrdem)
        {
            if (!File.Exists(caminhoDaCena)) return null;

            foreach (var nomeDoScript in scriptsEmOrdem)
            {
                string guid = GuidDoScript(nomeDoScript);
                if (string.IsNullOrEmpty(guid)) continue;

                var bloco = AcharBlocoPorScript(caminhoDaCena, guid);
                if (bloco != null) return bloco;
            }

            return null;
        }

        private static string GuidDoScript(string nomeDoArquivo)
        {
            var metas = Directory.GetFiles("Assets/Scripts", nomeDoArquivo + ".meta",
                SearchOption.AllDirectories);
            if (metas.Length == 0) return null;

            var m = Regex.Match(File.ReadAllText(metas[0]), @"guid:\s*([0-9a-f]{32})");
            return m.Success ? m.Groups[1].Value : null;
        }

        private static List<string> AcharBlocoPorScript(string caminhoDaCena, string guidDoScript)
        {
            var separador = new Regex(@"^--- !u!\d+ &\d+$");
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
                foreach (var linha in bloco)
                    if (linha.Contains("m_Script:") && linha.Contains(guidDoScript))
                        return bloco;

            return null;
        }

        /// <summary>
        /// <c>fileID</c> de um campo de referência, ou <c>"0"</c> quando desligado — e também
        /// quando a chave <b>não existe</b> no YAML, que é o caso de cena salva antes de o campo
        /// ser criado.
        /// </summary>
        /// <summary>
        /// A ficha do inventário só pode ser ligada por código.
        ///
        /// <para><b>Por quê (achado no playtest de 2026-08-28).</b> O <c>PainelDeFicha</c> mora
        /// dentro do <c>HUD_Gameplay.prefab</c>, que é um <b>asset em Resources</b> — e um
        /// prefab-asset não pode referenciar objeto de cena. O campo
        /// <c>vitalidadeDoJogador</c> era, portanto, <b>impossível de preencher</b>: ficava em
        /// <c>{fileID: 0}</c> e a tela abria dizendo "Ficha indisponível: sem VitalidadeBridge
        /// ligada". A mensagem estava certa sobre o sintoma e mandava o leitor procurar um
        /// campo de Inspector que nunca ia funcionar.</para>
        ///
        /// <para>Depois que a HUD virou persistente, ligar por Inspector deixou de ser possível
        /// <b>por construção</b>: o Damião troca a cada cena e a HUD não.</para>
        /// </summary>
        [Test]
        public void OBootstrap_LigaAFichaDoInventario()
        {
            string fonte = File.ReadAllText("Assets/Scripts/GameLoop/GameLoopBootstrap.cs");

            StringAssert.Contains("PainelDeFicha", fonte,
                "O GameLoopBootstrap parou de procurar o PainelDeFicha. Como ele vive num " +
                "prefab-asset, NÃO existe outro caminho: a ficha volta a abrir vazia.");

            StringAssert.Contains("ficha.Bind(_vitalidadeDamiao)", fonte,
                "O bind da ficha sumiu. Sem ele o painel abre com 'Ficha indisponível' e o " +
                "efeito de todo item equipado volta a ser invisível — que é justamente o " +
                "problema que essa tela existe para expor.");

            StringAssert.Contains("FindObjectsInactive.Include", fonte,
                "A busca do PainelDeFicha precisa incluir inativos: ele fica desligado " +
                "enquanto a mochila está fechada, que é a maior parte do tempo. Sem isso o " +
                "bind acha null e falha em silêncio.");
        }

        private static string ReferenciaDe(List<string> bloco, string nomeDoCampo)
        {
            var padrao = new Regex($@"^  {Regex.Escape(nomeDoCampo)}: \{{fileID: (-?\d+)");
            foreach (var linha in bloco)
            {
                var m = padrao.Match(linha);
                if (m.Success) return m.Groups[1].Value;
            }
            return "0";
        }

        private static float ValorNumerico(List<string> bloco, string nomeDoCampo)
        {
            var padrao = new Regex($@"^  {Regex.Escape(nomeDoCampo)}: (-?[\d.]+)");
            foreach (var linha in bloco)
            {
                var m = padrao.Match(linha);
                if (m.Success && float.TryParse(m.Groups[1].Value, NumberStyles.Float,
                        CultureInfo.InvariantCulture, out float v))
                    return v;
            }
            return float.NaN;
        }
    }
}
