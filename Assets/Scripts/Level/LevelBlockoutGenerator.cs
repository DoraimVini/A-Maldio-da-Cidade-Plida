using System.Collections.Generic;
using UnityEngine;

namespace FavelaAmarela.Level
{
    /// <summary>
    /// Editor utility: Generates the "S-Path" blockout for the Ruínas Pálidas level.
    /// All walls use SpriteRenderer + BoxCollider2D for proper 2D physics collision.
    /// No 3D primitives (Cube/Quad) are used — everything lives on the XY plane.
    ///
    /// Architecture: This MonoBehaviour is a thin bridge that reads layout data from
    /// <see cref="LevelLayoutData"/> (POCO) and instantiates Unity GameObjects.
    /// All geometric calculations live in the POCO for testability.
    ///
    /// USAGE:
    ///   1. Add this component to an empty GameObject in the scene.
    ///   2. Right-click the component header → "Generate S-Path Blockout".
    ///   3. Walls will be created as children with BoxCollider2D (solid, not triggers).
    /// </summary>
    public class LevelBlockoutGenerator : MonoBehaviour
    {
        [Header("General Settings")]
        [SerializeField] private float wallThickness = 0.5f;
        [SerializeField] private Color wallColor = new Color(0.4f, 0.35f, 0.25f, 1f);     // Dark brown
        [SerializeField] private Color floorColor = new Color(0.2f, 0.18f, 0.15f, 0.5f);   // Dark floor

        [Header("Zone 1: Rua de Entrada (East)")]
        [SerializeField] private float z1Length = 20.0f;
        [SerializeField] private float z1Width = 4.0f;

        [Header("Zone 2: Vila das Casas (South)")]
        [SerializeField] private float z2Length = 18.0f;
        [SerializeField] private float z2Width = 14.0f;

        [Header("Zone 3: Beco do Vento (West)")]
        [SerializeField] private float z3Length = 15.0f;
        [SerializeField] private float z3Width = 2.5f;

        [Header("Zone 4: Praça do Cerco (South)")]
        [SerializeField] private float z4Length = 12.0f;
        [SerializeField] private float z4Width = 12.0f;

        [Header("House Settings")]
        [SerializeField] private float houseSize = 4.0f;
        [SerializeField] private float doorGap = 1.2f;
        [SerializeField] private Vector2[] houseOffsets = new[]
        {
            new Vector2(3f, -4f),
            new Vector2(9f, -7f),
            new Vector2(5f, -13f)
        };

        [Header("Generated Objects Root")]
        [SerializeField] private Transform generationRoot;

        // Instance-level sprite cache — avoids static state issues on domain reload (fixes B2)
        [System.NonSerialized] private Sprite whitePixelSprite;

        // --- Public API (ContextMenu for Editor, callable at Runtime) ---

        [ContextMenu("Generate S-Path Blockout")]
        public void GenerateBlockout()
        {
            ClearExisting();

            if (generationRoot == null)
            {
                var rootGo = new GameObject("Blockout_Root");
                rootGo.transform.SetParent(this.transform);
                generationRoot = rootGo.transform;
            }

            // --- Build layout data via POCO ---
            var layout = new LevelLayoutData(
                wallThickness,
                z1Length, z1Width,
                z2Length, z2Width,
                z3Length, z3Width,
                z4Length, z4Width,
                houseSize, doorGap,
                houseOffsets
            );
            layout.Build();

            // --- Validate layout and log any errors ---
            var errors = layout.Validate();
            foreach (var error in errors)
            {
                Debug.LogError($"[LevelBlockout] Layout validation error: {error}", this);
            }

            // --- Instantiate zones ---
            foreach (var zone in layout.Zones)
            {
                var roomGo = new GameObject(zone.Name);
                roomGo.transform.SetParent(generationRoot);
                roomGo.transform.position = new Vector3(zone.Center.x, zone.Center.y, 0f);

                foreach (var wall in zone.Walls)
                {
                    Vector2 localPos = wall.WorldCenter - zone.Center;
                    SpawnWall2D(wall, roomGo.transform, localPos);
                }

                // Floor (visual only, no collider)
                SpawnFloor2D("Floor", roomGo.transform, Vector2.zero, zone.Size);
            }

            // --- Instantiate houses ---
            foreach (var house in layout.Houses)
            {
                var houseGo = new GameObject(house.Name);
                houseGo.transform.SetParent(generationRoot);
                houseGo.transform.position = new Vector3(house.Position.x, house.Position.y, 0f);

                foreach (var wall in house.Walls)
                {
                    Vector2 localPos = wall.WorldCenter - house.Position;
                    SpawnWall2D(wall, houseGo.transform, localPos);
                }
            }

            Debug.Log("[LevelBlockout] S-Path generated successfully with 4 zones.", this);
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
                    DestroyImmediate(child);
                else
#endif
                    Destroy(child);
            }
        }

