using System;
using System.Collections.Generic;
using UnityEngine;

namespace FavelaAmarela.Level.Core
{
    /// <summary>
    /// Categoria de um ponto de interesse do overworld do Deserto de Hali.
    /// Guia como o Generator instancia cada marcador (portal de dungeon,
    /// spawn do jogador, NPC de quest ou portão de fim de fase).
    /// </summary>
    public enum PointOfInterestKind
    {
        /// <summary>Onde Damião nasce ao entrar no deserto.</summary>
        PlayerSpawn,
        /// <summary>Entrada da Dungeon 1 (Tumba de Alhazred) — leva ao S-Path, drop do Necronomicon.</summary>
        EntradaTumbaAlhazred,
        /// <summary>Entrada da Dungeon 2 (Templo da Serpente, opcional/oculta).</summary>
        EntradaTemploSerpente,
        /// <summary>Santuário de Yhtill — Rainha Cassilda e a quest do Patuá das Luas Gêmeas.</summary>
        SantuarioYhtill,
        /// <summary>Portões das Ruínas — saída do deserto rumo à próxima fase (fim da Fase 1).</summary>
        PortoesDasRuinas,
    }

    /// <summary>
    /// Ponto de interesse já posicionado (em coordenadas de mundo) no overworld.
    /// POCO puro: dado plano, sem tocar GameObject/Transform.
    /// </summary>
    public readonly struct PointOfInterestSpec
    {
        /// <summary>Nome do GameObject a ser criado para este ponto.</summary>
        public readonly string Name;

        /// <summary>Posição no mundo (offset a partir do centro do deserto).</summary>
        public readonly Vector2 Position;

        /// <summary>Categoria do ponto (define o comportamento instanciado).</summary>
        public readonly PointOfInterestKind Kind;

        /// <summary>
        /// Nome da cena de destino, quando o ponto é um portal navegável
        /// (entrada de dungeon / portões). Vazio para pontos sem navegação
        /// (spawn, NPC, ou destino ainda inexistente).
        /// </summary>
        public readonly string CenaDestino;

        public PointOfInterestSpec(string name, Vector2 position, PointOfInterestKind kind, string cenaDestino = "")
        {
            Name = name;
            Position = position;
            Kind = kind;
            CenaDestino = cenaDestino ?? "";
        }
    }

    /// <summary>
    /// Saída final, plana e agnóstica de Unity, do overworld do deserto — pronta
    /// para instanciação pelo Generator. Reaproveita <see cref="WallSpec"/> e
    /// <see cref="FloorSpec"/> do blockout de salas (mesmos tipos, mesmo assembly).
    /// </summary>
    public sealed class DesertOverworldLayout
    {
        /// <summary>Limites sólidos do perímetro (dunas intransponíveis).</summary>
        public readonly List<WallSpec> Walls = new();

        /// <summary>Chão(s) de areia aberto(s).</summary>
        public readonly List<FloorSpec> Floors = new();

        /// <summary>Pontos de interesse posicionados (dungeons, santuário, portões, spawn).</summary>
        public readonly List<PointOfInterestSpec> PointsOfInterest = new();

        /// <summary>
        /// Barreira interna impassável do Lago de Hali (centro-norte), que
        /// separa o oeste (Entrada/Tumba) do norte (Santuário/Portões) e força
        /// a navegação a contorná-la. Nula se a config zerar o Lago.
        /// </summary>
        public WallSpec? Lago;
    }

    /// <summary>
    /// Config serializável do overworld do Deserto de Hali (DTO ajustável no
    /// Inspector). POCO no sentido arquitetural: não deriva de MonoBehaviour/
    /// ScriptableObject e não toca em API de Component — só carrega números.
    /// As posições dos pontos são offsets a partir do centro (origem) do deserto.
    /// Valores default seguem <c>systems/level_design_deserto_hali.md</c>
    /// (mapa compacto ~22×16 com 5 setores em torno do Lago de Hali).
    /// </summary>
    [Serializable]
    public sealed class DesertOverworldConfig
    {
        [Header("Dimensões do deserto aberto (grande — exploração longa)")]
        public float Width = 44f;
        public float Height = 32f;
        public float BoundaryThickness = 1f;

        [Header("Lago de Hali (barreira interna impassável, centro-norte)")]
        public Vector2 LagoCenter = new(4f, 6f);
        public Vector2 LagoSize = new(18f, 10f);

        [Header("Pontos de interesse (offset do centro, por bússola)")]
        [Tooltip("Entrada / Garganta de Pedra Pálida (sul): onde Damião chega e nasce.")]
        public Vector2 EntradaOffset = new(-12f, -13f);
        [Tooltip("Tumba de Alhazred (oeste): Dungeon 1, visível da Entrada.")]
        public Vector2 TumbaAlhazredOffset = new(-17f, -2f);
        [Tooltip("Santuário de Yhtill (noroeste, elevação): quest da Rainha Cassilda.")]
        public Vector2 SantuarioYhtillOffset = new(-15f, 10f);
        [Tooltip("Templo da Serpente (leste, oculto pela tempestade máxima): Dungeon 2 opcional.")]
        public Vector2 TemploSerpenteOffset = new(18f, 0f);
        [Tooltip("Portões das Ruínas (norte, além do Lago): fim da Fase 1 (Byakhee).")]
        public Vector2 PortoesOffset = new(-4f, 14f);

        [Header("Cenas destino dos portais")]
        [Tooltip("Cena da Tumba de Alhazred (o S-Path reaproveitado).")]
        public string CenaTumbaAlhazred = "Tumba_De_Alhazred";
        [Tooltip("Cena da próxima fase (Portões). Vazio enquanto não existir.")]
        public string CenaPortoes = "";
    }
}
