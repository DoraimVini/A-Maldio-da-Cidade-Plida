using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using FavelaAmarela.Core.Dialogo;
using FavelaAmarela.Core.Entrada;
using FavelaAmarela.Core.GameLoop;
using FavelaAmarela.Runtime.Entrada;

namespace FavelaAmarela.Tests.PlayMode
{
    /// <summary>
    /// Guarda que o jogador <b>recupera o comando</b> depois de cada painel modal, e que o menu
    /// pós-morte aparece de verdade.
    ///
    /// <para><b>O que motivou (2026-09-02).</b> O Vini, na luta do Abdul: "o boneco não pode mais
    /// andar e morre parado". E, no console:</para>
    ///
    /// <code>
    /// [ArbitroDeFoco] 1 camada(s) de entrada ainda presas ao trocar para 'Cena_Menu'
    /// </code>
    ///
    /// <para><b>A causa era minha.</b> <c>PainelDeEscolha.Confirmar()</c> repete o corpo do
    /// <c>Esconder()</c> dentro de si, de propósito, para soltar os bloqueios <i>depois</i> do
    /// callback. Quando acrescentei o árbitro de foco, em agosto, pus o <c>Devolver</c> só no
    /// <c>Esconder()</c> — e escolher uma opção, que é o caminho <b>normal</b> deste painel,
    /// nunca passa por lá. Cada escolha confirmada prendia uma camada de <c>PainelModal</c>,
    /// para sempre. Com o jogo preso nela, <c>PlayerMovement</c>, <c>DetectorDeInteracao</c> e
    /// <c>BarraDeItens</c> desligam todos de uma vez.</para>
    ///
    /// <para><b>Por que nenhum teste pegou.</b> Os oito testes do <c>FocoDeEntrada</c> exercitam
    /// o POCO direto — e o POCO está certo. O defeito é <b>quem chama</b>: um caminho de saída
    /// que esquece de devolver. Isso só aparece exercitando o painel de verdade.</para>
    /// </summary>
    public sealed class OComandoVoltaAoJogadorTests
    {
        private GameObject _hud;

        [SetUp]
        public void SetUp() => ArbitroDeFoco.Foco.Limpar();

        [TearDown]
        public void TearDown()
        {
            ArbitroDeFoco.Foco.Limpar();
            if (_hud != null) Object.DestroyImmediate(_hud);
        }

        private IEnumerator MontarOHud()
        {
            FavelaAmarela.Runtime.UI.HUDController.GarantirInstancia();
            yield return null;

            var c = Object.FindAnyObjectByType<FavelaAmarela.Runtime.UI.HUDController>(
                FindObjectsInactive.Include);
            Assert.IsNotNull(c, "O HUD_Gameplay não subiu — nada a medir.");
            _hud = c.gameObject;
        }

        [UnityTest]
        public IEnumerator EscolherUmaOpcaoDevolveOComandoAoJogador()
        {
            yield return MontarOHud();

            var painel = _hud.GetComponentInChildren<FavelaAmarela.Runtime.UI.PainelDeEscolha>(true);
            Assert.IsNotNull(painel, "Sem PainelDeEscolha no HUD.");

            Assert.IsTrue(ArbitroDeFoco.JogoNoComando,
                "O jogo já começou sem o comando — o teste não tem de onde partir.");

            // O rig nao tem PlayerInput ligado, e o painel avisa disso -- com razao. O aviso
            // e do ambiente de teste, nao do defeito sob medicao.
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
                @"\[PainelDeEscolha\] Abrindo escolha SEM PlayerInput"));

            int escolhido = -1;
            painel.Mostrar(
                new[] { new OpcaoDeDialogo("Lutar", 1), new OpcaoDeDialogo("Concordar", 2) },
                id => escolhido = id);
            yield return null;

            // REGRA DURA: se abrir o painel não tirou o comando do jogo, este teste não mede o
            // que diz medir — ele passaria verde com o árbitro inteiro desligado.
            Assert.IsFalse(ArbitroDeFoco.JogoNoComando,
                "Abrir a escolha não tomou o comando. Sem isso, este teste não prova nada " +
                "sobre devolvê-lo.");

            // O caminho NORMAL: o jogador confirma. É privado porque só o aperto de E o dispara,
            // e é exatamente o caminho que esquecia de devolver a camada.
            typeof(FavelaAmarela.Runtime.UI.PainelDeEscolha)
                .GetMethod("Confirmar", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(painel, null);
            yield return null;

            Assert.AreNotEqual(-1, escolhido, "O callback da escolha não foi chamado.");

            Assert.IsTrue(ArbitroDeFoco.JogoNoComando,
                "Escolhi uma opção e o jogo NÃO recuperou o comando (profundidade " +
                $"{ArbitroDeFoco.Foco.Profundidade}). O Damião não anda, não interage e não usa " +
                "item — ele fica parado até morrer, que foi o relato do Vini na luta do Abdul.");

            Assert.AreEqual(0, ArbitroDeFoco.Foco.Profundidade,
                "Sobrou camada presa na pilha depois de fechar o painel.");
        }

