using System.Linq;
using FavelaAmarela.Level.Core;
using NUnit.Framework;
using UnityEngine;

namespace FavelaAmarela.Level.Tests
{
    /// <summary>
    /// Suite NUnit (EditMode) para o LevelBlockoutPlanner.
    /// Roda sem cena, sem Play Mode — POCO puro.
    ///
    /// Cobre:
    ///   1. Zonas geradas na ordem correta com nomes esperados.
    ///   2. Progressão de largura (tensão ↑ = corredor mais estreito).
    ///   3. BUG 1 — Zona 2 → Zona 3: ausência de colisão de parede
    ///      Norte da Zona 3 dentro do chão da Zona 2.
    ///   4. BUG 2 — Zona 3 → Zona 4: parede Sul da Zona 3 não
    ///      deve cobrir a totalidade da abertura Norte da Zona 4.
    ///   5. Praça do Cerco (Zona 4) realmente acessível (porta ≥ 1u).
    ///   6. Nenhuma parede com largura ou altura negativa/zero.
    ///   7. Todos os segmentos de parede estão dentro do chão da sua sala.
    /// </summary>
    [TestFixture]
    public class LevelBlockoutPlannerTests
    {
        private LevelBlockoutLayout _layout;
        private LevelBlockoutConfig _cfg;

        [SetUp]
        public void SetUp()
        {
            _cfg = new LevelBlockoutConfig();
            _layout = LevelBlockoutPlanner.BuildSPathLayout(_cfg);
        }

        // ── 1. Estrutura básica ──────────────────────────────────────────────

        [Test]
        public void Layout_DeveTerDezZonas()
        {
            Assert.AreEqual(10, _layout.Rooms.Count,
                "S-Path deve ter 10 salas: a descida de 9 + a Câmara do Baú lateral.");
        }

        [Test]
        public void Layout_NomesDeZonasNaOrdemCorreta()
        {
            Assert.AreEqual("Zona1_RuaEntrada",           _layout.Rooms[0].Name);
            Assert.AreEqual("Zona2_VilaDasCasas",         _layout.Rooms[1].Name);
            Assert.AreEqual("Zona3_BecoDoVento",          _layout.Rooms[2].Name);
            Assert.AreEqual("Zona4_PracaDoCerco",         _layout.Rooms[3].Name);
            Assert.AreEqual("Zona5_TransicaoDimensional", _layout.Rooms[4].Name);
            Assert.AreEqual("Zona6_CriptaDosPrimeiros",   _layout.Rooms[5].Name);
            Assert.AreEqual("Zona6b_CamaraDoBau",         _layout.Rooms[6].Name);
            Assert.AreEqual("Zona7_FendaDosSussurros",    _layout.Rooms[7].Name);
            Assert.AreEqual("Zona8_Ossario",              _layout.Rooms[8].Name);
            Assert.AreEqual("Zona9_TumbaDeAbdul",         _layout.Rooms[9].Name);
        }

        [Test]
        public void CamaraDoBau_FicaALesteDaCripta_SemDeslocarADescida()
        {
            var cripta = _layout.Rooms[5];
            var camara = _layout.Rooms[6];

            Assert.Greater(camara.Center.x, cripta.Center.x,
                "A Câmara do Baú é uma sala lateral a Leste da Cripta.");
            Assert.AreEqual(cripta.Center.y, camara.Center.y, 0.001f,
                "Deve ficar na mesma faixa Y da Cripta (ligação Leste↔Oeste).");

            // A descida (Zonas 7-9) tem de continuar alinhada em X com a Zona 5:
            // é isso que garante que nada foi deslocado e que os inimigos já
            // posicionados na cena seguem válidos.
            var z5 = _layout.Rooms[4];
            Assert.AreEqual(z5.Center.x, _layout.Rooms[7].Center.x, 0.001f);
            Assert.AreEqual(z5.Center.x, _layout.Rooms[8].Center.x, 0.001f);
            Assert.AreEqual(z5.Center.x, _layout.Rooms[9].Center.x, 0.001f);
        }

