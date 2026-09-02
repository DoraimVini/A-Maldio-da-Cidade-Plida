using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using FavelaAmarela.Core.GameLoop;

namespace FavelaAmarela.Tests.PlayMode
{
    /// <summary>
    /// Guarda que a tela de Colapso <b>volta a ser invisível</b> depois de o jogador renascer.
    ///
    /// <para><b>O que motivou (2026-09-02).</b> O Vini mandou o print do console: "Está dando erro
    /// para renascer". A cena tinha carregado — a hierarquia mostrava <c>Deserto_Hali</c> —, mas a
    /// tela continuava preta. A causa não estava na navegação:</para>
    ///
    /// <code>
    /// SequenciaDeColapso.Awake()  ->  painelColapso.alpha = 0
    /// </code>
    ///
    /// <para>...era o <b>único</b> lugar que zerava o alpha, e este componente vive no
    /// <c>HUD_Gameplay</c>, que é <c>DontDestroyOnLoad</c>. <b>O <c>Awake</c> roda uma vez na vida
    /// do jogo, não uma por cena.</b> Depois da primeira morte o painel preto e opaco ficava
    /// aceso para sempre, e o jogo seguia rodando por baixo dele.</para>
    ///
    /// <para><b>Por que só um teste de PlayMode pega isto.</b> No YAML o painel está correto:
    /// <c>alpha 0</c>. O defeito só existe depois de a sequência de morte ter tocado — é estado de
    /// runtime que sobrevive à troca de cena. Nenhuma leitura de disco o enxerga, e foi por isso
    /// que ele atravessou a sessão inteira.</para>
    /// </summary>
    public sealed class TelaDeColapsoVoltaAoRepousoTests
    {
        private GameObject _hud;

        [TearDown]
        public void TearDown()
        {
            if (_hud != null) Object.DestroyImmediate(_hud);
        }

        [UnityTest]
        public IEnumerator RenascerApagaOPainelDeColapso()
        {
            var painel = MontarEChegarNoColapso();
            yield return painel;

            var grupo = _grupo;

            // REGRA DURA: se a sequência não acendeu o painel, o teste não mediu nada. Passar
            // verde aqui seria o mesmo silêncio que deixou o defeito atravessar a sessão.
            Assert.AreEqual(1f, grupo.alpha, 0.01f,
                "A sequência de Colapso não acendeu o painel — este teste não conseguiu MEDIR " +
                "nada, então não pode afirmar que o renascimento o apaga.");
            Assert.IsTrue(grupo.gameObject.activeInHierarchy,
                "O painel de Colapso deveria estar ligado depois da sequência de morte.");

            // O renascimento: a cena nova nasce, o bootstrap dela liga o HUD persistente numa
            // máquina de estados nova. É o único gancho por cena que a tela de Colapso tem.
            _retorno.Bind(new GameLoopStateMachine());
            yield return null;

            Assert.AreEqual(0f, grupo.alpha, 0.01f,
                "Renasci e o painel de Colapso continua ACESO (alpha " + grupo.alpha + "). " +
                "O jogo roda por baixo de um retângulo preto e opaco — é o 'erro para renascer'.");
            Assert.IsFalse(grupo.gameObject.activeInHierarchy,
                "O painel de Colapso continua ligado depois de renascer.");
        }

        [UnityTest]
        public IEnumerator ASegundaMorteTambemTocaASequencia()
        {
            var espera = MontarEChegarNoColapso();
            yield return espera;

            _retorno.Bind(new GameLoopStateMachine());
            yield return null;

            // REGRA DURA, e ela e o motivo desta linha existir: sem o conserto o painel continua
            // aceso aqui, e a asercao final ("acendeu de novo") passaria TRIVIALMENTE, verde pelo
            // motivo errado. Medido em 2026-09-02: sem esta pre-condicao, este teste passava com
            // o defeito presente.
            Assert.AreEqual(0f, _grupo.alpha, 0.01f,
                "O painel nao apagou ao renascer, entao este teste nem chega a MEDIR se a " +
                "segunda morte toca a sequencia.");

            // `_tocado` era uma trava sem destravamento. Com o HUD persistente, isso significava
            // que a SEGUNDA morte da partida não tocaria sequência nenhuma — defeito que ficava
            // escondido atrás do primeiro, porque a tela já estava preta de qualquer jeito.
            _sequencia.Tocar();
            yield return new WaitForSecondsRealtime(1.4f);

            Assert.AreEqual(1f, _grupo.alpha, 0.01f,
                "A segunda morte não tocou a sequência de Colapso: o painel ficou em alpha " +
                _grupo.alpha + ". A trave de idempotência nunca é destravada.");
        }

        // --- montagem ------------------------------------------------------------------

        private CanvasGroup _grupo;
        private FavelaAmarela.Runtime.UI.RetornoDoColapso _retorno;
        private FavelaAmarela.Runtime.GameLoop.SequenciaDeColapso _sequencia;

        /// <summary>
        /// Sobe o HUD de verdade, toca a sequência de morte e espera o fade terminar.
        /// </summary>
        private IEnumerator MontarEChegarNoColapso()
        {
            // GarantirInstancia, e não Instantiate: o Awake do HUDController destrói duplicatas.
            FavelaAmarela.Runtime.UI.HUDController.GarantirInstancia();
            yield return null;

            var controlador = Object.FindAnyObjectByType<FavelaAmarela.Runtime.UI.HUDController>(
                FindObjectsInactive.Include);

            Assert.IsNotNull(controlador, "O HUD_Gameplay não subiu — nada a medir.");
            _hud = controlador.gameObject;

            _sequencia = _hud.GetComponentInChildren<
                FavelaAmarela.Runtime.GameLoop.SequenciaDeColapso>(true);
            _retorno = _hud.GetComponentInChildren<
                FavelaAmarela.Runtime.UI.RetornoDoColapso>(true);

            Assert.IsNotNull(_sequencia, "Sem SequenciaDeColapso no HUD.");
            Assert.IsNotNull(_retorno, "Sem RetornoDoColapso no HUD.");

            // O CanvasGroup do painel escuro, achado pela hierarquia — e não pelo campo privado.
            // O que o jogador vê é o objeto, não o campo serializado.
            _grupo = _sequencia.GetComponentsInChildren<CanvasGroup>(true)
                               .FirstOrDefault(g => g.name == "Painel");

            Assert.IsNotNull(_grupo,
                "Não achei o CanvasGroup de Tela_Colapso/Painel — é ele que a sequência acende.");

            _sequencia.Tocar();

            // Tempo NÃO-escalado: a sequência roda com timeScale zerado de propósito.
            yield return new WaitForSecondsRealtime(1.4f);
        }
    }
}
