using System.Collections.Generic;
using NUnit.Framework;
using FavelaAmarela.Core.Loot;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Suite EditMode do sorteio de espólio (<see cref="SorteioDeDrop"/>). Toda a regra é
    /// afirmada com uma fonte de aleatoriedade falsa e valores fixos — é exatamente para
    /// isso que a <see cref="IFonteDeAleatoriedade"/> é injetada em vez de estática.
    /// </summary>
    public class SorteioDeDropTests
    {
        /// <summary>Fonte determinística: devolve os valores enfileirados, em ordem.</summary>
        private sealed class FonteFake : IFonteDeAleatoriedade
        {
            private readonly Queue<float> _valores;
            private readonly Queue<int> _inteiros;

            public FonteFake(params float[] valores)
            {
                _valores = new Queue<float>(valores);
                _inteiros = new Queue<int>();
            }

            public FonteFake ComInteiros(params int[] inteiros)
            {
                foreach (var i in inteiros) _inteiros.Enqueue(i);
                return this;
            }

            public float ProximoValor() => _valores.Count > 0 ? _valores.Dequeue() : 1f;

            public int ProximoInteiro(int minInclusivo, int maxExclusivo)
                => _inteiros.Count > 0 ? _inteiros.Dequeue() : minInclusivo;
        }

        private static CandidatoDeDrop Entrada(string id, float chance = 0.5f, bool garantido = false,
            int nivelMinimo = 1, int qtdMin = 1, int qtdMax = 1)
            => new CandidatoDeDrop(id, GrauDeImpregnacao.Inerte, garantido, chance, qtdMin, qtdMax, nivelMinimo);

        // ── Sortear (espólio de inimigo) ──────────────────────────────────────

        [Test]
        public void Garantido_CaiMesmoComFonteAdversa()
        {
            var tabela = new List<CandidatoDeDrop> { Entrada("elmo_de_set", chance: 0f, garantido: true) };

            var caiu = new SorteioDeDrop().Sortear(tabela, nivelDoJogador: 1, new FonteFake(0.99f), tetoDeItens: 0);

            Assert.AreEqual(1, caiu.Count);
            Assert.AreEqual("elmo_de_set", caiu[0].ItemDefId);
        }

        [Test]
        public void Garantido_IgnoraNivelMinimoAcimaDoJogador()
        {
            var tabela = new List<CandidatoDeDrop> { Entrada("coroa", garantido: true, nivelMinimo: 10) };

            var caiu = new SorteioDeDrop().Sortear(tabela, nivelDoJogador: 1, new FonteFake(0.99f), tetoDeItens: 0);

            Assert.AreEqual(1, caiu.Count);
        }

        [Test]
        public void NivelMinimoAcimaDoJogador_EntradaFiltrada()
        {
            var tabela = new List<CandidatoDeDrop> { Entrada("impregnado", chance: 1f, nivelMinimo: 5) };

            var caiu = new SorteioDeDrop().Sortear(tabela, nivelDoJogador: 4, new FonteFake(0f), tetoDeItens: 0);

            Assert.AreEqual(0, caiu.Count);
        }

        [Test]
        public void NivelMinimoAlcancado_EntradaVoltaAoSorteio()
        {
            var tabela = new List<CandidatoDeDrop> { Entrada("impregnado", chance: 1f, nivelMinimo: 5) };

            var caiu = new SorteioDeDrop().Sortear(tabela, nivelDoJogador: 5, new FonteFake(0f), tetoDeItens: 0);

            Assert.AreEqual(1, caiu.Count);
            Assert.AreEqual("impregnado", caiu[0].ItemDefId);
        }

        [Test]
        public void ChanceAbaixoDoSorteio_NaoCai()
        {
            var tabela = new List<CandidatoDeDrop> { Entrada("capuz", chance: 0.2f) };

            // 0,5 não é < 0,2.
            var caiu = new SorteioDeDrop().Sortear(tabela, nivelDoJogador: 1, new FonteFake(0.5f), tetoDeItens: 0);

            Assert.AreEqual(0, caiu.Count);
        }

        [Test]
        public void CadaEntrada_RolaDeFormaIndependente()
        {
            var tabela = new List<CandidatoDeDrop>
            {
                Entrada("capuz", chance: 0.5f),
                Entrada("colete", chance: 0.5f),
                Entrada("caneleiras", chance: 0.5f)
            };

            // Só a segunda rolagem passa.
            var caiu = new SorteioDeDrop().Sortear(tabela, nivelDoJogador: 1,
                new FonteFake(0.9f, 0.1f, 0.9f), tetoDeItens: 0);

            Assert.AreEqual(1, caiu.Count);
            Assert.AreEqual("colete", caiu[0].ItemDefId);
        }

        [Test]
        public void TetoDeItens_LimitaAResolucao()
        {
            var tabela = new List<CandidatoDeDrop>
            {
                Entrada("capuz", chance: 1f),
                Entrada("colete", chance: 1f),
                Entrada("caneleiras", chance: 1f)
            };

            var caiu = new SorteioDeDrop().Sortear(tabela, nivelDoJogador: 1,
                new FonteFake(0f, 0f, 0f), tetoDeItens: 2);

            Assert.AreEqual(2, caiu.Count);
        }

        [Test]
        public void MesmoItemDuasVezes_CaiUmaSo()
        {
            var tabela = new List<CandidatoDeDrop>
            {
                Entrada("capuz", chance: 1f),
                Entrada("capuz", chance: 1f)
            };

            var caiu = new SorteioDeDrop().Sortear(tabela, nivelDoJogador: 1,
                new FonteFake(0f, 0f), tetoDeItens: 0);

            Assert.AreEqual(1, caiu.Count);
        }

        [Test]
        public void QuantidadeEmFaixa_ConsultaAFonte()
        {
            var tabela = new List<CandidatoDeDrop> { Entrada("erva", chance: 1f, qtdMin: 1, qtdMax: 3) };

            var caiu = new SorteioDeDrop().Sortear(tabela, nivelDoJogador: 1,
                new FonteFake(0f).ComInteiros(3), tetoDeItens: 0);

            Assert.AreEqual(3, caiu[0].Quantidade);
        }

        [Test]
        public void QuantidadeFixa_NaoConsultaAFonte()
        {
            var tabela = new List<CandidatoDeDrop> { Entrada("capuz", chance: 1f, qtdMin: 2, qtdMax: 2) };

            var caiu = new SorteioDeDrop().Sortear(tabela, nivelDoJogador: 1,
                new FonteFake(0f).ComInteiros(99), tetoDeItens: 0);

            Assert.AreEqual(2, caiu[0].Quantidade);
        }

        [Test]
        public void TabelaVaziaOuNula_NadaCai()
        {
            var sorteio = new SorteioDeDrop();

            Assert.AreEqual(0, sorteio.Sortear(null, 1, new FonteFake(0f), 0).Count);
            Assert.AreEqual(0, sorteio.Sortear(new List<CandidatoDeDrop>(), 1, new FonteFake(0f), 0).Count);
        }

        // ── SortearUm (baú) ───────────────────────────────────────────────────

        [Test]
        public void SortearUm_EscolhePeloPesoAcumulado()
        {
            var tabela = new List<CandidatoDeDrop>
            {
                Entrada("cravo", chance: 1f),
                Entrada("estilete", chance: 1f),
                Entrada("alfanje", chance: 1f)
            };

            // Peso total 3; 0,5 × 3 = 1,5 → cai na segunda faixa [1, 2).
            var escolhido = new SorteioDeDrop().SortearUm(tabela, nivelDoJogador: 1, new FonteFake(0.5f));

            Assert.IsTrue(escolhido.HasValue);
            Assert.AreEqual("estilete", escolhido.Value.ItemDefId);
        }

        [Test]
        public void SortearUm_PesosIguais_PrimeiraFaixaComValorZero()
        {
            var tabela = new List<CandidatoDeDrop>
            {
                Entrada("cravo", chance: 1f),
                Entrada("estilete", chance: 1f)
            };

            var escolhido = new SorteioDeDrop().SortearUm(tabela, nivelDoJogador: 1, new FonteFake(0f));

            Assert.AreEqual("cravo", escolhido.Value.ItemDefId);
        }

        [Test]
        public void SortearUm_RespeitaNivelMinimo()
        {
            var tabela = new List<CandidatoDeDrop>
            {
                Entrada("cravo", chance: 1f, nivelMinimo: 9),
                Entrada("estilete", chance: 1f, nivelMinimo: 1)
            };

            // Com nível 1, só o Estilete é elegível — qualquer rolagem cai nele.
            var escolhido = new SorteioDeDrop().SortearUm(tabela, nivelDoJogador: 1, new FonteFake(0f));

            Assert.AreEqual("estilete", escolhido.Value.ItemDefId);
        }

        [Test]
        public void SortearUm_SemElegiveis_DevolveNulo()
        {
            var tabela = new List<CandidatoDeDrop> { Entrada("cravo", chance: 1f, nivelMinimo: 9) };

            var escolhido = new SorteioDeDrop().SortearUm(tabela, nivelDoJogador: 1, new FonteFake(0f));

            Assert.IsFalse(escolhido.HasValue);
        }

        [Test]
        public void SortearUm_TabelaVazia_DevolveNulo()
        {
            var sorteio = new SorteioDeDrop();

            Assert.IsFalse(sorteio.SortearUm(null, 1, new FonteFake(0f)).HasValue);
            Assert.IsFalse(sorteio.SortearUm(new List<CandidatoDeDrop>(), 1, new FonteFake(0f)).HasValue);
        }
    }
}
