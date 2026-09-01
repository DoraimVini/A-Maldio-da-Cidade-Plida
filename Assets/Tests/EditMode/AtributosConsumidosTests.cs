using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a tabela de "quais atributos fazem alguma coisa" do <c>PainelDeFicha</c> contra o
    /// que o código de produção realmente consome.
    ///
    /// <para><b>Por que existe:</b> o painel marca <c>SEM EFEITO</c> em bônus de
    /// <c>StatType</c> que nenhum sistema lê. Essa marca só vale se a lista estiver certa — e
    /// uma lista escrita à mão sobre 15 valores <b>já nasceu errada</b>: na primeira versão
    /// faltavam <c>RegenRM</c> e <c>DrenoRM</c>, que o <c>GerenciadorEfeitosPassivos.Update</c>
    /// converte em <c>Ancorar</c>/<c>SofrerTrauma</c> a cada frame. O painel teria dito "SEM
    /// EFEITO" sobre dois atributos funcionando.</para>
    ///
    /// <para>Um instrumento de diagnóstico que mente é pior que instrumento nenhum: leva a
    /// concluir que um item está quebrado quando ele funciona, ou o contrário. Este teste lê as
    /// <b>duas</b> fontes — a tabela e o código — e exige que concordem.</para>
    /// </summary>
    public sealed class AtributosConsumidosTests
    {
        private const string Painel = "Assets/Scripts/UI/PainelDeFicha.cs";

        /// <summary>
        /// <c>StatType</c> que o código menciona, mas <b>não</b> como bônus passivo — logo, não
        /// entram na tabela do painel, que só fala de passivas.
        ///
        /// <para>Existe porque a busca é por menção, e não por <c>GetBonus(StatType.X)</c>
        /// literal: <c>MaoFisicaBridge</c> chega lá por um helper (<c>BonusPassivo(atributo)</c>),
        /// e procurar só a chamada direta perderia <c>TraumaFisico</c> e <c>TraumaAnomalia</c>.
        /// O preço é este: menções em outros contextos precisam ser declaradas aqui, com o
        /// motivo.</para>
        /// </summary>
        private static readonly Dictionary<string, string> UsadosForaDePassivas =
            new Dictionary<string, string>
            {
                ["RMMaxima"] =
                    "Efeito de CONSUMÍVEL, não passiva: VitalidadeBridge.AplicarEfeitoConsumivel " +
                    "chama ResilienciaMental.Ancorar(mod.Valor). Como bônus de item equipado, " +
                    "ninguém lê GetBonus(RMMaxima) — por isso fica fora da tabela do painel.",
            };

        [Test]
        public void TabelaDoPainel_ConcordaComOQueOCodigoConsome()
        {
            var declarados = StatTypesDeclarados();
            Assert.IsNotEmpty(declarados, "Não consegui ler o enum StatType.");

            var naTabela = MarcadosComoConsumidos();
            var noCodigo = UsadosEmProducao(declarados);
            noCodigo.ExceptWith(UsadosForaDePassivas.Keys);

            var mentemQueFunciona = naTabela.Except(noCodigo).OrderBy(s => s).ToList();
            var mentemQueNaoFunciona = noCodigo.Except(naTabela).OrderBy(s => s).ToList();

            Assert.IsEmpty(mentemQueNaoFunciona,
                "StatType consumido pelo código mas AUSENTE da tabela do PainelDeFicha. O painel " +
                "vai marcar 'SEM EFEITO' num atributo que funciona:\n  " +
                string.Join("\n  ", mentemQueNaoFunciona) +
                "\nAcrescente-os a AtributoConsomeBonus em " + Painel);

            Assert.IsEmpty(mentemQueFunciona,
                "StatType marcado como consumido na tabela, mas nenhum código de produção o " +
                "menciona. O painel vai deixar de avisar que o atributo é decorativo:\n  " +
                string.Join("\n  ", mentemQueFunciona) +
                "\nRemova-os de AtributoConsomeBonus em " + Painel);
        }

        // ── Apoio ────────────────────────────────────────────────────────────

        private static List<string> StatTypesDeclarados()
        {
            string fonte = File.ReadAllText("Assets/Scripts/Inventario/ItemEnums.cs");
            var bloco = Regex.Match(fonte, @"enum\s+StatType\s*\{(.*?)\}", RegexOptions.Singleline);
            if (!bloco.Success) return new List<string>();

            // Nome no início da linha, antes de vírgula ou comentário.
            return Regex.Matches(bloco.Groups[1].Value, @"^\s*([A-Za-z_]\w*)\s*(?=[,\r\n/])",
                                 RegexOptions.Multiline)
                        .Cast<Match>()
                        .Select(m => m.Groups[1].Value)
                        .Distinct()
                        .ToList();
        }

        private const string Fonte = "Assets/Scripts/Inventario/NomesDeAtributo.cs";

        /// <summary>
        /// Os <c>StatType</c> declarados como <b>consumidos</b> — ou seja, todos menos os que
        /// <c>NomesDeAtributo.SemEfeito</c> lista.
        ///
        /// <para><b>Migrado em 2026-09-01.</b> Antes lia os <c>StatType.X =&gt; true</c> do
        /// switch do <c>PainelDeFicha</c>. Aquele switch era uma <b>segunda cópia</b> da mesma
        /// verdade, e divergiu exatamente como o comentário de <c>NomesDeAtributo</c> previa:
        /// <c>Furtividade</c> e <c>DefesaAnomalia</c> ganharam consumidor, só uma lista foi
        /// atualizada, e um afixo legítimo foi barrado por um guarda lendo a lista velha.</para>
        ///
        /// <para>Agora o painel <b>delega</b>, e este guarda lê a fonte única. A dependência
        /// aponta na direção certa: dado não pergunta à interface.</para>
        /// </summary>
        private static HashSet<string> MarcadosComoConsumidos()
        {
            var todos = StatTypesDeclarados().ToHashSet();

            string fonte = File.ReadAllText(Fonte);
            var corpo = Regex.Match(fonte, @"SemEfeito\s*=\s*new\[\]\s*\{(.*?)\};",
                                    RegexOptions.Singleline);

            Assert.IsTrue(corpo.Success,
                "Não achei a lista SemEfeito em " + Fonte + " — se foi renomeada, atualize este " +
                "guarda junto.");

            var semEfeito = Regex.Matches(corpo.Groups[1].Value, @"StatType\.(\w+)")
                                 .Cast<Match>()
                                 .Select(m => m.Groups[1].Value)
                                 .ToHashSet();

            todos.ExceptWith(semEfeito);
            return todos;
        }

        /// <summary>
        /// <c>StatType</c> mencionados por código de produção fora da declaração do enum e fora
        /// do próprio painel.
        ///
        /// <para>Menção, e não <c>GetBonus(StatType.X)</c> literal, porque há indireção real:
        /// <c>MaoFisicaBridge</c> passa por um helper <c>BonusPassivo(atributo)</c>. Procurar só
        /// a chamada direta perderia <c>TraumaFisico</c> e <c>TraumaAnomalia</c>. A
        /// contrapartida — um <c>StatType</c> citado só num comentário contaria como consumido —
        /// é aceitável: erra para o lado de não acusar falso positivo.</para>
        /// </summary>
        private static HashSet<string> UsadosEmProducao(List<string> declarados)
        {
            var usados = new HashSet<string>();

            foreach (var arquivo in Directory.GetFiles("Assets/Scripts", "*.cs",
                         SearchOption.AllDirectories))
            {
                var nome = Path.GetFileName(arquivo);
                // NomesDeAtributo.cs entrou aqui em 2026-08-27, pelo mesmo motivo dos
                // outros dois: ele NOMEIA atributos, não os consome. Citar um StatType
                // para escrever o rótulo dele não é ler o bônus e aplicá-lo -- sem esta
                // exclusão, a tabela de tradução faria os 4 atributos decorativos
                // parecerem implementados.
                if (nome == "ItemEnums.cs" || nome == "PainelDeFicha.cs" ||
                    nome == "NomesDeAtributo.cs") continue;

                string texto = File.ReadAllText(arquivo);
                foreach (var stat in declarados)
                {
                    if (Regex.IsMatch(texto, $@"StatType\.{Regex.Escape(stat)}\b"))
                        usados.Add(stat);
                }
            }

            return usados;
        }
    }
}
