using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Core.GameLoop;
using FavelaAmarela.Runtime.GameLoop;

namespace FavelaAmarela.Runtime.UI
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). O que o jogador faz <b>depois de morrer</b>.
    ///
    /// <para><b>Morrer não devolve ao menu principal</b> (decisão do Vini, 2026-08-11).
    /// Mandar o jogador para a tela-título a cada morte transforma cada erro em três cliques
    /// de burocracia até voltar a jogar — desestimula em vez de punir. A punição diegética já
    /// é a sequência de Colapso e o trecho perdido desde o último Refúgio.</para>
    ///
    /// <para>As saídas, em ordem de menor atrito:</para>
    /// <list type="number">
    ///   <item><b>Último Refúgio de Luz</b> — o padrão, quando já houve um.</item>
    ///   <item><b>Entrada do Deserto</b> — para quem morreu antes de alcançar qualquer Refúgio.</item>
    ///   <item><b>Menu principal</b> — sair de verdade, e só se o jogador escolher.</item>
    /// </list>
    ///
    /// <para>Os botões só aparecem depois da sequência de morte: deixar pular a própria morte
    /// no primeiro frame apagaria a única punição diegética que sobrou.</para>
    ///
    /// <para><b>Observa evento, não faz polling</b> (regra da camada UI): assina
    /// <c>OnStateChanged</c> em vez de perguntar o estado ao <c>GameManager</c> a cada frame.</para>
    /// </summary>
    [AddComponentMenu("FavelaAmarela/UI/Retorno do Colapso")]
    public sealed class RetornoDoColapso : MonoBehaviour
    {
        [Tooltip("Segundos de espera antes de mostrar as opções, para a morte ser vista.")]
        [Min(0f)]
        [SerializeField] private float atrasoAntesDeOferecer = 3f;

        [Header("Opções")]
        [Tooltip("Grupo que contém os botões; escondido durante a sequência. [ASSET]")]
        [SerializeField] private GameObject grupoDeOpcoes;

        [Tooltip("Retoma no último Refúgio. Vira 'entrada do Deserto' se não houver nenhum. [ASSET]")]
        [SerializeField] private Button botaoRetomar;

        [Tooltip("Rótulo do botão de retomar, trocado conforme haja Refúgio ou não. [ASSET]")]
        [SerializeField] private Text rotuloRetomar;

        [Tooltip("Sai para a tela-título. [ASSET]")]
        [SerializeField] private Button botaoMenu;

        private GameLoopStateMachine _maquina;
        private float _tempoNoColapso;
        private bool _oferecendo;
        private bool _emColapso;

        private void Awake()
        {
            if (botaoRetomar != null) botaoRetomar.onClick.AddListener(Retomar);
            if (botaoMenu != null) botaoMenu.onClick.AddListener(NavegacaoDeCenas.IrParaMenu);
        }

        private void Start()
        {
            _maquina = GameManager.Instance?.StateMachine;

            if (_maquina == null)
            {
                Debug.LogError("[RetornoDoColapso] Sem GameManager — morrer viraria beco sem " +
                               "saída.", this);
                return;
            }

            _maquina.OnStateChanged += HandleEstadoMudou;
            Esconder();
        }

        private void OnDestroy()
        {
            if (_maquina != null) _maquina.OnStateChanged -= HandleEstadoMudou;
        }

        private void HandleEstadoMudou(GameState anterior, GameState atual)
        {
            _emColapso = atual == GameState.Colapso;
            if (_emColapso) Esconder();
        }

        private void Update()
        {
            if (!_emColapso || _oferecendo) return;

            // Tempo real: o Colapso pode vir de um estado congelado, e um relógio preso ao
            // deltaTime escalado nunca avançaria — o jogador ficaria esperando para sempre.
            _tempoNoColapso += Time.unscaledDeltaTime;
            if (_tempoNoColapso < atrasoAntesDeOferecer) return;

            Oferecer();
        }

        private void Retomar()
        {
            if (NavegacaoDeCenas.TemRefugioRegistrado)
                NavegacaoDeCenas.RenascerNoUltimoRefugio();
            else
                NavegacaoDeCenas.VoltarParaEntradaDoDeserto();
        }

        private void Oferecer()
        {
            _oferecendo = true;

            // O rótulo diz para onde o botão leva de verdade. "Último refúgio" para quem nunca
            // achou um seria promessa falsa, e o jogador só descobriria ao clicar.
            if (rotuloRetomar != null)
            {
                rotuloRetomar.text = NavegacaoDeCenas.TemRefugioRegistrado
                    ? "Despertar no último refúgio"
                    : "Voltar à entrada do Deserto";
            }

            if (grupoDeOpcoes != null) grupoDeOpcoes.SetActive(true);
        }

        /// <summary>Zera o relógio e esconde as opções — chamado a cada nova morte.</summary>
        private void Esconder()
        {
            _tempoNoColapso = 0f;
            _oferecendo = false;

            if (grupoDeOpcoes != null) grupoDeOpcoes.SetActive(false);
        }
    }
}
