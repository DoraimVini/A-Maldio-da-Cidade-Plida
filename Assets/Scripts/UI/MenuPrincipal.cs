using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Core.GameLoop;
using FavelaAmarela.Runtime.GameLoop;
using FavelaAmarela.Runtime.Persistencia;

namespace FavelaAmarela.Runtime.UI
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). A tela inicial: <b>Continuar</b>, <b>Nova peregrinação</b>
    /// e <b>Sair</b>.
    ///
    /// <para>O <c>GameState.Menu</c> existia desde sempre na máquina de estados, mas nenhuma
    /// tela o desenhava — o jogo entrava direto em <c>Gameplay</c>. Sem isto não havia como
    /// começar, recomeçar nem sair.</para>
    ///
    /// <para><b>"Continuar" só aparece se houver save.</b> Um botão morto que não faz nada
    /// ensina o jogador a desconfiar da interface.</para>
    /// </summary>
    [AddComponentMenu("FavelaAmarela/UI/Menu Principal")]
    public sealed class MenuPrincipal : MonoBehaviour
    {
        [Header("Botões")]
        [Tooltip("Retoma do último Refúgio de Luz. Escondido se não houver save. [ASSET]")]
        [SerializeField] private Button botaoContinuar;

        [Tooltip("Começa do zero, apagando o progresso. [ASSET]")]
        [SerializeField] private Button botaoNovaPartida;

        [Tooltip("Fecha o jogo. [ASSET]")]
        [SerializeField] private Button botaoSair;

        [Header("Confirmação")]
        [Tooltip("Painel de confirmação de 'Nova peregrinação'. [ASSET]")]
        [SerializeField] private GameObject painelDeConfirmacao;

        [SerializeField] private Button botaoConfirmar;
        [SerializeField] private Button botaoCancelar;

        private void Awake()
        {
            if (botaoContinuar != null) botaoContinuar.onClick.AddListener(Continuar);
            if (botaoNovaPartida != null) botaoNovaPartida.onClick.AddListener(PedirConfirmacao);
            if (botaoSair != null) botaoSair.onClick.AddListener(Sair);

            if (botaoConfirmar != null) botaoConfirmar.onClick.AddListener(NovaPartida);
            if (botaoCancelar != null) botaoCancelar.onClick.AddListener(FecharConfirmacao);

            FecharConfirmacao();
        }

        private void OnEnable() => AtualizarBotoes();

        /// <summary>
        /// Esconde "Continuar" quando não há nada a continuar. Um botão que não faz nada
        /// ensina o jogador a desconfiar do resto da interface.
        /// </summary>
        private void AtualizarBotoes()
        {
            if (botaoContinuar == null) return;

            var gerenciador = GerenciadorDeSave.Instancia;
            bool temSave = gerenciador != null && gerenciador.ExisteSaveEmDisco;

            botaoContinuar.gameObject.SetActive(temSave);
        }

        private void Continuar()
        {
            GerenciadorDeSave.Instancia?.AplicarTudo();
            GameManager.Instance?.StateMachine?.TryTransition(GameState.Gameplay);
        }

        private void PedirConfirmacao()
        {
            // Apagar progresso é irreversível: confirma antes, sempre.
            if (painelDeConfirmacao != null) painelDeConfirmacao.SetActive(true);
            else NovaPartida();
        }

        private void FecharConfirmacao()
        {
            if (painelDeConfirmacao != null) painelDeConfirmacao.SetActive(false);
        }

        private void NovaPartida()
        {
            FecharConfirmacao();

            GerenciadorDeSave.Instancia?.ApagarSave();
            GameManager.Instance?.StateMachine?.TryTransition(GameState.Gameplay);
        }

        private void Sair()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
