using System.Linq;
using FavelaAmarela.Level.Core;
using NUnit.Framework;
using UnityEngine;

namespace FavelaAmarela.Level.Tests
{
    /// <summary>
    /// Suite NUnit (EditMode) para o <see cref="DesertOverworldPlanner"/>.
    /// Roda sem cena, sem Play Mode — POCO puro.
    ///
    /// Cobre:
    ///   1. Chão único e quatro limites de perímetro presentes.
    ///   2. Os cinco pontos de interesse gerados, sem duplicar categoria.
    ///   3. Todo ponto de interesse cai dentro do chão do deserto.
    ///   4. Só a entrada da Tumba (e os portões, se houver cena) carregam CenaDestino.
    ///   5. Nenhum limite com dimensão negativa/zero; perímetro cerca o chão.
    /// </summary>
    [TestFixture]
    public class DesertOverworldPlannerTests
    {
        private DesertOverworldLayout _layout;
        private DesertOverworldConfig _cfg;

        [SetUp]
        public void SetUp()
        {
            _cfg = new DesertOverworldConfig();
            _layout = DesertOverworldPlanner.BuildLayout(_cfg);
        }

        // ── 1. Estrutura básica ──────────────────────────────────────────────

        [Test]
        public void Layout_TemUmChaoEQuatroLimites()
        {
            Assert.AreEqual(1, _layout.Floors.Count, "Deserto deve ter exatamente um chão aberto.");
            Assert.AreEqual(4, _layout.Walls.Count, "Deserto deve ter exatamente quatro limites de perímetro.");

            var nomes = _layout.Walls.Select(w => w.Name).ToArray();
            CollectionAssert.AreEquivalent(
                new[] { "Limite_Norte", "Limite_Sul", "Limite_Leste", "Limite_Oeste" }, nomes);
        }

        [Test]
        public void Chao_TemTamanhoDaConfig()
        {
            var floor = _layout.Floors[0];
            Assert.AreEqual(_cfg.Width, floor.Size.x, 0.0001f);
            Assert.AreEqual(_cfg.Height, floor.Size.y, 0.0001f);
        }

        // ── 2. Pontos de interesse ───────────────────────────────────────────

        [Test]
        public void PontosDeInteresse_SaoCincoESemCategoriaDuplicada()
        {
            Assert.AreEqual(5, _layout.PointsOfInterest.Count);

            var categorias = _layout.PointsOfInterest.Select(p => p.Kind).ToArray();
            CollectionAssert.AllItemsAreUnique(categorias);
            CollectionAssert.Contains(categorias, PointOfInterestKind.PlayerSpawn);
            CollectionAssert.Contains(categorias, PointOfInterestKind.EntradaTumbaAlhazred);
            CollectionAssert.Contains(categorias, PointOfInterestKind.PortoesDasRuinas);
        }

        [Test]
        public void TodoPontoDeInteresse_CaiDentroDoChao()
        {
            float halfW = _cfg.Width * 0.5f;
            float halfH = _cfg.Height * 0.5f;

            foreach (var poi in _layout.PointsOfInterest)
            {
                Assert.LessOrEqual(Mathf.Abs(poi.Position.x), halfW,
                    $"'{poi.Name}' está fora do chão no eixo X.");
                Assert.LessOrEqual(Mathf.Abs(poi.Position.y), halfH,
                    $"'{poi.Name}' está fora do chão no eixo Y.");
            }
        }

        // ── 2b. Lago de Hali (barreira interna) ──────────────────────────────

        [Test]
        public void Lago_ExisteEEstaDentroDoChao()
        {
            Assert.IsTrue(_layout.Lago.HasValue, "O Lago de Hali deve ser gerado com a config padrão.");
            var lago = _layout.Lago.Value;

            float halfW = _cfg.Width * 0.5f;
            float halfH = _cfg.Height * 0.5f;
            Assert.LessOrEqual(Mathf.Abs(lago.Center.x) + lago.Size.x * 0.5f, halfW + 0.0001f,
                "O Lago vaza do chão no eixo X.");
            Assert.LessOrEqual(Mathf.Abs(lago.Center.y) + lago.Size.y * 0.5f, halfH + 0.0001f,
                "O Lago vaza do chão no eixo Y.");
        }