        [Test]
        public void CamaraDoBau_TemPortaLigandoDeVoltaAcripta()
        {
            var camara = _layout.Rooms[6];
            Assert.AreEqual(1, camara.Doorways.Count,
                "A Câmara do Baú é um cul-de-sac: só a porta de volta para a Cripta.");
            Assert.AreEqual(Side.West, camara.Doorways[0].Side);
            Assert.Greater(camara.Doorways[0].Width, 0f, "A porta precisa ter largura real.");
        }

        [Test]
        public void Layout_DeveTerTresCasas()
        {
            Assert.AreEqual(3, _layout.Houses.Count, "Vila deve ter 3 casas.");
        }

        // ── 2. Progressão de largura (curva de tensão) ───────────────────────

        [Test]
        public void Zona3BecoDoVento_DeveSerMaisEstreitoQueTodasAsOutras()
        {
            float z3Gargalo = Mathf.Min(_layout.Rooms[2].Width, _layout.Rooms[2].Height);
            Assert.Less(z3Gargalo, Mathf.Min(_layout.Rooms[0].Width, _layout.Rooms[0].Height), "Beco deve ser mais estreito que Rua de Entrada.");
            Assert.Less(z3Gargalo, Mathf.Min(_layout.Rooms[1].Width, _layout.Rooms[1].Height), "Beco deve ser mais estreito que Vila das Casas.");
            Assert.Less(z3Gargalo, Mathf.Min(_layout.Rooms[3].Width, _layout.Rooms[3].Height), "Beco deve ser mais estreito que Praça do Cerco.");
            Assert.Less(z3Gargalo, Mathf.Min(_layout.Rooms[4].Width, _layout.Rooms[4].Height), "Beco deve ser mais estreito que Zona de Transição Dimensional.");
        }

        // ── 3. BUG 1: Zona 2 → Zona 3 (parede Norte da Z3 dentro do chão Z2) ─

        [Test]
        public void Bug1_ParedeSulDeZ2_NaoDeveColidir_ComParedeNorteDeZ3()
        {
            var z2 = _layout.Rooms[1];
            var z3 = _layout.Rooms[2];

            float z2SouthY = z2.Center.y - z2.Height * 0.5f;
            float z3NorthY = z3.Center.y + z3.Height * 0.5f;

            // A fronteira norte da Zona 3 deve ser igual (ou quase) à fronteira
            // sul da Zona 2 — sem sobreposição, sem gap maior que 1 parede.
            float gap = z2SouthY - z3NorthY;
            Assert.LessOrEqual(Mathf.Abs(gap), _cfg.WallThickness + 0.01f,
                $"Fronteiras Z2 Sul e Z3 Norte devem ser adjacentes. Gap atual: {gap:F3}u");
        }

        [Test]
        public void Bug0_ParedeOesteDeZ2_NaoDeveSelarConexaoComZ1()
        {
            var z1 = _layout.Rooms[0];
            var z2 = _layout.Rooms[1];

            // Faixa Y onde o corredor da Zona 1 encosta na Zona 2 — a "porta" natural.
            float z1MinY = z1.Center.y - z1.Height * 0.5f;
            float z1MaxY = z1.Center.y + z1.Height * 0.5f;
            float z2WestCenterX = z2.Center.x - z2.Width * 0.5f + _cfg.WallThickness * 0.5f;

            // Segmentos de parede da Zona 2 na face Oeste (perto de z2WestCenterX).
            var z2WestWalls = _layout.Walls.Where(w =>
                w.ParentName == "Zona2_VilaDasCasas" &&
                !w.IsAnomalyBarrier &&
                Mathf.Abs(w.Center.x - z2WestCenterX) < _cfg.WallThickness).ToList();

            // Nenhum deles pode cobrir a faixa inteira de conexão: se cobrir,
            // o jogador nasce preso na Zona 1 sem passagem para a Zona 2.
            foreach (var wall in z2WestWalls)
            {
                float wallMinY = wall.Center.y - wall.Size.y * 0.5f;
                float wallMaxY = wall.Center.y + wall.Size.y * 0.5f;
                bool cobreConexao = wallMinY <= z1MinY + 0.01f && wallMaxY >= z1MaxY - 0.01f;
                Assert.IsFalse(cobreConexao,
                    $"Parede '{wall.Name}' sela a conexão Zona1→Zona2 (faixa y {z1MinY}..{z1MaxY}).");
            }
        }

