using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Inventario;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Monta a <b>tela de inventário</b> — mochila em grade + slots do
    /// corpo em coluna — e a liga ao <see cref="PainelDeInventario"/>.
    ///
    /// <para>Até 2026-08-11 o jogo só tinha a barra de 8 posições; os slots de equipamento
    /// não tinham interface nenhuma. Esta tela abre com <b>Tab</b> ou <b>I</b>.</para>
    ///
    /// <para>Idempotente: refaz do zero a cada execução, para ajuste de layout valer sem
    /// sobrar slot velho.</para>
    /// </summary>
    public static class MontarPainelDeInventario
    {
        private static readonly string[] Cenas =
        {
            "Assets/Scenes/Deserto_Hali.unity",
            "Assets/Scenes/Playtest_RuinasPalidas.unity",
            "Assets/Scenes/Santuario_Yhtill.unity",
        };

        private const int SlotsDaMochila = MainInventory.DefaultCapacidadeSurvivalHorror; // 12
        private const int SlotsDoCorpo = 7;                                               // anatomia (com Mão Secundária)
        private const int ColunasDaMochila = 4;

        [MenuItem("Tools/FavelaAmarela/Montar painel de inventário (Tab)")]
        public static void Executar()
        {
            var cenaAtiva = EditorSceneManager.GetActiveScene();
            if (cenaAtiva.isDirty && !string.IsNullOrEmpty(cenaAtiva.path))
                EditorSceneManager.SaveScene(cenaAtiva);

            string cenaOriginal = cenaAtiva.path;
            int feitas = 0;

            foreach (var caminho in Cenas)
            {
                if (!System.IO.File.Exists(caminho)) continue;

                var cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);
                if (Montar())
                {
                    EditorSceneManager.MarkSceneDirty(cena);
                    EditorSceneManager.SaveScene(cena);
                    feitas++;
                    Debug.Log($"[PainelDeInventario] Montado em '{cena.name}'.");
                }
            }

            if (!string.IsNullOrEmpty(cenaOriginal))
                EditorSceneManager.OpenScene(cenaOriginal, OpenSceneMode.Single);

            Debug.Log($"[PainelDeInventario] Pronto — {feitas} cena(s). Abre com Tab ou I.");
        }

        private static bool Montar()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                Debug.LogWarning("[PainelDeInventario] Sem Canvas nesta cena — pulada.");
                return false;
            }

            var antigo = GameObject.Find("PainelDeInventario");
            if (antigo != null) Object.DestroyImmediate(antigo);

            // Raiz do controlador: fica SEMPRE ativa, senão o Update que lê a tecla não roda.
            // Quem liga/desliga é o filho "Janela".
            var raiz = new GameObject("PainelDeInventario");
            raiz.transform.SetParent(canvas.transform, false);
            var comp = raiz.AddComponent<PainelDeInventario>();

            var janela = MontarJanela(raiz.transform);

            var mochila = new SlotRefs[SlotsDaMochila];
            var corpo = new SlotRefs[SlotsDoCorpo];

            var areaMochila = MontarArea(janela.transform, "Mochila", "MOCHILA",
                xMin: 0.06f, xMax: 0.56f);
            for (int i = 0; i < SlotsDaMochila; i++)
                mochila[i] = MontarSlot(areaMochila, $"Slot_{i}", i, ColunasDaMochila, SlotsDaMochila, comRotulo: false);

            var areaCorpo = MontarArea(janela.transform, "Corpo", "CORPO",
                xMin: 0.62f, xMax: 0.94f);
            for (int i = 0; i < SlotsDoCorpo; i++)
                corpo[i] = MontarSlot(areaCorpo, $"Corpo_{i}", i, 1, SlotsDoCorpo, comRotulo: true);

            Ligar(comp, janela, mochila, corpo);
            return true;
        }

        private struct SlotRefs
        {
            public CanvasGroup Grupo;
            public Image Icone;
            public Text Quantidade;
            public Text Rotulo;
        }

        private static GameObject MontarJanela(Transform pai)
        {
            var janela = new GameObject("Janela", typeof(Image));
            janela.transform.SetParent(pai, false);

            var rt = janela.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Véu escuro sobre o mundo: a tela pausa o jogo, e o escurecido comunica isso.
            var fundo = janela.GetComponent<Image>();
            fundo.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            fundo.type = Image.Type.Sliced;
            fundo.color = new Color(0.02f, 0.02f, 0.015f, 0.88f);

            MontarTexto(janela.transform, "Titulo", "INVENTÁRIO",
                new Vector2(0.06f, 0.88f), new Vector2(0.94f, 0.96f), 20, TextAnchor.MiddleLeft,
                new Color(0.92f, 0.86f, 0.55f, 0.9f));

            MontarTexto(janela.transform, "Dica", "Tab / I para fechar",
                new Vector2(0.06f, 0.03f), new Vector2(0.94f, 0.09f), 12, TextAnchor.MiddleRight,
                new Color(0.85f, 0.82f, 0.65f, 0.45f));

            janela.SetActive(false);
            return janela;
        }

        private static Transform MontarArea(Transform pai, string nome, string titulo, float xMin, float xMax)
        {
            var area = new GameObject(nome);
            area.transform.SetParent(pai, false);

            var rt = area.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(xMin, 0.12f);
            rt.anchorMax = new Vector2(xMax, 0.84f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            MontarTexto(area.transform, "Rotulo", titulo,
                new Vector2(0f, 0.93f), new Vector2(1f, 1f), 13, TextAnchor.MiddleLeft,
                new Color(0.85f, 0.80f, 0.60f, 0.7f));

            return area.transform;
        }

        private static SlotRefs MontarSlot(Transform pai, string nome, int indice, int colunas,
            int total, bool comRotulo)
        {
            int linhas = Mathf.CeilToInt(total / (float)colunas);
            int coluna = indice % colunas;
            int linha = indice / colunas;

            var go = new GameObject(nome, typeof(CanvasGroup), typeof(Image));
            go.transform.SetParent(pai, false);

            const float folga = 0.012f;
            float larguraCel = 1f / colunas;
            float alturaCel = 0.9f / linhas;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(coluna * larguraCel + folga,
                                       0.9f - (linha + 1) * alturaCel + folga);
            rt.anchorMax = new Vector2((coluna + 1) * larguraCel - folga,
                                       0.9f - linha * alturaCel - folga);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var moldura = go.GetComponent<Image>();
            moldura.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            moldura.type = Image.Type.Sliced;
            moldura.color = new Color(0.85f, 0.80f, 0.60f, 0.16f);
            moldura.raycastTarget = false;

            var refs = new SlotRefs { Grupo = go.GetComponent<CanvasGroup>() };

            var goIcone = new GameObject("Icone", typeof(Image));
            goIcone.transform.SetParent(go.transform, false);
            var rtIcone = goIcone.GetComponent<RectTransform>();
            rtIcone.anchorMin = new Vector2(0.16f, 0.16f);
            rtIcone.anchorMax = new Vector2(0.84f, 0.84f);
            rtIcone.offsetMin = Vector2.zero;
            rtIcone.offsetMax = Vector2.zero;
            refs.Icone = goIcone.GetComponent<Image>();
            refs.Icone.raycastTarget = false;
            refs.Icone.enabled = false;

            refs.Quantidade = MontarTexto(go.transform, "Quantidade", "",
                new Vector2(0.45f, 0.02f), new Vector2(0.96f, 0.4f), 12, TextAnchor.LowerRight,
                new Color(0.95f, 0.92f, 0.75f, 0.9f));

            if (comRotulo)
            {
                refs.Rotulo = MontarTexto(go.transform, "Rotulo", "",
                    new Vector2(0.04f, 0.6f), new Vector2(0.96f, 0.98f), 10, TextAnchor.UpperLeft,
                    new Color(0.85f, 0.82f, 0.62f, 0.55f));
            }

            return refs;
        }

        private static Text MontarTexto(Transform pai, string nome, string conteudo,
            Vector2 ancoraMin, Vector2 ancoraMax, int tamanho, TextAnchor alinhamento, Color cor)
        {
            var go = new GameObject(nome, typeof(Text));
            go.transform.SetParent(pai, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = ancoraMin;
            rt.anchorMax = ancoraMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var texto = go.GetComponent<Text>();
            texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texto.text = conteudo;
            texto.fontSize = tamanho;
            texto.alignment = alinhamento;
            texto.color = cor;
            texto.raycastTarget = false;

            return texto;
        }

        private static void Ligar(PainelDeInventario comp, GameObject janela,
            SlotRefs[] mochila, SlotRefs[] corpo)
        {
            var so = new SerializedObject(comp);
            so.FindProperty("raizDoPainel").objectReferenceValue = janela;

            PreencherArray(so.FindProperty("slotsDaMochila"), mochila);
            PreencherArray(so.FindProperty("slotsDoCorpo"), corpo);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(comp);
        }

        private static void PreencherArray(SerializedProperty arr, SlotRefs[] refs)
        {
            arr.arraySize = refs.Length;

            for (int i = 0; i < refs.Length; i++)
            {
                var el = arr.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("grupo").objectReferenceValue = refs[i].Grupo;
                el.FindPropertyRelative("icone").objectReferenceValue = refs[i].Icone;
                el.FindPropertyRelative("quantidade").objectReferenceValue = refs[i].Quantidade;
                el.FindPropertyRelative("rotulo").objectReferenceValue = refs[i].Rotulo;
            }
        }
    }
}
