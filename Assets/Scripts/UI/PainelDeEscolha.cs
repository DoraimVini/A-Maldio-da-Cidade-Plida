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

        /// <summary>
        /// O <c>Text</c> onde as opções são escritas. Exposto para as ferramentas de Editor —
        /// ver a nota em <see cref="TutorialHintUI.TextoDeSaida"/> sobre por que chegar nele por
        /// <c>SerializedObject</c> devolvia o objeto errado dentro de um prefab carregado.
        /// </summary>
        public Text TextoDeSaida => texto;

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
            // SEM AVISO por campo vazio (2026-09-02). Desde que este painel mora no HUD
            // persistente, playerInput e movimentoDoJogador NASCEM vazios por construção --
            // prefab-asset não referencia objeto de cena --, e quem os preenche é o
            // GameLoopBootstrap depois do Awake.
            //
            // Eu tinha trocado o erro por aviso, e o Vini mandou o log da sessão: os dois
            // disparavam em TODA inicialização. Aviso no caso normal tem a mesma doença do erro
            // no caso normal -- ensina a ignorar o log, que é o único canal de runtime que
            // temos. Quem avisa agora é o Mostrar(), onde a falta DÓI de verdade.

            if (playerInput != null)
            {
                _moveAction = playerInput.actions.FindAction("Move");
                _interactAction = playerInput.actions.FindAction("Interact");
            }

            Esconder();
        }

        /// <summary>
        /// Instância corrente, para quem não pode receber a referência por Inspector.
        ///
        /// <para><b>Por que existe (2026-09-02).</b> Este painel vivia em <b>duas cenas das
        /// seis</b>. O <c>CassildaNPC.cs:284</c> pula a ramificação <b>em silêncio</b> quando
        /// ele falta — então qualquer NPC de escolha posto no Deserto, nos Portões ou no
        /// Castelo perderia a conversa sem ninguém notar. Mesmo contrato de
        /// <c>TutorialHintUI.Instancia</c> e <c>HUDController.Instancia</c>.</para>
        /// </summary>
        public static PainelDeEscolha Instancia { get; private set; }

        private void OnEnable()
        {
            if (Instancia == null) Instancia = this;
        }

        private void OnDisable()
        {
            if (Instancia == this) Instancia = null;
        }

        /// <summary>
        /// Liga o painel ao jogador. Chamado pelo <c>GameLoopBootstrap</c>, pelo mesmo motivo do
        /// <c>PromptDeInteracao</c> e do <c>PainelDeFicha</c>: morando no HUD persistente, que é
        /// um prefab-asset, ele <b>não pode</b> referenciar objeto de cena por Inspector.
        /// </summary>
        public void Bind(PlayerInput input, PlayerMovement movimento,
                         DetectorDeInteracao detector)
        {
            playerInput = input;
            movimentoDoJogador = movimento;
            detectorDeInteracao = detector;

            if (playerInput != null && playerInput.actions != null)
            {
                _moveAction = playerInput.actions.FindAction("Move");
                _interactAction = playerInput.actions.FindAction("Interact");
            }
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

            // AQUI a falta dói: alguém pediu para o jogador escolher, e sem PlayerInput ele
            // vê as opções e não consegue navegar. Um menu que abre e não responde é pior que
            // menu nenhum.
            if (playerInput == null)
                Debug.LogError("[PainelDeEscolha] Abrindo escolha SEM PlayerInput — as opções " +
                               "aparecem e o jogador não consegue navegar nem confirmar. Quem " +
                               "deveria ter ligado é o GameLoopBootstrap, no Bind.", this);

            else if (movimentoDoJogador == null)
                Debug.LogWarning("[PainelDeEscolha] Abrindo escolha sem PlayerMovement — o " +
                                 "Damião pode andar enquanto o jogador navega as opções.", this);

            // Reabrir por cima de um painel ja aberto tomaria uma SEGUNDA camada para um unico
            // Devolver depois. O caso legitimo (o recital da Cassilda encadeando estrofes) passa
            // pelo Confirmar(), que zera _aberto antes do callback -- ali isto e falso e a camada
            // nova e tomada de verdade.
            bool jaEstavaAberto = _aberto;

            _opcoes = opcoes;
            _navegador = new NavegadorDeOpcoes(opcoes.Length);
            _aoConfirmar = aoConfirmar;
            _aberto = true;
            _timerMovimento = 0f;

            // O árbitro, além das duas travas finas que este painel já ligava à mão desde
            // agosto. Elas continuam: `MovimentoBloqueado` e `Bloqueado` foram o ÚNICO bloqueio
            // de input que este projeto teve por meses, e são o que impede o Damião de andar
            // enquanto se navega as opções. O árbitro é o que impede tudo o mais -- Artefato,
            // esquiva, teclas 1-8, clique -- que aquelas duas nunca cobriram.
            if (!jaEstavaAberto)
                FavelaAmarela.Runtime.Entrada.ArbitroDeFoco.Tomar(
                    FavelaAmarela.Core.Entrada.CamadaDeEntrada.PainelModal);

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

            // A camada que o Mostrar() tomou volta SEMPRE, e exatamente uma vez -- mesmo que o
            // callback tenha aberto um painel novo, porque esse novo tomou uma camada PROPRIA.
            //
            // ISTO FALTAVA (2026-09-02, relatado pelo Vini na luta do Abdul: "o boneco não pode
            // mais andar e morre parado"). Confirmar() repete o corpo do Esconder() aqui dentro,
            // de proposito, para poder soltar os bloqueios DEPOIS do callback -- e quando eu
            // acrescentei o arbitro, em agosto, pus o Devolver so no Esconder(). Escolher uma
            // opcao e o caminho NORMAL deste painel, e ele nunca passa pelo Esconder(): cada
            // escolha confirmada vazava uma camada de PainelModal, para sempre. Com o jogo preso
            // em PainelModal, PlayerMovement, DetectorDeInteracao e BarraDeItens desligam todos.
            FavelaAmarela.Runtime.Entrada.ArbitroDeFoco.Devolver(
                FavelaAmarela.Core.Entrada.CamadaDeEntrada.PainelModal);

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

            FavelaAmarela.Runtime.Entrada.ArbitroDeFoco.Devolver(
                FavelaAmarela.Core.Entrada.CamadaDeEntrada.PainelModal);
        }
    }
}
