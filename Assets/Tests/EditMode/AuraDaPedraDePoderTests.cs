using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a aura da <c>PedraDePoder</c> — a sinalização de que aquela Pedra ainda segura o
    /// Escudo Mágico do Abdul.
    ///
    /// <para><b>Por que isto é mecânica, e não enfeite.</b> A Pedra sustenta o Escudo na
    /// Fase 1, e quebrá-la é a <b>única</b> forma de causar dano naquela fase — é o que, nas
    /// palavras do doc da própria <c>PedraDePoder</c>, <i>"transforma a Fase 1 numa luta de
    /// arena (procurar e quebrar) em vez de bater no escudo"</i>. Até 2026-09-03 a Pedra era uma
    /// imagem parada: nada na tela dizia <b>quais</b> pedras ainda estavam de pé, e a Fase 1
    /// inteira dependia disso.</para>
    ///
    /// <para><b>O defeito que este teste pega</b> é o mesmo que já mordeu nos altares: o
    /// componente existe, o prefab abre sem erro, e o <c>GetComponent</c>/<c>Awake</c> nunca
    /// encontra nada porque a peça caiu no GameObject errado — ou com o array vazio. Nada disso
    /// aparece na compilação nem no console.</para>
    /// </summary>
    public sealed class AuraDaPedraDePoderTests
    {
        private const string Prefab = "Assets/FavelaAmarela/Art/Enemies/PedraDePoder.prefab";
        private const string Pedra = "PedraDePoder";
        private const string Animador = "AnimadorDaPedraDePoder";

        /// <summary>12 quadros na banda roxa da folha 397. Piso, não número exato.</summary>
        private const int QuadrosMinimos = 8;

        private sealed class Bloco
        {
            public string Tipo;
            public string Corpo;
            public string GameObject;
            public string Classe;
        }

        private static List<Bloco> Blocos()
        {
            Assert.IsTrue(File.Exists(Prefab), $"Prefab ausente: {Prefab}");

            // Quebra pelo separador de documento ANTES de olhar o conteúdo: um regex de arquivo
            // inteiro atravessa o "--- " e mistura campos de objetos diferentes.
            var saida = new List<Bloco>();

            foreach (var bruto in Regex.Split(File.ReadAllText(Prefab), @"(?m)^--- ").Skip(1))
            {
                var cabecalho = Regex.Match(bruto, @"^!u!(\d+) &\d+");
                if (!cabecalho.Success) continue;

                var go = Regex.Match(bruto, @"(?m)^  m_GameObject: \{fileID: (\d+)\}$");
                var classe = Regex.Match(bruto, @"(?m)^  m_EditorClassIdentifier: (.*)$");

                saida.Add(new Bloco
                {
                    Tipo = cabecalho.Groups[1].Value,
                    Corpo = bruto,
                    GameObject = go.Success ? go.Groups[1].Value : null,
                    Classe = classe.Success
                        ? classe.Groups[1].Value.Trim().Split('.').Last()
                        : "",
                });
            }

            return saida;
        }

        [Test]
        public void OAnimador_EstaNoMesmoObjetoDaPedra_ComQuadros()
        {
            var blocos = Blocos();

            var daPedra = blocos.FirstOrDefault(b => b.Classe == Pedra);
            Assert.IsNotNull(daPedra,
                $"Não achei o componente {Pedra} em {Prefab}. Este teste não está medindo o " +
                "prefab — conserte a varredura antes de confiar no verde.");

            var aura = blocos.FirstOrDefault(b => b.Classe == Animador);
            Assert.IsNotNull(aura,
                $"O {Prefab} está sem {Animador}. A Pedra volta a ser uma imagem parada, e " +
                "nada na tela diz quais pedras ainda seguram o Escudo Mágico da Fase 1.");

            Assert.AreEqual(daPedra.GameObject, aura.GameObject,
                $"O {Animador} está no GameObject {aura.GameObject} e a {Pedra} no " +
                $"{daPedra.GameObject}. O componente existe, o prefab abre sem erro, e a aura " +
                "nunca gira — foi exatamente assim que os três altares do Castelo nasceram " +
                "com o feixe pendurado no objeto errado.");

            int quadros = Regex.Matches(aura.Corpo,
                @"(?m)^  - \{fileID: \d+, guid: [0-9a-f]{32}, type: 3\}$").Count;

            Assert.GreaterOrEqual(quadros, QuadrosMinimos,
                $"O {Animador} tem {quadros} quadro(s), abaixo do piso de {QuadrosMinimos}. " +
                "Com o array vazio o Update sai na primeira linha e a aura fica congelada no " +
                "primeiro quadro — indistinguível de não ter aura.");
        }

        [Test]
        public void OSpriteRenderer_NasceNoPrimeiroQuadroDaAura()
        {
            var blocos = Blocos();

            var daPedra = blocos.FirstOrDefault(b => b.Classe == Pedra);
            Assert.IsNotNull(daPedra, $"Sem {Pedra} em {Prefab}.");

            var renderer = blocos.FirstOrDefault(
                b => b.Tipo == "212" && b.GameObject == daPedra.GameObject);

            Assert.IsNotNull(renderer,
                $"O GameObject da {Pedra} está sem SpriteRenderer — não há onde a aura desenhar.");

            var sprite = Regex.Match(renderer.Corpo,
                @"(?m)^  m_Sprite: \{fileID: \d+, guid: ([0-9a-f]{32}), type: 3\}$");

            Assert.IsTrue(sprite.Success,
                "O SpriteRenderer da Pedra de Poder está sem sprite. Em jogo isso é um objeto " +
                "invisível que ainda leva golpe — o mesmo defeito dos Cortesãos do Castelo.");

            // O sprite de partida tem de ser um dos quadros da aura, e não o cristal solto:
            // senão a Pedra nasce sem anel e só ganha um no primeiro tique do animador.
            var quadros = new List<string>();

            foreach (var caminho in Directory.EnumerateFiles(
                         "Assets/FavelaAmarela/Art/Enemies/PedraDePoder", "*.png.meta"))
            {
                var g = Regex.Match(File.ReadAllText(caminho), @"(?m)^guid: ([0-9a-f]{32})");
                if (g.Success) quadros.Add(g.Groups[1].Value);
            }

            Assert.IsNotEmpty(quadros,
                "Não achei nenhum quadro de aura em Art/Enemies/PedraDePoder — a pasta sumiu " +
                "ou este teste está olhando para o lugar errado.");

            Assert.Contains(sprite.Groups[1].Value, quadros,
                "O sprite de partida da Pedra de Poder não é um quadro da aura. Ela nasceria " +
                "como o cristal solto e só ganharia o anel no primeiro tique do animador.");
        }
    }
}
