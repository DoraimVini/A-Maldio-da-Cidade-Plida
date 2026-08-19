using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a arte e a animação do Rei em Amarelo, agora sobre o <b>Moonstone Keeper</b>
    /// (SUCART).
    ///
    /// <para>Antes ele era um recorte do spritesheet "Necromancer" da Inbox — arquétipo certo,
    /// cores erradas, sem animação. Com os três chefes ligados, este é o último a sair do
    /// quadro parado.</para>
    /// </summary>
    public sealed class AnimacaoDoReiEmAmareloTests
    {
        private const string Pasta = "Assets/FavelaAmarela/Art/Enemies/ReiEmAmarelo";
        private const string Controlador = Pasta + "/ReiEmAmarelo_AC.controller";
        private const string Prefab = "Assets/FavelaAmarela/Art/Enemies/ReiEmAmarelo.prefab";
        private const string Ai = "Assets/Scripts/Enemies/ReiEmAmareloAI.cs";

        private static readonly string[] Animacoes = { "idle", "selar", "desvelo", "dano", "queda" };

        [Test]
        public void OsCincoClipes_ExistemComQuadros()
        {
            var falhas = new List<string>();

            foreach (var nome in Animacoes)
            {
                string caminho = $"{Pasta}/Rei_{nome}.anim";
                if (!File.Exists(caminho)) { falhas.Add($"{nome}: clipe ausente"); continue; }

                if (Regex.Matches(File.ReadAllText(caminho), @"- time:").Count == 0)
                    falhas.Add($"{nome}: clipe sem keyframe de sprite");
            }

            Assert.IsEmpty(falhas,
                "Clipes do Rei incompletos. Rode 'Tools/FavelaAmarela/Ligar animacao do Rei em " +
                "Amarelo'.\n  " + string.Join("\n  ", falhas));
        }

        /// <summary>
        /// As folhas <c>idle</c> (2805px) e <c>queda</c> (3135px) passam do teto padrão de 2048
        /// da Unity. Se o <c>maxTextureSize</c> do <b>DefaultTexturePlatform</b> não subir, a
        /// textura é <b>reescalada em silêncio</b> — pixel art borrada, sem erro nenhum. Os
        /// blocos por plataforma não bastam: nascem com <c>overridden: 0</c> e são inertes.
        /// </summary>
        [Test]
        public void FolhasGrandes_NaoSaoReescaladasNoImport()
        {
            var falhas = new List<string>();

            foreach (var png in Directory.EnumerateFiles(Pasta, "Rei_*.png"))
            {
                string meta = png + ".meta";
                if (!File.Exists(meta)) { falhas.Add($"{Path.GetFileName(png)}: sem .meta"); continue; }

                string txt = File.ReadAllText(meta);
                string nome = Path.GetFileNameWithoutExtension(png);

                // Largura real da folha = quadros × largura do quadro, lida das próprias fatias.
                var rects = Regex.Matches(txt, @"width:\s*(\d+)\s*\r?\n\s*height:\s*(\d+)");
                int fatias = Regex.Matches(txt, @"(?m)^\s+name:\s*rei_").Count;
                if (fatias == 0 || rects.Count == 0) { falhas.Add($"{nome}: sem fatias"); continue; }

                int larguraDoQuadro = int.Parse(rects[0].Groups[1].Value);
                int larguraDaFolha = larguraDoQuadro * fatias;

                var teto = Regex.Match(txt, @"(?m)^\s{2}maxTextureSize:\s*(\d+)");
                if (!teto.Success) { falhas.Add($"{nome}: sem maxTextureSize no topo do meta"); continue; }

                int limite = int.Parse(teto.Groups[1].Value);
                if (larguraDaFolha > limite)
                    falhas.Add($"{nome}: folha de {larguraDaFolha}px com maxTextureSize={limite} " +
                               "— a Unity reescala e borra a arte, sem avisar");
            }

            Assert.IsEmpty(falhas, "Import reescalaria a arte:\n  " + string.Join("\n  ", falhas));
        }

        [Test]
        public void Controlador_TemOsEstados_E_UmDefault()
        {
            Assert.IsTrue(File.Exists(Controlador), $"Controller ausente: {Controlador}");

            string txt = File.ReadAllText(Controlador);

            foreach (var nome in Animacoes)
                Assert.IsTrue(Regex.IsMatch(txt, $@"m_Name:\s*{nome}\b"),
                    $"O ReiEmAmarelo_AC não tem o estado '{nome}'.");

            var padrao = Regex.Match(txt, @"m_DefaultState:\s*\{fileID:\s*(-?\d+)\}");
            Assert.IsTrue(padrao.Success && padrao.Groups[1].Value != "0",
                "ReiEmAmarelo_AC sem m_DefaultState.");
        }

        [Test]
        public void Prefab_TemAnimator_ApontandoParaOControlador()
        {
            Assert.IsTrue(File.Exists(Prefab), $"Prefab ausente: {Prefab}");

            string txt = File.ReadAllText(Prefab);

            Assert.IsTrue(Regex.IsMatch(txt, @"(?m)^Animator:"),
                "ReiEmAmarelo.prefab está sem componente Animator.");

            string guid = Regex.Match(File.ReadAllText(Controlador + ".meta"),
                                      @"(?m)^guid:\s*([0-9a-f]{32})").Groups[1].Value;

            Assert.IsTrue(txt.Contains(guid), "O Animator do Rei não aponta para o ReiEmAmarelo_AC.");
        }

        /// <summary>Mede código, não prosa.</summary>
        [Test]
        public void ReiEmAmareloAI_DirigeOAnimator()
        {
            Assert.IsTrue(File.Exists(Ai), $"Script ausente: {Ai}");

            string codigo = string.Join("\n", File.ReadAllLines(Ai)
                .Where(l => !l.TrimStart().StartsWith("//")));

            Assert.IsTrue(codigo.Contains("animator.Play("),
                "ReiEmAmareloAI não chama animator.Play.");

            Assert.IsTrue(codigo.Contains("OnReliquiaAtivada"),
                "O AI não assina OnReliquiaAtivada — o Rei não daria retorno visual nenhum ao " +
                "travar uma relíquia, e o jogador não saberia que a ação surtiu efeito.");
        }

        /// <summary>
        /// O pacote da SUCART <b>não traz arquivo de licença</b> — conferido no zip. Enquanto os
        /// termos não forem capturados da página do autor, o aviso fica no repositório, ao lado
        /// da arte. Este teste existe para o aviso não ser apagado por engano.
        /// </summary>
        [Test]
        public void AvisoDeLicencaPendente_ContinuaNoRepositorio()
        {
            string caminho = Pasta + "/LICENCA_PENDENTE.txt";

            Assert.IsTrue(File.Exists(caminho),
                "Sumiu o aviso de licença pendente do Moonstone Keeper. O pacote não trouxe " +
                "termos; se eles já foram capturados, substitua o arquivo por LICENCA_SUCART.txt " +
                "e atualize este teste.");
        }
    }
}
