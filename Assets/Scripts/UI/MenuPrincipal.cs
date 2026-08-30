using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Runtime.GameLoop;
using FavelaAmarela.Runtime.Persistencia;

namespace FavelaAmarela.Runtime.UI
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). A tela inicial, em <b>cena própria</b>:
    /// <b>Continuar</b>, <b>Nova peregrinação</b> e <b>Sair</b>.
    ///
    /// <para><b>Por que cena própria</b> (refactor de 2026-08-11, sugestão do Vini): antes o
    /// menu era um overlay dentro de cada cena de jogo. Isso significava três cópias para
    /// manter, e — pior — carregar o Deserto inteiro (tempestade, inimigos, tilemaps) só para
    /// cobrir tudo com uma tela preta. Também obrigava a congelar o tempo, porque o mundo
    /// ficava vivo por trás. Numa cena só, não há mundo atrás: nada a congelar, nada a
    /// esconder, e o menu abre instantâneo.</para>
    ///
    /// <para>O <b>pause</b> continua sendo overlay dentro da cena de jogo — ali sobrepor o
    /// mundo vivo é justamente o comportamento certo. Ver <see cref="MenuDePause"/>.</para>
    /// </summary>
    [AddComponentMenu("FavelaAmarela/UI/Menu Principal")]
    public sealed class MenuPrincipal : MonoBehaviour
    {
        [Header("Botões")]
        [Tooltip("Retoma na cena onde a partida parou. Escondido se não houver save. [ASSET]")]
        [SerializeField] private Button botaoContinuar;

        [Tooltip("Começa do zero, apagando o progresso. [ASSET]")]
        [SerializeField] private Button botaoNovaPartida;

        [Tooltip("Fecha o jogo. [ASSET]")]
        [SerializeField] private Button botaoSair;


        [Tooltip("Abre a tela de Opções (volume, tela cheia, sincronização vertical). [ASSET]")]
        [SerializeField] private Button botaoDeOpcoes;
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

            // A tela de Opções é persistente e vive fora deste menu: aqui só se pede que ela
            // apareça. Sem botão ligado, o jogador não tem como chegar nela -- que é como o
            // controle de volume não existiu até 2026-08-29.
            if (botaoDeOpcoes != null)
                botaoDeOpcoes.onClick.AddListener(PainelDeOpcoes.AbrirSeExistir);

            if (botaoConfirmar != null) botaoConfirmar.onClick.AddListener(NovaPartida);
            if (botaoCancelar != null) botaoCancelar.onClick.AddListener(FecharConfirmacao);

            FecharConfirmacao();

            // O menu pode ser alcançado vindo de um jogo pausado, que deixou o tempo parado.
            // Sem isto, a partida seguinte nasceria congelada.
            Time.timeScale = 1f;
        }

        private void OnEnable() => AtualizarBotoes();

        /// <summary>
        /// Esconde "Continuar" quando não há para onde continuar. Um botão que não faz nada
        /// ensina o jogador a desconfiar do resto da interface.
        /// </summary>
        private void AtualizarBotoes()
        {
            if (botaoContinuar == null) return;

            botaoContinuar.gameObject.SetActive(TemPartidaSalva());
        }

        private static bool TemPartidaSalva()
        {
            var gerenciador = GerenciadorDeSave.Instancia;
            return gerenciador != null
                   && gerenciador.ExisteSaveEmDisco
                   && !string.IsNullOrEmpty(gerenciador.CenaSalva);
        }

        private void Continuar() => NavegacaoDeCenas.Continuar();

        private void PedirConfirmacao()
        {
            // Apagar progresso é irreversível: confirma antes, sempre. Só que confirmar sem
            // haver o que apagar é atrito à toa — quem nunca jogou vai direto.
            if (painelDeConfirmacao != null && TemPartidaSalva())
                painelDeConfirmacao.SetActive(true);
            else
                NovaPartida();
        }

        private void FecharConfirmacao()
        {
            if (painelDeConfirmacao != null) painelDeConfirmacao.SetActive(false);
        }

        private void NovaPartida()
        {
            FecharConfirmacao();
            NavegacaoDeCenas.ComecarNovaPeregrinacao();
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
