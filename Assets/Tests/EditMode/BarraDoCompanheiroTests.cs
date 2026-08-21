using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a barra da <b>Resiliência do Companheiro</b> (Yug-Neth) — item 3 do Vertical Slice.
    ///
    /// <para><b>Por que esta cadeia merece guarda própria:</b> ela tem o formato exato do que
    /// falha calado neste projeto. A <c>VigorBar</c> já ficou órfã — 0 cenas, 0 prefabs — com o
    /// dado sendo injetado e nenhuma view ligada para mostrá-lo, e nada no console apontava a
    /// causa. A barra do companheiro é ainda mais fácil de perder, porque ela <b>nasce
    /// desativada</b> de propósito: uma barra ausente do HUD parece exatamente igual, esteja ela
    /// corretamente oculta ou nunca montada.</para>
    ///
    /// <para><b>O elo que ninguém mais cobre:</b> a barra é ligada por <b>evento</b>
    /// (<c>CompanionManager.OnCompanheiroRegistrado</c>), porque Yug-Neth só vira companheiro no
    /// meio do jogo. Se esse evento sumir numa refatoração, o jogo continua compilando, a barra
    /// continua montada, e ela simplesmente nunca aparece.</para>
    /// </summary>
    public sealed class BarraDoCompanheiroTests
    {
        private const string HUDController = "Assets/Scripts/UI/HUDController.cs";
        private const string CompanionManager = "Assets/Scripts/Player/CompanionManager.cs";
        private const string Bootstrap = "Assets/Scripts/GameLoop/GameLoopBootstrap.cs";
        private const string BuildHUD = "Assets/FavelaAmarela/Editor/BuildHUDCompleto.cs";

        /// <summary>Cenas jogáveis onde o HUD tem que estar completo.</summary>
        private static readonly string[] CenasJogaveis =
        {
            "Assets/Scenes/Deserto_Hali.unity",
            "Assets/Scenes/Playtest_RuinasPalidas.unity",
            "Assets/Scenes/Santuario_Yhtill.unity",
            "Assets/Scenes/Portoes_Das_Ruinas.unity",
            "Assets/Scenes/Castelo_Carcosa.unity",
        };

        [Test]
        public void ACadeiaDeLigacao_EstaInteira()
        {
            var falhas = new List<string>();

            void Exigir(string arquivo, string trecho, string porque)
            {
                if (!File.Exists(arquivo)) { falhas.Add($"{arquivo}: não existe"); return; }
                if (!File.ReadAllText(arquivo).Contains(trecho))
                    falhas.Add($"{Path.GetFileName(arquivo)}: falta '{trecho}' — {porque}");
            }

            Exigir(CompanionManager, "OnCompanheiroRegistrado",
                   "sem o evento, ninguém sabe quando Yug-Neth é libertado sem fazer polling");

            Exigir(Bootstrap, "OnCompanheiroRegistrado +=",
                   "o bootstrap não assinaria o registro e a barra nunca apareceria");

            // Os DOIS caminhos: o evento cobre a libertação nesta cena; a ligação imediata cobre
            // trocar de cena depois de libertá-lo (ou carregar um save). Sem o segundo, a barra
            // some na primeira transição depois da Tumba.
            Exigir(Bootstrap, "companheiro.YugNeth != null",
                   "sem ligar na hora, a barra some ao trocar de cena com o companheiro já solto");

            Exigir(HUDController, "InjetarCompanheiro",
                   "sem o ponto de injeção, nada liga a barra");

            Exigir(HUDController, "companheiroBar",
                   "sem o campo serializado, o HUD não tem onde guardar a view");

            Exigir(BuildHUD, "CompanheiroBar",
                   "o montador do HUD não criaria a barra em cena nenhuma");

            Assert.IsEmpty(falhas,
                "Cadeia da barra do companheiro rompida — o jogo compila e a barra nunca " +
                "aparece:\n  " + string.Join("\n  ", falhas));
        }

        /// <summary>
        /// A barra tem que estar montada em <b>toda</b> cena jogável — e <b>desativada</b>.
        ///
        /// <para>Os dois lados importam. Ausente, ela nunca aparece quando Yug-Neth é libertado.
        /// Ativa desde o começo, ela anuncia no menu um recurso que o jogador ainda não tem e
        /// lê como recurso zerado.</para>
        /// </summary>
        [Test]
        public void ABarra_EstaEmTodaCenaJogavel_ENasceOculta()
        {
            string guid = GuidDoScript("CompanheiroBar");
            Assert.IsNotNull(guid, "Script CompanheiroBar não encontrado em Assets/Scripts.");

            var falhas = new List<string>();

            foreach (var cena in CenasJogaveis)
            {
                if (!File.Exists(cena)) { falhas.Add($"{Nome(cena)}: cena ausente"); continue; }

                string txt = File.ReadAllText(cena);

                if (!txt.Contains(guid))
                {
                    falhas.Add($"{Nome(cena)}: sem CompanheiroBar. Rode " +
                               "'Tools/FavelaAmarela/Montar HUD Completo'.");
                    continue;
                }

                if (EstaAtivo(txt, "Barra_Companheiro"))
                    falhas.Add($"{Nome(cena)}: a Barra_Companheiro nasce ATIVA — apareceria no " +
                               "HUD antes de Yug-Neth ser libertado, parecendo recurso zerado.");
            }

            Assert.IsEmpty(falhas,
                "Barra do companheiro mal montada:\n  " + string.Join("\n  ", falhas));
        }

        // ── Auxiliares ────────────────────────────────────────────────────────

        private static string Nome(string caminho) => Path.GetFileNameWithoutExtension(caminho);

        /// <summary>
        /// Lê <c>m_IsActive</c> do GameObject de nome dado.
        ///
        /// <para>Casa o documento do GameObject inteiro e procura o campo <b>dentro dele</b>, em
        /// vez de pegar o primeiro <c>m_IsActive</c> depois do nome: a ordem dos documentos no
        /// YAML não é a da hierarquia, e ler o campo do objeto errado já fez um teste deste
        /// projeto reprovar dado correto.</para>
        /// </summary>
        private static bool EstaAtivo(string txt, string nome)
        {
            var doc = Regex.Match(txt,
                $@"---\s*!u!1\s*&\d+\r?\nGameObject:(?:(?!^---)[\s\S])*?m_Name:\s*{Regex.Escape(nome)}\s*$" +
                @"(?:(?!^---)[\s\S])*",
                RegexOptions.Multiline);

            if (!doc.Success) return false;

            var ativo = Regex.Match(doc.Value, @"(?m)^\s*m_IsActive:\s*(\d)");
            return ativo.Success && ativo.Groups[1].Value == "1";
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
