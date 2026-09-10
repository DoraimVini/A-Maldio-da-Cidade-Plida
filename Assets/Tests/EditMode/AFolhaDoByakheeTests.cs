using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// A folha do Byakhee olha para <b>um lado só</b>.
    ///
    /// <para><b>O defeito (2026-09-10).</b> O Vini: <i>"a sprite da Byakhee está virando de um
    /// lado para outro"</i>. Nenhum código virava o sprite — nem <c>flipX</c>, nem escala, nem
    /// rotação (medido no Editor vivo por 600 quadros). Era a <b>folha</b>: gerada em pares, cada
    /// pose uma vez para cada lado, e fatiada como sequência. Espreita D-E-D-E, rasante
    /// D-E-D-E-D-E. A 8 quadros por segundo o bicho virava 4 vezes por segundo.</para>
    ///
    /// <para><b>Como se mede o lado</b>, sem olho humano: o olho do bicho é a única cor
    /// (252, 0, 0) da folha, e a cabeça avança no sentido do olhar. Se o olho está à direita do
    /// centroide do corpo escuro, o quadro olha para a direita. Quadros frontais (grito de boca
    /// aberta) ficam perto de zero e não contam; quadros sem olho visível (garras_3, derrota
    /// 2 e 3) não têm veredito e não contam — o guarda é sobre o que dá para medir.</para>
    ///
    /// <para>Os 12 quadros virados foram espelhados <b>na própria folha</b> em 2026-09-10
    /// (registro em <c>PROCEDENCIA_Byakhee.txt</c>). Se alguém reimportar a folha original, este
    /// teste reprova antes de o jogador ver.</para>
    /// </summary>
    public sealed class AFolhaDoByakheeTests
    {
        private const string Folha = "Assets/FavelaAmarela/Art/Enemies/Byakhee_Spritesheet.png";

        /// <summary>Abaixo disto o quadro é frontal e não tem lado.</summary>
        private const float MargemFrontal = 5f;

        /// <summary>Menos pixels de olho que isto é ruído (uma faísca cor de olho).</summary>
        private const int MinimoDePixelsDeOlho = 4;

        private readonly struct Lado
        {
            public readonly string Quadro;
            public readonly float OlhoMenosCorpo;
            public Lado(string quadro, float d) { Quadro = quadro; OlhoMenosCorpo = d; }
        }

        [Test]
        public void TodoQuadroComOlhoVisivel_OlhaParaADireita()
        {
            var lados = MedirLados();
            Assert.IsNotEmpty(lados, "Nenhum quadro com olho medível — a cor do olho mudou?");

            var virados = lados.Where(l => l.OlhoMenosCorpo < -MargemFrontal).ToList();

            TestContext.WriteLine(string.Join("\n", lados.Select(l =>
                $"{l.Quadro}: olho - corpo = {l.OlhoMenosCorpo:+0;-0} px")));

            Assert.IsEmpty(virados,
                "Quadro(s) olhando para a ESQUERDA na folha do Byakhee: " +
                string.Join(", ", virados.Select(v => v.Quadro)) + ". A folha inteira tem de " +
                "olhar para a direita — quem vira é o flipX do AnimadorDoByakhee, pela " +
                "velocidade. Com lados misturados o bicho pisca de lado a cada quadro.");
        }

        [Test]
        public void OsCiclosDeVoo_TemOlhoMedivelEmTodoQuadro()
        {
            // Espreita e rasante são o que se vê 90% da luta; se a medição deles falhar, o
            // teste de cima passa vazio e não guarda nada.
            var lados = MedirLados().Select(l => l.Quadro).ToList();

            foreach (string nome in new[] { "espreita_0", "espreita_1", "espreita_2", "espreita_3",
                                            "rasante_0", "rasante_1", "rasante_2", "rasante_3",
                                            "rasante_4", "rasante_5" })
                Assert.Contains("byakhee_" + nome, lados,
                    $"byakhee_{nome} ficou sem olho medível — a prova de lado não cobre mais o voo.");
        }

        private static List<Lado> MedirLados()
        {
            Assert.IsTrue(File.Exists(Folha), $"Folha ausente: {Folha}");

            // Lê o PNG cru: independe de Read/Write Enabled no importer, e (0,0) fica em baixo,
            // como nos rects dos sprites.
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(ImageConversion.LoadImage(tex, File.ReadAllBytes(Folha)),
                "O PNG da folha não carregou.");
            var px = tex.GetPixels32();
            int largura = tex.width;

            var sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(Folha)
                                       .OfType<Sprite>()
                                       .OrderBy(s => s.name)
                                       .ToList();
            Assert.IsNotEmpty(sprites, "A folha não está fatiada em sprites.");

            var lados = new List<Lado>();
            foreach (var s in sprites)
            {
                var r = s.rect;
                float somaOlhoX = 0f, somaCorpoX = 0f;
                int olho = 0, corpo = 0;

                for (int y = (int)r.yMin; y < (int)r.yMax; y++)
                for (int x = (int)r.xMin; x < (int)r.xMax; x++)
                {
                    var c = px[y * largura + x];
                    if (c.a > 200 && c.r == 252 && c.g == 0 && c.b == 0) { somaOlhoX += x; olho++; }
                    if (c.a > 40 && (c.r + c.g + c.b) / 3 < 90) { somaCorpoX += x; corpo++; }
                }

                if (olho < MinimoDePixelsDeOlho || corpo == 0) continue;
                lados.Add(new Lado(s.name, somaOlhoX / olho - somaCorpoX / corpo));
            }

            Object.DestroyImmediate(tex);
            return lados;
        }
    }
}
