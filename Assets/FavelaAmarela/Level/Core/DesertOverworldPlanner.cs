using UnityEngine;

namespace FavelaAmarela.Level.Core
{
    /// <summary>
    /// Camada POCO: monta o layout do overworld do Deserto de Hali em matemática
    /// pura (Vector2/float), sem tocar GameObject/Transform. Irmão aberto do
    /// <see cref="LevelBlockoutPlanner"/> (que monta o S-Path de salas fechadas):
    /// aqui o espaço é um único chão de areia com um perímetro sólido e pontos de
    /// interesse posicionados (dungeons, santuário, portões, spawn). Testável via
    /// NUnit puro em EditMode, sem cena nem Play Mode.
    /// </summary>
    public static class DesertOverworldPlanner
    {
        private const string Owner = "Deserto";

        /// <summary>
        /// Constrói o layout do deserto a partir da config: um chão central de
        /// <c>Width×Height</c>, quatro limites de perímetro sólidos (sem portas —
        /// dunas intransponíveis), a barreira interna do Lago de Hali e os cinco
        /// pontos de interesse posicionados por bússola (Entrada=sul, Tumba=oeste,
        /// Santuário=noroeste, Templo=leste, Portões=norte, além do Lago) — seguindo
        /// <c>systems/level_design_deserto_hali.md</c>.
        /// </summary>
        public static DesertOverworldLayout BuildLayout(DesertOverworldConfig cfg)
        {
            var layout = new DesertOverworldLayout();
            var center = Vector2.zero;
            float halfW = cfg.Width * 0.5f;
            float halfH = cfg.Height * 0.5f;
            float t = cfg.BoundaryThickness;

            // Chão aberto único (dunas de cinza).
            layout.Floors.Add(new FloorSpec("Floor", Owner, center, new Vector2(cfg.Width, cfg.Height)));

            // Perímetro: 4 limites sólidos, encostados por dentro da borda do chão.
            layout.Walls.Add(new WallSpec("Limite_Norte", Owner, new Vector2(0f, halfH - t * 0.5f), new Vector2(cfg.Width, t)));
            layout.Walls.Add(new WallSpec("Limite_Sul", Owner, new Vector2(0f, -halfH + t * 0.5f), new Vector2(cfg.Width, t)));
            layout.Walls.Add(new WallSpec("Limite_Leste", Owner, new Vector2(halfW - t * 0.5f, 0f), new Vector2(t, cfg.Height)));
            layout.Walls.Add(new WallSpec("Limite_Oeste", Owner, new Vector2(-halfW + t * 0.5f, 0f), new Vector2(t, cfg.Height)));

            // Lago de Hali: barreira interna impassável (centro-norte). Separa o oeste
            // (Entrada/Tumba) do norte (Santuário/Portões), forçando o contorno.
            if (cfg.LagoSize.x > 0f && cfg.LagoSize.y > 0f)
                layout.Lago = new WallSpec("Lago_De_Hali", Owner, cfg.LagoCenter, cfg.LagoSize);

            // Pontos de interesse (o portal da Tumba carrega a cena do S-Path).
            layout.PointsOfInterest.Add(new PointOfInterestSpec(
                "Spawn_Damiao", cfg.EntradaOffset, PointOfInterestKind.PlayerSpawn));
            layout.PointsOfInterest.Add(new PointOfInterestSpec(
                "Entrada_TumbaAlhazred", cfg.TumbaAlhazredOffset, PointOfInterestKind.EntradaTumbaAlhazred, cfg.CenaTumbaAlhazred));
            layout.PointsOfInterest.Add(new PointOfInterestSpec(
                "Santuario_Yhtill", cfg.SantuarioYhtillOffset, PointOfInterestKind.SantuarioYhtill));
            layout.PointsOfInterest.Add(new PointOfInterestSpec(
                "Entrada_TemploSerpente", cfg.TemploSerpenteOffset, PointOfInterestKind.EntradaTemploSerpente));
            layout.PointsOfInterest.Add(new PointOfInterestSpec(
                "Portoes_DasRuinas", cfg.PortoesOffset, PointOfInterestKind.PortoesDasRuinas, cfg.CenaPortoes));

            return layout;
        }
    }
}