        // --- Internal: Wall and Floor builders (Unity-side only) ---

        /// <summary>
        /// Creates a pure 2D wall: SpriteRenderer (white pixel, tinted) + BoxCollider2D.
        /// No 3D primitives. Collider is NOT a trigger — it blocks movement.
        /// </summary>
        private void SpawnWall2D(LevelLayoutData.WallData data, Transform parent, Vector2 localPos)
        {
            // Derive a readable name from the wall's relative position
            string wallName = GetWallName(localPos);

            var wallGo = new GameObject(wallName);
            wallGo.transform.SetParent(parent);
            wallGo.transform.localPosition = new Vector3(localPos.x, localPos.y, 0f);

            // SpriteRenderer for visual
            var sr = wallGo.AddComponent<SpriteRenderer>();
            sr.sprite = GetOrCreateWhitePixelSprite();
            sr.color = wallColor;
            sr.sortingOrder = 1;
            wallGo.transform.localScale = new Vector3(data.Size.x, data.Size.y, 1f);

            // BoxCollider2D for physics collision (NOT trigger)
            var col = wallGo.AddComponent<BoxCollider2D>();
            col.isTrigger = false;
            // Size is (1,1) because the collider scales with the transform
            col.size = new Vector2(1f, 1f);
        }

        private void SpawnFloor2D(string name, Transform parent, Vector2 localPos, Vector2 size)
        {
            var floorGo = new GameObject(name);
            floorGo.transform.SetParent(parent);
            floorGo.transform.localPosition = new Vector3(localPos.x, localPos.y, 0f);

            var sr = floorGo.AddComponent<SpriteRenderer>();
            sr.sprite = GetOrCreateWhitePixelSprite();
            sr.color = floorColor;
            sr.sortingOrder = 0; // Behind walls
            floorGo.transform.localScale = new Vector3(size.x, size.y, 1f);
            // No collider on floor — player walks on it freely
        }

        // --- Sprite cache (instance-level, survives domain reload correctly) ---

        /// <summary>
        /// Gets or creates a 1x1 white pixel sprite for blockout visuals.
        /// Uses an instance field instead of a static field to avoid domain reload issues.
        /// The Unity-overloaded == operator correctly detects destroyed objects here
        /// because Sprite inherits from UnityEngine.Object.
        /// </summary>
        private Sprite GetOrCreateWhitePixelSprite()
        {
            // Unity's overloaded == handles destroyed native objects correctly for instance fields
            if (whitePixelSprite != null) return whitePixelSprite;

            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();

            whitePixelSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

            if (whitePixelSprite == null)
            {
                Debug.LogError("[LevelBlockout] Failed to create white pixel sprite!", this);
            }

            return whitePixelSprite;
        }

        /// <summary>
        /// Derives a wall name from its local position relative to the parent room.
        /// </summary>
        private static string GetWallName(Vector2 localPos)
        {
            if (Mathf.Abs(localPos.x) > Mathf.Abs(localPos.y))
                return localPos.x > 0 ? "Wall_East" : "Wall_West";
            if (Mathf.Abs(localPos.y) > Mathf.Abs(localPos.x))
                return localPos.y > 0 ? "Wall_North" : "Wall_South";
            return "Wall";
        }
    }
}
