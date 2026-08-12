using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using FavelaAmarela.CameraSystem;
using FavelaAmarela.Runtime.GameLoop;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Cria a <b>Cena_ArenaDeTestes</b>: chão neutro, <c>GameManager</c>,
    /// câmera isométrica, Damião e HUD completo — uma cena mínima e genérica onde qualquer
    /// chefe pode ser invocado via <see cref="CarcosaDebuggerWindow"/> e testado antes de a
    /// fase real existir.
    ///
    /// <para><b>Por que existe separada das fases:</b> decisão explícita do Vini — testar
    /// lutas não deveria depender de level design real (Castelo, Trono de Aldebaran) ainda não
    /// construído. Esta cena fica vazia de conteúdo de fase de propósito; quem povoa é o
    /// Debugger, em Play Mode, sob demanda.</para>
    ///
    /// <para><b>Nunca entra no Build Settings</b> — é ferramenta de desenvolvimento, não
    /// conteúdo de jogo. Diferente de <c>MontarCenaDeMenu</c>, esta ferramenta
    /// deliberadamente NÃO chama <c>EditorBuildSettings.scenes</c>.</para>
    ///
    /// <para>Idempotente: refaz a cena do zero a cada execução.</para>
    /// </summary>
    public static class MontarArenaDeTestes
    {
        private const string CaminhoDaCena = "Assets/Scenes/Cena_ArenaDeTestes.unity";
        private const string PrefabDamiao = "Assets/FavelaAmarela/Art/Characters/Damiao/Player_Damiao.prefab";

        private const float RaioDoChao = 12f;

        [MenuItem("Tools/FavelaAmarela/Montar Arena de Testes")]
        public static void Executar()
        {
            var atual = EditorSceneManager.GetActiveScene();
            if (atual.isDirty && !string.IsNullOrEmpty(atual.path))
                EditorSceneManager.SaveScene(atual);

            var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Montar();

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena, CaminhoDaCena);

            Debug.Log($"[ArenaDeTestes] Pronto — '{CaminhoDaCena}' montada. NÃO foi adicionada " +
                      "ao Build Settings (é ferramenta de dev). Dê Play e abra " +
                      "'Tools/FavelaAmarela/Carcosa Debugger' para invocar um chefe.");
        }

        private static void Montar()
        {
            MontarChao();

            new GameObject("GameManager", typeof(GameManager));

            var damiao = InstanciarDamiao();
            MontarCamera(damiao);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            BuildHUDCompleto.Build();
            MontarBarraDeArtefatos();
        }

        // ── Chão ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Quadrado cinza neutro cobrindo a arena — placeholder até haver arte de fase. Não é
        /// nível de verdade, só espaço para o chefe e o jogador se moverem.
        /// </summary>
        private static void MontarChao()
        {
            var go = new GameObject("Chao_Placeholder", typeof(SpriteRenderer));
            go.transform.position = Vector3.zero;
            go.transform.localScale = new Vector3(RaioDoChao * 2f, RaioDoChao * 2f, 1f);

            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = CriarSpriteSolido();
            sr.color = new Color(0.22f, 0.21f, 0.20f);
            sr.sortingOrder = -1000;
        }

        private static Sprite CriarSpriteSolido()
        {
            var tex = new Texture2D(32, 32) { filterMode = FilterMode.Point };
            var pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
        }

        // ── Damião e câmera ──────────────────────────────────────────────────

        private static GameObject InstanciarDamiao()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDamiao);
            if (prefab == null)
            {
                Debug.LogError($"[ArenaDeTestes] Prefab do Damião não encontrado em '{PrefabDamiao}'.");
                return null;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = "Player_Damiao";
            go.transform.position = Vector3.zero;
            return go;
        }

        private static void MontarCamera(GameObject damiao)
        {
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(IsometricCameraController));
            camGo.tag = "MainCamera";
            camGo.transform.rotation = Quaternion.identity; // sem tilt — ver favela-isometric-standards

            if (damiao != null)
                camGo.GetComponent<IsometricCameraController>().SetTarget(damiao.transform);
        }

        // ── Barra de Artefatos ───────────────────────────────────────────────

        /// <summary>
        /// Versão mínima de 4 slots (só texto, sem ícone/recarga visual) — o suficiente para
        /// o HUDController ter algo para injetar via <c>InjetarArtefatos</c> e o jogador ver
        /// que artefato está em qual tecla durante o teste. Sub-campos de ícone e recarga
        /// ficam nulos de propósito: <c>BarraDeArtefatos.Redesenhar/Update</c> já checam null
        /// em cada um.
        /// </summary>
        private static void MontarBarraDeArtefatos()
        {
            var hud = Object.FindAnyObjectByType<HUDController>();
            if (hud == null)
            {
                Debug.LogWarning("[ArenaDeTestes] Sem HUDController — barra de artefatos não montada.");
                return;
            }

            if (hud.GetComponentInChildren<BarraDeArtefatos>(true) != null) return;

            var raiz = new GameObject("Barra_Artefatos", typeof(RectTransform));
            raiz.transform.SetParent(hud.transform, false);

            var rt = raiz.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(16f, 16f);
            rt.sizeDelta = new Vector2(220f, 44f);

            var fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var slots = new BarraDeArtefatos.SlotDeArtefato[4];

            for (int i = 0; i < slots.Length; i++)
            {
                var slotGo = new GameObject($"Slot_F{i + 1}", typeof(RectTransform), typeof(CanvasGroup));
                slotGo.transform.SetParent(raiz.transform, false);

                var slotRt = slotGo.GetComponent<RectTransform>();
                slotRt.anchorMin = slotRt.anchorMax = new Vector2(0f, 0f);
                slotRt.pivot = new Vector2(0f, 0f);
                slotRt.anchoredPosition = new Vector2(i * 56f, 0f);
                slotRt.sizeDelta = new Vector2(52f, 44f);

                var texto = new GameObject("Texto", typeof(RectTransform)).AddComponent<Text>();
                texto.transform.SetParent(slotGo.transform, false);
                texto.font = fonte;
                texto.fontSize = 11;
                texto.alignment = TextAnchor.MiddleCenter;
                texto.color = new Color(0.93f, 0.90f, 0.75f);
                texto.horizontalOverflow = HorizontalWrapMode.Wrap;
                texto.text = "—";
                var textoRt = texto.GetComponent<RectTransform>();
                textoRt.anchorMin = Vector2.zero;
                textoRt.anchorMax = Vector2.one;
                textoRt.offsetMin = Vector2.zero;
                textoRt.offsetMax = Vector2.zero;

                slots[i] = new BarraDeArtefatos.SlotDeArtefato
                {
                    grupo = slotGo.GetComponent<CanvasGroup>(),
                    nomeDaHabilidade = texto,
                    rotuloTecla = null,
                    icone = null,
                    preenchimentoRecarga = null,
                };
            }

            var barra = raiz.AddComponent<BarraDeArtefatos>();
            var so = new SerializedObject(barra);
            var slotsProp = so.FindProperty("slots");
            slotsProp.arraySize = slots.Length;
            for (int i = 0; i < slots.Length; i++)
            {
                var elemento = slotsProp.GetArrayElementAtIndex(i);
                elemento.FindPropertyRelative("grupo").objectReferenceValue = slots[i].grupo;
                elemento.FindPropertyRelative("nomeDaHabilidade").objectReferenceValue = slots[i].nomeDaHabilidade;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            var hudSo = new SerializedObject(hud);
            hudSo.FindProperty("barraDeArtefatos").objectReferenceValue = barra;
            hudSo.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
