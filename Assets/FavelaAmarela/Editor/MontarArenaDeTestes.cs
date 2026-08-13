using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Tilemaps;
using FavelaAmarela.CameraSystem;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.GameLoop;

namespace FavelaAmarela.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor. Cria a <b>Cena_ArenaDeTestes</b>: chão neutro, <c>GameManager</c>,
    /// câmera isométrica, Damião e HUD completo — uma cena mínima e genérica onde qualquer
    /// chefe pode ser invocado via <see cref="CarcosaDebuggerWindow"/> e testado antes de a
    /// fase real existir.
    ///
    /// <para><b>Por que existe separada das fases:</b> decisão explícita do Vini — testar
    /// lutas não deveria depender de level design real (Castelo, Trono de Aldebaran) ainda não
    /// construído. Esta cena fica vazia de conteúdo de fase de propósito; quem povoa é o
    /// Debugger, em Play Mode, sob demanda.</para>
    ///
    /// <para><b>Nunca entra no Build Settings</b> — é ferramenta de desenvolvimento, não
    /// conteúdo de jogo. Diferente de <c>MontarCenaDeMenu</c>, esta ferramenta
    /// deliberadamente NÃO chama <c>EditorBuildSettings.scenes</c>.</para>
    ///
    /// <para>Idempotente: refaz a cena do zero a cada execução.</para>
    /// </summary>
    public static class MontarArenaDeTestes
    {
        private const string CaminhoDaCena = "Assets/Scenes/Cena_ArenaDeTestes.unity";
        private const string PrefabDamiao = "Assets/FavelaAmarela/Art/Characters/Damiao/Player_Damiao.prefab";

        private const string PastaDeTiles = "Assets/FavelaAmarela/Art/Tiles";
        private const string CaminhoPngDaArena = PastaDeTiles + "/arena_piso_placeholder.png";
        private const string CaminhoTileDaArena = PastaDeTiles + "/arena_piso_placeholder.asset";
        private const string CaminhoTileDeColisao = PastaDeTiles + "/arena_colisao.asset";

        /// <summary>
        /// Metade do lado do bloco de células. 32 → losango de <b>64 × 32</b> unidades em mundo
        /// (dobrado a pedido em 2026-08-13; era 16, que dava 32 × 16 e ficava apertado para o
        /// rasante do Byakhee atravessar).
        /// </summary>
        private const int MetadeLadoDoChao = 32;

        private static readonly Color CorDoChaoNeutro = new Color(0.22f, 0.21f, 0.20f, 1f);

        [MenuItem("Tools/FavelaAmarela/Montar Arena de Testes")]
        public static void Executar()
        {
            var atual = EditorSceneManager.GetActiveScene();
            if (atual.isDirty && !string.IsNullOrEmpty(atual.path))
                EditorSceneManager.SaveScene(atual);

            var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Montar();

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena, CaminhoDaCena);

            Debug.Log($"[ArenaDeTestes] Pronto — '{CaminhoDaCena}' montada. NÃO foi adicionada " +
                      "ao Build Settings (é ferramenta de dev). Dê Play e abra " +
                      "'Tools/FavelaAmarela/Carcosa Debugger' para invocar um chefe.");
        }

        private static void Montar()
        {
            MontarChao();

            new GameObject("GameManager", typeof(GameManager));

            var damiao = InstanciarDamiao();
            MontarCamera(damiao);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            // HUD completo: BuildHUDCompleto é o único ponto de montagem desde 2026-08-13 — ele
            // já monta as seis views (inclusive Vigor e Artefatos, que só a Arena tinha antes)
            // mais a barra de itens e o painel de inventário. Nada de HUD "da Arena" separado
            // do HUD do jogo.
            BuildHUDCompleto.Build();
        }

        // ── Chão ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Chão em <b>losango isométrico 2:1</b>, a mesma receita de
        /// <c>BuildSantuarioIsoFloor</c>: <c>Grid</c> com <c>cellSize (1, 0.5, 1)</c> e
        /// <c>cellLayout Isometric</c>, mais um <c>Tilemap</c> bem atrás de tudo.
        ///
        /// <para><b>Era um quadrado plano até 2026-08-12</b> — um único <c>SpriteRenderer</c>
        /// escalado, sem Grid nenhum. Testar luta num chão que não é o do jogo engana a leitura
        /// de distância e de profundidade: o que parece alcance de garras na arena não é o mesmo
        /// alcance no Deserto, porque uma unidade em Y vale metade de uma em X no losango.</para>
        ///
        /// <para><b>Com borda de colisão</b> desde o playtest de 2026-08-12: a arena não tinha
        /// colisor nenhum, então jogador e chefe saíam andando para fora do chão. A borda é
        /// gerada a partir das células vizinhas do piso, a mesma receita do
        /// <c>BuildSantuarioIsoFloor</c>.</para>
        /// </summary>
        private static void MontarChao()
        {
            var tile = GarantirTileDoLosango();

            var gridGO = new GameObject("ArenaFloorGrid", typeof(Grid));
            var grid = gridGO.GetComponent<Grid>();
            grid.cellSize = new Vector3(1f, 0.5f, 1f);
            grid.cellLayout = GridLayout.CellLayout.Isometric;

            var pisoGO = new GameObject("ArenaFloor", typeof(Tilemap), typeof(TilemapRenderer));
            pisoGO.transform.SetParent(gridGO.transform, false);
            pisoGO.GetComponent<TilemapRenderer>().sortingOrder = -1000;

            // Com cellSize (1, 0.5), um bloco de N células por eixo vira um losango de
            // (2N largura × N altura) em mundo. MetadeLado 16 → meia-largura 16, meia-altura 8,
            // cobrindo com folga o raio de 12 que o quadrado antigo tinha.
            var tilemap = pisoGO.GetComponent<Tilemap>();
            for (int gx = -MetadeLadoDoChao; gx < MetadeLadoDoChao; gx++)
                for (int gy = -MetadeLadoDoChao; gy < MetadeLadoDoChao; gy++)
                    tilemap.SetTile(new Vector3Int(gx, gy, 0), tile);

            MontarBordaDeColisao(grid);
        }

        /// <summary>
        /// Anel de células invisíveis em volta do piso, com <c>TilemapCollider2D</c>. Segura o
        /// jogador e o chefe dentro do losango sem desenhar parede — a arena continua legível
        /// de cima, mas deixa de ser um plano infinito.
        ///
        /// <para><b>Usa um tile próprio, com <c>colliderType Grid</c>.</b> A primeira versão
        /// reaproveitou o tile do piso, que tem <c>colliderType None</c> — e o
        /// <c>TilemapCollider2D</c> gera a geometria <b>a partir do colliderType dos tiles</b>,
        /// então não gerava nada. O colisor existia na cena e não colidia com coisa alguma
        /// (playtest de 2026-08-13).</para>
        /// </summary>
        private static void MontarBordaDeColisao(Grid grid)
        {
            var tileColisao = GarantirTileDeColisao();

            var colGO = new GameObject("Colisao", typeof(Tilemap), typeof(TilemapRenderer));
            colGO.transform.SetParent(grid.transform, false);

            // Invisível: o renderer existe porque o Tilemap exige um, mas não desenha nada.
            colGO.GetComponent<TilemapRenderer>().enabled = false;

            var colisao = colGO.GetComponent<Tilemap>();
            const int borda = MetadeLadoDoChao;

            // Anel de duas células: com uma só, um ator rápido (o rasante do Byakhee) pode
            // atravessar entre dois FixedUpdate mesmo com Continuous.
            for (int gx = -borda - 2; gx <= borda + 1; gx++)
                for (int gy = -borda - 2; gy <= borda + 1; gy++)
                {
                    bool dentroDoPiso = gx >= -borda && gx < borda && gy >= -borda && gy < borda;
                    if (!dentroDoPiso) colisao.SetTile(new Vector3Int(gx, gy, 0), tileColisao);
                }

            colGO.AddComponent<TilemapCollider2D>();
        }

        /// <summary>
        /// Tile sem sprite e com <c>colliderType Grid</c>: não desenha nada, mas é o que faz o
        /// <c>TilemapCollider2D</c> gerar geometria. Mesma função do
        /// <c>colisao_invisivel.asset</c> que o Santuário usa.
        /// </summary>
        private static TileBase GarantirTileDeColisao()
        {
            var existente = AssetDatabase.LoadAssetAtPath<Tile>(CaminhoTileDeColisao);
            if (existente != null) return existente;

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = null;
            tile.colliderType = Tile.ColliderType.Grid;

            Directory.CreateDirectory(PastaDeTiles);
            AssetDatabase.CreateAsset(tile, CaminhoTileDeColisao);
            AssetDatabase.SaveAssets();
            return tile;
        }

        /// <summary>
        /// Tile de piso da arena. Gera o PNG do losango 32×16 (proporção 2:1, a mesma da célula
        /// em mundo a 32 PPU) na primeira execução e reaproveita depois.
        /// </summary>
        private static TileBase GarantirTileDoLosango()
        {
            var existente = AssetDatabase.LoadAssetAtPath<Tile>(CaminhoTileDaArena);
            if (existente != null && existente.sprite != null) return existente;

            if (AssetDatabase.LoadAssetAtPath<Sprite>(CaminhoPngDaArena) == null)
                GerarPngDoLosango();

            var tile = existente != null ? existente : ScriptableObject.CreateInstance<Tile>();
            tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CaminhoPngDaArena);
            tile.colliderType = Tile.ColliderType.None;

            if (existente == null) AssetDatabase.CreateAsset(tile, CaminhoTileDaArena);
            EditorUtility.SetDirty(tile);
            AssetDatabase.SaveAssets();
            return tile;
        }

        private static void GerarPngDoLosango()
        {
            const int w = 32, h = 16;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var transparente = new Color(0f, 0f, 0f, 0f);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x + 0.5f) - w / 2f;
                    float dy = (y + 0.5f) - h / 2f;
                    bool dentro = Mathf.Abs(dx) / (w / 2f) + Mathf.Abs(dy) / (h / 2f) <= 1f;
                    tex.SetPixel(x, y, dentro ? CorDoChaoNeutro : transparente);
                }
            tex.Apply();

            Directory.CreateDirectory(PastaDeTiles);
            File.WriteAllBytes(CaminhoPngDaArena, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(CaminhoPngDaArena, ImportAssetOptions.ForceUpdate);
            ConfigurarImportPixelArt(CaminhoPngDaArena);
        }

        /// <summary>PPU 32, Point, sem compressão — skill <c>favela-pixelart-standards</c>.</summary>
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
            importer.SaveAndReimport();
        }

        // ── Damião e câmera ──────────────────────────────────────────────────

        private static GameObject InstanciarDamiao()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDamiao);
            if (prefab == null)
            {
                Debug.LogError($"[ArenaDeTestes] Prefab do Damião não encontrado em '{PrefabDamiao}'.");
                return null;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = "Player_Damiao";
            go.transform.position = Vector3.zero;

            GarantirSistemasDoJogador(go);
            return go;
        }

        /// <summary>
        /// Acrescenta os componentes que o prefab do Damião <b>não</b> traz mas que as cenas
        /// reais adicionam por wiring (<c>LigarSistemasNovos</c>).
        ///
        /// <para><b>Playtest de 2026-08-12:</b> sem a <c>ArtefatosBridge</c> não havia como
        /// equipar artefato nenhum na Arena — o Carcosa Debugger concede no
        /// <c>ArtefatosBridge</c> do jogador, e aqui não existia nenhum. Deserto, Playtest e
        /// Santuário tinham; só a cena de teste ficou de fora, justamente a cena onde os
        /// chefes são testados. Sem o <c>GerenciadorDeVigor</c>, a Esquiva não tem recurso
        /// para cobrar — e esquivar é a única defesa em luta de chefe.</para>
        /// </summary>
        private static void GarantirSistemasDoJogador(GameObject damiao)
        {
            if (damiao.GetComponent<ArtefatosBridge>() == null)
                damiao.AddComponent<ArtefatosBridge>();

            if (damiao.GetComponent<GerenciadorDeVigor>() == null)
                damiao.AddComponent<GerenciadorDeVigor>();
        }

        private static void MontarCamera(GameObject damiao)
        {
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(IsometricCameraController));
            camGo.tag = "MainCamera";
            camGo.transform.rotation = Quaternion.identity; // sem tilt — ver favela-isometric-standards

            if (damiao != null)
                camGo.GetComponent<IsometricCameraController>().SetTarget(damiao.transform);
        }

    }
}
