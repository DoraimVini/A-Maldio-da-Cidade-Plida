using System.Collections;
using System.Linq;
using FavelaAmarela.Core.Enemies;
using FavelaAmarela.Runtime.Combat;
using FavelaAmarela.Runtime.Enemies;
using FavelaAmarela.Runtime.Itens;
using FavelaAmarela.Runtime.Persistencia;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FavelaAmarela.Tests.PlayMode
{
    /// <summary>
    /// O Rito do Olhar <b>na cena real do Castelo</b>, com o Damião teleportado por roteiro: a
    /// linha de visão responde aos Nobres, a mente paga e se ancora, e o rito atravessa as cinco
    /// fases até o selamento — cada fase mudando a sala do jeito que o plano promete.
    ///
    /// <para>O guarda de geometria (<c>OTronoDoOlharTests</c>, EditMode) confere a conta parada;
    /// este confere que o adaptador faz a conta <b>quadro a quadro</b>, com a cena ligada, e
    /// que as peças que só existem em runtime (o fio, o farol, a tela, o Eco) aparecem.</para>
    /// </summary>
    public sealed class ORitoDoOlharTests
    {
        private const string Cena = "Castelo_Carcosa";
        private float _escala;

        [UnitySetUp]
        public IEnumerator Preparar()
        {
            _escala = Time.timeScale;
            // O save real do jogador (relíquias, posição) não pode entrar no teste.
            GerenciadorDeSave.Instancia?.LimparRegistro();
            FavelaAmarela.Runtime.UI.HUDController.GarantirInstancia();
            yield return SceneManager.LoadSceneAsync(Cena, LoadSceneMode.Single);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Desmontar()
        {
            Time.timeScale = _escala;
            GerenciadorDeSave.Instancia?.LimparRegistro();

            var castelo = SceneManager.GetSceneByName(Cena);
            if (!castelo.IsValid() || !castelo.isLoaded) yield break;

            var vazia = SceneManager.CreateScene("Vazia_DepoisDoRito");
            SceneManager.SetActiveScene(vazia);
            yield return SceneManager.UnloadSceneAsync(castelo);

            foreach (var go in GameObject.FindGameObjectsWithTag("Player"))
                Object.Destroy(go);
            yield return null;
        }

        // ── Utilidades ───────────────────────────────────────────────────────

        private static ReiEmAmareloAI Rei()
        {
            var rei = Object.FindAnyObjectByType<ReiEmAmareloAI>();
            Assert.NotNull(rei, "Sem Rei no Castelo.");
            return rei;
        }

        private static GameObject Damiao()
        {
            var jogador = GameObject.FindGameObjectWithTag("Player");
            Assert.NotNull(jogador, "Sem Player no Castelo.");
            return jogador;
        }

        private static void Teleportar(GameObject jogador, Vector2 p)
        {
            var rb = jogador.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.position = p;
                rb.linearVelocity = Vector2.zero;
            }
            jogador.transform.position = p;
        }

        /// <summary>Um ponto 1 un além da pegada do Nobre, na linha do olho do Rei.</summary>
        private static Vector2 AtrasDo(CoberturaDoTrono nobre, Vector2 olho)
        {
            var caixa = nobre.Caixa;
            Vector2 centro = (caixa.Min + caixa.Max) * 0.5f;
            Vector2 dir = (centro - olho).normalized;
            float t = 0f;
            while (caixa.Contem(centro + dir * t)) t += 0.02f;
            return centro + dir * (t + 1f);
        }

        private static IEnumerator Quadros(int n)
        {
            for (int i = 0; i < n; i++)
            {
                yield return new WaitForFixedUpdate();
                yield return null;
            }
        }

        // ── Os testes ────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator ALinhaDeVisao_SegueOsNobres_EAMentePagaOuSeAncora()
        {
            var rei = Rei();
            var jogador = Damiao();
            var mente = jogador.GetComponentInChildren<ResilienciaBridge>();
            Assert.NotNull(mente, "Damião sem ResilienciaBridge.");

            Assert.IsNull(rei.Rito, "O rito começou antes de o Damião entrar no Trono.");

            Teleportar(jogador, rei.Altar.transform.position);
            yield return Quadros(3);

            Assert.NotNull(rei.Rito, "Entrar no Trono não começou o rito.");
            Assert.AreEqual(FaseDoRito.Chegada, rei.Rito.Fase);
            Assert.IsTrue(rei.UltimaLeitura.NoAltar, "No centro do Altar, a leitura diz que não está no Altar.");
            Assert.IsTrue(rei.VendoODamiao,
                "No Altar o Damião tem de ser visto — nem o colisor do Rei nem o dele podem cortar a linha.");

            float antes = mente.Atual;
            yield return new WaitForSeconds(0.6f);
            Assert.Less(mente.Atual, antes, "Exposto no Altar, a mente não pagou nada.");
            Assert.Greater(rei.Rito.Selo, 0f, "Exposto no Altar, o selo não avançou.");

            var nobre = rei.CoberturasNaOrdemDoVerbo[0];
            Teleportar(jogador, AtrasDo(nobre, rei.OrigemDoOlhar));
            yield return Quadros(3);

            Assert.IsFalse(rei.UltimaLeitura.LinhaLivre, $"Atrás do Nobre '{nobre.name}' a linha continua livre.");
            Assert.IsFalse(rei.VendoODamiao, "Atrás de um Nobre o Rei ainda vê o Damião.");

            float naSombra = mente.Atual;
            float selo = rei.Rito.Selo;
            yield return new WaitForSeconds(0.6f);
            Assert.Greater(mente.Atual, naSombra, "Na sombra a mente não se ancorou.");
            Assert.AreEqual(selo, rei.Rito.Selo, 1e-4f, "Na sombra o selo andou — não existe estado seguro que avance o selo.");

            nobre.Desfazer();
            yield return Quadros(2);
            Assert.IsTrue(rei.VendoODamiao, "O Nobre foi desfeito e a sombra dele continua escondendo o Damião.");
        }

        [UnityTest]
        public IEnumerator ORito_AtravessaAsCincoFases_AteOSelamento()
        {
            var rei = Rei();
            var jogador = Damiao();

            Teleportar(jogador, rei.Altar.transform.position);
            yield return Quadros(3);
            var rito = rei.Rito;
            Assert.NotNull(rito, "Entrar no Trono não começou o rito.");

            var tela = GameObject.Find("Rito_Tela");
            Assert.IsTrue(tela != null && tela.activeInHierarchy, "A barra do selo não apareceu.");

            // Fase 2: o farol aparece.
            rito.PularPara(FaseDoRito.Mascara);
            yield return Quadros(2);
            var farol = GameObject.Find("Rito_FarolDaMascara")?.GetComponent<LineRenderer>();
            Assert.IsTrue(farol != null && farol.enabled, "Na Máscara o cone do olhar não está desenhado.");

            // Fase 3: um fragmento arde num altar de relíquia.
            rito.PularPara(FaseDoRito.Peca);
            yield return Quadros(2);
            Assert.IsFalse(farol.enabled, "O farol continuou depois da Máscara.");
            Assert.IsTrue(Object.FindObjectsByType<PontoFocalDeReliquia>().Any(p => p.ComFragmento),
                "Na Peça nenhum altar mostra fragmento.");

            // Fase 4: o Verbo desfaz o Nobre mais perto do Altar. O Damião espera atrás do último,
            // o que resiste — e o tempo corre acelerado para o pulso chegar.
            rito.PularPara(FaseDoRito.Verbo);
            Assert.IsFalse(Object.FindObjectsByType<PontoFocalDeReliquia>().Any(p => p.ComFragmento),
                "O fragmento continuou ardendo depois da Peça.");
            var ordem = rei.CoberturasNaOrdemDoVerbo;
            Teleportar(jogador, AtrasDo(ordem[ordem.Count - 1], rei.OrigemDoOlhar));
            Time.timeScale = 4f;
            float limite = Time.realtimeSinceStartup + 6f;
            while (rito.Verbo.Desfeitas < 1 && Time.realtimeSinceStartup < limite) yield return null;
            Time.timeScale = _escala;

            Assert.GreaterOrEqual(rito.Verbo.Desfeitas, 1, "O Verbo não pulsou.");
            Assert.IsFalse(ordem[0].DePe, $"O primeiro pulso não desfez o Nobre mais perto do Altar ('{ordem[0].name}').");
            Assert.IsTrue(ordem[ordem.Count - 1].DePe, "O último Nobre caiu no primeiro pulso.");

            // Fase 5: o Eco se manifesta em quem fica parado.
            rito.PularPara(FaseDoRito.Queda);
            yield return Quadros(2);
            var eco = Object.FindObjectsByType<EcoDeCarcosa>().FirstOrDefault(e => e.name == "Eco_Da_Queda");
            Assert.NotNull(eco, "Na Queda o Eco do Trono não foi ativado.");
            yield return new WaitForSeconds(2.2f);
            var visual = eco.transform.Find("Visual_Eco");
            Assert.IsTrue(visual != null && visual.gameObject.activeSelf,
                "Parado por 2 s na Queda, o Eco não se manifestou (o limite é 1,5 s).");

            bool venceu = false;
            rei.OnVitoria += () => venceu = true;
            rito.PularPara(FaseDoRito.Selado);
            yield return null;

            Assert.IsTrue(venceu, "O selo fechou e o Rei não disparou OnVitoria — a SequenciaDeSelamento não fecharia o jogo.");
            Assert.IsTrue(rito.Encerrado);
            Assert.IsFalse(tela.activeInHierarchy, "A barra do selo ficou na tela depois do selamento.");
        }
    }
}
