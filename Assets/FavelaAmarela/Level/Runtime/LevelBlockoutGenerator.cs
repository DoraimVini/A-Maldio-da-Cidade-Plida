using System.Collections.Generic;
using FavelaAmarela.Level.Core;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FavelaAmarela.Level.Runtime
{
    /// <summary>
    /// Camada Runtime: único arquivo que toca GameObject, Transform,
    /// SpriteRenderer e BoxCollider2D. Recebe um LevelBlockoutLayout já
    /// calculado pelo LevelBlockoutPlanner (POCO puro) e instancia a cena.
    ///
    /// Padrões aplicados (favela-isometric-standards + favela-pixelart-standards):
    ///   • SortingOrder automático por Y (paredes mais ao sul ficam na frente).
    ///   • FilterMode.Point nos sprites gerados em runtime (PPU = 16).
    ///   • Nenhum Rigidbody2D nas paredes/chão (objetos estáticos não precisam).
    ///   • BoxCollider2D sólido (isTrigger = false) em todas as paredes.
    ///   • Undo completo via UnityEditor.Undo — Ctrl+Z desfaz o Generate.
    /// </summary>
    [AddComponentMenu("FavelaAmarela/Level/Level Blockout Generator")]
    public sealed class LevelBlockoutGenerator : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────

        [SerializeField] private LevelBlockoutConfig config = new();

        [Header("Cores")]
        [SerializeField] private Color wallColor = new(0.4f, 0.35f, 0.25f, 1f);
        [SerializeField] private Color floorColor = new(0.2f, 0.18f, 0.15f, 0.5f);

        [Header("Runtime")]
        [SerializeField] private Transform generationRoot;

        // ── Editor API ───────────────────────────────────────────────────────

        [ContextMenu("Generate S-Path Blockout")]
        public void GenerateBlockout()
        {
#if UNITY_EDITOR
            Undo.SetCurrentGroupName("Generate S-Path Blockout");
            int undoGroup = Undo.GetCurrentGroup();
#endif
            ClearExisting();
            EnsureRoot();

            var layout = LevelBlockoutPlanner.BuildSPathLayout(config);

            // Agrupa paredes e chãos por sala para criar hierarquia limpa
            var roomRoots = new Dictionary<string, Transform>();

            Transform GetOrCreateRoomRoot(string roomName)
            {
                if (roomRoots.TryGetValue(roomName, out var t)) return t;
                var go = new GameObject(roomName);
                go.transform.SetParent(generationRoot);
                go.transform.localPosition = Vector3.zero;
#if UNITY_EDITOR
                Undo.RegisterCreatedObjectUndo(go, "Generate S-Path Blockout");
#endif
                roomRoots[roomName] = go.transform;
                return go.transform;
            }

            foreach (var w in layout.Walls)
            {
                var parent = GetOrCreateRoomRoot(w.ParentName);
                SpawnWall2D(w.Name, parent, w.Center, w.Size);
            }

            foreach (var f in layout.Floors)
            {
                var parent = GetOrCreateRoomRoot(f.ParentName);
                SpawnFloor2D(f.Name, parent, f.Center, f.Size);
            }

#if UNITY_EDITOR
            Undo.CollapseUndoOperations(undoGroup);
#endif
            Debug.Log($"[LevelBlockout] S-Path gerado: {layout.Walls.Count} paredes, " +
                      $"{layout.Floors.Count} chãos, {layout.Houses.Count} casas.");
        }

        [ContextMenu("Clear Blockout")]
        public void ClearExisting()
        {
            if (generationRoot == null) return;

            var children = new List<GameObject>();
            foreach (Transform child in generationRoot)
                children.Add(child.gameObject);

            foreach (var child in children)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    Undo.DestroyObjectImmediate(child);
                else
#endif
                    Destroy(child);
            }
        }

        // ── Instanciação ─────────────────────────────────────────────────────

        /// <summary>
        /// Parede: SpriteRenderer colorido + BoxCollider2D sólido (não trigger).
        /// SortingOrder calculado por Y (maior Y = mais atrás = menor order),
        /// alinhado com favela-isometric-standards (profundidade automática no Y).
        /// </summary>
        private void SpawnWall2D(string name, Transform parent, Vector2 worldCenter, Vector2 size)
        {
            var go = CreateSpriteObject(name, parent, worldCenter, size, wallColor, isTrigger: false);

            // SortingOrder por Y: paredes ao norte (Y maior) ficam atrás de paredes ao sul
            // Fator 10 para dar resolução suficiente sem overflow no int do SortingOrder
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sortingOrder = Mathf.RoundToInt(-worldCenter.y * 10f);

#if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(go, "Generate S-Path Blockout");
#endif
        }

        /// <summary>
        /// Chão: SpriteRenderer (atrás de tudo, sortingOrder fixo em 0),
        /// sem BoxCollider2D — o jogador anda livremente sobre ele.
        /// </summary>
        private void SpawnFloor2D(string name, Transform parent, Vector2 worldCenter, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(worldCenter.x, worldCenter.y, 0f);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = WhitePixelSprite();
            sr.color = floorColor;
            sr.sortingOrder = 0; // sempre atrás de paredes e sprites de personagem
            // FilterMode.Point garantido pelo WhitePixelSprite()

#if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(go, "Generate S-Path Blockout");
#endif
        }

        private GameObject CreateSpriteObject(string name, Transform parent,
            Vector2 worldCenter, Vector2 size, Color color, bool isTrigger)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(worldCenter.x, worldCenter.y, 0f);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = WhitePixelSprite();
            sr.color = color;
            // FilterMode.Point já definido no sprite; não mexemos aqui

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = isTrigger;
            col.size = Vector2.one; // collider escala junto com o Transform
            col.edgeRadius = 0f;    // sem radius, faz colisão quadrada limpa

            return go;
        }

        private void EnsureRoot()
        {
            if (generationRoot != null) return;
            var rootGo = new GameObject("Blockout_Root");
            rootGo.transform.SetParent(transform);
            rootGo.transform.localPosition = Vector3.zero;
#if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(rootGo, "Generate S-Path Blockout");
#endif
            generationRoot = rootGo.transform;
        }

        // ── Sprite utilitário ────────────────────────────────────────────────

        private static Sprite _cachedWhiteSprite;

        /// <summary>
        /// Sprite de 1×1 pixel branco, gerado uma única vez em runtime.
        /// FilterMode.Point (sem blur) alinhado com favela-pixelart-standards.
        /// PPU = 16 — escala visual correta com o grid isométrico (célula 1.0u).
        /// </summary>
        private static Sprite WhitePixelSprite()
        {
            if (_cachedWhiteSprite != null) return _cachedWhiteSprite;

            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();

            // PPU = 16 para alinhar com favela-pixelart-standards
            _cachedWhiteSprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), pixelsPerUnit: 16f);
            return _cachedWhiteSprite;
        }
    }
}
