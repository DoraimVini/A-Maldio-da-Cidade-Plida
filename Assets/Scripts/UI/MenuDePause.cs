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

        /// <summary>Volta ao gameplay. O <c>GameManager</c> cuida de descongelar o tempo.</summary>
        public void Retomar()
        {
            var gm = GameManager.Instance;
            if (gm?.StateMachine == null) return;

            gm.StateMachine.TryTransition(GameState.Gameplay);
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
