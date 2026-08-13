using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Utilitário de Editor: <b>o único ponto de montagem do HUD de gameplay</b> — as seis
    /// views (<see cref="ResilienciaBar"/>, <see cref="VitalidadeBar"/>, <see cref="VigorBar"/>,
    /// <see cref="BarraDeAcoes"/>, <see cref="BarraDeItens"/>, <see cref="BarraDeArtefatos"/>)
    /// mais o <see cref="PainelDeInventario"/>, todas ligadas no <see cref="HUDController"/>.
    ///
    /// <para><b>Por que existe centralizado (2026-08-13):</b> até aqui cada peça vinha de uma
    /// ferramenta com lista de cenas própria — <c>MontarBarraDeItens</c>,
    /// <c>MontarPainelDeInventario</c>, e uma <c>MontarBarraDeArtefatos</c> que só existia
    /// dentro de <c>MontarArenaDeTestes</c>. O resultado: <b>nenhuma cena tinha HUD
    /// completo</b>. O Deserto e o Santuário não mostravam a arma empunhada nem os artefatos
    /// F1–F4; a <c>VigorBar</c> nunca foi instanciada em cena ou prefab nenhum, então a
    /// Esquiva não tinha indicador de recurso em lugar algum.</para>
    ///
    /// <para>Idempotente: acha o <see cref="HUDController"/> existente (inclusive dentro do
    /// prefab <c>HUD_ResilienciaBar</c>, que só liga duas das seis views) e só completa o que
    /// falta — não recria views já presentes.</para>
    /// </summary>
    public static class BuildHUDCompleto
    {
        private const string NomeRaiz = "HUD_Gameplay";

        private static readonly string[] CenasDeJogo =
        {
            "Assets/Scenes/Deserto_Hali.unity",
            "Assets/Scenes/Playtest_RuinasPalidas.unity",
            "Assets/Scenes/Santuario_Yhtill.unity",
        };

        [MenuItem("Tools/FavelaAmarela/Build HUD Completo (cena aberta)")]
        public static void Build()
        {
            var canvasGO = ObterOuCriarCanvas(out var hud);

            // ── Resiliência Mental (sanidade) ────────────────────────────────
            var resiliencia = ObterOuCriarBarra<ResilienciaBar>(canvasGO, "Barra_ResilienciaMental",
                ancoraY: -24f, corFill: new Color(0.85f, 0.78f, 0.30f));

            // ── Vitalidade corpórea (a carne) ────────────────────────────────
            var vitalidade = ObterOuCriarBarra<VitalidadeBar>(canvasGO, "Barra_Vitalidade",
                ancoraY: -48f, corFill: new Color(0.72f, 0.18f, 0.18f));

            // ── Vigor (estamina da Esquiva) ───────────────────────────────────
            var vigor = ObterOuCriarBarra<VigorBar>(canvasGO, "Barra_Vigor",
                ancoraY: -72f, corFill: new Color(0.35f, 0.70f, 0.25f));

            // ── Barra de Ações da Mão Física ─────────────────────────────────
            var acoes = canvasGO.GetComponentInChildren<BarraDeAcoes>(true)
                        ?? CriarBarraDeAcoes(canvasGO.transform);

            // ── Barra de Artefatos (4 slots, F1–F4) ──────────────────────────
            var artefatos = canvasGO.GetComponentInChildren<BarraDeArtefatos>(true)
                            ?? CriarBarraDeArtefatos(canvasGO.transform);

            LigarViews(hud, resiliencia, vitalidade, vigor, acoes, artefatos);

            // Barra de itens (teclas 1–8) e painel de inventário (Tab): ferramentas próprias
            // que já se auto-ligam no HUDController — ver MontarBarraDeItens.MontarNaCenaAberta.
            // Idempotentes, então chamar de novo numa cena que já as tem não duplica nada.
            MontarBarraDeItens.MontarNaCenaAberta();
            MontarPainelDeInventario.MontarNaCenaAberta();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = canvasGO;

            Debug.Log($"[BuildHUDCompleto] HUD completo em '{EditorSceneManager.GetActiveScene().name}': " +
                      "Resiliência, Vitalidade, Vigor, Ações, Artefatos, Itens e Painel de Inventário. " +
                      "O GameManager injeta as fontes no bootstrap (Play para ver).");
        }

        /// <summary>
        /// Aplica <see cref="Build"/> nas três cenas de jogo do Vertical Slice em sequência.
        /// Existe porque rodar <c>Build</c> só na cena aberta exige lembrar de repetir para
        /// cada fase — e foi exatamente por não repetir que o Deserto e o Santuário ficaram
        /// para trás.
        /// </summary>
        [MenuItem("Tools/FavelaAmarela/Build HUD Completo em todas as cenas de jogo")]
        public static void BuildEmTodasAsCenas()
        {
            var cenaAtiva = EditorSceneManager.GetActiveScene();
            if (cenaAtiva.isDirty && !string.IsNullOrEmpty(cenaAtiva.path))
                EditorSceneManager.SaveScene(cenaAtiva);
            string cenaOriginal = cenaAtiva.path;

            int feitas = 0;
            foreach (var caminho in CenasDeJogo)
            {
                if (!File.Exists(caminho)) continue;

                var cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);
                Build();
                EditorSceneManager.MarkSceneDirty(cena);
                EditorSceneManager.SaveScene(cena);
                feitas++;
            }

            if (!string.IsNullOrEmpty(cenaOriginal) && System.Array.IndexOf(CenasDeJogo, cenaOriginal) < 0)
                EditorSceneManager.OpenScene(cenaOriginal, OpenSceneMode.Single);

            Debug.Log($"[BuildHUDCompleto] HUD completo aplicado em {feitas} cena(s) de jogo.");
        }

        // ── Canvas raiz ──────────────────────────────────────────────────────

        private static GameObject ObterOuCriarCanvas(out HUDController hud)
        {
            hud = Object.FindAnyObjectByType<HUDController>(FindObjectsInactive.Include);
            if (hud != null) return hud.gameObject;

            var go = new GameObject(NomeRaiz,
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(HUDController));

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // acima do fade/painel de Colapso

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640f, 360f); // pixel art 16:9
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            hud = go.GetComponent<HUDController>();
            return go;
        }

        // ── Barras de recurso (trilho + preenchimento) ───────────────────────

        private static T ObterOuCriarBarra<T>(GameObject canvasGO, string nome, float ancoraY, Color corFill)
            where T : Component
        {
            var existente = canvasGO.GetComponentInChildren<T>(true);
            return existente != null ? existente : CriarBarra<T>(canvasGO.transform, nome, ancoraY, corFill);
        }

        private static T CriarBarra<T>(Transform pai, string nome, float ancoraY, Color corFill)
            where T : Component
        {
            var raiz = NovoRetangulo(nome, pai);
            AncorarTopoEsquerda(raiz, x: 16f, y: ancoraY, largura: 180f, altura: 16f);

            var fundo = NovoRetangulo("Trilho", raiz.transform);
            Esticar(fundo);
            var imgFundo = fundo.AddComponent<Image>();
            imgFundo.color = new Color(0.08f, 0.07f, 0.06f, 0.85f);

            var fill = NovoRetangulo("Preenchimento", raiz.transform);
            Esticar(fill);
            var imgFill = fill.AddComponent<Image>();
            imgFill.color = corFill;
            imgFill.type = Image.Type.Filled;
            imgFill.fillMethod = Image.FillMethod.Horizontal;
            imgFill.fillAmount = 1f;

            var barra = raiz.AddComponent<T>();
            AtribuirCampo(barra, "fillImage", imgFill);
            AtribuirCampo(barra, "backgroundImage", imgFundo);
            return barra;
        }

        // ── Barra de ações (slots de arma e habilidade) ──────────────────────

        private static BarraDeAcoes CriarBarraDeAcoes(Transform pai)
        {
            var raiz = NovoRetangulo("BarraDeAcoes", pai);
            // Canto inferior direito — longe das barras de recurso.
            var rt = raiz.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-16f, 16f);
            rt.sizeDelta = new Vector2(200f, 44f);

            var fonte = ObterFontePadrao();

            var textoArma = CriarTexto("NomeDaArma", raiz.transform, fonte, 14,
                new Color(0.93f, 0.90f, 0.75f), TextAnchor.LowerRight);
            AncorarNoRetangulo(textoArma, new Vector2(0f, 22f), new Vector2(200f, 22f));

            var textoHab = CriarTexto("NomeDaHabilidade", raiz.transform, fonte, 12,
                new Color(0.72f, 0.66f, 0.45f), TextAnchor.LowerRight);
            AncorarNoRetangulo(textoHab, new Vector2(0f, 0f), new Vector2(200f, 20f));

            // Grupo da habilidade: opacidade cai enquanto recarrega.
            var grupo = textoHab.gameObject.AddComponent<CanvasGroup>();

            // Preenchimento de recarga (barrinha fina sob o nome da habilidade).
            var recarga = NovoRetangulo("Recarga", raiz.transform);
            AncorarNoRetangulo(recarga, new Vector2(0f, -6f), new Vector2(200f, 3f));
            var imgRecarga = recarga.AddComponent<Image>();
            imgRecarga.color = new Color(0.85f, 0.78f, 0.30f);
            imgRecarga.type = Image.Type.Filled;
            imgRecarga.fillMethod = Image.FillMethod.Horizontal;
            imgRecarga.fillAmount = 1f;

            var barra = raiz.AddComponent<BarraDeAcoes>();
            AtribuirCampo(barra, "nomeDaArma", textoArma);
            AtribuirCampo(barra, "nomeDaHabilidade", textoHab);
            AtribuirCampo(barra, "preenchimentoRecarga", imgRecarga);
            AtribuirCampo(barra, "grupoHabilidade", grupo);
            return barra;
        }

        // ── Barra de Artefatos (4 slots, F1–F4) ──────────────────────────────
        // Movida de MontarArenaDeTestes.MontarBarraDeArtefatos (2026-08-13): a Arena era a
        // única cena que a montava, então nenhuma cena de jogo mostrava os artefatos F1–F4.

        /// <summary>
        /// Versão mínima de 4 slots (só texto, sem ícone/recarga visual) — o suficiente para o
        /// jogador ver que artefato está em qual tecla. Sub-campos de ícone e recarga ficam
        /// nulos de propósito: <c>BarraDeArtefatos.Redesenhar/Update</c> já checam null em cada
        /// um.
        /// </summary>
        private static BarraDeArtefatos CriarBarraDeArtefatos(Transform pai)
        {
            var raiz = new GameObject("Barra_Artefatos", typeof(RectTransform));
            raiz.transform.SetParent(pai, false);

            var rt = raiz.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(16f, 16f);
            rt.sizeDelta = new Vector2(220f, 44f);

            var fonte = ObterFontePadrao();
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

            return barra;
        }

        // ── Helpers de layout ────────────────────────────────────────────────

        private static GameObject NovoRetangulo(string nome, Transform pai)
        {
            var go = new GameObject(nome, typeof(RectTransform));
            go.transform.SetParent(pai, worldPositionStays: false);
            return go;
        }

        private static void AncorarTopoEsquerda(GameObject go, float x, float y, float largura, float altura)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(largura, altura);
        }

        private static void AncorarNoRetangulo(GameObject go, Vector2 posicao, Vector2 tamanho)
            => AncorarNoRetangulo(go.transform, posicao, tamanho);

        private static void AncorarNoRetangulo(Component alvo, Vector2 posicao, Vector2 tamanho)
        {
            var rt = alvo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = posicao;
            rt.sizeDelta = tamanho;
        }

        private static void Esticar(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Text CriarTexto(string nome, Transform pai, Font fonte, int tamanho,
            Color cor, TextAnchor alinhamento)
        {
            var go = NovoRetangulo(nome, pai);
            var texto = go.AddComponent<Text>();
            texto.font = fonte;
            texto.fontSize = tamanho;
            texto.color = cor;
            texto.alignment = alinhamento;
            texto.horizontalOverflow = HorizontalWrapMode.Overflow;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
            texto.text = "—";
            return texto;
        }

        /// <summary>
        /// Fonte embutida da Unity 6 — <c>LegacyRuntime.ttf</c>. O nome antigo
        /// (<c>Arial.ttf</c>) foi removido e <b>lança</b> ArgumentException; ver
        /// <c>FonteBuiltinTests</c>.
        /// </summary>
        private static Font ObterFontePadrao()
        {
            try
            {
                return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BuildHUDCompleto] Fonte built-in indisponível: {e.Message}");
                return null;
            }
        }

        // ── Wiring por SerializedObject (campos privados [SerializeField]) ───

        private static void AtribuirCampo(Component alvo, string campo, Object valor)
        {
            var so = new SerializedObject(alvo);
            var prop = so.FindProperty(campo);
            if (prop == null)
            {
                Debug.LogWarning($"[BuildHUDCompleto] Campo '{campo}' não encontrado em {alvo.GetType().Name}.");
                return;
            }
            prop.objectReferenceValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void LigarViews(HUDController hud, ResilienciaBar resiliencia,
            VitalidadeBar vitalidade, VigorBar vigor, BarraDeAcoes acoes, BarraDeArtefatos artefatos)
        {
            if (hud == null) return;
            AtribuirCampo(hud, "resilienciaBar", resiliencia);
            AtribuirCampo(hud, "vitalidadeBar", vitalidade);
            AtribuirCampo(hud, "vigorBar", vigor);
            AtribuirCampo(hud, "barraDeAcoes", acoes);
            AtribuirCampo(hud, "barraDeArtefatos", artefatos);
        }
    }
}
