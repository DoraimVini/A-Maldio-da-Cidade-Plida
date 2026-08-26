using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda o <b>HUD persistente</b> — o prefab que nasce uma vez e sobrevive às trocas de
    /// cena — e serve de referência única para os outros guardas que antes checavam cena a cena.
    ///
    /// <para><b>A mudança de contrato (2026-08-22).</b> Até aqui, cada cena carregava a sua
    /// própria cópia do HUD, e havia um punhado de testes verificando isso cena a cena. O Vini
    /// apontou o problema de fundo: enquanto o HUD for por cena, ele é mais uma das listas
    /// escritas à mão que envelhecem neste projeto — já foram <b>oito</b>. Agora o HUD vive em
    /// <c>Resources/HUD_Gameplay.prefab</c>, carregado por
    /// <c>HUDController.GarantirInstancia</c> com <c>DontDestroyOnLoad</c>, no mesmo padrão de
    /// <c>InventoryManager</c>, <c>GerenciadorDeSave</c> e <c>ProgressionBridge</c>.</para>
    ///
    /// <para>Por isso os guardas <b>mudaram de alvo, não sumiram</b>: verificar a ausência do
    /// HUD nas cenas seria verificar o vazio. O que precisa estar íntegro agora é o prefab.</para>
    /// </summary>
    public sealed class HudPersistenteTests
    {
        /// <summary>Caminho do prefab. Precisa estar sob <c>Resources/</c> para o load funcionar.</summary>
        public const string Prefab = "Assets/FavelaAmarela/Resources/HUD_Gameplay.prefab";

        private static readonly string NovaLinha = System.Environment.NewLine;

        /// <summary>
        /// As sete views que o <c>HUDController</c> liga, mais as duas telas de fluxo que
        /// migraram para o prefab em 2026-08-22.
        /// </summary>
        private static readonly string[] CamposObrigatorios =
        {
            "resilienciaBar", "vitalidadeBar", "vigorBar",
            "barraDeAcoes", "barraDeItens", "barraDeArtefatos", "companheiroBar",
            "telaPause", "sequenciaColapso",
        };

        [Test]
        public void OPrefabDoHud_ExisteEmResources()
        {
            Assert.IsTrue(File.Exists(Prefab),
                $"'{Prefab}' não existe. Sem ele, HUDController.GarantirInstancia não carrega " +
                "nada e o jogo roda inteiro sem HUD — sem barras, sem inventário, sem pause. " +
                "Conserto: 'Tools/FavelaAmarela/HUD: extrair para prefab persistente'.");

            StringAssert.Contains("/Resources/", Prefab,
                "O prefab precisa estar sob uma pasta Resources para Resources.Load encontrá-lo.");
        }

        [Test]
        public void OPrefabDoHud_TemTodasAsReferenciasLigadas()
        {
            Assert.IsTrue(File.Exists(Prefab), $"'{Prefab}' ausente.");

            string yaml = File.ReadAllText(Prefab);
            var soltas = new List<string>();

            foreach (var campo in CamposObrigatorios)
            {
                var m = Regex.Match(yaml, campo + @":\s*\{fileID:\s*(-?\d+)");

                if (!m.Success) soltas.Add($"{campo}: campo ausente no prefab");
                else if (m.Groups[1].Value == "0") soltas.Add($"{campo}: nulo");
            }

            Assert.IsEmpty(soltas,
                "HUD persistente com referência solta:" + NovaLinha + "  " +
                string.Join(NovaLinha + "  ", soltas) + NovaLinha + NovaLinha +
                "Conserto: 'Tools/FavelaAmarela/HUD: extrair para prefab persistente'.");
        }

        /// <summary>
        /// O bootstrap tem de existir <b>e</b> carregar pelo nome certo. Um erro de digitação
        /// aqui não quebra compilação: <c>Resources.Load</c> devolve <c>null</c> em silêncio.
        /// </summary>
        [Test]
        public void OHudController_CarregaOPrefabNoBootstrap()
        {
            string codigo = File.ReadAllText("Assets/Scripts/UI/HUDController.cs");

            StringAssert.Contains("RuntimeInitializeOnLoadMethod", codigo,
                "HUDController não tem bootstrap — o HUD não nasceria em cena nenhuma.");

            StringAssert.Contains("DontDestroyOnLoad", codigo,
                "Sem DontDestroyOnLoad o HUD morre na primeira troca de cena.");

            StringAssert.Contains("\"HUD_Gameplay\"", codigo,
                "O nome carregado por Resources.Load precisa bater com o do prefab.");
        }

        /// <summary>
        /// Com <c>DontDestroyOnLoad</c>, recarregar uma cena instanciaria um segundo HUD por
        /// cima do primeiro. A guarda de singleton é o que impede isso — e é a mesma do
        /// <c>InventoryManager</c>.
        /// </summary>
        [Test]
        public void OHudController_TemGuardaDeDuplicata()
        {
            string codigo = File.ReadAllText("Assets/Scripts/UI/HUDController.cs");

            StringAssert.Contains("Instancia != null && Instancia != this", codigo,
                "Sem guarda de duplicata, recarregar uma cena empilha HUDs.");
        }

        /// <summary>
        /// O HUD nasce oculto e só aparece onde há mundo de jogo. Sem isso ele ficaria por cima
        /// do menu principal — que é uma cena sem <c>GameLoopBootstrap</c>.
        /// </summary>
        [Test]
        public void OHud_NasceOcultoEEhReveladoPeloBootstrap()
        {
            string hud = File.ReadAllText("Assets/Scripts/UI/HUDController.cs");
            string boot = File.ReadAllText("Assets/Scripts/GameLoop/GameLoopBootstrap.cs");

            StringAssert.Contains("sceneLoaded", hud,
                "O HUD precisa se ocultar a cada troca de cena.");
            StringAssert.Contains("public void Revelar()", hud,
                "Falta o método que o bootstrap chama para mostrar o HUD.");
            StringAssert.Contains("Revelar()", boot,
                "O GameLoopBootstrap não revela o HUD — ele ficaria invisível no jogo inteiro.");
        }

        /// <summary>
        /// As telas de fluxo vivem no prefab, então a ligação com <c>GameStatePresenter</c> e
        /// <c>PlayerDeathController</c> <b>não pode</b> ser serializada na cena: apontaria para
        /// um objeto de fora dela. Tem de ser feita em runtime.
        /// </summary>
        [Test]
        public void AsTelasDeFluxo_SaoLigadasEmRuntime()
        {
            string boot = File.ReadAllText("Assets/Scripts/GameLoop/GameLoopBootstrap.cs");

            StringAssert.Contains("DefinirTelaPause", boot,
                "O bootstrap não entrega a tela de pause ao GameStatePresenter — Esc " +
                "congelaria o mundo sem mostrar nada.");

            StringAssert.Contains("DefinirSequenciaColapso", boot,
                "O bootstrap não entrega a sequência de Colapso ao PlayerDeathController — " +
                "morrer aconteceria em silêncio.");
        }

        /// <summary>
        /// Nenhuma cena pode voltar a carregar a sua própria cópia do HUD: com o persistente
        /// nascendo antes, a cópia se autodestruiria pela guarda de singleton, e qualquer coisa
        /// pendurada nela iria junto — foi exatamente assim que <c>Tela_Pause</c> e
        /// <c>Tela_Colapso</c> se perderam durante esta migração.
        /// </summary>
        [Test]
        public void NenhumaCena_TemCopiaDoHud()
        {
            string guid = GuidDoScript("HUDController");
            Assert.IsNotNull(guid, "Não achei o .meta do HUDController.");

            var comCopia = Directory.GetFiles("Assets/Scenes", "*.unity")
                .Where(c => File.ReadAllText(c).Contains(guid))
                .Select(Path.GetFileName)
                .ToList();

            Assert.IsEmpty(comCopia,
                "Cena(s) com cópia própria do HUD: " + string.Join(", ", comCopia) +
                ". Conserto: 'Tools/FavelaAmarela/HUD: limpar cópias das cenas'.");
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
