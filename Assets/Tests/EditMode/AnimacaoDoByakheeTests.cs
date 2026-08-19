using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a animação do Byakhee — que é feita pelo <c>AnimadorDoByakhee</c>, um
    /// <c>MonoBehaviour</c> que lê a <c>ByakheeFSM</c> e troca o sprite, <b>sem</b>
    /// <c>AnimatorController</c>.
    ///
    /// <para><b>O erro que motivou (2026-08-19):</b> auditei o projeto procurando por
    /// <c>AnimationClip</c> e componente <c>Animator</c>, não achei nenhum no Byakhee e concluí
    /// que ele não animava. Ele animava — por código. Montei clipes e um controller por cima, e
    /// os dois sistemas passaram a escrever no mesmo <c>SpriteRenderer</c>. Revertido.</para>
    ///
    /// <para>Por isso o primeiro teste é <b>negativo</b>: ele proíbe o Animator voltar. O XML doc
    /// do <c>AnimadorDoByakhee</c> explica a razão de projeto — um Animator seria uma segunda
    /// máquina de estados a manter em sincronia com a FSM do Core, que é a duplicação de regra
    /// que <c>Assets/Scripts/CLAUDE.md</c> proíbe.</para>
    /// </summary>
    public sealed class AnimacaoDoByakheeTests
    {
        private const string Prefab = "Assets/FavelaAmarela/Art/Enemies/Byakhee.prefab";
        private const string Folha = "Assets/FavelaAmarela/Art/Enemies/Byakhee_Spritesheet.png";
        private const string Animador = "Assets/Scripts/Enemies/AnimadorDoByakhee.cs";

        private static readonly string[] Ciclos =
            { "espreita", "rasante", "garras", "grito", "dano", "derrota" };

        [Test]
        public void Prefab_NaoTemAnimator()
        {
            Assert.IsTrue(File.Exists(Prefab), $"Prefab ausente: {Prefab}");

            Assert.IsFalse(Regex.IsMatch(File.ReadAllText(Prefab), @"(?m)^Animator:"),
                "Voltou um componente Animator ao Byakhee. A animação dele é do " +
                "AnimadorDoByakhee, que lê a ByakheeFSM direto; um Animator por cima faz os " +
                "dois escreverem no mesmo SpriteRenderer e brigarem. Ver o XML doc do " +
                "AnimadorDoByakhee para o porquê de projeto.");
        }

        [Test]
        public void Prefab_TemOAnimadorComTodosOsCiclosPreenchidos()
        {
            string meta = Animador + ".meta";
            Assert.IsTrue(File.Exists(meta), $"Meta ausente: {meta}");

            string guid = Regex.Match(File.ReadAllText(meta), @"(?m)^guid:\s*([0-9a-f]{32})")
                               .Groups[1].Value;

            string txt = File.ReadAllText(Prefab);

            var bloco = Regex.Split(txt, @"(?m)^--- ")
                             .FirstOrDefault(d => d.Contains(guid));

            Assert.IsNotNull(bloco,
                "Byakhee.prefab está sem o AnimadorDoByakhee — o boss ficaria num quadro parado.");

            var vazios = new System.Collections.Generic.List<string>();

            foreach (var ciclo in Ciclos)
            {
                // Conta os elementos da lista serializada daquele campo até o próximo campo.
                var m = Regex.Match(bloco, $@"(?ms)^\s+{ciclo}:\s*(.*?)(?=^\s+\w+:)");
                int n = m.Success ? Regex.Matches(m.Groups[1].Value, "fileID:").Count : -1;

                if (n <= 0) vazios.Add($"{ciclo}: {(n < 0 ? "campo ausente" : "vazio")}");
            }

            Assert.IsEmpty(vazios,
                "Ciclos do AnimadorDoByakhee sem quadros — o componente existe e não anima, que " +
                "é pior que não existir, porque não avisa:\n  " + string.Join("\n  ", vazios));
        }

        /// <summary>
        /// A folha veio fatiada com pivô <c>Center</c>, mas o <c>BoxCollider2D</c> do prefab
        /// (<c>offset 0, 2.19</c>) só faz sentido com pivô no rodapé: com <c>Center</c> o colisor
        /// ficava <b>1,3 unidade acima da arte</b>, e o <c>offsetPes = 0</c> do
        /// <c>DynamicYSort</c> ordenava pelo meio do sprite.
        ///
        /// <para>Confere o <b>pivô</b>, não o enum <c>alignment</c>: pedindo <c>BottomCenter</c>
        /// (7) com pivô explícito, a Unity grava <c>9</c> (<i>Custom</i>) com
        /// <c>pivot {0.5, 0}</c> — a mesma coisa. Testar o enum reprovaria um import correto.</para>
        /// </summary>
        [Test]
        public void Folha_TemPivoNoRodape()
        {
            string meta = Folha + ".meta";
            Assert.IsTrue(File.Exists(meta), $"Meta ausente: {meta}");

            var pivos = Regex.Matches(File.ReadAllText(meta),
                                      @"(?m)^\s+pivot:\s*\{x:\s*([\d.eE+-]+),\s*y:\s*([\d.eE+-]+)\}")
                             .Cast<Match>()
                             .Select(m => m.Groups[2].Value)
                             .ToList();

            Assert.IsNotEmpty(pivos, "Nenhum pivô de fatia encontrado no meta.");

            var fora = pivos.Where(y => y != "0").ToList();

            Assert.IsEmpty(fora,
                $"{fora.Count} de {pivos.Count} fatias do Byakhee não estão com o pivô no rodapé. " +
                "Rode 'Tools/FavelaAmarela/Corrigir pivo do Byakhee'.");
        }
    }
}
