using System;
using NUnit.Framework;
using FavelaAmarela.Core.Quests;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Testes do recital final da quest de Cassilda — as estrofes que a rainha cobra depois
    /// de receber os fragmentos. O que estes testes protegem é o desenho <b>sem punição</b>:
    /// errar não pode fazer o jogador perder terreno já conquistado.
    /// </summary>
    public sealed class RecitalDaCancaoTests
    {
        // Índices arbitrários de "opção correta" — o recital não conhece texto nenhum.
        private const int CertaDaTerceira = 1;
        private const int CertaDaQuarta = 2;

        private static RecitalDaCancao NovoRecital()
            => new RecitalDaCancao(CertaDaTerceira, CertaDaQuarta);

        [Test]
        public void RecitalVazio_NasceCompleto()
        {
            // É o que mantém a quest jogável antes das estrofes serem autoradas no Inspector.
            var r = new RecitalDaCancao();

            Assert.AreEqual(0, r.Total);
            Assert.IsTrue(r.Completo);
        }

        [Test]
        public void RecitalNovo_ComecaNaPrimeiraEstrofe()
        {
            var r = NovoRecital();

            Assert.AreEqual(2, r.Total);
            Assert.AreEqual(0, r.EstrofeAtual);
            Assert.AreEqual(0, r.Erros);
            Assert.IsFalse(r.Completo);
        }

        [Test]
        public void Responder_Certo_AvancaEAvisa()
        {
            var r = NovoRecital();
            int avisada = -1;
            r.OnAcerto += i => avisada = i;

            Assert.IsTrue(r.Responder(CertaDaTerceira));
            Assert.AreEqual(0, avisada, "o evento carrega a estrofe que fechou, não a próxima");
            Assert.AreEqual(1, r.EstrofeAtual);
            Assert.IsFalse(r.Completo, "ainda falta a quarta estrofe");
        }

        [Test]
        public void Responder_Errado_NaoAvancaEPermiteTentarDeNovo()
        {
            var r = NovoRecital();
            int erroNa = -1;
            r.OnErro += i => erroNa = i;

            Assert.IsFalse(r.Responder(CertaDaTerceira + 1));
            Assert.AreEqual(0, erroNa);
            Assert.AreEqual(0, r.EstrofeAtual, "errar mantém a mesma estrofe em aberto");
            Assert.AreEqual(1, r.Erros);

            // Retry livre: a mesma estrofe aceita a resposta certa logo depois.
            Assert.IsTrue(r.Responder(CertaDaTerceira));
            Assert.AreEqual(1, r.EstrofeAtual);
        }

        [Test]
        public void Errar_NaQuarta_NaoDevolveOAcertoDaTerceira()
        {
            // A regra que importa: nunca voltar ao começo. Perder um acerto já conquistado
            // seria punição, e o desenho é sem punição.
            var r = NovoRecital();
            r.Responder(CertaDaTerceira);

            Assert.IsFalse(r.Responder(CertaDaQuarta + 1));
            Assert.AreEqual(1, r.EstrofeAtual, "o acerto da terceira continua valendo");
        }

        [Test]
        public void Responder_TodasCertas_CompletaORecital()
        {
            var r = NovoRecital();

            Assert.IsTrue(r.Responder(CertaDaTerceira));
            Assert.IsTrue(r.Responder(CertaDaQuarta));
            Assert.IsTrue(r.Completo);
            Assert.AreEqual(0, r.Erros);
        }

        [Test]
        public void Responder_DepoisDeCompleto_NaoFazNada()
        {
            var r = NovoRecital();
            r.Responder(CertaDaTerceira);
            r.Responder(CertaDaQuarta);

            int acertosDepois = 0;
            int errosDepois = 0;
            r.OnAcerto += _ => acertosDepois++;
            r.OnErro += _ => errosDepois++;

            Assert.IsFalse(r.Responder(CertaDaQuarta));
            Assert.IsFalse(r.Responder(99));
            Assert.AreEqual(0, acertosDepois);
            Assert.AreEqual(0, errosDepois, "uma resposta fora de hora não pode contar como erro");
            Assert.AreEqual(0, r.Erros);
        }

        [Test]
        public void RespostaCertaDe_ForaDaFaixa_DevolveMenosUm()
        {
            var r = NovoRecital();

            Assert.AreEqual(CertaDaTerceira, r.RespostaCertaDe(0));
            Assert.AreEqual(CertaDaQuarta, r.RespostaCertaDe(1));
            Assert.AreEqual(-1, r.RespostaCertaDe(-1));
            Assert.AreEqual(-1, r.RespostaCertaDe(2));
        }

        [Test]
        public void Construtor_ComArrayNulo_Lanca()
        {
            Assert.Throws<ArgumentNullException>(() => new RecitalDaCancao(null));
        }
    }
}
