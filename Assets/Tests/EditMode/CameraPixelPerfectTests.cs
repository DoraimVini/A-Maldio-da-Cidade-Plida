using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using FavelaAmarela.Core.Rendering;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda o enquadramento: projeção, zoom e a ampliação inteira do pixel.
    ///
    /// <para><b>O que a varredura de 2026-08-27 achou.</b> Sete ferramentas de Editor montavam
    /// ou ajustavam a câmera, cada uma com o seu <c>orthographicSize</c> escrito à mão — 4,21875,
    /// 5,625, 7, 6, 6, 6 e 8. A cena ficava com o valor de quem rodou por último:</para>
    ///
    /// <list type="bullet">
    ///   <item><b>Portões das Ruínas em 7</b>, que não é ampliação inteira de nada:
    ///   1080 ÷ (7 × 2 × 32) = <b>2,41×</b>. Pixel de arte com tamanhos diferentes na mesma tela
    ///   é o "cintilar" da pixel art em movimento.</item>
    ///
    ///   <item><b>Cena_ArenaDeTestes em PERSPECTIVA</b>, tamanho 5, em z = 0. E o padronizador
    ///   de cenas não a consertava porque começava com <c>if (!cam.orthographic) continue;</c> —
    ///   uma guarda que impedia a ferramenta de tocar exatamente no caso quebrado.</item>
    /// </list>
    ///
    /// <para><b>E havia dois tamanhos por cena.</b> O <c>IsometricCameraController</c> guarda uma
    /// cópia própria de <c>orthographicSize</c> e a reimpunha no <c>Awake</c>. Mexer só na
    /// <c>Camera</c> dava a impressão de resolver e voltava ao antigo em Play. Os dois números
    /// são conferidos aqui, separadamente.</para>
    /// </summary>
    public sealed class CameraPixelPerfectTests
    {
        private const string PastaDeCenas = "Assets/Scenes";

        // ── A conta, sem cena ─────────────────────────────────────────────────

        [Test]
        public void AConta_BateComOsValoresConhecidos()
        {
            Assert.AreEqual(270, EscalaDePixel.AlturaDeReferencia(4));
            Assert.AreEqual(480, EscalaDePixel.LarguraDeReferencia(4));
            Assert.AreEqual(4.21875f, EscalaDePixel.TamanhoOrtografico(4), 0.00001f,
                "4,21875 é o zoom padrão do jogo desde 2026-08-19; se a conta deixar de dar " +
                "isso, todas as cenas mudam de enquadramento de uma vez.");

            Assert.AreEqual(360, EscalaDePixel.AlturaDeReferencia(3));
            Assert.AreEqual(640, EscalaDePixel.LarguraDeReferencia(3));
            Assert.AreEqual(5.625f, EscalaDePixel.TamanhoOrtografico(3), 0.00001f);
        }

        [Test]
        public void OValorQueEstavaNosPortoes_NaoPreservaOPixel()
        {
            Assert.IsFalse(EscalaDePixel.PreservaOPixel(7f),
                "7 precisa continuar reprovando: é 2,41× e mistura pixels de 2 e de 3 na mesma " +
                "tela. Se este teste passar a aceitar, o detector de zoom quebrado morreu.");

            Assert.IsFalse(EscalaDePixel.PreservaOPixel(6f), "6 é 2,81×.");
            Assert.IsFalse(EscalaDePixel.PreservaOPixel(8f), "8 é 2,11×.");

            Assert.IsTrue(EscalaDePixel.PreservaOPixel(EscalaDePixel.TamanhoOrtografico(4)));
            Assert.IsTrue(EscalaDePixel.PreservaOPixel(EscalaDePixel.TamanhoOrtografico(3)));
        }

        // ── As cenas ──────────────────────────────────────────────────────────

        [Test]
        public void TodaCameraDeJogo_EhOrtografica()
        {
            var erradas = Cameras()
                .Where(c => !c.Ortografica)
                .Select(c => $"{c.Cena} · '{c.Nome}' está em PERSPECTIVA")
                .ToList();

            Assert.IsEmpty(erradas,
                "Câmera(s) de jogo fora de projeção ortográfica:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", erradas) + Environment.NewLine +
                "O IsometricCameraController força ortográfica no Awake, então em Play parece " +
                "certo — e o Scene View mostra outra coisa. Autorar errado e corrigir em " +
                "runtime é o pior dos dois mundos.");
        }

        [Test]
        public void TodaCameraDeJogo_TemAmpliacaoInteira()
        {
            var quebradas = new List<string>();

            foreach (var cam in Cameras())
            {
                if (!EscalaDePixel.PreservaOPixel(cam.Tamanho))
                    quebradas.Add($"{cam.Cena} · Camera = {cam.Tamanho} → " +
                                  $"{Ampliacao(cam.Tamanho):0.00}× (não inteira)");

                if (cam.TamanhoDoControlador.HasValue &&
                    !Mathf.Approximately(cam.TamanhoDoControlador.Value, cam.Tamanho))
                    quebradas.Add($"{cam.Cena} · a Camera diz {cam.Tamanho} e o " +
                                  $"IsometricCameraController diz {cam.TamanhoDoControlador} — " +
                                  "o do controlador é o que vale em Play");
            }

            Assert.IsEmpty(quebradas,
                "Enquadramento(s) fora do padrão:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", quebradas) + Environment.NewLine +
                "Ampliação não inteira mistura pixels de tamanhos diferentes e a arte cintila " +
                "ao mover. Conserto: 'Tools/FavelaAmarela/Padronizar Canvas e moldura do menu'.");
        }

        [Test]
        public void TodaCameraDeJogo_TemPixelPerfectCameraNaResolucaoCerta()
        {
            var faltando = new List<string>();

            foreach (var cam in Cameras())
            {
                if (!cam.TemPixelPerfect)
                {
                    faltando.Add($"{cam.Cena} · '{cam.Nome}' sem PixelPerfectCamera");
                    continue;
                }

                if (cam.Ppu != EscalaDePixel.PixelsPorUnidade)
                    faltando.Add($"{cam.Cena} · assetsPPU = {cam.Ppu}, mas a arte é " +
                                 $"{EscalaDePixel.PixelsPorUnidade}");

                float esperado = cam.AlturaDeReferencia / (2f * EscalaDePixel.PixelsPorUnidade);
                if (!Mathf.Approximately(esperado, cam.Tamanho))
                    faltando.Add($"{cam.Cena} · referência {cam.LarguraDeReferencia}×" +
                                 $"{cam.AlturaDeReferencia} pede ortográfico {esperado}, " +
                                 $"e a cena tem {cam.Tamanho}");
            }

            Assert.IsEmpty(faltando,
                "PixelPerfectCamera ausente ou desalinhado:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", faltando) + Environment.NewLine +
                "Sem ele, o zoom só é exato a 1080p — em qualquer outra resolução o pixel de " +
                "arte deixa de ser inteiro.");
        }

        [Test]
        public void NenhumaCameraDeJogo_TemRotacao()
        {
            var tortas = Cameras()
                .Where(c => !c.SemRotacao)
                .Select(c => $"{c.Cena} · '{c.Nome}'")
                .ToList();

            Assert.IsEmpty(tortas,
                "Câmera(s) com rotação:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", tortas) + Environment.NewLine +
                "Mandato da skill favela-isometric-standards: a sensação isométrica vem do " +
                "Y-sorting e do remapeamento de input, nunca de câmera inclinada. Inclinar " +
                "dessincroniza visualmente os colisores dos sprites.");
        }

        // ── Leitura do YAML ───────────────────────────────────────────────────

        private sealed class CameraDeCena
        {
            public string Cena;
            public string Nome;
            public bool Ortografica;
            public float Tamanho;
            public float? TamanhoDoControlador;
            public bool SemRotacao;
            public bool TemPixelPerfect;
            public int Ppu;
            public int LarguraDeReferencia;
            public int AlturaDeReferencia;
        }

        private static float Ampliacao(float tamanhoOrtografico)
            => tamanhoOrtografico <= 0f
                ? 0f
                : EscalaDePixel.AlturaDaTelaAlvo /
                  (tamanhoOrtografico * 2f * EscalaDePixel.PixelsPorUnidade);

        /// <summary>
        /// Só as câmeras de <b>jogo</b> — as que carregam o <c>IsometricCameraController</c>.
        /// A do menu não enquadra mundo nenhum e não entra neste padrão; é regra derivada, não
        /// uma lista de cenas a envelhecer.
        /// </summary>
        private static IEnumerable<CameraDeCena> Cameras()
        {
            string guidDoControlador = GuidDo("Assets/Scripts/Camera/CameraController.cs");

            foreach (var caminho in Directory.GetFiles(PastaDeCenas, "*.unity",
                                                       SearchOption.AllDirectories).OrderBy(c => c))
            {
                string yaml = File.ReadAllText(caminho);
                var docs = Documentos(yaml);

                foreach (var cam in docs.Where(d => d.Tipo == "Camera"))
                {
                    string dono = Referencia(cam.Corpo, "m_GameObject");
                    if (dono == null) continue;

                    var ctrl = docs.FirstOrDefault(
                        d => d.Tipo == "MonoBehaviour" &&
                             d.Corpo.Contains(guidDoControlador) &&
                             Referencia(d.Corpo, "m_GameObject") == dono);

                    if (ctrl == null) continue;   // câmera que não segue o Damião

                    var go = docs.FirstOrDefault(d => d.Tipo == "GameObject" && d.Id == dono);
                    var tr = docs.FirstOrDefault(d => d.Tipo == "Transform" &&
                                                      Referencia(d.Corpo, "m_GameObject") == dono);
                    // Identificado pelos CAMPOS, não pelo nome da classe: um componente de
                    // pacote nem sempre serializa m_EditorClassIdentifier, e o GUID do script
                    // muda com a versão do pacote. 'm_AssetsPPU' + 'm_RefResolutionX' só
                    // existem no PixelPerfectCamera.
                    var ppc = docs.FirstOrDefault(
                        d => d.Tipo == "MonoBehaviour" &&
                             d.Corpo.Contains("m_AssetsPPU:") &&
                             d.Corpo.Contains("m_RefResolutionX:") &&
                             Referencia(d.Corpo, "m_GameObject") == dono);

                    yield return new CameraDeCena
                    {
                        Cena = Path.GetFileName(caminho),
                        Nome = go == null ? "?" : Campo(go.Corpo, "m_Name", "?"),
                        Ortografica = Numero(cam.Corpo, "orthographic", 1) != 0,
                        Tamanho = Fracionario(cam.Corpo, "orthographic size", -1f),
                        TamanhoDoControlador = ctrl.Corpo.Contains("orthographicSize:")
                            ? Fracionario(ctrl.Corpo, "orthographicSize", -1f)
                            : (float?)null,
                        SemRotacao = tr == null || SemRotacaoNo(tr.Corpo),
                        TemPixelPerfect = ppc != null,
                        Ppu = ppc == null ? 0 : Numero(ppc.Corpo, "m_AssetsPPU", 0),
                        LarguraDeReferencia = ppc == null ? 0 : Numero(ppc.Corpo, "m_RefResolutionX", 0),
                        AlturaDeReferencia = ppc == null ? 0 : Numero(ppc.Corpo, "m_RefResolutionY", 0),
                    };
                }
            }
        }

        private static bool SemRotacaoNo(string transform)
        {
            var m = Regex.Match(transform,
                @"m_LocalRotation:\s*\{x:\s*(\S+?),\s*y:\s*(\S+?),\s*z:\s*(\S+?),\s*w:\s*(\S+?)\}");
            if (!m.Success) return true;

            return Aprox(m.Groups[1].Value, 0f) && Aprox(m.Groups[2].Value, 0f) &&
                   Aprox(m.Groups[3].Value, 0f) && Aprox(m.Groups[4].Value, 1f);
        }

        private static bool Aprox(string texto, float alvo)
            => float.TryParse(texto, System.Globalization.NumberStyles.Float,
                              System.Globalization.CultureInfo.InvariantCulture, out float v)
               && Mathf.Abs(v - alvo) < 0.0001f;

        private static string GuidDo(string script)
        {
            string meta = script + ".meta";
            Assert.IsTrue(File.Exists(meta), $"Meta ausente: {meta}");

            var m = Regex.Match(File.ReadAllText(meta), @"guid:\s*(\w+)");
            Assert.IsTrue(m.Success, $"Sem guid em {meta}");
            return m.Groups[1].Value;
        }

        private sealed class Documento
        {
            public string Tipo;
            public string Id;
            public string Corpo;
        }

        private static List<Documento> Documentos(string yaml)
        {
            var docs = new List<Documento>();
            var marcadores = Regex.Matches(yaml, @"^--- !u!\d+ &(-?\d+).*$", RegexOptions.Multiline);

            for (int i = 0; i < marcadores.Count; i++)
            {
                int inicio = marcadores[i].Index + marcadores[i].Length;
                int fim = i + 1 < marcadores.Count ? marcadores[i + 1].Index : yaml.Length;

                string corpo = yaml.Substring(inicio, fim - inicio);
                var tipo = Regex.Match(corpo, @"^(\w+):\s*$", RegexOptions.Multiline);

                docs.Add(new Documento
                {
                    Tipo = tipo.Success ? tipo.Groups[1].Value : "?",
                    Id = marcadores[i].Groups[1].Value,
                    Corpo = corpo,
                });
            }

            return docs;
        }

        private static string Referencia(string corpo, string campo)
        {
            var m = Regex.Match(corpo, Regex.Escape(campo) + @":\s*\{fileID:\s*(-?\d+)\}");
            return m.Success ? m.Groups[1].Value : null;
        }

        private static string Campo(string corpo, string campo, string padrao)
        {
            var m = Regex.Match(corpo, @"^\s*" + Regex.Escape(campo) + @":\s*(.*)$",
                                RegexOptions.Multiline);
            return m.Success ? m.Groups[1].Value.Trim() : padrao;
        }

        private static int Numero(string corpo, string campo, int padrao)
        {
            var m = Regex.Match(corpo, @"^\s*" + Regex.Escape(campo) + @":\s*(-?\d+)\s*$",
                                RegexOptions.Multiline);
            return m.Success && int.TryParse(m.Groups[1].Value, out int v) ? v : padrao;
        }

        private static float Fracionario(string corpo, string campo, float padrao)
        {
            var m = Regex.Match(corpo, @"^\s*" + Regex.Escape(campo) + @":\s*(-?[\d.eE+-]+)\s*$",
                                RegexOptions.Multiline);

            return m.Success && float.TryParse(m.Groups[1].Value,
                                               System.Globalization.NumberStyles.Float,
                                               System.Globalization.CultureInfo.InvariantCulture,
                                               out float v)
                ? v
                : padrao;
        }
    }
}
