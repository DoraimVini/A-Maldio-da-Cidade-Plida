using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using FavelaAmarela.Core.Progression;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a <b>economia de Exposição</b> — quanto vale cada abate, e onde isso põe o jogador
    /// quando ele encara cada chefe.
    ///
    /// <para><b>O pedido do Vini (2026-08-28):</b> <i>"Temos que usar uma escala que cresça com
    /// o jogo e com o personagem, saber que ele no nível 2 está mais forte e com mais defesa.
    /// Logo, toda lógica de combate tem que ser escalonável, junto do jogo."</i></para>
    ///
    /// <para><b>Por que este arquivo existe.</b> Toda a Fase 2 — dano branco por nível do item,
    /// ficha por nível da unidade, curva de raridade por nível do jogador — pendura no nível do
    /// jogador. E o nível <b>não subia</b>: a curva pede 100 para o nível 2, cada abate concedia
    /// <b>1</b>, e o caminho crítico inteiro tem <b>treze</b> abates. O eixo existia em código,
    /// compilava, tinha teste — e era invisível em jogo. É o modo de falha que este repositório
    /// coleciona, aplicado ao sistema que a sessão inteira construiu.</para>
    /// </summary>
    public sealed class EconomiaDeExposicaoTests
    {
        private const string Enemies = "Assets/FavelaAmarela/Art/Enemies";
        private const string Cenas = "Assets/Scenes";

        /// <summary>A curva autorada no <c>ProgressionBridge</c>. Espelhada aqui de propósito:
        /// se alguém a mexer, este arquivo tem de ser relido junto — a conta do caminho crítico
        /// depende dela.</summary>
        private static readonly int[] Curva =
        {
            0, 100, 300, 600, 1000, 1500, 2100, 2800, 3600, 4500, 5500, 6600
        };

        // ── Ninguém que larga espólio pode conceder zero ───────────────────────

        /// <summary>
        /// <b>Largar espólio e não dar nível é meio inimigo.</b> Este é o guarda que pega o
        /// buraco real que a calibração encontrou: a concessão morava dentro do
        /// <c>EnemyBase</c>, e o elenco tem nove prefabs com <b>dois</b> <c>EnemyBase</c>. O
        /// Abdul — primeiro chefe do jogo — concedia zero, e ninguém tinha como notar.
        /// </summary>
        [Test]
        public void TodoAtorQueLargaEspolio_ConcedeExposicao()
        {
            var mudos = new List<string>();

            foreach (var caminho in Directory.GetFiles(Enemies, "*.prefab").OrderBy(c => c))
            {
                string yaml = File.ReadAllText(caminho);
                string nome = Path.GetFileNameWithoutExtension(caminho);

                // O nome completo do componente vem no m_EditorClassIdentifier do YAML.
                if (!yaml.Contains("Itens.DropAoAbater"))
                    continue;   // não larga espólio: fora do contrato

                int porEnemyBase = LerInteiro(yaml, "exposicaoAoAbater");
                int porComponente = LerInteiro(yaml, "exposicao");

                if (Math.Max(porEnemyBase, porComponente) <= 0)
                    mudos.Add($"{nome}: larga espólio e concede ZERO de Exposição");
            }

            Assert.IsEmpty(mudos,
                "Ator(es) que recompensam com item mas não com nível:" + Environment.NewLine +
                "  " + string.Join(Environment.NewLine + "  ", mudos) + Environment.NewLine +
                "Conserto: 'Tools/FavelaAmarela/Progressão: calibrar a Exposição do elenco'.");
        }

        /// <summary>
        /// O <b>1</b> é o valor de fábrica do <c>EnemyBase</c>, e foi o que o elenco inteiro
        /// carregou até hoje. Com a curva pedindo 100 para o nível 2, ele significa "cem abates
        /// por nível" — ou seja, nível travado. Vê-lo de novo é sinal de campo esquecido, não de
        /// decisão.
        /// </summary>
        [Test]
        public void NenhumInimigo_FicouComOValorDeFabrica()
        {
            var padroes = Directory.GetFiles(Enemies, "*.prefab")
                .Where(c => LerInteiro(File.ReadAllText(c), "exposicaoAoAbater") == 1)
                .Select(Path.GetFileNameWithoutExtension)
                .ToList();

            Assert.IsEmpty(padroes,
                "Inimigo(s) ainda com exposicaoAoAbater = 1 (o valor de fábrica):" +
                Environment.NewLine + "  " + string.Join(", ", padroes) + Environment.NewLine +
                "Cem abates por nível é o mesmo que nível travado.");
        }

        // ── A conta do caminho crítico ────────────────────────────────────────

        /// <summary>
        /// <b>A pergunta que importa:</b> em que nível o jogador chega ao Byakhee jogando o
        /// caminho crítico e matando o que aparece? Este teste conta os inimigos <b>nas cenas</b>
        /// — não estima — e roda a soma pela <c>Progressao</c> real.
        ///
        /// <para>O alvo é <b>nível 3</b>. É onde a luta deixa de ser 14 golpes contra 5: um
        /// Alfanje de nível 3 sai de ~45 para ~67,7 esperados, e a ficha do Damião sai de
        /// 100/6 para 160/7,8 de Vitalidade e Defesa. A troca vira 9 por 9 — vencível com as
        /// três armas da Tumba, sem o Set Lendário (que não tem fonte jogável).</para>
        /// </summary>
        [Test]
        public void OCaminhoCriticoDaFase1_ChegaAoByakheeNoNivel3()
        {
            int total = ExposicaoAcumuladaAteOByakhee(out string memoria);

            var progressao = new Progressao(Curva);
            progressao.AdicionarExposicao(total);

            Assert.GreaterOrEqual(progressao.NivelAtual, 3,
                $"O jogador chega ao Byakhee no nível {progressao.NivelAtual} com {total} de " +
                $"Exposição. O alvo é 3 — abaixo disso a luta volta a ser a que o Vini jogou e " +
                $"não conseguiu vencer." + Environment.NewLine + memoria);

            Assert.LessOrEqual(progressao.NivelAtual, 4,
                $"O jogador chega ao Byakhee no nível {progressao.NivelAtual}. Acima de 4 o " +
                "chefe que fecha a Fase 1 vira formalidade — e a Fase 2 começa com o jogador " +
                "escalado demais para o conteúdo dela." + Environment.NewLine + memoria);
        }

        /// <summary>
        /// Um chefe tem de <b>ser sentido</b>. A medida honesta não é "subiu de nível ao
        /// abatê-lo" — isso depende de onde na faixa o jogador calhou de estar, e um chefe
        /// generoso pode cair logo depois de um level-up e parecer mesquinho. A medida é a
        /// <b>fração do nível</b> que ele entrega.
        ///
        /// <para>Medido: no caminho crítico o jogador encontra o Abdul por volta de 325 de
        /// Exposição — nível 3, cuja faixa vale 300 (de 300 a 600). Os 150 dele são
        /// <b>metade do nível</b> de uma vez; os 200 do Byakhee, dois terços. É o que o Vini
        /// pediu: <i>"que ele sinta a progressão do personagem"</i>.</para>
        /// </summary>
        [Test]
        public void UmChefe_EntregaPeloMenosMeioNivel()
        {
            const int nivelEsperado = 3;                                  // ver o teste acima
            int faixa = Curva[nivelEsperado] - Curva[nivelEsperado - 1];   // 600 - 300

            foreach (var chefe in new[] { "Abdul_Alhazred", "Byakhee" })
            {
                int valor = ValorDe(chefe);

                Assert.Greater(valor, 0, $"{chefe} concede ZERO de Exposição.");

                Assert.GreaterOrEqual(valor / (float)faixa, 0.5f,
                    $"{chefe} concede {valor}, e a faixa do nível {nivelEsperado} vale {faixa} " +
                    $"— {valor / (float)faixa:P0} do nível. Um chefe que entrega menos de meio " +
                    "nível não é um pico da campanha, é mais uma sala limpa.");
            }
        }

        /// <summary>
        /// O chefe vale <b>bem mais</b> que a tropa. Sem essa distância, matar um chefe é
        /// equivalente a limpar mais uma sala — e o pico da fase não existe na progressão.
        /// </summary>
        [Test]
        public void UmChefe_ValeMuitoMaisQueATropa()
        {
            foreach (var chefe in new[] { "Byakhee", "Abdul_Alhazred" })
                Assert.GreaterOrEqual(ValorDe(chefe), ValorDe("Cultista") * 5,
                    $"{chefe} vale {ValorDe(chefe)} contra {ValorDe("Cultista")} de um Cultista. " +
                    "Um chefe que vale cinco inimigos comuns não é um pico, é uma sala.");
        }

        // ── O abate acontece uma vez ──────────────────────────────────────────

        /// <summary>
        /// O <c>OnAbatido</c> do Abdul ficou pendurado dentro do <c>InstanciarNecronomicon</c> —
        /// e <b>a restauração de save chama esse método de novo</b> quando o tomo ficou no chão.
        /// Recarregar a cena sem recolhê-lo re-rolava a tabela do chefe inteira, e agora
        /// concederia a Exposição de novo junto. Farm de chefe por saída e volta.
        /// </summary>
        [Test]
        public void OAbateDoAbdul_EhAnunciadoUmaVezSo()
        {
            string fonte = File.ReadAllText("Assets/Scripts/Enemies/AbdulAlhazredAI.cs");

            StringAssert.Contains("_jaAnunciouAbate", fonte,
                "A trava de anúncio único sumiu do Abdul.");

            int dentroDoNecronomicon = fonte.IndexOf("private void InstanciarNecronomicon",
                                                    StringComparison.Ordinal);
            Assert.Greater(dentroDoNecronomicon, 0, "InstanciarNecronomicon não existe mais.");

            string corpo = fonte.Substring(dentroDoNecronomicon,
                Math.Min(400, fonte.Length - dentroDoNecronomicon));

            StringAssert.DoesNotContain("OnAbatido?.Invoke", corpo,
                "O OnAbatido voltou para dentro do InstanciarNecronomicon. Esse método TAMBÉM " +
                "roda na restauração de save: sair da cena sem pegar o Necronomicon e voltar " +
                "rolaria o espólio e a Exposição do chefe outra vez.");
        }

        // ── Apoio ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Soma a Exposição de tudo que o jogador encontra até a arena do Byakhee, contando os
        /// prefabs instanciados nas cenas — a fonte da verdade, não uma tabela paralela.
        /// </summary>
        private static int ExposicaoAcumuladaAteOByakhee(out string memoria)
        {
            var linhas = new List<string>();
            int total = 0;

            // O Byakhee mora em Portoes_Das_Ruinas; ele próprio não conta (é o que se vai
            // enfrentar). As duas cenas anteriores do caminho crítico, sim.
            foreach (var cena in new[] { "Deserto_Hali", "Tumba_De_Alhazred" })
            {
                foreach (var ator in new[] { "Cultista", "Abdul_Alhazred" })
                {
                    int quantos = InstanciasNaCena(cena, ator);
                    if (quantos == 0) continue;

                    int valor = ValorDe(ator);
                    total += quantos * valor;
                    linhas.Add($"  {cena}: {quantos}× {ator} × {valor} = {quantos * valor}");
                }
            }

            linhas.Add($"  TOTAL: {total}");
            memoria = string.Join(Environment.NewLine, linhas);
            return total;
        }

        /// <summary>Quanto um prefab concede, lido do asset — pelos dois caminhos possíveis.</summary>
        private static int ValorDe(string prefab)
        {
            string caminho = $"{Enemies}/{prefab}.prefab";
            if (!File.Exists(caminho)) return 0;

            string yaml = File.ReadAllText(caminho);
            return Math.Max(LerInteiro(yaml, "exposicaoAoAbater"), LerInteiro(yaml, "exposicao"));
        }

        /// <summary>
        /// Conta instâncias de um prefab numa cena pelo GUID do <c>m_SourcePrefab</c>. Contar
        /// ocorrências soltas do GUID daria número inflado — cada instância o referencia várias
        /// vezes (uma por override).
        /// </summary>
        private static int InstanciasNaCena(string cena, string prefab)
        {
            string meta = $"{Enemies}/{prefab}.prefab.meta";
            string arquivo = $"{Cenas}/{cena}.unity";

            if (!File.Exists(meta) || !File.Exists(arquivo)) return 0;

            var guid = Regex.Match(File.ReadAllText(meta), @"guid:\s*(\w+)");
            if (!guid.Success) return 0;

            return Regex.Matches(File.ReadAllText(arquivo),
                       @"m_SourcePrefab:\s*\{fileID:\s*100100000,\s*guid:\s*" +
                       guid.Groups[1].Value).Count;
        }

        /// <summary>
        /// Lê um campo inteiro do YAML. Ausente devolve <b>0</b> de propósito — para o campo que
        /// nunca foi serializado (e vale o padrão do C#) não passar por calibrado. Foi
        /// exatamente assim que a ferramenta de nível de drop deixou o Cultista para trás.
        /// </summary>
        private static int LerInteiro(string yaml, string campo)
        {
            var m = Regex.Match(yaml, $@"^\s*{campo}:\s*(-?\d+)\s*$", RegexOptions.Multiline);
            return m.Success ? int.Parse(m.Groups[1].Value) : 0;
        }
    }
}
