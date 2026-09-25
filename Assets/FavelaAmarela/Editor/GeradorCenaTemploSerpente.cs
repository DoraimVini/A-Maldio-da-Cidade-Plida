using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using System.Linq;

namespace FavelaAmarela.Editor
{
    public static class GeradorCenaTemploSerpente
    {
        [MenuItem("Tools/FavelaAmarela/Gerar Cena Templo Serpente")]
        public static void GerarCena()
        {
            string scenePath = "Assets/Scenes/TemploSerpente.unity";
            
            // Cria cena aditiva vazia
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            newScene.name = "TemploSerpente";

            // Root
            GameObject root = new GameObject("TemploSerpente");
            
            // 1. Grid
            GameObject gridObj = new GameObject("Grid");
            gridObj.transform.SetParent(root.transform);
            var grid = gridObj.AddComponent<Grid>();
            grid.cellLayout = GridLayout.CellLayout.IsometricZAsY;
            grid.cellSize = new Vector3(1.0f, 0.5f, 1.0f);

            // Ground_Base
            GameObject groundBaseObj = new GameObject("Ground_Base");
            groundBaseObj.transform.SetParent(gridObj.transform);
            groundBaseObj.AddComponent<Tilemap>();
            var groundBaseTR = groundBaseObj.AddComponent<TilemapRenderer>();
            groundBaseTR.sortingLayerName = "Ground";
            groundBaseTR.sortingOrder = 0;

            // Ground_Decals
            GameObject groundDecalsObj = new GameObject("Ground_Decals");
            groundDecalsObj.transform.SetParent(gridObj.transform);
            groundDecalsObj.AddComponent<Tilemap>();
            var groundDecalsTR = groundDecalsObj.AddComponent<TilemapRenderer>();
            groundDecalsTR.sortingLayerName = "Ground";
            groundDecalsTR.sortingOrder = 1;

            // Walls
            GameObject wallsObj = new GameObject("Walls");
            wallsObj.transform.SetParent(gridObj.transform);
            var rb = wallsObj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            rb.gravityScale = 0.0f;
            wallsObj.AddComponent<CompositeCollider2D>();

            // 2. Entities
            GameObject entitiesObj = new GameObject("Entities");
            entitiesObj.transform.SetParent(root.transform);
            GameObject enemiesObj = new GameObject("Enemies");
            enemiesObj.transform.SetParent(entitiesObj.transform);

            // 3. Triggers
            GameObject triggersObj = new GameObject("Triggers");
            triggersObj.transform.SetParent(root.transform);

            GameObject triggerEntradaObj = new GameObject("Trigger_Entrada_Templo");
            triggerEntradaObj.transform.SetParent(triggersObj.transform);
            var bc1 = triggerEntradaObj.AddComponent<BoxCollider2D>();
            bc1.isTrigger = true;

            GameObject triggerChefeObj = new GameObject("Trigger_Chefe");
            triggerChefeObj.transform.SetParent(triggersObj.transform);
            var bc2 = triggerChefeObj.AddComponent<BoxCollider2D>();
            bc2.isTrigger = true;
            triggerChefeObj.SetActive(false);

            // 4. SceneSetup
            GameObject sceneSetupObj = new GameObject("SceneSetup");
            sceneSetupObj.transform.SetParent(root.transform);
            sceneSetupObj.AddComponent<FavelaAmarela.Dungeons.TemploSerpenteSetup>();

            // Adicionar comentário sobre quem chama (Streaming)
            // Em Unity, adicionar componente de comentário nativo ou apenas logar.
            Debug.Log("Lembre-se: O Trigger de transição no Overworld do Deserto deve apontar para TemploSerpente.");

            // Salvar a Cena
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }
            
            bool saveResult = EditorSceneManager.SaveScene(newScene, scenePath);
            if (saveResult)
            {
                Debug.Log($"[Gerador] Cena salva em {scenePath}");
                
                // Adicionar ao Build Settings se não existir
                var originalScenes = EditorBuildSettings.scenes;
                if (!originalScenes.Any(s => s.path == scenePath))
                {
                    var newScenes = new EditorBuildSettingsScene[originalScenes.Length + 1];
                    System.Array.Copy(originalScenes, newScenes, originalScenes.Length);
                    newScenes[originalScenes.Length] = new EditorBuildSettingsScene(scenePath, true);
                    EditorBuildSettings.scenes = newScenes;
                    Debug.Log("[Gerador] Cena adicionada ao Build Settings.");
                }
            }
            else
            {
                Debug.LogError("[Gerador] Falha ao salvar a cena.");
            }
        }
    }
}
