using UnityEngine;
using FavelaAmarela.Core.GameLoop;

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// Camada Runtime. Aplica a <b>apresentação</b> de um estado do
    /// <see cref="GameLoopStateMachine"/>: congela ou solta o tempo e liga/desliga as telas de
    /// pausa, transição e gameplay.
    ///
    /// <para><b>Por que não virou um POCO novo:</b> um documento de refatoração pediu um
    /// <c>GameStateController</c> em <c>Core/</c> que controlasse <c>Time.timeScale</c> — mas
    /// <c>Time</c> é <c>UnityEngine</c>, proibido no Core (<c>Core/CLAUDE.md</c>), e a máquina de
    /// estados <b>já existe</b> como POCO testado (<see cref="GameLoopStateMachine"/>, 5 estados,
    /// validação de transição e 8 testes). O que faltava não era uma máquina nova: era o
    /// adaptador que traduz a regra <see cref="GameLoopStateMachine.MundoCongelado"/> em efeito
    /// visível. Este componente é esse adaptador.</para>
    ///
    /// <para>Extraído do <c>GameManager.HandleStateChanged</c> em 2026-08-13.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/GameLoop/Apresentação de Estado")]
    public sealed class GameStatePresenter : MonoBehaviour
    {
        [Header("Telas por estado (opcionais)")]
        [Tooltip("Ligada em Pausado. [CENA]")]
        [SerializeField] private GameObject telaPause;

        /// <summary>
        /// Recebe o overlay de pause do <c>GameLoopBootstrap</c>.
        ///
        /// <para>Existe porque a tela migrou para o prefab persistente do HUD em 2026-08-22:
        /// uma referência serializada na cena apontaria para um objeto que vive fora dela, e a
        /// Unity não resolve isso. A entrega passou a ser em runtime.</para>
        /// </summary>
        public void DefinirTelaPause(GameObject tela) => telaPause = tela;

        // Não há `telaTransicaoDeFase` nem `gameplayRoot` aqui de propósito. O `GameManager`
        // antigo tinha os dois, e ambos estavam em `fileID: 0` nas cinco cenas — eram da cena
        // antiga, de antes dela virar a Tumba, e não têm mais uso no jogo (confirmado pelo Vini
        // em 2026-08-13). O `SetActive` deles nunca rodou; trazê-los para cá seria migrar código
        // morto para dentro do componente novo.
        //
        // O estado `TransicaoDeFase` continua existindo e congelando o mundo — o que não existe
        // é uma tela para ele.

        private GameLoopStateMachine _maquina;

        /// <summary>
        /// Conecta à máquina de estados e <b>aplica o estado corrente na hora</b>.
        ///
        /// <para>Aplicar no bind não é redundância: <c>OnStateChanged</c> só dispara em
        /// transição, e no arranque não há transição nenhuma. Sem isto, o estado inicial ficaria
        /// declarado na máquina mas invisível na tela — foi exatamente o limbo que o
        /// <c>GameManager</c> corrigia chamando o handler à mão no fim do <c>Awake</c>.</para>
        /// </summary>
        public void Bind(GameLoopStateMachine maquina)
        {
            if (maquina == null)
            {
                Debug.LogError("[GameStatePresenter] Bind recebeu máquina nula — as telas e o " +
                               "tempo não vão reagir a mudança de estado.", this);
                return;
            }

            Desinscrever();

            _maquina = maquina;
            _maquina.OnStateChanged += HandleStateChanged;

            Aplicar(_maquina.CurrentState);
        }

        private void OnDestroy() => Desinscrever();

        private void Desinscrever()
        {
            if (_maquina != null) _maquina.OnStateChanged -= HandleStateChanged;
            _maquina = null;
        }

        private void HandleStateChanged(GameState anterior, GameState atual) => Aplicar(atual);

        private void Aplicar(GameState estado)
        {
            // A regra de congelar mora no POCO; aqui só se traduz em Time.timeScale.
            Time.timeScale = _maquina != null && _maquina.MundoCongelado ? 0f : 1f;

            if (telaPause != null) telaPause.SetActive(estado == GameState.Pausado);
        }
    }
}
