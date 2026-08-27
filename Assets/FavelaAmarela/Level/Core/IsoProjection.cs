using UnityEngine;

namespace FavelaAmarela.Level.Core
{
    /// <summary>
    /// POCO: projeção isométrica dimétrica 2:1 entre "grid-space" (o layout lógico
    /// cartesiano produzido pelo <see cref="LevelBlockoutPlanner"/>) e "world-space"
    /// (a malha de losango que o jogador vê e onde a física acontece). C# puro — sem
    /// GameObject/Transform, testável em NUnit EditMode.
    ///
    /// Eixos: grid-X projeta em <c>(Unit, Unit/2)</c> e grid-Y em <c>(-Unit, Unit/2)</c>
    /// — exatamente os eixos 2:1 que <c>Core.Player.BaseIsometrica.ParaMundo</c> já usa pro
    /// movimento (input D→(1,0.5), W→(-1,0.5)) e a mesma razão do Tilemap isométrico
    /// (<c>cellSize (1, 0.5)</c>). Por isso o jogador anda naturalmente ao longo das
    /// arestas do losango depois que a geometria do nível é projetada por aqui.
    /// </summary>
    public readonly struct IsoProjection
    {
        /// <summary>
        /// Escala da projeção — metade da largura de célula do losango no mundo
        /// (para <c>cellSize.x = 1</c> do Tilemap, <c>Unit = 0.5</c>).
        /// </summary>
        public readonly float Unit;

        public IsoProjection(float unit) => Unit = unit;

        /// <summary>Projeta um ponto de grid-space para world-space (losango).</summary>
        public Vector2 GridToWorld(Vector2 grid) => new Vector2(
            (grid.x - grid.y) * Unit,
            (grid.x + grid.y) * Unit * 0.5f);

        /// <summary>Inversa de <see cref="GridToWorld"/>: world-space (losango) → grid cartesiano.</summary>
        public Vector2 WorldToGrid(Vector2 world) => new Vector2(
            (world.x + 2f * world.y) / (2f * Unit),
            (2f * world.y - world.x) / (2f * Unit));

        /// <summary>
        /// Projeta um segmento reto (parede) definido por duas pontas em grid-space,
        /// devolvendo como plantá-lo no mundo: centro, ângulo (graus) e comprimento.
        /// A espessura (perpendicular) é tratada à parte pelo gerador.
        /// </summary>
        public IsoSegment ProjectSegment(Vector2 gridA, Vector2 gridB)
        {
            Vector2 a = GridToWorld(gridA);
            Vector2 b = GridToWorld(gridB);
            Vector2 delta = b - a;
            return new IsoSegment(
                (a + b) * 0.5f,
                Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg,
                delta.magnitude);
        }
    }

    /// <summary>
    /// Resultado de <see cref="IsoProjection.ProjectSegment"/> — como plantar a parede
    /// projetada no mundo (posição do Transform, rotação em Z e comprimento visível).
    /// </summary>
    public readonly struct IsoSegment
    {
        public readonly Vector2 Center;
        public readonly float AngleDeg;
        public readonly float Length;

        public IsoSegment(Vector2 center, float angleDeg, float length)
        {
            Center = center;
            AngleDeg = angleDeg;
            Length = length;
        }
    }
}
