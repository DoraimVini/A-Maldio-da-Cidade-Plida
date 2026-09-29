using System.Collections.Generic;
using System.Linq;
using FavelaAmarela.Core.Enemies;
using FavelaAmarela.Runtime.Enemies;
using FavelaAmarela.Runtime.Itens;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// As regras de geometria do Rito do Olhar (plano, §6.2), medidas <b>na cena do Castelo</b>
    /// com a mesma conta que o jogo faz (<see cref="LinhaDeVisao"/> contra a pegada de cada
    /// <see cref="CoberturaDoTrono"/>, a partir de <see cref="ReiEmAmareloAI.OrigemDoOlhar"/>).
    ///
    /// <para><b>Por que existe.</b> Uma luta de linha de visão quebra em silêncio quando alguém
    /// arrasta uma estátua: o Altar passa a ficar coberto (o selo avança sem custo), um fragmento
    /// cai na sombra (a Fase 3 perde a decisão) ou a sombra mais perto fica longe demais (a
    /// Fase 1 vira corrida). Nada disso dá erro; tudo isso muda a luta. As posições saíram de uma
    /// busca que já passava nestas regras — este guarda impede que deixem de passar.</para>
    /// </summary>
    public sealed class OTronoDoOlharTests
    {
        private const string Cena = "Assets/Scenes/Castelo_Carcosa.unity";

        /// <summary>Plano, §6.2 regra 2: sombra a ≤ 1,5 s correndo, ~11 un.</summary>
        private const float SombraMaisLongePermitida = 11f;

        /// <summary>Plano, §6.2 regra 3: a sombra cabe duas larguras do Damião (0,6 un).</summary>
        private const float LarguraMinimaDaSombra = 1.2f;

        private const float Passo = 0.25f;

        private ReiEmAmareloAI _rei;
        private Tilemap _chao;
        private Vector2 _olho;
        private List<CaixaDeCobertura> _caixas;

        [OneTimeSetUp]
        public void Abrir()
        {
            EditorSceneManager.OpenScene(Cena, OpenSceneMode.Single);
            _rei = Object.FindAnyObjectByType<ReiEmAmareloAI>(FindObjectsInactive.Include);
            Assert.NotNull(_rei, "Nenhum ReiEmAmareloAI no Castelo.");
            _chao = Object.FindObjectsByType<Tilemap>().FirstOrDefault(t => t.name == "Piso_Castelo");
            Assert.NotNull(_chao, "Sem o Tilemap 'Piso_Castelo'.");
            _olho = _rei.OrigemDoOlhar;
            _caixas = Nobres().Select(n => n.Caixa).ToList();
        }

        private CoberturaDoTrono[] Nobres()
        {
            var nobres = Campo<CoberturaDoTrono[]>("coberturas");
            Assert.IsNotNull(nobres);
            Assert.AreEqual(5, nobres.Length, "O rito foi desenhado com 5 Nobres (o Verbo desfaz 4).");
            Assert.IsFalse(nobres.Any(n => n == null), "Há Nobre vazio na lista do Rei.");
            return nobres;
        }

        private T Campo<T>(string nome) where T : class
        {
            var so = new UnityEditor.SerializedObject(_rei);
            var p = so.FindProperty(nome);
            Assert.NotNull(p, $"O Rei perdeu o campo '{nome}'.");
            if (p.isArray && typeof(T).IsArray)
            {
                var tipo = typeof(T).GetElementType();
                var arr = System.Array.CreateInstance(tipo, p.arraySize);
                for (int i = 0; i < p.arraySize; i++) arr.SetValue(p.GetArrayElementAtIndex(i).objectReferenceValue, i);
                return arr as T;
            }
            return p.objectReferenceValue as T;
        }

        private bool TemChao(Vector2 p) => _chao.HasTile(_chao.WorldToCell(p));

        private bool Coberto(Vector2 p) => _caixas.Any(c => LinhaDeVisao.Cruza(_olho, p, c));

        private static IEnumerable<Vector2> Elipse(Vector2 centro, float ax, float ay)
        {
            foreach (float r in new[] { 0f, 0.5f, 1f })
                for (int i = 0; i < 24; i++)
                {
                    float t = i / 24f * Mathf.PI * 2f;
                    yield return centro + new Vector2(r * ax * Mathf.Cos(t), r * ay * Mathf.Sin(t));
                }
        }

        private IEnumerable<Vector2> SombraDe(CaixaDeCobertura caixa)
        {
            for (float x = -16f; x <= 16f; x += Passo)
            for (float y = 53f; y <= 72f; y += Passo)
            {
                var p = new Vector2(x, y);
                if (caixa.Contem(p) || !TemChao(p)) continue;
                if (LinhaDeVisao.Cruza(_olho, p, caixa)) yield return p;
            }
        }

        // ── A ligação ────────────────────────────────────────────────────────

        [Test]
        public void ORei_TemAltarNobresFragmentosEEco()
        {
            Assert.NotNull(Campo<AltarDeSelamento>("altar"), "Rode 'Tools/FavelaAmarela/Trono: montar o Rito do Olhar'.");
            var fragmentos = Campo<PontoFocalDeReliquia[]>("altaresDeFragmento");
            Assert.AreEqual(3, fragmentos.Length);
            CollectionAssert.AreEquivalent(_rei.ReliquiasExigidas, fragmentos.Select(f => f.ArtefatoId),
                "Cada relíquia tem um altar de fragmento.");
            var eco = Campo<EcoDeCarcosa>("eco");
            Assert.NotNull(eco, "Sem o Eco da Queda, a Fase 5 não pune quem para.");
            Assert.IsFalse(eco.gameObject.activeSelf, "O Eco da Queda nasce inativo; o Rei o liga na Fase 5.");
        }

        // ── As seis regras ───────────────────────────────────────────────────

        [Test]
        public void Regra1_OAltarEstaExposto_ComTodosOsNobresDePe()
        {
            var altar = Campo<AltarDeSelamento>("altar");
            foreach (var p in Elipse(altar.transform.position, altar.SemiEixoX, altar.SemiEixoY))
            {
                Assert.IsTrue(TemChao(p), $"O Altar chega a {p}, onde não há chão.");
                Assert.IsFalse(Coberto(p), $"O ponto {p} do Altar está na sombra de um Nobre: ali o selo avançaria sem custo.");
            }
        }

        [Test]
        public void Regra2_DeQualquerPontoDoAltar_HaSombraPerto()
        {
            var altar = Campo<AltarDeSelamento>("altar");
            var sombras = _caixas.SelectMany(SombraDe).ToList();
            Assert.IsNotEmpty(sombras);

            foreach (var p in Elipse(altar.transform.position, altar.SemiEixoX, altar.SemiEixoY))
            {
                float d = sombras.Min(s => Vector2.Distance(p, s));
                Assert.LessOrEqual(d, SombraMaisLongePermitida,
                    $"Do ponto {p} do Altar a sombra mais perto está a {d:F1} un.");
            }
        }

        [Test]
        public void Regra3_TodaSombraCabeODamiao()
        {
            foreach (var caixa in _caixas)
            {
                Vector2 centro = (caixa.Min + caixa.Max) * 0.5f;
                Vector2 dir = (centro - _olho).normalized;
                Vector2 perp = new Vector2(-dir.y, dir.x);

                // O ponto 1 un além da pegada, ao longo da linha do olho.
                float t = 0f;
                while (caixa.Contem(centro + dir * t)) t += 0.02f;
                Vector2 atras = centro + dir * (t + 1f);

                float largura = 0f;
                for (float s = -3f; s <= 3f; s += 0.02f)
                {
                    var p = atras + perp * s;
                    if (TemChao(p) && LinhaDeVisao.Cruza(_olho, p, caixa)) largura += 0.02f;
                }

                Assert.GreaterOrEqual(largura, LarguraMinimaDaSombra,
                    $"A sombra do Nobre em {centro} tem {largura:F2} un a 1 un da pedra — o Damião mal cabe.");
            }
        }

        [Test]
        public void Regra4_OsFragmentosEstaoExpostos()
        {
            foreach (var f in Campo<PontoFocalDeReliquia[]>("altaresDeFragmento"))
                foreach (var p in Elipse(f.transform.position, f.SemiEixoX, f.SemiEixoY))
                {
                    Assert.IsTrue(TemChao(p), $"O fragmento '{f.ArtefatoId}' chega a {p}, onde não há chão.");
                    Assert.IsFalse(Coberto(p), $"O fragmento '{f.ArtefatoId}' está na sombra em {p}: lê-lo não custaria nada.");
                }
        }

        [Test]
        public void Regra5_OVerboDesfazDoMaisPertoAoMaisLonge()
        {
            var altar = Campo<AltarDeSelamento>("altar");
            var ordem = ReiEmAmareloAI.OrdenarParaOVerbo(Nobres(), altar.transform.position);

            float Sombra(CoberturaDoTrono n) =>
                SombraDe(n.Caixa).Min(s => Vector2.Distance(altar.transform.position, s));

            float primeira = Sombra(ordem[0]);
            float ultima = Sombra(ordem[ordem.Length - 1]);
            Assert.Less(primeira, ultima,
                $"O primeiro Nobre a cair tem a sombra a {primeira:F1} un do Altar e o último (o que " +
                $"resiste) a {ultima:F1}: a cobertura tem de ir ficando mais longe, não mais perto.");
        }

        [Test]
        public void Regra6_ONobreNaoFechaAEntradaNemTocaOsAltares()
        {
            var altar = Campo<AltarDeSelamento>("altar");
            var fragmentos = Campo<PontoFocalDeReliquia[]>("altaresDeFragmento");

            foreach (var caixa in _caixas)
            {
                foreach (var canto in new[] { caixa.Min, caixa.Max, new Vector2(caixa.Min.x, caixa.Max.y), new Vector2(caixa.Max.x, caixa.Min.y) })
                    Assert.IsTrue(TemChao(canto), $"A pegada de um Nobre sai do chão em {canto}.");

                foreach (var p in Elipse(altar.transform.position, altar.SemiEixoX, altar.SemiEixoY)
                             .Concat(fragmentos.SelectMany(f => Elipse(f.transform.position, f.SemiEixoX, f.SemiEixoY))))
                    Assert.IsFalse(caixa.Contem(p), $"Um Nobre ocupa {p}, dentro de um altar.");
            }

            // Do vértice sul (a entrada) ao Altar, o caminho reto é livre.
            for (float y = 55f; y <= 62f; y += 0.2f)
                Assert.IsFalse(_caixas.Any(c => c.Contem(new Vector2(0f, y))), $"Um Nobre fecha a entrada em y={y:F1}.");
        }
    }
}
