using System.Collections;
using FavelaAmarela.Runtime.Enemies;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FavelaAmarela.Tests.PlayMode
{
    /// <summary>
    /// O <c>AnimadorDoByakhee</c> <b>ouve</b> a <c>ByakheeFSM</c> — na cena real, com a ordem
    /// de inicialização real.
    ///
    /// <para><b>O defeito (2026-09-10).</b> A inscrição no <c>OnStateChanged</c> era feita no
    /// <c>OnEnable</c>, lendo <c>_ai.Fsm</c> — criada no <c>Awake</c> do <c>ByakheeAI</c>. A
    /// Unity não garante Awake de um componente antes do OnEnable de outro; no Editor vivo a
    /// FSM tinha <b>um</b> inscrito, o próprio AI, e a luta inteira tocava os quatro quadros da
    /// espreita — rasante, garras e grito nunca apareceram. Nenhum teste reprovava porque
    /// nenhum instanciava o prefab e olhava o sprite depois de uma transição.</para>
    ///
    /// <para>Por isso este teste carrega a <b>cena dos Portões</b> (o prefab como está ligado)
    /// e não um rig: a ordem de Awake/OnEnable é justamente o que se quer exercitar.</para>
    /// </summary>
    public sealed class OAnimadorDoByakheeOuveAFsmTests
    {
        private const string Cena = "Portoes_Das_Ruinas";

        [UnityTearDown]
        public IEnumerator Desmontar()
        {
            var portoes = SceneManager.GetSceneByName(Cena);
            if (!portoes.IsValid() || !portoes.isLoaded) yield break;

            var vazia = SceneManager.CreateScene("Vazia_DepoisDoAnimador");
            SceneManager.SetActiveScene(vazia);
            yield return SceneManager.UnloadSceneAsync(portoes);

            // O que a cena promoveu a DontDestroyOnLoad e carrega a tag Player não pode
            // sobreviver para o rig seguinte.
            foreach (var go in GameObject.FindGameObjectsWithTag("Player"))
                Object.Destroy(go);

            yield return null;
        }

        [UnityTest]
        public IEnumerator DepoisDoStart_OAnimadorEstaInscrito()
        {
            yield return SceneManager.LoadSceneAsync(Cena, LoadSceneMode.Single);
            yield return null;

            var animador = Object.FindAnyObjectByType<AnimadorDoByakhee>();
            Assert.NotNull(animador, "Sem AnimadorDoByakhee na cena dos Portões.");

            Assert.IsTrue(animador.EstaInscritoNaFsm,
                "O AnimadorDoByakhee não está inscrito na ByakheeFSM depois do Start. Sem isto " +
                "a luta inteira toca os quadros da espreita, seja qual for o estado.");
        }

        [UnityTest]
        public IEnumerator AoComecarALuta_OSpriteTrocaParaORasante()
        {
            yield return SceneManager.LoadSceneAsync(Cena, LoadSceneMode.Single);
            yield return null;

            var ai = Object.FindAnyObjectByType<ByakheeAI>();
            Assert.NotNull(ai, "Sem ByakheeAI na cena dos Portões.");
            var sprite = ai.GetComponent<SpriteRenderer>();

            string antes = sprite.sprite != null ? sprite.sprite.name : "(nulo)";
            Assert.IsTrue(antes.StartsWith("byakhee_espreita"),
                $"Antes da luta o Byakhee devia estar na espreita; está em '{antes}'.");

            ai.IniciarLuta();
            Assert.AreEqual(FavelaAmarela.Core.Enemies.ByakheeState.Rasante, ai.Fsm.CurrentState,
                "IniciarLuta não levou a FSM ao Rasante — o teste mede outra coisa.");

            // O HandleEstadoMudou troca o ciclo na hora; um quadro basta para o sprite refletir.
            yield return null;

            Assert.IsTrue(sprite.sprite.name.StartsWith("byakhee_rasante"),
                $"A FSM está em Rasante e o sprite é '{sprite.sprite.name}'. O animador não " +
                "ouviu a transição — é o defeito de 2026-09-10 de volta.");
        }
    }
}
