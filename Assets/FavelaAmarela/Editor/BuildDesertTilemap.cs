using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Utilitário de Editor (Parte B da expansão da Fase 1): monta o chão-tilemap do
    /// deserto. Cria os 5 Tile assets a partir dos sprites de areia, um Grid+Tilemap
    /// retangular (cellSize 1×1) com sortingOrder bem atrás das paredes, pinta o piso
    /// sobre o retângulo de cada `Floor` gerado (misturando bases + acentos esparsos) e
    /// oculta os SpriteRenderers dos Floors placeholder. Idempotente/re-executável.
    /// </summary>
    public static class BuildDesertTilemap
    {
        private const string TileDir = "Assets/FavelaAmarela/Art/Tiles";
        private static readonly string[] TileNames = { "sand_01", "sand_02", "sand_03", "sand_crack", "sand_pebbles" };

        [MenuItem("Tools/FavelaAmarela/Build Desert Tilemap")]
        public static void Build()
        {
            // 1. Tile assets a partir dos sprites importados.
            var tiles = new TileBase[TileNames.Length];
            for (int i = 0; i < TileNames.Length; i++)
            {
                string pngPath = $"{TileDir}/{TileNames[i]}.png";
                string tilePath = $"{TileDir}/{TileNames[i]}.asset";

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
                if (sprite == null)
                {
                    Debug.LogError($"[DesertTilemap] Sprite não encontrado em '{pngPath}' (import pendente? Assets/Refresh).");
                    return;
                }

                var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    AssetDatabase.CreateAsset(tile, tilePath);
                }
                tile.sprite = sprite;
                EditorUtility.SetDirty(tile);
                tiles[i] = tile;
            }
            AssetDatabase.SaveAssets();

            // 2. Grid + Tilemap (retangular; atrás de tudo).
            var gridGO = GameObject.Find("DesertFloorGrid") ?? new GameObject("DesertFloorGrid", typeof(Grid));
            gridGO.GetComponent<Grid>().cellSize = new Vector3(1f, 1f, 0f);

            var tmTf = gridGO.transform.Find("DesertFloor");
            var tmGO = tmTf != null
                ? tmTf.gameObject
                : new GameObject("DesertFloor", typeof(Tilemap), typeof(TilemapRenderer));
            if (tmTf == null) tmGO.transform.SetParent(gridGO.transform, false);

            var tilemap = tmGO.GetComponent<Tilemap>();
            tmGO.GetComponent<TilemapRenderer>().sortingOrder = -1000; // atrás das paredes (-y*10) e do resto
            tilemap.ClearAllTiles();

            // 3. Pinta sobre cada Floor gerado + oculta o sprite placeholder.
            var rnd = new System.Random(2024);
            int painted = 0, floors = 0;
            foreach (var tf in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (tf.name != "Floor") continue;
                floors++;

                Vector2 c = tf.position;
                Vector2 size = tf.lossyScale;
                int minX = Mathf.FloorToInt(c.x - size.x * 0.5f);
                int maxX = Mathf.CeilToInt(c.x + size.x * 0.5f);
                int minY = Mathf.FloorToInt(c.y - size.y * 0.5f);
                int maxY = Mathf.CeilToInt(c.y + size.y * 0.5f);

                for (int y = minY; y < maxY; y++)
                {
                    for (int x = minX; x < maxX; x++)
                    {
                        double r = rnd.NextDouble();
                        int idx = r < 0.05 ? 3 : (r < 0.12 ? 4 : rnd.Next(0, 3)); // fenda 5%, seixos 7%, resto bases
                        tilemap.SetTile(new Vector3Int(x, y, 0), tiles[idx]);
                        painted++;
                    }
                }

                var sr = tf.GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = false; // esconde o Floor placeholder (reversível)
            }

            EditorSceneManager.MarkSceneDirty(gridGO.scene);
            EditorSceneManager.SaveScene(gridGO.scene);
            Debug.Log($"[DesertTilemap] {painted} tiles pintados sobre {floors} chãos; Floors placeholder ocultados.");
        }
    }
}
