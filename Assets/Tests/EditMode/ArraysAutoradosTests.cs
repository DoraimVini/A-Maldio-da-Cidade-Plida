using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda uma forma de defeito específica e muito cara: <b>o array dimensionado e vazio</b>.
    ///
    /// <para><b>O caso que originou este arquivo (playtest de 2026-08-28).</b> O Vini reportou
    /// que "a luta do Abdul continua quebrada, as Pedras de Poder não aparecem". A causa estava
    /// num override de instância de prefab na cena da Tumba:</para>
    ///
    /// <code>
    ///   propertyPath: pontosDasPedras.Array.size
    ///   value: 1
    ///   propertyPath: 'pontosDasPedras.Array.data[0]'
    ///   objectReference: {fileID: 0}      ← vazio
    /// </code>
    ///
    /// <para>Alguém dimensionou o array no Inspector e nunca arrastou o <c>Transform</c>. O
    /// código perguntava <c>Length &gt; 0</c> para decidir entre "usar os pontos autorados" e
    /// "usar o anel de fallback"; com tamanho 1, escolhia o ramo autorado, filtrava o nulo e
    /// ficava com <b>zero</b> posições. Nenhuma Pedra nascia — <b>sem um log sequer</b>, porque
    /// a guarda que existia era sobre o <i>prefab</i> da Pedra, que estava atribuído.</para>
    ///
    /// <para><b>E a consequência não parava aí.</b> Sem Pedras, <c>DefinirTotalDePedras(0)</c>
    /// deixava <c>EscudoDestruido</c> (que exige <c>TotalDePedras &gt; 0</c>) falso para sempre,
    /// e nada podia chamar <c>QuebrarPedraDePoder</c>. O escudo do chefe <b>nunca caía</b>: a
    /// luta era matematicamente invencível. Um array com um slot vazio tornou o clímax da
    /// dungeon impossível.</para>
    ///
    /// <para><b>Por que só os overrides de cena.</b> Um array vazio no prefab você vê abrindo o
    /// prefab. Um array vazio num <b>override de instância</b> só aparece expandindo os
    /// overrides daquele objeto naquela cena — é invisível no fluxo normal de trabalho, e é
    /// exatamente onde este defeito se escondeu.</para>
    /// </summary>
    public sealed class ArraysAutoradosTests
    {
        [Test]
        public void NenhumArrayDeCena_TemTamanhoComTodosOsSlotsVazios()
        {
            var suspeitos = new List<string>();

            foreach (var caminho in Directory.GetFiles("Assets/Scenes", "*.unity",
                                                       SearchOption.AllDirectories).OrderBy(c => c))
            {
                foreach (var (campo, tamanho, vazios) in ArraysSobrescritos(File.ReadAllText(caminho)))
                {
                    if (tamanho <= 0 || vazios < tamanho) continue;

                    suspeitos.Add($"{Path.GetFileName(caminho)} · '{campo}': tamanho {tamanho}, " +
                                  $"{vazios} slot(s) e TODOS vazios");
                }
            }

            Assert.IsEmpty(suspeitos,
                "Array(s) dimensionado(s) e sem nenhum conteúdo:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", suspeitos) + Environment.NewLine +
                "Isto nunca é intencional: ou alguém dimensionou o array e esqueceu de " +
                "preencher, ou o objeto referenciado foi apagado. E é PIOR que deixar em zero, " +
                "porque o código costuma perguntar 'Length > 0' para decidir se há configuração " +
                "— então ele entra no ramo autorado e trabalha com nada. Foi assim que a luta " +
                "do Abdul ficou invencível: 'pontosDasPedras' com um slot vazio fez zero Pedras " +
                "nascerem, e sem Pedras o escudo dele nunca cai.");
        }

        // ── Leitura do YAML ───────────────────────────────────────────────────

        /// <summary>
        /// Cada array sobrescrito num bloco <c>m_Modifications</c>: o nome do campo, o tamanho
        /// declarado, e quantos slots estão vazios.
        ///
        /// <para>"Vazio" = <c>value:</c> em branco <b>e</b> <c>objectReference: {fileID: 0}</c>.
        /// Um array de números tem <c>value: 3.5</c> e passa longe; um array de referências
        /// preenchido tem <c>objectReference</c> não-zero.</para>
        /// </summary>
        private static IEnumerable<(string Campo, int Tamanho, int Vazios)> ArraysSobrescritos(
            string yaml)
        {
            var tamanhos = new Dictionary<string, int>();
            var slots = new Dictionary<string, int>();
            var vazios = new Dictionary<string, int>();

            foreach (Match m in Regex.Matches(yaml,
                         @"propertyPath:\s*'?(?<campo>[\w.]+)\.Array\.size'?\s*\r?\n\s*value:\s*(?<n>\d+)"))
            {
                string campo = m.Groups["campo"].Value;
                int n = int.Parse(m.Groups["n"].Value);

                // O mesmo campo pode aparecer em mais de uma instância na mesma cena; ficamos
                // com o maior, que é o pior caso a reportar.
                if (!tamanhos.TryGetValue(campo, out int atual) || n > atual) tamanhos[campo] = n;
            }

            foreach (Match m in Regex.Matches(yaml,
                         @"propertyPath:\s*'?(?<campo>[\w.]+)\.Array\.data\[\d+\]'?\s*\r?\n" +
                         @"\s*value:(?<valor>[^\r\n]*)\r?\n\s*objectReference:\s*\{fileID:\s*(?<ref>-?\d+)\}"))
            {
                string campo = m.Groups["campo"].Value;
                slots[campo] = slots.TryGetValue(campo, out int s) ? s + 1 : 1;

                bool semValor = string.IsNullOrWhiteSpace(m.Groups["valor"].Value);
                bool semReferencia = m.Groups["ref"].Value == "0";

                if (semValor && semReferencia)
                    vazios[campo] = vazios.TryGetValue(campo, out int v) ? v + 1 : 1;
            }

            foreach (var (campo, tamanho) in tamanhos.Select(kv => (kv.Key, kv.Value)))
            {
                // Sem nenhum slot sobrescrito, o conteúdo vem do prefab — nada a afirmar aqui.
                if (!slots.ContainsKey(campo)) continue;

                yield return (campo, Math.Min(tamanho, slots[campo]),
                              vazios.TryGetValue(campo, out int v) ? v : 0);
            }
        }
    }
}
