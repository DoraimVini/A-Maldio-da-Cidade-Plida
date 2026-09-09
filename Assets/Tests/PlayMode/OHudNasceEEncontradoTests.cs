using FavelaAmarela.Runtime.UI;
using NUnit.Framework;
using UnityEngine;

namespace FavelaAmarela.Tests.PlayMode
{
    /// <summary>
    /// Guarda o <b>mecanismo</b> de nascimento do HUD persistente.
    ///
    /// <para><b>O que estes testes medem, e o que deliberadamente NÃO medem.</b> Eles provam que
    /// o prefab está onde o <c>Resources.Load</c> o procura e que
    /// <c>HUDController.GarantirInstancia()</c> de fato cria o HUD. Eles <b>não</b> provam que o
    /// HUD sobrevive a uma sessão de jogo — e entender essa diferença custou caro.</para>
    ///
    /// <para><b>A investigação, registrada para não se repetir (2026-09-09).</b> Rodando as
    /// cenas de verdade num teste apareceu <c>"[GameLoopBootstrap] Nenhum HUDController na
    /// cena"</c>. Duas conclusões minhas caíram, uma depois da outra:</para>
    ///
    /// <para><b>1.</b> Procurei o GUID do <c>HUDController</c> e o do <c>HUD_Gameplay.prefab</c>
    /// nas seis cenas, não achei, e concluí que o HUD estava morto no jogo inteiro. Errado:
    /// <b>ausência das cenas é o desenho</b> — ele nasce de <c>Resources</c> em runtime, e o
    /// <c>Ocultar()</c> desliga o <c>Canvas</c> e não o <c>GameObject</c>, justamente para
    /// continuar encontrável por <c>FindAnyObjectByType</c>.</para>
    ///
    /// <para><b>2.</b> Instrumentei o caminho, e o log mostrou o oposto do que eu supunha:
    /// <c>prefab=True</c>, <c>Awake rodou</c>, <c>ativo=True</c>, <c>Instancia=True</c> — o HUD
    /// <b>nasce perfeitamente</b>. E logo depois <c>Instancia=False</c>: ele é <b>destruído</b>,
    /// repetidamente. O runner de PlayMode limpa objetos entre testes, inclusive
    /// <c>DontDestroyOnLoad</c>.</para>
    ///
    /// <para><b>Conclusão honesta: um teste em batch não decide isto</b> — o que ele mede é o
    /// próprio runner. Se o HUD aparece numa sessão de jogo de verdade, só olhando. Por isso
    /// estes testes guardam o mecanismo, que é o que dá para guardar, em vez de fingir que
    /// guardam o resultado.</para>
    /// </summary>
    public sealed class OHudNasceEEncontradoTests
    {
        /// <summary>
        /// O prefab precisa continuar numa pasta <c>Resources</c>. Tirá-lo de lá não quebra
        /// compilação nem cena — só faz o <c>Resources.Load</c> devolver <c>null</c> em runtime,
        /// e o jogo roda sem HUD nenhum, avisando por um <c>LogError</c> no console.
        /// </summary>
        [Test]
        public void OPrefabDoHud_ContinuaEmResources()
        {
            Assert.NotNull(Resources.Load<GameObject>("HUD_Gameplay"),
                "Resources.Load<GameObject>(\"HUD_Gameplay\") devolveu null.");
        }

        /// <summary>
        /// Chamado, o método cria o HUD: prefab carregado, objeto ativo, componente presente.
        ///
        /// <para>É o passo que a instrumentação confirmou funcionar. Guardá-lo impede regressão
        /// no <i>mecanismo</i> — por exemplo alguém salvar o prefab com a raiz <b>inativa</b>, o
        /// que faria o <c>Awake</c> nunca rodar e o HUD nunca se registrar.</para>
        /// </summary>
        [Test]
        public void GarantirInstancia_CriaOHudAtivoEComOComponente()
        {
            var antes = HUDController.Instancia;

            HUDController.GarantirInstancia();
            var hud = HUDController.Instancia;

            Assert.NotNull(hud,
                "GarantirInstancia() não deixou instância. Se o prefab foi salvo com a raiz " +
                "INATIVA, o Awake nunca roda e o HUD nunca se registra.");

            Assert.IsTrue(hud.gameObject.activeInHierarchy,
                "O HUD nasceu inativo. O FindAnyObjectByType do GameLoopBootstrap pula " +
                "inativos — é por isso que Ocultar() desliga o Canvas e não o objeto.");

            if (antes == null) Object.DestroyImmediate(hud.gameObject);
        }

        /// <summary>
        /// As duas views que interessam ao jogador — a da vida corpórea e a da Resiliência
        /// Mental — precisam continuar dentro do prefab.
        ///
        /// <para>Elas <b>já existiam</b> quando o pedido de "inserir UIs para vida e RM"
        /// chegou. O que falta não é construí-las: é confirmar que aparecem em jogo.</para>
        /// </summary>
        [Test]
        public void OPrefabTemABarraDeVidaEADeResiliencia()
        {
            var prefab = Resources.Load<GameObject>("HUD_Gameplay");
            Assert.NotNull(prefab, "Sem prefab não há barra para conferir.");

            Assert.NotNull(prefab.GetComponentInChildren<VitalidadeBar>(true),
                "Sumiu a VitalidadeBar — a barra da vida corpórea.");

            Assert.NotNull(prefab.GetComponentInChildren<ResilienciaBar>(true),
                "Sumiu a ResilienciaBar — a barra da Resiliência Mental.");
        }
    }
}
