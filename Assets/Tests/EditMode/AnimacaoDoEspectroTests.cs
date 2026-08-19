using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>Guarda a animação do Espectro de Hali — <c>AnimadorDoEspectro</c>.</summary>
    public sealed class AnimacaoDoEspectroTests
    {
        private const string Prefab = "Assets/FavelaAmarela/Art/Enemies/EspectroHali.prefab";
        private const string Folha = "Assets/FavelaAmarela/Art/Enemies/EspectroHali_Spritesheet_24x48.png";
        private const string Componente = "Assets/Scripts/Enemies/AnimadorDoEspectro.cs";
        private const string Ai = "Assets/Scripts/Enemies/EspectroAI.cs";

        private static readonly string[] Ciclos = { "idle", "mover" };

        [Test]
        public void Prefab_TemOAnimadorComOsDoisCiclosPreenchidos()
        {
            Assert.IsTrue(File.Exists(Prefab), $"Prefab ausente: {Prefab}");

            string guid = Regex.Match(File.ReadAllText(Componente + ".meta"),
                                      @"(?m)^guid:\s*([0-9a-f]{32})").Groups[1].Value;

            string txt = File.ReadAllText(Prefab);
            var bloco = Regex.Split(txt, @"(?m)^--- ").FirstOrDefault(d => d.Contains(guid));
            Assert.IsNotNull(bloco, "EspectroHali.prefab está sem o AnimadorDoEspectro.");

            var vazios = new System.Collections.Generic.List<string>();
            foreach (var ciclo in Ciclos)
            {
                var m = Regex.Match(bloco, $@"(?ms)^\s+{ciclo}:\s*(.*?)(?=^\s+\w+:)");
                int n = m.Success ? Regex.Matches(m.Groups[1].Value, "fileID:").Count : -1;
                if (n <= 0) vazios.Add($"{ciclo}: {(n < 0 ? "campo ausente" : "vazio")}");
            }

            Assert.IsEmpty(vazios,
                "Ciclos do AnimadorDoEspectro sem quadros. Rode 'Tools/FavelaAmarela/Montar " +
                "Animação do Espectro'.\n  " + string.Join("\n  ", vazios));
        }

        [Test]
        public void Folha_TemPpu32EPivoNoRodape()
        {
            string meta = Folha + ".meta";
            Assert.IsTrue(File.Exists(meta), $"Meta ausente: {meta}");

            string txt = File.ReadAllText(meta);
            Assert.IsTrue(Regex.IsMatch(txt, @"spritePixelsToUnits:\s*32\b"),
                "PPU da folha do Espectro não é 32.");

            var pivosY = Regex.Matches(txt, @"(?m)^\s+pivot:\s*\{x:\s*[\d.eE+-]+,\s*y:\s*([\d.eE+-]+)\}")
                              .Cast<Match>().Select(m => m.Groups[1].Value).ToList();

            Assert.IsNotEmpty(pivosY, "Nenhum pivô de fatia encontrado.");
            Assert.IsTrue(pivosY.All(y => y == "0"),
                "Alguma fatia da folha do Espectro não está com pivô no rodapé (y=0).");
        }

        /// <summary>
        /// Sem este acesso público, <c>AnimadorDoEspectro</c> não teria como observar
        /// <c>OnStateChanged</c>.
        /// </summary>
        [Test]
        public void EspectroAI_ExpoeAFsmPublicamente()
        {
            Assert.IsTrue(File.Exists(Ai), $"Script ausente: {Ai}");
            Assert.IsTrue(Regex.IsMatch(File.ReadAllText(Ai), @"public\s+EspectroFSM\s+Fsm\s*=>"),
                "EspectroAI não expõe mais 'public EspectroFSM Fsm'.");
        }

        [Test]
        public void Componente_NaoUsaAnimatorController()
        {
            Assert.IsTrue(File.Exists(Componente), $"Script ausente: {Componente}");

            string codigo = string.Join("\n", File.ReadAllLines(Componente)
                .Where(l => !l.TrimStart().StartsWith("///") && !l.TrimStart().StartsWith("//")));

            Assert.IsFalse(codigo.Contains("AnimatorController") || codigo.Contains(": Animator"),
                "AnimadorDoEspectro passou a referenciar Animator/AnimatorController.");
        }
    }
}
