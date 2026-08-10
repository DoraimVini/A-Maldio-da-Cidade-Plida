using FavelaAmarela.Level.Core;
using NUnit.Framework;
using UnityEngine;

namespace FavelaAmarela.Level.Tests
{
    /// <summary>
    /// Suite NUnit (EditMode) para o <see cref="IsoProjection"/>. POCO puro — roda sem
    /// cena nem Play Mode. Cobre projeção, inversa (roundtrip) e projeção de segmento.
    /// </summary>
    [TestFixture]
    public class IsoProjectionTests
    {
        private const float Eps = 1e-4f;

        private static void AssertVec(Vector2 expected, Vector2 actual)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(Eps), "componente X");
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(Eps), "componente Y");
        }

        [Test]
        public void GridToWorld_Origem_ContinuaNaOrigem()
        {
            var iso = new IsoProjection(1f);
            AssertVec(Vector2.zero, iso.GridToWorld(Vector2.zero));
        }

        [Test]
        public void GridToWorld_EixosBatemComConvertToIsometric()
        {
            // Os eixos do losango devem casar com PlayerMovement.ConvertToIsometric:
            // input D=(1,0) -> (1, 0.5) e W=(0,1) -> (-1, 0.5).
            var iso = new IsoProjection(1f);
            AssertVec(new Vector2(1f, 0.5f), iso.GridToWorld(new Vector2(1f, 0f)));
            AssertVec(new Vector2(-1f, 0.5f), iso.GridToWorld(new Vector2(0f, 1f)));
        }

        [Test]
        public void GridToWorld_DiagonalDoLosango()
        {
            var iso = new IsoProjection(1f);
            // (1,1): X se cancela, Y soma -> (0, 1)
            AssertVec(new Vector2(0f, 1f), iso.GridToWorld(new Vector2(1f, 1f)));
        }

        [Test]
        public void GridToWorld_RespeitaAEscalaUnit()
        {
            var iso = new IsoProjection(0.5f); // razão do Tilemap cellSize (1, 0.5)
            AssertVec(new Vector2(0.5f, 0.25f), iso.GridToWorld(new Vector2(1f, 0f)));
        }

        [Test]
        public void WorldToGrid_EhInversaDeGridToWorld()
        {
            var iso = new IsoProjection(0.5f);
            var pontos = new[]
            {
                new Vector2(3f, 7f), new Vector2(-4.5f, 2.25f),
                new Vector2(0f, 0f), new Vector2(10f, -6f),
            };
            foreach (var g in pontos)
                AssertVec(g, iso.WorldToGrid(iso.GridToWorld(g)));
        }

        [Test]
        public void ProjectSegment_Horizontal_CentroAnguloComprimento()
        {
            var iso = new IsoProjection(1f);
            // parede horizontal no grid: (0,0)->(2,0)  =>  mundo A(0,0), B(2,1)
            var seg = iso.ProjectSegment(new Vector2(0f, 0f), new Vector2(2f, 0f));
            AssertVec(new Vector2(1f, 0.5f), seg.Center);
            Assert.That(seg.Length, Is.EqualTo(Mathf.Sqrt(5f)).Within(Eps));
            Assert.That(seg.AngleDeg, Is.EqualTo(Mathf.Atan2(1f, 2f) * Mathf.Rad2Deg).Within(Eps)); // ~26.565°
        }

        [Test]
        public void ProjectSegment_Vertical_ProjetaNaOutraDiagonal()
        {
            var iso = new IsoProjection(1f);
            // parede vertical no grid: (0,0)->(0,2)  =>  mundo A(0,0), B(-2,1)
            var seg = iso.ProjectSegment(new Vector2(0f, 0f), new Vector2(0f, 2f));
            AssertVec(new Vector2(-1f, 0.5f), seg.Center);
            Assert.That(seg.Length, Is.EqualTo(Mathf.Sqrt(5f)).Within(Eps));
            Assert.That(seg.AngleDeg, Is.EqualTo(Mathf.Atan2(1f, -2f) * Mathf.Rad2Deg).Within(Eps)); // ~153.43°
        }

        [Test]
        public void ProjectSegment_ComprimentoZero_NaoQuebra()
        {
            var iso = new IsoProjection(1f);
            var seg = iso.ProjectSegment(new Vector2(2f, 3f), new Vector2(2f, 3f));
            Assert.That(seg.Length, Is.EqualTo(0f).Within(Eps));
        }
    }
}
