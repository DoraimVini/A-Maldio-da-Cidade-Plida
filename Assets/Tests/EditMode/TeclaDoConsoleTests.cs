using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda que o <b>console de diagnóstico não disputa tecla</b> com nenhuma ação do jogo.
    ///
    /// <para><b>O que motivou (2026-09-02).</b> O Vini reclamou por nome: <i>"você pôs o console
    /// de cheat no mesmo botão do artefato"</i>. Estava certo — o console lia <c>f1Key</c> cru e
    /// o <c>InputSystem_Actions</c> tinha <c>&lt;Keyboard&gt;/f1</c> ligado a
    /// <c>HabilidadeArtefato1</c>. Os dois disparavam no mesmo frame, sem ordem definida:
    /// <b>abrir o console queimava o Artefato do slot 1</b>, com o custo de Resiliência Mental
    /// junto. O save dele tinha o Necronomicon justamente em F1.</para>
    ///
    /// <para><b>Por que este teste lê CÓDIGO-FONTE, se eu mesmo critiquei isso.</b> O
    /// <c>TextoLegivelTests</c> faz regex no fonte para adivinhar um resultado visual, e por isso
    /// é teatro. Aqui a pergunta <b>é</b> textual dos dois lados: "que teclas este arquivo lê" e
    /// "que teclas o asset de ações reserva". Nenhum dos dois vira outra coisa em runtime.</para>
    /// </summary>
    public sealed class TeclaDoConsoleTests
    {
        private const string Console = "Assets/Scripts/Diagnostico/ConsoleDeCarcosa.cs";
        private const string Acoes = "Assets/InputSystem_Actions.inputactions";

        /// <summary>
        /// Teclas que o console pode dividir com o asset de ações, <b>com a razão</b>.
        ///
        /// <para><b>Vazia, e isso é o correto.</b> Cheguei a pôr <c>escape</c> aqui achando que
        /// era disputa aceita — e o teste irmão me corrigiu: <c>escape</c> <b>não aparece</b> no
        /// <c>InputSystem_Actions</c>. O Esc do jogo é lido <b>cru</b> pelo
        /// <c>PausaInputHandler</c>, e o <c>Cancel</c> da UI vem do asset <i>default do pacote</i>,
        /// não do nosso.</para>
        ///
        /// <para>Ou seja: a disputa do Escape é real (console × pausa × UI) e este arquivo
        /// <b>não a enxerga</b>, porque só compara o console com o asset de ações. Ela pertence à
        /// camada de input — Fase 3 do plano de 2026-09-02 — e vai precisar de um teste que
        /// varra os leitores de <c>Keyboard.current</c> de todo o projeto.</para>
        /// </summary>
        private static readonly string[] Permitidas = new string[0];

        private static string Ler(string caminho)
        {
            Assert.IsTrue(File.Exists(caminho), $"Arquivo ausente: {caminho}");
            return File.ReadAllText(caminho);
        }

        /// <summary>As teclas que o console lê de <c>Keyboard.current</c>.</summary>
        private static string[] TeclasDoConsole() =>
            Regex.Matches(Ler(Console), @"teclado\.(\w+)Key\b")
                 .Cast<Match>()
                 .Select(m => m.Groups[1].Value.ToLowerInvariant())
                 .Distinct()
                 .ToArray();

        /// <summary>As teclas que o asset de ações reserva para o jogo.</summary>
        private static string[] TeclasDasAcoes() =>
            Regex.Matches(Ler(Acoes), @"<Keyboard>/(\w+)")
                 .Cast<Match>()
                 .Select(m => m.Groups[1].Value.ToLowerInvariant())
                 .Distinct()
                 .ToArray();

        [Test]
        public void OConsoleNaoDisputaTeclaComOJogo()
        {
            var doConsole = TeclasDoConsole();
            var doJogo = TeclasDasAcoes();

            Assert.IsNotEmpty(doConsole,
                "Não achei nenhuma leitura 'teclado.XKey' no console — ou ele mudou de API, e " +
                "este teste deixou de guardar o que diz guardar.");

            Assert.IsNotEmpty(doJogo,
                "Não achei nenhum '<Keyboard>/x' no asset de ações — mesma suspeita.");

            var disputadas = doConsole
                .Where(t => doJogo.Contains(t))
                .Where(t => !Permitidas.Contains(t))
                .ToList();

            Assert.IsEmpty(disputadas,
                "Tecla(s) que o console divide com uma ação do jogo: " +
                string.Join(", ", disputadas) + Environment.NewLine +
                "Os dois leitores disparam no mesmo frame, e não há ordem definida entre eles: " +
                "abrir o console executa a ação junto. Foi assim que o F1 queimava o Artefato " +
                "do slot 1." + Environment.NewLine +
                "Conserto: mover a tecla do console, ou declarar em " +
                "TeclaDoConsoleTests.Permitidas COM A RAZÃO.");
        }

        /// <summary>
        /// O outro lado: uma tecla declarada como permitida que <b>deixou</b> de ser disputada
        /// tem de sair da lista, senão a lista vira ficção e o próximo a ler acredita.
        /// </summary>
        [Test]
        public void NenhumaPermitida_JaDeixouDeSerDisputada()
        {
            var doConsole = TeclasDoConsole();
            var doJogo = TeclasDasAcoes();

            var obsoletas = Permitidas
                .Where(t => !doConsole.Contains(t) || !doJogo.Contains(t))
                .ToList();

            Assert.IsEmpty(obsoletas,
                "Tecla(s) declaradas como disputa aceita que não são mais disputadas: " +
                string.Join(", ", obsoletas) + ". Remova de TeclaDoConsoleTests.Permitidas.");
        }
    }
}
