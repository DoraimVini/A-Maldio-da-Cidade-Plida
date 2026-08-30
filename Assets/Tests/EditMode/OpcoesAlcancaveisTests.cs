using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda que a tela de <b>Opções</b> exista <b>e seja alcançável</b>.
    ///
    /// <para><b>As duas coisas, porque não são a mesma.</b> Este repositório coleciona peças que
    /// existem, compilam, não dão erro e ninguém consegue chamar: o <c>ProgressionManager</c>
    /// fora de cena, o <c>GeradorDeItem</c> sem chamador, a tabela de drop do Abdul apontando
    /// para nada. Um painel de opções perfeito atrás de nenhum botão é a mesma coisa.</para>
    /// </summary>
    public sealed class OpcoesAlcancaveisTests
    {
        private const string Painel = "Assets/FavelaAmarela/Resources/Painel_Opcoes.prefab";
        private const string Hud = "Assets/FavelaAmarela/Resources/HUD_Gameplay.prefab";
        private const string CenaDoMenu = "Assets/Scenes/Cena_Menu.unity";

        // ── A tela existe e está inteira ──────────────────────────────────────

        /// <summary>
        /// O painel é carregado por <c>Resources.Load("Painel_Opcoes")</c> em
        /// <c>BeforeSceneLoad</c>. Fora de <c>Resources</c>, ou com outro nome, o jogo roda sem
        /// opções e só um <c>LogError</c> avisa.
        /// </summary>
        [Test]
        public void OPainel_ExisteEmResources()
        {
            Assert.IsTrue(File.Exists(Painel),
                $"{Painel} não existe. Conserto: 'Tools/FavelaAmarela/UI: montar o painel de " +
                "opções'.");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Painel);
            Assert.IsNotNull(prefab?.GetComponent<PainelDeOpcoes>(),
                "O prefab existe mas não tem o componente PainelDeOpcoes.");
        }

        /// <summary>
        /// Toda referência do painel ligada. Um <c>Slider</c> solto não quebra nada — só faz a
        /// barra de volume não mexer em nada, que é o defeito mais difícil de notar.
        /// </summary>
        [Test]
        public void TodoControleDoPainel_EstaLigado()
        {
            string yaml = File.ReadAllText(Painel);
            var soltos = new List<string>();

            foreach (var campo in new[]
                     {
                         "conteudo", "barraDeVolume", "rotuloDoVolume",
                         "alternadorDeTelaCheia", "alternadorDeVSync",
                         "seletorDeQuadros", "botaoDeFechar", "botaoDeRestaurar",
                     })
            {
                var m = Regex.Match(yaml, $@"^\s*{campo}:\s*\{{fileID:\s*(-?\d+)",
                                    RegexOptions.Multiline);

                if (!m.Success) soltos.Add($"{campo}: não existe mais no componente");
                else if (m.Groups[1].Value == "0") soltos.Add($"{campo}: NULO");
            }

            Assert.IsEmpty(soltos,
                "Controle(s) do painel de opções sem referência:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", soltos));
        }

        // ── E é alcançável dos DOIS lugares ───────────────────────────────────

        /// <summary>
        /// <b>Antes</b> de começar (menu principal) e <b>durante</b> a partida (pausa). Ligar só
        /// um entrega metade da funcionalidade, e qual metade depende de quem está jogando.
        /// </summary>
        [TestCase(Hud, "MenuDePause", "a tela de pausa")]
        [TestCase(CenaDoMenu, "MenuPrincipal", "o menu principal")]
        public void OBotaoDeOpcoes_EstaLigado(string arquivo, string componente, string onde)
        {
            Assert.IsTrue(File.Exists(arquivo), $"{arquivo} não existe.");

            string yaml = File.ReadAllText(arquivo);

            StringAssert.Contains($"UI.{componente}", yaml,
                $"{componente} sumiu de {arquivo}.");

            var m = Regex.Match(yaml, @"^\s*botaoDeOpcoes:\s*\{fileID:\s*(-?\d+)",
                                RegexOptions.Multiline);

            Assert.IsTrue(m.Success,
                $"O campo 'botaoDeOpcoes' não aparece em {arquivo} — o componente foi salvo " +
                "antes do campo existir, ou o campo foi removido.");

            Assert.AreNotEqual("0", m.Groups[1].Value,
                $"O botão de Opções não está ligado em {onde}. A tela existe e o jogador não " +
                "tem como chegar nela. Conserto: " +
                "'Tools/FavelaAmarela/UI: ligar o botão de Opções'.");
        }

        /// <summary>
        /// Os dois menus precisam <b>chamar</b> a abertura. Um campo ligado a um botão que não
        /// escuta nada é o mesmo nada, com aparência melhor.
        /// </summary>
        [TestCase("MenuDePause")]
        [TestCase("MenuPrincipal")]
        public void OsMenus_AbremOPainel(string componente)
        {
            string fonte = File.ReadAllText($"Assets/Scripts/UI/{componente}.cs");

            StringAssert.Contains("botaoDeOpcoes.onClick.AddListener", fonte,
                $"{componente} tem o campo e não assina o clique.");

            StringAssert.Contains("PainelDeOpcoes.AbrirSeExistir", fonte,
                $"{componente} assina o clique e não abre o painel.");
        }

        /// <summary>
        /// A tela vive <b>fora</b> do HUD de propósito: o HUD se oculta em toda cena sem
        /// <c>GameLoopBootstrap</c> — ou seja, no menu principal, que é justamente onde as
        /// opções precisam aparecer.
        /// </summary>
        [Test]
        public void OPainel_NaoDependeDoHud()
        {
            string fonte = File.ReadAllText("Assets/Scripts/UI/PainelDeOpcoes.cs");

            StringAssert.Contains("RuntimeInitializeOnLoadMethod", fonte,
                "O painel deixou de nascer sozinho.");

            StringAssert.DoesNotContain("HUDController", fonte,
                "O painel de opções passou a depender do HUDController. O HUD se oculta no " +
                "menu principal, e as opções sumiriam justamente de onde mais se procura por " +
                "elas.");
        }
    }
}
