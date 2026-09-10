using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// A tela de créditos existe, está ligada no menu, e <b>cita quem as licenças mandam citar</b>.
    ///
    /// <para><b>Por que isto é guarda e não cortesia (2026-09-10).</b> O Sucart (Rei em Amarelo)
    /// escreveu <i>"as long as you credit me using the name Sucart"</i>; o Warren Clark autorizou o
    /// uso a quem lhe desse crédito. Cada arquivo <c>LICENCA_*.txt</c>/<c>LICENSE.txt</c> do repo
    /// nomeia um autor; se o nome sumir do <c>CREDITOS.txt</c>, a build viola a condição em
    /// silêncio — e a submissão do edital é uma build.</para>
    /// </summary>
    public sealed class CreditosTests
    {
        private const string Creditos = "Assets/FavelaAmarela/Resources/CREDITOS.txt";
        private const string Prefab = "Assets/FavelaAmarela/Resources/Painel_Creditos.prefab";
        private const string CenaDoMenu = "Assets/Scenes/Cena_Menu.unity";

        /// <summary>
        /// (arquivo de licença que prova a obrigação, nome exigido). A lista é explícita de
        /// propósito: um regex sobre os arquivos acharia "Hypnobius" mas também "Thank you".
        /// </summary>
        private static readonly (string Licenca, string Nome)[] Obrigatorios =
        {
            ("Assets/FavelaAmarela/Art/Enemies/ReiEmAmarelo/LICENCA_Sucart.txt", "Sucart"),
            ("Assets/FavelaAmarela/Art/Characters/Damiao/Animado/LICENCA_WarrenClark.txt", "Warren Clark"),
            ("Assets/FavelaAmarela/Art/Enemies/Abdul/LICENCA_WarrenClark.txt", "Warren Clark"),
            ("Assets/FavelaAmarela/Art/Enemies/Abdul/LICENCA_AshDeal.txt", "AshDeal"),
            ("Assets/ThirdParty/DarkAgesUI/DarkAgesUi_v1.0/LICENSE.txt", "Hypnobius"),
            ("Assets/ThirdParty/Kenney/kenney_isometric-miniature-dungeon/License.txt", "Kenney"),
            ("Assets/FavelaAmarela/Art/Items/Icones/LICENCA_CraftPix_UndeadLoot.txt", "CraftPix"),
        };

        [Test]
        public void OTextoDosCreditos_CitaTodoAutorComLicencaNoRepo()
        {
            Assert.IsTrue(File.Exists(Creditos), $"Sem {Creditos} — a tela de créditos abriria vazia.");
            string texto = File.ReadAllText(Creditos);

            var faltam = new List<string>();
            foreach (var (licenca, nome) in Obrigatorios)
            {
                Assert.IsTrue(File.Exists(licenca),
                    $"A licença {licenca} sumiu do repo — se a arte saiu, tire a linha desta lista; " +
                    "se não saiu, a prova da obrigação é que sumiu.");
                if (!texto.Contains(nome)) faltam.Add($"{nome} (exigido por {Path.GetFileName(licenca)})");
            }

            Assert.IsEmpty(faltam,
                "CREDITOS.txt não cita: " + string.Join(", ", faltam) + ". Para o Sucart e o Warren " +
                "Clark isso é condição de uso; para os outros, é a palavra dada.");
        }

        [Test]
        public void OPrefabDaTela_ExisteEmResources_ELeOTexto()
        {
            Assert.IsTrue(File.Exists(Prefab),
                $"Sem {Prefab}: PainelDeCreditos.GarantirInstancia loga erro e o jogo roda sem créditos. " +
                "Rode 'Tools/FavelaAmarela/UI: montar o painel de créditos'.");

            string meta = Creditos + ".meta";
            string guid = Regex.Match(File.ReadAllText(meta), @"(?m)^guid:\s*([0-9a-f]{32})").Groups[1].Value;

            Assert.IsTrue(File.ReadAllText(Prefab).Contains(guid),
                "O prefab não referencia o CREDITOS.txt — ele ainda carrega por Resources em runtime, " +
                "mas a ferramenta deixou o campo 'texto' solto; rode-a de novo.");
        }

        [Test]
        public void OMenuPrincipal_TemOBotaoDeCreditosLigado()
        {
            string cena = File.ReadAllText(CenaDoMenu);

            var m = Regex.Match(cena, @"botaoDeCreditos:\s*\{fileID:\s*(-?\d+)\}");
            Assert.IsTrue(m.Success, "MenuPrincipal na Cena_Menu não tem o campo 'botaoDeCreditos' serializado.");
            Assert.AreNotEqual("0", m.Groups[1].Value,
                "'botaoDeCreditos' está vazio na Cena_Menu: a tela existe e o jogador não chega nela. " +
                "Rode 'Tools/FavelaAmarela/UI: ligar o botão de Créditos'.");

            Assert.IsTrue(cena.Contains("m_Name: Botao_Creditos"),
                "Não há 'Botao_Creditos' na Cena_Menu.");
        }
    }
}
