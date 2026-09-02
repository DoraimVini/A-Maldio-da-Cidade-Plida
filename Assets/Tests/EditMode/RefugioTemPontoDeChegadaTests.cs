using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda que <b>todo Refúgio de Luz tem um <c>PontoDeChegada</c> irmão</b>, com
    /// identificador preenchido.
    ///
    /// <para><b>O que motivou (2026-09-02).</b> O Vini mandou o aviso do console:</para>
    ///
    /// <code>
    /// [RefugioDeLuz] 'Refugio_DosPortoes' não tem PontoDeChegada irmão:
    ///                o renascimento vai cair na posição padrão da cena.
    /// </code>
    ///
    /// <para><b>Por que isto é o "menu pós-morte não funciona".</b>
    /// <c>MarcarComoPontoDeRenascimento()</c> grava a <b>cena</b> do refúgio sempre, mas só grava
    /// a <b>posição</b> se achar o componente irmão. Sem ele, "Despertar no último refúgio"
    /// recarrega a cena certa e larga Damião no ponto padrão dela — longe do poste, às vezes do
    /// outro lado do mapa. Para quem está jogando, o botão simplesmente não faz o que promete.
    /// É a mesma causa do "estamos nascendo no meio do mapa" que ele relatou antes.</para>
    ///
    /// <para><b>Estava faltando em 2 dos 6 refúgios</b> — o do Castelo e o do Santuário. Os
    /// outros quatro já tinham, o que é justamente o que torna o defeito difícil de ver jogando:
    /// renascer funciona na maior parte do mapa.</para>
    /// </summary>
    public sealed class RefugioTemPontoDeChegadaTests
    {
        private const string GuidRefugio = "0d15fff5d6e115142aebaaecaadbcf53";
        private const string GuidPonto = "9d6ce1573f2483346b53522bc0b86f10";

        [Test]
        public void TodoRefugioTemPontoDeChegadaIrmao()
        {
            var cenas = Directory.EnumerateFiles("Assets/Scenes", "*.unity",
                                                 SearchOption.AllDirectories).ToArray();

            Assert.IsNotEmpty(cenas, "Nenhuma cena encontrada — este teste não mediu nada.");

            var semPonto = new List<string>();
            int refugios = 0;

            foreach (var cena in cenas)
            {
                string yaml = File.ReadAllText(cena);

                var donosDeRefugio = DonosDoComponente(yaml, GuidRefugio);
                var donosDePonto = DonosDoComponente(yaml, GuidPonto);

                refugios += donosDeRefugio.Count;

                foreach (var dono in donosDeRefugio)
                {
                    if (donosDePonto.Contains(dono)) continue;

                    semPonto.Add($"  {Path.GetFileName(cena)} · " +
                                 $"{NomeDoObjeto(yaml, dono) ?? dono}");
                }
            }

            // REGRA DURA: sem refúgio nenhum, o teste passaria vazio e verde.
            Assert.Greater(refugios, 3,
                $"Só achei {refugios} Refúgio(s) nas cenas. Este teste não está lendo o jogo.");

            Assert.IsEmpty(semPonto,
                "Refúgio(s) sem PontoDeChegada irmão — renascer neles larga Damião na posição " +
                "padrão da cena, longe do poste:" + System.Environment.NewLine +
                string.Join(System.Environment.NewLine, semPonto));
        }

        [Test]
        public void TodoPontoDeChegadaTemIdentificador()
        {
            var vazios = new List<string>();
            int total = 0;

            foreach (var cena in Directory.EnumerateFiles("Assets/Scenes", "*.unity",
                                                          SearchOption.AllDirectories))
            {
                string yaml = File.ReadAllText(cena);

                foreach (var (_, corpo) in Documentos(yaml))
                {
                    if (!corpo.Contains(GuidPonto)) continue;
                    total++;

                    var id = Regex.Match(corpo, @"^  identificador: (.*)$", RegexOptions.Multiline);
                    if (id.Success && !string.IsNullOrWhiteSpace(id.Groups[1].Value)) continue;

                    vazios.Add($"  {Path.GetFileName(cena)}: PontoDeChegada sem identificador");
                }
            }

            Assert.Greater(total, 0, "Nenhum PontoDeChegada nas cenas — nada a medir.");

            Assert.IsEmpty(vazios,
                "PontoDeChegada com identificador vazio — o save grava a chave e o " +
                "renascimento não acha o ponto:" + System.Environment.NewLine +
                string.Join(System.Environment.NewLine, vazios));
        }

        /// <summary>Âncoras dos GameObjects que têm um componente com este script.</summary>
        private static HashSet<string> DonosDoComponente(string yaml, string guid)
        {
            var donos = new HashSet<string>();

            foreach (var (_, corpo) in Documentos(yaml))
            {
                if (!corpo.StartsWith("MonoBehaviour:") || !corpo.Contains(guid)) continue;

                var go = Regex.Match(corpo, @"m_GameObject: \{fileID: (\d+)\}");
                if (go.Success) donos.Add(go.Groups[1].Value);
            }

            return donos;
        }

        private static string NomeDoObjeto(string yaml, string ancora)
        {
            foreach (var (anc, corpo) in Documentos(yaml))
            {
                if (anc != ancora || !corpo.StartsWith("GameObject:")) continue;
                var n = Regex.Match(corpo, @"^  m_Name: (.*)$", RegexOptions.Multiline);
                if (n.Success) return n.Groups[1].Value.Trim();
            }
            return null;
        }

        private static IEnumerable<(string Ancora, string Corpo)> Documentos(string yaml)
        {
            var partes = Regex.Split(yaml, @"^--- !u!\d+ &(\d+)(?: stripped)?\r?\n",
                                     RegexOptions.Multiline);

            for (int i = 1; i + 1 < partes.Length; i += 2)
                yield return (partes[i], partes[i + 1]);
        }
    }
}
