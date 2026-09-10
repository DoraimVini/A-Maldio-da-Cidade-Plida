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
    ///
    /// <para><b>Segundo defeito, mesma folha (2026-09-10, mais tarde):</b> com todos olhando para
    /// a direita, o corpo ainda <i>saltava de lado</i> — o torso pulava até 22 px (1,7 un à
    /// escala 2,47) entre quadros consecutivos, porque cada quadro foi gerado sem âncora comum
    /// e o espelhamento ainda inverteu o deslocamento dos ímpares. Cada quadro foi alinhado
    /// pelo torso (os 35 % de baixo do corpo escuro) ao centro da célula, onde está o pivô.
    /// <see cref="OTorso_NaoSaltaEntreQuadrosConsecutivos"/> guarda isso.</para>
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

        /// <summary>Salto do torso entre dois quadros seguidos que ainda se lê como o mesmo bicho parado.</summary>
        private const float SaltoMaximoDoTorsoPx = 4f;

        [Test]
        public void OTorso_NaoSaltaEntreQuadrosConsecutivos()
        {
            var torsos = MedirTorsos();
            Assert.IsNotEmpty(torsos, "Nenhum quadro com corpo medível.");

            var porCiclo = new Dictionary<string, List<KeyValuePair<string, float>>>();
            foreach (var kv in torsos)
            {
                string ciclo = kv.Key.Substring(0, kv.Key.LastIndexOf('_'));
                if (!porCiclo.TryGetValue(ciclo, out var lista)) porCiclo[ciclo] = lista = new List<KeyValuePair<string, float>>();
                lista.Add(kv);
            }

            var saltos = new List<string>();
            foreach (var ciclo in porCiclo)
            {
                var q = ciclo.Value.OrderBy(kv => kv.Key).ToList();
                for (int i = 0; i < q.Count; i++)
                {
                    var a = q[i]; var b = q[(i + 1) % q.Count];
                    float salto = Mathf.Abs(a.Value - b.Value);
                    if (salto > SaltoMaximoDoTorsoPx) saltos.Add($"{a.Key}→{b.Key}: {salto:F0} px");
                }
            }

            TestContext.WriteLine(string.Join("\n", torsos.Select(t => $"{t.Key}: torso x = {t.Value:F1}")));

            Assert.IsEmpty(saltos,
                "O torso do Byakhee salta de lado entre quadros seguidos: " + string.Join(", ", saltos) +
                $". Acima de {SaltoMaximoDoTorsoPx} px (à escala 2,47 cada px vale 0,077 un) o " +
                "jogador vê o corpo tremer para os lados a 8 qps — é o 'oscila lateralmente' de " +
                "2026-09-10. Realinhe os quadros pelo torso (ver PROCEDENCIA_Byakhee.txt).");
        }

        /// <summary>Centroide x do torso (35 % de baixo do corpo escuro) de cada sprite, por nome.</summary>
        private static Dictionary<string, float> MedirTorsos()
        {
            var tex = CarregarFolha(out var px, out int largura, out var sprites);
            var torsos = new Dictionary<string, float>();

            foreach (var s in sprites)
            {
                var r = s.rect;
                int yMin = int.MaxValue, yMax = int.MinValue;
                var corpo = new List<Vector2Int>();

                for (int y = (int)r.yMin; y < (int)r.yMax; y++)
                for (int x = (int)r.xMin; x < (int)r.xMax; x++)
                {
                    var c = px[y * largura + x];
                    if (c.a > 40 && (c.r + c.g + c.b) / 3 < 90)
                    {
                        corpo.Add(new Vector2Int(x, y));
                        if (y < yMin) yMin = y;
                        if (y > yMax) yMax = y;
                    }
                }

                if (corpo.Count == 0) continue;

                // (0,0) em baixo: o torso são os 35 % de baixo do corpo, logo os y MENORES.
                float teto = yMin + (yMax - yMin) * 0.35f;
                float soma = 0f; int n = 0;
                foreach (var p in corpo) if (p.y <= teto) { soma += p.x - r.xMin; n++; }
                if (n > 0) torsos[s.name] = soma / n;
            }

            Object.DestroyImmediate(tex);
            return torsos;
        }

        private static Texture2D CarregarFolha(out Color32[] px, out int largura, out List<Sprite> sprites)
        {
            Assert.IsTrue(File.Exists(Folha), $"Folha ausente: {Folha}");

            // Lê o PNG cru: independe de Read/Write Enabled no importer, e (0,0) fica em baixo,
            // como nos rects dos sprites.
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(ImageConversion.LoadImage(tex, File.ReadAllBytes(Folha)),
                "O PNG da folha não carregou.");
            px = tex.GetPixels32();
            largura = tex.width;

            sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(Folha)
                                   .OfType<Sprite>()
                                   .OrderBy(s => s.name)
                                   .ToList();
            Assert.IsNotEmpty(sprites, "A folha não está fatiada em sprites.");
            return tex;
        }

        private static List<Lado> MedirLados()
        {
            var tex = CarregarFolha(out var px, out int largura, out var sprites);

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
