using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda que <b>nenhuma tecla do mapa "Player" dispara duas ações</b>.
    ///
    /// <para><b>O que motivou (2026-09-02).</b> A auditoria mediu sete disputas de tecla, e uma
    /// delas estava <b>dentro do próprio asset de ações</b>: <c>Sprint</c> e <c>Crouch</c>
    /// ligadas ao mesmo <c>leftShift</c>. O <c>PlayerMovement.cs</c> lê as duas com
    /// <c>if/else</c> e o Furtivo vem primeiro, então <b>correr era inalcançável pelo
    /// teclado</b> — e o Vigor nunca era gasto correndo. Uma mecânica inteira morta, na build
    /// final, sem uma linha de erro.</para>
    ///
    /// <para>O mapa <b>UI</b> fica de fora: ele é código morto (ninguém o referencia — o
    /// <c>EventSystem</c> usa o asset <i>default do pacote</i>), e ali as repetições são do
    /// template da Unity, não decisão nossa.</para>
    /// </summary>
    public sealed class ConflitoDeTeclaTests
    {
        private const string Acoes = "Assets/InputSystem_Actions.inputactions";

        /// <summary>
        /// Teclas que podem servir a duas ações, <b>com a razão</b>. Vazia hoje, e é para
        /// continuar assim: dividir tecla no mesmo mapa é sempre uma decisão, nunca um
        /// descuido.
        /// </summary>
        private static readonly string[] Permitidas = new string[0];

        /// <summary>
        /// O trecho do arquivo que descreve o mapa "Player". Recortado pelo nome do mapa
        /// seguinte, para não arrastar as ações da UI.
        /// </summary>
        private static string MapaDoJogador()
        {
            Assert.IsTrue(File.Exists(Acoes), $"Asset ausente: {Acoes}");
            string todo = File.ReadAllText(Acoes);

            int inicio = todo.IndexOf("\"name\": \"Player\"", StringComparison.Ordinal);
            Assert.Greater(inicio, 0, "Mapa 'Player' não encontrado no asset de ações.");

            int fim = todo.IndexOf("\"name\": \"UI\"", inicio, StringComparison.Ordinal);
            if (fim < 0) fim = todo.Length;

            return todo.Substring(inicio, fim - inicio);
        }

        [Test]
        public void NenhumaTeclaDoJogo_DisparaDuasAcoes()
        {
            string mapa = MapaDoJogador();

            // Cada binding é um objeto com "path" e, mais abaixo, "action". Pego os dois na
            // ordem em que aparecem dentro do mesmo bloco.
            var bindings = Regex.Matches(mapa,
                    "\"path\": \"(<Keyboard>/[^\"]+)\"(?:(?!\"path\").)*?\"action\": \"([^\"]*)\"",
                    RegexOptions.Singleline)
                .Cast<Match>()
                .Select(m => new { Tecla = m.Groups[1].Value, Acao = m.Groups[2].Value })
                .Where(b => !string.IsNullOrEmpty(b.Acao))
                .ToList();

            Assert.IsNotEmpty(bindings,
                "Nenhum binding de teclado encontrado no mapa Player — ou o formato do asset " +
                "mudou, e este teste deixou de guardar o que diz guardar.");

            var disputadas = bindings
                .GroupBy(b => b.Tecla)
                .Where(g => g.Select(b => b.Acao).Distinct().Count() > 1)
                .Where(g => !Permitidas.Contains(g.Key))
                .Select(g => $"{g.Key} → {string.Join(" e ", g.Select(b => b.Acao).Distinct())}")
                .ToList();

            Assert.IsEmpty(disputadas,
                "Tecla(s) com duas ações no mapa Player:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", disputadas) + Environment.NewLine +
                "Quem lê as duas decide com if/else, e a que vier depois nunca é alcançada — " +
                "foi assim que correr virou mecânica morta. Conserto: outra tecla, ou declarar " +
                "em ConflitoDeTeclaTests.Permitidas COM A RAZÃO.");
        }

        /// <summary>
        /// O console de diagnóstico não pode voltar a morar numa tecla do jogo. O
        /// <c>TeclaDoConsoleTests</c> guarda o mesmo do outro lado; aqui a asserção é direta,
        /// para o motivo ficar junto do resto das teclas.
        /// </summary>
        [Test]
        public void ATeclaDoConsole_NaoEstaNoMapaDoJogador()
        {
            const string ConsoleCs = "Assets/Scripts/Diagnostico/ConsoleDeCarcosa.cs";
            Assert.IsTrue(File.Exists(ConsoleCs), $"Ausente: {ConsoleCs}");

            // A tecla que ABRE o console é a primeira lida sem o guard de `_aberto`.
            var abre = Regex.Match(File.ReadAllText(ConsoleCs),
                                   @"if \(teclado\.(\w+)Key\.wasPressedThisFrame\) Alternar\(\);");

            Assert.IsTrue(abre.Success,
                "Não achei a linha que abre o console — ele mudou de forma e este teste " +
                "deixou de guardar o que diz guardar.");

            string tecla = abre.Groups[1].Value.ToLowerInvariant();

            var noJogo = Regex.Matches(MapaDoJogador(), @"<Keyboard>/(\w+)")
                .Cast<Match>()
                .Any(m => m.Groups[1].Value.ToLowerInvariant() == tecla);

            Assert.IsFalse(noJogo,
                $"O console abre com '{tecla}', que o mapa Player também usa. Os dois disparam " +
                "no mesmo frame, sem ordem definida entre eles: abrir o console executa a ação " +
                "junto — foi assim que o F1 queimava o Artefato do slot 1.");
        }
    }
}
