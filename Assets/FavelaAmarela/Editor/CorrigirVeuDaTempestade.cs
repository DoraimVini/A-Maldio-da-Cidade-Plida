using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Troca o véu da tempestade de <b>retângulo chapado</b> para
    /// <b>vinheta radial</b>.
    ///
    /// <para><b>Problema (relatado pelo Vini):</b> o véu era uma <c>Image</c> de cor sólida
    /// esticada na tela inteira. Ao subir a intensidade, ele tingia o mapa todo por igual —
    /// lia-se como "filtro por cima do jogo", não como tempestade. E escondia justamente o
    /// que o jogador precisa ver: o chão à sua volta.</para>
    ///
    /// <para><b>A vinheta resolve os dois:</b> fecha nas bordas e mantém o centro limpo, que
    /// é o que "visibilidade reduzida" significa de fato — você enxerga perto de você e
    /// perde o horizonte. Casa com a tabela de visibilidade do design (§3), em que o valor
    /// é sempre "quanto do redor dá para ver".</para>
    ///
    /// <para>Só troca o sprite e o alpha máximo; o <c>TempestadeVisualOverlay</c> continua
    /// dirigindo o alpha pela intensidade, sem mudança de código.</para>
    /// </summary>
    public static class CorrigirVeuDaTempestade
    {
        // Correção de rumo (2026-08-02): a vinheta veio de uma leitura errada minha do
        // pedido do Vini. Ele relatava que a tempestade NÃO cobria o mapa todo; eu entendi
        // como pedido para não cobrir. A areia cobre a tela inteira, com variação de
        // textura em vez de variação de cobertura.
        private const string SpriteVinheta = "Assets/FavelaAmarela/Art/UI/Areia_Tempestade.png";

        private static readonly string[] Cenas =
        {
            "Assets/Scenes/Deserto_Hali.unity",
            "Assets/Scenes/Tumba_De_Alhazred.unity",
            "Assets/Scenes/Santuario_Yhtill.unity",
        };

        /// <summary>
        /// Alpha no pico da tempestade. Mais alto que o antigo 0,5 porque agora a opacidade
        /// se concentra nas bordas — no centro continua transparente.
        /// </summary>
        private const float AlphaMaximo = 0.7f;

        [MenuItem("Tools/FavelaAmarela/Corrigir veu da tempestade")]
        public static void Executar()
        {
            var vinheta = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteVinheta);
            if (vinheta == null)
            {
                Debug.LogError($"[Véu] Sprite da vinheta não encontrado em {SpriteVinheta}.");
                return;
            }

            var cenaAtiva = EditorSceneManager.GetActiveScene();
            if (cenaAtiva.isDirty && !string.IsNullOrEmpty(cenaAtiva.path))
                EditorSceneManager.SaveScene(cenaAtiva);

            string cenaOriginal = cenaAtiva.path;
            int corrigidos = 0;

            foreach (var caminho in Cenas)
            {
                if (!System.IO.File.Exists(caminho)) continue;

                var cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);
                if (Corrigir(vinheta))
                {
                    EditorSceneManager.MarkSceneDirty(cena);
                    EditorSceneManager.SaveScene(cena);
                    corrigidos++;
                    Debug.Log($"[Véu] Vinheta aplicada em '{cena.name}'.");
                }
            }

            if (!string.IsNullOrEmpty(cenaOriginal))
                EditorSceneManager.OpenScene(cenaOriginal, OpenSceneMode.Single);

            Debug.Log($"[Véu] Pronto — {corrigidos} cena(s) com véu em vinheta.");
        }

        private static bool Corrigir(Sprite vinheta)
        {
            var overlay = Object.FindAnyObjectByType<TempestadeVisualOverlay>(FindObjectsInactive.Include);
            if (overlay == null) return false;

            var so = new SerializedObject(overlay);
            var veu = so.FindProperty("veu").objectReferenceValue as Image;
            if (veu == null)
            {
                Debug.LogWarning("[Véu] Overlay sem Image atribuída — nada a corrigir.", overlay);
                return false;
            }

            Undo.RecordObject(veu, "Trocar véu por vinheta");
            veu.sprite = vinheta;
            veu.type = Image.Type.Simple;

            // preserveAspect FALSE de propósito: a vinheta precisa esticar até as bordas em
            // qualquer proporção de tela. Preservando o aspecto, sobrariam cantos sem véu
            // numa tela widescreen — exatamente onde a tempestade deveria estar mais densa.
            veu.preserveAspect = false;

            var cor = veu.color;
            veu.color = new Color(cor.r, cor.g, cor.b, cor.a);  // alpha segue vindo da intensidade
            EditorUtility.SetDirty(veu);

            so.FindProperty("alphaMaximo").floatValue = AlphaMaximo;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(overlay);

            return true;
        }
    }
}
