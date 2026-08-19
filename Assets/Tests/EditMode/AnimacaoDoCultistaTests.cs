using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a animação do Cultista — <c>AnimadorDoCultista</c>, no molde do
    /// <c>AnimadorDoByakhee</c> (lê a FSM do Core, sem <c>AnimatorController</c>).
    /// </summary>
    public sealed class AnimacaoDoCultistaTests
    {
        private const string Prefab = "Assets/FavelaAmarela/Art/Enemies/Cultista.prefab";
        private const string Folha = "Assets/Sprites/Cultistas/Cultista_Spritesheet_16x32.png";
        private const string Componente = "Assets/Scripts/Enemies/AnimadorDoCultista.cs";

        private static readonly string[] Ciclos = { "idle", "walk", "attack", "death" };

        [Test]
        public void Prefab_TemOAnimadorComOsQuatroCiclosPreenchidos()
        {
            Assert.IsTrue(File.Exists(Prefab), $"Prefab ausente: {Prefab}");
            Assert.IsTrue(File.Exists(Componente + ".meta"), $"Meta ausente: {Componente}.meta");

            string guid = Regex.Match(File.ReadAllText(Componente + ".meta"),
                                      @"(?m)^guid:\s*([0-9a-f]{32})").Groups[1].Value;

            string txt = File.ReadAllText(Prefab);
            var bloco = Regex.Split(txt, @"(?m)^--- ").FirstOrDefault(d => d.Contains(guid));

            Assert.IsNotNull(bloco, "Cultista.prefab está sem o AnimadorDoCultista.");

            var vazios = new System.Collections.Generic.List<string>();
            foreach (var ciclo in Ciclos)
            {
                var m = Regex.Match(bloco, $@"(?ms)^\s+{ciclo}:\s*(.*?)(?=^\s+\w+:)");
                int n = m.Success ? Regex.Matches(m.Groups[1].Value, "fileID:").Count : -1;
                if (n <= 0) vazios.Add($"{ciclo}: {(n < 0 ? "campo ausente" : "vazio")}");
            }

            Assert.IsEmpty(vazios,
                "Ciclos do AnimadorDoCultista sem quadros. Rode 'Tools/FavelaAmarela/Montar " +
                "Animação do Cultista'.\n  " + string.Join("\n  ", vazios));
        }

        /// <summary>
        /// PPU 32 e pivô no rodapé em <b>todas</b> as fatias — a folha chegou a PPU 16 com pivô
        /// Center; sem corrigir, o Cultista dobraria de tamanho e flutuaria acima do chão.
        /// </summary>
        [Test]
        public void Folha_TemPpu32EPivoNoRodape()
        {
            string meta = Folha + ".meta";
            Assert.IsTrue(File.Exists(meta), $"Meta ausente: {meta}");

            string txt = File.ReadAllText(meta);

            Assert.IsTrue(Regex.IsMatch(txt, @"spritePixelsToUnits:\s*32\b"),
                "PPU da folha do Cultista não é 32.");

            var pivosY = Regex.Matches(txt, @"(?m)^\s+pivot:\s*\{x:\s*[\d.eE+-]+,\s*y:\s*([\d.eE+-]+)\}")
                              .Cast<Match>().Select(m => m.Groups[1].Value).ToList();

            Assert.IsNotEmpty(pivosY, "Nenhum pivô de fatia encontrado.");
            Assert.IsTrue(pivosY.All(y => y == "0"),
                "Alguma fatia da folha do Cultista não está com pivô no rodapé (y=0).");
        }

        /// <summary>
        /// Mede código, não prosa. Guarda contra a regressão já cometida com o Byakhee: um
        /// <c>AnimatorController</c> por cima de um animador que já lê a FSM do Core duplica a
        /// máquina de estados — proibido por <c>Assets/Scripts/CLAUDE.md</c>.
        /// </summary>
        [Test]
        public void Componente_NaoUsaAnimatorController()
        {
            Assert.IsTrue(File.Exists(Componente), $"Script ausente: {Componente}");

            string codigo = string.Join("\n", File.ReadAllLines(Componente)
                .Where(l => !l.TrimStart().StartsWith("///") && !l.TrimStart().StartsWith("//")));

            Assert.IsFalse(codigo.Contains("AnimatorController") || codigo.Contains(": Animator"),
                "AnimadorDoCultista passou a referenciar Animator/AnimatorController — isso " +
                "duplicaria a EnemyStateMachine numa segunda máquina de estados.");
        }
    }
}
