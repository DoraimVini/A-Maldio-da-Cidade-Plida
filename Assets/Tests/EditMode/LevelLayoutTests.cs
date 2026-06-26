using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using FavelaAmarela.Level;

namespace FavelaAmarela.Tests
{
    /// <summary>
    /// EditMode unit tests for <see cref="LevelLayoutData"/> POCO.
    /// Validates zone connectivity, wall generation, house placement, and edge cases.
    /// </summary>
    [TestFixture]
    public class LevelLayoutTests
    {
        private LevelLayoutData layout;

        [SetUp]
        public void SetUp()
        {
            layout = new LevelLayoutData();
            layout.Build();
        }

        // --- Basic structure ---

        [Test]
        public void Build_Creates4Zones()
        {
            Assert.AreEqual(4, layout.Zones.Count);
        }

        [Test]
        public void Build_Creates3Houses()
        {
            Assert.AreEqual(3, layout.Houses.Count);
        }

        [Test]
        public void Zone1_NameIsCorrect()
        {
            Assert.AreEqual("Zona1_RuaEntrada", layout.Zones[0].Name);
        }

        [Test]
        public void Zone2_NameIsCorrect()
        {
            Assert.AreEqual("Zona2_VilaDasCasas", layout.Zones[1].Name);
        }

        [Test]
        public void Zone3_NameIsCorrect()
        {
            Assert.AreEqual("Zona3_BecoDoVento", layout.Zones[2].Name);
        }

        [Test]
        public void Zone4_NameIsCorrect()
        {
            Assert.AreEqual("Zona4_PracaDoCerco", layout.Zones[3].Name);
        }

        // --- Zone connectivity (the B3 bug fix) ---

        [Test]
        public void Validate_DefaultLayout_ReturnsNoErrors()
        {
            var errors = layout.Validate();
            Assert.IsEmpty(errors, $"Validation errors: {string.Join("; ", errors)}");
        }

        [Test]
        public void Zone1_HasSouthOpening_ForZ2Connection()
        {
            Assert.IsTrue(layout.Zones[0].OpenSides.Contains(LevelLayoutData.Side.South),
                "Zone 1 must have a South opening to connect to Zone 2.");
        }

        [Test]
        public void Zone1_HasEastOpening()
        {
            Assert.IsTrue(layout.Zones[0].OpenSides.Contains(LevelLayoutData.Side.East),
                "Zone 1 must have an East opening (corridor entrance).");
        }

        [Test]
        public void Zone2_HasNorthOpening_ForZ1Connection()
        {
            Assert.IsTrue(layout.Zones[1].OpenSides.Contains(LevelLayoutData.Side.North),
                "Zone 2 must have a North opening to connect to Zone 1.");
        }

        [Test]
        public void Zone2_HasSouthOpening_ForZ3Connection()
        {
            Assert.IsTrue(layout.Zones[1].OpenSides.Contains(LevelLayoutData.Side.South),
                "Zone 2 must have a South opening to connect to Zone 3.");
        }

        [Test]
        public void Zone3_HasEastOpening_ForZ2Connection()
        {
            Assert.IsTrue(layout.Zones[2].OpenSides.Contains(LevelLayoutData.Side.East),
                "Zone 3 must have an East opening to connect to Zone 2.");
        }

        [Test]
        public void Zone3_HasWestOpening_ForZ4Connection()
        {
            Assert.IsTrue(layout.Zones[2].OpenSides.Contains(LevelLayoutData.Side.West),
                "Zone 3 must have a West opening to connect to Zone 4.");
        }

        [Test]
        public void Zone4_HasNorthOpening_ForZ3Connection()
        {
            Assert.IsTrue(layout.Zones[3].OpenSides.Contains(LevelLayoutData.Side.North),
                "Zone 4 must have a North opening to connect to Zone 3.");
        }

        [Test]
        public void Zone4_IsDeadEnd_Only1Opening()
        {
            Assert.AreEqual(1, layout.Zones[3].OpenSides.Count,
                "Zone 4 (Praça do Cerco) should be a dead end with only 1 opening.");
        }

        // --- Edge alignment: Z1 bottom should touch Z2 top ---

        [Test]
        public void Z1SouthEdge_AlignsWithZ2NorthEdge()
        {
            var z1 = layout.Zones[0];
            var z2 = layout.Zones[1];

            float z1South = z1.Center.y - z1.Size.y * 0.5f;
            float z2North = z2.Center.y + z2.Size.y * 0.5f;

            float gap = Mathf.Abs(z1South - z2North);
            Assert.Less(gap, layout.WallThickness * 2f,
                $"Gap between Z1 south ({z1South:F2}) and Z2 north ({z2North:F2}) is {gap:F2}, exceeds tolerance.");
        }