        [Test]
        public void Bug4_ParedesDeZ1_NaoDevemInvadirCasasDaZona2()
        {
            // Zona 1 e Zona 2 se sobrepõem de propósito só no chão (ver comentário
            // em BuildSPathLayout, seção Zona 2). Acontecia de a parede Sul da
            // Zona 1 atravessar o telhado da Casa_1 nessa faixa de sobreposição
            // (achado em playtest — a parede corria pelo comprimento inteiro,
            // sem descontar a faixa). Um resíduo de até ~CornerInset*WallThickness
            // nas pontas truncadas é esperado (mesma tolerância de canto usada em
            // toda porta do jogo); o que não pode é uma parede de verdade cortando
            // o miolo de uma casa.
            float toleranciaArea = _cfg.CornerInset * _cfg.WallThickness + 0.05f;

            var z1Walls = _layout.Walls.Where(w => w.ParentName == "Zona1_RuaEntrada").ToList();

            foreach (var house in _layout.Houses)
            {
                float houseMinX = house.Position.x - house.Size * 0.5f;
                float houseMaxX = house.Position.x + house.Size * 0.5f;
                float houseMinY = house.Position.y - house.Size * 0.5f;
                float houseMaxY = house.Position.y + house.Size * 0.5f;

                foreach (var wall in z1Walls)
                {
                    float wallMinX = wall.Center.x - wall.Size.x * 0.5f;
                    float wallMaxX = wall.Center.x + wall.Size.x * 0.5f;
                    float wallMinY = wall.Center.y - wall.Size.y * 0.5f;
                    float wallMaxY = wall.Center.y + wall.Size.y * 0.5f;

                    float overlapX = Mathf.Min(wallMaxX, houseMaxX) - Mathf.Max(wallMinX, houseMinX);
                    float overlapY = Mathf.Min(wallMaxY, houseMaxY) - Mathf.Max(wallMinY, houseMinY);
                    if (overlapX <= 0f || overlapY <= 0f) continue;

                    float area = overlapX * overlapY;
                    Assert.Less(area, toleranciaArea,
                        $"Parede '{wall.Name}' da Zona 1 invade a casa '{house.Name}' com área {area:F3} (além da tolerância de canto).");
                }
            }
        }

        [Test]
        public void Bug1_NenhumSegmentoDeParedeDeZ3_DeveFicarDentroDoChaoDeZ2()
        {
            var z2 = _layout.Rooms[1];
            float z2MinY = z2.Center.y - z2.Height * 0.5f;
            float z2MaxY = z2.Center.y + z2.Height * 0.5f;
            float z2MinX = z2.Center.x - z2.Width * 0.5f;
            float z2MaxX = z2.Center.x + z2.Width * 0.5f;

            var z3Walls = _layout.Walls.Where(w => w.ParentName == "Zona3_BecoDoVento").ToList();

            foreach (var wall in z3Walls)
            {
                float wallMaxY = wall.Center.y + wall.Size.y * 0.5f;
                float wallMinX = wall.Center.x - wall.Size.x * 0.5f;
                float wallMaxX = wall.Center.x + wall.Size.x * 0.5f;

                bool overlapY = wall.Center.y < z2MaxY && wallMaxY > z2MinY;
                bool overlapX = wallMinX < z2MaxX && wallMaxX > z2MinX;

                Assert.IsFalse(overlapY && overlapX,
                    $"Parede '{wall.Name}' da Zona 3 está dentro do chão da Zona 2 (sobreposição em XY).");
            }
        }

        // ── 4. BUG 2: Zona 3 → Zona 4 (parede Sul de Z3 cobre abertura de Z4) ─

