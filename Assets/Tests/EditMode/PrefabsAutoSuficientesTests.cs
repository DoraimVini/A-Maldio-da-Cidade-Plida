using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda que um prefab de ator seja <b>auto-suficiente</b>: tudo que ele instancia está
    /// ligado <b>nele</b>, e não só na instância de uma cena.
    ///
    /// <para><b>O que a auditoria de 2026-08-29 encontrou.</b> O prefab do Abdul tinha
    /// <c>prefabEsqueleto</c>, <c>prefabConeDeGelo</c>, <c>prefabPedraDePoder</c> e
    /// <c>prefabNecronomicon</c> <b>todos nulos</b>. A luta funciona porque a instância em
    /// <c>Playtest_RuinasPalidas</c> sobrescreve os quatro — então não era bug ativo, era um
    /// prefab que só funciona num lugar.</para>
    ///
    /// <para><b>Por que isso importa mais do que parece.</b> Pôr o Abdul em outra cena — ou
    /// perder o <i>override</i> num "Revert Prefab Instance" — apagaria, em silêncio: a
    /// invocação de esqueletos, o Cone de Gelo, <b>e as Pedras de Poder</b>. Sem pedras,
    /// <c>TotalDePedras = 0</c> e <c>EscudoDestruido</c> nunca vira verdadeiro: <b>o chefe fica
    /// invencível</b>. Foi exatamente esse desfecho que o Vini encontrou em playtest por outra
    /// causa (um array dimensionado com elemento nulo), e o conserto de lá não protege
    /// deste.</para>
    ///
    /// <para><b>Por que ler o YAML e não a AssetDatabase.</b> Carregar o prefab resolveria o
    /// <i>override</i> junto em alguns caminhos, e o teste passaria justamente no caso que ele
    /// existe para pegar. O arquivo no disco é a única fonte que diz o que o <b>prefab</b>
    /// carrega.</para>
    /// </summary>
    public sealed class PrefabsAutoSuficientesTests
    {
        /// <summary>
        /// Campos de prefab que cada ator precisa carregar por conta própria, com o que se
        /// perde quando faltam. Lista escrita à mão: <b>o que é obrigatório é decisão de
        /// design</b> — nem todo campo de prefab é (ver <c>prefabColetavel</c> abaixo).
        /// </summary>
        private static readonly (string Prefab, string Campo, string OQueSePerde)[] Obrigatorios =
        {
            ("Abdul_Alhazred", "prefabPedraDePoder",
             "sem pedras não há como derrubar o escudo — o chefe fica INVENCÍVEL"),

            ("Abdul_Alhazred", "prefabEsqueleto",
             "a invocação de esqueletos, que é metade da pressão da fase 1 da luta"),

            ("Abdul_Alhazred", "prefabConeDeGelo",
             "o Cone de Gelo, o único ataque à distância dele"),

            ("Abdul_Alhazred", "prefabNecronomicon",
             "o tomo que a luta existe para entregar — derrotá-lo não daria recompensa"),
        };

        /// <summary>
        /// Campos de prefab que <b>podem</b> ficar vazios, e por quê. Estar aqui é decisão
        /// registrada; estar fora das duas listas é esquecimento.
        /// </summary>
        private static readonly (string Campo, string Porque)[] LegitimamenteVazios =
        {
            ("prefabColetavel",
             "documentado como opcional: vazio faz o DropAoAbater montar um coletável mínimo " +
             "em runtime"),
        };

        private const string Elenco = "Assets/FavelaAmarela/Art/Enemies";

        [Test]
        public void TodoPrefabDeAtor_CarregaOQueEleInstancia()
        {
            var soltos = new List<string>();

            foreach (var (prefab, campo, perda) in Obrigatorios)
            {
                string caminho = $"{Elenco}/{prefab}.prefab";

                if (!File.Exists(caminho))
                {
                    soltos.Add($"{prefab}: prefab ausente");
                    continue;
                }

                var m = Regex.Match(File.ReadAllText(caminho),
                                    $@"^\s*{campo}:\s*\{{fileID:\s*(-?\d+)", RegexOptions.Multiline);

                if (!m.Success)
                {
                    soltos.Add($"{prefab}.{campo}: campo não existe mais no componente");
                    continue;
                }

                if (m.Groups[1].Value == "0")
                    soltos.Add($"{prefab}.{campo} está NULO no prefab — {perda}");
            }

            Assert.IsEmpty(soltos,
                "Prefab(s) que dependem de override de cena para funcionar:" +
                Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", soltos) + Environment.NewLine +
                "Uma instância de cena pode sobrescrever e esconder isto — e some num " +
                "'Revert Prefab Instance'.");
        }

        /// <summary>
        /// Campo de prefab novo tem de entrar numa das duas listas. Sem isto, o próximo ator
        /// entra com uma conjuração desligada e ninguém percebe até o playtest — que é a
        /// história inteira deste repositório.
        /// </summary>
        [Test]
        public void NenhumCampoDePrefab_FicaForaDasDuasListas()
        {
            var desconhecidos = new List<string>();

            var conhecidos = Obrigatorios.Select(o => o.Campo)
                .Concat(LegitimamenteVazios.Select(v => v.Campo))
                .ToHashSet();

            foreach (var caminho in Directory.GetFiles(Elenco, "*.prefab").OrderBy(c => c))
            {
                string nome = Path.GetFileNameWithoutExtension(caminho);

                foreach (Match m in Regex.Matches(File.ReadAllText(caminho),
                             @"^\s*(prefab[A-Z]\w*):\s*\{fileID:\s*0\}", RegexOptions.Multiline))
                {
                    string campo = m.Groups[1].Value;
                    if (conhecidos.Contains(campo)) continue;

                    desconhecidos.Add($"{nome}.{campo} está nulo e não está em lista nenhuma");
                }
            }

            Assert.IsEmpty(desconhecidos,
                "Campo(s) de prefab nulos sem decisão registrada:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", desconhecidos) + Environment.NewLine +
                "Acrescente a PrefabsAutoSuficientesTests.Obrigatorios (com o que se perde) ou " +
                "a LegitimamenteVazios (com o porquê).");
        }
    }
}
