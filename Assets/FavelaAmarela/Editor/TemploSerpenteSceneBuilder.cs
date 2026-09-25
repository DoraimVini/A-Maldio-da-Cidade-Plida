using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using FavelaAmarela.Dungeons;

namespace FavelaAmarela.Editor
{
    public class TemploSerpenteSceneBuilder : UnityEditor.EditorWindow
    {
        [MenuItem("Favela Amarela/Dungeons/Gerar Templo Povo Serpente")]
        public static void BuildScene()
        {
            if (!EditorUtility.DisplayDialog("Gerar Cena", "Deseja gerar a cena TemploSerpente agora? Isso criará uma nova cena não salva.", "Sim", "Não"))
                return;

            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            newScene.name = "TemploSerpente";

            // Root Object
            GameObject root = new GameObject("TemploSerpente");
            
            // 1. Grid
            GameObject gridObj = new GameObject("Grid");
            gridObj.transform.SetParent(root.transform);
            Grid grid = gridObj.AddComponent<Grid>();
            grid.cellLayout = GridLayout.CellLayout.IsometricZAsY;
            grid.cellSize = new Vector3(1f, 0.5f, 1f);

            // 1.1 Ground_Base
            GameObject groundBase = CreateTilemap(gridObj, "Ground_Base", "Ground", 0);
            // 1.2 Ground_Decals
            GameObject groundDecals = CreateTilemap(gridObj, "Ground_Decals", "Ground", 1);

            // 1.3 Walls
            GameObject wallsObj = new GameObject("Walls");
            wallsObj.transform.SetParent(gridObj.transform);
            Rigidbody2D wallsRb = wallsObj.AddComponent<Rigidbody2D>();
            wallsRb.bodyType = RigidbodyType2D.Static;
            CompositeCollider2D compCol = wallsObj.AddComponent<CompositeCollider2D>();
            compCol.geometryType = CompositeCollider2D.GeometryType.Polygons;

            // 1.4 Ceilings
            GameObject ceilings = new GameObject("Ceilings");
            ceilings.transform.SetParent(gridObj.transform);

            // 2. Entities
            GameObject entitiesObj = new GameObject("Entities");
            entitiesObj.transform.SetParent(root.transform);
            GameObject enemiesObj = new GameObject("Enemies");
            enemiesObj.transform.SetParent(entitiesObj.transform);
            GameObject npcsObj = new GameObject("NPCs");
            npcsObj.transform.SetParent(entitiesObj.transform);

            // 3. Triggers
            GameObject triggersObj = new GameObject("Triggers");
            triggersObj.transform.SetParent(root.transform);
            
            GameObject tEntrada = new GameObject("Trigger_Entrada_Templo");
            tEntrada.transform.SetParent(triggersObj.transform);
            BoxCollider2D bcEntrada = tEntrada.AddComponent<BoxCollider2D>();
            bcEntrada.isTrigger = true;

            GameObject tSaida = new GameObject("Trigger_Transicao_Saida");
            tSaida.transform.SetParent(triggersObj.transform);
            BoxCollider2D bcSaida = tSaida.AddComponent<BoxCollider2D>();
            bcSaida.isTrigger = true;
            
            GameObject tChefe = new GameObject("Trigger_Chefe");
            tChefe.transform.SetParent(triggersObj.transform);
            BoxCollider2D bcChefe = tChefe.AddComponent<BoxCollider2D>();
            bcChefe.isTrigger = true;
            tChefe.SetActive(false); // Desativado por enquanto, conforme spec

            // 4. SceneSetup
            GameObject setupObj = new GameObject("SceneSetup");
            setupObj.transform.SetParent(root.transform);
            setupObj.AddComponent<TemploSerpenteSetup>();

            EditorUtility.SetDirty(root);
            Debug.Log("[FavelaAmarela] Cena TemploSerpente gerada com sucesso! Salve a cena para continuar.");
        }

        private static GameObject CreateTilemap(GameObject parent, string name, string sortingLayer, int order)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform);
            Tilemap tilemap = obj.AddComponent<Tilemap>();
            TilemapRenderer renderer = obj.AddComponent<TilemapRenderer>();
            
            // Tenta definir a sorting layer se ela existir no projeto
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = order;
            
            return obj;
        }
    }
}
