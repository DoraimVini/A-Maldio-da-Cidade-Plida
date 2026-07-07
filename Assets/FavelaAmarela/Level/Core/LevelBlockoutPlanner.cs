using System.Collections.Generic;
using UnityEngine;

namespace FavelaAmarela.Level.Core
{
    /// <summary>
    /// Camada POCO: monta o layout S-Path das Ruínas Pálidas inteiramente em
    /// matemática pura (Vector2/float), sem tocar GameObject, Transform ou
    /// qualquer outra API de Component. Testável via NUnit puro em EditMode,
    /// sem precisar de cena nem Play Mode.
    ///
    /// Correções aplicadas em relação ao gerador original:
    ///   1) Zona 2 → Zona 3: a parede Norte da Zona 3 estava posicionada
    ///      1.25u dentro do chão da Zona 2 (sólida, bloqueando a descida).
    ///      Agora a Zona 3 fica rente à fronteira sul da Zona 2, com uma
    ///      porta calculada pela sobreposição real entre as duas salas.
    ///   2) Zona 3 → Zona 4: a parede Sul da Zona 3 era um bloco sólido de
    ///      largura cheia (15u), cobrindo por completo a abertura Norte de
    ///      12u da Zona 4 — o beco sem saída ficava geometricamente isolado
    ///      do resto do nível. Mesma técnica de porta por sobreposição.
    ///   3) Selada uma abertura órfã: a Zona 2 tinha o lado Norte aberto sem
    ///      nada conectado ali, deixando o jogador andar para o vazio.
    /// </summary>
    public static class LevelBlockoutPlanner
    {
        public static LevelBlockoutLayout BuildSPathLayout(LevelBlockoutConfig cfg)
        {
            var layout = new LevelBlockoutLayout();

            // --- Zona 1: Rua de Entrada ---
            var z1Center = Vector2.zero;
            float z1MinX = z1Center.x - cfg.Zone1Length * 0.5f;
            float z1MaxX = z1Center.x + cfg.Zone1Length * 0.5f;
            float z1MinY = z1Center.y - cfg.Zone1Width * 0.5f;
            float z1MaxY = z1Center.y + cfg.Zone1Width * 0.5f;

            // --- Zona 2: Vila das Casas ---
            // Conecta-se à Zona 1 por sobreposição interior: a abertura Leste
            // da Zona 1 (x = z1Center.x + Zone1Length/2) cai dentro do chão da
            // Zona 2, não numa de suas próprias paredes de fronteira — mesma
            // técnica usada para compor formas em L a partir de retângulos.
            // Calculamos o centro/bounds da Zona 2 ANTES de fechar a Zona 1
            // porque as duas salas se sobrepõem de propósito no trecho
            // x:[z2MinX, z1MaxX] — ver BUG abaixo.
            var z2Center = new Vector2(
                z1Center.x + cfg.Zone1Length - cfg.Zone2Width * 0.5f,
                z1Center.y - cfg.Zone2Length * 0.5f + cfg.Zone1Width * 0.5f);
            float z2MinX = z2Center.x - cfg.Zone2Width * 0.5f;
            float z2MaxX = z2Center.x + cfg.Zone2Width * 0.5f;
            float z2MinY = z2Center.y - cfg.Zone2Length * 0.5f;
            float z2MaxY = z2Center.y + cfg.Zone2Length * 0.5f;

            // BUG (achado em playtest): a sobreposição Z1↔Z2 é só de CHÃO —
            // "não há parede de nenhum dos dois lados nessa faixa" dizia o
            // comentário original, mas isso nunca foi garantido de verdade: as
            // paredes Norte/Sul da Zona 1 corriam pelo comprimento inteiro
            // (x:[z1MinX,z1MaxX]), sem descontar a faixa de sobreposição. A
            // parede Sul da Zona 1 atravessava o telhado da Casa_1 (que mora
            // bem nessa faixa) e a Norte duplicava por cima da parede Norte da
            // Zona 2. Fechamos as duas como "porta" cobrindo exatamente a faixa
            // de sobreposição — como essa faixa termina na própria borda leste
            // da Zona 1, o efeito é truncar a parede ali em vez de furar o meio.
            var z1NorteOverlap = MakeOverlapDoorway(Side.North, z1MinX, z1MaxX, z2MinX, z2MaxX, z1Center.x);
            var z1SulOverlap = MakeOverlapDoorway(Side.South, z1MinX, z1MaxX, z2MinX, z2MaxX, z1Center.x);

            layout.Rooms.Add(new RoomSpec("Zona1_RuaEntrada", z1Center, cfg.Zone1Length, cfg.Zone1Width,
                Side.East, new[] { z1NorteOverlap, z1SulOverlap }));

            // Zona 1 → Zona 2: a Zona 1 é um corredor que encosta na Zona 2 pela
            // face Oeste desta. Sem porta, a parede Oeste sólida da Zona 2 corta
            // justamente a faixa de sobreposição (o jogador nascia preso na Zona
            // 1). Abrimos essa parede exatamente na sobreposição em Y entre os
            // dois chãos — mesma técnica de porta-por-overlap das demais
            // fronteiras. (O lado Norte da Zona 2 segue fechado; a conexão real
            // sempre foi por aqui, não pelo Norte.)
            var z1z2Doorway = MakeOverlapDoorway(Side.West, z1MinY, z1MaxY, z2MinY, z2MaxY, z2Center.y);

            layout.Rooms.Add(new RoomSpec("Zona2_VilaDasCasas", z2Center, cfg.Zone2Width, cfg.Zone2Length,
                Side.South, new[] { z1z2Doorway }));

            var vilaTopLeft = new Vector2(z2Center.x - cfg.Zone2Width * 0.5f, z2Center.y + cfg.Zone2Length * 0.5f);
            layout.Houses.Add(new HouseSpec("Casa_1", vilaTopLeft + new Vector2(3f, -4f), cfg.HouseSize, cfg.HouseDoorGap));
            layout.Houses.Add(new HouseSpec("Casa_2", vilaTopLeft + new Vector2(9f, -7f), cfg.HouseSize, cfg.HouseDoorGap));
            layout.Houses.Add(new HouseSpec("Casa_3", vilaTopLeft + new Vector2(5f, -13f), cfg.HouseSize, cfg.HouseDoorGap));

            float z2SouthY = z2Center.y - cfg.Zone2Length * 0.5f;

            // --- Zona 3: Beco do Vento (CORRIGIDA) ---
            // Reposicionada rente à fronteira sul da Zona 2 (sem sobreposição
            // de 1.25u). North vira porta calculada pela sobreposição real em
            // X entre as duas salas, em vez de lado totalmente aberto/fechado.
            var z3Center = new Vector2(
                z2Center.x - cfg.Zone3Length * 0.5f,
                z2SouthY - cfg.Zone3Width * 0.5f);
            float z3MinX = z3Center.x - cfg.Zone3Length * 0.5f;
            float z3MaxX = z3Center.x + cfg.Zone3Length * 0.5f;

            var z2z3Doorway = MakeOverlapDoorway(Side.North, z2MinX, z2MaxX, z3MinX, z3MaxX, z3Center.x);

            // --- Zona 4: Praça do Cerco ---
            // Posição mantida igual ao original (relativa à Zona 3), já que a
            // relação vertical entre Zona 3 e Zona 4 sempre esteve correta —
            // o problema era a parede cheia cobrindo a porta, não a posição.
            var z4Center = new Vector2(
                z3Center.x - cfg.Zone3Length * 0.5f + cfg.Zone4Width * 0.5f,
                z3Center.y - cfg.Zone4Length * 0.5f - cfg.Zone3Width * 0.5f);
            float z4MinX = z4Center.x - cfg.Zone4Width * 0.5f;
            float z4MaxX = z4Center.x + cfg.Zone4Width * 0.5f;

            var z3z4Doorway = MakeOverlapDoorway(Side.South, z3MinX, z3MaxX, z4MinX, z4MaxX, z3Center.x);

            layout.Rooms.Add(new RoomSpec("Zona3_BecoDoVento", z3Center, cfg.Zone3Length, cfg.Zone3Width,
                Side.None, new[] { z2z3Doorway, z3z4Doorway }));

            // --- Zona 5: Transição Dimensional ---
            // Pendurada ao Sul da Praça do Cerco (antes um beco sem saída de
            // verdade). A fronteira Zona4→Zona5 não é uma porta comum: é uma
            // parede que parece sólida e bloqueia o jogador andando
            // normalmente, mas é marcada como barreira anômala — atravessável
            // só durante o Salto Dimensional (ver LevelBlockoutGenerator).
            var z5Center = new Vector2(z4Center.x, z4Center.y - cfg.Zone4Length * 0.5f - cfg.Zone5Width * 0.5f);
            float z5MinX = z5Center.x - cfg.Zone5Length * 0.5f;
            float z5MaxX = z5Center.x + cfg.Zone5Length * 0.5f;

            var z4z5Barrier = MakeOverlapDoorway(Side.South, z4MinX, z4MaxX, z5MinX, z5MaxX, z4Center.x, isAnomalyBarrier: true);

            layout.Rooms.Add(new RoomSpec("Zona4_PracaDoCerco", z4Center, cfg.Zone4Width, cfg.Zone4Length,
                Side.North, new[] { z4z5Barrier }));
            layout.Rooms.Add(new RoomSpec("Zona5_TransicaoDimensional", z5Center, cfg.Zone5Length, cfg.Zone5Width, Side.North));

            foreach (var room in layout.Rooms)
                BuildRoomGeometry(room, cfg, layout);

            foreach (var house in layout.Houses)
                BuildHouseGeometry(house, cfg, layout);

            return layout;
        }

