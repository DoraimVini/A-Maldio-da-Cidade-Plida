using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda que <b>todo leitor de teclado consulta o árbitro de foco</b>.
    ///
    /// <para><b>O que motivou (2026-09-02).</b> A auditoria mediu sete disputas de tecla e
    /// concluiu que a causa não era nenhuma delas: era <b>não existir camada de input</b>. Três
    /// fontes liam teclado ao mesmo tempo e nenhuma conhecia as outras, e
    /// <c>Time.timeScale = 0</c> <b>não engole tecla</b> — <c>Update()</c> continua rodando.</para>
    ///
    /// <para>Os sintomas: com a mochila aberta, F1–F4 continuavam queimando Artefatos e 1–8
    /// continuavam consumindo itens; <b>digitar "3" no console consumia o item do slot 3</b>;
    /// Esc não fechava o inventário, pausava o jogo por baixo dele.</para>
    ///
    /// <para>Este teste é a versão generalizada da lição: leitor novo nasce respeitando o
    /// árbitro, ou entra na lista <b>com a razão escrita</b>.</para>
    /// </summary>
    public sealed class LeitorDeTeclaRespeitaOFocoTests
    {
        private const string Scripts = "Assets/Scripts";

        /// <summary>Como um arquivo prova que conhece o árbitro.</summary>
        private static readonly string[] Provas =
        {
            "ArbitroDeFoco",       // consulta ou toma/devolve
            "CamadaDeEntrada",     // idem
        };

        /// <summary>
        /// Quem lê teclado e <b>não</b> precisa consultar, com a razão.
        /// </summary>
        private static readonly (string Arquivo, string Porque)[] Dispensados =
        {
            ("ArbitroDeFoco.cs", "é o próprio árbitro"),

            // O PainelDeEscolha chegou a entrar aqui e o teste irmão me corrigiu: ele lê
            // Move/Interact pelo sistema de AÇÕES, não por Keyboard.current, então nunca foi
            // candidato. Uma dispensa que não dispensa nada é ruído que ensina a confiar na
            // lista errada.
        };

        [Test]
        public void TodoLeitorDeTecladoCru_ConsultaOArbitro()
        {
            Assert.IsTrue(Directory.Exists(Scripts), $"Pasta ausente: {Scripts}");

            var infratores = Directory
                .GetFiles(Scripts, "*.cs", SearchOption.AllDirectories)
                .Select(caminho => new { Nome = Path.GetFileName(caminho),
                                         Fonte = File.ReadAllText(caminho) })
                // Leitura CRUA de teclado: fora do sistema de ações, e portanto fora de
                // qualquer mapa que pudesse ser desligado.
                .Where(a => a.Fonte.Contains("Keyboard.current"))
                .Where(a => !Dispensados.Any(d => d.Arquivo == a.Nome))
                .Where(a => !Provas.Any(p => a.Fonte.Contains(p)))
                .Select(a => a.Nome)
                .ToList();

            Assert.IsEmpty(infratores,
                "Arquivo(s) que leem Keyboard.current sem consultar o árbitro de foco: " +
                string.Join(", ", infratores) + Environment.NewLine +
                "Leitura crua ignora mapa de ação, painel aberto e Time.timeScale — foi assim " +
                "que digitar no console consumia itens da mochila." + Environment.NewLine +
                "Conserto: 'if (!ArbitroDeFoco.JogoNoComando) return;', ou declarar em " +
                "LeitorDeTeclaRespeitaOFocoTests.Dispensados COM A RAZÃO.");
        }

        /// <summary>
        /// O outro lado: um dispensado que deixou de ler teclado sai da lista, senão ela vira
        /// ficção e o próximo a ler acredita.
        /// </summary>
        [Test]
        public void NenhumDispensado_DeixouDeLerTeclado()
        {
            var obsoletos = Dispensados
                .Where(d =>
                {
                    var achados = Directory.GetFiles(Scripts, d.Arquivo, SearchOption.AllDirectories);
                    if (achados.Length == 0) return true;

                    string fonte = File.ReadAllText(achados[0]);

                    // O árbitro é dispensado por ser o árbitro, não por ler teclado.
                    if (d.Arquivo == "ArbitroDeFoco.cs") return false;

                    return !fonte.Contains("Keyboard.current");
                })
                .Select(d => d.Arquivo)
                .ToList();

            Assert.IsEmpty(obsoletos,
                "Dispensado(s) que não leem mais teclado cru: " + string.Join(", ", obsoletos) +
                ". Remova de LeitorDeTeclaRespeitaOFocoTests.Dispensados.");
        }
    }
}
