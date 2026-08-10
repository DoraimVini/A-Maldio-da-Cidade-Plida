using NUnit.Framework;
using FavelaAmarela.Core.Interaction;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Suite EditMode do <see cref="SeletorDeInteracao"/> — a regra de "qual alvo o
    /// Damião interage quando aperta o botão". POCO puro, sem cena.
    /// </summary>
    public class SeletorDeInteracaoTests
    {
        private static CandidatoDeInteracao C(int id, float dist, bool disp = true, int prio = 0)
            => new CandidatoDeInteracao(id, dist, disp, prio);

        [Test]
        public void SemCandidatos_NaoSelecionaNada()
        {
            var seletor = new SeletorDeInteracao(2f);
            Assert.IsNull(seletor.Selecionar(new CandidatoDeInteracao[0], 0));
            Assert.IsNull(seletor.Selecionar(null, 0));
        }

        [Test]
        public void SelecionaOMaisProximo()
        {
            var seletor = new SeletorDeInteracao(5f);
            var buffer = new[] { C(10, 3f), C(20, 1f), C(30, 2f) };
            Assert.AreEqual(20, seletor.Selecionar(buffer, 3));
        }

        [Test]
        public void IgnoraAlvoForaDoAlcance()
        {
            var seletor = new SeletorDeInteracao(1.5f);
            var buffer = new[] { C(10, 4f), C(20, 9f) };
            Assert.IsNull(seletor.Selecionar(buffer, 2), "Todos estão além do alcance.");
        }

        [Test]
        public void IgnoraAlvoIndisponivel()
        {
            // O mais perto está indisponível (ex.: baú já aberto) — vale o próximo.
            var seletor = new SeletorDeInteracao(5f);
            var buffer = new[] { C(10, 1f, disp: false), C(20, 3f) };
            Assert.AreEqual(20, seletor.Selecionar(buffer, 2));
        }

        [Test]
        public void PrioridadeVenceDistancia()
        {
            // Um item de história (prioridade alta) ganha de um cenário mais perto.
            var seletor = new SeletorDeInteracao(5f);
            var buffer = new[] { C(10, 1f, prio: 0), C(20, 4f, prio: 5) };
            Assert.AreEqual(20, seletor.Selecionar(buffer, 2));
        }

        [Test]
        public void EmpateTotal_ResolvePeloMenorId_Deterministico()
        {
            // Mesma prioridade e mesma distância: o resultado não pode depender da
            // ordem em que o Physics devolveu os colisores.
            var seletor = new SeletorDeInteracao(5f);
            var ordemA = new[] { C(77, 2f), C(31, 2f) };
            var ordemB = new[] { C(31, 2f), C(77, 2f) };

            Assert.AreEqual(31, seletor.Selecionar(ordemA, 2));
            Assert.AreEqual(31, seletor.Selecionar(ordemB, 2),
                "A escolha deve ser estável independente da ordem do buffer.");
        }

        [Test]
        public void RespeitaAQuantidadeInformada_IgnorandoLixoDoBuffer()
        {
            // O buffer é pré-alocado e reusado: posições além da contagem são lixo
            // do frame anterior e não podem influenciar a escolha.
            var seletor = new SeletorDeInteracao(5f);
            var buffer = new[] { C(10, 3f), C(99, 0.1f) };
            Assert.AreEqual(10, seletor.Selecionar(buffer, quantidade: 1));
        }

        [Test]
        public void QuantidadeMaiorQueOBuffer_NaoEstoura()
        {
            var seletor = new SeletorDeInteracao(5f);
            var buffer = new[] { C(10, 1f) };
            Assert.AreEqual(10, seletor.Selecionar(buffer, quantidade: 50));
        }

        [Test]
        public void ExatamenteNoAlcance_Vale()
        {
            var seletor = new SeletorDeInteracao(1.5f);
            var buffer = new[] { C(10, 1.5f) };
            Assert.AreEqual(10, seletor.Selecionar(buffer, 1));
        }

        [Test]
        public void AlcanceInvalido_CaiNoPadrao()
        {
            Assert.AreEqual(1.5f, new SeletorDeInteracao(0f).Alcance, 0.0001f);
            Assert.AreEqual(1.5f, new SeletorDeInteracao(-3f).Alcance, 0.0001f);
        }
    }
}
