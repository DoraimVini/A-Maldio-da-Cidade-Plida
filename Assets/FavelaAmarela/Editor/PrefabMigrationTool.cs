using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using FavelaAmarela.Level.Runtime;
using FavelaAmarela.Runtime.GameLoop;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.Enemies;
using FavelaAmarela.CameraSystem;

namespace FavelaAmarela.Editor
{
    /// <summary>
    /// Script de migração ONE-SHOT. Roda uma vez para criar/corrigir prefabs
    /// e montar a cena de playtest. Depois de usado, deve ser deletado.
    /// 
    /// Cada MenuItem é independente — pode rodar isolado e é idempotente.
    /// </summary>
    public static class PrefabMigrationTool
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 1. PLAYER PREFAB
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        [MenuItem("Favela Amarela/Migração/1 — Criar Player_Damiao Prefab")]
        public static void CriarPlayerPrefab()
        {
            const string prefabPath = "Assets/FavelaAmarela/Art/Player/Player_Damiao.prefab";
            EnsureDirectoryExists("Assets/FavelaAmarela/Art/Player");

            // Criar GameObject temporário na cena para montar
            var go = new GameObject("Player_Damiao");

            // ORDEM IMPORTA: PlayerInput ANTES de PlayerMovement
            // porque PlayerMovement.Awake() faz GetComponent<PlayerInput>()
            var playerInput = go.AddComponent<PlayerInput>();
            var actionsAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/InputSystem_Actions.inputactions");
            if (actionsAsset != null)
            {
                playerInput.actions = actionsAsset;
                playerInput.defaultActionMap = "Player";
                Debug.Log("[Migração] InputActionAsset carregado com sucesso.");
            }
            else
            {
                Debug.LogError("[Migração] FALHA: InputSystem_Actions.inputactions não encontrado!");
            }

            // PlayerMovement injeta Rigidbody2D + BoxCollider2D via RequireComponent
            go.AddComponent<PlayerMovement>();
            go.AddComponent<AnomalyPowerBridge>();

            // Configurar Rigidbody2D (já existe via RequireComponent)
            var rb = go.GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            // SpriteRenderer — pode já existir ou não
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetFirstSprite("Assets/Damiao_Placeholder.aseprite");
            if (sr.sprite == null)
                Debug.LogWarning("[Migração] Sprite Damiao_Placeholder não encontrado. Atribuir manualmente.");

            // Salvar como prefab
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);

            if (prefab != null)
                Debug.Log($"[Migração] ✅ Player_Damiao.prefab criado em: {prefabPath}");
            else
                Debug.LogError($"[Migração] ❌ Falha ao criar prefab em: {prefabPath}");
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 2. ESPECTRO HALI PREFAB (corrigir existente)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        [MenuItem("Favela Amarela/Migração/2 — Corrigir EspectroHali Prefab")]
        public static void CorrigirEspectroHaliPrefab()
        {
            const string prefabPath = "Assets/FavelaAmarela/Art/Enemies/EspectroHali.prefab";
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset == null)
            {
                Debug.LogError($"[Migração] Prefab não encontrado em: {prefabPath}");
                return;
            }

