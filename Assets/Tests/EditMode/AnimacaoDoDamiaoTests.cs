using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a animação de Damião — <c>AnimadorDoDamiao</c>, com corrida e golpe em 4 direções.
    /// </summary>
    public sealed class AnimacaoDoDamiaoTests
    {
        private const string Prefab = "Assets/FavelaAmarela/Art/Characters/Damiao/Player_Damiao.prefab";
        private const string Pasta = "Assets/FavelaAmarela/Art/Characters/Damiao/Animado";
        private const string Componente = "Assets/Scripts/Player/AnimadorDoDamiao.cs";
        private const string Licenca = Pasta + "/LICENCA_PENDENTE.txt";

        private static readonly string[] Campos =
        {
            "idle", "correrBaixo", "correrCima", "correrEsquerda", "correrDireita",
            "golpeBaixo", "golpeCima", "golpeEsquerda", "golpeDireita",
        };

        private static readonly string[] Tiras =
        {
            "idle", "run_down", "run_up", "run_left", "run_right",
            "slice_down", "slice_up", "slice_left", "slice_right",
        };

        [Test]
        public void AsNoveTiras_ExistemComPpu32EPivoNoRodape()
        {
            var falhas = new System.Collections.Generic.List<string>();

            foreach (var nome in Tiras)
            {
                string png = $"{Pasta}/Damiao_{nome}.png";
                string meta = png + ".meta";

                if (!File.Exists(png)) { falhas.Add($"{nome}: PNG ausente"); continue; }
                if (!File.Exists(meta)) { falhas.Add($"{nome}: sem .meta"); continue; }

                string txt = File.ReadAllText(meta);

                if (!Regex.IsMatch(txt, @"spritePixelsToUnits:\s*32\b"))
                    falhas.Add($"{nome}: PPU != 32");

                var pivosY = Regex.Matches(txt, @"(?m)^\s+pivot:\s*\{x:\s*[\d.eE+-]+,\s*y:\s*([\d.eE+-]+)\}")
                                  .Cast<Match>().Select(m => m.Groups[1].Value).ToList();

                if (pivosY.Count == 0 || pivosY.Any(y => y != "0"))
                    falhas.Add($"{nome}: pivô fora do rodapé");
            }

            Assert.IsEmpty(falhas,
                "Tiras de Damião incompletas. Rode 'Tools/FavelaAmarela/Montar Animação do " +
                "Damião'.\n  " + string.Join("\n  ", falhas));
        }

        [Test]
        public void Prefab_TemOAnimadorComOsNoveCamposPreenchidos()
        {
            Assert.IsTrue(File.Exists(Prefab), $"Prefab ausente: {Prefab}");

            string guid = Regex.Match(File.ReadAllText(Componente + ".meta"),
                                      @"(?m)^guid:\s*([0-9a-f]{32})").Groups[1].Value;

            string txt = File.ReadAllText(Prefab);
            var bloco = Regex.Split(txt, @"(?m)^--- ").FirstOrDefault(d => d.Contains(guid));
            Assert.IsNotNull(bloco, "Player_Damiao.prefab está sem o AnimadorDoDamiao.");

            var vazios = new System.Collections.Generic.List<string>();
            foreach (var campo in Campos)
            {
                var m = Regex.Match(bloco, $@"(?ms)^\s+{campo}:\s*(.*?)(?=^\s+\w+:)");
                int n = m.Success ? Regex.Matches(m.Groups[1].Value, "fileID:").Count : -1;
                if (n <= 0) vazios.Add($"{campo}: {(n < 0 ? "campo ausente" : "vazio")}");
            }

            Assert.IsEmpty(vazios, "Campos do AnimadorDoDamiao sem quadros:\n  " +
                                   string.Join("\n  ", vazios));
        }

        /// <summary>
        /// A escala mudou (arte nova de 84px de altura, contra 48px antes) — o colisor precisa
        /// ter sido recalculado para o volume de mundo continuar o mesmo (0,5 × 0,5), senão a
        /// hitbox de Damião mudou como efeito colateral de uma troca de arte.
        /// </summary>
        [Test]
        public void Colisor_PreservaOVolumeDeMundo()
        {
            string txt = File.ReadAllText(Prefab);
            var docs = Regex.Split(txt, @"(?m)^--- ").Where(d => d.Contains("!u!")).ToList();

            var raiz = docs.FirstOrDefault(d =>
                Regex.IsMatch(d, @"!u!4\b") && Regex.IsMatch(d, @"m_Father:\s*\{fileID:\s*0\}"));
            Assert.IsNotNull(raiz, "Transform raiz não encontrado.");

            var escala = Regex.Match(raiz, @"m_LocalScale:\s*\{x:\s*([\d.eE+-]+),\s*y:\s*([\d.eE+-]+)");
            Assert.IsTrue(escala.Success);
            float sx = float.Parse(escala.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            float sy = float.Parse(escala.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);

            var box = docs.FirstOrDefault(d => Regex.IsMatch(d, @"!u!61\b"));
            Assert.IsNotNull(box, "Sem BoxCollider2D na raiz.");

            var tam = Regex.Match(box, @"m_Size:\s*\{x:\s*([\d.eE+-]+),\s*y:\s*([\d.eE+-]+)");
            float cx = float.Parse(tam.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            float cy = float.Parse(tam.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);

            float mundoX = cx * sx, mundoY = cy * sy;

            Assert.That(mundoX, Is.EqualTo(0.5f).Within(0.01f),
                $"Colisor de Damião no mundo mudou em X: {mundoX:0.###} (esperado 0.5).");
            Assert.That(mundoY, Is.EqualTo(0.5f).Within(0.01f),
                $"Colisor de Damião no mundo mudou em Y: {mundoY:0.###} (esperado 0.5).");
        }

        /// <summary>
        /// As duas cenas com override de escala (não-uniforme, calibrado para o sprite antigo)
        /// precisam ter sido corrigidas para a mesma escala uniforme do prefab — senão a arte
        /// nova aparece esticada só nessas duas cenas.
        /// </summary>
        [Test]
        public void CenasComOverrideDeEscala_UsamEscalaUniforme()
        {
            string txtPrefab = File.ReadAllText(Prefab);
            var raizPrefab = Regex.Split(txtPrefab, @"(?m)^--- ")
                .First(d => Regex.IsMatch(d, @"!u!4\b") && Regex.IsMatch(d, @"m_Father:\s*\{fileID:\s*0\}"));
            var escalaPrefab = Regex.Match(raizPrefab,
                @"m_LocalScale:\s*\{x:\s*([\d.eE+-]+),\s*y:\s*([\d.eE+-]+)");

            var cenas = new[] { "Assets/Scenes/Deserto_Hali.unity",
                                 "Assets/Scenes/Playtest_RuinasPalidas.unity" };
            var falhas = new System.Collections.Generic.List<string>();

            foreach (var caminho in cenas)
            {
                if (!File.Exists(caminho)) { falhas.Add($"{caminho}: ausente"); continue; }

                string txt = File.ReadAllText(caminho);
                var pares = Regex.Matches(txt,
                    @"propertyPath:\s*(m_LocalScale\.[xy])\s*\r?\n\s*value:\s*([^\r\n]*)");

                // A instância do Player_Damiao é a que fica perto de um m_Name: Player_Damiao
                // no mesmo bloco PrefabInstance — aproxima procurando o par mais próximo do nome.
                var blocos = Regex.Split(txt, @"(?m)^--- ")
                    .Where(d => d.Contains("m_Name: Player_Damiao") && d.Contains("PrefabInstance"));

                foreach (var bloco in blocos)
                {
                    var xs = Regex.Match(bloco,
                        @"propertyPath:\s*m_LocalScale\.x\s*\r?\n\s*value:\s*([\d.eE+-]+)");
                    var ys = Regex.Match(bloco,
                        @"propertyPath:\s*m_LocalScale\.y\s*\r?\n\s*value:\s*([\d.eE+-]+)");

                    if (!xs.Success || !ys.Success) continue; // sem override: herda o prefab, ok

                    if (xs.Groups[1].Value != ys.Groups[1].Value)
                        falhas.Add($"{Path.GetFileNameWithoutExtension(caminho)}: escala não-uniforme " +
                                   $"({xs.Groups[1].Value}, {ys.Groups[1].Value})");
                }
            }

            Assert.IsEmpty(falhas,
                "Instância de Damião com escala não-uniforme numa cena — a arte nova ficaria " +
                "esticada:\n  " + string.Join("\n  ", falhas));
        }

        [Test]
        public void AvisoDeLicencaPendente_ContinuaNoRepositorio()
        {
            Assert.IsTrue(File.Exists(Licenca),
                "Sumiu o aviso de licença pendente do pacote '4 directional character'.");
        }

        [Test]
        public void Componente_NaoUsaAnimatorController()
        {
            Assert.IsTrue(File.Exists(Componente), $"Script ausente: {Componente}");

            string codigo = string.Join("\n", File.ReadAllLines(Componente)
                .Where(l => !l.TrimStart().StartsWith("///") && !l.TrimStart().StartsWith("//")));

            Assert.IsFalse(codigo.Contains("AnimatorController") || codigo.Contains(": Animator"),
                "AnimadorDoDamiao passou a referenciar Animator/AnimatorController.");
        }
    }
}
