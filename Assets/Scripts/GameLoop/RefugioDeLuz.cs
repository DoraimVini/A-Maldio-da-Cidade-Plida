using UnityEngine;
using FavelaAmarela.Runtime.Persistencia;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). <b>Poste de Luz / Refúgio</b> — área de luz onde
    /// Damião fica a salvo (ver <c>lore/world_rules.md</c>: "luz é refúgio"). Faz três
    /// coisas ao jogador entrar:
    /// <list type="number">
    ///   <item><b>Ancoragem</b>: devolve Resiliência Mental.</item>
    ///   <item><b>Reanima Yug-Neth</b> se ele estiver incapacitado.</item>
    ///   <item><b>Salva a partida</b> em disco — o GDD §8.3 já decidia que o save acontece
    ///   nos Postes de Luz, e este é o único lugar do jogo que grava.</item>
    /// </list>
    ///
    /// <para>Automático por proximidade (entrar na luz), não por botão — casa com a leitura
    /// diegética de "descansar sob o poste", diferente da interação deliberada (botão E)
    /// usada para objetos que o jogador escolhe usar (baú, patuá, NPC).</para>
    ///
    /// <para><b>Reentrada não repete o efeito completo</b>: sair e voltar não vira uma
    /// bomba de cura infinita. A Ancoragem só acontece de novo depois de
    /// <see cref="intervaloDeAncoragem"/>, mas <b>reanimar Yug-Neth e salvar sempre
    /// valem</b> — são estados, não recursos.</para>
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Favela Amarela/GameLoop/Refúgio de Luz")]
    public sealed class RefugioDeLuz : MonoBehaviour
    {
        [Header("Ancoragem")]
        [Tooltip("Quanta Resiliência Mental a luz devolve. 0 = não ancora (poste apagado).")]
        [Min(0f)]
        [SerializeField] private float resilienciaRestaurada = 100f;

        [Tooltip("Segundos até este Refúgio poder ancorar de novo. Impede farm de cura " +
                 "entrando e saindo da luz.")]
        [Min(0f)]
        [SerializeField] private float intervaloDeAncoragem = 30f;

        [Header("Save")]
        [Tooltip("Grava a partida em disco ao descansar aqui (GDD §8.3: save nos Postes de Luz).")]
        [SerializeField] private bool salvaAoDescansar = true;

        [Header("Feedback")]
        [Tooltip("Caixa de texto do Refúgio (reaproveita a UI de dica por ora).")]
        [SerializeField] private TutorialHintUI caixaDeTexto;

        [TextArea]
        [Tooltip("Mensagem ao descansar sob a luz.")]
        [SerializeField] private string mensagemDeDescanso =
            "A luz te alcança. Por um instante, o amarelo lá fora não parece te ver.";

        [TextArea]
        [Tooltip("Mensagem ao reanimar Yug-Neth aqui.")]
        [SerializeField] private string mensagemDeReanimacao =
            "Sob a luz, Yug-Neth estremece e volta a pulsar. Ele se levanta.";

        private float _proximaAncoragem;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (!col.isTrigger)
                Debug.LogError($"[RefugioDeLuz] '{name}' precisa de um Collider2D marcado " +
                               "como Trigger — corrigindo em runtime.", this);
            col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            bool algoAconteceu = ReanimarCompanheiro();
            algoAconteceu |= Ancorar();

            if (salvaAoDescansar) Salvar();

            if (algoAconteceu && caixaDeTexto != null && !string.IsNullOrWhiteSpace(mensagemDeDescanso))
                caixaDeTexto.Mostrar(mensagemDeDescanso);
        }

        /// <summary>Devolve Resiliência Mental, respeitando o intervalo entre descansos.</summary>
        private bool Ancorar()
        {
            if (resilienciaRestaurada <= 0f || Time.time < _proximaAncoragem) return false;

            var resiliencia = GameManager.Instance != null ? GameManager.Instance.Resiliencia : null;
            if (resiliencia == null) return false;

            resiliencia.Ancorar(resilienciaRestaurada);
            _proximaAncoragem = Time.time + intervaloDeAncoragem;
            return true;
        }

        /// <summary>
        /// Reanima Yug-Neth se ele tiver caído. <b>Sem intervalo</b>: é um estado que precisa
        /// ser desfeito, não um recurso — e ele bloqueia os Portões de Carcosa enquanto
        /// estiver caído.
        /// </summary>
        private bool ReanimarCompanheiro()
        {
            var yugNeth = GameManager.Instance != null ? GameManager.Instance.YugNeth : null;
            if (yugNeth == null || !yugNeth.EstaIncapacitado) return false;

            yugNeth.Reanimar();

            if (caixaDeTexto != null) caixaDeTexto.Mostrar(mensagemDeReanimacao);
            return true;
        }

        /// <summary>
        /// Fotografa o estado e grava em disco. É o <b>único ponto do jogo que salva</b> —
        /// até aqui o <c>GerenciadorDeSave</c> tinha a gravação pronta mas ninguém a chamava,
        /// então fechar o jogo perdia tudo.
        /// </summary>
        private void Salvar()
        {
            var gerenciador = GerenciadorDeSave.Instancia;
            if (gerenciador == null) return;

            gerenciador.CapturarTudo();
            gerenciador.GravarEmDisco();
        }

        // TODO(design): pausar o dreno de RM da tempestade enquanto o jogador estiver na luz
        // (o dreno em si ainda não existe — decisão do Vini de a tempestade afetar só inimigos).
    }
}