        [Test]
        public void NenhumPontoDeInteresse_CaiDentroDoLago()
        {
            Assert.IsTrue(_layout.Lago.HasValue);
            var lago = _layout.Lago.Value;
            float hx = lago.Size.x * 0.5f;
            float hy = lago.Size.y * 0.5f;

            foreach (var poi in _layout.PointsOfInterest)
            {
                bool dentro = Mathf.Abs(poi.Position.x - lago.Center.x) < hx
                           && Mathf.Abs(poi.Position.y - lago.Center.y) < hy;
                Assert.IsFalse(dentro, $"'{poi.Name}' cai dentro do Lago de Hali (impassável).");
            }
        }

        [Test]
        public void Lago_PodeSerDesligadoZerandoOTamanho()
        {
            var cfg = new DesertOverworldConfig { LagoSize = Vector2.zero };
            var layout = DesertOverworldPlanner.BuildLayout(cfg);
            Assert.IsFalse(layout.Lago.HasValue, "LagoSize zerado deve resultar em nenhum Lago.");
        }

        // ── 3. Portais / cena destino ────────────────────────────────────────

        [Test]
        public void EntradaTumba_CarregaCenaDoSPath()
        {
            var tumba = _layout.PointsOfInterest.Single(p => p.Kind == PointOfInterestKind.EntradaTumbaAlhazred);
            Assert.AreEqual(_cfg.CenaTumbaAlhazred, tumba.CenaDestino);
            Assert.IsNotEmpty(tumba.CenaDestino, "A Tumba deve ter cena destino (o drop do Necronomicon vive lá).");
        }

        [Test]
        public void PontosSemNavegacao_NaoTemCenaDestino()
        {
            var spawn = _layout.PointsOfInterest.Single(p => p.Kind == PointOfInterestKind.PlayerSpawn);
            var santuario = _layout.PointsOfInterest.Single(p => p.Kind == PointOfInterestKind.SantuarioYhtill);
            var templo = _layout.PointsOfInterest.Single(p => p.Kind == PointOfInterestKind.EntradaTemploSerpente);

            Assert.IsEmpty(spawn.CenaDestino);
            Assert.IsEmpty(santuario.CenaDestino);
            Assert.IsEmpty(templo.CenaDestino, "Templo da Serpente ainda não tem cena; destino deve ficar vazio.");
        }

        // ── 4. Robustez do perímetro ─────────────────────────────────────────

        [Test]
        public void Limites_NaoTemDimensaoNegativaOuZero()
        {
            foreach (var w in _layout.Walls)
            {
                Assert.Greater(w.Size.x, 0f, $"'{w.Name}' com largura não-positiva.");
                Assert.Greater(w.Size.y, 0f, $"'{w.Name}' com altura não-positiva.");
            }
        }

        [Test]
        public void Limites_CercamOChaoNosQuatroLados()
        {
            float halfW = _cfg.Width * 0.5f;
            float halfH = _cfg.Height * 0.5f;

            var norte = _layout.Walls.Single(w => w.Name == "Limite_Norte");
            var sul = _layout.Walls.Single(w => w.Name == "Limite_Sul");
            var leste = _layout.Walls.Single(w => w.Name == "Limite_Leste");
            var oeste = _layout.Walls.Single(w => w.Name == "Limite_Oeste");

            Assert.Greater(norte.Center.y, 0f, "Limite Norte deve ficar na metade positiva de Y.");
            Assert.Less(sul.Center.y, 0f, "Limite Sul deve ficar na metade negativa de Y.");
            Assert.Greater(leste.Center.x, 0f, "Limite Leste deve ficar na metade positiva de X.");
            Assert.Less(oeste.Center.x, 0f, "Limite Oeste deve ficar na metade negativa de X.");

            // Os limites N/S correm ao longo de X (largura = Width); L/O ao longo de Y (altura = Height).
            Assert.AreEqual(_cfg.Width, norte.Size.x, 0.0001f);
            Assert.AreEqual(_cfg.Height, leste.Size.y, 0.0001f);
        }
    }
}
