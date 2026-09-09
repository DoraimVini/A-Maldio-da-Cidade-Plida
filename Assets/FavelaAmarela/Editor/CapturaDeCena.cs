using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Renderiza a câmera de cada cena para um PNG — para comparar <b>built-in contra URP</b>
    /// pixel a pixel.
    ///
    /// <para><b>Por que isto é necessário.</b> A suíte ficou verde sob a URP (1099 + 61) e o
    /// <c>orthographicSize</c> derivado continuou 4,21875. Isso prova que a <i>lógica</i>
    /// sobreviveu. Não prova que a <b>imagem</b> sobreviveu: o renderer 2D da URP tem outra
    /// cadeia de passes, e um shader escrito para o built-in pode compilar, ser "suportado", e
    /// mesmo assim compor diferente. "Verde" e "parece igual" são perguntas diferentes, e só a
    /// segunda decide se a migração é aceitável.</para>
    ///
    /// <para>Roda com <c>-ComGraficos</c>: sem gráficos não há o que capturar.</para>
    /// </summary>
    public static class CapturaDeCena
    {
        private const string Marcador = "[Captura]";

        private const int Largura = 640;
        private const int Altura = 360;

        [MenuItem("Tools/FavelaAmarela/Captura: renderizar as cenas para PNG")]
        public static void Executar()
        {
            string rotulo = GraphicsSettings.defaultRenderPipeline == null ? "builtin" : "urp";
            string pasta = Path.Combine(Directory.GetCurrentDirectory(), "Capturas", rotulo);
            Directory.CreateDirectory(pasta);

            int feitas = 0;

            foreach (var entrada in EditorBuildSettings.scenes)
            {
                if (!entrada.enabled || !File.Exists(entrada.path)) continue;

                Scene cena = EditorSceneManager.OpenScene(entrada.path, OpenSceneMode.Single);
                string nome = Path.GetFileNameWithoutExtension(entrada.path);

                var cam = cena.GetRootGameObjects()
                    .SelectMany(g => g.GetComponentsInChildren<Camera>(true))
                    .FirstOrDefault(c => c.orthographic);

                if (cam == null)
                {
                    Debug.Log($"{Marcador} {nome}: sem câmera ortográfica, pulado.");
                    continue;
                }

                var rt = new RenderTexture(Largura, Altura, 24, RenderTextureFormat.ARGB32);
                var anterior = cam.targetTexture;
                var ativoAntes = RenderTexture.active;

                try
                {
                    cam.targetTexture = rt;
                    cam.Render();

                    RenderTexture.active = rt;
                    var tex = new Texture2D(Largura, Altura, TextureFormat.RGBA32, false);
                    tex.ReadPixels(new Rect(0, 0, Largura, Altura), 0, 0);
                    tex.Apply();

                    File.WriteAllBytes(Path.Combine(pasta, nome + ".png"), tex.EncodeToPNG());
                    Object.DestroyImmediate(tex);

                    feitas++;
                    Debug.Log($"{Marcador} {nome} -> {rotulo}/{nome}.png " +
                              $"(orthographicSize {cam.orthographicSize})");
                }
                finally
                {
                    cam.targetTexture = anterior;
                    RenderTexture.active = ativoAntes;
                    rt.Release();
                    Object.DestroyImmediate(rt);
                }
            }

            Debug.Log($"{Marcador} {feitas} captura(s) em Capturas/{rotulo}/");
        }
    }
}
