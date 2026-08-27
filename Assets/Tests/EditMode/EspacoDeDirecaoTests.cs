using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using FavelaAmarela.Core.Player;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// O guarda que teria pego o defeito mais caro desta branch.
    ///
    /// <para><b>O que aconteceu.</b> <c>ConvertToIsometric</c> estava correta e casava com o
    /// grid 2:1 das cenas. Mas <b>só o movimento a usava</b>: <c>LookDirection</c> e a direção
    /// do golpe recebiam o input cru. O corpo ia para um lado, a mira e o sprite para outro —
    /// <b>26,6° de desvio na horizontal, 63,4° na vertical</b>. O Vini viu jogando: <i>"as 8
    /// direções... tudo parece meio fora"</i>.</para>
    ///
    /// <para><b>Por que os testes de antes não pegaram:</b> não havia teste nenhum sobre
    /// direção. A conversão era <c>private static</c> dentro de um <c>MonoBehaviour</c> e
    /// portanto intestável — e o que não é testável não é testado.</para>
    ///
    /// <para>A tabela dos 8 inputs abaixo é o <b>oráculo</b> desta suíte: ela descreve o que o
    /// jogador vê na tela, e todo consumidor de direção tem de concordar com ela.</para>
    /// </summary>
    public sealed class EspacoDeDirecaoTests
    {
        /// <summary>
        /// Os 8 inputs, o ângulo de mundo que cada um produz, e o que se vê.
        ///
        /// <para>A consequência que surpreende e é <b>correta</b> num grid 2:1: <b>as diagonais
        /// do teclado viram as cardinais da tela</b>. W+D sobe reto; W sobe para a esquerda.</para>
        /// </summary>
        private static readonly (string Tecla, float X, float Y, float AnguloDeMundo, string OQueSeVe)[] Tabela =
        {
            ("D",    1f,  0f,   26.565f, "cima-direita"),
            ("W+D",  1f,  1f,   90.000f, "reto para CIMA"),
            ("W",    0f,  1f,  153.435f, "cima-esquerda"),
            ("W+A", -1f,  1f,  180.000f, "reto para ESQUERDA"),
            ("A",   -1f,  0f, -153.435f, "baixo-esquerda"),
            ("S+A", -1f, -1f,  -90.000f, "reto para BAIXO"),
            ("S",    0f, -1f,  -26.565f, "baixo-direita"),
            ("S+D",  1f, -1f,    0.000f, "reto para DIREITA"),
        };

        private static float AnguloDe(Vector2 v) => Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;

        [Test]
        public void OsOitoInputs_ProduzemOsAngulosDeMundoEsperados()
        {
            var erros = new List<string>();

            foreach (var (tecla, x, y, esperado, oQueSeVe) in Tabela)
            {
                var mundo = BaseIsometrica.ParaMundo(new Vector2(x, y));
                float obtido = AnguloDe(mundo);

                // Normaliza a diferença para [-180, 180]: -180 e 180 são o mesmo ângulo.
                float diferenca = Mathf.Abs(Mathf.DeltaAngle(obtido, esperado));

                if (diferenca > 0.01f)
                    erros.Add($"{tecla}: esperado {esperado:0.###}° ({oQueSeVe}), obtido {obtido:0.###}°");
            }

            Assert.IsEmpty(erros,
                "A base isométrica mudou de resultado:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", erros) + Environment.NewLine +
                "Esta tabela descreve o que o jogador VÊ. Mudá-la é mudar o jogo, não corrigir " +
                "um número.");
        }

        /// <summary>
        /// O ponto que mais confunde quem lê o código pela primeira vez, fixado em teste para
        /// não ser "consertado" por engano.
        /// </summary>
        [Test]
        public void AsDiagonaisDoTeclado_SaoAsCardinaisDaTela()
        {
            Assert.AreEqual(90f, AnguloDe(BaseIsometrica.ParaMundo(new Vector2(1f, 1f))), 0.01f,
                "W+D tem de subir RETO na tela. Se isto virar 45°, a conversão foi removida.");

            Assert.AreEqual(0f, AnguloDe(BaseIsometrica.ParaMundo(new Vector2(1f, -1f))), 0.01f,
                "S+D tem de ir RETO para a direita.");

            Assert.AreNotEqual(90f, AnguloDe(BaseIsometrica.ParaMundo(new Vector2(0f, 1f))),
                "W NÃO sobe reto num grid isométrico — ele sobe para a esquerda (153,4°).");
        }

        [Test]
        public void ParadoContinuaParado()
        {
            Assert.AreEqual(Vector2.zero, BaseIsometrica.ParaMundo(Vector2.zero),
                "Normalizar um vetor nulo devolveria lixo (NaN ou (0,0) dependendo da versão), " +
                "e 'parado' viraria uma direção qualquer.");
        }

        [Test]
        public void OResultado_ESempreNormalizado()
        {
            foreach (var (tecla, x, y, _, _) in Tabela)
            {
                var v = BaseIsometrica.ParaMundo(new Vector2(x, y) * 37f);   // magnitude qualquer
                Assert.AreEqual(1f, v.magnitude, 0.001f,
                    $"{tecla}: a direção precisa sair normalizada — ela multiplica velocidade.");
            }
        }

        /// <summary>
        /// A altura da célula é parâmetro porque, segundo o manual da Unity 6.4, ela <b>é</b> o
        /// <c>cellSize.y</c> do Grid: <i>"(1, 0.5, 1) ... simulates dimetric projection angles.
        /// True isometric projection instead uses a Y value of 0.57735."</i>
        /// </summary>
        [Test]
        public void AAlturaDaCelula_MudaAProjecao()
        {
            float dimetrico = AnguloDe(BaseIsometrica.ParaMundo(Vector2.right, 0.5f));
            float isometricoDeVerdade = AnguloDe(BaseIsometrica.ParaMundo(Vector2.right, 0.57735f));

            Assert.AreEqual(26.565f, dimetrico, 0.01f, "0,5 é dimétrico — o padrão do projeto.");
            Assert.AreEqual(30f, isometricoDeVerdade, 0.05f,
                "0,57735 é isométrico verdadeiro, e dá exatamente 30°.");
        }

        // ── O guarda de derivação: o código tem de concordar com o Grid das cenas ──

        /// <summary>
        /// A constante do movimento tem de bater com o <c>cellSize.y</c> do Grid de cada cena
        /// do build.
        ///
        /// <para>Sem isto, mexer no Grid faria o movimento divergir do mundo desenhado
        /// <b>em silêncio</b> — o personagem andaria num ângulo e o chão estaria pintado em
        /// outro. É a mesma classe de defeito que acabamos de consertar, só que na outra
        /// ponta.</para>
        /// </summary>
        [Test]
        public void AConstanteDoMovimento_ConcordaComOGridDasCenas()
        {
            var divergentes = new List<string>();

            foreach (var caminho in Directory.GetFiles("Assets/Scenes", "*.unity",
                                                       SearchOption.AllDirectories))
            {
                string yaml = File.ReadAllText(caminho);

                // Só Grids ISOMÉTRICOS importam: m_CellLayout 2 = Isometric, 3 = IsometricZAsY.
                foreach (Match m in Regex.Matches(yaml,
                             @"m_CellSize:\s*\{x:\s*([-\d.]+),\s*y:\s*([-\d.]+),[^}]*\}\s*\r?\n\s*m_CellGap:[^\n]*\r?\n\s*m_CellLayout:\s*(\d+)"))
                {
                    int layout = int.Parse(m.Groups[3].Value);
                    if (layout != 2 && layout != 3) continue;

                    float y = float.Parse(m.Groups[2].Value,
                                          System.Globalization.CultureInfo.InvariantCulture);

                    if (Mathf.Abs(y - BaseIsometrica.AlturaDeCelulaPadrao) > 0.001f)
                        divergentes.Add($"{Path.GetFileName(caminho)}: Grid isométrico com " +
                                        $"cellSize.y = {y}, movimento usa " +
                                        $"{BaseIsometrica.AlturaDeCelulaPadrao}");
                }
            }

            Assert.IsEmpty(divergentes,
                "Grid e movimento divergem:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", divergentes) + Environment.NewLine +
                "O manual da Unity 6.4 diz que o cellSize.y DEFINE a projeção simulada. Com os " +
                "dois em desacordo, o personagem anda num ângulo e o chão está pintado em outro.");
        }

        // ── O guarda de contrato: a direção não pode voltar a ser crua ──

        /// <summary>
        /// <c>LookDirection</c> é lida por quatro sistemas de geometria de mundo — o bucket de
        /// sprite, o cone de costas da Máscara Pálida, o Eco de Carcosa e a Pressão Psíquica.
        /// Se ela voltar a receber input cru, os quatro erram juntos e em silêncio.
        /// </summary>
        [Test]
        public void LookDirection_NaoVoltaAReceberInputCru()
        {
            string codigo = File.ReadAllText("Assets/Scripts/Player/PlayerMovement.cs");

            StringAssert.DoesNotContain("LookDirection = inputDirection", codigo,
                "LookDirection voltou a receber o input CRU. Ela é consumida como geometria de " +
                "MUNDO por AnimadorDoDamiao, ReiEmAmareloAI, EcoDeCarcosa e PressaoPsiquicaZone " +
                "— os quatro passariam a errar juntos, com desvio de até 63,4°.");

            StringAssert.Contains("LookDirection = direcaoNoMundo", codigo,
                "LookDirection precisa receber a direção convertida para o mundo.");
        }

        /// <summary>
        /// Duas implementações da base isométrica no mesmo arquivo foi exatamente como
        /// movimento e mira acabaram em espaços diferentes. Só pode haver uma.
        /// </summary>
        [Test]
        public void ExisteUmaSoImplementacaoDaBaseIsometrica()
        {
            var reimplementacoes = Directory
                .GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories)
                .Where(c => Path.GetFileName(c) != "BaseIsometrica.cs")
                .Where(c => Regex.IsMatch(File.ReadAllText(c),
                                          @"\.x\s*-\s*\w+\.y.*\r?\n.*\.x\s*\+\s*\w+\.y"))
                .Select(Path.GetFileName)
                .ToList();

            Assert.IsEmpty(reimplementacoes,
                "Alguém reimplementou a base isométrica em: " +
                string.Join(", ", reimplementacoes) + ". Use Core.Player.BaseIsometrica — duas " +
                "cópias divergem, e foi assim que este defeito nasceu.");
        }
    }
}
