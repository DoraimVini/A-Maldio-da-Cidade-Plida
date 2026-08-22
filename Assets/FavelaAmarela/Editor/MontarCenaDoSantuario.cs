using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FavelaAmarela.CameraSystem;
using FavelaAmarela.Runtime.Environment;
using FavelaAmarela.Runtime.GameLoop;
using FavelaAmarela.Runtime.Quests;
using FavelaAmarela.Runtime.Rendering;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Cria a cena <b>interior do Santuário de Yhtill</b> e liga o
    /// Deserto a ela por portal — mesmo padrão da Tumba (decisão do Vini, 2026-08-01).
    ///
    /// <para>Cassilda e o Refúgio <b>mudam-se para dentro</b>: ficavam soltos no overworld,
    /// o que contradizia a ideia de um santuário. O marco no Deserto vira só a porta.</para>
    ///
    /// <para><b>Calmaria sobrenatural</b> (design §4.3): a tempestade não entra. Implementada
    /// com um <c>TempestadeAmbiente</c> de faixa 0–0 em vez de simplesmente não pôr driver
    /// nenhum — sem driver, o <c>EnvironmentState</c> ficaria no valor inicial dele (0,3) e o
    /// Santuário teria uma tempestade fraca por acidente.</para>
    ///
    /// <para>Idempotente: rodar de novo reaproveita a cena e os objetos pelo nome.</para>
    /// </summary>
    public static class MontarCenaDoSantuario
    {
        private const string CenaSantuario = "Assets/Scenes/Santuario_Yhtill.unity";
        private const string CenaDeserto = "Assets/Scenes/Deserto_Hali.unity";

        private const string PrefabJogador = "Assets/FavelaAmarela/Art/Characters/Damiao/Player_Damiao.prefab";
        private const string PrefabHUD = "Assets/FavelaAmarela/Art/UI/HUD_ResilienciaBar.prefab";
        private const string CaminhoPrefabCassilda = "Assets/FavelaAmarela/Art/Characters/Cassilda/Cassilda.prefab";

        private const string IdChegadaNoSantuario = "SantuarioDeYhtill";
        private const string IdChegadaNoDeserto = "VoltaDoSantuario";

        // Plataforma pequena e fechada: é um respiro, não uma área de exploração.
        private static readonly Vector2 TamanhoDoPiso = new Vector2(16f, 11f);
        private static readonly Vector3 PosDoJogador = new Vector3(0f, -3.5f, 0f);
        private static readonly Vector3 PosDaCassilda = new Vector3(0f, 2.5f, 0f);
        private static readonly Vector3 PosDoRefugio = new Vector3(-4.5f, -1f, 0f);
        private static readonly Vector3 PosDaSaida = new Vector3(0f, -4.8f, 0f);

        [MenuItem("Tools/FavelaAmarela/Montar cena do Santuario de Yhtill")]
        public static void Executar()
        {
            var cenaAtiva = EditorSceneManager.GetActiveScene();
            if (cenaAtiva.isDirty && !string.IsNullOrEmpty(cenaAtiva.path))
                EditorSceneManager.SaveScene(cenaAtiva);

            ConstruirSantuario();
            LigarOPortalNoDeserto();
            RegistrarEmBuildSettings();

            Debug.Log("[Santuário] Cena interior pronta e ligada ao Deserto.");
        }

        // ── A cena ───────────────────────────────────────────────────────────

        private static void ConstruirSantuario()
        {
            Scene cena = System.IO.File.Exists(CenaSantuario)
                ? EditorSceneManager.OpenScene(CenaSantuario, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var jogador = GarantirJogador();
            GarantirCamera(jogador);
            GarantirGameManager();
            GarantirPrefabPorNome(PrefabHUD, "HUD");
            var caixa = GarantirCaixaDeDialogo();

            GarantirCalmaria();
            GarantirPiso();
            GarantirCassilda(caixa);
            GarantirRefugio(caixa);
            GarantirSaida();
            GarantirChegada();

            if (!System.IO.File.Exists(CenaSantuario))
                EditorSceneManager.SaveScene(cena, CenaSantuario);
            else
                EditorSceneManager.SaveScene(cena);
        }

        private static GameObject GarantirJogador()
        {
            var existente = GameObject.FindGameObjectWithTag("Player");
            if (existente != null) return existente;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabJogador);
            if (prefab == null)
            {
                Debug.LogError($"[Santuário] Prefab do jogador não encontrado em {PrefabJogador}.");
                return null;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = PosDoJogador;
            return go;
        }

        private static void GarantirCamera(GameObject jogador)
        {
            var cam = Object.FindAnyObjectByType<Camera>(FindObjectsInactive.Include);
            if (cam == null)
            {
                var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                go.tag = "MainCamera";
                cam = go.GetComponent<Camera>();
            }

            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.backgroundColor = new Color(0.10f, 0.09f, 0.07f);  // interior: mais escuro que o deserto
            cam.transform.position = new Vector3(0f, 0f, -10f);

            var ctrl = cam.GetComponent<IsometricCameraController>();
            if (ctrl == null) ctrl = cam.gameObject.AddComponent<IsometricCameraController>();

            if (jogador != null)
            {
                var so = new SerializedObject(ctrl);
                so.FindProperty("target").objectReferenceValue = jogador.transform;
                so.FindProperty("orthographicSize").floatValue = 6f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void GarantirGameManager() => MontarBootstrapDaCena.Garantir();

        private static void GarantirPrefabPorNome(string caminho, string nome)
        {
            if (GameObject.Find(nome) != null) return;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
            if (prefab == null)
            {
                Debug.LogWarning($"[Santuário] Prefab '{caminho}' não encontrado.");
                return;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = nome;
        }

        /// <summary>
        /// Calmaria: driver com faixa 0–0. <b>Não basta omitir o driver</b> — sem ele o
        /// <c>EnvironmentState</c> fica no valor inicial (0,3) e o Santuário teria uma
        /// tempestade fraca por acidente, justamente onde o design promete silêncio.
        /// </summary>
        private static void GarantirCalmaria()
        {
            var driver = Object.FindAnyObjectByType<TempestadeAmbiente>(FindObjectsInactive.Include);
            if (driver == null)
                driver = new GameObject("Calmaria_Sobrenatural").AddComponent<TempestadeAmbiente>();

            var so = new SerializedObject(driver);
            so.FindProperty("minimoInicial").floatValue = 0f;
            so.FindProperty("maximoInicial").floatValue = 0f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void GarantirPiso()
        {
            var raiz = GameObject.Find("Santuario_Root") ?? new GameObject("Santuario_Root");

            var piso = raiz.transform.Find("Piso")?.gameObject;
            if (piso == null)
            {
                piso = new GameObject("Piso", typeof(SpriteRenderer));
                piso.transform.SetParent(raiz.transform, false);
            }

            var sr = piso.GetComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = TamanhoDoPiso;
            sr.color = new Color(0.72f, 0.69f, 0.62f);   // calcário frio (paleta §6.1)
            sr.sortingOrder = -1000;                      // sempre atrás de todo mundo

            // Paredes: caixa fechada em volta do piso, para não sair andando no vazio.
            GarantirParede(raiz.transform, "Parede_Norte", new Vector3(0f, TamanhoDoPiso.y / 2f, 0f), new Vector2(TamanhoDoPiso.x, 0.5f));
            GarantirParede(raiz.transform, "Parede_Sul", new Vector3(0f, -TamanhoDoPiso.y / 2f, 0f), new Vector2(TamanhoDoPiso.x, 0.5f));
            GarantirParede(raiz.transform, "Parede_Leste", new Vector3(TamanhoDoPiso.x / 2f, 0f, 0f), new Vector2(0.5f, TamanhoDoPiso.y));
            GarantirParede(raiz.transform, "Parede_Oeste", new Vector3(-TamanhoDoPiso.x / 2f, 0f, 0f), new Vector2(0.5f, TamanhoDoPiso.y));
        }

        private static void GarantirParede(Transform raiz, string nome, Vector3 pos, Vector2 tamanho)
        {
            var t = raiz.Find(nome);
            var go = t != null ? t.gameObject : new GameObject(nome);
            if (t == null) go.transform.SetParent(raiz, false);

            go.transform.localPosition = pos;
            go.layer = LayerMask.NameToLayer("Obstacle");

            var col = go.GetComponent<BoxCollider2D>();
            if (col == null) col = go.AddComponent<BoxCollider2D>();
            col.size = tamanho;
        }

        private static TutorialHintUI GarantirCaixaDeDialogo()
        {
            var existente = Object.FindAnyObjectByType<TutorialHintUI>(FindObjectsInactive.Include);
            if (existente != null) return existente;

            var canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                var goCanvas = new GameObject("Canvas_Santuario",
                    typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = goCanvas.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            var painel = new GameObject("CaixaDeDialogo", typeof(CanvasGroup), typeof(Image));
            painel.transform.SetParent(canvas.transform, false);

            var rt = painel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.08f, 0.04f);
            rt.anchorMax = new Vector2(0.92f, 0.28f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var fundo = painel.GetComponent<Image>();
            fundo.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            fundo.color = new Color(0.05f, 0.04f, 0.02f, 0.85f);
            fundo.raycastTarget = false;

            var goTexto = new GameObject("Texto", typeof(Text));
            goTexto.transform.SetParent(painel.transform, false);

            var rtTexto = goTexto.GetComponent<RectTransform>();
            rtTexto.anchorMin = Vector2.zero;
            rtTexto.anchorMax = Vector2.one;
            rtTexto.offsetMin = new Vector2(24f, 18f);
            rtTexto.offsetMax = new Vector2(-24f, -18f);

            var texto = goTexto.GetComponent<Text>();
            // Unity 6: a fonte embutida é LegacyRuntime.ttf, e vem por Resources —
            // AssetDatabase.GetBuiltinExtraResource com "Arial.ttf" LANÇA exceção.
            texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texto.fontSize = 60;
            texto.color = new Color(0.93f, 0.89f, 0.72f);
            texto.raycastTarget = false;
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;

            var comp = painel.AddComponent<TutorialHintUI>();
            var so = new SerializedObject(comp);
            so.FindProperty("grupo").objectReferenceValue = painel.GetComponent<CanvasGroup>();
            so.FindProperty("texto").objectReferenceValue = texto;
            so.ApplyModifiedPropertiesWithoutUndo();

            return comp;
        }

        /// <summary>
        /// Todo o conteúdo de Cassilda (falas, estrofes do recital) mora no prefab dela
        /// (<c>MontarPrefabDaCassilda</c>) — este método só garante que uma instância exista
        /// na cena e liga a caixa de diálogo. O painel de escolha do recital é wiring de
        /// <c>MontarSantuarioDeYhtill</c>, que roda depois desta ferramenta.
        /// </summary>
        private static void GarantirCassilda(TutorialHintUI caixa)
        {
            var npc = Object.FindAnyObjectByType<CassildaNPC>(FindObjectsInactive.Include);

            // Autocorreção: substitui uma Cassilda solta (de antes do prefab existir) por
            // uma instância real do prefab, mesmo raciocínio de MontarSantuarioDeYhtill.
            if (npc != null && PrefabUtility.GetPrefabInstanceStatus(npc.gameObject) == PrefabInstanceStatus.NotAPrefab)
            {
                Object.DestroyImmediate(npc.gameObject);
                npc = null;
            }

            GameObject go;

            if (npc != null)
            {
                go = npc.gameObject;
            }
            else
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoPrefabCassilda);
                if (prefab != null)
                {
                    go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                }
                else
                {
                    Debug.LogWarning("[Santuário] Prefab da Cassilda não encontrado em " +
                                      $"{CaminhoPrefabCassilda} — criando um placeholder sem " +
                                      "conteúdo. Rode 'Tools/FavelaAmarela/Montar Prefab da " +
                                      "Cassilda' e depois 'Montar Santuário de Yhtill e " +
                                      "fragmentos' para preencher as falas.");
                    go = new GameObject("Cassilda", typeof(SpriteRenderer), typeof(DynamicYSort),
                        typeof(CircleCollider2D), typeof(CassildaNPC));
                    var sr = go.GetComponent<SpriteRenderer>();
                    sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                    sr.color = new Color(0.93f, 0.89f, 0.55f);
                    go.GetComponent<CircleCollider2D>().isTrigger = true;
                    go.GetComponent<CircleCollider2D>().radius = 1.2f;
                }
                go.name = "Cassilda";
                npc = go.GetComponent<CassildaNPC>();
            }

            go.transform.position = PosDaCassilda;

            if (caixa != null)
            {
                var so = new SerializedObject(npc);
                so.FindProperty("caixaDeTexto").objectReferenceValue = caixa;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void GarantirRefugio(TutorialHintUI caixa)
        {
            var refugio = Object.FindAnyObjectByType<RefugioDeLuz>(FindObjectsInactive.Include);
            GameObject go;

            if (refugio != null)
            {
                go = refugio.gameObject;
            }
            else
            {
                go = new GameObject("Refugio_Santuario", typeof(CircleCollider2D), typeof(RefugioDeLuz));
                go.GetComponent<CircleCollider2D>().isTrigger = true;
                go.GetComponent<CircleCollider2D>().radius = 1.8f;
                refugio = go.GetComponent<RefugioDeLuz>();

                if (TagExiste("PontoDeLuz")) go.tag = "PontoDeLuz";
            }

            go.transform.position = PosDoRefugio;

            if (caixa != null)
            {
                var so = new SerializedObject(refugio);
                so.FindProperty("caixaDeTexto").objectReferenceValue = caixa;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void GarantirSaida()
        {
            var go = GameObject.Find("Saida_Santuario") ?? new GameObject("Saida_Santuario");
            go.transform.position = PosDaSaida;

            var col = go.GetComponent<BoxCollider2D>();
            if (col == null) col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2.5f, 1.2f);

            var portal = go.GetComponent<PortalDeCena>();
            if (portal == null) portal = go.AddComponent<PortalDeCena>();
            portal.DefinirCenaDestino("Deserto_Hali");
            portal.DefinirChegada(IdChegadaNoDeserto);
        }

        private static void GarantirChegada()
        {
            var go = GameObject.Find("Chegada_Santuario") ?? new GameObject("Chegada_Santuario");
            go.transform.position = PosDoJogador;

            var ponto = go.GetComponent<PontoDeChegada>();
            if (ponto == null) ponto = go.AddComponent<PontoDeChegada>();

            var so = new SerializedObject(ponto);
            so.FindProperty("identificador").stringValue = IdChegadaNoSantuario;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── O lado do Deserto ────────────────────────────────────────────────

        private static void LigarOPortalNoDeserto()
        {
            var cena = EditorSceneManager.OpenScene(CenaDeserto, OpenSceneMode.Single);

            // O marco vira a porta.
            var marco = GameObject.Find("Santuario_Yhtill");
            if (marco == null)
            {
                Debug.LogError("[Santuário] Marco 'Santuario_Yhtill' não achado no Deserto.");
                return;
            }

            var col = marco.GetComponent<BoxCollider2D>();
            if (col == null) col = marco.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(3f, 2f);

            var portal = marco.GetComponent<PortalDeCena>();
            if (portal == null) portal = marco.AddComponent<PortalDeCena>();
            portal.DefinirCenaDestino("Santuario_Yhtill");
            portal.DefinirChegada(IdChegadaNoSantuario);

            // Ponto de volta, ao lado do marco (não em cima: cair sobre o portal de ida
            // dispararia a entrada de novo se a carência acabasse antes de o jogador sair).
            var volta = GameObject.Find("Chegada_VoltaDoSantuario") ?? new GameObject("Chegada_VoltaDoSantuario");
            volta.transform.position = marco.transform.position + new Vector3(0f, -2.5f, 0f);

            var ponto = volta.GetComponent<PontoDeChegada>();
            if (ponto == null) ponto = volta.AddComponent<PontoDeChegada>();
            var so = new SerializedObject(ponto);
            so.FindProperty("identificador").stringValue = IdChegadaNoDeserto;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Cassilda e o Refúgio do Santuário mudaram-se para dentro: o que sobrou aqui
            // são cópias que confundiriam (duas Cassildas, quest em dois lugares).
            RemoverSeExistir("Cassilda");
            RemoverSeExistir("Refugio_SantuarioDeYhtill");

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
        }

        private static void RemoverSeExistir(string nome)
        {
            var go = GameObject.Find(nome);
            if (go == null) return;

            Object.DestroyImmediate(go);
            Debug.Log($"[Santuário] '{nome}' removido do Deserto — mudou-se para dentro.");
        }

        // ── Build Settings ───────────────────────────────────────────────────

        private static void RegistrarEmBuildSettings()
        {
            var cenas = EditorBuildSettings.scenes.ToList();
            if (cenas.Any(c => c.path == CenaSantuario))
            {
                Debug.Log("[Santuário] Cena já estava em Build Settings.");
                return;
            }

            cenas.Add(new EditorBuildSettingsScene(CenaSantuario, true));
            EditorBuildSettings.scenes = cenas.ToArray();

            // Sem isto, LoadScene por nome falha em runtime — a cena existiria mas seria
            // inalcançável pelo portal.
            Debug.Log("[Santuário] Cena registrada em Build Settings.");
        }

        private static bool TagExiste(string tag)
            => UnityEditorInternal.InternalEditorUtility.tags.Contains(tag);
    }
}
