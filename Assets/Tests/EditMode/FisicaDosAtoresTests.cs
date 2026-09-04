using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda o padrão de física de todo ator dinâmico.
    ///
    /// <para><b>Por que existe (2026-08-21):</b> o Vini relatou <i>"uma coisa muito estranha nos
    /// mobs e até no boss"</i> e apontou a causa: <b>freeze rotation</b>. A auditoria achou
    /// quatro corpos <c>Dynamic</c> sem <c>FreezeRotation</c> — <c>Byakhee.prefab</c> (o chefe),
    /// <c>Cortesao_Palido_0</c> e <c>_1</c> (os mobs do Castelo), e o Damião de uma cena de
    /// legado (<c>cena_1</c>, apagada em 2026-09-04). Casamento exato com o relato: os mobs e
    /// o boss.</para>
    ///
    /// <para><b>O que acontece sem isso:</b> corpo <c>Dynamic</c> que leva impulso fora do
    /// centro ganha velocidade angular; com <c>gravityScale 0</c> nada a zera depressa. O
    /// <c>transform</c> roda e o sprite gira junto — e num isométrico cuja profundidade é
    /// fingida por <c>sortingOrder</c>, personagem rodando destrói a ilusão. Gira o colisor
    /// também, mudando a pegada a cada quadro.</para>
    ///
    /// <para><b>A regra não estava na skill.</b> <c>favela-isometric-standards</c> exigia
    /// <c>gravityScale = 0</c>, câmera sem tilt, PPU 32 e Y-sorting, e <b>não dizia nada sobre
    /// rotação</b>. A lacuna estava no padrão escrito, não só nos prefabs — por isso este guarda
    /// existe além da correção.</para>
    /// </summary>
    public sealed class FisicaDosAtoresTests
    {
        private const string PastaDeArte = "Assets/FavelaAmarela/Art";

        /// <summary>
        /// <c>RigidbodyConstraints2D.FreezeRotation</c> vale 4. As de posição são 1 e 2, então
        /// o teste usa máscara de bit em vez de igualdade — travar posição junto é legítimo.
        /// </summary>
        private const int BitFreezeRotation = 4;

        /// <summary>
        /// Cenas que o guarda de física não cobre. <b>Vazia desde 2026-09-04</b>, quando a
        /// <c>cena_1</c> — a única entrada que havia, legado anterior à Tumba — foi apagada.
        ///
        /// <para>Fica declarada em vez de removida porque a lista é o lugar certo para uma
        /// exceção futura: apagá-la faria a próxima exceção nascer como um <c>if</c> solto no
        /// meio do laço, sem obrigação de justificar-se.</para>
        /// </summary>
        private static readonly string[] CenasIgnoradas = System.Array.Empty<string>();

        [Test]
        public void TodoCorpoDinamico_TravaARotacao()
        {
            var falhas = Auditar((nome, corpo) =>
            {
                var c = Regex.Match(corpo, @"m_Constraints:\s*(\d+)");
                int v = c.Success ? int.Parse(c.Groups[1].Value) : 0;

                return (v & BitFreezeRotation) != 0
                    ? null
                    : $"{nome}: sem FreezeRotation (constraints={v})";
            });

            Assert.IsEmpty(falhas,
                "Corpos dinâmicos que vão girar ao colidir:\n  " + string.Join("\n  ", falhas) +
                "\n\nConserto: 'Tools/FavelaAmarela/Física: padronizar os atores'.");
        }

        /// <summary>
        /// <c>CLAUDE.md</c> §5 manda <c>Continuous</c> para atores que se movem. No mesmo
        /// levantamento de 2026-08-21, <b>sete dos nove</b> estavam em <c>Discrete</c> —
        /// inclusive o Damião. Discrete deixa ator rápido atravessar parede fina entre dois
        /// <c>FixedUpdate</c>, que é como o Byakhee em mergulho escapa da arena.
        /// </summary>
        [Test]
        public void TodoCorpoDinamico_UsaDeteccaoContinua()
        {
            var falhas = Auditar((nome, corpo) =>
            {
                var d = Regex.Match(corpo, @"m_CollisionDetection:\s*(\d+)");
                int v = d.Success ? int.Parse(d.Groups[1].Value) : 0;

                return v == 1 ? null : $"{nome}: detecção Discrete";
            });

            Assert.IsEmpty(falhas,
                "Atores com detecção de colisão Discrete (CLAUDE.md §5 pede Continuous):\n  " +
                string.Join("\n  ", falhas));
        }

        [Test]
        public void NenhumAtor_TemGravidade()
        {
            var falhas = Auditar((nome, corpo) =>
            {
                var g = Regex.Match(corpo, @"m_GravityScale:\s*([\d.eE+-]+)");
                float v = g.Success ? float.Parse(g.Groups[1].Value, CultureInfo.InvariantCulture) : 0f;

                return System.Math.Abs(v) < 0.0001f
                    ? null
                    : $"{nome}: gravityScale={v} (a skill exige 0)";
            });

            Assert.IsEmpty(falhas,
                "Gravidade em ator isométrico quebra linha de visão e movimento " +
                "(favela-isometric-standards, mandato 1):\n  " + string.Join("\n  ", falhas));
        }

        /// <summary>
        /// Varre prefabs e cenas, aplica <paramref name="checar"/> em cada Rigidbody2D
        /// <b>dinâmico</b> e devolve as mensagens não-nulas.
        /// </summary>
        private static List<string> Auditar(System.Func<string, string, string> checar)
        {
            var falhas = new List<string>();

            var arquivos = Directory.GetFiles(PastaDeArte, "*.prefab", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles("Assets/Scenes", "*.unity")
                                 .Where(c => !CenasIgnoradas.Contains(Path.GetFileName(c))));

            foreach (var arquivo in arquivos)
            {
                string yaml = File.ReadAllText(arquivo);

                foreach (Match corpo in Regex.Matches(
                             yaml, @"---\s*!u!50\s*&-?\d+\r?\n(?:(?!^---)[\s\S])*",
                             RegexOptions.Multiline))
                {
                    var b = Regex.Match(corpo.Value, @"m_BodyType:\s*(\d+)");
                    int tipo = b.Success ? int.Parse(b.Groups[1].Value) : 0;
                    if (tipo != 0) continue;   // só Dynamic recebe impulso

                    string erro = checar(Path.GetFileName(arquivo), corpo.Value);
                    if (erro != null) falhas.Add(erro);
                }
            }

            return falhas;
        }
    }
}
