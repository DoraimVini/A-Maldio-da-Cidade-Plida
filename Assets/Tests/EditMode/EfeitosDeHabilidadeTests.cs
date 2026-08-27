using NUnit.Framework;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Abilities.Efeitos;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda os <b>efeitos</b> — a peça que faz arma nova custar um asset em vez de uma classe.
    ///
    /// <para>O ganho de teste é o argumento central de <c>habilidades_de_item.md</c>: um efeito
    /// testado <b>uma vez</b> vale para toda arma futura que o use, em vez de a mesma asserção
    /// ser recontada em cada classe de arma.</para>
    ///
    /// <para>POCO puro: nada aqui precisa de cena, prefab ou Unity rodando.</para>
    /// </summary>
    public sealed class EfeitosDeHabilidadeTests
    {
        private static ConstrutorDeGolpe Golpe() => new ConstrutorDeGolpe(0.3f, 0.4f);

        [Test]
        public void Dano_Soma()
        {
            var g = Golpe();
            new EfeitoDeDano(30f).Aplicar(g);
            new EfeitoDeDano(12f).Aplicar(g);

            Assert.AreEqual(42f, g.Construir().Dano, 0.001f,
                "Dois efeitos de dano no mesmo golpe somam — é a leitura natural de 'bate mais'.");
        }

        [Test]
        public void DanoNegativo_ViraZero()
        {
            var g = Golpe();
            new EfeitoDeDano(-50f).Aplicar(g);

            Assert.AreEqual(0f, g.Construir().Dano, 0.001f,
                "Dano negativo curaria o inimigo. Um Item Creator vai produzir esse valor por " +
                "acidente mais cedo ou mais tarde.");
        }

        /// <summary>
        /// Somar atordoamentos travaria o alvo pelo dobro do tempo — nunca é a intenção de quem
        /// autora, e é exatamente o tipo de composição que uma ferramenta de criação de itens
        /// produz sem querer.
        /// </summary>
        [Test]
        public void Atordoamento_FicaComOMaiorEmVezDeSomar()
        {
            var g = Golpe();
            new EfeitoDeAtordoamento(1.5f).Aplicar(g);
            new EfeitoDeAtordoamento(2f).Aplicar(g);
            new EfeitoDeAtordoamento(0.5f).Aplicar(g);

            var r = g.Construir();
            Assert.IsTrue(r.Atordoou);
            Assert.AreEqual(2f, r.DuracaoAtordoamento, 0.001f,
                "Três atordoamentos não podem virar 4 segundos de trava.");
        }

        [Test]
        public void AtordoamentoZerado_NaoAtordoa()
        {
            var g = Golpe();
            new EfeitoDeAtordoamento(0f).Aplicar(g);

            Assert.IsFalse(g.Construir().Atordoou,
                "Atordoamento de duração zero marcaria o golpe como atordoante sem travar " +
                "nada — a FSM entraria e sairia do estado no mesmo quadro.");
        }

        [Test]
        public void Repulsao_FicaComAMaior()
        {
            var g = Golpe();
            new EfeitoDeRepulsao(6f).Aplicar(g);
            new EfeitoDeRepulsao(2f).Aplicar(g);

            Assert.AreEqual(6f, g.Construir().ForcaRepulsao, 0.001f);
        }

        /// <summary>
        /// Acúmulos somam (mais feridas), mas intensidade e duração ficam na maior — é o que a
        /// mecânica de estouro por acúmulo espera.
        /// </summary>
        [Test]
        public void Sangramento_AcumulaMasNaoEmpilhaIntensidade()
        {
            var g = Golpe();
            new EfeitoDeSangramento(1, 4f, 5f).Aplicar(g);
            new EfeitoDeSangramento(3, 6f, 3f).Aplicar(g);

            var r = g.Construir();
            Assert.AreEqual(4, r.AcumulosDeSangramento, "Acúmulos somam.");
            Assert.AreEqual(6f, r.SangramentoPorSegundo, 0.001f, "Intensidade fica na maior.");
            Assert.AreEqual(5f, r.DuracaoSangramento, 0.001f, "Duração fica na maior.");
        }

        [Test]
        public void SangramentoSemAcumulo_NaoFazNada()
        {
            var g = Golpe();
            new EfeitoDeSangramento(0, 99f, 99f).Aplicar(g);

            var r = g.Construir();
            Assert.AreEqual(0, r.AcumulosDeSangramento);
            Assert.AreEqual(0f, r.SangramentoPorSegundo, 0.001f,
                "Sem acúmulo não há ferida, então a intensidade não pode vazar para o golpe.");
        }

        [Test]
        public void Interrupcao_CortaConjuracao()
        {
            var g = Golpe();
            new EfeitoDeInterrupcao().Aplicar(g);

            Assert.IsTrue(g.Construir().InterrompeConjuracao,
                "É o que faz o Cravo de Aklo ser a arma anti-mago do arsenal.");
        }

        [Test]
        public void GolpeSemEfeitoNenhum_SaiInofensivoMasValido()
        {
            var r = Golpe().Construir();

            Assert.IsTrue(r.Success, "O golpe acontece — só não carrega nada.");
            Assert.AreEqual(0f, r.Dano, 0.001f);
            Assert.AreEqual(0.3f, r.DurationSeconds, 0.001f,
                "A duração vem do golpe, não de efeito — senão a FSM não teria por quanto " +
                "tempo prender o ator.");
        }

        // ── A arma montada por efeitos ────────────────────────────────────────

        /// <summary>
        /// O Alfanje de Alhazred como o <c>habilidades_de_item.md</c> previu: 60 linhas de
        /// classe C# viram três efeitos numa lista.
        /// </summary>
        [Test]
        public void UmaArmaMontadaPorEfeitos_SeComportaComoAClasseQueEleSubstitui()
        {
            var arma = new HabilidadeComposta(
                "Alfanje de Alhazred", "Golpe do Deserto",
                efeitosDoBasico: new IEfeitoDeHabilidade[] { new EfeitoDeDano(45f) },
                efeitosDaHabilidade: new IEfeitoDeHabilidade[]
                {
                    new EfeitoDeDano(40f),
                    new EfeitoDeAtordoamento(2f),
                    new EfeitoDeRepulsao(6f),
                },
                duracaoBasico: 0.45f, cooldownBasico: 0.7f,
                duracaoHabilidade: 0.5f, cooldownHabilidade: 5f);

            var basico = arma.Execute();
            Assert.AreEqual(45f, basico.Dano, 0.001f);
            Assert.IsFalse(basico.Atordoou, "O básico do Alfanje não atordoa — só a habilidade.");

            var habilidade = arma.ExecuteHabilidade();
            Assert.AreEqual(40f, habilidade.Dano, 0.001f);
            Assert.IsTrue(habilidade.Atordoou);
            Assert.AreEqual(2f, habilidade.DuracaoAtordoamento, 0.001f);
            Assert.AreEqual(6f, habilidade.ForcaRepulsao, 0.001f);
        }

        /// <summary>
        /// A cadência tem de continuar sendo respeitada pela arma a dado — foi por não ser
        /// perguntada que o <c>cooldownBasico</c> virou dado morto em todas as armas até
        /// 2026-08-27.
        /// </summary>
        [Test]
        public void ArmaADado_RespeitaACadencia()
        {
            var arma = new HabilidadeComposta(
                "teste", "teste",
                new IEfeitoDeHabilidade[0], new IEfeitoDeHabilidade[0],
                duracaoBasico: 0.3f, cooldownBasico: 0.7f,
                duracaoHabilidade: 0.4f, cooldownHabilidade: 5f);

            Assert.IsFalse(arma.CanActivate(0.69f), "Não pode liberar antes do cooldown.");
            Assert.IsTrue(arma.CanActivate(0.7f), "Libera exatamente no cooldown.");

            Assert.IsFalse(arma.CanActivateHabilidade(0.7f),
                "A habilidade tem cooldown próprio e não pode liberar com o do básico.");
            Assert.IsTrue(arma.CanActivateHabilidade(5f));
        }

        [Test]
        public void ArmaSemNome_NaoMostraStringVaziaAoJogador()
        {
            var arma = new HabilidadeComposta(
                null, "  ",
                new IEfeitoDeHabilidade[0], new IEfeitoDeHabilidade[0],
                0.3f, 0.4f, 0.4f, 5f);

            Assert.IsNotEmpty(arma.NomeDaArma, "A barra de ações desenharia um rótulo vazio.");
            Assert.IsNotEmpty(arma.NomeHabilidade);
        }

        [Test]
        public void ListaDeEfeitosNula_NaoEstoura()
        {
            var arma = new HabilidadeComposta("a", "b", null, null, 0.3f, 0.4f, 0.4f, 5f);

            Assert.DoesNotThrow(() => arma.Execute(),
                "Um HabilidadeDef recém-criado no Inspector tem listas vazias ou nulas — " +
                "equipar essa arma não pode derrubar a partida.");
        }
    }
}
