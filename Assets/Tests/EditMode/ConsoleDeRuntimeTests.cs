using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using FavelaAmarela.Core.Progression;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda o <b>console de runtime</b> — a metade do Carcosa Debugger que a build pode ter.
    ///
    /// <para><b>A pergunta do Vini (2026-08-29):</b> <i>"O Carcosa Debugger vai funcionar na
    /// build?"</i> Não, e não tinha como: o <c>CarcosaDebuggerWindow</c> é um
    /// <c>EditorWindow</c> em pasta <c>Editor/</c>, que a Unity remove do player. O console
    /// existe para cobrir o que faz falta jogando uma build — e ele tem <b>duas</b> propriedades
    /// que, se quebrarem, o tornam pior que inútil.</para>
    /// </summary>
    public sealed class ConsoleDeRuntimeTests
    {
        private const string Console = "Assets/Scripts/Diagnostico/ConsoleDeCarcosa.cs";

        // ── A guarda: não pode vazar para a build final ───────────────────────

        /// <summary>
        /// <b>A propriedade que mais importa.</b> Um console de trapaça numa build de release é
        /// um jogo que o jogador pode quebrar em dois cliques — e o Vini pretende vender este.
        /// A guarda tem de envolver o arquivo <b>inteiro</b>: um <c>#if</c> que cobre só parte
        /// dele deixaria a classe existir e o menu acessível.
        /// </summary>
        [Test]
        public void OConsole_NaoExisteEmBuildDeRelease()
        {
            Assert.IsTrue(File.Exists(Console), $"{Console} não existe.");

            var linhas = File.ReadAllLines(Console);

            int abertura = Array.FindIndex(linhas,
                l => l.Trim() == "#if UNITY_EDITOR || DEVELOPMENT_BUILD");

            Assert.GreaterOrEqual(abertura, 0,
                "A guarda '#if UNITY_EDITOR || DEVELOPMENT_BUILD' sumiu do console. Sem ela, um " +
                "console de trapaça vai junto na build que o jogador compra.");

            // Antes da guarda só pode haver comentário e linha em branco -- nada de código.
            for (int i = 0; i < abertura; i++)
            {
                string l = linhas[i].Trim();

                Assert.IsTrue(l.Length == 0 || l.StartsWith("//"),
                    $"Linha {i + 1} está FORA da guarda: '{l}'. Todo o arquivo tem de estar " +
                    "dentro dela.");
            }

            int fechamento = Array.FindLastIndex(linhas, l => l.Trim() == "#endif");

            Assert.Greater(fechamento, abertura, "O '#endif' que fecha a guarda sumiu.");

            for (int i = fechamento + 1; i < linhas.Length; i++)
            {
                Assert.IsEmpty(linhas[i].Trim(),
                    $"Linha {i + 1} está DEPOIS do #endif: '{linhas[i].Trim()}'.");
            }
        }

        /// <summary>
        /// O console vive em <c>Assets/Scripts/</c>, não em <c>Editor/</c> — é código de runtime,
        /// e tem de ser. Mas então ele <b>não pode</b> tocar em <c>UnityEditor</c>: isso quebraria
        /// a compilação de qualquer build, inclusive a de desenvolvimento.
        /// </summary>
        [Test]
        public void OConsole_NaoDependeDoUnityEditor()
        {
            string fonte = File.ReadAllText(Console);

            StringAssert.DoesNotContain("using UnityEditor", fonte,
                "O console de runtime passou a depender de UnityEditor. Isso não compila em " +
                "build nenhuma — e o ponto dele é justamente rodar onde o Editor não existe.");
        }

        // ── O nascimento: sem isto, ele existe e não aparece ──────────────────

        /// <summary>
        /// <b>O modo de falha dominante deste repositório</b>, aplicado ao console: a peça
        /// existe, compila, não dá erro, e não está em cena nenhuma. Foi assim que a progressão
        /// ficou inerte por meses (o <c>ProgressionManager</c> não estava em cena alguma e
        /// <c>Instance</c> era sempre nulo).
        ///
        /// <para>Um console que precisasse ser arrastado para cada cena estaria ausente
        /// justamente na cena onde algo deu errado.</para>
        /// </summary>
        [Test]
        public void OConsole_NasceSozinhoEmQualquerCena()
        {
            string fonte = File.ReadAllText(Console);

            StringAssert.Contains("RuntimeInitializeOnLoadMethod", fonte,
                "O console deixou de nascer sozinho. Ele passa a depender de alguém lembrar de " +
                "pô-lo em cada cena — e vai faltar exatamente na cena com o bug.");

            StringAssert.Contains("BeforeSceneLoad", fonte,
                "O console nasce depois da cena carregar. Bug de bootstrap acontece antes disso.");

            StringAssert.Contains("DontDestroyOnLoad", fonte,
                "O console morre na troca de cena — e trocar de cena é uma das coisas que ele faz.");
        }

        /// <summary>
        /// Ele congela o jogo enquanto está aberto, e <b>tem de descongelar</b>. Um console que
        /// deixa <c>timeScale</c> em 0 ao fechar trava a partida, e o sintoma pareceria um bug do
        /// jogo — o pior desfecho possível para uma ferramenta de diagnóstico.
        /// </summary>
        [Test]
        public void OConsole_DevolveOTempoAoFechar()
        {
            string fonte = File.ReadAllText(Console);

            StringAssert.Contains("Time.timeScale = _escalaAnterior", fonte,
                "O console voltou a restaurar o tempo com um valor fixo, ou parou de restaurar. " +
                "Restaurar '1' também é errado: se a partida já estava pausada quando o console " +
                "abriu, fechá-lo despausaria o jogo por conta própria.");
        }

        /// <summary>
        /// O item concedido tem de passar pelo <b>mesmo</b> gerador do espólio e do baú. Um
        /// caminho próprio produziria um item que o jogo não produz — e aí o console estaria
        /// testando outra coisa que não o jogo, que é a única forma de uma ferramenta de
        /// diagnóstico mentir.
        /// </summary>
        [Test]
        public void OItemConcedido_PassaPelasRegrasDoJogo()
        {
            string fonte = File.ReadAllText(Console);

            foreach (var (trecho, porque) in new[]
                     {
                         ("_gerador.Gerar(", "o item sai do GeradorDeItem, com afixos rolados"),
                         ("CurvaDeGrau.Sortear", "o grau sai da curva, como no drop de verdade"),
                     })
            {
                StringAssert.Contains(trecho, fonte,
                    $"O console deixou de garantir que {porque}.");
            }
        }

        // ── A peça que o console precisou do Core ─────────────────────────────

        /// <summary>
        /// <c>ExposicaoParaONivel</c> nasceu para o console poder dizer "faltam N para o
        /// próximo" sem manter uma cópia da curva. Cópia de curva é como este repositório chegou
        /// a ter dois testes e um documento defendendo números que o jogo não usava.
        /// </summary>
        [Test]
        public void ACurva_PodeSerConsultadaSemSerCopiada()
        {
            var curva = new[] { 0, 100, 300, 600 };
            var progressao = new Progressao(curva);

            Assert.AreEqual(0, progressao.ExposicaoParaONivel(1), "O nível 1 não custa nada.");
            Assert.AreEqual(100, progressao.ExposicaoParaONivel(2));
            Assert.AreEqual(600, progressao.ExposicaoParaONivel(4));

            Assert.AreEqual(0, progressao.ExposicaoParaONivel(0),
                "Nível abaixo de 1 tem de devolver 0, não estourar índice.");

            Assert.AreEqual(600, progressao.ExposicaoParaONivel(99),
                "Acima do teto devolve o último degrau — o console pergunta pelo nível seguinte " +
                "mesmo estando no teto.");
        }

        [Test]
        public void OQuantoFalta_ZeraNoTeto()
        {
            var progressao = new Progressao(new[] { 0, 100, 300 });

            Assert.AreEqual(100, progressao.ExposicaoAteOProximoNivel);

            progressao.AdicionarExposicao(40);
            Assert.AreEqual(60, progressao.ExposicaoAteOProximoNivel,
                "Somar 40 tem de reduzir o que falta em 40.");

            progressao.AdicionarExposicao(1000);

            Assert.IsTrue(progressao.NoTeto, "1040 passa do último degrau.");
            Assert.AreEqual(0, progressao.ExposicaoAteOProximoNivel,
                "No teto não falta nada — uma barra de progresso não pode pedir mais.");
        }

        /// <summary>
        /// O botão "ir direto para o nível N" soma exatamente a diferença. Se a conta estiver
        /// errada, o console entrega um nível que não é o pedido — e todo teste de balanceamento
        /// feito com ele mede a coisa errada.
        /// </summary>
        [Test]
        public void OSaltoDeNivel_ChegaExatamenteNoNivelPedido()
        {
            var curva = new[] { 0, 100, 300, 600, 1000, 1500, 2100, 2800, 3600, 4500, 5500, 6600 };

            for (int alvo = 2; alvo <= curva.Length; alvo++)
            {
                var progressao = new Progressao(curva);

                // É a conta que o console faz.
                int falta = progressao.ExposicaoParaONivel(alvo) - progressao.ExposicaoAtual;
                progressao.AdicionarExposicao(falta);

                Assert.AreEqual(alvo, progressao.NivelAtual,
                    $"Pedir o nível {alvo} entregou o {progressao.NivelAtual}.");
            }
        }
    }
}
