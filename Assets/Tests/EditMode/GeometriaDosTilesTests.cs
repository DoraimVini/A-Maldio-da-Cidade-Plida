using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a <b>geometria de colisão dos tiles</b> — o defeito clássico do isométrico em
    /// Unity, e o mais invisível.
    ///
    /// <para><b>O que <c>ColliderType</c> decide.</b> <c>Grid</c> faz o colisor seguir o
    /// <b>losango da célula</b>; <c>Sprite</c> o deriva do contorno do PNG, que num sprite
    /// isométrico é o <b>retângulo</b> em volta do losango — e os cantos vazios viram parede. O
    /// sintoma em jogo é o jogador "encostando no nada" perto das quinas, e isso se lê como
    /// controle ruim, nunca como colisão errada.</para>
    ///
    /// <para><b>O que a auditoria de 2026-08-31 encontrou.</b> Os tiles de colisão estavam
    /// certos (<c>Grid</c>), mas os <b>cinco tiles de areia — que são CHÃO — estavam em
    /// <c>Sprite</c></b>, gerando geometria de colisão para cada célula do Deserto. Ficava
    /// inerte porque o tilemap de chão não tem <c>TilemapCollider2D</c>: era <b>mina, não
    /// bug</b>. No dia em que alguém acrescentasse um, o Deserto inteiro viraria parede.</para>
    /// </summary>
    public sealed class GeometriaDosTilesTests
    {
        private const string Pasta = "Assets/FavelaAmarela/Art/Tiles";

        private static int ColisaoDe(string caminho)
        {
            var m = Regex.Match(File.ReadAllText(caminho), @"m_ColliderType:\s*(\d)");
            return m.Success ? int.Parse(m.Groups[1].Value) : -1;
        }

        private static string Nome(string c) => Path.GetFileNameWithoutExtension(c);

        /// <summary>
        /// Chão não colide. Um piso com colisor é, na melhor das hipóteses, geometria
        /// desperdiçada em cada célula do mapa; na pior, um mundo intransitável.
        /// </summary>
        [Test]
        public void TileDeChao_NaoColide()
        {
            var errados = Directory.GetFiles(Pasta, "*.asset")
                .Where(c => Nome(c).StartsWith("sand_", StringComparison.Ordinal) ||
                            Nome(c).Contains("piso"))
                .Where(c => ColisaoDe(c) != 0)
                .Select(c => $"{Nome(c)}: ColliderType {ColisaoDe(c)} (deveria ser 0 = None)")
                .ToList();

            Assert.IsEmpty(errados,
                "Tile(s) de chão gerando colisão:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", errados));
        }

        /// <summary>
        /// Tile que bloqueia usa <c>Grid</c>, e nunca <c>Sprite</c>: só <c>Grid</c> dá o losango
        /// da célula num grid isométrico.
        /// </summary>
        [Test]
        public void TileQueBloqueia_UsaOLosangoDaCelula()
        {
            var errados = Directory.GetFiles(Pasta, "*.asset")
                .Where(c => Nome(c).Contains("colisao") || Nome(c).Contains("Colisao"))
                .Where(c => ColisaoDe(c) != 2)
                .Select(c => $"{Nome(c)}: ColliderType {ColisaoDe(c)} (deveria ser 2 = Grid)")
                .ToList();

            Assert.IsEmpty(errados,
                "Tile(s) de colisão com geometria errada:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", errados) + Environment.NewLine +
                "Com Sprite, os cantos vazios do losango viram parede e o jogador encosta no " +
                "nada perto das quinas.");
        }

        /// <summary>
        /// <b>Nenhum</b> tile do projeto pode ficar em <c>Sprite</c>. Num isométrico esse valor
        /// não tem uso legítimo: ou a célula bloqueia (Grid) ou não bloqueia (None).
        /// </summary>
        [Test]
        public void NenhumTile_DerivaColisaoDoContornoDoPNG()
        {
            var suspeitos = Directory.GetFiles(Pasta, "*.asset", SearchOption.AllDirectories)
                .Where(c => ColisaoDe(c) == 1)
                .Select(Nome)
                .ToList();

            Assert.IsEmpty(suspeitos,
                "Tile(s) em ColliderType.Sprite: " + string.Join(", ", suspeitos) +
                Environment.NewLine + "Num grid isométrico, Sprite dá o RETÂNGULO do PNG em vez " +
                "do losango. Use Grid para bloquear e None para chão.");
        }

        // ── Rule Tiles ────────────────────────────────────────────────────────

        private const string Regras = "Assets/FavelaAmarela/Art/Tiles/Regras";

        /// <summary>
        /// Os pincéis existem. Sem eles, montar uma sala é colocar tile a tile — o gargalo que
        /// decide se a Dungeon 2 sai.
        /// </summary>
        [Test]
        public void OsRuleTiles_Existem()
        {
            foreach (var nome in new[] { "RuleTile_Areia", "RuleTile_Muro" })
                Assert.IsTrue(File.Exists($"{Regras}/{nome}.asset"),
                    $"{nome} não existe. Conserto: " +
                    "'Tools/FavelaAmarela/Arte: montar os Rule Tiles'.");
        }

        /// <summary>
        /// O pincel de muro precisa <b>colidir em losango</b> — é ele que torna o Deserto
        /// construível, e um muro que não bloqueia é decoração.
        /// </summary>
        [Test]
        public void OPincelDeMuro_ColideEmLosango()
        {
            string caminho = $"{Regras}/RuleTile_Muro.asset";
            Assert.IsTrue(File.Exists(caminho), "RuleTile_Muro não existe.");

            string yaml = File.ReadAllText(caminho);

            StringAssert.Contains("m_DefaultColliderType: 2", yaml,
                "O pincel de muro parou de colidir em Grid. Pintar ruína deixaria de produzir " +
                "obstáculo — e o Deserto continuaria sendo um plano aberto.");
        }

        /// <summary>
        /// O pincel de areia sorteia entre as variações. Uma variação só devolve o Deserto
        /// atual: um tile repetido cuja repetição se vê da tela inteira.
        /// </summary>
        [Test]
        public void OPincelDeAreia_SorteiaVariacoes()
        {
            string yaml = File.ReadAllText($"{Regras}/RuleTile_Areia.asset");

            StringAssert.Contains("m_Output: 1", yaml,
                "O pincel de areia deixou de sortear (Output Random).");

            int variacoes = Regex.Matches(yaml, @"fileID: 21300000").Count;

            Assert.GreaterOrEqual(variacoes, 4,
                $"O pincel de areia tem {variacoes} sprite(s) — com uma variação só, a " +
                "repetição do chão volta a se ver da tela inteira.");

            StringAssert.Contains("m_DefaultColliderType: 0", yaml,
                "O pincel de areia passou a colidir. Chão não colide.");
        }
    }
}
