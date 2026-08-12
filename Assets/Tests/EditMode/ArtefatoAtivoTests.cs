using System.Collections.Generic;
using NUnit.Framework;
using FavelaAmarela.Core.Artefatos;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Suite EditMode da habilidade ativa dos Artefatos: cooldown, custo de Resiliência
    /// Mental e aplicação dos efeitos. O contexto é falso — é justamente para isso que o
    /// Core declara <see cref="IContextoDeArtefato"/> em vez de tocar o mundo direto.
    /// </summary>
    public class ArtefatoAtivoTests
    {
        /// <summary>Contexto de teste: anota o que foi pedido, sem Unity nem cena.</summary>
        private sealed class ContextoFake : IContextoDeArtefato
        {
            public readonly List<string> Chamadas = new List<string>();
            public float UltimoRaio;
            public float UltimaDuracao;
            public float UltimoValor;

            public void RevelarEntidades(float raio, float duracao)
            {
                Chamadas.Add("revelar");
                UltimoRaio = raio;
                UltimaDuracao = duracao;
            }

            public void AncorarJogador(float valor)
            {
                Chamadas.Add("ancorar");
                UltimoValor = valor;
            }

            public void SilenciarPassos(float duracao)
            {
                Chamadas.Add("silenciar");
                UltimaDuracao = duracao;
            }

            public void AplacarSerpentes(float raio, float duracao)
            {
                Chamadas.Add("aplacar");
                UltimoRaio = raio;
                UltimaDuracao = duracao;
            }
        }

        private static ArtefatoAtivo Ativo(float custoRM = 10f, float cooldown = 20f,
            params IEfeitoDeArtefato[] efeitos)
            => new ArtefatoAtivo("Recitar o Aklo", custoRM, cooldown, 5f, efeitos);

        [Test]
        public void RMInsuficiente_NaoAtiva()
        {
            var ativo = Ativo(custoRM: 10f);

            // 10 de RM contra custo 10: o gasto colapsaria Damião pela própria habilidade.
            Assert.IsFalse(ativo.PodeAtivar(rmAtual: 10f, tempoDesdeUltimoUso: 999f));
            Assert.IsFalse(ativo.PodeAtivar(rmAtual: 4f, tempoDesdeUltimoUso: 999f));
        }

        [Test]
        public void RMSuficiente_Ativa()
        {
            var ativo = Ativo(custoRM: 10f);

            Assert.IsTrue(ativo.PodeAtivar(rmAtual: 10.5f, tempoDesdeUltimoUso: 999f));
        }

        [Test]
        public void SemCusto_AtivaMesmoComRMZerada()
        {
            var ativo = Ativo(custoRM: 0f);

            Assert.IsTrue(ativo.PodeAtivar(rmAtual: 0f, tempoDesdeUltimoUso: 999f));
        }

        [Test]
        public void EmRecarga_NaoAtiva()
        {
            var ativo = Ativo(custoRM: 0f, cooldown: 20f);

            Assert.IsFalse(ativo.PodeAtivar(rmAtual: 100f, tempoDesdeUltimoUso: 19.9f));
        }

        [Test]
        public void RecargaCumprida_Ativa()
        {
            var ativo = Ativo(custoRM: 0f, cooldown: 20f);

            Assert.IsTrue(ativo.PodeAtivar(rmAtual: 100f, tempoDesdeUltimoUso: 20f));
        }

        [Test]
        public void Ativar_AplicaOsEfeitosNaOrdem()
        {
            var ctx = new ContextoFake();
            var ativo = Ativo(0f, 0f,
                new EfeitoDeRevelacao(8f, 5f),
                new EfeitoDeAncoragem(12f));

            ativo.Ativar(ctx);

            CollectionAssert.AreEqual(new[] { "revelar", "ancorar" }, ctx.Chamadas);
        }

        [Test]
        public void Ativar_DevolveOCustoEOCooldownParaOAdaptador()
        {
            var ativo = Ativo(custoRM: 15f, cooldown: 30f);

            var resultado = ativo.Ativar(new ContextoFake());

            Assert.IsTrue(resultado.Sucesso);
            Assert.AreEqual(15f, resultado.CustoRM, 0.0001f);
            Assert.AreEqual(30f, resultado.Cooldown, 0.0001f);
            Assert.AreEqual(5f, resultado.Duracao, 0.0001f);
        }

        [Test]
        public void EfeitoDeRevelacao_RepassaRaioEDuracao()
        {
            var ctx = new ContextoFake();

            new EfeitoDeRevelacao(12f, 6f).Aplicar(ctx);

            Assert.AreEqual(12f, ctx.UltimoRaio, 0.0001f);
            Assert.AreEqual(6f, ctx.UltimaDuracao, 0.0001f);
        }

        [Test]
        public void EfeitoDeAncoragem_RepassaOValor()
        {
            var ctx = new ContextoFake();

            new EfeitoDeAncoragem(25f).Aplicar(ctx);

            Assert.AreEqual(25f, ctx.UltimoValor, 0.0001f);
        }

        [Test]
        public void EfeitoDeAplacamento_RepassaRaioEDuracao()
        {
            var ctx = new ContextoFake();

            new EfeitoDeAplacamento(7f, 3f).Aplicar(ctx);

            Assert.AreEqual("aplacar", ctx.Chamadas[0]);
            Assert.AreEqual(7f, ctx.UltimoRaio, 0.0001f);
        }

        [Test]
        public void ValoresNegativos_SaoSaneados()
        {
            var ativo = new ArtefatoAtivo("x", -5f, -1f, -2f, null);

            Assert.AreEqual(0f, ativo.CustoRM, 0.0001f);
            Assert.AreEqual(0f, ativo.Cooldown, 0.0001f);
            Assert.AreEqual(0f, ativo.Duracao, 0.0001f);
        }

        [Test]
        public void SemEfeitos_AtivaSemEstourar()
        {
            var ativo = new ArtefatoAtivo("x", 0f, 0f, 0f, null);

            Assert.IsTrue(ativo.Ativar(new ContextoFake()).Sucesso);
        }
    }
}
