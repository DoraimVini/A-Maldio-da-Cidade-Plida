using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a <b>fidelidade da documentação de combate aos assets</b>.
    ///
    /// <para><b>Por que isto existe (2026-08-28).</b> A
    /// <c>systems/ficha_de_atributos.md</c> afirmava "Alfanje 60" quando o asset dizia
    /// <b>45</b>, "Estilete 25" quando dizia <b>30</b>, e "Cultista Ataque 24" quando dizia
    /// 14. Não é um problema cosmético: <c>LutaContraByakheeTests</c> <b>copiou o 25 daqui</b>
    /// e passou meses verde defendendo um número que o jogo não usava. Documento errado num
    /// repositório que documenta as decisões vira fonte de erro, não de verdade.</para>
    ///
    /// <para><b>O que este teste NÃO faz.</b> Não valida prosa, nem exige que cada número do
    /// documento apareça num asset. Ele lê as <b>tabelas</b> — que são onde os números moram e
    /// onde a divergência se esconde — e confere linha a linha. Prosa desatualizada continua
    /// possível; tabela mentirosa, não.</para>
    /// </summary>
    public sealed class DocumentacaoFielAosAssetsTests
    {
        private const string Doc = "Docs/KnowledgeBundle/systems/ficha_de_atributos.md";

        /// <summary>Lê as células de uma linha de tabela Markdown, sem os pipes das pontas.</summary>
        private static string[] Celulas(string linha) =>
            linha.Trim().Trim('|').Split('|').Select(x => x.Trim()).ToArray();

        /// <summary>
        /// Converte "45,1", "5 % × 2,0", "**160**" e "40 – 61" no primeiro número que a célula
        /// carrega. A documentação usa vírgula decimal e negrito de propósito — quem lê é
        /// humano.
        /// </summary>
        private static float? PrimeiroNumero(string celula)
        {
            var m = Regex.Match(celula.Replace(",", "."), @"-?\d+(\.\d+)?");

            return m.Success
                ? float.Parse(m.Value, CultureInfo.InvariantCulture)
                : (float?)null;
        }

        private static IEnumerable<string[]> LinhasDaTabelaApos(string doc, string cabecalho)
        {
            var linhas = doc.Split('\n');
            int i = Array.FindIndex(linhas, l => l.Contains(cabecalho));

            Assert.Greater(i, -1, $"A tabela com o cabeçalho '{cabecalho}' sumiu de {Doc}.");

            // Pula o cabeçalho e a linha de separação (|---|---|).
            for (int j = i + 2; j < linhas.Length; j++)
            {
                string linha = linhas[j].Trim();
                if (!linha.StartsWith("|")) yield break;

                yield return Celulas(linha);
            }
        }

        // ── A tabela de fichas ────────────────────────────────────────────────

        /// <summary>
        /// Cada linha de "Escala e balanceamento atual" tem de bater com o `.asset` que ela
        /// nomeia — Vitalidade, Ataque e Defesa.
        /// </summary>
        [Test]
        public void ATabelaDeFichas_BateComOsAssets()
        {
            Assert.IsTrue(File.Exists(Doc), $"{Doc} não existe.");

            var divergencias = new List<string>();
            int conferidas = 0;

            foreach (var celulas in LinhasDaTabelaApos(File.ReadAllText(Doc),
                                                       "| Ficha | Vitalidade | Ataque | Defesa"))
            {
                var nome = Regex.Match(celulas[0], @"Ficha_(\w+)");
                if (!nome.Success) continue;

                var ficha = AssetDatabase.LoadAssetAtPath<FichaAtributosConfig>(
                    $"Assets/FavelaAmarela/Config/Ficha_{nome.Groups[1].Value}.asset");

                if (ficha == null)
                {
                    divergencias.Add($"{celulas[0]}: o documento cita uma ficha que não existe");
                    continue;
                }

                conferidas++;

                Conferir(divergencias, ficha.name, "Vitalidade", PrimeiroNumero(celulas[1]),
                         ficha.VitalidadeMax);
                Conferir(divergencias, ficha.name, "Ataque", PrimeiroNumero(celulas[2]),
                         ficha.Ataque);
                Conferir(divergencias, ficha.name, "Defesa", PrimeiroNumero(celulas[3]),
                         ficha.Defesa);
            }

            Assert.Greater(conferidas, 0, "Nenhuma linha de ficha foi conferida — o formato da " +
                                          "tabela mudou e este teste virou decorativo.");

            Assert.IsEmpty(divergencias, Mensagem(divergencias));
        }

        // ── A tabela de armas ─────────────────────────────────────────────────

        /// <summary>
        /// A tabela das 3 armas da Tumba — faixa, precisão e valor esperado. É a que continha o
        /// "Alfanje 60" e o "Estilete 25", os dois números que enganaram um teste.
        /// </summary>
        [Test]
        public void ATabelaDasArmas_BateComOsAssets()
        {
            var divergencias = new List<string>();
            int conferidas = 0;

            // O documento nomeia as armas pelo nome diegético; o asset, pela família.
            var familias = new Dictionary<string, string>
            {
                ["Alfanje"] = "Alfanje",
                ["Cravo"] = "Cravo",
                ["Estilete"] = "LaminaFina",
            };

            foreach (var celulas in LinhasDaTabelaApos(File.ReadAllText(Doc),
                                                       "| Arma | Faixa (nv 1) | Crítico"))
            {
                var chave = familias.Keys.FirstOrDefault(k => celulas[0].Contains(k));
                if (chave == null) continue;

                var familia = AssetDatabase.LoadAssetAtPath<BaseDeArma>(
                    $"Assets/FavelaAmarela/Config/Armas/BaseArma_{familias[chave]}.asset");

                if (familia == null)
                {
                    divergencias.Add($"{celulas[0]}: BaseArma_{familias[chave]}.asset não existe");
                    continue;
                }

                conferidas++;

                // "40 – 61" → dois números.
                var faixa = Regex.Matches(celulas[1].Replace(",", "."), @"\d+(\.\d+)?")
                    .Cast<Match>()
                    .Select(m => float.Parse(m.Value, CultureInfo.InvariantCulture))
                    .ToArray();

                if (faixa.Length == 2)
                {
                    Conferir(divergencias, chave, "dano mínimo", faixa[0], familia.DanoMinBase);
                    Conferir(divergencias, chave, "dano máximo", faixa[1], familia.DanoMaxBase);
                }
                else
                {
                    divergencias.Add($"{chave}: a célula de faixa '{celulas[1]}' não tem dois " +
                                     "números");
                }

                // "85 %" → fração.
                var precisao = PrimeiroNumero(celulas[3]);
                if (precisao.HasValue)
                    Conferir(divergencias, chave, "precisão", precisao.Value / 100f,
                             familia.PrecisaoBase, tolerancia: 0.005f);

                // O valor esperado é derivado — se ele bater, a linha inteira é coerente.
                var esperadoDoc = PrimeiroNumero(celulas[4]);
                if (esperadoDoc.HasValue)
                    Conferir(divergencias, chave, "valor esperado", esperadoDoc.Value,
                             EsperadoDe(familia), tolerancia: 0.15f);
            }

            Assert.AreEqual(3, conferidas,
                "A tabela das armas da Tumba deixou de descrever as três armas.");

            Assert.IsEmpty(divergencias, Mensagem(divergencias));
        }

        /// <summary>Valor esperado do básico: média da faixa, corrigida por precisão e crítico.</summary>
        private static float EsperadoDe(BaseDeArma familia)
        {
            var p = familia.PerfilNoNivel(1);

            float percentual = familia.Habilidade == null ? 1f
                : familia.Habilidade.EfeitosDoBasico
                    .Where(e => e.Tipo == TipoDeEfeito.DanoDaArma)
                    .Select(e => e.Valor)
                    .DefaultIfEmpty(1f)
                    .Sum();

            float media = (p.DanoMin + p.DanoMax) * 0.5f * percentual;
            return media * p.Precisao * (1f + p.ChanceCritica * (p.MultiplicadorCritico - 1f));
        }

        private static void Conferir(List<string> destino, string quem, string campo,
                                     float? documentado, float noAsset, float tolerancia = 0.01f)
        {
            if (!documentado.HasValue) return;

            if (Math.Abs(documentado.Value - noAsset) > tolerancia)
                destino.Add($"{quem} / {campo}: o documento diz {documentado.Value:0.##} e o " +
                            $"asset diz {noAsset:0.##}");
        }

        private static string Mensagem(List<string> divergencias) =>
            "A documentação de combate diverge dos assets:" + Environment.NewLine + "  " +
            string.Join(Environment.NewLine + "  ", divergencias) + Environment.NewLine +
            $"Corrija {Doc} — ou o asset, se o documento é que estiver certo. O que não pode é " +
            "ficarem os dois, porque foi assim que um teste copiou 'Estilete 25' daqui e passou " +
            "meses defendendo um número que o jogo não usava.";
    }
}
