using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using FavelaAmarela.Runtime.Combat;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a física de impacto — o corpo que o golpe finalmente move.
    ///
    /// <para><b>O estado de antes (medido em 2026-08-27).</b> O combate não tinha física
    /// nenhuma: <c>AddForce</c>, <c>MovePosition</c> e <c>linearVelocity +=</c> não apareciam em
    /// <c>Assets/Scripts</c>, e <c>ForcaRepulsao</c> tinha <b>zero leitores</b> — o Alfanje de
    /// Alhazred, cuja identidade de design em <c>armas_da_tumba.md</c> é <i>"força bruta e
    /// espaço"</i>, preenchia o campo com 6 e não empurrava nada. O golpe era aritmética sobre
    /// uma consulta de sobreposição.</para>
    ///
    /// <para>Boa parte do que importa aqui só se manifesta em Play Mode, então estes guardas
    /// verificam o <b>contrato no código</b> — o mesmo estilo de
    /// <c>AtributosConsumidosTests</c>. É deliberado: as propriedades abaixo são invisíveis e,
    /// se alguém as remover, nada quebra visivelmente. O empurrão só some.</para>
    /// </summary>
    public sealed class FisicaDeImpactoTests
    {
        private const string Repulsao = "Assets/Scripts/Combat/RepulsaoDeImpacto.cs";
        private const string HitStopArquivo = "Assets/Scripts/Combat/HitStop.cs";
        private const string Bridge = "Assets/Scripts/Player/MaoFisicaBridge.cs";
        private const string HitboxArquivo = "Assets/Scripts/Combat/Hitbox.cs";

        private static string Ler(string caminho)
        {
            Assert.IsTrue(File.Exists(caminho), $"Arquivo ausente: {caminho}");
            return File.ReadAllText(caminho);
        }

        /// <summary>
        /// <b>O guarda mais importante deste arquivo.</b> <c>PlayerMovement</c> e as IAs
        /// <b>atribuem</b> <c>linearVelocity</c> a cada <c>FixedUpdate</c> — são 20+ atribuições
        /// em 9 arquivos. A repulsão só sobrevive porque roda <b>depois</b> delas.
        ///
        /// <para>Se alguém remover o atributo, o empurrão é sobrescrito no mesmo passo de física
        /// e desaparece <b>sem erro, sem log e sem teste falhando</b> — a não ser este.</para>
        /// </summary>
        [Test]
        public void ARepulsao_RodaDepoisDoMovimento()
        {
            var ordem = typeof(RepulsaoDeImpacto)
                .GetCustomAttributes(typeof(DefaultExecutionOrder), false);

            Assert.IsNotEmpty(ordem,
                "RepulsaoDeImpacto perdeu o [DefaultExecutionOrder]. Sem ele, o movimento " +
                "sobrescreve o empurrão no mesmo FixedUpdate e o knockback some em silêncio.");

            int valor = ((DefaultExecutionOrder)ordem[0]).order;
            Assert.Greater(valor, 0,
                $"Ordem de execução {valor} não é maior que a do movimento (0) — o empurrão " +
                "seria escrito antes e apagado depois.");
        }

        /// <summary>
        /// O empurrão tem de ser <b>atribuição de velocidade</b>, não força. A doc da Unity 6.4
        /// diz que <c>linearVelocity</c> "is not usually set directly but rather by using
        /// forces" — este projeto faz o contrário por convenção (<c>CLAUDE.md</c> §5), e por
        /// isso um <c>AddForce</c> aqui seria engolido pela atribuição do movimento.
        /// </summary>
        [Test]
        public void ARepulsao_NaoUsaAddForce()
        {
            // Casa a CHAMADA (`.AddForce(`), não a palavra: o XML doc do próprio componente
            // menciona AddForce para explicar por que ele não é usado, e a primeira versão
            // deste guarda falhou contra o comentário que ele mesmo motivou.
            StringAssert.DoesNotContain(".AddForce(", Ler(Repulsao),
                "AddForce seria sobrescrito pela atribuição de linearVelocity do movimento no " +
                "passo de física seguinte. O empurrão precisa ser atribuído, não aplicado.");

            StringAssert.Contains("linearVelocity =", Ler(Repulsao),
                "A repulsão precisa ATRIBUIR velocidade — é a convenção de movimento deste " +
                "projeto (CLAUDE.md §5) e o único jeito de não ser apagada.");
        }

        /// <summary>
        /// Os dois — e apenas os dois — pontos onde um golpe aterrissa têm de aplicar repulsão:
        /// o golpe do Damião e o golpe que acerta alguém pela <c>Hitbox</c>.
        /// </summary>
        [Test]
        public void OsDoisCaminhosDeGolpe_AplicamRepulsao()
        {
            var faltando = new List<string>();

            string bridge = Ler(Bridge);
            if (!bridge.Contains("ForcaRepulsao"))
                faltando.Add("MaoFisicaBridge não lê ForcaRepulsao — o golpe do Damião não empurra");

            string hitbox = Ler(HitboxArquivo);
            if (!hitbox.Contains("ForcaRepulsao"))
                faltando.Add("Hitbox não lê ForcaRepulsao — o golpe do inimigo não empurra");

            Assert.IsEmpty(faltando,
                "ForcaRepulsao voltou a ser dado morto:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", faltando));
        }

        /// <summary>
        /// <c>IArma.CanActivate</c> ficou declarado e <b>nunca chamado</b>, com a cadência
        /// caindo em <c>DurationSeconds</c>. Efeito: o <c>cooldownBasico</c> de toda arma era
        /// dado morto e as três batiam quase na mesma velocidade — o Alfanje (0,7 s) não pesava
        /// mais que o Estilete (0,3 s). Os POCOs sempre tiveram o contrato testado; era a
        /// bridge que não perguntava.
        /// </summary>
        [Test]
        public void ACadenciaDoBasico_EPerguntadaAArma()
        {
            StringAssert.Contains("CanActivate(", Ler(Bridge),
                "A MaoFisicaBridge voltou a ignorar a cadência da arma. Sem isto, autorar uma " +
                "arma pesada e lenta não produz arma pesada e lenta.");
        }

        /// <summary>
        /// O gizmo desenhava raio <c>alcance</c> centrado no jogador enquanto a consulta usa
        /// raio <c>alcance/2</c> deslocado à frente — <b>quatro vezes</b> a área real, no lugar
        /// errado. Calibrar alcance olhando para ele levava à conclusão oposta da verdadeira.
        /// </summary>
        [Test]
        public void OGizmoDoGolpe_DesenhaOVolumeQueEConsultado()
        {
            string bridge = Ler(Bridge);

            int inicio = bridge.IndexOf("OnDrawGizmosSelected", StringComparison.Ordinal);
            Assert.Greater(inicio, -1, "O gizmo do golpe sumiu da MaoFisicaBridge.");

            string gizmo = bridge.Substring(inicio);

            StringAssert.Contains("alcance * 0.5f", gizmo,
                "O gizmo não usa o mesmo raio da consulta (alcance * 0.5f). Um gizmo que mente " +
                "é pior que gizmo nenhum: leva a calibrar o alcance para o lado errado.");
        }

        /// <summary>
        /// <c>GameStatePresenter</c> também escreve <c>Time.timeScale</c>. Se o jogador pausar
        /// durante um hit-stop, restaurar cegamente para 1 <b>despausaria o jogo sozinho</b>.
        /// </summary>
        [Test]
        public void OHitStop_NaoDespausaOJogo()
        {
            string codigo = Ler(HitStopArquivo);

            StringAssert.Contains("Approximately(Time.timeScale", codigo,
                "O HitStop voltou a restaurar o timeScale sem conferir se ainda é dono dele. " +
                "Pausar durante um hit-stop despausaria o jogo.");

            StringAssert.Contains("unscaledDeltaTime", codigo,
                "O hit-stop tem de contar em tempo não-escalado — em tempo escalado ele se " +
                "prolonga na proporção do próprio congelamento.");
        }

        // ── A tabela diegética ────────────────────────────────────────────────

        private const string PastaDosInimigos = "Assets/FavelaAmarela/Art/Enemies";

        private static float ResistenciaDe(string prefab)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>($"{PastaDosInimigos}/{prefab}.prefab");
            Assert.IsNotNull(go, $"Prefab ausente: {prefab}");

            var corpo = go.GetComponent<CorpoImpregnado>();
            Assert.IsNotNull(corpo,
                $"'{prefab}' não tem CorpoImpregnado. Conserto: " +
                "'Tools/FavelaAmarela/Física: marcar corpos impregnados'.");

            return corpo.ResistenciaAImpulso;
        }

        /// <summary>
        /// A <b>ordem</b> é o que importa, não os números: é ela que faz o impacto ensinar o que
        /// cada coisa é. Um Cultista sempre cede mais que um Byakhee, que sempre cede mais que
        /// um Espectro. Os valores em si são botões de balanceamento.
        /// </summary>
        [Test]
        public void AImpregnacao_CresceComOQuantoACoisaDeixouDeSerMateria()
        {
            float esqueleto = ResistenciaDe("EsqueletoInvocado");
            float cultista = ResistenciaDe("Cultista");
            float coisa = ResistenciaDe("CoisaDoCemiterio");
            float byakhee = ResistenciaDe("Byakhee");
            float espectro = ResistenciaDe("EspectroHali");

            Assert.Less(esqueleto, cultista, "Ossos montados às pressas cedem mais que gente.");
            Assert.Less(cultista, coisa, "Gente cede mais que a caça pesada do cemitério.");
            Assert.Less(coisa, byakhee, "A Coisa ainda é matéria; o Byakhee é de fora.");
            Assert.Less(byakhee, espectro, "O Espectro é o que menos ainda é corpo.");

            Assert.LessOrEqual(espectro, 1f, "Resistência acima de 1 não tem significado.");
        }

        /// <summary>
        /// Ator sem o componente é corpo comum. É o padrão certo — a maioria do elenco é gente,
        /// e exigir o componente em todo mundo faria dele mais uma lista à mão para envelhecer.
        /// </summary>
        [Test]
        public void SemOComponente_OCorpoEComum()
        {
            var go = new GameObject("alvo de teste");
            try
            {
                Assert.AreEqual(0f, CorpoImpregnado.De(go.transform),
                    "Ator sem CorpoImpregnado tem de ser totalmente empurrável.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
