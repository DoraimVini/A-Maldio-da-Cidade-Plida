using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.Combat;
using FavelaAmarela.Runtime.Enemies;
using FavelaAmarela.Runtime.Interaction;
using FavelaAmarela.Runtime.Persistencia;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Utilitário de Editor: monta na cena aberta tudo que falta para o botão de
    /// interação (E) e a conversa ramificada com Abdul funcionarem de ponta a ponta —
    /// <see cref="DetectorDeInteracao"/> + prompt no Damião, o
    /// <see cref="PainelDeEscolha"/> no HUD, a ficha e a instância de <b>Yug-Neth</b>
    /// (cativa na arena, antes de libertada), e o wiring dos campos do
    /// <see cref="AbdulAlhazredAI"/> na cena.
    ///
    /// <para>Yug-Neth é colocado <b>diretamente na cena</b> (não instanciado sob demanda
    /// em runtime): decisão do Vini — ele já existe visível, vagando perto de Abdul,
    /// antes de qualquer interação.</para>
    ///
    /// Idempotente: reaproveita o que já existe e só completa o que falta, mesmo padrão
    /// dos outros builders desta pasta (<c>BuildHUDCompleto</c>).
    /// </summary>
    public static class MontarInteracaoEDialogoAbdul
    {
        private const string CaminhoFichaYugNeth = "Assets/FavelaAmarela/Config/Ficha_YugNeth.asset";
        private const string CaminhoPrefabYugNeth = "Assets/FavelaAmarela/Art/Characters/MiGo/YugNeth.prefab";
        private const string CaminhoSpriteYugNeth = "Assets/FavelaAmarela/Art/Characters/MiGo/yug_neth_idle.png";

        [MenuItem("Tools/FavelaAmarela/Montar Interação e Diálogo do Abdul (cena aberta)")]
        public static void Montar()
        {
            var player = Object.FindAnyObjectByType<PlayerMovement>();
            if (player == null)
            {
                Debug.LogError("[MontarInteracao] Nenhum PlayerMovement na cena — abortado.");
                return;
            }

            var detector = MontarDetectorDeInteracao(player);
            var canvasGO = ObterCanvasHUD();

            if (canvasGO == null)
            {
                Debug.LogError("[MontarInteracao] Nenhum HUD_Gameplay na cena — rode " +
                               "'Build HUD Completo' primeiro. Abortado.");
                return;
            }

            var prompt = MontarPromptDeInteracao(canvasGO, detector);
            var painelDeEscolha = MontarPainelDeEscolha(canvasGO, player);
            var caixaDeDialogo = Object.FindAnyObjectByType<TutorialHintUI>(FindObjectsInactive.Include);

            var abdul = Object.FindAnyObjectByType<AbdulAlhazredAI>(FindObjectsInactive.Include);
            if (abdul == null)
            {
                Debug.LogError("[MontarInteracao] Nenhum AbdulAlhazredAI na cena — abortado " +
                               "antes de posicionar Yug-Neth (ele nasce perto do Abdul).");
                return;
            }

            var ficha = ObterOuCriarFichaYugNeth();
            var prefab = ObterOuCriarPrefabYugNeth(ficha);
            var yugNethNaCena = ObterOuCriarInstanciaNaCena(prefab, abdul.transform);

            int camposLigados = 0;
            camposLigados += AtribuirSeVazio(abdul, "painelDeEscolha", painelDeEscolha);
            camposLigados += AtribuirSeVazio(abdul, "yugNethNaArena", yugNethNaCena);
            if (caixaDeDialogo != null)
                camposLigados += AtribuirSeVazio(abdul, "caixaDeDialogo", caixaDeDialogo);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("[MontarInteracao] Concluído: DetectorDeInteracao + Prompt no Damião, " +
                      "Painel de Escolha no HUD, Yug-Neth cativo posicionado perto do Abdul, " +
                      $"{camposLigados} campo(s) do Abdul ligados. Cena NÃO foi salva — confira antes.");
        }

        // ── Detector + Prompt de Interação ───────────────────────────────────

        private static DetectorDeInteracao MontarDetectorDeInteracao(PlayerMovement player)
        {
            var detector = player.GetComponent<DetectorDeInteracao>();
            if (detector == null) detector = player.gameObject.AddComponent<DetectorDeInteracao>();
            return detector;
        }

        private static PromptDeInteracao MontarPromptDeInteracao(GameObject canvasGO, DetectorDeInteracao detector)
        {
            var existente = canvasGO.GetComponentInChildren<PromptDeInteracao>(true);
            if (existente != null) return existente;

            var raizGO = NovoRetangulo("Prompt_Interacao", canvasGO.transform);
            AncorarBaixoCentro(raizGO, x: 0f, y: 64f, largura: 260f, altura: 24f);

            var fundo = raizGO.AddComponent<Image>();
            fundo.color = new Color(0f, 0f, 0f, 0.55f);

            var textoGO = NovoRetangulo("Texto", raizGO.transform);
            Esticar(textoGO);
            var texto = textoGO.AddComponent<Text>();
            texto.font = ObterFontePadrao();
            texto.fontSize = 14;
            texto.color = Color.white;
            texto.alignment = TextAnchor.MiddleCenter;
            texto.text = "E — Interagir";

            var promptGO = new GameObject("PromptDeInteracao");
            promptGO.transform.SetParent(canvasGO.transform, false);
            var prompt = promptGO.AddComponent<PromptDeInteracao>();
            AtribuirCampo(prompt, "detector", detector);
            AtribuirCampo(prompt, "raiz", raizGO);
            AtribuirCampo(prompt, "label", texto);

            raizGO.transform.SetParent(promptGO.transform, false);
            return prompt;
        }

        // ── Painel de Escolha ────────────────────────────────────────────────

        private static PainelDeEscolha MontarPainelDeEscolha(GameObject canvasGO, PlayerMovement player)
        {
            var existente = canvasGO.GetComponentInChildren<PainelDeEscolha>(true);
            if (existente != null) return existente;

            var playerInput = player.GetComponent<PlayerInput>();

            var raizGO = NovoRetangulo("Painel_Escolha", canvasGO.transform);
            AncorarCentro(raizGO, largura: 320f, altura: 90f);

            var fundo = raizGO.AddComponent<Image>();
            fundo.color = new Color(0.05f, 0.04f, 0.03f, 0.9f);

            var textoGO = NovoRetangulo("Texto", raizGO.transform);
            Esticar(textoGO);
            var texto = textoGO.AddComponent<Text>();
            texto.font = ObterFontePadrao();
            texto.fontSize = 16;
            texto.color = new Color(0.93f, 0.90f, 0.75f);
            texto.alignment = TextAnchor.MiddleLeft;
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;

            var painelGO = new GameObject("PainelDeEscolha");
            painelGO.transform.SetParent(canvasGO.transform, false);
            var painel = painelGO.AddComponent<PainelDeEscolha>();
            AtribuirCampo(painel, "raiz", raizGO);
            AtribuirCampo(painel, "texto", texto);
            AtribuirCampo(painel, "playerInput", playerInput);
            AtribuirCampo(painel, "movimentoDoJogador", player);

            raizGO.transform.SetParent(painelGO.transform, false);
            return painel;
        }

        // ── Ficha, prefab e instância de cena do Yug-Neth ────────────────────

        private static FichaAtributosConfig ObterOuCriarFichaYugNeth()
        {
            var ficha = AssetDatabase.LoadAssetAtPath<FichaAtributosConfig>(CaminhoFichaYugNeth);
            if (ficha != null) return ficha;

            ficha = ScriptableObject.CreateInstance<FichaAtributosConfig>();
            // Frágil de propósito: o filhote é passivo e não deve aguentar o combate como
            // um Cultista — poucos golpes o derrubam se descuidado.
            var so = new SerializedObject(ficha);
            so.FindProperty("vitalidadeMax").floatValue = 40f;
            so.FindProperty("ataque").floatValue = 0f;
            so.FindProperty("defesa").floatValue = 0f;
            so.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(CaminhoFichaYugNeth)!);
            AssetDatabase.CreateAsset(ficha, CaminhoFichaYugNeth);
            AssetDatabase.SaveAssets();
            return ficha;
        }

        /// <summary>
        /// Garante que um prefab já existente do Yug-Neth tenha
        /// <see cref="EstadoPersistenteDoCompanheiro"/> — adicionado depois da criação
        /// original do prefab (2026-08-02), para a Vitalidade dele sobreviver à travessia
        /// de cena. Edita o <b>asset</b> diretamente via <c>EditPrefabContentsScope</c>, não
        /// uma instância solta, para não deixar cada instância na cena divergindo do prefab.
        /// </summary>
        private static GameObject GarantirEstadoPersistente(GameObject prefabExistente)
        {
            string caminho = AssetDatabase.GetAssetPath(prefabExistente);
            using (var escopo = new PrefabUtility.EditPrefabContentsScope(caminho))
            {
                if (escopo.prefabContentsRoot.GetComponent<EstadoPersistenteDoCompanheiro>() == null)
                    escopo.prefabContentsRoot.AddComponent<EstadoPersistenteDoCompanheiro>();
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
        }

        private static GameObject ObterOuCriarPrefabYugNeth(FichaAtributosConfig ficha)
        {
            var existente = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoPrefabYugNeth);
            if (existente != null) return GarantirEstadoPersistente(existente);

            var go = new GameObject("YugNeth",
                typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(BoxCollider2D),
                typeof(VitalidadeBridge), typeof(EstadoPersistenteDoCompanheiro), typeof(YugNethAI));

            go.layer = LayerMask.NameToLayer("Enemy"); // pode ser alvo de golpe, mesma camada dos inimigos

            var rb = go.GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = go.GetComponent<BoxCollider2D>();
            col.size = new Vector2(0.6f, 0.6f);

            var sr = go.GetComponent<SpriteRenderer>();
            var spriteReal = AssetDatabase.LoadAssetAtPath<Sprite>(CaminhoSpriteYugNeth);
            if (spriteReal != null)
            {
                sr.sprite = spriteReal;
            }
            else
            {
                Debug.LogWarning($"[MontarInteracao] Sprite de Yug-Neth não encontrado em " +
                                 $"'{CaminhoSpriteYugNeth}' — usando placeholder colorido.");
                sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                sr.color = new Color(0.61f, 0.42f, 0.48f); // paleta do design (#9B6B7A)
                go.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
            }

            var vitalidadeBridge = go.GetComponent<VitalidadeBridge>();
            AtribuirCampo(vitalidadeBridge, "ficha", ficha);

            AdicionarYSortSeExistir(go);

            Directory.CreateDirectory(Path.GetDirectoryName(CaminhoPrefabYugNeth)!);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, CaminhoPrefabYugNeth);
            Object.DestroyImmediate(go);
            return prefab;
        }

        /// <summary>
        /// Coloca Yug-Neth <b>diretamente na cena</b>, cativo, um pouco ao lado de Abdul —
        /// ele precisa estar visível e vagando antes de qualquer interação, não nascer só
        /// quando libertado. Idempotente: se já existir uma instância na cena, reaproveita.
        /// </summary>
        private static YugNethAI ObterOuCriarInstanciaNaCena(GameObject prefab, Transform abdulTransform)
        {
            var existente = Object.FindAnyObjectByType<YugNethAI>(FindObjectsInactive.Include);
            if (existente != null) return existente;

            var instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab, abdulTransform.parent);
            // Um pouco ao lado do Abdul: "preso no canto da arena", não em cima dele.
            instancia.transform.position = abdulTransform.position + new Vector3(1.5f, -0.5f, 0f);

            return instancia.GetComponent<YugNethAI>();
        }

        private static void AdicionarYSortSeExistir(GameObject go)
        {
            var tipo = System.AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("FavelaAmarela.Runtime.Rendering.DynamicYSort"))
                .FirstOrDefault(t => t != null);
            if (tipo != null) go.AddComponent(tipo);
        }

        // ── Canvas / layout helpers (mesmo padrão de BuildHUDCompleto) ───────

        private static GameObject ObterCanvasHUD()
        {
            var hud = Object.FindAnyObjectByType<HUDController>();
            return hud != null ? hud.gameObject : null;
        }

        private static GameObject NovoRetangulo(string nome, Transform pai)
        {
            var go = new GameObject(nome, typeof(RectTransform));
            go.transform.SetParent(pai, worldPositionStays: false);
            return go;
        }

        private static void AncorarBaixoCentro(GameObject go, float x, float y, float largura, float altura)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(largura, altura);
        }

        private static void AncorarCentro(GameObject go, float largura, float altura)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(largura, altura);
        }

        private static void Esticar(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Font ObterFontePadrao()
        {
            try { return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch (System.Exception e)
            {
                Debug.LogError($"[MontarInteracao] Fonte built-in indisponível: {e.Message}");
                return null;
            }
        }

        private static int AtribuirSeVazio(Component alvo, string campo, Object valor)
        {
            var so = new SerializedObject(alvo);
            var prop = so.FindProperty(campo);
            if (prop == null)
            {
                Debug.LogWarning($"[MontarInteracao] Campo '{campo}' não encontrado em {alvo.GetType().Name}.");
                return 0;
            }
            if (prop.objectReferenceValue != null) return 0; // não sobrescreve o que já foi setado

            prop.objectReferenceValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
            return 1;
        }

        private static void AtribuirCampo(Component alvo, string campo, Object valor)
        {
            var so = new SerializedObject(alvo);
            var prop = so.FindProperty(campo);
            if (prop == null)
            {
                Debug.LogWarning($"[MontarInteracao] Campo '{campo}' não encontrado em {alvo.GetType().Name}.");
                return;
            }
            prop.objectReferenceValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