        /// <summary>
        /// Calcula uma porta a partir da sobreposição real (em X) entre duas
        /// salas vizinhas, em vez de assumir lado totalmente aberto/fechado.
        /// Isso garante automaticamente que a porta nunca seja mais larga do
        /// que a sobreposição física real — eliminando por construção tanto
        /// o bug de "parede cobrindo a porta" quanto vazamentos para áreas
        /// sem sala nenhuma do outro lado.
        /// </summary>
        private static Doorway MakeOverlapDoorway(Side side, float aMinX, float aMaxX, float bMinX, float bMaxX, float roomCenterX, bool isAnomalyBarrier = false)
        {
            float overlapMin = Mathf.Max(aMinX, bMinX);
            float overlapMax = Mathf.Min(aMaxX, bMaxX);
            float width = Mathf.Max(0f, overlapMax - overlapMin);
            float offset = (overlapMin + overlapMax) * 0.5f - roomCenterX;
            return new Doorway(side, width, offset, isAnomalyBarrier);
        }

        private static void BuildRoomGeometry(RoomSpec room, LevelBlockoutConfig cfg, LevelBlockoutLayout layout)
        {
            float halfW = room.Width * 0.5f, halfH = room.Height * 0.5f;
            float t = cfg.WallThickness;

            AddSideWithDoorways(layout, room, Side.North, new Vector2(0f, halfH - t * 0.5f), room.Width, t, cfg.CornerInset);
            AddSideWithDoorways(layout, room, Side.South, new Vector2(0f, -halfH + t * 0.5f), room.Width, t, cfg.CornerInset);
            AddSideWithDoorways(layout, room, Side.East, new Vector2(halfW - t * 0.5f, 0f), room.Height, t, cfg.CornerInset);
            AddSideWithDoorways(layout, room, Side.West, new Vector2(-halfW + t * 0.5f, 0f), room.Height, t, cfg.CornerInset);

            layout.Floors.Add(new FloorSpec("Floor", room.Name, room.Center, new Vector2(room.Width, room.Height)));
        }

