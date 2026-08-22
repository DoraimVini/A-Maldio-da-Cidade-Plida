using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda que <b>todo texto de UI é legível e cabe na própria caixa</b>.
    ///
    /// <para><b>Os dois defeitos que motivaram isto (2026-08-21).</b> O Vini relatou que "até as
    /// caixas de diálogo do menu estão quebradas e sem texto" e que as letras estavam
    /// "muito toscas". Medindo, eram duas coisas distintas:</para>
    ///
    /// <list type="number">
    ///   <item><b>Texto some sem erro.</b> <c>Text.verticalOverflow</c> nasce em
    ///   <c>Truncate</c>: o que não cabe na caixa é <b>escondido</b>, sem exceção, sem log, sem
    ///   nada. O título do menu era 132 pt numa caixa de 129,6 px (a linha precisa de ~152 px) e
    ///   os cinco botões eram 66 pt em 75,6 px. Resultado: menu sem texto nenhum, e nada no
    ///   console.</item>
    ///   <item><b>Migração de fonte pela metade.</b> Quando as fontes triplicaram para a
    ///   referência 1920 × 1080, só parte dos montadores foi atualizada. Sobraram tamanhos 12,
    ///   13, 14, 16, 20 e 22 convivendo com 33, 36, 39, 42 e 60 — no <b>mesmo</b> canvas. É a
    ///   mesma família de falha das listas de cenas que envelhecem: migração parcial que
    ///   ninguém percebe porque nada quebra ruidosamente.</item>
    /// </list>
    ///
    /// <para>Este guarda lê os <b>montadores</b>, não as cenas. As cenas são produto: consertar
    /// a cena sem consertar quem a gera deixa o defeito voltar na próxima execução da
    /// ferramenta.</para>
    /// </summary>
    public sealed class TextoLegivelTests
    {
        private const string PastaDeEditor = "Assets/FavelaAmarela/Editor";

        /// <summary>
        /// Menor tamanho de fonte aceitável na referência 1920 × 1080.
        ///
        /// <para><b>24 px é ~2,2% da altura da tela</b> — o piso usual para texto de interface
        /// em jogo a 1080p. Abaixo disso o jogador tem de se aproximar do monitor, o que num
        /// jogo de horror com leitura de diálogo é atrito puro.</para>
        /// </summary>
        private const int TamanhoMinimo = 24;

        /// <summary>
        /// Montadores que ainda usam tamanhos pequenos <b>de propósito</b>, com justificativa.
        /// Vazio hoje — existe para que uma exceção futura precise ser escrita e defendida, em
        /// vez de aparecer por descuido.
        /// </summary>
        private static readonly Dictionary<string, string> ExcecoesJustificadas =
            new Dictionary<string, string>();

        [Test]
        public void NenhumMontador_CriaTextoIlegivel()
        {
            var falhas = new List<string>();

            foreach (var arquivo in Directory.GetFiles(PastaDeEditor, "*.cs", SearchOption.AllDirectories))
            {
                string nome = Path.GetFileName(arquivo);
                if (ExcecoesJustificadas.ContainsKey(nome)) continue;

                var linhas = File.ReadAllLines(arquivo);

                for (int i = 0; i < linhas.Length; i++)
                {
                    // Ignora comentário: vários montadores citam tamanhos antigos no texto que
                    // explica a correção, e contá-los daria falso positivo.
                    string linha = linhas[i];
                    int comentario = linha.IndexOf("//", System.StringComparison.Ordinal);
                    if (comentario >= 0) linha = linha.Substring(0, comentario);

                    foreach (Match m in Regex.Matches(linha, @"fontSize\s*=\s*(\d+)"))
                        Avaliar(falhas, nome, i + 1, m.Groups[1].Value);

                    // Forma posicional dos helpers: Texto(..., <tamanho>, TextAnchor...)
                    foreach (Match m in Regex.Matches(linha, @",\s*(\d+),\s*TextAnchor"))
                        Avaliar(falhas, nome, i + 1, m.Groups[1].Value);
                }
            }

            Assert.IsEmpty(falhas,
                $"Texto de UI abaixo de {TamanhoMinimo} px na referência 1920x1080:" +
                NovaLinha + "  " + string.Join(NovaLinha + "  ", falhas) +
                NovaLinha + NovaLinha +
                "Se algum for pequeno de propósito, declare em ExcecoesJustificadas com o motivo.");
        }

        private static void Avaliar(List<string> falhas, string arquivo, int linha, string valor)
        {
            if (!int.TryParse(valor, NumberStyles.Integer, CultureInfo.InvariantCulture, out int tamanho))
                return;

            if (tamanho < TamanhoMinimo)
                falhas.Add($"{arquivo}:{linha} usa fonte {tamanho} (mínimo {TamanhoMinimo})");
        }

        /// <summary>
        /// O menu é onde o defeito apareceu primeiro e de forma mais visível — o jogo abria sem
        /// título e com cinco botões vazios. As contas ficam explícitas aqui para que mudá-las
        /// exija refazer a conta, não chutar.
        /// </summary>
        [Test]
        public void OMenu_TemCaixasQueComportamAPropriaFonte()
        {
            string fonte = File.ReadAllText(PastaDeEditor + "/MontarCenaDeMenu.cs");

            // Altura da linha ~= 1,15 x fontSize para a fonte builtin.
            const float FatorDeLinha = 1.15f;
            const float AlturaDaTela = 1080f;

            var falhas = new List<string>();

            // Título: âncoras 0,70..0,87 com fonte 132.
            var titulo = Regex.Match(fonte,
                @"new Vector2\(0\.1f,\s*([\d.]+)f\),\s*new Vector2\(0\.9f,\s*([\d.]+)f\),\s*(\d+),");

            if (!titulo.Success) falhas.Add("não consegui ler a geometria do título");
            else
            {
                float alturaCaixa = (Num(titulo.Groups[2].Value) - Num(titulo.Groups[1].Value)) * AlturaDaTela;
                float alturaLinha = Num(titulo.Groups[3].Value) * FatorDeLinha;

                if (alturaCaixa < alturaLinha)
                    falhas.Add($"título: caixa {alturaCaixa:0} px < linha {alturaLinha:0} px " +
                               "— seria truncado e sumiria da tela");
            }

            // Botão: meia-altura no Vector2(0.30f, alturaCentro - Xf), fonte no Texto(...)
            var meia = Regex.Match(fonte, @"alturaCentro\s*-\s*([\d.]+)f");
            var fonteBotao = Regex.Match(fonte, @"Vector2\.zero,\s*Vector2\.one,\s*(\d+),");

            if (!meia.Success || !fonteBotao.Success) falhas.Add("não consegui ler a geometria do botão");
            else
            {
                float alturaCaixa = Num(meia.Groups[1].Value) * 2f * AlturaDaTela;
                float alturaLinha = Num(fonteBotao.Groups[1].Value) * FatorDeLinha;

                if (alturaCaixa < alturaLinha)
                    falhas.Add($"botão: caixa {alturaCaixa:0} px < linha {alturaLinha:0} px " +
                               "— os rótulos sumiriam");
            }

            Assert.IsEmpty(falhas,
                "Geometria do menu não comporta a própria fonte:" + NovaLinha + "  " +
                string.Join(NovaLinha + "  ", falhas));
        }

        /// <summary>
        /// A rede de segurança: com <c>Truncate</c> (o padrão da Unity) texto que não cabe some
        /// <b>em silêncio</b>. Em <c>Overflow</c> ele vaza e fica feio — e feio se vê e se
        /// conserta, enquanto invisível vai parar na build.
        /// </summary>
        [Test]
        public void OMontadorDoMenu_UsaOverflowEmVezDeTruncate()
        {
            string fonte = File.ReadAllText(PastaDeEditor + "/MontarCenaDeMenu.cs");

            StringAssert.Contains("verticalOverflow = VerticalWrapMode.Overflow", fonte,
                "O montador do menu não protege contra truncamento silencioso. Sem isso, " +
                "qualquer aumento de fonte futuro volta a apagar o texto sem avisar.");
        }

        private static float Num(string s) => float.Parse(s, CultureInfo.InvariantCulture);

        private static readonly string NovaLinha = System.Environment.NewLine;
    }
}
