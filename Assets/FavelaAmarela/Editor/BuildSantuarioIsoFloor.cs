using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Substitui o piso retangular placeholder do Santuário de Yhtill
    /// (um <c>SpriteRenderer</c> liso, sem nenhuma pista visual de ângulo) por um chão em
    /// Tilemap isométrico de losango 2:1 — a mesma receita já usada no Deserto de Hali e na
    /// Tumba de Alhazred (confirmada na cena, não no código desatualizado de
    /// <c>BuildDesertTilemap</c>): <c>Grid</c> com <c>cellSize (1, 0.5, 1)</c> e
    /// <c>cellLayout Isometric</c>, um <c>Tilemap</c> de chão (sortingOrder bem atrás de
    /// tudo) e um <c>Tilemap</c> "Colisao" com <c>TilemapCollider2D</c> nas células de
    /// borda. Documentado em <c>Docs/KnowledgeBundle/systems/tilemap_isometrico_losango.md</c>.
    ///
    /// <para><b>Sem arte de piso ainda</b> (2026-08-02): o tile é um losango de cor sólida
    /// gerado por código, na paleta "calcário frio" já usada — mesma pendência de arte que o
    /// piso antigo já tinha, só que agora com a forma isométrica certa em vez de um
    /// retângulo liso.</para>
    ///
    /// <para>Desativa (não deleta) o <c>Piso</c> e as 4 <c>Parede_*</c> antigas — reversível,
    /// mesmo padrão de <c>BuildIsoCollisionFromFloor</c> ao aposentar o blockout antigo.</para>
    ///
    /// <para>Idempotente: reaproveita o Grid/Tilemap/tile se já existirem.</para>
    /// </summary>
    public static class BuildSantuarioIsoFloor
    {
        private const string CenaSantuario = "Assets/Scenes/Santuario_Yhtill.unity";
        private const string PastaTiles = "Assets/FavelaAmarela/Art/Tiles";
        private const string CaminhoPngPiso = PastaTiles + "/santuario_piso_placeholder.png";
        private const string CaminhoTileAsset = PastaTiles + "/santuario_piso_placeholder.asset";
        private const string CaminhoTileColisao = PastaTiles + "/colisao_invisivel.asset";

        // Com cellSize (1, 0.5) e um bloco quadrado de N células por eixo em grid-space, o
        // losango em mundo sai (2N largura x N altura) — meia-altura = N/2. As posições já
        // fixadas na cena vão até y=-4,8 (Saida_Santuario); a ponta do losango é um único
        // ponto sem área, então a meia-altura precisa passar bem disso, não só alcançar.
        // N=28 → meia-altura 7, meia-largura 14: em y=-4,8 ainda sobram ~8,8 de largura.
        private const int MetadeLado = 14; // grid de -14..13 em X e Y (N=28)

        private static readonly Color CorCalcario = new Color(0.72f, 0.69f, 0.62f, 1f);

        [MenuItem("Tools/FavelaAmarela/Build Santuario Iso Floor")]
        public static void Build()
        {
            var cenaAtiva = EditorSceneManager.GetActiveScene();
            if (cenaAtiva.isDirty && !string.IsNullOrEmpty(cenaAtiva.path))
                EditorSceneManager.SaveScene(cenaAtiva);
            string cenaOriginal = cenaAtiva.path;

            var cena = EditorSceneManager.OpenScene(CenaSantuario, OpenSceneMode.Single);

            var tilePiso = GarantirTilePiso();
            var tileColisao = AssetDatabase.LoadAssetAtPath<TileBase>(CaminhoTileColisao);
            if (tileColisao == null)
            {
                Debug.LogError($"[SantuarioIsoFloor] Tile de colisão não encontrado em " +
                                $"{CaminhoTileColisao}. Rode 'Build Iso Collision From Floor' " +
                                "no Deserto ao menos uma vez antes (ele cria esse asset).");
                return;
            }

            var grid = GarantirGrid();
            var floorTilemap = GarantirTilemapDeChao(grid);
            PintarChao(floorTilemap, tilePiso);
            GerarColisao(grid, floorTilemap, tileColisao);
            DesativarPisoEParedesAntigas();

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);

            if (!string.IsNullOrEmpty(cenaOriginal) && cenaOriginal != CenaSantuario)
                EditorSceneManager.OpenScene(cenaOriginal, OpenSceneMode.Single);

            Debug.Log("[SantuarioIsoFloor] Chão isométrico de losango pronto no Santuário.");
        }

        // ── Tile de piso (placeholder gerado por código) ────────────────────────

        private static TileBase GarantirTilePiso()
        {
            var existente = AssetDatabase.LoadAssetAtPath<Tile>(CaminhoTileAsset);
            if (existente != null && existente.sprite != null) return existente;

            if (AssetDatabase.LoadAssetAtPath<Sprite>(CaminhoPngPiso) == null)
                GerarPngDoLosango();

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CaminhoPngPiso);
            var tile = existente != null ? existente : ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.None; // colisão vem do tilemap "Colisao" à parte

            if (existente == null) AssetDatabase.CreateAsset(tile, CaminhoTileAsset);
            EditorUtility.SetDirty(tile);
            AssetDatabase.SaveAssets();
            return tile;
        }

        /// <summary>
        /// Desenha um losango 32×16px (proporção 2:1, mesma da célula isométrica em mundo:
        /// 1 × 0.5 a 32 PPU) preenchido na cor "calcário frio", transparente fora da forma.
        /// </summary>
        private static void GerarPngDoLosango()
        {
            const int w = 32, h = 16;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var transparente = new Color(0f, 0f, 0f, 0f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x + 0.5f) - w / 2f;
                    float dy = (y + 0.5f) - h / 2f;
                    bool dentro = Mathf.Abs(dx) / (w / 2f) + Mathf.Abs(dy) / (h / 2f) <= 1f;
                    tex.SetPixel(x, y, dentro ? CorCalcario : transparente);
                }
            }
            tex.Apply();

            Directory.CreateDirectory(PastaTiles);
            File.WriteAllBytes(CaminhoPngPiso, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(CaminhoPngPiso, ImportAssetOptions.ForceUpdate);
            ConfigurarImportPixelArt(CaminhoPngPiso);
        }

        private static void ConfigurarImportPixelArt(string caminho)
        {
            if (!(AssetImporter.GetAtPath(caminho) is TextureImporter importer)) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spritePivot = new Vector2(0.5f, 0.5f);
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        // ── Grid + Tilemap de chão ───────────────────────────────────────────────

        private static Grid GarantirGrid()
        {
            var go = GameObject.Find("SantuarioFloorGrid");
            if (go == null) go = new GameObject("SantuarioFloorGrid", typeof(Grid));

            var grid = go.GetComponent<Grid>();
            grid.cellSize = new Vector3(1f, 0.5f, 1f);
            grid.cellLayout = GridLayout.CellLayout.Isometric;
            return grid;
        }

        private static Tilemap GarantirTilemapDeChao(Grid grid)
        {
            var tf = grid.transform.Find("SantuarioFloor");
            var go = tf != null ? tf.gameObject
                : new GameObject("SantuarioFloor", typeof(Tilemap), typeof(TilemapRenderer));
            if (tf == null) go.transform.SetParent(grid.transform, false);

            go.GetComponent<TilemapRenderer>().sortingOrder = -1000; // atrás de tudo, igual ao Deserto/Tumba
            return go.GetComponent<Tilemap>();
        }

        private static void PintarChao(Tilemap tilemap, TileBase tile)
        {
            tilemap.ClearAllTiles();
            for (int gx = -MetadeLado; gx < MetadeLado; gx++)
                for (int gy = -MetadeLado; gy < MetadeLado; gy++)
                    tilemap.SetTile(new Vector3Int(gx, gy, 0), tile);
        }

        // ── Colisão (borda a partir do chão pintado) ─────────────────────────────

        private static void GerarColisao(Grid grid, Tilemap floorTilemap, TileBase tileColisao)
        {
            var floorCells = new HashSet<Vector3Int>();
            floorTilemap.CompressBounds();
            foreach (var cell in floorTilemap.cellBounds.allPositionsWithin)
                if (floorTilemap.HasTile(cell)) floorCells.Add(cell);

            var wallCells = new HashSet<Vector3Int>();
            foreach (var c in floorCells)
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        var n = new Vector3Int(c.x + dx, c.y + dy, c.z);
                        if (!floorCells.Contains(n)) wallCells.Add(n);
                    }

            var colTf = grid.transform.Find("Colisao");
            if (colTf != null) Object.DestroyImmediate(colTf.gameObject);
            var colGO = new GameObject("Colisao", typeof(Tilemap));
            colGO.transform.SetParent(grid.transform, false);

            var colisor = colGO.GetComponent<TilemapCollider2D>();
            if (colisor == null) colisor = colGO.AddComponent<TilemapCollider2D>();

            var colTilemap = colGO.GetComponent<Tilemap>();
            foreach (var w in wallCells)
                colTilemap.SetTile(w, tileColisao);

            // Depois de pintar: o Composite mescla o que EXISTE, e a extrusão só vale para
            // célula já posta. Camada, corpo estático e composite vêm de um lugar só.
            ConsolidarColisaoDosTilemaps.Padronizar(colisor);
        }

        // ── Aposentar o piso/paredes antigas (reversível) ────────────────────────

        private static void DesativarPisoEParedesAntigas()
        {
            DesativarSeExistir("Piso");
            DesativarSeExistir("Parede_Norte");
            DesativarSeExistir("Parede_Sul");
            DesativarSeExistir("Parede_Leste");
            DesativarSeExistir("Parede_Oeste");
        }

        private static void DesativarSeExistir(string nome)
        {
            var go = GameObject.Find(nome);
            if (go != null) go.SetActive(false);
        }
    }
}
