using System.Collections;
using FavelaAmarela.Core.Persistencia;
using FavelaAmarela.Runtime.Enemies;
using FavelaAmarela.Runtime.GameLoop;
using FavelaAmarela.Runtime.Persistencia;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FavelaAmarela.Tests.PlayMode
{
    /// <summary>
    /// O Byakhee <b>morre uma vez</b>: o abate grava um marco, e a arena volta resolvida.
    ///
    /// <para><b>O defeito (2026-09-10).</b> O Vini: <i>"não está salvando pós a luta da
    /// Byakhee; se você sair e voltar para a masmorra pode enfrentar ela de novo e ela vira
    /// farm infinito"</i>. Nada gravava a vitória — a cena dos Portões não tinha um único
    /// <c>ObjetoPersistente</c>, e o gatilho da arena não lia o save. Cada visita: chefe
    /// inteiro, 200 de Exposição e o espólio de novo.</para>
    ///
    /// <para>Na <b>cena real</b>, porque o defeito era de ligação: o que se testa é que o
    /// gatilho grava, e que a cena recarregada lê. Um rig com a chave em memória não provaria
    /// nem uma coisa nem outra.</para>
    /// </summary>
    public sealed class OByakheeNaoEhFarmTests
    {
        private const string Cena = "Portoes_Das_Ruinas";

        [UnitySetUp]
        public IEnumerator Montar()
        {
            // Registro limpo: um teste não pode herdar o marco do outro.
            GerenciadorDeSave.Instancia?.LimparRegistro();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Desmontar()
        {
            GerenciadorDeSave.Instancia?.LimparRegistro();

            var portoes = SceneManager.GetSceneByName(Cena);
            if (!portoes.IsValid() || !portoes.isLoaded) yield break;

            var vazia = SceneManager.CreateScene("Vazia_DepoisDoFarm");
            SceneManager.SetActiveScene(vazia);
            yield return SceneManager.UnloadSceneAsync(portoes);

            foreach (var go in GameObject.FindGameObjectsWithTag("Player"))
                Object.Destroy(go);

            yield return null;
        }

        [UnityTest]
        public IEnumerator AbaterOByakhee_GravaOMarcoDaQuest()
        {
            // O HUD é singleton persistente e outros testes da suíte o destroem no TearDown;
            // sem ele o GameLoopBootstrap loga um Error na carga e o runner reprova o teste
            // por "unhandled log message" — sem relação com o que se mede aqui.
            FavelaAmarela.Runtime.UI.HUDController.GarantirInstancia();
            yield return SceneManager.LoadSceneAsync(Cena, LoadSceneMode.Single);
            yield return null;

            Assert.IsFalse(GerenciadorDeSave.JaAconteceu(ChavesDeSave.ByakheeAbatido),
                "O marco já estava gravado antes da luta — o registro não começou limpo.");

            var ai = Object.FindAnyObjectByType<ByakheeAI>();
            Assert.NotNull(ai, "Sem ByakheeAI na cena dos Portões.");
            var corpo = ai.GetComponent<EnemyBase>();
            Assert.NotNull(corpo, "O Byakhee não tem EnemyBase.");

            ai.IniciarLuta();
            yield return null;

            // Direto na Vitalidade, sem passar pela imunidade do ar: o que se testa é o que
            // acontece DEPOIS de a vida acabar, não o caminho até lá.
            corpo.Vitalidade.Ferir(corpo.Vitalidade.Max * 10f);
            yield return null;

            Assert.IsTrue(GerenciadorDeSave.JaAconteceu(ChavesDeSave.ByakheeAbatido),
                "O Byakhee caiu e ninguém gravou 'Quest.Portoes.ByakheeAbatido'. Sair e voltar " +
                "vai remontar a luta — é o farm infinito de 2026-09-10.");
        }

        [UnityTest]
        public IEnumerator AoVoltarComOByakheeAbatido_AArenaNasceResolvida()
        {
            GerenciadorDeSave.MarcarAconteceu(ChavesDeSave.ByakheeAbatido);

            // O HUD é singleton persistente e outros testes da suíte o destroem no TearDown;
            // sem ele o GameLoopBootstrap loga um Error na carga e o runner reprova o teste
            // por "unhandled log message" — sem relação com o que se mede aqui.
            FavelaAmarela.Runtime.UI.HUDController.GarantirInstancia();
            yield return SceneManager.LoadSceneAsync(Cena, LoadSceneMode.Single);
            yield return null;
            yield return null;

            var arena = Object.FindAnyObjectByType<ArenaDosPortoes>();
            Assert.NotNull(arena, "Sem ArenaDosPortoes na cena.");

            Assert.IsNull(Object.FindAnyObjectByType<ByakheeAI>(),
                "O Byakhee está ATIVO numa arena cuja quest já foi resolvida — ele pode ser " +
                "enfrentado (e farmado) de novo.");

            var portao = Object.FindAnyObjectByType<PortaoDosPortoes>();
            Assert.NotNull(portao, "Sem PortaoDosPortoes na cena.");
            Assert.IsTrue(portao.Destrancado,
                "Os Portões nasceram TRANCADOS com o chefe já abatido: sem chefe para cair, " +
                "ninguém os destranca — softlock.");

            var refugio = Object.FindAnyObjectByType<RefugioDeLuz>();
            Assert.NotNull(refugio, "Sem RefugioDeLuz na cena.");
            Assert.IsTrue(refugio.enabled,
                "O Poste de Luz nasceu apagado com o chefe já abatido — o Refúgio da arena " +
                "some para quem volta.");

            Assert.IsTrue(arena.LutaComecou,
                "A arena não se considera resolvida: um segundo passo no gatilho tentaria " +
                "despertar um chefe que não existe.");
        }

        [UnityTest]
        public IEnumerator AoVoltarComOsPortoesAbertos_ElesNascemAbertos()
        {
            GerenciadorDeSave.MarcarAconteceu(ChavesDeSave.ByakheeAbatido);
            GerenciadorDeSave.MarcarAconteceu(ChavesDeSave.PortoesAbertos);

            // O HUD é singleton persistente e outros testes da suíte o destroem no TearDown;
            // sem ele o GameLoopBootstrap loga um Error na carga e o runner reprova o teste
            // por "unhandled log message" — sem relação com o que se mede aqui.
            FavelaAmarela.Runtime.UI.HUDController.GarantirInstancia();
            yield return SceneManager.LoadSceneAsync(Cena, LoadSceneMode.Single);
            yield return null;
            yield return null;

            var portao = Object.FindAnyObjectByType<PortaoDosPortoes>();
            Assert.NotNull(portao, "Sem PortaoDosPortoes na cena.");
            Assert.IsTrue(portao.Destrancado && !portao.PodeInteragir,
                "Quem volta do Castelo encontrou os Portões fechados de novo.");
        }
    }
}
