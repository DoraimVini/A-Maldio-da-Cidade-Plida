using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Runtime.Environment;
using FavelaAmarela.Runtime.Rendering;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Monta o que falta no Deserto de Hali:
    /// <list type="number">
    ///   <item><b>Tempestade de Memória</b> — o driver (<see cref="TempestadeAmbiente"/>) e o
    ///   véu visual (<see cref="TempestadeVisualOverlay"/>). A infra existia desde a demo das
    ///   Ruínas mas <b>nunca tinha sido instalada no Deserto</b>, então a tempestade
    ///   simplesmente não acontecia lá.</item>
    ///   <item><b>Arte das entradas</b> — liga cada sprite de diorama à localização
    ///   correspondente, que já existia na cena como objeto com <c>SpriteRenderer</c> vazio.</item>
    /// </list>
    ///
    /// <para>Idempotente: reaproveita o que já existir e nunca sobrescreve sprite já atribuído.</para>
    /// </summary>
    public static class MontarDesertoDeHali
    {
        private const string CenaDeserto = "Assets/Scenes/Deserto_Hali.unity";
        private const string PastaEntradas = "Assets/FavelaAmarela/Art/Entradas";

        /// <summary>Localização na cena → sprite da entrada correspondente.</summary>
        private static readonly (string objeto, string sprite)[] Entradas =
        {
            ("Entrada_TumbaAlhazred",  "Entrada_TumbaDeAlhazred"),
            ("Santuario_Yhtill",       "Entrada_SantuarioDeYhtill"),
            ("Lago_De_Hali",           "Entrada_LagoNegroDeHali"),
            ("Entrada_TemploSerpente", "Entrada_TemploDoPovoSerpente"),
            // O objeto de cena chama-se "DasRuinas" e a arte "DeCarcosa" — mesmo lugar,
            // nomes divergentes. Ver systems/entradas_do_deserto.md.
            ("Portoes_DasRuinas",      "Entrada_PortoesDeCarcosa"),
        };

        // Sem parênteses no caminho: o ExecuteMenuItem não os resolve de forma confiável.
        [MenuItem("Tools/FavelaAmarela/Montar Deserto de Hali")]
        public static void Executar()
        {
            // Salva sem perguntar. `SaveCurrentModifiedScenesIfUserWantsTo` abre um
            // diálogo MODAL, e uma ferramenta disparada pela ponte MCP trava a Unity
            // inteira esperando um clique que ninguém vê.
            var cenaAtiva = EditorSceneManager.GetActiveScene();
            if (cenaAtiva.isDirty && !string.IsNullOrEmpty(cenaAtiva.path))
                EditorSceneManager.SaveScene(cenaAtiva);

            string cenaOriginal = cenaAtiva.path;
            var cena = EditorSceneManager.OpenScene(CenaDeserto, OpenSceneMode.Single);

            InstalarTempestade();
            LigarSpritesDasEntradas();

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);

            if (!string.IsNullOrEmpty(cenaOriginal) && cenaOriginal != CenaDeserto)
                EditorSceneManager.OpenScene(cenaOriginal, OpenSceneMode.Single);

            Debug.Log("[MontarDeserto] Pronto — Deserto de Hali salvo.");
        }

        private static void InstalarTempestade()
        {
            // 1. Driver: o GameManager o encontra no bootstrap e injeta o EnvironmentState.
            var driver = Object.FindAnyObjectByType<TempestadeAmbiente>(FindObjectsInactive.Include);
            if (driver == null)
            {
                var go = new GameObject("TempestadeDeMemoria");
                Undo.RegisterCreatedObjectUndo(go, "Criar Tempestade");
                driver = go.AddComponent<TempestadeAmbiente>();
                Debug.Log("[MontarDeserto] TempestadeAmbiente criada (faixa padrão do componente).", go);
            }

            // 2. Véu visual. Sem ele a tempestade existe mas é invisível — e a mecânica de
            // stealth invertido depende de o jogador PERCEBER a rajada para aproveitá-la.
            var overlay = Object.FindAnyObjectByType<TempestadeVisualOverlay>(FindObjectsInactive.Include);
            if (overlay != null) return;

            var canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                var goCanvas = new GameObject("Canvas_Deserto",
                    typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Undo.RegisterCreatedObjectUndo(goCanvas, "Criar Canvas do Deserto");
                canvas = goCanvas.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                Debug.Log("[MontarDeserto] Canvas criado (o Deserto não tinha nenhum).", goCanvas);
            }

            var goVeu = new GameObject("Veu_Tempestade", typeof(Image));
            Undo.RegisterCreatedObjectUndo(goVeu, "Criar véu da tempestade");
            goVeu.transform.SetParent(canvas.transform, false);

            var rt = goVeu.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;      // full-stretch: cobre a tela inteira
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = goVeu.GetComponent<Image>();
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.color = new Color(0.85f, 0.72f, 0.42f, 0f); // areia; alpha vem da intensidade
            img.raycastTarget = false;                       // não pode capturar cliques

            var goOverlay = new GameObject("TempestadeVisualOverlay");
            Undo.RegisterCreatedObjectUndo(goOverlay, "Criar overlay da tempestade");
            goOverlay.transform.SetParent(canvas.transform, false);
            var comp = goOverlay.AddComponent<TempestadeVisualOverlay>();

            var so = new SerializedObject(comp);
            so.FindProperty("veu").objectReferenceValue = img;
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("[MontarDeserto] Véu da tempestade instalado.", goVeu);
        }

        private static void LigarSpritesDasEntradas()
        {
            int ligados = 0, ysort = 0;

            foreach (var (nomeObjeto, nomeSprite) in Entradas)
            {
                var go = GameObject.Find(nomeObjeto);
                if (go == null)
                {
                    Debug.LogWarning($"[MontarDeserto] Objeto '{nomeObjeto}' não achado na cena.");
                    continue;
                }

                var sr = go.GetComponent<SpriteRenderer>();
                if (sr == null)
                {
                    Debug.LogWarning($"[MontarDeserto] '{nomeObjeto}' não tem SpriteRenderer.", go);
                    continue;
                }

                // Substitui enquanto o sprite não vier da pasta de Entradas: as localizações
                // nascem com o quadrado embutido da Unity como placeholder, então "já tem
                // sprite" não significa "já tem a arte certa". Só pula se já for a definitiva.
                string caminhoAtual = sr.sprite != null ? AssetDatabase.GetAssetPath(sr.sprite) : null;
                bool jaEhDefinitiva = caminhoAtual != null && caminhoAtual.StartsWith(PastaEntradas);

                if (!jaEhDefinitiva)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{PastaEntradas}/{nomeSprite}.png");
                    if (sprite == null)
                    {
                        Debug.LogError($"[MontarDeserto] Sprite '{nomeSprite}.png' não encontrado.");
                        continue;
                    }

                    Undo.RecordObject(sr, "Atribuir sprite da entrada");
                    sr.sprite = sprite;

                    // O placeholder usava tint para diferenciar as localizações (Tumba
                    // vermelha, Lago preto...). Mantê-lo tingiria a arte de verdade.
                    sr.color = Color.white;

                    EditorUtility.SetDirty(sr);
                    ligados++;
                }

                // Y-sort: sem isto o marco não passa por trás/na frente do Damião direito.
                if (go.GetComponent<DynamicYSort>() == null)
                {
                    Undo.AddComponent<DynamicYSort>(go);
                    ysort++;
                }
            }

            Debug.Log($"[MontarDeserto] Entradas: {ligados} sprite(s) atribuídos, " +
                      $"{ysort} DynamicYSort adicionado(s).");
        }
    }
}