        /// <summary>
        /// Constrói 0, 1 ou 2 segmentos de parede para um lado da sala,
        /// dividindo ao redor de qualquer porta registrada nesse lado e
        /// recuando cornerInset na ponta de cada segmento que encosta numa
        /// porta — evita o collider do jogador travar exatamente no canto
        /// onde parede sólida encontra abertura.
        /// </summary>
        private static void AddSideWithDoorways(LevelBlockoutLayout layout, RoomSpec room, Side side,
            Vector2 localCenter, float fullLength, float thickness, float cornerInset)
        {
            if ((room.FullyOpenSides & side) != 0) return; // lado totalmente aberto, sem parede

            bool isHorizontalAxis = side == Side.North || side == Side.South; // parede corre ao longo de X

            // Portas comuns viram buraco vazio (comportamento original). Barreiras
            // anômalas NÃO entram nessa lista — são emitidas à parte, como um
            // segmento de parede próprio, sem cornerInset (a barreira preenche o
            // vão inteiro, não é um buraco a contornar).
            var gaps = new List<(float from, float to)>();
            var anomalySegments = new List<(float from, float to)>();
            foreach (var d in room.Doorways)
            {
                if (d.Side != side || d.Width <= 0f) continue;
                var range = (d.Offset - d.Width * 0.5f, d.Offset + d.Width * 0.5f);
                if (d.IsAnomalyBarrier) anomalySegments.Add(range);
                else gaps.Add(range);
            }

            float wallHalf = fullLength * 0.5f;
            if (gaps.Count == 0)
            {
                EmitSegment(layout, room, side, -wallHalf, wallHalf, localCenter, thickness, isHorizontalAxis, 0, isAnomalyBarrier: false);
            }
            else
            {
                gaps.Sort((a, b) => a.from.CompareTo(b.from));
                float cursor = -wallHalf;
                int segIndex = 0;
                foreach (var (from, to) in gaps)
                {
                    EmitSegment(layout, room, side, cursor, from + cornerInset, localCenter, thickness, isHorizontalAxis, segIndex++, isAnomalyBarrier: false);
                    cursor = to - cornerInset;
                }
                EmitSegment(layout, room, side, cursor, wallHalf, localCenter, thickness, isHorizontalAxis, segIndex, isAnomalyBarrier: false);
            }

            int anomalyIndex = 0;
            foreach (var (from, to) in anomalySegments)
                EmitSegment(layout, room, side, from, to, localCenter, thickness, isHorizontalAxis, anomalyIndex++, isAnomalyBarrier: true);
        }

