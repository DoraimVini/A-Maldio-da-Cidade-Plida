using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Runtime.Quests;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Cria a <b>caixa de diálogo</b> (<see cref="TutorialHintUI"/>)
    /// nas cenas jogáveis e liga nela todo mundo que precisa falar com o jogador.
    ///
    /// <para><b>Descoberta que motivou:</b> o <c>TutorialHintUI</c> existia no projeto mas
    /// <b>não estava em cena nenhuma</b> — nem na Tumba, nem no Deserto. Todo componente que
    /// mostra texto (baú, patuá, Necronomicon, Refúgio, Cassilda, fragmentos) tinha o campo
    /// vazio e falava para o vazio: a mecânica rodava, o jogador não via nada.</para>
    ///
    /// <para>Idempotente: reaproveita a caixa existente e só preenche campos vazios.</para>
    /// </summary>
    public static class MontarCaixaDeDialogo
    {
        private static readonly string[] CenasJogaveis =
        {
            "Assets/Scenes/Deserto_Hali.unity",
            "Assets/Scenes/Playtest_RuinasPalidas.unity",
            "Assets/Scenes/Santuario_Yhtill.unity",
        };

        [MenuItem("Tools/FavelaAmarela/Montar caixa de dialogo nas cenas")]
        public static void Executar()
        {
            // Salva em silêncio, sem perguntar. `SaveCurrentModifiedScenesIfUserWantsTo`
            // abre um diálogo MODAL — e uma ferramenta disparada pela ponte MCP trava a
            // Unity inteira esperando um clique que ninguém vê. Foi o que aconteceu aqui.
            var ativa = EditorSceneManager.GetActiveScene();
            if (ativa.isDirty && !string.IsNullOrEmpty(ativa.path))
                EditorSceneManager.SaveScene(ativa);

            string cenaOriginal = ativa.path;

            foreach (var caminho in CenasJogaveis)
            {
                var cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);
                var caixa = GarantirCaixa();
                int ligados = LigarQuemFala(caixa);

                EditorSceneManager.MarkSceneDirty(cena);
                EditorSceneManager.SaveScene(cena);
                Debug.Log($"[CaixaDeDialogo] '{cena.name}': caixa pronta, {ligados} componente(s) ligados.");
            }

            if (!string.IsNullOrEmpty(cenaOriginal))
                EditorSceneManager.OpenScene(cenaOriginal, OpenSceneMode.Single);
        }

        private static TutorialHintUI GarantirCaixa()
        {
            var existente = Object.FindAnyObjectByType<TutorialHintUI>(FindObjectsInactive.Include);
            if (existente != null)
            {
                CorrigirCaixaExistente(existente);
                return existente;
            }

            var canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                var goCanvas = new GameObject("Canvas_Dialogo",
                    typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Undo.RegisterCreatedObjectUndo(goCanvas, "Criar Canvas");
                canvas = goCanvas.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            // Painel na faixa inferior — leitura de visual novel, como o design de Cassilda pede.
            var painel = new GameObject("CaixaDeDialogo", typeof(CanvasGroup), typeof(Image));
            Undo.RegisterCreatedObjectUndo(painel, "Criar caixa de diálogo");
            painel.transform.SetParent(canvas.transform, false);

            var rt = painel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.08f, 0.04f);
            rt.anchorMax = new Vector2(0.92f, 0.28f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var fundo = painel.GetComponent<Image>();
            fundo.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            // Sliced, não Simple: o UISprite é um retângulo de cantos arredondados feito
            // para 9-slice. Em Simple os cantos escalam junto com o painel e o retângulo
            // vira uma elipse — foi o que apareceu em playtest como "bolha preta".
            fundo.type = Image.Type.Sliced;

            fundo.color = new Color(0.05f, 0.04f, 0.02f, 0.85f);  // quase preto, quente
            fundo.raycastTarget = false;

            var goTexto = new GameObject("Texto", typeof(Text));
            Undo.RegisterCreatedObjectUndo(goTexto, "Criar texto do diálogo");
            goTexto.transform.SetParent(painel.transform, false);

            var rtTexto = goTexto.GetComponent<RectTransform>();
            rtTexto.anchorMin = Vector2.zero;
            rtTexto.anchorMax = Vector2.one;
            rtTexto.offsetMin = new Vector2(24f, 18f);   // respiro nas bordas
            rtTexto.offsetMax = new Vector2(-24f, -18f);

            var texto = goTexto.GetComponent<Text>();

            // Unity 6: a fonte embutida é "LegacyRuntime.ttf", e vem por
            // Resources.GetBuiltinResource — não por AssetDatabase.GetBuiltinExtraResource,
            // que LANÇA ArgumentException com o nome antigo ("Arial.ttf"). Mesma armadilha
            // já documentada em DanoFlutuante.cs e coberta por FonteBuiltinTests.
            texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (texto.font == null)
                Debug.LogError("[CaixaDeDialogo] Fonte built-in não encontrada — a caixa " +
                               "existirá mas não mostrará texto.");
            texto.fontSize = 20;
            texto.alignment = TextAnchor.UpperLeft;
            texto.color = new Color(0.93f, 0.89f, 0.72f);  // amarelo-pálido de Carcosa
            texto.raycastTarget = false;
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            texto.verticalOverflow = VerticalWrapMode.Truncate;

            // Nasce invisível. O TutorialHintUI zera o alpha no Awake, mas o valor
            // serializado precisa ser 0 também: senão a caixa fica plantada na tela no
            // Editor e pisca no primeiro frame do Play.
            var grupo = painel.GetComponent<CanvasGroup>();
            grupo.alpha = 0f;
            grupo.interactable = false;
            grupo.blocksRaycasts = false;

            var comp = painel.AddComponent<TutorialHintUI>();
            var so = new SerializedObject(comp);
            so.FindProperty("grupo").objectReferenceValue = grupo;
            so.FindProperty("texto").objectReferenceValue = texto;
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("[CaixaDeDialogo] Caixa criada (não existia nesta cena).", painel);
            return comp;
        }

        /// <summary>
        /// Conserta uma caixa já montada: alpha zerado e fundo em <c>Sliced</c>.
        ///
        /// <para>As primeiras caixas nasceram com <c>alpha 1</c> e o fundo em <c>Simple</c>,
        /// e o resultado foi uma <b>bolha preta permanente</b> no rodapé — o sprite de cantos
        /// arredondados esticado virou elipse, e ela nunca sumia porque o valor serializado
        /// não era 0.</para>
        /// </summary>
        private static void CorrigirCaixaExistente(TutorialHintUI caixa)
        {
            var so = new SerializedObject(caixa);

            if (so.FindProperty("grupo").objectReferenceValue is CanvasGroup grupo)
            {
                Undo.RecordObject(grupo, "Zerar alpha da caixa");
                grupo.alpha = 0f;
                grupo.interactable = false;
                grupo.blocksRaycasts = false;
                EditorUtility.SetDirty(grupo);
            }

            var fundo = caixa.GetComponent<Image>();
            if (fundo != null && fundo.type != Image.Type.Sliced)
            {
                Undo.RecordObject(fundo, "Fundo da caixa em Sliced");
                fundo.type = Image.Type.Sliced;
                EditorUtility.SetDirty(fundo);
            }

            Debug.Log("[CaixaDeDialogo] Caixa existente corrigida (alpha 0, fundo Sliced).", caixa);
        }

        /// <summary>
        /// Liga a caixa em todo componente que fala. Só preenche campo vazio — quem já tem
        /// uma caixa atribuída à mão fica como está.
        /// </summary>
        private static int LigarQuemFala(TutorialHintUI caixa)
        {
            int ligados = 0;
            ligados += Ligar<CassildaNPC>(caixa, "caixaDeTexto");
            ligados += Ligar<FragmentoDeYhtill>(caixa, "caixaDeTexto");
            ligados += Ligar<Runtime.GameLoop.RefugioDeLuz>(caixa, "caixaDeTexto");
            ligados += Ligar<Runtime.GameLoop.BauDaTumba>(caixa, "hintUI");
            ligados += Ligar<Runtime.GameLoop.PatuaPickup>(caixa, "hintUI");
            return ligados;
        }

        private static int Ligar<T>(TutorialHintUI caixa, string campo) where T : MonoBehaviour
        {
            var alvos = Object.FindObjectsByType<T>(FindObjectsInactive.Include);
            int ligados = 0;

            foreach (var alvo in alvos)
            {
                var so = new SerializedObject(alvo);
                var prop = so.FindProperty(campo);
                if (prop == null || prop.objectReferenceValue != null) continue;

                prop.objectReferenceValue = caixa;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(alvo);
                ligados++;
            }

            return ligados;
        }
    }
}
