using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using FavelaAmarela.Core.Rendering;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// A configuração de câmera do jogo, num lugar só.
    ///
    /// <para><b>O defeito que isto encerra (2026-08-27).</b> Sete ferramentas de Editor montavam
    /// ou ajustavam a câmera, cada uma com o seu <c>orthographicSize</c> escrito à mão:
    /// <c>PadronizarCanvasDasCenas</c> em 4,21875 (5,625 nas arenas),
    /// <c>MontarPortoesDasRuinas</c> em 7, três outras em 6 e o <c>PrefabMigrationTool</c> em 8.
    /// A cena ficava com o valor de quem rodou por último — e os Portões estavam em <b>7</b>,
    /// que não é ampliação inteira de nada: 1080 ÷ (7 × 2 × 32) = <b>2,41×</b>. Pixel de arte com
    /// tamanhos diferentes na mesma tela é o "cintilar" que a pixel art tem quando se move.</para>
    ///
    /// <para><b>E havia um segundo tamanho.</b> O <c>IsometricCameraController</c> guarda uma
    /// cópia própria de <c>orthographicSize</c> e a reimpõe no <c>Awake</c>. Mexer só na
    /// <c>Camera</c> dá a impressão de resolver e volta ao antigo em Play. As duas são escritas
    /// aqui, juntas, sempre.</para>
    ///
    /// <para><b>PixelPerfectCamera.</b> O pacote <c>com.unity.2d.pixel-perfect</c> já é
    /// dependência do projeto (vem no <c>com.unity.feature.2d</c>) e estava com <b>zero</b>
    /// componentes em cena. Ele é o mecanismo documentado: em vez de um tamanho ortográfico fixo
    /// que só é exato a 1080p, ele recebe a <b>resolução de referência</b> e recalcula o tamanho
    /// a cada quadro para a tela que existir. Também alinha a posição da câmera à grade de
    /// pixels (<c>PixelSnap</c>), o que importa aqui porque o seguimento é por
    /// <c>SmoothDamp</c> e produz posição fracionária a cada quadro.</para>
    ///
    /// <para>A doc do pacote avisa que ele <i>"is not compatible with Unity's Scriptable Render
    /// Pipeline"</i>. Este projeto está no <b>Built-in</b> (<c>m_CustomRenderPipeline</c> em zero
    /// no <c>GraphicsSettings</c>), então é este o componente certo — a versão de URP seria a
    /// errada.</para>
    ///
    /// <para><b>Crop Frame fica desligado</b>, e é decisão: ligado, ele põe tarja preta para a
    /// tela bater exatamente com a referência. Desligado, a área visível cresce um pouco em
    /// telas que não são múltiplo exato, sem tarja. Para um jogo que ainda vai ser jogado em
    /// resolução desconhecida, ver um pouco mais de cenário é melhor que ver tarja.</para>
    /// </summary>
    public static class PadraoDeCamera
    {
        /// <summary>
        /// As arenas de chefe, onde o enquadramento é mais aberto (3× em vez de 4×). É lista
        /// escrita à mão de propósito: <b>qual cena é arena é decisão de design</b>, não algo
        /// derivável do conteúdo. O que a lista não decide é o número — esse vem de
        /// <see cref="EscalaDePixel"/>.
        /// </summary>
        private static readonly string[] Arenas =
        {
            "Portoes_Das_Ruinas.unity",
            "Cena_ArenaDeTestes.unity",
        };

        /// <summary>A ampliação que uma cena usa, pelo caminho do arquivo.</summary>
        public static int AmpliacaoDe(string caminhoDaCena)
        {
            foreach (var arena in Arenas)
                if (caminhoDaCena != null && caminhoDaCena.Replace('\\', '/').EndsWith(arena))
                    return EscalaDePixel.AmpliacaoDeArena;

            return EscalaDePixel.AmpliacaoPadrao;
        }

        /// <summary>
        /// Põe uma câmera no padrão do jogo e devolve o que mudou (vazio = já estava certa).
        /// </summary>
        /// <param name="cam">A câmera da cena.</param>
        /// <param name="ampliacao">Ampliação inteira; use <see cref="AmpliacaoDe"/>.</param>
        public static List<string> Aplicar(Camera cam, int ampliacao)
        {
            var notas = new List<string>();
            if (cam == null) return notas;

            // ── Projeção ──────────────────────────────────────────────────────
            // A versão anterior desta padronização PULAVA câmera não-ortográfica
            // ('if (!cam.orthographic) continue;'). Era uma guarda que impedia a ferramenta de
            // consertar exatamente o caso quebrado: a Cena_ArenaDeTestes estava em perspectiva,
            // e por isso nunca foi tocada por nenhuma rodada de padronização.
            if (!cam.orthographic)
            {
                cam.orthographic = true;
                notas.Add("projeção Perspective → Orthographic");
            }

            // ── Rotação ───────────────────────────────────────────────────────
            // Mandato da skill favela-isometric-standards: a sensação isométrica vem do
            // Y-sorting e do remapeamento de input, nunca de câmera inclinada.
            if (cam.transform.rotation != Quaternion.identity)
            {
                cam.transform.rotation = Quaternion.identity;
                notas.Add("rotação zerada");
            }

            // ── Tamanho ───────────────────────────────────────────────────────
            float alvo = EscalaDePixel.TamanhoOrtografico(ampliacao);

            if (!Mathf.Approximately(cam.orthographicSize, alvo))
            {
                notas.Add($"orthographicSize {cam.orthographicSize} → {alvo} ({ampliacao}×)");
                cam.orthographicSize = alvo;
            }

            // O controlador tem cópia própria e a reimpõe no Awake.
            var ctrl = cam.GetComponent<FavelaAmarela.CameraSystem.IsometricCameraController>();
            if (ctrl != null)
            {
                var so = new SerializedObject(ctrl);
                var prop = so.FindProperty("orthographicSize");

                if (prop != null && !Mathf.Approximately(prop.floatValue, alvo))
                {
                    notas.Add($"controlador {prop.floatValue} → {alvo}");
                    prop.floatValue = alvo;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                // ── Z ─────────────────────────────────────────────────────────
                // O controlador põe a câmera no zOffset dele assim que tem alvo, mas só em
                // Play. Autorado errado, o Scene View e o Game View fora de Play mostram o
                // plano dos sprites cortado pelo near clip -- a Arena estava em z = 0.
                var zOffset = so.FindProperty("zOffset");
                float z = zOffset != null ? zOffset.floatValue : -10f;

                if (!Mathf.Approximately(cam.transform.position.z, z))
                {
                    var p = cam.transform.position;
                    notas.Add($"z {p.z} → {z}");
                    cam.transform.position = new Vector3(p.x, p.y, z);
                }
            }

            // ── Pixel Perfect ─────────────────────────────────────────────────
            var ppc = cam.GetComponent<PixelPerfectCamera>();
            if (ppc == null)
            {
                ppc = cam.gameObject.AddComponent<PixelPerfectCamera>();
                notas.Add("PixelPerfectCamera criado");
            }

            int larguraRef = EscalaDePixel.LarguraDeReferencia(ampliacao);
            int alturaRef = EscalaDePixel.AlturaDeReferencia(ampliacao);

            if (ppc.assetsPPU != EscalaDePixel.PixelsPorUnidade)
            {
                notas.Add($"assetsPPU {ppc.assetsPPU} → {EscalaDePixel.PixelsPorUnidade}");
                ppc.assetsPPU = EscalaDePixel.PixelsPorUnidade;
            }

            if (ppc.refResolutionX != larguraRef || ppc.refResolutionY != alturaRef)
            {
                notas.Add($"referência {ppc.refResolutionX}×{ppc.refResolutionY} → " +
                          $"{larguraRef}×{alturaRef}");
                ppc.refResolutionX = larguraRef;
                ppc.refResolutionY = alturaRef;
            }

            // Upscale desligado é pré-requisito do Pixel Snapping (a doc diz que a opção só
            // aparece com ele desligado), e mantém a nitidez sem passar por textura intermediária.
            if (ppc.upscaleRT) { ppc.upscaleRT = false; notas.Add("upscaleRT → off"); }
            if (!ppc.pixelSnapping) { ppc.pixelSnapping = true; notas.Add("pixelSnapping → on"); }
            if (ppc.cropFrameX) { ppc.cropFrameX = false; notas.Add("cropFrameX → off"); }
            if (ppc.cropFrameY) { ppc.cropFrameY = false; notas.Add("cropFrameY → off"); }
            if (ppc.stretchFill) { ppc.stretchFill = false; notas.Add("stretchFill → off"); }

            if (notas.Count > 0) EditorUtility.SetDirty(cam);

            return notas;
        }
    }
}
