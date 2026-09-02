using UnityEngine;
using UnityEngine.InputSystem;
using FavelaAmarela.Core.GameLoop;

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// Camada Runtime. Lê o Esc e alterna Gameplay ↔ Pausado. Só isso.
    ///
    /// <para>Virou componente próprio porque era a última coisa que mantinha um <c>Update</c> no
    /// <c>GameManager</c>. Quem <b>lê input</b> e quem <b>apresenta estado</b> são papéis
    /// diferentes: este pede a transição, o <see cref="GameStatePresenter"/> reage a ela. Nenhum
    /// dos dois sabe do outro.</para>
    ///
    /// <para>Extraído do <c>GameManager.Update</c> em 2026-08-14.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/GameLoop/Entrada de Pausa")]
    public sealed class PausaInputHandler : MonoBehaviour
    {
        private GameLoopStateMachine _maquina;

        /// <summary>Conecta à máquina de estados. Sem ela, o Esc não faz nada.</summary>
        public void Bind(GameLoopStateMachine maquina)
        {
            if (maquina == null)
            {
                Debug.LogError("[PausaInputHandler] Bind recebeu máquina nula — o Esc não vai " +
                               "pausar o jogo.", this);
                return;
            }

            _maquina = maquina;
        }

        private void Update()
        {
            if (_maquina == null) return;

            // Keyboard.current é nulo em builds sem teclado (e em alguns testes de PlayMode).
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;

            // Só com o jogo no comando (2026-09-02). Antes, um Esc com a mochila aberta
            // PAUSAVA O JOGO POR BAIXO dela: duas telas empilhadas e dois donos disputando o
            // Time.timeScale. Quem fecha o painel é o próprio painel; a pausa é do jogo.
            //
            // A exceção é despausar: já pausado, o comando é da pausa e o Esc tem de sair.
            bool jogoNoComando = FavelaAmarela.Runtime.Entrada.ArbitroDeFoco.JogoNoComando;
            if (!jogoNoComando && _maquina.CurrentState != GameState.Pausado) return;

            if (_maquina.CurrentState == GameState.Gameplay)
                _maquina.TryTransition(GameState.Pausado);
            else if (_maquina.CurrentState == GameState.Pausado)
                _maquina.TryTransition(GameState.Gameplay);
        }
    }
}
