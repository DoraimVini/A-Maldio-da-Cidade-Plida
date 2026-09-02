using System.Collections.Generic;
using NUnit.Framework;
using FavelaAmarela.Core.Entrada;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// O árbitro de foco de entrada — POCO puro, sem cena e sem <c>MonoBehaviour</c>.
    ///
    /// <para>Cada teste aqui corresponde a um sintoma <b>medido</b> na auditoria de 2026-09-02,
    /// e não a uma propriedade inventada da estrutura.</para>
    /// </summary>
    public sealed class FocoDeEntradaTests
    {
        [Test]
        public void SemNinguemPedindo_OJogoComanda()
        {
            var foco = new FocoDeEntrada();

            Assert.IsTrue(foco.JogoNoComando);
            Assert.AreEqual(CamadaDeEntrada.Jogo, foco.Atual);
            Assert.Zero(foco.Profundidade);
        }

        /// <summary>
        /// O sintoma: com a mochila aberta, F1–F4 continuavam disparando Artefatos, 1–8
        /// continuavam consumindo itens e o clique continuava golpeando — porque
        /// <c>Time.timeScale = 0</c> não engole tecla nenhuma.
        /// </summary>
        [Test]
        public void ComPainelAberto_OJogoNaoComanda()
        {
            var foco = new FocoDeEntrada();

            foco.Tomar(CamadaDeEntrada.PainelModal);

            Assert.IsFalse(foco.JogoNoComando);
            Assert.AreEqual(CamadaDeEntrada.PainelModal, foco.Atual);
        }

        /// <summary>
        /// O console abre <b>por cima</b> do inventário, e fechá-lo devolve o comando ao
        /// inventário — não ao jogo. É por isso que a estrutura é pilha e não um valor só.
        /// </summary>
        [Test]
        public void ConsoleSobrePainel_DevolveAoPainel()
        {
            var foco = new FocoDeEntrada();

            foco.Tomar(CamadaDeEntrada.PainelModal);
            foco.Tomar(CamadaDeEntrada.Console);

            Assert.AreEqual(CamadaDeEntrada.Console, foco.Atual);

            foco.Devolver(CamadaDeEntrada.Console);

            Assert.AreEqual(CamadaDeEntrada.PainelModal, foco.Atual,
                "Fechar o console devolveu o comando ao JOGO com o inventário ainda aberto.");
        }

        /// <summary>
        /// Um painel pode fechar por dois caminhos (Esc e o botão). O segundo não pode derrubar
        /// a camada de outro.
        /// </summary>
        [Test]
        public void DevolverDuasVezes_NaoDerrubaCamadaAlheia()
        {
            var foco = new FocoDeEntrada();

            foco.Tomar(CamadaDeEntrada.Console);
            foco.Tomar(CamadaDeEntrada.PainelModal);

            Assert.IsTrue(foco.Devolver(CamadaDeEntrada.PainelModal));
            Assert.IsFalse(foco.Devolver(CamadaDeEntrada.PainelModal),
                "Devolver o que já foi devolvido precisa ser inócuo.");

            Assert.AreEqual(CamadaDeEntrada.Console, foco.Atual,
                "A segunda devolução derrubou a camada do console.");
        }

        /// <summary>
        /// Devolver uma camada que não está no topo remove a ocorrência mais alta DELA, e deixa
        /// o resto da pilha intacto.
        /// </summary>
        [Test]
        public void DevolverForaDeOrdem_NaoCorrompeAPilha()
        {
            var foco = new FocoDeEntrada();

            foco.Tomar(CamadaDeEntrada.PainelModal);
            foco.Tomar(CamadaDeEntrada.Console);

            foco.Devolver(CamadaDeEntrada.PainelModal);

            Assert.AreEqual(CamadaDeEntrada.Console, foco.Atual);
            Assert.AreEqual(1, foco.Profundidade);
        }

        /// <summary>
        /// <c>Jogo</c> é o piso, não uma camada que se toma. Sem esta guarda um chamador
        /// distraído devolveria o controle ao jogo por cima de um painel aberto.
        /// </summary>
        [Test]
        public void TomarOJogo_NaoFazNada()
        {
            var foco = new FocoDeEntrada();

            foco.Tomar(CamadaDeEntrada.PainelModal);
            foco.Tomar(CamadaDeEntrada.Jogo);

            Assert.AreEqual(CamadaDeEntrada.PainelModal, foco.Atual);
            Assert.AreEqual(1, foco.Profundidade);
        }

        /// <summary>
        /// Troca de cena: um painel destruído no meio do caminho deixaria a pilha suja e o
        /// jogador sem controle, sem nada explicando.
        /// </summary>
        [Test]
        public void Limpar_DevolveTudoAoJogo()
        {
            var foco = new FocoDeEntrada();

            foco.Tomar(CamadaDeEntrada.PainelModal);
            foco.Tomar(CamadaDeEntrada.Console);

            foco.Limpar();

            Assert.IsTrue(foco.JogoNoComando);
            Assert.Zero(foco.Profundidade);
        }

        [Test]
        public void OEventoDispara_SoQuandoODonoMuda()
        {
            var foco = new FocoDeEntrada();
            var vistos = new List<CamadaDeEntrada>();

            foco.OnMudou += c => vistos.Add(c);

            foco.Tomar(CamadaDeEntrada.PainelModal);   // Jogo -> PainelModal
            foco.Tomar(CamadaDeEntrada.PainelModal);   // PainelModal -> PainelModal: não avisa
            foco.Tomar(CamadaDeEntrada.Console);       // -> Console
            foco.Devolver(CamadaDeEntrada.Console);    // -> PainelModal

            CollectionAssert.AreEqual(
                new[]
                {
                    CamadaDeEntrada.PainelModal,
                    CamadaDeEntrada.Console,
                    CamadaDeEntrada.PainelModal,
                },
                vistos,
                "Avisar sem mudança faria a UI reagir a nada — e quatro avisos para três " +
                "mudanças é ruído que ensina a ignorar o evento.");
        }
    }
}
