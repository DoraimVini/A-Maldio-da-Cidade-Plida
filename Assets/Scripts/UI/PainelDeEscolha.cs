using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using FavelaAmarela.Core.Dialogo;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.Interaction;

namespace FavelaAmarela.Runtime.UI
{
    /// <summary>
    /// Caixa de escolha de diálogo ramificado: mostra opções, deixa o jogador navegar com
    /// o eixo vertical do movimento e confirmar com o botão de interação (E) — o mesmo
    /// controle já usado para avançar falas, então não introduz um botão novo.
    ///
    /// <para>Primeiro uso: a conversa com o Abdul (lutar × concordar), mas é genérico —
    /// qualquer diálogo futuro com ramificação chama <see cref="Mostrar"/>.</para>
    ///
    /// <para>A lógica de cursor é o POCO <see cref="NavegadorDeOpcoes"/>; este componente
    /// só lê input, desenha o destaque (prefixo <c>"> "</c> na opção atual) e trava o
    /// movimento do Damião enquanto está aberto (senão W/S andaria o personagem junto
    /// com a navegação).</para>
    /// </summary>
    [AddComponentMenu("FavelaAmarela/UI/Painel de Escolha")]
    public sealed class PainelDeEscolha : MonoBehaviour
    {
        [Header("Referências")]
        [Tooltip("Raiz do painel — ligada/desligada conforme há escolha ativa.")]
        [SerializeField] private GameObject raiz;

        [Tooltip("Texto onde as opções são escritas, uma por linha.")]
        [SerializeField] private Text texto;

        [Tooltip("PlayerInput de onde ler Move (navegar) e Interact (confirmar).")]
        [SerializeField] private PlayerInput playerInput;

        [Tooltip("Movimento do Damião — travado enquanto o painel está aberto.")]
        [SerializeField] private PlayerMovement movimentoDoJogador;

        [Tooltip("Detector de interação do Damião — travado enquanto o painel está aberto. " +
                 "Sem isto, o mesmo aperto de E que confirma uma opção também reabre a " +
                 "conversa com o NPC ainda ao alcance, resetando o painel para o índice 0 " +
                 "e auto-confirmando a opção errada antes do jogador poder navegar.")]
        [SerializeField] private DetectorDeInteracao detectorDeInteracao;

        [Header("Navegação")]
        [Tooltip("Segundos de espera entre um movimento de cursor e o próximo (evita pular 3 opções num único empurrão do stick).")]
        [SerializeField] private float intervaloEntreMovimentos = 0.18f;

        private InputAction _moveAction;
        private InputAction _interactAction;

        private OpcaoDeDialogo[] _opcoes;
        private NavegadorDeOpcoes _navegador;
        private Action<int> _aoConfirmar;
        private float _timerMovimento;
        private bool _aberto;

        private void Awake()
        {
            if (raiz == null)
                Debug.LogError("[PainelDeEscolha] 'Raiz' não atribuída — o painel nunca aparecerá.", this);
            if (texto == null)
                Debug.LogError("[PainelDeEscolha] 'Texto' não atribuído — nada será escrito.", this);
            if (playerInput == null)
                Debug.LogError("[PainelDeEscolha] PlayerInput não atribuído — a escolha não responde a input.", this);
            if (movimentoDoJogador == null)
                Debug.LogWarning("[PainelDeEscolha] PlayerMovement não atribuído — o Damião pode " +
                                 "andar enquanto o jogador navega as opções.", this);

            if (playerInput != null)
            {
                _moveAction = playerInput.actions.FindAction("Move");
                _interactAction = playerInput.actions.FindAction("Interact");
            }

            Esconder();
        }

        /// <summary>Se há uma escolha aberta aguardando confirmação.</summary>
        public bool Aberto => _aberto;

        /// <summary>
        /// Abre o painel com as opções dadas. <paramref name="aoConfirmar"/> recebe o
        /// <see cref="OpcaoDeDialogo.Id"/> da opção escolhida quando o jogador confirma.
        /// </summary>
        public void Mostrar(OpcaoDeDialogo[] opcoes, Action<int> aoConfirmar)
        {
            if (opcoes == null || opcoes.Length == 0)
            {
                Debug.LogError("[PainelDeEscolha] Mostrar chamado sem opções.", this);
                return;
            }

            _opcoes = opcoes;
            _navegador = new NavegadorDeOpcoes(opcoes.Length);
            _aoConfirmar = aoConfirmar;
            _aberto = true;
            _timerMovimento = 0f;

            if (movimentoDoJogador != null) movimentoDoJogador.MovimentoBloqueado = true;
            if (detectorDeInteracao != null) detectorDeInteracao.Bloqueado = true;
            if (raiz != null) raiz.SetActive(true);
            RenderizarOpcoes();
        }

        private void Update()
        {
            if (!_aberto) return;

            _timerMovimento -= Time.unscaledDeltaTime;

            if (_moveAction != null && _timerMovimento <= 0f)
            {
                float vertical = _moveAction.ReadValue<Vector2>().y;
                if (vertical > 0.5f)
                {
                    _navegador.Retroceder(); // "cima" destaca a opção anterior na lista
                    RenderizarOpcoes();
                    _timerMovimento = intervaloEntreMovimentos;
                }
                else if (vertical < -0.5f)
                {
                    _navegador.Avancar();
                    RenderizarOpcoes();
                    _timerMovimento = intervaloEntreMovimentos;
                }
            }

            if (_interactAction != null && _interactAction.WasPressedThisFrame())
                Confirmar();
        }

        private void Confirmar()
        {
            int id = _opcoes[_navegador.IndiceAtual].Id;
            var callback = _aoConfirmar;

            // Esconde a UI e marca como fechado, mas NÃO libera movimento/interação ainda
            // — o callback pode abrir um novo painel na mesma call stack (ex.: o recital da
            // Cassilda encadeia estrofe 3 -> 4). Liberar cedo demais reabre, no mesmo frame,
            // a corrida que este bloqueio existe para evitar: o aperto de E que confirmou
            // esta escolha teria uma segunda chance de vazar para o DetectorDeInteracao
            // antes do novo painel assumir o controle.
            _aberto = false;
            _opcoes = null;
            _navegador = null;
            _aoConfirmar = null;
            if (raiz != null) raiz.SetActive(false);

            callback?.Invoke(id);

            // Só libera se nada reabriu o painel durante o callback — Mostrar() teria
            // religado _aberto (e os dois bloqueios) de novo.
            if (!_aberto)
            {
                if (movimentoDoJogador != null) movimentoDoJogador.MovimentoBloqueado = false;
                if (detectorDeInteracao != null) detectorDeInteracao.Bloqueado = false;
            }
        }

        private void RenderizarOpcoes()
        {
            if (texto == null || _opcoes == null) return;

            var linhas = new System.Text.StringBuilder();
            for (int i = 0; i < _opcoes.Length; i++)
            {
                linhas.Append(i == _navegador.IndiceAtual ? "> " : "   ");
                linhas.AppendLine(_opcoes[i].Texto);
            }
            texto.text = linhas.ToString();
        }

        private void Esconder()
        {
            _aberto = false;
            _opcoes = null;
            _navegador = null;
            _aoConfirmar = null;

            if (movimentoDoJogador != null) movimentoDoJogador.MovimentoBloqueado = false;
            if (detectorDeInteracao != null) detectorDeInteracao.Bloqueado = false;
            if (raiz != null) raiz.SetActive(false);
        }
    }
}
