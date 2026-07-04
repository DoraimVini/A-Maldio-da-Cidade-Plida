using UnityEditor;
using UnityEngine;
using FavelaAmarela.Level.Runtime;
using FavelaAmarela.Runtime.GameLoop;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.Enemies;
using FavelaAmarela.Runtime.UI;
using FavelaAmarela.Core.Enemies;
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
            
            var gameManager = Object.FindAnyObjectByType<GameManager>();
            if (gameManager == null)
            {
                var go = new GameObject("GameManager");
                gameManager = go.AddComponent<GameManager>();
            }

            // 2. Player (Damião)
            var player = GameObject.Find("Player_Damiao");
            if (player != null)
            {
                Object.DestroyImmediate(player);
            }
            player = new GameObject("Player_Damiao");
            
            // 1. Input deve ser injetado ANTES de scripts que dependem dele no Awake()
            var playerInput = player.AddComponent<PlayerInput>();
            playerInput.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            playerInput.defaultActionMap = "Player";
            
            // 2. Comportamentos do Player
            player.AddComponent<PlayerMovement>();
            player.AddComponent<AnomalyPowerBridge>();
            
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
                cultistaGo.AddComponent<BoxCollider2D>(); // BoxCollider2D precisa ser adicionado
                
                var csr = cultistaGo.GetComponent<SpriteRenderer>();
                if (csr == null) csr = cultistaGo.AddComponent<SpriteRenderer>();
                csr.sprite = GetFirstSprite("Assets/Espectro_Hali_Placeholder.aseprite");
                
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
