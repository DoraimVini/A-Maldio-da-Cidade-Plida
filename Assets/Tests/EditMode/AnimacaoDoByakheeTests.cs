using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a animação do Byakhee — o primeiro chefe do jogo a se mexer.
    ///
    /// <para><b>O que motivou:</b> até 2026-08-19 existiam 7 clipes <c>.anim</c> no projeto
    /// inteiro, todos do Abdul e todos desligados; nenhum dos três chefes tinha
    /// <c>Animator</c>. Arte animada existia e não estava ligada — o modo de falha dominante
    /// deste projeto. Estes testes existem para a ligação não se desfazer em silêncio: sem
    /// eles, apagar o <c>Animator</c> do prefab não quebra compilação, não gera erro no
    /// console, e o boss volta a ser um quadro parado.</para>
    /// </summary>
    public sealed class AnimacaoDoByakheeTests
    {
        private const string Pasta = "Assets/FavelaAmarela/Art/Enemies/Byakhee";
        private const string Controlador = Pasta + "/Byakhee_AC.controller";
        private const string Prefab = "Assets/FavelaAmarela/Art/Enemies/Byakhee.prefab";
        private const string Ai = "Assets/Scripts/Enemies/ByakheeAI.cs";

        private static readonly string[] Animacoes =
            { "espreita", "rasante", "garras", "grito", "dano", "derrota" };

        [Test]
        public void OsSeisClipes_ExistemComQuadros()
        {
            var falhas = new System.Collections.Generic.List<string>();

            foreach (var nome in Animacoes)
            {
                string caminho = $"{Pasta}/Byakhee_{nome}.anim";
                if (!File.Exists(caminho)) { falhas.Add($"{nome}: clipe ausente"); continue; }

                string txt = File.ReadAllText(caminho);

                // Um clipe sem keyframe de sprite compila, abre no Editor e nao anima nada.
                int quadros = Regex.Matches(txt, @"- time:").Count;
                if (quadros == 0) falhas.Add($"{nome}: clipe sem keyframe de sprite");
            }

            Assert.IsEmpty(falhas,
                "Clipes do Byakhee incompletos. Rode 'Tools/FavelaAmarela/Ligar animacao do " +
                "Byakhee'.\n  " + string.Join("\n  ", falhas));
        }

        [Test]
        public void Controlador_TemOsSeisEstados_E_UmDefault()
        {
            Assert.IsTrue(File.Exists(Controlador), $"Controller ausente: {Controlador}");

            string txt = File.ReadAllText(Controlador);

            foreach (var nome in Animacoes)
                Assert.IsTrue(Regex.IsMatch(txt, $@"m_Name:\s*{nome}\b"),
                    $"O Byakhee_AC não tem o estado '{nome}'.");

            // Sem estado default o Animator nao toca nada ao entrar na camada.
            var padrao = Regex.Match(txt, @"m_DefaultState:\s*\{fileID:\s*(-?\d+)\}");
            Assert.IsTrue(padrao.Success && padrao.Groups[1].Value != "0",
                "O Byakhee_AC está sem m_DefaultState — o boss entraria na luta sem animação " +
                "nenhuma, exatamente o sintoma que o Abdul tem hoje.");
        }

        [Test]
        public void Prefab_TemAnimator_ApontandoParaOControlador()
        {
            Assert.IsTrue(File.Exists(Prefab), $"Prefab ausente: {Prefab}");

            string txt = File.ReadAllText(Prefab);

            Assert.IsTrue(Regex.IsMatch(txt, @"(?m)^Animator:"),
                "Byakhee.prefab está sem componente Animator — a arte animada não seria usada.");

            string meta = Controlador + ".meta";
            Assert.IsTrue(File.Exists(meta), "Byakhee_AC.controller sem .meta.");

            string guid = Regex.Match(File.ReadAllText(meta), @"(?m)^guid:\s*([0-9a-f]{32})")
                               .Groups[1].Value;

            Assert.IsTrue(txt.Contains(guid),
                "O Animator do Byakhee não aponta para o Byakhee_AC. Um Animator sem controller " +
                "é tão estático quanto não ter Animator, e não avisa.");
        }

        /// <summary>
        /// Mede <b>código</b>, não prosa: as linhas de comentário saem antes da busca. Um guarda
        /// anterior nesta suíte deu falso positivo justamente por casar com a documentação XML
        /// que <i>explicava</i> o defeito que ele procurava.
        /// </summary>
        [Test]
        public void ByakheeAI_DirigeOAnimator()
        {
            Assert.IsTrue(File.Exists(Ai), $"Script ausente: {Ai}");

            string codigo = string.Join("\n", File.ReadAllLines(Ai)
                .Where(l => !l.TrimStart().StartsWith("//")));

            Assert.IsTrue(codigo.Contains("animator.Play("),
                "ByakheeAI não chama animator.Play — o Animator ficaria parado no estado " +
                "default por toda a luta, independente da fase.");

            Assert.IsTrue(codigo.Contains("OnDanoSofrido"),
                "ByakheeAI não assina OnDanoSofrido — o clipe 'dano' nunca tocaria.");
        }
    }
}