        [Test]
        public void Bug2_ParedeSulDeZ3_NaoDeveBloquearAberturaDeZ4()
        {
            var z3 = _layout.Rooms[2];
            var z4 = _layout.Rooms[3];

            float z3SouthY = z3.Center.y - z3.Height * 0.5f;
            float z4NorthY = z4.Center.y + z4.Height * 0.5f;
            float z4MinX   = z4.Center.x - z4.Width * 0.5f;
            float z4MaxX   = z4.Center.x + z4.Width * 0.5f;

            // Paredes no lado Sul da Zona 3 (Y ≈ z3SouthY)
            var southWalls = _layout.Walls
                .Where(w => w.ParentName == "Zona3_BecoDoVento"
                            && Mathf.Abs(w.Center.y - z3SouthY) < _cfg.WallThickness)
                .ToList();

            // Calcula a cobertura total dessas paredes sobre a faixa X da Zona 4
            float totalCoveredOverOpeningX = 0f;
            foreach (var wall in southWalls)
            {
                float wMin = Mathf.Max(wall.Center.x - wall.Size.x * 0.5f, z4MinX);
                float wMax = Mathf.Min(wall.Center.x + wall.Size.x * 0.5f, z4MaxX);
                if (wMax > wMin) totalCoveredOverOpeningX += wMax - wMin;
            }

            Assert.Less(totalCoveredOverOpeningX, z4.Width,
                $"Paredes Sul da Zona 3 cobrem {totalCoveredOverOpeningX:F2}u de {z4.Width:F2}u da abertura da Zona 4 — entrada bloqueada.");
        }

        // ── 5. Praça do Cerco realmente acessível ───────────────────────────

        [Test]
        public void Zona4PracaDoCerco_DeveTermPelomenosUmaPortaUsavel()
        {
            var z3 = _layout.Rooms[2];
            float z3SouthY = z3.Center.y - z3.Height * 0.5f;

            var southWalls = _layout.Walls
                .Where(w => w.ParentName == "Zona3_BecoDoVento"
                            && Mathf.Abs(w.Center.y - z3SouthY) < _cfg.WallThickness)
                .OrderBy(w => w.Center.x)
                .ToList();

            float z3MinX = z3.Center.x - z3.Width * 0.5f;
            float z3MaxX = z3.Center.x + z3.Width * 0.5f;

            // Mede o maior gap contínuo entre segmentos de parede
            float maxGap = 0f;
            float prevEnd = z3MinX;
            foreach (var wall in southWalls)
            {
                float wallStart = wall.Center.x - wall.Size.x * 0.5f;
                maxGap = Mathf.Max(maxGap, wallStart - prevEnd);
                prevEnd = wall.Center.x + wall.Size.x * 0.5f;
            }
            maxGap = Mathf.Max(maxGap, z3MaxX - prevEnd);

            Assert.GreaterOrEqual(maxGap, 1.0f,
                $"Abertura máxima na parede Sul da Zona 3 é {maxGap:F2}u — menor que 1u, jogador não passa.");
        }

        // ── 6. Nenhuma parede com dimensão zero ou negativa ─────────────────

        [Test]
        public void TodasAsParedes_DevemTerDimensoesPositivas()
        {
            foreach (var wall in _layout.Walls)
            {
                Assert.Greater(wall.Size.x, 0f, $"Parede '{wall.Name}' (pai: {wall.ParentName}) tem largura ≤ 0.");
                Assert.Greater(wall.Size.y, 0f, $"Parede '{wall.Name}' (pai: {wall.ParentName}) tem altura ≤ 0.");
            }
        }

        // ── 7. Paredes contidas dentro do chão da sua própria sala ───────────

