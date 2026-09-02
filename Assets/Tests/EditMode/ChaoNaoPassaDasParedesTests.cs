using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda que o chão do Deserto <b>não se estende muito além das paredes</b>.
    ///
    /// <para><b>O que motivou (2026-09-02).</b> O Vini relatou que, depois de o mapa dobrar, "a
    /// borda continua no mesmo tamanho de antes". Medido, as paredes estavam <b>certas</b>
    /// (±43 × ±31, dobradas junto) — quem passou do ponto foi o chão:</para>
    ///
    /// <code>
    /// chão pintado :  x -64..64    y -35..35
    /// paredes      :  x -43..43    y -31..31
    /// </code>
    ///
    /// <para><b>21 unidades de chão visível e inalcançável</b> a leste e a oeste. O jogador via
    /// um deserto de 128 de largura, andava em 86, e era parado por uma parede <b>invisível</b>
    /// muito antes da borda que os olhos enxergam.</para>
    ///
    /// <para><b>É o par do <c>ConferirCoberturaDoChao</c>.</b> Aquele pergunta "falta chão onde
    /// o jogador pisa?"; este pergunta "sobra chão onde ele não pode ir?". As duas perguntas
    /// nascem do mesmo repintor, que varre o mundo e pinta a célula de cada amostra — ele
    /// garante cobertura por construção e <b>não</b> apaga o que ficou fora.</para>
    /// </summary>
    public sealed class ChaoNaoPassaDasParedesTests
    {
        private const string Cena = "Assets/Scenes/Deserto_Hali.unity";

        /// <summary>
        /// Quanto de chão pode sobrar para fora da parede.
        ///
        /// <para>Não é zero: chão terminando exatamente na parede invisível mostraria o vazio no
        /// instante em que o jogador encosta nela. <b>6</b> é o dobro da margem que a ferramenta
        /// de aparo usa (3), para uma diferença de arredondamento na conversão isométrica não
        /// derrubar a suíte.</para>
        /// </summary>
        private const float SobraTolerada = 6f;

        /// <summary>
        /// Célula do grid isométrico do Deserto, conferida no YAML da cena
        /// (<c>m_CellSize: {x: 1, y: 0.5}</c>).
        /// </summary>
        private const float CelulaX = 1f;
        private const float CelulaY = 0.5f;

        [Test]
        public void OChaoNaoSeEstendeAlemDasParedes()
        {
            Assert.IsTrue(File.Exists(Cena), $"Cena ausente: {Cena}");
            string yaml = File.ReadAllText(Cena);

            // As paredes. Posição basta: elas são simétricas em torno da origem.
            var paredes = Regex.Matches(yaml,
                    @"m_Name: Limite_\w+\s*\n(?:(?!m_Name:).)*?",
                    RegexOptions.Singleline);

            var limites = LimitesDaCena(yaml);

            Assert.Greater(limites.X, 0f, "Não achei os Limite_* da cena — nada a comparar.");

            // As células pintadas, convertidas para MUNDO. Num grid isométrico com swizzle XYZ:
            //   mundoX = (cx - cy) * celulaX / 2
            //   mundoY = (cx + cy) * celulaY / 2
            var celulas = Regex.Matches(yaml, @"- first: \{x: (-?\d+), y: (-?\d+)")
                .Cast<Match>()
                .Select(m => (X: int.Parse(m.Groups[1].Value), Y: int.Parse(m.Groups[2].Value)))
                .ToArray();

            Assert.IsNotEmpty(celulas, "Nenhuma célula pintada — o chão sumiu.");

            float maxMundoX = celulas.Max(c => Math.Abs((c.X - c.Y) * CelulaX / 2f));
            float maxMundoY = celulas.Max(c => Math.Abs((c.X + c.Y) * CelulaY / 2f));

            float sobraX = maxMundoX - limites.X;
            float sobraY = maxMundoY - limites.Y;

            Assert.LessOrEqual(sobraX, SobraTolerada,
                $"O chão passa {sobraX:0} unidades da parede no eixo X (chão até " +
                $"{maxMundoX:0}, parede em {limites.X:0})." + Environment.NewLine +
                "O jogador vê um deserto maior do que pode andar, e é parado por uma parede " +
                "INVISÍVEL antes da borda que os olhos enxergam." + Environment.NewLine +
                "Conserto: 'Tools/FavelaAmarela/Deserto: aparar o chão que passa das paredes'.");

            Assert.LessOrEqual(sobraY, SobraTolerada,
                $"O chão passa {sobraY:0} unidades da parede no eixo Y (chão até " +
                $"{maxMundoY:0}, parede em {limites.Y:0}).");
        }

        /// <summary>Até onde vão as paredes, em módulo.</summary>
        private static (float X, float Y) LimitesDaCena(string yaml)
        {
            var docs = Regex.Split(yaml, @"^--- !u!\d+ &(\d+)\r?\n", RegexOptions.Multiline);
            var pares = new System.Collections.Generic.List<(string Ancora, string Corpo)>();
            for (int i = 1; i + 1 < docs.Length; i += 2) pares.Add((docs[i], docs[i + 1]));

            var deObjeto = pares
                .Where(p => Regex.IsMatch(p.Corpo, @"^  m_Name: Limite_\w+\s*$",
                                          RegexOptions.Multiline))
                .Select(p => p.Ancora)
                .ToHashSet();

            float mx = 0f, my = 0f;

            foreach (var (_, corpo) in pares)
            {
                if (!corpo.StartsWith("Transform:")) continue;

                var go = Regex.Match(corpo, @"m_GameObject: \{fileID: (\d+)\}");
                if (!go.Success || !deObjeto.Contains(go.Groups[1].Value)) continue;

                var p = Regex.Match(corpo,
                    @"m_LocalPosition: \{x: (-?[\d.eE+-]+), y: (-?[\d.eE+-]+)");

                if (!p.Success) continue;

                mx = Math.Max(mx, Math.Abs(float.Parse(p.Groups[1].Value,
                    System.Globalization.CultureInfo.InvariantCulture)));
                my = Math.Max(my, Math.Abs(float.Parse(p.Groups[2].Value,
                    System.Globalization.CultureInfo.InvariantCulture)));
            }

            return (mx, my);
        }
    }
}
