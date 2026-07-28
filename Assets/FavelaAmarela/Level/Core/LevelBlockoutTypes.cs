using System;
using System.Collections.Generic;
using UnityEngine;

namespace FavelaAmarela.Level.Core
{
    /// <summary>
    /// Camada POCO: dados puros + matemática pura, sem dependência de
    /// MonoBehaviour, GameObject, Transform ou qualquer outra API de
    /// Component da Unity. Totalmente testável com NUnit puro, sem precisar
    /// de cena ou Play Mode.
    /// </summary>
    [Flags]
    public enum Side
    {
        None = 0,
        North = 1 << 0,
        South = 1 << 1,
        East = 1 << 2,
        West = 1 << 3,
    }

    /// <summary>
    /// Abertura explícita e dimensionada em um lado de uma sala. Usada
    /// sempre que a sala vizinha for mais estreita que o lado inteiro —
    /// ex: Zona 3 (15u de comprimento) entregando pra Zona 4 (12u de
    /// largura). Evita o bug de "parede fantasma" onde um lado totalmente
    /// aberto sobrepõe sem querer a parede sólida de uma sala vizinha.
    /// </summary>
    public readonly struct Doorway
    {
        public readonly Side Side;
        public readonly float Width;
        public readonly float Offset; // a partir do centro do próprio lado, ao longo do seu eixo

        /// <summary>
        /// Se true, esta abertura não vira um buraco vazio na parede — vira um
        /// segmento de parede próprio, marcado como barreira anômala (só
        /// atravessável durante o Salto Dimensional). Ver <see cref="WallSpec.IsAnomalyBarrier"/>.
        /// </summary>
        public readonly bool IsAnomalyBarrier;

        public Doorway(Side side, float width, float offset = 0f, bool isAnomalyBarrier = false)
        {
            Side = side;
            Width = width;
            Offset = offset;
            IsAnomalyBarrier = isAnomalyBarrier;
        }
    }

    public readonly struct RoomSpec
    {
        public readonly string Name;
        public readonly Vector2 Center;
        public readonly float Width;
        public readonly float Height;
        public readonly Side FullyOpenSides;
        public readonly IReadOnlyList<Doorway> Doorways;

        public RoomSpec(string name, Vector2 center, float width, float height,
            Side fullyOpenSides, IReadOnlyList<Doorway> doorways = null)
        {
            Name = name;
            Center = center;
            Width = width;
            Height = height;
            FullyOpenSides = fullyOpenSides;
            Doorways = doorways ?? Array.Empty<Doorway>();
        }
    }

    public readonly struct HouseSpec
    {
        public readonly string Name;
        public readonly Vector2 Position;
        public readonly float Size;
        public readonly float DoorGap;

        public HouseSpec(string name, Vector2 position, float size, float doorGap)
        {
            Name = name;
            Position = position;
            Size = size;
            DoorGap = doorGap;
        }
    }

    /// <summary>Segmento de parede já calculado, em coordenadas absolutas (mundo).</summary>
    public readonly struct WallSpec
    {
        public readonly string Name;
        public readonly string ParentName; // nome da sala/casa dona desta parede
        public readonly Vector2 Center;
        public readonly Vector2 Size;

        /// <summary>
        /// Se true, esta parede deve ser instanciada na layer "AnomalyBarrier"
        /// (bloqueia o jogador andando normalmente, mas é atravessável durante
        /// o Salto Dimensional — ver LevelBlockoutGenerator.ConfigureAnomalyBarrierPhysics).
        /// </summary>
        public readonly bool IsAnomalyBarrier;

        public WallSpec(string name, string parentName, Vector2 center, Vector2 size, bool isAnomalyBarrier = false)
        {
            Name = name;
            ParentName = parentName;
            Center = center;
            Size = size;
            IsAnomalyBarrier = isAnomalyBarrier;
        }
    }

    public readonly struct FloorSpec
    {
        public readonly string Name;
        public readonly string ParentName;
        public readonly Vector2 Center;
        public readonly Vector2 Size;

        public FloorSpec(string name, string parentName, Vector2 center, Vector2 size)
        {
            Name = name;
            ParentName = parentName;
            Center = center;
            Size = size;
        }
    }

    /// <summary>Saída final, plana e agnóstica de Unity, pronta para instanciação.</summary>
    public sealed class LevelBlockoutLayout
    {
        public readonly List<RoomSpec> Rooms = new();
        public readonly List<HouseSpec> Houses = new();
        public readonly List<WallSpec> Walls = new();
        public readonly List<FloorSpec> Floors = new();
    }

    /// <summary>
    /// Config serializável (DTO simples) com todos os parâmetros ajustáveis
    /// no Inspector. Ainda é POCO no sentido arquitetural: não deriva de
    /// MonoBehaviour/ScriptableObject e não toca em nenhuma API de Component.
    /// </summary>
    [Serializable]
    public sealed class LevelBlockoutConfig
    {
        [Header("Estrutura")]
        public float WallThickness = 0.5f;
        public float CornerInset = 0.15f; // folga deixada em cantos abertos, evita colisor travar

        [Header("Zona 1: Rua de Entrada (Leste)")]
        public float Zone1Length = 20f;
        public float Zone1Width = 4f;

        [Header("Zona 2: Vila das Casas (Sul)")]
        public float Zone2Length = 18f;
        public float Zone2Width = 14f;
        public float HouseSize = 4f;
        public float HouseDoorGap = 1.2f;

        [Header("Zona 3: Beco do Vento (Oeste)")]
        public float Zone3Length = 15f;
        public float Zone3Width = 2.5f;

        [Header("Zona 4: Praça do Cerco (beco sem saída)")]
        public float Zone4Length = 12f;
        public float Zone4Width = 12f;

        [Header("Zona 5: Transição Dimensional (Sul da Praça)")]
        public float Zone5Length = 10f;
        public float Zone5Width = 8f;

        // Zonas 6-9: descida de combate abaixo da Z5 (o "jogo de verdade" abre aqui,
        // culminando na arena do miniboss). Length = largura em X, Width = altura em Y.
        [Header("Zona 6: Cripta dos Primeiros (arena)")]
        public float Zone6Length = 12f;
        public float Zone6Width = 10f;

        [Header("Zona 7: Fenda dos Sussurros (corredor)")]
        public float Zone7Length = 4f;
        public float Zone7Width = 4.5f;

        [Header("Zona 8: Ossário (arena maior)")]
        public float Zone8Length = 14f;
        public float Zone8Width = 14f;

        [Header("Zona 9: Trono do Vulto (arena do miniboss)")]
        public float Zone9Length = 16f;
        public float Zone9Width = 12f;
    }
}
