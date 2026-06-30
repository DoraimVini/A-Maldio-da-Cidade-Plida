# Contexto do Projeto: A Maldição da Cidade Pálida
**Foco atual:** Sistema de Level Blockout ("Ruínas Pálidas")

Este documento contém os scripts de geração procedural/data-driven do blockout de nível, conhecido como "S-Path" das Ruínas Pálidas. A arquitetura atual separa a lógica de instanciação de objetos do Unity (MonoBehaviour) da definição matemática e estrutural do layout (POCO).

## Arquivos Relevantes

### 1. `LevelBlockoutGenerator.cs`
**Caminho:** `Assets/Scripts/Level/LevelBlockoutGenerator.cs`
**Responsabilidade:** Componente MonoBehaviour que lê os dados de `LevelLayoutData` e instancia os GameObjects no Unity (paredes, chão, casas) utilizando física e renderização 2D (SpriteRenderer + BoxCollider2D).

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace FavelaAmarela.Level
{
    /// <summary>
    /// Editor utility: Generates the "S-Path" blockout for the Ruínas Pálidas level.
    /// The Ruínas Pálidas (Ruins of Hali) are a specific location within the
    /// Cidade Pálida (Carcosa) — the overarching dimensional setting of the game.
    ///
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
        [SerializeField] private float z1Width = 8.0f;

        [Header("Zone 2: Vila das Casas (South)")]
        [SerializeField] private float z2Length = 18.0f;
        [SerializeField] private float z2Width = 14.0f;

        [Header("Zone 3: Beco do Vento (West)")]
        [SerializeField] private float z3Length = 15.0f;
        [SerializeField] private float z3Width = 6.0f;

        [Header("Zone 4: Praça do Cerco (South)")]
        [SerializeField] private float z4Length = 16.0f;
        [SerializeField] private float z4Width = 16.0f;

        [Header("House Settings")]
        [SerializeField] private float houseSize = 4.0f;
        [SerializeField] private float doorGap = 2.0f;
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

            // TODO: Adicionar zona de transição dimensional (Salto Dimensional) — ver GDD seção 5

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

            // Fix W1: Use Texture2D.whiteTexture to avoid leaking custom textures on domain reload
            whitePixelSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 1f);

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
            return $"Wall_{localPos.x:F0}_{localPos.y:F0}";
        }
    }
}
```

### 2. `LevelLayoutData.cs`
**Caminho:** `Assets/Scripts/Level/LevelLayoutData.cs`
**Responsabilidade:** Classe POCO (Pure Old C# Object) que retém toda a representação de dados, cálculos e geometria do layout "S-Path". É puramente matemática e totalmente testável sem precisar rodar ou referenciar a API do Unity.

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace FavelaAmarela.Level
{
    /// <summary>
    /// POCO: Pure data representation of the S-Path level layout for the Ruínas Pálidas
    /// (Ruins of Hali), a location within the Cidade Pálida (Carcosa).
    /// Contains zero Unity API calls for object creation — only math and data.
    /// Fully unit-testable without a Unity scene.
    /// </summary>
    public class LevelLayoutData
    {
        // --- Data records ---

        public enum Side { North, South, East, West }

        /// <summary>
        /// Represents a single rectangular wall segment.
        /// Position is world-space center; Size is full width/height.
        /// </summary>
        public readonly struct WallData
        {
            public readonly Vector2 WorldCenter;
            public readonly Vector2 Size;

            public WallData(Vector2 worldCenter, Vector2 size)
            {
                WorldCenter = worldCenter;
                Size = size;
            }

            /// <summary>Returns the axis-aligned bounding box of this wall.</summary>
            public Rect Bounds => new Rect(
                WorldCenter.x - Size.x * 0.5f,
                WorldCenter.y - Size.y * 0.5f,
                Size.x, Size.y);
        }

        /// <summary>
        /// Represents a zone (room) with its walls, floor bounds, and open sides.
        /// </summary>
        public class ZoneData
        {
            public string Name { get; }
            public Vector2 Center { get; }
            public Vector2 Size { get; }
            public HashSet<Side> OpenSides { get; }
            public List<WallData> Walls { get; } = new List<WallData>();
            public Rect FloorBounds { get; }

            public ZoneData(string name, Vector2 center, Vector2 size, HashSet<Side> openSides)
            {
                Name = name;
                Center = center;
                Size = size;
                OpenSides = openSides;
                FloorBounds = new Rect(
                    center.x - size.x * 0.5f,
                    center.y - size.y * 0.5f,
                    size.x, size.y);
            }
        }

        /// <summary>
        /// Represents a house structure with walls and a door gap.
        /// </summary>
        public class HouseData
        {
            public string Name { get; }
            public Vector2 Position { get; }
            public float Size { get; }
            public float DoorGap { get; }
            public List<WallData> Walls { get; } = new List<WallData>();

            public HouseData(string name, Vector2 position, float size, float doorGap)
            {
                Name = name;
                Position = position;
                Size = size;
                DoorGap = doorGap;
            }
        }

        // --- Layout parameters (injected via constructor) ---

        /// <summary>Thickness of the walls.</summary>
        public float WallThickness { get; }

        /// <summary>Length of Zone 1 (Rua de Entrada).</summary>
        public float Z1Length { get; }
        /// <summary>Width of Zone 1 (Rua de Entrada).</summary>
        public float Z1Width { get; }

        /// <summary>Length of Zone 2 (Vila das Casas).</summary>
        public float Z2Length { get; }
        /// <summary>Width of Zone 2 (Vila das Casas).</summary>
        public float Z2Width { get; }

        /// <summary>Length of Zone 3 (Beco do Vento).</summary>
        public float Z3Length { get; }
        /// <summary>Width of Zone 3 (Beco do Vento).</summary>
        public float Z3Width { get; }

        /// <summary>Length of Zone 4 (Praça do Cerco).</summary>
        public float Z4Length { get; }
        /// <summary>Width of Zone 4 (Praça do Cerco).</summary>
        public float Z4Width { get; }

        /// <summary>Size of each square house.</summary>
        public float HouseSize { get; }
        /// <summary>Width of the door gap in each house.</summary>
        public float DoorGap { get; }
        /// <summary>Offsets for house placement within Zone 2.</summary>
        public Vector2[] HouseOffsets { get; }

        // --- Output ---

        public List<ZoneData> Zones { get; } = new List<ZoneData>();
        public List<HouseData> Houses { get; } = new List<HouseData>();

        // --- Constructor ---

        public LevelLayoutData(
            float wallThickness = 0.5f,
            float z1Length = 20f, float z1Width = 8f,
            float z2Length = 18f, float z2Width = 14f,
            float z3Length = 15f, float z3Width = 6f,
            float z4Length = 16f, float z4Width = 16f,
            float houseSize = 4f, float doorGap = 2f,
            Vector2[] houseOffsets = null)
        {
            WallThickness = wallThickness;
            Z1Length = z1Length;
            Z1Width = z1Width;
            Z2Length = z2Length;
            Z2Width = z2Width;
            Z3Length = z3Length;
            Z3Width = z3Width;
            Z4Length = z4Length;
            Z4Width = z4Width;
            HouseSize = houseSize;
            DoorGap = doorGap;
            HouseOffsets = houseOffsets ?? new[]
            {
                new Vector2(3f, -4f),
                new Vector2(9f, -7f),
                new Vector2(5f, -13f)
            };
        }

        // --- Public API ---

        /// <summary>
        /// Computes the full S-Path layout: 4 zones + houses.
        /// Pure math — no Unity objects created.
        /// </summary>
        public void Build()
        {
            Zones.Clear();
            Houses.Clear();

            Vector2 origin = Vector2.zero;

            // --- ZONA 1: Rua de Entrada (runs East → +X) ---
            // Opens East (corridor exit) AND partial South (connection to Z2)
            var z1 = CreateZone("Zona1_RuaEntrada", origin, Z1Length, Z1Width,
                new HashSet<Side> { Side.East, Side.South });
            Zones.Add(z1);

            // --- ZONA 2: Vila das Casas (runs South → -Y) ---
            // Positioned so its top edge aligns with Z1's bottom edge at the East end.
            Vector2 z2Center = new Vector2(
                origin.x + Z1Length - Z2Width * 0.5f,
                origin.y - Z1Width * 0.5f - Z2Length * 0.5f
            );
            var z2 = CreateZone("Zona2_VilaDasCasas", z2Center, Z2Width, Z2Length,
                new HashSet<Side> { Side.North, Side.South });
            Zones.Add(z2);

            // --- Houses inside Vila ---
            Vector2 vilaTopLeft = new Vector2(z2Center.x - Z2Width * 0.5f, z2Center.y + Z2Length * 0.5f);
            foreach (var offset in HouseOffsets)
            {
                string houseName = $"Casa_{Houses.Count + 1}";
                var house = CreateHouse(houseName, vilaTopLeft + offset, HouseSize, DoorGap);
                Houses.Add(house);
            }

            // --- ZONA 3: Beco do Vento (runs West → -X) ---
            // Positioned so its right edge connects to Z2's bottom edge.
            Vector2 z3Center = new Vector2(
                z2Center.x - Z3Length * 0.5f,
                z2Center.y - Z2Length * 0.5f - Z3Width * 0.5f
            );
            var z3 = CreateZone("Zona3_BecoDoVento", z3Center, Z3Length, Z3Width,
                new HashSet<Side> { Side.East, Side.West });
            Zones.Add(z3);

            // --- ZONA 4: Praça do Cerco (dead end South → -Y) ---
            Vector2 z4Center = new Vector2(
                z3Center.x - Z3Length * 0.5f + Z4Width * 0.5f,
                z3Center.y - Z3Width * 0.5f - Z4Length * 0.5f
            );
            var z4 = CreateZone("Zona4_PracaDoCerco", z4Center, Z4Width, Z4Length,
                new HashSet<Side> { Side.North });
            Zones.Add(z4);
        }

        // --- Validation ---

        /// <summary>
        /// Validates that adjacent zones share aligned openings.
        /// Returns a list of error messages (empty = valid).
        /// </summary>
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (Zones.Count < 4)
            {
                errors.Add("Layout has fewer than 4 zones. Call Build() first.");
                return errors;
            }

            // Z1 (South open) → Z2 (North open): Z2's top edge must align with Z1's bottom edge
            ValidateConnection(Zones[0], Side.South, Zones[1], Side.North, errors);

            // Z2 (South open) → Z3 (East open): Z3's right edge must align with Z2's bottom edge
            // Note: Z2 opens South, Z3 opens East — they meet at a corner turn.
            ValidateConnection(Zones[1], Side.South, Zones[2], Side.East, errors);

            // Z3 (West open) → Z4 (North open): Z4's top edge must align with Z3's left edge
            ValidateConnection(Zones[2], Side.West, Zones[3], Side.North, errors);

            // Validate houses are inside Z2 bounds
            foreach (var house in Houses)
            {
                Rect houseBounds = new Rect(
                    house.Position.x - house.Size * 0.5f,
                    house.Position.y - house.Size * 0.5f,
                    house.Size, house.Size);

                if (!Zones[1].FloorBounds.Overlaps(houseBounds))
                {
                    errors.Add($"House '{house.Name}' at {house.Position} is outside Zone 2 bounds.");
                }
            }

            return errors;
        }

        // --- Internal helpers ---

        private ZoneData CreateZone(string name, Vector2 center, float width, float height, HashSet<Side> openSides)
        {
            var zone = new ZoneData(name, center, new Vector2(width, height), openSides);
            float halfW = width * 0.5f;
            float halfH = height * 0.5f;

            if (!openSides.Contains(Side.North))
                zone.Walls.Add(new WallData(
                    center + new Vector2(0f, halfH),
                    new Vector2(width + WallThickness, WallThickness)));

            if (!openSides.Contains(Side.South))
                zone.Walls.Add(new WallData(
                    center + new Vector2(0f, -halfH),
                    new Vector2(width + WallThickness, WallThickness)));

            if (!openSides.Contains(Side.East))
                zone.Walls.Add(new WallData(
                    center + new Vector2(halfW, 0f),
                    new Vector2(WallThickness, height + WallThickness)));

            if (!openSides.Contains(Side.West))
                zone.Walls.Add(new WallData(
                    center + new Vector2(-halfW, 0f),
                    new Vector2(WallThickness, height + WallThickness)));

            return zone;
        }

        private HouseData CreateHouse(string name, Vector2 position, float size, float doorGap)
        {
            var house = new HouseData(name, position, size, doorGap);
            float half = size * 0.5f;

            // 3 solid walls (N, E, W)
            house.Walls.Add(new WallData(position + new Vector2(0f, half), new Vector2(size, WallThickness)));
            house.Walls.Add(new WallData(position + new Vector2(half, 0f), new Vector2(WallThickness, size)));
            house.Walls.Add(new WallData(position + new Vector2(-half, 0f), new Vector2(WallThickness, size)));

            // South wall with door gap (two segments)
            float segmentWidth = (size - doorGap) * 0.5f;
            float segmentOffset = (doorGap + segmentWidth) * 0.5f;

            house.Walls.Add(new WallData(position + new Vector2(-segmentOffset, -half), new Vector2(segmentWidth, WallThickness)));
            house.Walls.Add(new WallData(position + new Vector2(segmentOffset, -half), new Vector2(segmentWidth, WallThickness)));

            return house;
        }

        private void ValidateConnection(ZoneData zoneA, Side sideA, ZoneData zoneB, Side sideB, List<string> errors)
        {
            if (!zoneA.OpenSides.Contains(sideA))
            {
                errors.Add($"'{zoneA.Name}' does not have an opening on {sideA} to connect to '{zoneB.Name}'.");
            }
            if (!zoneB.OpenSides.Contains(sideB))
            {
                errors.Add($"'{zoneB.Name}' does not have an opening on {sideB} to connect to '{zoneA.Name}'.");
            }

            // Check edge alignment: the edges should be adjacent (touching or overlapping)
            float edgeA = GetEdgeCoordinate(zoneA, sideA);
            float edgeB = GetEdgeCoordinate(zoneB, sideB);

            // For S-path turns, edges connect perpendicular — check proximity
            bool isTurn = IsPerpendicularConnection(sideA, sideB);

            if (!isTurn)
            {
                // Parallel connection (e.g., South→North): edges should be at the same coordinate
                float gap = Mathf.Abs(edgeA - edgeB);
                if (gap > WallThickness * 2f)
                {
                    errors.Add($"Gap of {gap:F2} between '{zoneA.Name}'.{sideA} and '{zoneB.Name}'.{sideB} exceeds tolerance.");
                }
            }
        }

        private static float GetEdgeCoordinate(ZoneData zone, Side side)
        {
            return side switch
            {
                Side.North => zone.Center.y + zone.Size.y * 0.5f,
                Side.South => zone.Center.y - zone.Size.y * 0.5f,
                Side.East  => zone.Center.x + zone.Size.x * 0.5f,
                Side.West  => zone.Center.x - zone.Size.x * 0.5f,
                _ => 0f
            };
        }

        private static bool IsPerpendicularConnection(Side a, Side b)
        {
            bool aIsVertical = a == Side.North || a == Side.South;
            bool bIsVertical = b == Side.North || b == Side.South;
            return aIsVertical != bIsVertical;
        }
    }
}
```
