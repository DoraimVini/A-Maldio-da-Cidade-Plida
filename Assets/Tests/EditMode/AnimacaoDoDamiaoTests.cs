using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

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

        /// <summary>
        /// Margem em px que o contorno acrescentou embaixo de cada quadro, para a elipse de
        /// sombra caber sem ser cortada. É o que move o pivô para fora do zero.
        /// </summary>
        private const float MargemDaSombra = 2f;

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
                                  .Cast<Match>()
                                  .Select(m => float.Parse(m.Groups[1].Value,
                                                           CultureInfo.InvariantCulture))
                                  .ToList();

                var alturas = Regex.Matches(txt, @"height:\s*([\d.]+)")
                                   .Cast<Match>()
                                   .Select(m => float.Parse(m.Groups[1].Value,
                                                            CultureInfo.InvariantCulture))
                                   .DefaultIfEmpty(0f).Max();

                if (pivosY.Count == 0 || alturas <= 0f)
                {
                    falhas.Add($"{nome}: sem fatias para conferir o pivô");
                    continue;
                }

                // O rodapé deixou de ser y=0: o contorno acrescentou MargemDaSombra px ABAIXO
                // dos pés, para caber a elipse. Um pivô em 0 apoiaria o Damião na borda do
                // quadro e o levantaria 2px do chão em todas as nove tiras.
                float esperado = MargemDaSombra / alturas;

                if (pivosY.Any(y => Mathf.Abs(y - esperado) > 0.0005f))
                    falhas.Add($"{nome}: pivô fora do rodapé " +
                               $"(esperado {esperado:0.000000}, achei " +
                               $"{string.Join(", ", pivosY.Select(y => y.ToString("0.000000")))})");
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
