using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Level.Runtime;
using FavelaAmarela.Runtime.GameLoop;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.Enemies;
using FavelaAmarela.Runtime.UI;
using FavelaAmarela.CameraSystem;
using UnityEngine.InputSystem;

namespace FavelaAmarela.Editor
{
    public static class PlaytestSceneSetup
    {
        [MenuItem("Favela Amarela/Montar Cena de Playtest")]
        public static void SetupScene()
        {
            // 1. Base do nível
            var blockoutGen = Object.FindAnyObjectByType<LevelBlockoutGenerator>();
            if (blockoutGen == null)
            {
                var go = new GameObject("LevelBlockoutGenerator");
                blockoutGen = go.AddComponent<LevelBlockoutGenerator>();
            }
            blockoutGen.GenerateBlockout();
            
            FavelaAmarela.EditorTools.MontarBootstrapDaCena.Garantir();

            // 2. Player (Damião)
            var player = GameObject.Find("Player_Damiao");
            if (player != null)
            {
                Object.DestroyImmediate(player);
            }
            player = new GameObject("Player_Damiao");
            player.tag = "Player"; // necessário para ColapsoTrigger e QuedaZ4Z5Trigger (OnTriggerEnter2D + CompareTag)

            // 1. Input deve ser injetado ANTES de scripts que dependem dele no Awake()
            var playerInput = player.AddComponent<PlayerInput>();
            playerInput.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            playerInput.defaultActionMap = "Player";
            
            // 2. Comportamentos do Player
            player.AddComponent<PlayerMovement>();
            player.AddComponent<EsquivaBridge>();
            
            // 3. Física e Visual (RequireComponent do PlayerMovement já injeta Rigidbody2D e BoxCollider2D)
            var rb = player.GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            
            var sr = player.GetComponent<SpriteRenderer>();
            if (sr == null) sr = player.AddComponent<SpriteRenderer>();
            sr.sprite = GetFirstSprite("Assets/Damiao_Placeholder.aseprite");
            
            // Ponto inicial - Zona 1 (Aprox. x=0, y=0)
            player.transform.position = new Vector3(0, 0, 0);

            // 2.5 Câmera Isométrica
            var camObj = GameObject.Find("Main Camera");
            if (camObj == null)
            {
                camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                camObj.AddComponent<UnityEngine.Camera>();
            }
            camObj.transform.rotation = Quaternion.identity; // Câmera plana — ver favela-isometric-standards
            
            var isoCam = camObj.GetComponent<IsometricCameraController>();
            if (isoCam == null) isoCam = camObj.AddComponent<IsometricCameraController>();
            isoCam.SetTarget(player.transform);

            // 3. Prefab do Cultista
            string prefabPath = "Assets/FavelaAmarela/Art/Enemies/EspectroHali.prefab";
            if (!System.IO.Directory.Exists("Assets/FavelaAmarela/Art/Enemies"))
            {
                System.IO.Directory.CreateDirectory("Assets/FavelaAmarela/Art/Enemies");
            }

            GameObject cultistaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (cultistaPrefab == null)
            {
                var cultistaGo = GameObject.Find("Espectro_Hali");
                if (cultistaGo != null) Object.DestroyImmediate(cultistaGo);

                cultistaGo = new GameObject("Espectro_Hali");
                cultistaGo.AddComponent<CultistaAI>(); // CultistaAI injeta SpriteRenderer e Rigidbody2D

                var crb = cultistaGo.GetComponent<Rigidbody2D>();
                crb.gravityScale = 0f;

                // O sprite precisa ser atribuído ANTES do BoxCollider2D, senão o
                // collider auto-dimensiona para ~zero (sem bounds de sprite) e o
                // inimigo atravessa as paredes. Depois forçamos o tamanho pelos
                // bounds do sprite para garantir um collider sólido e visível.
                var csr = cultistaGo.GetComponent<SpriteRenderer>();
                if (csr == null) csr = cultistaGo.AddComponent<SpriteRenderer>();
                csr.sprite = GetFirstSprite("Assets/Espectro_Hali_Placeholder.aseprite");

                var ccol = cultistaGo.AddComponent<BoxCollider2D>();
                if (csr.sprite != null) ccol.size = csr.sprite.bounds.size;

                cultistaPrefab = PrefabUtility.SaveAsPrefabAsset(cultistaGo, prefabPath);
                Object.DestroyImmediate(cultistaGo);
            }

            // 4. Popular a Vila das Casas (Zona 2 - aprox x=15, y=5)
            var enemiesRoot = GameObject.Find("Inimigos_Playtest");
            if (enemiesRoot != null)
            {
                Object.DestroyImmediate(enemiesRoot);
            }
            enemiesRoot = new GameObject("Inimigos_Playtest");

            InstanciarCultista(cultistaPrefab, enemiesRoot.transform, new Vector2(9, -3), new Vector2[] { new Vector2(9, -3), new Vector2(13, -4), new Vector2(13, -7), new Vector2(9, -7) });
            InstanciarCultista(cultistaPrefab, enemiesRoot.transform, new Vector2(11, -10), new Vector2[] { new Vector2(11, -10), new Vector2(15, -9), new Vector2(15, -12), new Vector2(11, -12) });

            // 5. HUD
            var hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FavelaAmarela/Art/UI/HUD_ResilienciaBar.prefab");
            if (hudPrefab != null)
            {
                var existingHud = GameObject.Find("HUD_ResilienciaBar");
                if (existingHud != null) Object.DestroyImmediate(existingHud);

                var hud = PrefabUtility.InstantiatePrefab(hudPrefab) as GameObject;
                hud.name = "HUD_ResilienciaBar";
            }

            // 6. Screen Fader (mascara transições roteirizadas, ex.: a queda Z4→Z5)
            var faderGo = GameObject.Find("ScreenFader");
            if (faderGo != null) Object.DestroyImmediate(faderGo);

            faderGo = new GameObject("ScreenFader");
            var faderCanvas = faderGo.AddComponent<Canvas>();
            faderCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            faderCanvas.sortingOrder = 100; // por cima do HUD (Canvas sortingOrder 0)
            faderGo.AddComponent<CanvasScaler>();
            faderGo.AddComponent<GraphicRaycaster>();

            var fadeImageGo = new GameObject("FadeImage");
            fadeImageGo.transform.SetParent(faderGo.transform, false);
            var fadeImage = fadeImageGo.AddComponent<Image>();
            fadeImage.color = new Color(0f, 0f, 0f, 0f); // começa transparente
            fadeImage.raycastTarget = false; // não deve bloquear clique quando invisível
            var fadeRect = fadeImage.rectTransform;
            fadeRect.anchorMin = Vector2.zero;
            fadeRect.anchorMax = Vector2.one;
            fadeRect.offsetMin = Vector2.zero;
            fadeRect.offsetMax = Vector2.zero;

            var fader = faderGo.AddComponent<ScreenFader>();
            var faderSO = new SerializedObject(fader);
            faderSO.FindProperty("fadeImage").objectReferenceValue = fadeImage;
            faderSO.ApplyModifiedProperties();

            // 7. Trigger de queda Z4 → Z5 (Damião cercado, chão cede, cai na Zona 5;
            // a barreira anômala entre as duas zonas já bloqueia a volta a pé)
            var z4Root = GameObject.Find("Zona4_PracaDoCerco");
            var z5Root = GameObject.Find("Zona5_TransicaoDimensional");
            if (z4Root != null && z5Root != null)
            {
                var z4Floor = z4Root.transform.Find("Floor");
                var z5Floor = z5Root.transform.Find("Floor");
                if (z4Floor != null && z5Floor != null)
                {
                    var destinoGo = GameObject.Find("QuedaZ4Z5_Destino");
                    if (destinoGo != null) Object.DestroyImmediate(destinoGo);
                    destinoGo = new GameObject("QuedaZ4Z5_Destino");
                    destinoGo.transform.position = z5Floor.position;

                    var triggerGo = GameObject.Find("Trigger_QuedaZ4Z5");
                    if (triggerGo != null) Object.DestroyImmediate(triggerGo);
                    triggerGo = new GameObject("Trigger_QuedaZ4Z5");
                    triggerGo.transform.position = z4Floor.position;

                    var floorSr = z4Floor.GetComponent<SpriteRenderer>();
                    Vector2 triggerSize = floorSr != null ? (Vector2)floorSr.bounds.size : new Vector2(10f, 10f);

                    var triggerCol = triggerGo.AddComponent<BoxCollider2D>();
                    triggerCol.isTrigger = true;
                    triggerCol.size = triggerSize * 0.8f; // menor que a sala inteira, evita disparar já na porta

                    var quedaTrigger = triggerGo.AddComponent<QuedaZ4Z5Trigger>();
                    var qtSO = new SerializedObject(quedaTrigger);
                    qtSO.FindProperty("destino").objectReferenceValue = destinoGo.transform;
                    qtSO.FindProperty("isoCameraController").objectReferenceValue = isoCam;
                    qtSO.FindProperty("fader").objectReferenceValue = fader;
                    qtSO.ApplyModifiedProperties();
                }
            }

            Debug.Log("Cena de Playtest montada com sucesso! Você pode dar Play.");
        }

        private static Sprite GetFirstSprite(string path)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in assets)
            {
                if (asset is Sprite sprite) return sprite;
            }
            return null;
        }

        private static void InstanciarCultista(GameObject prefab, Transform parent, Vector2 pos, Vector2[] wpPoses)
        {
            var go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            go.transform.position = pos;
            go.transform.SetParent(parent);

            var waypointsRoot = new GameObject(go.name + "_Waypoints");
            waypointsRoot.transform.SetParent(parent);

            Transform[] wps = new Transform[wpPoses.Length];
            for (int i = 0; i < wpPoses.Length; i++)
            {
                var wp = new GameObject($"WP_{i}");
                wp.transform.position = wpPoses[i];
                wp.transform.SetParent(waypointsRoot.transform);
                wps[i] = wp.transform;
            }

            var ai = go.GetComponent<CultistaAI>();
            var so = new SerializedObject(ai);
            var wpProp = so.FindProperty("waypoints");
            wpProp.arraySize = wps.Length;
            for(int i = 0; i < wps.Length; i++)
            {
                wpProp.GetArrayElementAtIndex(i).objectReferenceValue = wps[i];
            }
            so.ApplyModifiedProperties();
        }
    }
}
