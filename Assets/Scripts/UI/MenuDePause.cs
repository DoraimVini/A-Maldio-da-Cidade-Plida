using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Core.GameLoop;
using FavelaAmarela.Runtime.GameLoop;

namespace FavelaAmarela.Runtime.UI
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). O menu de <b>pause</b> (Esc), sobreposto ao jogo vivo.
    ///
    /// <para>Diferente do <see cref="MenuPrincipal"/>, que vive em cena própria, o pause é
    /// overlay <b>de propósito</b>: ele existe para interromper uma partida em andamento, e o
    /// mundo congelado por trás é parte da informação — o jogador vê onde estava.</para>
    ///
    /// <para><b>Botões previstos e ainda não construídos</b> (decisão do Vini, 2026-08-11:
    /// registrar agora, implementar depois):</para>
    /// <list type="bullet">
    ///   <item><b>Opções</b> — áudio, controles, vídeo.</item>
    ///   <item><b>Enciclopédia</b> — bestiário e lore desbloqueados durante a peregrinação.</item>
    ///   <item><b>Voltar ao menu principal</b> — hoje só dá para sair do jogo inteiro.</item>
    /// </list>
    /// <para>Nenhum desses foi criado: botão morto ensina o jogador a desconfiar da interface.
    /// Quando forem construídos, entram aqui e no <c>MontarTelasDeFluxo</c>.</para>
    /// </summary>
    [AddComponentMenu("FavelaAmarela/UI/Menu de Pause")]
    public sealed class MenuDePause : MonoBehaviour
    {
        [Header("Botões")]
        [Tooltip("Volta ao jogo (mesma coisa que apertar Esc de novo). [ASSET]")]
        [SerializeField] private Button botaoContinuar;

        [Tooltip("Fecha o jogo. [ASSET]")]
        [SerializeField] private Button botaoSair;

        private void Awake()
        {
            if (botaoContinuar != null) botaoContinuar.onClick.AddListener(Retomar);
            if (botaoSair != null) botaoSair.onClick.AddListener(Sair);
        }

        private FavelaAmarela.Core.GameLoop.GameLoopStateMachine _maquina;

        /// <summary>
        /// Liga à máquina de estados da cena. Chamado pelo <c>GameLoopBootstrap</c>.
        ///
        /// <para><b>Fase 5, 2026-08-18:</b> substitui a busca por
        /// <c>GameManager.Instance.StateMachine</c>.</para>
        /// </summary>
        public void Bind(FavelaAmarela.Core.GameLoop.GameLoopStateMachine maquina)
        {
            if (maquina == null)
            {
                Debug.LogError("[MenuDePause] Bind recebeu máquina nula — não dá para despausar.",
                               this);
                return;
            }

            _maquina = maquina;
        }

        /// <summary>Volta ao gameplay. O <c>GameStatePresenter</c> cuida de descongelar o tempo.</summary>
        public void Retomar()
        {
            if (_maquina == null) return;

            _maquina.TryTransition(GameState.Gameplay);
        }

        private void Sair()
        {
            // Captura antes de fechar: sair pelo menu não pode custar o progresso da sessão.
            // Quem grava em disco de verdade é o Refúgio de Luz — aqui só garantimos que o
            // registro em memória esteja íntegro caso a gravação esteja ligada.
            Persistencia.GerenciadorDeSave.Instancia?.CapturarTudo();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