        [Test]
        public void TodasAsParedes_DevemFicarDentroDoChaoDesuaSala()
        {
            const float tolerance = 0.01f;
            var floorBySala = _layout.Floors.ToDictionary(f => f.ParentName, f => f);

            foreach (var wall in _layout.Walls)
            {
                if (!floorBySala.TryGetValue(wall.ParentName, out var floor)) continue;

                float floorMinX = floor.Center.x - floor.Size.x * 0.5f - tolerance;
                float floorMaxX = floor.Center.x + floor.Size.x * 0.5f + tolerance;
                float floorMinY = floor.Center.y - floor.Size.y * 0.5f - tolerance;
                float floorMaxY = floor.Center.y + floor.Size.y * 0.5f + tolerance;

                float wallMinX = wall.Center.x - wall.Size.x * 0.5f;
                float wallMaxX = wall.Center.x + wall.Size.x * 0.5f;
                float wallMinY = wall.Center.y - wall.Size.y * 0.5f;
                float wallMaxY = wall.Center.y + wall.Size.y * 0.5f;

                Assert.GreaterOrEqual(wallMinX, floorMinX, $"'{wall.Name}' ({wall.ParentName}) transborda para o Oeste do seu chão.");
                Assert.LessOrEqual(wallMaxX, floorMaxX,    $"'{wall.Name}' ({wall.ParentName}) transborda para o Leste do seu chão.");
                Assert.GreaterOrEqual(wallMinY, floorMinY, $"'{wall.Name}' ({wall.ParentName}) transborda para o Sul do seu chão.");
                Assert.LessOrEqual(wallMaxY, floorMaxY,    $"'{wall.Name}' ({wall.ParentName}) transborda para o Norte do seu chão.");
            }
        }

        // ── 8. Zona 5: Transição Dimensional (barreira anômala) ─────────────

        [Test]
        public void Bug3_ParedeSulDeZ4_DeveSerBarreiraAnomala_ELargarComOOverlapReal()
        {
            var z4 = _layout.Rooms[3];
            var z5 = _layout.Rooms[4];

            float z4SouthY = z4.Center.y - z4.Height * 0.5f;
            float z4MinX = z4.Center.x - z4.Width * 0.5f;
            float z4MaxX = z4.Center.x + z4.Width * 0.5f;
            float z5MinX = z5.Center.x - z5.Width * 0.5f;
            float z5MaxX = z5.Center.x + z5.Width * 0.5f;
            float expectedOverlap = Mathf.Min(z4MaxX, z5MaxX) - Mathf.Max(z4MinX, z5MinX);

            var anomalyWalls = _layout.Walls
                .Where(w => w.ParentName == "Zona4_PracaDoCerco"
                            && Mathf.Abs(w.Center.y - z4SouthY) < _cfg.WallThickness
                            && w.IsAnomalyBarrier)
                .ToList();

            Assert.AreEqual(1, anomalyWalls.Count, "Deve existir exatamente uma parede anômala na fronteira Sul da Zona 4.");
            Assert.AreEqual(expectedOverlap, anomalyWalls[0].Size.x, 0.01f,
                "Largura da barreira anômala deve ser igual à sobreposição real entre Zona4 e Zona5.");
        }

        [Test]
        public void Zona5TransicaoDimensional_NaoDeveDesenharParedePropriaNoNorte()
        {
            var z5 = _layout.Rooms[4];
            Assert.AreEqual(Side.North, z5.FullyOpenSides,
                "Zona5 não deve desenhar parede própria no lado Norte — a fronteira é de responsabilidade da Zona4 (convenção do S-Path).");

            bool hasOwnNorthWall = _layout.Walls.Any(w => w.ParentName == "Zona5_TransicaoDimensional"
                && Mathf.Abs(w.Center.y - (z5.Center.y + z5.Height * 0.5f)) < _cfg.WallThickness);
            Assert.IsFalse(hasOwnNorthWall, "Zona5 não deve ter nenhuma parede própria na fronteira Norte.");
        }

        [Test]
        public void NenhumaOutraParede_AlemDaFronteiraZ4Z5_DeveEstarMarcadaComoAnomalyBarrier()
        {
            var anomalyWalls = _layout.Walls.Where(w => w.IsAnomalyBarrier).ToList();
            Assert.AreEqual(1, anomalyWalls.Count,
                "Apenas a fronteira Zona4→Zona5 deve gerar parede anômala no layout atual.");
        }
    }
}
