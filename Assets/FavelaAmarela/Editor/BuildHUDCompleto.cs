using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Utilitário de Editor: monta o HUD de gameplay completo na cena aberta —
    /// barra de <b>Resiliência Mental</b> (sanidade), barra de <b>Vitalidade</b> (a carne)
    /// e a <b>Barra de Ações</b> da Mão Física (arma empunhada + habilidade e sua recarga).
    ///
    /// Existe porque as cenas de playtest não tinham HUD nenhum: o prefab
    /// <c>HUD_ResilienciaBar</c> nunca foi instanciado, então nem a Resiliência aparecia.
    /// Construir a hierarquia por código (e não por YAML na mão) deixa a Unity resolver
    /// anchors, fonte e sprites — o mesmo padrão dos outros builders desta pasta.
    ///
    /// Idempotente: se o HUD já existir na cena, reaproveita e só completa o que falta.
    /// </summary>
    public static class BuildHUDCompleto
    {
        private const string NomeRaiz = "HUD_Gameplay";

        [MenuItem("Tools/FavelaAmarela/Build HUD Completo (cena aberta)")]
        public static void Build()
        {
            var canvasGO = ObterOuCriarCanvas(out var hud);

            // ── Resiliência Mental (sanidade) ────────────────────────────────
            var resiliencia = canvasGO.GetComponentInChildren<ResilienciaBar>(true);
            if (resiliencia == null)
            {
                resiliencia = CriarBarra<ResilienciaBar>(
                    canvasGO.transform, "Barra_ResilienciaMental",
                    ancoraY: -24f, corFill: new Color(0.85f, 0.78f, 0.30f));
            }

            // ── Vitalidade corpórea (a carne) ────────────────────────────────
            var vitalidade = canvasGO.GetComponentInChildren<VitalidadeBar>(true);
            if (vitalidade == null)
            {
                vitalidade = CriarBarra<VitalidadeBar>(
                    canvasGO.transform, "Barra_Vitalidade",
                    ancoraY: -48f, corFill: new Color(0.72f, 0.18f, 0.18f));
            }

            // ── Barra de Ações da Mão Física ─────────────────────────────────
            var acoes = canvasGO.GetComponentInChildren<BarraDeAcoes>(true);
            if (acoes == null)
                acoes = CriarBarraDeAcoes(canvasGO.transform);

            // ── Liga as views no HUDController ──────────────────────────────
            LigarViews(hud, resiliencia, vitalidade, acoes);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = canvasGO;

            Debug.Log($"[BuildHUDCompleto] HUD montado em '{EditorSceneManager.GetActiveScene().name}': " +
                      "Resiliência Mental, Vitalidade e Barra de Ações. " +
                      "O GameManager injeta as fontes no bootstrap (Play para ver).");
        }

        // ── Canvas raiz ──────────────────────────────────────────────────────

        private static GameObject ObterOuCriarCanvas(out HUDController hud)
        {
            hud = Object.FindAnyObjectByType<HUDController>();
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
            VitalidadeBar vitalidade, BarraDeAcoes acoes)
        {
            if (hud == null) return;
            AtribuirCampo(hud, "resilienciaBar", resiliencia);
            AtribuirCampo(hud, "vitalidadeBar", vitalidade);
            AtribuirCampo(hud, "barraDeAcoes", acoes);
        }
    }
}
