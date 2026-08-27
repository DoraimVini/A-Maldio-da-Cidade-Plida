using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a ordem em que o jogo é desenhado — <b>as duas metades dela</b>.
    ///
    /// <para><b>A correção de rumo (2026-08-27).</b> O plano da revisão de física trazia uma
    /// fase inteira para "decidir se troca a ordenação para Custom Axis", com prós, contras e
    /// uma recomendação de medir numa cena antes. A premissa estava <b>errada</b>: o
    /// <c>GraphicsSettings</c> já está em <c>m_TransparencySortMode: 3</c> (CustomAxis) com eixo
    /// <c>(0, 1, 0)</c> desde o commit <c>92410413</c>. O que estava em <c>Default</c> era o
    /// check-in inicial. A skill <c>favela-isometric-standards</c> ainda registrava a divergência
    /// como pendente, e foi de lá que a premissa veio.</para>
    ///
    /// <para><b>E os dois mecanismos não competem, ao contrário do que aquele plano dizia.</b> A
    /// doc de <c>Camera.transparencySortAxis</c> é explícita: o eixo é usado <i>"for sorting
    /// Renderer components when other, higher priority, criterias fail to distinguish the render
    /// order"</i>. Ou seja, <c>sortingLayer</c> e <c>sortingOrder</c> mandam; o eixo é o
    /// <b>desempate</b>.</para>
    ///
    /// <para><b>Por que o desempate importa muito aqui.</b> O <c>DynamicYSort</c> escreve
    /// <c>sortingOrder = round(−y × 10)</c>, um <c>int</c>: a resolução é <b>0,1 unidade</b>, ou
    /// 3,2 pixels a PPU 32. Dois atores a menos de 3 pixels de distância vertical recebem o
    /// <b>mesmo</b> <c>sortingOrder</c>. Sem o eixo, o desempate de uma câmera ortográfica é a
    /// distância em z — e todo sprite está em z ≈ 0, então a ordem entre eles fica arbitrária e
    /// pode <b>alternar de quadro em quadro</b>. É um dos "tudo parece meio fora".</para>
    ///
    /// <para>Ninguém vigiava isso: é um campo, num arquivo de ProjectSettings, que uma caixinha
    /// do Editor desmarca sem deixar rastro.</para>
    /// </summary>
    public sealed class OrdenacaoIsometricaTests
    {
        private const string Graficos = "ProjectSettings/GraphicsSettings.asset";

        /// <summary><c>TransparencySortMode.CustomAxis</c> na serialização.</summary>
        private const int CustomAxis = 3;

        /// <summary>Fator Y→<c>sortingOrder</c> do <c>LevelBlockoutGenerator</c>.</summary>
        private const float FatorDoGerador = 10f;

        [Test]
        public void OProjeto_OrdenaPeloEixoVertical()
        {
            Assert.IsTrue(File.Exists(Graficos), $"{Graficos} sumiu.");

            string yaml = File.ReadAllText(Graficos);

            var modo = Regex.Match(yaml, @"^\s*m_TransparencySortMode:\s*(\d+)\s*$",
                                   RegexOptions.Multiline);
            Assert.IsTrue(modo.Success, "m_TransparencySortMode não existe mais no arquivo.");

            Assert.AreEqual(CustomAxis.ToString(), modo.Groups[1].Value,
                "O Transparency Sort Mode saiu de Custom Axis. O manual da 6.4 prescreve " +
                "Custom Axis (0,1,0) para isométrico, e sem ele o desempate entre dois sprites " +
                "com o MESMO sortingOrder vira a distância em z — que é zero para todo mundo. " +
                "Resultado: a ordem entre eles alterna de quadro em quadro.");

            var eixo = Regex.Match(yaml,
                @"^\s*m_TransparencySortAxis:\s*\{x:\s*(\S+?),\s*y:\s*(\S+?),\s*z:\s*(\S+?)\}\s*$",
                RegexOptions.Multiline);
            Assert.IsTrue(eixo.Success, "m_TransparencySortAxis não existe mais no arquivo.");

            Assert.AreEqual("0", eixo.Groups[1].Value, "O eixo de ordenação ganhou X.");
            Assert.AreEqual("1", eixo.Groups[2].Value, "O eixo de ordenação perdeu o Y.");
            Assert.AreEqual("0", eixo.Groups[3].Value, "O eixo de ordenação ganhou Z.");
        }

        /// <summary>
        /// Uma câmera pode sobrescrever o modo do projeto. Se alguma o fizer para algo que não
        /// seja Custom Axis, aquela cena volta a ter ordem instável — e só aquela.
        /// </summary>
        [Test]
        public void NenhumaCamera_SobrescreveOModoDeOrdenacao()
        {
            var rebeldes = new List<string>();

            foreach (var caminho in Directory.GetFiles("Assets/Scenes", "*.unity",
                                                       SearchOption.AllDirectories).OrderBy(c => c))
            {
                foreach (Match m in Regex.Matches(File.ReadAllText(caminho),
                             @"^\s*m_TransparencySortMode:\s*(\d+)\s*$", RegexOptions.Multiline))
                {
                    if (m.Groups[1].Value != CustomAxis.ToString())
                        rebeldes.Add($"{Path.GetFileName(caminho)}: modo {m.Groups[1].Value}");
                }
            }

            Assert.IsEmpty(rebeldes,
                "Câmera(s) sobrescrevendo o modo de ordenação do projeto:" + Environment.NewLine +
                "  " + string.Join(Environment.NewLine + "  ", rebeldes));
        }

        /// <summary>
        /// O <c>DynamicYSort</c> e o <c>LevelBlockoutGenerator</c> precisam usar o <b>mesmo</b>
        /// fator. Se divergirem, o ator móvel e a parede estática passam a viver em escalas de
        /// <c>sortingOrder</c> diferentes, e o ator atravessa a parede visualmente sem que nada
        /// dê erro.
        /// </summary>
        [Test]
        public void TodoDynamicYSort_UsaOFatorDoGerador()
        {
            string gerador = File.ReadAllText(
                "Assets/FavelaAmarela/Level/Runtime/LevelBlockoutGenerator.cs");

            StringAssert.Contains($"-worldCenter.y * {FatorDoGerador}f", gerador,
                "O fator Y→sortingOrder da geometria estática mudou. Ele é o contrato que o " +
                "DynamicYSort de cada ator tem de casar.");

            var divergentes = new List<string>();
            var vistos = 0;

            foreach (var caminho in Arquivos())
            {
                string yaml = File.ReadAllText(caminho);

                // 'fator' é campo exclusivo do DynamicYSort — conferido: é o único script do
                // projeto com esse nome de campo serializado. Por isso a varredura não filtra
                // por m_EditorClassIdentifier antes: esse campo nem sempre é serializado, e
                // filtrar por ele faria o guarda PULAR arquivos em silêncio, que é o modo de
                // falha que este arquivo inteiro existe para não repetir.
                foreach (Match m in Regex.Matches(yaml, @"^\s*fator:\s*(\S+)\s*$",
                                                  RegexOptions.Multiline))
                {
                    vistos++;

                    if (!float.TryParse(m.Groups[1].Value,
                                        System.Globalization.NumberStyles.Float,
                                        System.Globalization.CultureInfo.InvariantCulture,
                                        out float f) ||
                        Math.Abs(f - FatorDoGerador) > 0.001f)
                    {
                        divergentes.Add($"{Path.GetFileName(caminho)}: fator {m.Groups[1].Value}");
                    }
                }
            }

            Assert.Greater(vistos, 0,
                "Nenhum DynamicYSort com campo 'fator' foi encontrado — este guarda parou de " +
                "olhar para o jogo (o componente foi renomeado?).");

            Assert.IsEmpty(divergentes,
                "DynamicYSort fora do fator da geometria estática:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", divergentes) + Environment.NewLine +
                $"Todos precisam ser {FatorDoGerador}: é a escala em que as paredes já foram " +
                "geradas. Fator diferente = ator e parede em escalas diferentes de " +
                "sortingOrder, e o ator aparece por cima de uma parede que está na frente dele.");
        }

        private static IEnumerable<string> Arquivos()
        {
            foreach (var c in Directory.GetFiles("Assets", "*.unity", SearchOption.AllDirectories))
                yield return c;

            foreach (var c in Directory.GetFiles("Assets", "*.prefab", SearchOption.AllDirectories))
                yield return c;
        }
    }
}
