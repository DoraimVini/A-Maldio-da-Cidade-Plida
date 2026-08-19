using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
// LoadImage é método de extensão de UnityEngine.ImageConversion: sem este using ele não
// resolve, mesmo qualificando Texture2D pelo nome completo.
using UnityEngine;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a arte e a animação do Abdul Alhazred, agora sobre o <b>Mage</b> do Horror Enemy
    /// Pack (AshDeal).
    ///
    /// <para><b>O que motivou:</b> a folha anterior
    /// (<c>abdul_alhazred_spritesheet.png</c>, gerada por IA) era <b>totalmente opaca</b> — o
    /// xadrez de transparência ficou achatado dentro do PNG, e o boss renderizava como um
    /// quadrado de 4×4 unidades com fundo claro. O teste de opacidade abaixo existe para essa
    /// classe de defeito não voltar: é invisível na compilação, no console e no Inspector, e
    /// só aparece olhando o jogo rodando.</para>
    /// </summary>
    public sealed class AnimacaoDoAbdulTests
    {
        private const string Pasta = "Assets/FavelaAmarela/Art/Enemies/Abdul";
        private const string Folha = Pasta + "/Abdul_Mage_Sheet.png";
        private const string Controlador = Pasta + "/Abdul_AC_Mage.controller";
        private const string Licenca = Pasta + "/LICENCA_AshDeal.txt";
        private const string Prefab = "Assets/FavelaAmarela/Art/Enemies/Abdul_Alhazred.prefab";
        private const string Ai = "Assets/Scripts/Enemies/AbdulAlhazredAI.cs";

        private static readonly string[] Animacoes = { "idle", "walk", "attack", "hit", "death" };

        [Test]
        public void OsCincoClipes_ExistemComQuadros()
        {
            var falhas = new List<string>();

            foreach (var nome in Animacoes)
            {
                string caminho = $"{Pasta}/Abdul_{nome}_Mage.anim";
                if (!File.Exists(caminho)) { falhas.Add($"{nome}: clipe ausente"); continue; }

                if (Regex.Matches(File.ReadAllText(caminho), @"- time:").Count == 0)
                    falhas.Add($"{nome}: clipe sem keyframe de sprite");
            }

            Assert.IsEmpty(falhas,
                "Clipes do Abdul incompletos. Rode 'Tools/FavelaAmarela/Ligar animacao do Abdul " +
                "(Mage)'.\n  " + string.Join("\n  ", falhas));
        }

        /// <summary>
        /// A folha precisa ter transparência de verdade. Este é o teste que a folha antiga
        /// reprovaria: alfa 255 em todo pixel, com o xadrez desenhado dentro da imagem.
        /// </summary>
        [Test]
        public void Folha_TemTransparenciaDeVerdade()
        {
            Assert.IsTrue(File.Exists(Folha), $"Folha ausente: {Folha}");

            // Mede o PIXEL, não o que o importador promete: a folha antiga também declarava
            // alphaIsTransparency e mesmo assim era 100% opaca. LoadImage decodifica o PNG do
            // disco sem depender de isReadable nem do estado do AssetDatabase.
            var tex = new Texture2D(2, 2);
            Assert.IsTrue(tex.LoadImage(File.ReadAllBytes(Folha)), "PNG não decodificou.");

            var px = tex.GetPixels32();
            int opacos = px.Count(p => p.a > 250);
            float pct = 100f * opacos / px.Length;

            Assert.Less(pct, 92f,
                $"A folha do Abdul está {pct:0.0}% opaca — praticamente sem transparência. " +
                "Foi exatamente assim que a folha anterior quebrou: o xadrez de transparência " +
                "ficou achatado dentro do PNG e o boss renderizava como um quadrado claro de " +
                "4×4 unidades. Não aparece na compilação, no console nem no Inspector.");
        }

        [Test]
        public void Controlador_TemOsEstados_E_UmDefault()
        {
            Assert.IsTrue(File.Exists(Controlador), $"Controller ausente: {Controlador}");

            string txt = File.ReadAllText(Controlador);

            foreach (var nome in Animacoes)
                Assert.IsTrue(Regex.IsMatch(txt, $@"m_Name:\s*{nome}\b"),
                    $"O Abdul_AC_Mage não tem o estado '{nome}'.");

            var padrao = Regex.Match(txt, @"m_DefaultState:\s*\{fileID:\s*(-?\d+)\}");
            Assert.IsTrue(padrao.Success && padrao.Groups[1].Value != "0",
                "Abdul_AC_Mage sem m_DefaultState — o boss entraria na luta sem animação nenhuma.");
        }

        [Test]
        public void Prefab_TemAnimator_ApontandoParaOControlador()
        {
            Assert.IsTrue(File.Exists(Prefab), $"Prefab ausente: {Prefab}");

            string txt = File.ReadAllText(Prefab);

            Assert.IsTrue(Regex.IsMatch(txt, @"(?m)^Animator:"),
                "Abdul_Alhazred.prefab está sem componente Animator.");

            string guid = Regex.Match(File.ReadAllText(Controlador + ".meta"),
                                      @"(?m)^guid:\s*([0-9a-f]{32})").Groups[1].Value;

            Assert.IsTrue(txt.Contains(guid),
                "O Animator do Abdul não aponta para o Abdul_AC_Mage. Um Animator sem " +
                "controller é tão estático quanto não ter Animator, e não avisa.");
        }

        /// <summary>Mede código, não prosa: linhas de comentário saem antes da busca.</summary>
        [Test]
        public void AbdulAlhazredAI_DirigeOAnimator()
        {
            Assert.IsTrue(File.Exists(Ai), $"Script ausente: {Ai}");

            string codigo = string.Join("\n", File.ReadAllLines(Ai)
                .Where(l => !l.TrimStart().StartsWith("//")));

            Assert.IsTrue(codigo.Contains("animator.Play("),
                "AbdulAlhazredAI não chama animator.Play — o boss ficaria no estado default por " +
                "toda a luta.");

            Assert.IsTrue(Regex.IsMatch(codigo, @"HandleConjurarConeDeGelo[\s\S]{0,220}Anim\.Attack"),
                "A conjuração do Cone de Gelo não dispara a animação de ataque — o jogador " +
                "perderia o aviso visual do golpe.");
        }

        /// <summary>
        /// Os termos da licença ficam <b>ao lado da arte</b>, não só como URL. Para um edital,
        /// um link é frágil: pode sair do ar, mudar, ou não valer como prova na submissão.
        /// </summary>
        [Test]
        public void LicencaDoPacote_EstaNoRepositorio()
        {
            Assert.IsTrue(File.Exists(Licenca),
                $"Falta a cópia dos termos em {Licenca}. A arte do Abdul vem do Horror Pixel " +
                "Art Enemy Pack (AshDeal) e o pacote precisa ser rastreável na submissão.");

            string txt = File.ReadAllText(Licenca);
            Assert.IsTrue(txt.Contains("commercial"),
                "A cópia da licença não menciona uso comercial — confira se é o arquivo certo.");
        }
    }
}