        // --- Wall generation ---

        [Test]
        public void Zone1_HasCorrectWallCount()
        {
            // Z1 opens East and South → 2 walls closed (North, West) = 2 walls
            Assert.AreEqual(2, layout.Zones[0].Walls.Count,
                "Zone 1 should have 2 walls (North and West closed).");
        }

        [Test]
        public void Zone4_HasCorrectWallCount()
        {
            // Z4 opens North only → 3 walls closed (South, East, West) = 3 walls
            Assert.AreEqual(3, layout.Zones[3].Walls.Count,
                "Zone 4 should have 3 walls (South, East, West closed).");
        }

        [Test]
        public void AllWalls_HavePositiveSize()
        {
            foreach (var zone in layout.Zones)
            {
                foreach (var wall in zone.Walls)
                {
                    Assert.Greater(wall.Size.x, 0f, $"Wall in {zone.Name} has non-positive width.");
                    Assert.Greater(wall.Size.y, 0f, $"Wall in {zone.Name} has non-positive height.");
                }
            }
        }

        // --- House validation ---

        [Test]
        public void EachHouse_Has5Walls()
        {
            // 3 solid walls (N, E, W) + 2 door segments (S_L, S_R) = 5
            foreach (var house in layout.Houses)
            {
                Assert.AreEqual(5, house.Walls.Count,
                    $"House '{house.Name}' should have 5 walls (3 solid + 2 door segments).");
            }
        }

        [Test]
        public void Houses_AreInsideZone2Bounds()
        {
            var z2Bounds = layout.Zones[1].FloorBounds;
            foreach (var house in layout.Houses)
            {
                Rect houseBounds = new Rect(
                    house.Position.x - house.Size * 0.5f,
                    house.Position.y - house.Size * 0.5f,
                    house.Size, house.Size);

                Assert.IsTrue(z2Bounds.Overlaps(houseBounds),
                    $"House '{house.Name}' at {house.Position} is outside Zone 2 bounds {z2Bounds}.");
            }
        }

        [Test]
        public void House_DoorGap_IsPositive()
        {
            foreach (var house in layout.Houses)
            {
                Assert.Greater(house.DoorGap, 0f,
                    $"House '{house.Name}' has non-positive door gap.");
            }
        }

        // --- S-Path flow: zones descend and sweep ---

        [Test]
        public void SPath_ZonesFormDescendingPath()
        {
            // Z1 → Z2: Z2 is below Z1
            Assert.Less(layout.Zones[1].Center.y, layout.Zones[0].Center.y,
                "Z2 should be below Z1.");

            // Z2 → Z3: Z3 is below Z2
            Assert.Less(layout.Zones[2].Center.y, layout.Zones[1].Center.y,
                "Z3 should be below Z2.");

            // Z3 → Z4: Z4 is below Z3
            Assert.Less(layout.Zones[3].Center.y, layout.Zones[2].Center.y,
                "Z4 should be below Z3.");
        }

        [Test]
        public void SPath_Zone3_IsLeftOfZone2()
        {
            // The S-path turns west at Z3
            Assert.Less(layout.Zones[2].Center.x, layout.Zones[1].Center.x,
                "Z3 (Beco do Vento) should be to the left of Z2 (Vila das Casas).");
        }

        // --- Edge case: empty build ---

        [Test]
        public void Validate_BeforeBuild_ReturnsError()
        {
            var fresh = new LevelLayoutData();
            // Don't call Build()
            var errors = fresh.Validate();
            Assert.IsNotEmpty(errors, "Validate() before Build() should return errors.");
        }

        // --- Custom parameters ---

        [Test]
        public void CustomParameters_AffectZoneSizes()
        {
            var custom = new LevelLayoutData(
                wallThickness: 1f,
                z1Length: 10f, z1Width: 3f,
                z2Length: 8f, z2Width: 6f,
                z3Length: 7f, z3Width: 2f,
                z4Length: 5f, z4Width: 5f);
            custom.Build();

            Assert.AreEqual(4, custom.Zones.Count);
            Assert.AreEqual(new Vector2(10f, 3f), custom.Zones[0].Size);
            Assert.AreEqual(new Vector2(6f, 8f), custom.Zones[1].Size);
        }
    }
}
