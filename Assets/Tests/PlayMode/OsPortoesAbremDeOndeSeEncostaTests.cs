using System.Collections;
using System.Reflection;
using FavelaAmarela.Runtime.GameLoop;
using FavelaAmarela.Runtime.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FavelaAmarela.Tests.PlayMode
{
    /// <summary>
    /// Os Portões abrem de onde o jogador <b>consegue encostar</b>, seja qual for a altura da
    /// barreira.
    ///
    /// <para><b>O defeito (2026-09-10).</b> O Vini alteou a barreira dos Portões (1 → 4,3 un) para
    /// o Damião não entrar por baixo da arte, e os Portões deixaram de abrir. A
    /// <c>PosicaoDeInteracao</c> era a origem do objeto, na linha dos pilares (y ≈ 13); com a
    /// barreira descendo até y ≈ 9,6, o jogador parava a 3,4 un dela, e o
    /// <c>SeletorDeInteracao</c> descarta tudo acima do alcance (1,5). Sem prompt, sem abrir,
    /// sem Castelo.</para>
    ///
    /// <para>Na cena real, com o detector real do Damião: o que se guarda é que o Damião
    /// encostado na barreira <b>vê o prompt</b> — não uma conta de distância isolada.</para>
    /// </summary>
    public sealed class OsPortoesAbremDeOndeSeEncostaTests
    {
        private const string Cena = "Portoes_Das_Ruinas";

        [UnityTearDown]
        public IEnumerator Desmontar()
        {
            var portoes = SceneManager.GetSceneByName(Cena);
            if (!portoes.IsValid() || !portoes.isLoaded) yield break;

            var vazia = SceneManager.CreateScene("Vazia_DepoisDosPortoesAbrem");
            SceneManager.SetActiveScene(vazia);
            yield return SceneManager.UnloadSceneAsync(portoes);

            foreach (var go in GameObject.FindGameObjectsWithTag("Player"))
                Object.Destroy(go);

            yield return null;
        }

        [UnityTest]
        public IEnumerator DamiaoEncostadoNaBarreira_VeOPromptDosPortoes()
        {
            // O HUD é singleton persistente e outros testes da suíte o destroem no TearDown;
            // sem ele o GameLoopBootstrap loga um Error na carga e o runner reprova o teste
            // por "unhandled log message" — sem relação com o que se mede aqui.
            FavelaAmarela.Runtime.UI.HUDController.GarantirInstancia();
            yield return SceneManager.LoadSceneAsync(Cena, LoadSceneMode.Single);
            yield return null;

            var portao = Object.FindAnyObjectByType<PortaoDosPortoes>();
            Assert.NotNull(portao, "Sem PortaoDosPortoes na cena.");

            var barreira = portao.GetComponent<Collider2D>();
            Assert.NotNull(barreira, "Os Portões não têm colisor de barreira.");

            var jogador = GameObject.FindGameObjectWithTag("Player");
            Assert.NotNull(jogador, "Sem Player na cena.");
            var detector = jogador.GetComponentInChildren<DetectorDeInteracao>();
            Assert.NotNull(detector, "O Damião está sem DetectorDeInteracao.");
            var colisorDoJogador = jogador.GetComponent<Collider2D>();
            Assert.NotNull(colisorDoJogador, "O Damião está sem colisor.");

            // O mais perto que dá para chegar: o colisor do Damião encostado na face sul da
            // barreira, com uma folga de meia célula para a física não empurrar.
            float meiaAltura = colisorDoJogador.bounds.extents.y;
            var encostado = new Vector2(barreira.bounds.center.x, barreira.bounds.min.y - meiaAltura - 0.1f);

            var rb = jogador.GetComponent<Rigidbody2D>();
            if (rb != null) rb.position = encostado;
            jogador.transform.position = encostado;

            portao.Destrancar();
            Assert.IsTrue(portao.PodeInteragir, "Destrancar() não liberou a interação.");

            // O detector varre em FixedUpdate/Update; dois passos de física bastam.
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return null;

            float alcance = AlcanceDo(detector);
            float distancia = Vector2.Distance(jogador.transform.position, portao.PosicaoDeInteracao);
            TestContext.WriteLine($"barreira y {barreira.bounds.min.y:F2}..{barreira.bounds.max.y:F2}; " +
                                  $"Damião em {encostado}; distância à interação {distancia:F2}; alcance {alcance:F2}");

            Assert.LessOrEqual(distancia, alcance,
                $"Encostado na barreira, o Damião está a {distancia:F2} un do ponto de interação " +
                $"dos Portões e o alcance é {alcance:F2}: o prompt nunca aparece e os Portões não " +
                "abrem. É o defeito de 2026-09-10 — a PosicaoDeInteracao tem de ser a face que ele encosta.");

            Assert.AreSame(portao, detector.AlvoAtual,
                "O detector do Damião não escolheu os Portões como alvo, mesmo encostado neles e " +
                $"destrancados (alvo atual: {(detector.AlvoAtual == null ? "nenhum" : detector.AlvoAtual.GetType().Name)}).");
        }

        private static float AlcanceDo(DetectorDeInteracao detector)
        {
            var campo = typeof(DetectorDeInteracao).GetField("alcance", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(campo, "DetectorDeInteracao não tem mais o campo 'alcance' — adapte o teste.");
            return (float)campo.GetValue(detector);
        }
    }
}
