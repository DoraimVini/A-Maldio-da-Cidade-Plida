using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Utilitário de Editor: monta a tela de Colapso (Game Over diegético) na cena ativa
    /// e fia o <c>SequenciaDeColapso</c> + o hook no <c>GameManager</c>. Cria um painel
    /// escuro full-screen com um Text sob o Canvas "ScreenFader", um GameObject com o
    /// componente de sequência, e liga: sprite do Damião, material de dissolução
    /// (OcclusionDither), CanvasGroup do painel, Text. Idempotente (reusa se já existir).
    /// </summary>
    public static class BuildColapsoScreen
    {
        private const string MatPath = "Assets/FavelaAmarela/Art/Materials/OcclusionDither.mat";

        [MenuItem("Tools/FavelaAmarela/Build Colapso Screen")]
        public static void Build()
        {
            var canvas = GameObject.Find("ScreenFader");
            var player = GameObject.Find("Player_Damiao");
            var gm = GameObject.Find("GameManager");
            if (canvas == null || player == null || gm == null)
            {
                Debug.LogError("[Colapso] Falta ScreenFader, Player_Damiao ou GameManager na cena.");
                return;
            }

            // 1. Painel escuro full-stretch + CanvasGroup.
            var painel = Achar(canvas.transform, "PainelColapso")
                         ?? Novo("PainelColapso", canvas.transform, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            EsticarTotal(painel.GetComponent<RectTransform>());
            var img = painel.GetComponent<Image>();
            img.color = new Color(0.02f, 0.02f, 0.0f, 0.93f);
            var cg = painel.GetComponent<CanvasGroup>();
            cg.alpha = 0f;

            // 2. Text da frase.
            var textoGO = Achar(painel.transform, "TextoColapso")
                          ?? Novo("TextoColapso", painel.transform, typeof(RectTransform), typeof(Text));
            var trt = textoGO.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.08f, 0.38f);
            trt.anchorMax = new Vector2(0.92f, 0.62f);
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            var text = textoGO.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.83f, 0.79f, 0.3f); // amarelo pálido
            text.fontSize = 30;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = "Colapso Mental";

            painel.SetActive(false); // começa oculto

            // 3. Material de dissolução.
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (mat == null) { Debug.LogError($"[Colapso] Material não encontrado em {MatPath}."); return; }

            // 4. GameObject + componente da sequência.
            var seqGO = GameObject.Find("SequenciaColapso") ?? new GameObject("SequenciaColapso");
            var tipoSeq = ResolverTipo("FavelaAmarela.Runtime.GameLoop.SequenciaDeColapso");
            if (tipoSeq == null) { Debug.LogError("[Colapso] Tipo SequenciaDeColapso não encontrado (recompile?)."); return; }
            var seq = seqGO.GetComponent(tipoSeq) ?? seqGO.AddComponent(tipoSeq);

            // 5. Fiação via SerializedObject.
            var soSeq = new SerializedObject(seq);
            soSeq.FindProperty("damiaoSprite").objectReferenceValue = player.GetComponent<SpriteRenderer>();
            soSeq.FindProperty("materialDissolucao").objectReferenceValue = mat;
            soSeq.FindProperty("painelColapso").objectReferenceValue = cg;
            soSeq.FindProperty("textoColapso").objectReferenceValue = text;
            soSeq.ApplyModifiedPropertiesWithoutUndo();

            // 6. Hook no GameManager.
            foreach (var comp in gm.GetComponents<Component>())
            {
                if (comp != null && comp.GetType().Name == "GameManager")
                {
                    var soGm = new SerializedObject(comp);
                    var prop = soGm.FindProperty("sequenciaColapso");
                    if (prop != null) { prop.objectReferenceValue = seq; soGm.ApplyModifiedPropertiesWithoutUndo(); }
                    break;
                }
            }

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Colapso] Tela de Colapso montada e fiada (painel + SequenciaDeColapso + GameManager).");
        }

        private static GameObject Achar(Transform pai, string nome)
        {
            var t = pai.Find(nome);
            return t != null ? t.gameObject : null;
        }

        private static GameObject Novo(string nome, Transform pai, params Type[] comps)
        {
            var go = new GameObject(nome, comps);
            go.transform.SetParent(pai, false);
            return go;
        }

        private static void EsticarTotal(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        private static Type ResolverTipo(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullName);
                if (t != null) return t;
            }
            return null;
        }
    }
}
