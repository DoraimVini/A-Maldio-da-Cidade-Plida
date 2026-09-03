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
    /// Guarda a arte e a animação do Abdul Alhazred, hoje sobre o pacote
    /// <b>"sorcerer villain"</b> (ver <c>PROCEDENCIA_Abdul.txt</c>).
    ///
    /// <para><b>O que motivou:</b> a folha de duas trocas atrás
    /// (<c>abdul_alhazred_spritesheet.png</c>, gerada por IA) era <b>totalmente opaca</b> — o
    /// xadrez de transparência ficou achatado dentro do PNG, e o boss renderizava como um
    /// quadrado de 4×4 unidades com fundo claro. O teste de opacidade abaixo existe para essa
    /// classe de defeito não voltar: é invisível na compilação, no console e no Inspector, e
    /// só aparece olhando o jogo rodando.</para>
    ///
    /// <para><b>Histórico da arte.</b> Folha de IA (opaca) → <b>Mage</b> do Horror Enemy Pack
    /// (AshDeal, 2026-08-27) → <b>"sorcerer villain"</b> (2026-09-03). A troca do Mage não foi
    /// por gosto: o desenho dele ocupava 16×31 px numa célula de 112×48, contra os 32×81 do
    /// Damião — ver <c>OChefeNaoEhMenorQueOJogador</c>, escrito para essa medida não se perder
    /// de novo.</para>
    ///
    /// <para><b>A troca não mexeu em clipe, controller nem IA.</b> Os 29 sprites do meta
    /// mantiveram nome, <c>spriteID</c> e <c>internalID</c>; mudaram só os bytes do PNG e os 29
    /// blocos <c>rect</c>. Por isso os testes de clipe, controller e Animator abaixo continuam
    /// valendo palavra por palavra depois de uma troca completa de arte.</para>
    /// </summary>
    public sealed class AnimacaoDoAbdulTests
    {
        private const string Pasta = "Assets/FavelaAmarela/Art/Enemies/Abdul";
        private const string Folha = Pasta + "/Abdul_Mage_Sheet.png";
        private const string Controlador = Pasta + "/Abdul_AC_Mage.controller";
        private const string Prefab = "Assets/FavelaAmarela/Art/Enemies/Abdul_Alhazred.prefab";
        private const string Ai = "Assets/Scripts/Enemies/AbdulAlhazredAI.cs";
        private const string Procedencia = Pasta + "/PROCEDENCIA_Abdul.txt";

        /// <summary>Pacote de onde a arte do Abdul vem hoje. Ver <c>PROCEDENCIA_Abdul.txt</c>.</summary>
        private const string PacoteEmUso = "sorcerer villain";

        /// <summary>
        /// Piso de altura desenhada, em pixels. Não é meta de arte — é o limite abaixo do qual
        /// o chefe deixa de ler como chefe. Ver <c>OChefeNaoEhMenorQueOJogador</c>.
        /// </summary>
        private const int AlturaMinimaDesenhada = 64;

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
        /// A procedência fica <b>ao lado da arte</b>, não só como URL. Para um edital, um link é
        /// frágil: pode sair do ar, mudar, ou não valer como prova na submissão.
        ///
        /// <para><b>Por que este teste mudou de forma (2026-09-03).</b> Ele exigia
        /// <c>LICENCA_AshDeal.txt</c> contendo a palavra <c>commercial</c>. Quando a arte do
        /// Abdul foi trocada pelo pacote "sorcerer villain", o arquivo da AshDeal continuou lá,
        /// continuou dizendo "commercial", e o teste <b>continuou verde</b> — descrevendo uma
        /// arte que não estava mais no projeto. Um arquivo de licença que sobrevive à arte que
        /// ele cobre é pior que nenhum: ele passa por prova numa submissão.</para>
        ///
        /// <para>Então a pergunta virou outra: a procedência existe <b>e nomeia o pacote que
        /// está em uso</b>? Essa versão pega a troca de arte; a anterior, não.</para>
        /// </summary>
        [Test]
        public void ProcedenciaDaArte_NomeiaOPacoteEmUso()
        {
            Assert.IsTrue(File.Exists(Procedencia),
                $"Falta {Procedencia}. A arte do Abdul precisa ser rastreável na submissão do " +
                "edital: de que pacote veio, o que ela substituiu, e sob que licença está.");

            string txt = File.ReadAllText(Procedencia);
            Assert.IsTrue(txt.Contains(PacoteEmUso),
                $"{Procedencia} não menciona '{PacoteEmUso}', que é o pacote de onde a arte do " +
                "Abdul vem hoje. Ou a procedência envelheceu numa troca de arte, ou a constante " +
                "PacoteEmUso deste teste é que ficou para trás — os dois casos precisam de mão.");

            Assert.IsNotEmpty(Directory.GetFiles(Pasta, "LICENCA_*.txt"),
                $"Nenhum LICENCA_*.txt em {Pasta}. Se os termos do pacote ainda não foram " +
                "obtidos, o projeto já tem forma para isso: um LICENCA_PENDENTE.txt dizendo o " +
                "que falta, como em Art/Enemies/ReiEmAmarelo/.");
        }

        /// <summary>
        /// O chefe não pode ser menor que o jogador.
        ///
        /// <para><b>O defeito que motivou (2026-09-03).</b> O Abdul era o "Mage" da AshDeal, e
        /// o desenho dentro da célula de 112×48 ocupava <b>16×31 px</b> — 0,50 × 0,97 unidades
        /// de mundo. O Damião tem 32×81 px = 1,00 × 2,53. O <b>segundo chefe do jogo</b> tinha
        /// 38% da altura do jogador, e nenhum teste reclamava: a folha existia, tinha
        /// transparência, os cinco clipes estavam ligados e o controller tinha default. Tudo
        /// verde, e um chefe que não lia como chefe.</para>
        ///
        /// <para>Este é um <b>piso</b>, não uma meta de arte. Ele não diz qual deve ser o
        /// tamanho — só que abaixo dele a troca de arte deu errado.</para>
        /// </summary>
        [Test]
        public void OChefeNaoEhMenorQueOJogador()
        {
            var tex = new Texture2D(2, 2);
            Assert.IsTrue(tex.LoadImage(File.ReadAllBytes(Folha)), "PNG não decodificou.");

            var px = tex.GetPixels32();
            int topo = -1, baixo = -1;
            for (int y = 0; y < tex.height; y++)
            {
                bool temPixel = false;
                for (int x = 0; x < tex.width; x++)
                    if (px[y * tex.width + x].a > 8) { temPixel = true; break; }

                if (!temPixel) continue;
                if (topo < 0) topo = y;
                baixo = y;
            }

            Assert.Greater(topo, -1, "A folha do Abdul está inteiramente transparente.");

            int altura = baixo - topo + 1;
            Assert.GreaterOrEqual(altura, AlturaMinimaDesenhada,
                $"O Abdul desenhado tem {altura} px de altura — abaixo do piso de " +
                $"{AlturaMinimaDesenhada}. O Damião tem 81 px. Um chefe mais baixo que o " +
                "jogador não lê como chefe, e isso não aparece na compilação, no console nem " +
                "em nenhum outro teste desta classe.");
        }
    }
}