            // Abrir para edição interna (não instancia na cena)
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);

            // Corrigir Rigidbody2D
            var rb = contents.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                Debug.Log("[Migração] Rigidbody2D: FreezeRotation + Continuous aplicados.");
            }

            // Corrigir BoxCollider2D (size era 0.0001 × 0.0001)
            var col = contents.GetComponent<BoxCollider2D>();
            var sr = contents.GetComponent<SpriteRenderer>();
            if (col != null && sr != null && sr.sprite != null)
            {
                // Usar o bounds do sprite para dimensionar o collider
                col.size = sr.sprite.bounds.size;
                col.offset = Vector2.zero;
                Debug.Log($"[Migração] BoxCollider2D.size ajustado para: {col.size}");
            }
            else if (col != null)
            {
                // Fallback: tamanho razoável se não tiver sprite
                col.size = new Vector2(1f, 1f);
                Debug.LogWarning("[Migração] Sprite não encontrado. BoxCollider2D.size = (1,1) fallback.");
            }

            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            PrefabUtility.UnloadPrefabContents(contents);

            Debug.Log($"[Migração] ✅ EspectroHali.prefab corrigido em: {prefabPath}");
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 3. HUD PREFAB (corrigir anchors e CanvasScaler)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        [MenuItem("Favela Amarela/Migração/3 — Corrigir HUD Prefab")]
        public static void CorrigirHUDPrefab()
        {
            const string prefabPath = "Assets/FavelaAmarela/Art/UI/HUD_ResilienciaBar.prefab";
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset == null)
            {
                Debug.LogError($"[Migração] Prefab HUD não encontrado em: {prefabPath}");
                return;
            }

            var contents = PrefabUtility.LoadPrefabContents(prefabPath);

            // 3a. CanvasScaler → Scale With Screen Size
            var scaler = contents.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                Debug.Log("[Migração] CanvasScaler: Scale With Screen Size (1920×1080, match 0.5).");
            }

            // 3b. Encontrar os RectTransforms por nome
            var barRoot = FindChildRecursive(contents.transform, "ResilienciaBar_Root");
            var background = FindChildRecursive(contents.transform, "Background");
            var fill = FindChildRecursive(contents.transform, "Fill");
            var panicOverlay = FindChildRecursive(contents.transform, "PanicOverlay");

            // ResilienciaBar_Root: topo-esquerdo, barra fina
            if (barRoot != null)
            {
                var rt = barRoot.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(20, -20);
                rt.sizeDelta = new Vector2(300, 24);
                Debug.Log("[Migração] ResilienciaBar_Root: ancorado topo-esquerdo, 300×24.");
            }

            // Background: esticar dentro do root
            if (background != null)
            {
                var rt = background.GetComponent<RectTransform>();
                SetStretchAll(rt);
                Debug.Log("[Migração] Background: stretch all.");
            }

            // Fill: esticar dentro do root
            if (fill != null)
            {
                var rt = fill.GetComponent<RectTransform>();
                SetStretchAll(rt);
                Debug.Log("[Migração] Fill: stretch all.");
            }

            // PanicOverlay: esticar dentro do root
            if (panicOverlay != null)
            {
                var rt = panicOverlay.GetComponent<RectTransform>();
                SetStretchAll(rt);
                Debug.Log("[Migração] PanicOverlay: stretch all.");
            }

            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            PrefabUtility.UnloadPrefabContents(contents);

            Debug.Log($"[Migração] ✅ HUD_ResilienciaBar.prefab corrigido em: {prefabPath}");
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 4. MONTAR CENA DE PLAYTEST
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        [MenuItem("Favela Amarela/Migração/4 — Montar Cena Playtest")]
        public static void MontarCenaPlaytest()
        {
            const string scenePath = "Assets/Scenes/Playtest_RuinasPalidas.unity";

            // Criar cena nova
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── 1. Level Blockout ──────────────────────────────────────────────
            var blockoutGo = new GameObject("LevelBlockoutGenerator");
            var blockoutGen = blockoutGo.AddComponent<LevelBlockoutGenerator>();
            blockoutGen.GenerateBlockout();
            Debug.Log("[Migração] Blockout gerado.");

            // ── 2. GameManager ─────────────────────────────────────────────────
            var gmGo = new GameObject("GameManager");
            gmGo.AddComponent<GameManager>();
            Debug.Log("[Migração] GameManager criado.");

            // ── 3. Player (instância do prefab) ───────────────────────────────
            const string playerPrefabPath = "Assets/FavelaAmarela/Art/Player/Player_Damiao.prefab";
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPrefabPath);
            GameObject playerInstance;
            if (playerPrefab != null)
            {
                playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
                playerInstance.transform.position = new Vector3(0, 0, 0);
                Debug.Log("[Migração] Player_Damiao instanciado na Zona 1.");
            }
            else
            {
                Debug.LogError($"[Migração] Prefab do Player não encontrado em: {playerPrefabPath}. " +
                               "Rode '1 — Criar Player_Damiao Prefab' primeiro.");
                playerInstance = new GameObject("Player_Damiao_PLACEHOLDER");
            }

            // ── 4. Câmera ─────────────────────────────────────────────────────
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.backgroundColor = new Color(0.05f, 0.04f, 0.06f, 1f); // Escuro de Carcosa
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.transform.position = new Vector3(0, 0, -10);
            camGo.transform.rotation = Quaternion.identity; // SEM rotação — cenário A

            var isoCam = camGo.AddComponent<IsometricCameraController>();
            isoCam.SetTarget(playerInstance.transform);
            Debug.Log("[Migração] Main Camera criada: Ortho, Size 8, sem rotação, fundo escuro.");

            // ── 5. HUD ────────────────────────────────────────────────────────
            const string hudPrefabPath = "Assets/FavelaAmarela/Art/UI/HUD_ResilienciaBar.prefab";
            var hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(hudPrefabPath);
            if (hudPrefab != null)
            {
                var hudInstance = (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab);
                hudInstance.name = "HUD_ResilienciaBar";
                Debug.Log("[Migração] HUD instanciado.");
            }
            else
            {
                Debug.LogWarning("[Migração] HUD prefab não encontrado. Pular.");
            }

            // ── 6. Cultistas (Zona 2 — Vila das Casas) ────────────────────────
            const string cultistaPrefabPath = "Assets/FavelaAmarela/Art/Enemies/EspectroHali.prefab";
            var cultistaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(cultistaPrefabPath);
            if (cultistaPrefab != null)
            {
                var enemiesRoot = new GameObject("Inimigos_Playtest");

                InstanciarCultistaComWaypoints(cultistaPrefab, enemiesRoot.transform,
                    new Vector2(9, -3),
                    new[] { new Vector2(9, -3), new Vector2(13, -4), new Vector2(13, -7), new Vector2(9, -7) },
                    "Cultista_1");

                InstanciarCultistaComWaypoints(cultistaPrefab, enemiesRoot.transform,
                    new Vector2(11, -10),
                    new[] { new Vector2(11, -10), new Vector2(15, -9), new Vector2(15, -12), new Vector2(11, -12) },
                    "Cultista_2");

                Debug.Log("[Migração] 2 Cultistas instanciados na Vila (Zona 2).");
            }
            else
            {
                Debug.LogWarning("[Migração] EspectroHali.prefab não encontrado. Pular cultistas.");
            }

            // ── 7. Salvar cena ─────────────────────────────────────────────────
            EnsureDirectoryExists("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[Migração] ✅ Cena salva em: {scenePath}");
            Debug.Log("[Migração] ✅ PRONTO! Abra a cena e dê Play.");
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // UTILITÁRIOS
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private static Sprite GetFirstSprite(string path)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets == null) return null;
            foreach (var asset in assets)
            {
                if (asset is Sprite sprite) return sprite;
            }
            return null;
        }

        private static void SetStretchAll(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; // Left, Bottom
            rt.offsetMax = Vector2.zero; // Right, Top (negatives go inward)
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var found = FindChildRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private static void EnsureDirectoryExists(string assetPath)
        {
            // Converte "Assets/Foo/Bar" em caminho de sistema e cria se não existe
            string fullPath = System.IO.Path.Combine(Application.dataPath,
                assetPath.Replace("Assets/", "").Replace("Assets\\", ""));
            if (!System.IO.Directory.Exists(fullPath))
            {
                System.IO.Directory.CreateDirectory(fullPath);
                AssetDatabase.Refresh();
            }
        }

        private static void InstanciarCultistaComWaypoints(
            GameObject prefab, Transform parent, Vector2 pos, Vector2[] wpPositions, string label)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = label;
            go.transform.position = (Vector3)(pos);
            go.transform.SetParent(parent);

            var waypointsRoot = new GameObject($"{label}_Waypoints");
            waypointsRoot.transform.SetParent(parent);

            var wps = new Transform[wpPositions.Length];
            for (int i = 0; i < wpPositions.Length; i++)
            {
                var wp = new GameObject($"WP_{i}");
                wp.transform.position = (Vector3)(wpPositions[i]);
                wp.transform.SetParent(waypointsRoot.transform);
                wps[i] = wp.transform;
            }

            // Atribuir waypoints via SerializedObject (campo privado)
            var ai = go.GetComponent<CultistaAI>();
            if (ai != null)
            {
                var so = new SerializedObject(ai);
                var wpProp = so.FindProperty("waypoints");
                if (wpProp != null)
                {
                    wpProp.arraySize = wps.Length;
                    for (int i = 0; i < wps.Length; i++)
                    {
                        wpProp.GetArrayElementAtIndex(i).objectReferenceValue = wps[i];
                    }
                    so.ApplyModifiedProperties();
                }
                else
                {
                    Debug.LogError($"[Migração] Campo 'waypoints' não encontrado em CultistaAI de {label}!");
                }
            }
        }
    }
}
