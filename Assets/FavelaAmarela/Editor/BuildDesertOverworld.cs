using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using FavelaAmarela.Level.Core;
using FavelaAmarela.Runtime.GameLoop;
using FavelaAmarela.CameraSystem;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Utilitário de Editor (Fase 1 — overworld): gera do zero a cena
    /// <c>Assets/Scenes/Deserto_Hali.unity</c> — um deserto aberto e caminhável.
    /// Roda o <see cref="DesertOverworldPlanner"/> (POCO) e instancia: câmera que
    /// segue o Damião, GameManager, o prefab do jogador no spawn, o chão-tilemap
    /// de areia, os limites de perímetro sólidos e os marcadores dos pontos de
    /// interesse (a entrada da Tumba de Alhazred vira um <see cref="PortalDeCena"/>
    /// que carrega o S-Path). Idempotente: sobrescreve a cena a cada execução.
    /// </summary>
    public static class BuildDesertOverworld
    {
        private const string ScenePath = "Assets/Scenes/Deserto_Hali.unity";
        private const string PlaytestPath = "Assets/Scenes/Tumba_De_Alhazred.unity";
        private const string PlayerPrefabPath = "Assets/FavelaAmarela/Art/Characters/Damiao/Player_Damiao.prefab";

        private const string TileDir = "Assets/FavelaAmarela/Art/Tiles";
        private static readonly string[] TileNames = { "sand_01", "sand_02", "sand_03", "sand_crack", "sand_pebbles" };

        [MenuItem("Tools/FavelaAmarela/Build Desert Overworld")]
        public static void Build()
        {
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (playerPrefab == null)
            {
                Debug.LogError($"[DesertOverworld] Prefab do jogador não encontrado em '{PlayerPrefabPath}'. Abortado.");
                return;
            }

            var cfg = new DesertOverworldConfig();
            var layout = DesertOverworldPlanner.BuildLayout(cfg);

            // 1. Cena nova, vazia.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 2. Câmera ortográfica que segue o jogador (sem rotação — iso vem do Y-sort).
            var camGO = new GameObject("Main Camera", typeof(Camera), typeof(IsometricCameraController));
            camGO.tag = "MainCamera";
            var cam = camGO.GetComponent<Camera>();
            // Zoom, projeção, rotação e PixelPerfectCamera vêm de UM lugar (PadraoDeCamera).
            // Cada ferramenta tinha o seu número escrito à mão e a cena ficava com o de
            // quem rodasse por último.
            PadraoDeCamera.Aplicar(cam, PadraoDeCamera.AmpliacaoDe(ScenePath));
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.48f, 0.42f, 0.23f, 1f); // céu amarelo-ocre (#7A6A3A aprox.)
            camGO.transform.position = new Vector3(cfg.EntradaOffset.x, cfg.EntradaOffset.y, -10f);
            camGO.transform.rotation = Quaternion.identity;

            // 3. GameManager (bootstrap dos POCOs + Bind do jogador).
            MontarBootstrapDaCena.Garantir();

            // 4. Raiz do deserto + geometria do planner.
            var root = new GameObject("Deserto_Root").transform;

            foreach (var w in layout.Walls)
                SpawnSolidBarrier(w, root, new Color(0.32f, 0.27f, 0.18f, 1f)); // duna/rocha

            // Lago de Hali: barreira interna impassável, preto absoluto.
            if (layout.Lago.HasValue)
                SpawnSolidBarrier(layout.Lago.Value, root, new Color(0.03f, 0.03f, 0.03f, 1f));

            GameObject player = null;
            foreach (var poi in layout.PointsOfInterest)
            {
                if (poi.Kind == PointOfInterestKind.PlayerSpawn)
                {
                    player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
                    player.transform.position = new Vector3(poi.Position.x, poi.Position.y, 0f);
                    player.name = "Player_Damiao";
                }
                else
                {
                    SpawnPointOfInterest(poi, root);
                }
            }

            // 5. Câmera segue o jogador (SerializedObject garante persistência do campo serializado).
            if (player != null)
            {
                var so = new SerializedObject(camGO.GetComponent<IsometricCameraController>());
                so.FindProperty("target").objectReferenceValue = player.transform;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // 6. Chão-tilemap de areia sobre o retângulo do chão.
            PaintSandFloor(cfg);

            // 7. Salva a cena e registra as duas cenas em Build Settings (LoadScene por nome).
            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureScenesInBuild(ScenePath, PlaytestPath);

            Debug.Log($"[DesertOverworld] Cena gerada em '{ScenePath}': {layout.Walls.Count} limites, " +
                      $"{layout.PointsOfInterest.Count} pontos de interesse. Player e câmera prontos.");
        }

        // ── Perímetro ────────────────────────────────────────────────────────

        private static void SpawnSolidBarrier(WallSpec w, Transform parent, Color color)
        {
            var go = new GameObject(w.Name);
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(w.Center.x, w.Center.y, 0f);
            go.transform.localScale = new Vector3(w.Size.x, w.Size.y, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = WhitePixelSprite();
            sr.color = color;
            sr.sortingOrder = Mathf.RoundToInt(-w.Center.y * 10f);

            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one; // escala junto com o Transform

            int obstacleLayer = LayerMask.NameToLayer("Obstacle");
            if (obstacleLayer >= 0) go.layer = obstacleLayer;
            else Debug.LogWarning("[DesertOverworld] Layer 'Obstacle' não existe; barreira ficará na Default.");
        }

        // ── Pontos de interesse ──────────────────────────────────────────────

        private static void SpawnPointOfInterest(PointOfInterestSpec poi, Transform parent)
        {
            var go = new GameObject(poi.Name);
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(poi.Position.x, poi.Position.y, 0f);
            go.transform.localScale = new Vector3(2f, 2f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = WhitePixelSprite();
            sr.color = ColorFor(poi.Kind);
            sr.sortingOrder = Mathf.RoundToInt(-poi.Position.y * 10f);

            // A entrada da Tumba é um portal funcional para o S-Path; os demais são
            // marcadores-placeholder até ganharem sua própria cena/quest.
            if (poi.Kind == PointOfInterestKind.EntradaTumbaAlhazred && !string.IsNullOrEmpty(poi.CenaDestino))
            {
                var col = go.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = Vector2.one;
                go.AddComponent<PortalDeCena>().DefinirCenaDestino(poi.CenaDestino);
            }
        }

        private static Color ColorFor(PointOfInterestKind kind) => kind switch
        {
            PointOfInterestKind.EntradaTumbaAlhazred => new Color(0.55f, 0.12f, 0.10f, 1f), // vermelho-sangue
            PointOfInterestKind.EntradaTemploSerpente => new Color(0.15f, 0.45f, 0.20f, 1f), // verde-serpente
            PointOfInterestKind.SantuarioYhtill => new Color(0.90f, 0.78f, 0.20f, 1f), // dourado (Sinal Amarelo)
            PointOfInterestKind.PortoesDasRuinas => new Color(0.45f, 0.45f, 0.50f, 1f), // pedra dos portões
            _ => Color.white,
        };

        // ── Chão de areia (reaproveita a abordagem do BuildDesertTilemap) ─────

        private static void PaintSandFloor(DesertOverworldConfig cfg)
        {
            var tiles = new TileBase[TileNames.Length];
            for (int i = 0; i < TileNames.Length; i++)
            {
                string pngPath = $"{TileDir}/{TileNames[i]}.png";
                string tilePath = $"{TileDir}/{TileNames[i]}.asset";

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
                if (sprite == null)
                {
                    Debug.LogError($"[DesertOverworld] Sprite de areia ausente em '{pngPath}' (import pendente?). Chão ficará sem tiles.");
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

            // Grid ISOMÉTRICO idêntico ao da dungeon (Tumba_De_Alhazred:
            // m_CellSize {1, 0.5, 1}, m_CellLayout: 2). É daqui que vem a angulação
            // isométrica do chão — losangos 2:1, não quadrados top-down.
            var gridGO = new GameObject("DesertFloorGrid", typeof(Grid));
            var grid = gridGO.GetComponent<Grid>();
            grid.cellLayout = GridLayout.CellLayout.Isometric;
            grid.cellSize = new Vector3(1f, 0.5f, 1f);

            var tmGO = new GameObject("DesertFloor", typeof(Tilemap), typeof(TilemapRenderer));
            tmGO.transform.SetParent(gridGO.transform, false);
            var tilemap = tmGO.GetComponent<Tilemap>();
            tmGO.GetComponent<TilemapRenderer>().sortingOrder = -1000; // atrás de tudo

            // Faixa de células que cobre o retângulo de jogo: converte os 4 cantos do
            // mundo para célula (no grid iso, um bloco retangular de células vira o
            // losango de chão que preenche a área de jogo).
            float halfW = cfg.Width * 0.5f, halfH = cfg.Height * 0.5f;
            var corners = new[]
            {
                new Vector3(-halfW, -halfH, 0f), new Vector3(halfW, -halfH, 0f),
                new Vector3(-halfW,  halfH, 0f), new Vector3(halfW,  halfH, 0f),
            };
            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            foreach (var c in corners)
            {
                var cell = grid.WorldToCell(c);
                minX = Mathf.Min(minX, cell.x); maxX = Mathf.Max(maxX, cell.x);
                minY = Mathf.Min(minY, cell.y); maxY = Mathf.Max(maxY, cell.y);
            }

            var rnd = new System.Random(2026);
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    // Recorta ao retângulo do mapa: só pinta a célula cujo centro cai
                    // dentro dos limites — assim o chão preenche exatamente a área
                    // cercada pelo perímetro (a parede envolve o mapa inteiro, sem transbordo).
                    Vector3 wc = tilemap.GetCellCenterWorld(cell);
                    if (Mathf.Abs(wc.x) > halfW || Mathf.Abs(wc.y) > halfH) continue;

                    double r = rnd.NextDouble();
                    int idx = r < 0.05 ? 3 : (r < 0.12 ? 4 : rnd.Next(0, 3)); // fenda 5%, seixos 7%, resto areia
                    tilemap.SetTile(cell, tiles[idx]);
                }
            }
        }

        // ── Build Settings ───────────────────────────────────────────────────

        private static void EnsureScenesInBuild(params string[] paths)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            bool changed = false;
            foreach (var path in paths)
            {
                if (scenes.Any(s => s.path == path)) continue;
                scenes.Add(new EditorBuildSettingsScene(path, true));
                changed = true;
            }
            if (changed) EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ── Sprite utilitário (1×1 branco, Point, PPU 1) ─────────────────────

        private static Sprite _cachedWhite;

        private static Sprite WhitePixelSprite()
        {
            if (_cachedWhite != null) return _cachedWhite;
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _cachedWhite = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), pixelsPerUnit: 1f);
            return _cachedWhite;
        }
    }
}
