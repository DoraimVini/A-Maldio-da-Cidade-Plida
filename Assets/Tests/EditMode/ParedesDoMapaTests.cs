using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda o tamanho das <b>paredes de limite do mapa</b>.
    ///
    /// <para><b>Por que este arquivo existe.</b> Em 2026-09-04 uma ferramenta de escala rodou
    /// com um filtro largo demais e esmagou a geometria de nível do Deserto de Hali numa única
    /// execução:</para>
    ///
    /// <code>
    /// Limite_Norte   88 × 1   ->   1 × 1
    /// Limite_Sul     88 × 1   ->   1 × 1
    /// Limite_Leste    1 × 64  ->  64 × 64
    /// Limite_Oeste    1 × 64  ->  64 × 64
    /// </code>
    ///
    /// <para><b>E as duas suítes passaram verdes por cima disso</b> — 1052 testes de EditMode e
    /// 50 de PlayMode, zero falhas. As paredes que impedem o jogador de sair do mapa viraram
    /// quadrados de uma unidade e <b>nada</b> no projeto reclamou. Só a leitura do log da
    /// ferramenta pegou.</para>
    ///
    /// <para><b>O que se guarda aqui é o eixo longo em MUNDO</b> — tamanho da caixa vezes a
    /// escala —, e não a escala exata. Duas razões: uma parede de limite existe para ser
    /// <i>comprida</i>, e fixar o número exato reprovaria qualquer ajuste legítimo de tamanho
    /// de mapa; e as paredes deste projeto ganham comprimento por <b>dois caminhos
    /// diferentes</b> — o Deserto estica o transform (88 × 1 sobre uma caixa de 1 × 1) e o
    /// Santuário deixa a escala em 1 × 1 e dimensiona a caixa (16 × 0,5). A primeira versão
    /// deste teste media só a escala e reprovou o Santuário inteiro sem que nada estivesse
    /// errado.</para>
    /// </summary>
    public sealed class ParedesDoMapaTests
    {
        private const string PastaDeCenas = "Assets/Scenes";

        /// <summary>
        /// Menor comprimento aceitável para o eixo longo de uma parede de limite, em unidades
        /// de mundo.
        ///
        /// <para><b>Calibrado contra a menor parede REAL do projeto</b>, e não por palpite. As
        /// paredes existentes medem 88 e 64 no Deserto (mapa aberto) e 16 e 11 no Santuário
        /// (sala fechada). O colapso que este teste existe para pegar levou as do Deserto a
        /// <b>1</b>.</para>
        ///
        /// <para>8 fica com folga abaixo de 11 — para uma sala menor que o Santuário não
        /// reprovar — e oito vezes acima do colapso. A primeira versão usou 20 e reprovou o
        /// Santuário inteiro: o limiar tinha sido escolhido olhando só o Deserto.</para>
        /// </summary>
        private const float ComprimentoMinimo = 8f;

        [Test]
        public void ParedesDeLimite_ContinuamCompridas()
        {
            var falhas = new List<string>();
            int encontradas = 0;

            foreach (var caminho in Directory.EnumerateFiles(PastaDeCenas, "*.unity"))
            {
                string yaml = File.ReadAllText(caminho);
                string cena = Path.GetFileNameWithoutExtension(caminho);

                // GameObject -> nome, para saber qual Transform é de parede.
                var nomes = new Dictionary<string, string>();
                foreach (var bloco in yaml.Split(new[] { "--- " }, System.StringSplitOptions.None))
                {
                    var id = Regex.Match(bloco, @"^!u!1 &(\d+)");
                    if (!id.Success) continue;

                    var nome = Regex.Match(bloco, @"m_Name: (.+)");
                    if (nome.Success) nomes[id.Groups[1].Value] = nome.Groups[1].Value.Trim();
                }

                // Tamanho do COLISOR por GameObject. As paredes deste projeto ganham
                // comprimento por dois caminhos diferentes: o Deserto estica o transform
                // (escala 88 × 1 sobre uma caixa de 1 × 1) e o Santuário deixa a escala em
                // 1 × 1 e dimensiona a CAIXA (16 × 0,5). Medir só a escala reprovaria o
                // Santuário inteiro sem que nada estivesse errado -- foi o que a primeira
                // versão deste teste fez.
                var caixas = new Dictionary<string, (float X, float Y)>();
                foreach (var bloco in yaml.Split(new[] { "--- " }, System.StringSplitOptions.None))
                {
                    if (!Regex.IsMatch(bloco, @"^!u!61 &\d+")) continue;

                    var dono = Regex.Match(bloco, @"m_GameObject: \{fileID: (\d+)\}");
                    var tam = Regex.Match(bloco, @"m_Size: \{x: ([-\d.eE]+), y: ([-\d.eE]+)");
                    if (!dono.Success || !tam.Success) continue;

                    caixas[dono.Groups[1].Value] = (
                        float.Parse(tam.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture),
                        float.Parse(tam.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture));
                }

                foreach (var bloco in yaml.Split(new[] { "--- " }, System.StringSplitOptions.None))
                {
                    if (!Regex.IsMatch(bloco, @"^!u!4 &\d+")) continue;

                    var go = Regex.Match(bloco, @"m_GameObject: \{fileID: (\d+)\}");
                    if (!go.Success) continue;

                    if (!nomes.TryGetValue(go.Groups[1].Value, out var nome)) continue;
                    if (!nome.StartsWith("Limite") && !nome.StartsWith("Parede")) continue;

                    var e = Regex.Match(bloco,
                        @"m_LocalScale: \{x: ([-\d.eE]+), y: ([-\d.eE]+)");
                    if (!e.Success) continue;

                    float ex = float.Parse(e.Groups[1].Value,
                        System.Globalization.CultureInfo.InvariantCulture);
                    float ey = float.Parse(e.Groups[2].Value,
                        System.Globalization.CultureInfo.InvariantCulture);

                    // Sem caixa, a parede não barra nada -- e aí o problema é outro, coberto
                    // pela auditoria de colisores. Aqui só se mede quem tem colisor.
                    if (!caixas.TryGetValue(go.Groups[1].Value, out var caixa)) continue;

                    encontradas++;

                    float x = caixa.X * System.Math.Abs(ex);
                    float y = caixa.Y * System.Math.Abs(ey);
                    float longo = System.Math.Max(x, y);

                    if (longo >= ComprimentoMinimo) continue;

                    falhas.Add($"{cena} / {nome}: {x:0.##} × {y:0.##} em mundo " +
                               $"(caixa {caixa.X:0.##} × {caixa.Y:0.##} × escala {ex:0.##} × {ey:0.##}) " +
                               $"— o eixo longo tem {longo:0.##}, e o mínimo é {ComprimentoMinimo:0}");
                }
            }

            Assert.Greater(encontradas, 0,
                "Nenhuma parede de limite encontrada nas cenas. Ou elas foram renomeadas — e " +
                "este teste deixou de guardar qualquer coisa — ou foram apagadas, e o jogador " +
                "sai do mapa andando.");

            Assert.IsEmpty(falhas,
                "Parede de limite esmagada:\n  " + string.Join("\n  ", falhas) +
                "\n\nUma parede de limite existe para ser COMPRIDA. Quando ela colapsa, o " +
                "jogador atravessa a borda do mapa e cai no vazio — e isso não aparece em " +
                "nenhum outro teste do projeto.");
        }
    }
}