        private static void EmitSegment(LevelBlockoutLayout layout, RoomSpec room, Side side,
            float from, float to, Vector2 localCenter, float thickness, bool isHorizontalAxis, int index, bool isAnomalyBarrier)
        {
            float length = to - from;
            if (length <= 0.001f) return; // porta consumiu o segmento inteiro, nada a construir

            float mid = (from + to) * 0.5f;
            Vector2 segCenter = isHorizontalAxis
                ? new Vector2(mid, localCenter.y)
                : new Vector2(localCenter.x, mid);

            var size = isHorizontalAxis ? new Vector2(length, thickness) : new Vector2(thickness, length);
            string name = isAnomalyBarrier ? $"AnomalyWall_{side}_{index}" : $"Wall_{side}_{index}";
            layout.Walls.Add(new WallSpec(name, room.Name, room.Center + segCenter, size, isAnomalyBarrier));
        }

        private static void BuildHouseGeometry(HouseSpec house, LevelBlockoutConfig cfg, LevelBlockoutLayout layout)
        {
            float half = house.Size * 0.5f;
            float t = cfg.WallThickness;

            layout.Walls.Add(new WallSpec("Wall_N", house.Name, house.Position + new Vector2(0f, half - t * 0.5f), new Vector2(house.Size, t)));
            layout.Walls.Add(new WallSpec("Wall_E", house.Name, house.Position + new Vector2(half - t * 0.5f, 0f), new Vector2(t, house.Size)));
            layout.Walls.Add(new WallSpec("Wall_W", house.Name, house.Position + new Vector2(-half + t * 0.5f, 0f), new Vector2(t, house.Size)));

            float segmentWidth = (house.Size - house.DoorGap) * 0.5f;
            float segmentOffset = (house.DoorGap + segmentWidth) * 0.5f;
            layout.Walls.Add(new WallSpec("Wall_S_L", house.Name, house.Position + new Vector2(-segmentOffset, -half + t * 0.5f), new Vector2(segmentWidth, t)));
            layout.Walls.Add(new WallSpec("Wall_S_R", house.Name, house.Position + new Vector2(segmentOffset, -half + t * 0.5f), new Vector2(segmentWidth, t)));

            layout.Floors.Add(new FloorSpec("Floor", house.Name, house.Position, new Vector2(house.Size, house.Size)));
        }
    }
}
