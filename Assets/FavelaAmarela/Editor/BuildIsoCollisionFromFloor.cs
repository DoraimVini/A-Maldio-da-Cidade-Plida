using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Gera o limite físico da dungeon A PARTIR do chão isométrico (tilemap
    /// <c>DesertFloor</c>), no MESMO grid — assim a colisão segue o desenho do chão
    /// em isométrico, em vez do blockout top-down (que vivia num espaço de
    /// coordenadas diferente e não batia com o chão, barrando o jogador nos lugares
    /// errados). Cria um tilemap "Colisao" com um tile de colisão invisível em cada
    /// célula de borda (não-chão 8-adjacente a chão — traça o perímetro externo E os
    /// buracos internos), e desativa o <c>Blockout_Root</c> (paredes + chãos-placeholder
    /// redundantes). A colisão é invisível por ora; a arte de parede iso, quando existir,
    /// é só pintar nesse mesmo tilemap (já alinhada ao chão).
    /// </summary>
    public static class BuildIsoCollisionFromFloor
    {
        private const string FloorTilemapName = "DesertFloor";
        private const string CollisionTilemapName = "Colisao";
        private const string CollisionTilePath = "Assets/FavelaAmarela/Art/Tiles/colisao_invisivel.asset";

        [MenuItem("Tools/FavelaAmarela/Build Iso Collision From Floor")]
        public static void Build()
        {
            var floorGO = GameObject.Find(FloorTilemapName);
            var floorTilemap = floorGO != null ? floorGO.GetComponent<Tilemap>() : null;
            if (floorTilemap == null)
            {
                Debug.LogError($"[IsoCollision] Tilemap '{FloorTilemapName}' não encontrado na cena.");
                return;
            }

            var gridTransform = floorGO.transform.parent; // DesertFloorGrid (mesmo grid iso)
            if (gridTransform == null || gridTransform.GetComponent<Grid>() == null)
            {
                Debug.LogError("[IsoCollision] DesertFloor precisa estar sob um Grid (DesertFloorGrid).");
                return;
            }

            // 1. Células de chão pintadas.
            var floorCells = new HashSet<Vector3Int>();
            floorTilemap.CompressBounds();
            foreach (var cell in floorTilemap.cellBounds.allPositionsWithin)
                if (floorTilemap.HasTile(cell)) floorCells.Add(cell);

            if (floorCells.Count == 0)
            {
                Debug.LogError("[IsoCollision] Nenhuma célula de chão pintada em DesertFloor — nada a cercar.");
                return;
            }

            // 2. Células de borda: não-chão 8-adjacentes a algum chão (o desenho da dungeon).
            var wallCells = new HashSet<Vector3Int>();
            foreach (var c in floorCells)
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        var n = new Vector3Int(c.x + dx, c.y + dy, c.z);
                        if (!floorCells.Contains(n)) wallCells.Add(n);
                    }

            var collisionTile = LoadOrCreateCollisionTile();

            // 3. Tilemap "Colisao" sob o mesmo grid do chão (mesmo espaço isométrico).
            //    Recria do zero: evita herdar componentes de uma execução anterior que falhou.
            var colTf = gridTransform.Find(CollisionTilemapName);
            if (colTf != null) Object.DestroyImmediate(colTf.gameObject);
            var colGO = new GameObject(CollisionTilemapName, typeof(Tilemap));
            colGO.transform.SetParent(gridTransform, false);

            var colTilemap = colGO.GetComponent<Tilemap>();

            // 4. Física: TilemapCollider2D estático — a colisão segue a forma das células
            //    (colliderType Grid = losango iso por célula, contíguo na borda). As células
            //    são mescladas num CompositeCollider2D no passo 6 -- o "polish futuro" que este
            //    comentário prometia em 2026-08-13 e que ficou parado porque o Composite exige
            //    um Rigidbody2D (e o cria Dynamic, o que faria a parede ser empurrável).
            var colisor = colGO.GetComponent<TilemapCollider2D>();
            if (colisor == null) colisor = colGO.AddComponent<TilemapCollider2D>();

            // 5. Pinta o tile de colisão nas células de borda + gera a geometria.
            foreach (var w in wallCells)
                colTilemap.SetTile(w, collisionTile);

            // 6. Camada, corpo estático e CompositeCollider2D. O "polish futuro" prometido no
            //    comentário do passo 4 (o Composite exige Rigidbody2D) está feito: ele existe,
            //    e o Rigidbody2D nasce Static para a parede não ser empurrável.
            ConsolidarColisaoDosTilemaps.Padronizar(colisor);

            // 6. Desativa o blockout (paredes top-down + chãos-placeholder redundantes).
            //    SetActive(false) em vez de deletar: reversível se algo sair errado.
            var blockoutRoot = GameObject.Find("Blockout_Root");
            if (blockoutRoot != null) blockoutRoot.SetActive(false);

            var scene = floorGO.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[IsoCollision] Limite iso gerado: {wallCells.Count} células de borda a partir de {floorCells.Count} de chão. " +
                      $"Blockout {(blockoutRoot != null ? "desativado" : "não encontrado")}. Cena salva.");
        }

        private static TileBase LoadOrCreateCollisionTile()
        {
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(CollisionTilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                // Colisor pela forma da célula (losango iso), independente de sprite — invisível.
                tile.colliderType = Tile.ColliderType.Grid;
                AssetDatabase.CreateAsset(tile, CollisionTilePath);
                AssetDatabase.SaveAssets();
            }
            return tile;
        }
    }
}
