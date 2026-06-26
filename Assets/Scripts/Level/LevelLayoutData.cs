using System.Collections.Generic;
using UnityEngine;

namespace FavelaAmarela.Level
{
    /// <summary>
    /// POCO: Pure data representation of the S-Path level layout.
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

        public float WallThickness { get; }

        public float Z1Length { get; }
        public float Z1Width { get; }

        public float Z2Length { get; }
        public float Z2Width { get; }

        public float Z3Length { get; }
        public float Z3Width { get; }

        public float Z4Length { get; }
        public float Z4Width { get; }

        public float HouseSize { get; }
        public float DoorGap { get; }
        public Vector2[] HouseOffsets { get; }

        // --- Output ---

        public List<ZoneData> Zones { get; } = new List<ZoneData>();
        public List<HouseData> Houses { get; } = new List<HouseData>();

        // --- Constructor ---

        public LevelLayoutData(
            float wallThickness = 0.5f,
            float z1Length = 20f, float z1Width = 4f,
            float z2Length = 18f, float z2Width = 14f,
            float z3Length = 15f, float z3Width = 2.5f,
            float z4Length = 12f, float z4Width = 12f,
            float houseSize = 4f, float doorGap = 1.2f,
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
