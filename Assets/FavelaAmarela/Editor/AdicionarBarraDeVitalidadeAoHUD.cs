using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Põe a <b>barra de Vitalidade dentro do prefab do HUD</b>, e
    /// remove a cópia solta que existia só na cena da Tumba.
    ///
    /// <para><b>O bug (relatado pelo Vini):</b> a barra de vida não aparecia no Deserto. O
    /// prefab <c>HUD_ResilienciaBar</c> só continha a barra de Resiliência — a de Vitalidade
    /// era um objeto <b>montado à mão na cena da Tumba</b>. Toda cena nova nascia sem ela.</para>
    ///
    /// <para><b>Por que no prefab e não em cada cena:</b> montar HUD por cena é exatamente o
    /// que causou a divergência. Com a barra no prefab, qualquer cena que instancie o HUD
    /// ganha as duas barras, e uma melhoria futura chega às três cenas sozinha.</para>
    ///
    /// <para>O nome do prefab (<c>HUD_ResilienciaBar</c>) ficou impreciso — agora ele traz o
    /// HUD inteiro. Não renomeei para não quebrar as referências já gravadas nas cenas.</para>
    /// </summary>
    public static class AdicionarBarraDeVitalidadeAoHUD
    {
        private const string PrefabHUD = "Assets/FavelaAmarela/Art/UI/HUD_ResilienciaBar.prefab";
        private const string CenaTumba = "Assets/Scenes/Tumba_De_Alhazred.unity";
        private const string NomeDaBarra = "Barra_Vitalidade";

        [MenuItem("Tools/FavelaAmarela/Adicionar barra de Vitalidade ao HUD")]
        public static void Executar()
        {
            if (!AdicionarAoPrefab()) return;
            RemoverCopiaDaTumba();

            Debug.Log("[Vitalidade] Barra agora vive no prefab do HUD — todas as cenas a recebem.");
        }

        private static bool AdicionarAoPrefab()
        {
            var raiz = PrefabUtility.LoadPrefabContents(PrefabHUD);
            if (raiz == null)
            {
                Debug.LogError($"[Vitalidade] Prefab não encontrado em {PrefabHUD}.");
                return false;
            }

            try
            {
                if (raiz.GetComponentInChildren<VitalidadeBar>(true) != null)
                {
                    Debug.Log("[Vitalidade] O prefab já tem a barra — nada a fazer.");
                    return true;
                }

                var hud = raiz.GetComponentInChildren<HUDController>(true);
                if (hud == null)
                {
                    Debug.LogError("[Vitalidade] Prefab sem HUDController.");
                    return false;
                }

                var barra = Montar(raiz.transform);

                var so = new SerializedObject(hud);
                so.FindProperty("vitalidadeBar").objectReferenceValue = barra;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(raiz, PrefabHUD);
                AssetDatabase.SaveAssets();

                Debug.Log("[Vitalidade] Barra adicionada ao prefab e ligada no HUDController.");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        /// <summary>
        /// Monta trilho + preenchimento no canto superior esquerdo, logo abaixo da barra de
        /// Resiliência (que ancora em 0,1). Duas barras empilhadas: a mente em cima, a carne
        /// embaixo — a ordem em que o jogo as apresenta na ficha.
        /// </summary>
        private static VitalidadeBar Montar(Transform raiz)
        {
            var go = new GameObject(NomeDaBarra, typeof(RectTransform));
            go.transform.SetParent(raiz, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(24f, -58f);   // abaixo da barra de Resiliência
            rt.sizeDelta = new Vector2(200f, 18f);

            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            var trilho = new GameObject("Trilho", typeof(Image));
            trilho.transform.SetParent(go.transform, false);
            EsticarNoPai(trilho.GetComponent<RectTransform>());
            var imgTrilho = trilho.GetComponent<Image>();
            imgTrilho.sprite = sprite;
            imgTrilho.type = Image.Type.Sliced;
            imgTrilho.color = new Color(0.10f, 0.08f, 0.06f, 0.75f);
            imgTrilho.raycastTarget = false;

            var preench = new GameObject("Preenchimento", typeof(Image));
            preench.transform.SetParent(go.transform, false);
            EsticarNoPai(preench.GetComponent<RectTransform>());
            var imgFill = preench.GetComponent<Image>();
            imgFill.sprite = sprite;

            // Filled + Horizontal: é o que o VitalidadeBar dirige por fillAmount. Sem sprite
            // atribuído, fillAmount é ignorado pela Unity e a barra nunca se move.
            imgFill.type = Image.Type.Filled;
            imgFill.fillMethod = Image.FillMethod.Horizontal;
            imgFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            imgFill.fillAmount = 1f;
            imgFill.color = new Color(0.72f, 0.18f, 0.18f);   // vermelho-carne
            imgFill.raycastTarget = false;

            var barra = go.AddComponent<VitalidadeBar>();
            var so = new SerializedObject(barra);
            so.FindProperty("fillImage").objectReferenceValue = imgFill;
            so.FindProperty("backgroundImage").objectReferenceValue = imgTrilho;
            so.ApplyModifiedPropertiesWithoutUndo();

            return barra;
        }

        private static void EsticarNoPai(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Tira a barra montada à mão na Tumba. Com a do prefab presente, as duas
        /// apareceriam empilhadas — e a override da cena venceria, mantendo a divergência
        /// que esta correção existe para acabar.
        /// </summary>
        private static void RemoverCopiaDaTumba()
        {
            var cenaAtiva = EditorSceneManager.GetActiveScene();
            if (cenaAtiva.isDirty && !string.IsNullOrEmpty(cenaAtiva.path))
                EditorSceneManager.SaveScene(cenaAtiva);

            string cenaOriginal = cenaAtiva.path;
            var cena = EditorSceneManager.OpenScene(CenaTumba, OpenSceneMode.Single);

            int removidas = 0;
            foreach (var barra in Object.FindObjectsByType<VitalidadeBar>(FindObjectsInactive.Include))
            {
                // A do prefab fica; só a solta na cena sai.
                if (PrefabUtility.IsPartOfPrefabInstance(barra)) continue;

                Object.DestroyImmediate(barra.gameObject);
                removidas++;
            }

            if (removidas > 0)
            {
                EditorSceneManager.MarkSceneDirty(cena);
                EditorSceneManager.SaveScene(cena);
                Debug.Log($"[Vitalidade] {removidas} barra(s) solta(s) removida(s) da Tumba.");
            }

            if (!string.IsNullOrEmpty(cenaOriginal) && cenaOriginal != CenaTumba)
                EditorSceneManager.OpenScene(cenaOriginal, OpenSceneMode.Single);
        }
    }
}