        [UnityTest]
        public IEnumerator AbrirDuasVezesNaoPrendeDuasCamadas()
        {
            yield return MontarOHud();

            var painel = _hud.GetComponentInChildren<FavelaAmarela.Runtime.UI.PainelDeEscolha>(true);
            var opcoes = new[] { new OpcaoDeDialogo("A", 1), new OpcaoDeDialogo("B", 2) };

            for (int i = 0; i < 2; i++)
                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
                    @"\[PainelDeEscolha\] Abrindo escolha SEM PlayerInput"));

            painel.Mostrar(opcoes, _ => { });
            painel.Mostrar(opcoes, _ => { });          // sem fechar entre as duas
            yield return null;

            Assert.AreEqual(1, ArbitroDeFoco.Foco.Profundidade,
                "Reabrir o painel por cima de si mesmo prendeu uma camada a mais — e só um " +
                "Devolver vai acontecer depois.");
        }

        /// <summary>
        /// O menu pós-morte aparece, e os botões dele podem ser clicados.
        ///
        /// <para><b>O Vini (2026-09-02):</b> "o menu pós-morte também não está funcionando".
        /// Aparecer não basta: um botão desenhado atrás de um painel que não recebe raycast,
        /// ou desativado, é a mesma coisa que botão nenhum para quem está jogando.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator OMenuPosMorteApareceEEClicavel()
        {
            yield return MontarOHud();

            var retorno = _hud.GetComponentInChildren<FavelaAmarela.Runtime.UI.RetornoDoColapso>(true);
            Assert.IsNotNull(retorno, "Sem RetornoDoColapso no HUD.");

            var maquina = new GameLoopStateMachine();
            retorno.Bind(maquina);

            Assert.IsTrue(maquina.TryTransition(GameState.Gameplay), "Menu -> Gameplay recusada.");
            Assert.IsTrue(maquina.TryTransition(GameState.Colapso), "Gameplay -> Colapso recusada.");

            var sequencia = _hud.GetComponentInChildren<
                FavelaAmarela.Runtime.GameLoop.SequenciaDeColapso>(true);
            Assert.IsNotNull(sequencia, "Sem SequenciaDeColapso no HUD.");
            sequencia.Tocar();

            // O atraso antes de oferecer é autorado (3 s por padrão) e corre em tempo real.
            yield return new WaitForSecondsRealtime(5f);

            var botoes = _hud.GetComponentsInChildren<Button>(true)
                             .Where(b => Caminho(b.transform).Contains("Tela_Colapso"))
                             .ToArray();

            Assert.IsNotEmpty(botoes, "A tela de Colapso não tem Button nenhum.");

            var mortos = botoes
                .Where(b => !b.gameObject.activeInHierarchy || !b.interactable ||
                            !b.GetComponent<Graphic>().raycastTarget)
                .Select(b => $"  {Caminho(b.transform)}: " +
                             $"{(b.gameObject.activeInHierarchy ? "" : "DESLIGADO ")}" +
                             $"{(b.interactable ? "" : "nao-interagivel ")}" +
                             $"{(b.GetComponent<Graphic>().raycastTarget ? "" : "sem raycast")}")
                .ToArray();

            Assert.IsEmpty(mortos,
                "Botao(oes) do menu pos-morte que o jogador nao consegue clicar:" +
                System.Environment.NewLine + string.Join(System.Environment.NewLine, mortos));
        }

        /// <summary>
        /// A fala pode ser dispensada antes da duração pedida.
        ///
        /// <para><b>O Vini (2026-09-02), na luta do Abdul:</b> "a janela de diálogo demora a
        /// sumir". A luta começa antes de a duração autorada acabar, e não havia como tirar a
        /// caixa da tela: o único caminho para o alpha zerar era a corrotina terminar sozinha.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator AFalaPodeSerDispensadaAntesDaHora()
        {
            yield return MontarOHud();

            var caixa = _hud.GetComponentInChildren<FavelaAmarela.Runtime.UI.TutorialHintUI>(true);
            Assert.IsNotNull(caixa, "Sem TutorialHintUI no HUD.");

            var grupo = caixa.GetComponentsInChildren<CanvasGroup>(true)
                             .FirstOrDefault(g => g.transform == caixa.transform)
                        ?? caixa.GetComponentInChildren<CanvasGroup>(true);
            Assert.IsNotNull(grupo, "A caixa de fala não tem CanvasGroup — nada a medir.");

            // Duração longa de proposito: sem o Esconder, nada tiraria isto da tela por 30 s.
            caixa.Mostrar("O grimório respira.", 30f, 0.1f);
            yield return new WaitForSecondsRealtime(0.4f);

            // REGRA DURA: se a fala nao apareceu, medir o desaparecimento nao afirma nada.
            Assert.Greater(grupo.alpha, 0.5f,
                $"A fala nao chegou a aparecer (alpha {grupo.alpha}) — este teste nao mediu nada.");

            caixa.Esconder(0.1f);
            yield return new WaitForSecondsRealtime(0.4f);

            Assert.Less(grupo.alpha, 0.02f,
                $"A fala continua na tela (alpha {grupo.alpha}) depois de dispensada. Na luta " +
                "do Abdul isso deixa a caixa aberta com o chefe ja conjurando por baixo.");
        }

        private static string Caminho(Transform t)
        {
            var partes = new System.Collections.Generic.List<string>();
            for (var a = t; a != null; a = a.parent) partes.Add(a.name);
            partes.Reverse();
            return string.Join("/", partes);
        }
    }
}
