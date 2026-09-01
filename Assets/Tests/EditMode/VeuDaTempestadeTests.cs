using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using FavelaAmarela.Core.Environment;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda o <b>véu da tempestade sobre o Templo do Povo Serpente</b>.
    ///
    /// <para><b>A ideia é do Vini (2026-09-01):</b> <i>"vamos esconder o templo debaixo da
    /// tempestade — se não tiver o mapa, a tempestade joga o Damião para outro canto."</i></para>
    ///
    /// <para>A regra é POCO: dá para afirmar o destino sem cena, sem Play Mode e sem
    /// tempestade.</para>
    /// </summary>
    public sealed class VeuDaTempestadeTests
    {
        private static DesorientacaoDaTempestade.Ponto P(float x, float y) =>
            new DesorientacaoDaTempestade.Ponto(x, y);

        /// <summary>Os quatro cantos de um mapa 80×60, para os testes lerem como o Deserto.</summary>
        private static DesorientacaoDaTempestade Regra() =>
            new DesorientacaoDaTempestade(P(-40, -30), P(-40, 30), P(40, -30), P(40, 30));

        // ── Quando age ────────────────────────────────────────────────────────

        [Test]
        public void ComACarta_ATempestadeNaoAge()
        {
            Assert.IsFalse(DesorientacaoDaTempestade.DeveArremessar(temACarta: true),
                "Quem tem a Carta das Areias atravessa. Se a tempestade agisse mesmo assim, a " +
                "carta não seria recompensa nenhuma.");
        }

        [Test]
        public void SemACarta_ATempestadeAge()
        {
            Assert.IsTrue(DesorientacaoDaTempestade.DeveArremessar(temACarta: false));
        }

        // ── Para onde manda ───────────────────────────────────────────────────

        /// <summary>
        /// <b>Nunca devolve ao mesmo canto.</b> Ser arremessado para dois passos de onde se
        /// estava faria a tempestade parecer quebrada em vez de perigosa — e o jogador
        /// simplesmente andaria de volta.
        /// </summary>
        [Test]
        public void NuncaArremessa_ParaOCantoOndeOJogadorJaEsta()
        {
            var regra = Regra();

            foreach (var canto in new[] { P(-40, -30), P(-40, 30), P(40, -30), P(40, 30) })
            {
                var destino = regra.Arremessar(canto);

                Assert.Greater(canto.DistanciaAte(destino), regra.RaioDoMesmoCanto,
                    $"De {canto} a tempestade devolveu para {destino} — perto demais para " +
                    "custar alguma coisa.");
            }
        }

        /// <summary>
        /// <b>Determinístico, e o mais distante.</b> Um jogador que aprende a regra aprende que
        /// o preço de insistir sem a carta é a travessia inteira de volta — e aprender uma regra
        /// é jogo, enquanto sofrer um sorteio é frustração.
        /// </summary>
        [Test]
        public void ArremessaSempre_ParaOCantoMaisDistante()
        {
            var regra = Regra();

            // Vindo do leste (onde fica o Templo), o destino tem de ser o oeste.
            var destino = regra.Arremessar(P(38, 10));

            Assert.Less(destino.X, 0f,
                $"Do lado do Templo (leste), a tempestade mandou para {destino} — o custo tem " +
                "de ser a travessia, e não um passo ao lado.");
        }

        [Test]
        public void OMesmoPonto_ProduzSempreODestinoIgual()
        {
            var regra = Regra();
            var de = P(38, 10);

            var a = regra.Arremessar(de);
            var b = regra.Arremessar(de);

            Assert.AreEqual(a.X, b.X, 0.001f, "A regra é determinística.");
            Assert.AreEqual(a.Y, b.Y, 0.001f);
        }

        [Test]
        public void UmCantoSo_EhRecusadoNaConstrucao()
        {
            Assert.Throws<ArgumentException>(() => new DesorientacaoDaTempestade(P(0, 0)),
                "Com um canto só a tempestade devolveria o jogador exatamente onde ele estava.");
        }

        // ── A ligação com o mundo ─────────────────────────────────────────────

        [Test]
        public void ACarta_ExisteComoItemDeChave()
        {
            var def = AssetDatabase.LoadAssetAtPath<ItemDef>(
                "Assets/FavelaAmarela/Config/Resources/Itens/Item_Chave_CartaDasAreias.asset");

            Assert.IsNotNull(def,
                "A Carta das Areias não existe. Sem ela o véu é intransponível e o Templo " +
                "fica inalcançável para sempre. Conserto: " +
                "'Tools/FavelaAmarela/Deserto: esconder o Templo sob a tempestade'.");

            Assert.AreEqual(ItemType.Chave, def.Tipo,
                "A carta virou outro tipo. Como Consumível ela se gastaria; como equipamento " +
                "ocuparia slot. Chave é o que não se perde.");
        }

        /// <summary>
        /// O véu e a carta <b>na cena</b>. É a checagem que este repositório mais precisa: a
        /// mecânica existir em código e não estar em cena nenhuma é o modo de falha que ele já
        /// catalogou dez vezes.
        /// </summary>
        [Test]
        public void OVeuEACarta_EstaoNoDeserto()
        {
            string yaml = File.ReadAllText("Assets/Scenes/Deserto_Hali.unity");

            StringAssert.Contains("Environment.VeuDaTempestade", yaml,
                "O véu não está no Deserto. O Templo volta a estar aberto desde o começo, e a " +
                "Carta das Areias não serve para nada.");

            StringAssert.Contains("Coletavel_CartaDasAreias", yaml,
                "A carta não está no mundo. O véu existiria sem chave, e o Templo ficaria " +
                "INALCANÇÁVEL — pior que não ter a mecânica.");
        }

        /// <summary>
        /// A carta tem de ficar <b>longe</b> do Templo. Semeá-la ao lado tornaria o véu um
        /// pedágio de dez segundos, e não uma razão para atravessar o Deserto.
        /// </summary>
        [Test]
        public void ACarta_FicaLongeDoTemplo()
        {
            string yaml = File.ReadAllText("Assets/Scenes/Deserto_Hali.unity");

            var carta = PosicaoDe(yaml, "Coletavel_CartaDasAreias");
            var templo = PosicaoDe(yaml, "Entrada_TemploSerpente");

            Assert.IsNotNull(carta, "Coletavel_CartaDasAreias sem posição legível.");
            Assert.IsNotNull(templo, "Entrada_TemploSerpente sem posição legível.");

            float d = (float)Math.Sqrt(Math.Pow(carta.Value.x - templo.Value.x, 2) +
                                       Math.Pow(carta.Value.y - templo.Value.y, 2));

            Assert.Greater(d, 40f,
                $"A carta está a {d:0.#} unidades do Templo. Perto demais transforma o véu num " +
                "pedágio em vez de numa razão para conhecer o Deserto.");
        }

        /// <summary>Lê a posição do Transform de um GameObject pelo nome, direto do YAML.</summary>
        private static (float x, float y)? PosicaoDe(string yaml, string nome)
        {
            var docs = System.Text.RegularExpressions.Regex.Matches(
                yaml, @"--- !u!(\d+) &(\d+)\n(.*?)(?=\n--- !u!|\Z)",
                System.Text.RegularExpressions.RegexOptions.Singleline);

            string alvo = null;

            foreach (System.Text.RegularExpressions.Match m in docs)
            {
                if (m.Groups[1].Value != "1") continue;

                var n = System.Text.RegularExpressions.Regex.Match(
                    m.Groups[3].Value, @"m_Name:\s*(.*)");

                if (n.Success && n.Groups[1].Value.Trim() == nome)
                {
                    alvo = m.Groups[2].Value;
                    break;
                }
            }

            if (alvo == null) return null;

            foreach (System.Text.RegularExpressions.Match m in docs)
            {
                if (m.Groups[1].Value != "4") continue;

                var go = System.Text.RegularExpressions.Regex.Match(
                    m.Groups[3].Value, @"m_GameObject:\s*\{fileID:\s*(\d+)\}");

                if (!go.Success || go.Groups[1].Value != alvo) continue;

                var p = System.Text.RegularExpressions.Regex.Match(
                    m.Groups[3].Value,
                    @"m_LocalPosition:\s*\{x:\s*([-\d.eE]+),\s*y:\s*([-\d.eE]+)");

                if (p.Success)
                    return (float.Parse(p.Groups[1].Value,
                                System.Globalization.CultureInfo.InvariantCulture),
                            float.Parse(p.Groups[2].Value,
                                System.Globalization.CultureInfo.InvariantCulture));
            }

            return null;
        }
    }
}
