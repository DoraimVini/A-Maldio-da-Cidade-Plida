using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda que <b>nenhum botão de menu fique escondido debaixo de outro</b>.
    ///
    /// <para><b>O defeito, relatado pelo Vini em 2026-08-31 com um print:</b> <i>"não tem botão
    /// para sair do jogo"</i>. E tinha — <c>Botao_Sair</c> estava na cena, <b>ativo e
    /// clicável</b>, chamando <c>Application.Quit()</c>. Ele só era invisível: o
    /// <c>Botao_Opcoes</c>, criado por ferramenta que <b>clonou</b> o Sair, herdou as âncoras
    /// dele e ficou exatamente por cima. Irmão posterior desenha depois, então o clone cobriu o
    /// original.</para>
    ///
    /// <para><b>Por que só o print pegou.</b> O botão existia no YAML, o campo estava ligado, o
    /// componente respondia ao clique, e nenhum teste anterior falhava — porque todos
    /// verificavam <i>existência</i>. Um botão coberto passa em toda checagem de existência e
    /// falha na única que importa: aparecer.</para>
    ///
    /// <para>O <c>Menu</c> não usa layout automático — a posição de cada botão vem das
    /// <b>âncoras</b>. Duas âncoras idênticas entre irmãos é, portanto, sempre um erro.</para>
    /// </summary>
    public sealed class MenuSemBotaoEscondidoTests
    {
        private const string Cena = "Assets/Scenes/Cena_Menu.unity";

        private readonly struct Retangulo
        {
            public readonly string Nome, Pai;
            public readonly float MinY, MaxY;

            public Retangulo(string nome, string pai, float minY, float maxY)
            {
                Nome = nome; Pai = pai; MinY = minY; MaxY = maxY;
            }
        }

        /// <summary>
        /// Lê os <c>RectTransform</c> dos botões direto do YAML: carregar a cena resolveria
        /// layout e prefab, e o objetivo aqui é justamente ver o que está <b>gravado</b>.
        /// </summary>
        private static List<Retangulo> BotoesDaCena()
        {
            string t = File.ReadAllText(Cena);

            var blocos = Regex.Matches(t, @"--- !u!(\d+) &(\d+)\n(.*?)(?=\n--- !u!|\Z)",
                                       RegexOptions.Singleline);

            var nomes = new Dictionary<string, string>();
            foreach (Match b in blocos)
            {
                if (b.Groups[1].Value != "1") continue;
                var n = Regex.Match(b.Groups[3].Value, @"m_Name:\s*(.*)");
                if (n.Success) nomes[b.Groups[2].Value] = n.Groups[1].Value.Trim();
            }

            var achados = new List<Retangulo>();

            foreach (Match b in blocos)
            {
                if (b.Groups[1].Value != "224") continue;   // RectTransform

                string corpo = b.Groups[3].Value;

                var go = Regex.Match(corpo, @"m_GameObject:\s*\{fileID:\s*(\d+)\}");
                if (!go.Success || !nomes.TryGetValue(go.Groups[1].Value, out string nome)) continue;
                if (!nome.StartsWith("Botao_", StringComparison.Ordinal)) continue;

                var pai = Regex.Match(corpo, @"m_Father:\s*\{fileID:\s*(\d+)\}");
                var min = Regex.Match(corpo, @"m_AnchorMin:\s*\{x:\s*[-\d.]+,\s*y:\s*([-\d.]+)\}");
                var max = Regex.Match(corpo, @"m_AnchorMax:\s*\{x:\s*[-\d.]+,\s*y:\s*([-\d.]+)\}");

                if (!min.Success || !max.Success) continue;

                achados.Add(new Retangulo(
                    nome,
                    pai.Success ? pai.Groups[1].Value : "0",
                    float.Parse(min.Groups[1].Value, CultureInfo.InvariantCulture),
                    float.Parse(max.Groups[1].Value, CultureInfo.InvariantCulture)));
            }

            return achados;
        }

        [Test]
        public void NenhumBotaoIrmao_OcupaAMesmaFaixa()
        {
            var botoes = BotoesDaCena();

            Assert.IsNotEmpty(botoes,
                $"Nenhum botão lido de {Cena} — o formato mudou e este guarda virou decorativo.");

            var sobrepostos = new List<string>();

            foreach (var grupo in botoes.GroupBy(b => b.Pai))
            {
                var lista = grupo.ToList();

                for (int i = 0; i < lista.Count; i++)
                for (int j = i + 1; j < lista.Count; j++)
                {
                    var a = lista[i];
                    var b = lista[j];

                    // Faixas verticais que se cruzam: num menu sem layout automático, dois
                    // botões na mesma altura significam um escondendo o outro.
                    bool cruzam = a.MinY < b.MaxY && b.MinY < a.MaxY;

                    if (cruzam)
                        sobrepostos.Add($"{a.Nome} (y {a.MinY:0.###}–{a.MaxY:0.###}) e " +
                                        $"{b.Nome} (y {b.MinY:0.###}–{b.MaxY:0.###})");
                }
            }

            Assert.IsEmpty(sobrepostos,
                "Botão(ões) sobrepostos no menu — um esconde o outro, e ele passa em toda " +
                "checagem de existência:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", sobrepostos));
        }

        /// <summary>
        /// Fechar o jogo é a única ação que o jogador <b>precisa</b> conseguir fazer sempre. Um
        /// jogo sem saída visível é um jogo que se fecha por Alt+F4 — e isso se lê como defeito.
        /// </summary>
        [Test]
        public void OBotaoDeSair_ExisteEEhOUltimo()
        {
            var botoes = BotoesDaCena();

            var sair = botoes.FirstOrDefault(b => b.Nome.Contains("Sair"));

            Assert.IsNotNull(sair.Nome, "Não há Botao_Sair na cena do menu.");

            var irmaos = botoes.Where(b => b.Pai == sair.Pai).ToList();

            float maisBaixo = irmaos.Min(b => b.MinY);

            Assert.AreEqual(maisBaixo, sair.MinY, 0.001f,
                $"O Sair não é a última opção do menu (está em y {sair.MinY:0.###}, e o mais " +
                $"baixo é {maisBaixo:0.###}). Sair por último é a convenção que o jogador " +
                "espera — e foi ao inserir o Opções que essa ordem se perdeu.");
        }

        /// <summary>
        /// A ferramenta que criou o problema tem de saber não recriá-lo. Clonar um botão copia
        /// as âncoras junto; sem deslocamento, o clone nasce em cima do modelo.
        /// </summary>
        [Test]
        public void AFerramentaDeClone_DeslocaOBotaoNovo()
        {
            string fonte = File.ReadAllText(
                "Assets/FavelaAmarela/Editor/LigarBotaoDeOpcoes.cs");

            StringAssert.Contains("DescerUmDegrau", fonte,
                "A ferramenta voltou a clonar sem deslocar. O clone herda as âncoras do modelo " +
                "e o esconde — foi assim que o botão de sair sumiu da tela.");

            StringAssert.Contains("GetComponentInParent<LayoutGroup>()", fonte,
                "A ferramenta parou de checar se há layout automático. Com LayoutGroup o " +
                "deslocamento é desnecessário e atrapalha; sem ele, é obrigatório.");
        }
    }
}
