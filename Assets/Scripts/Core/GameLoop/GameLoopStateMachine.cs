using System;

namespace FavelaAmarela.Core.GameLoop
{
    public enum GameState
    {
        Menu,
        Gameplay,
        Pausado,
        Colapso,
        Vitoria
    }

    /// <summary>
    /// Máquina de estados pura para o ciclo principal do jogo.
    /// Define as transições válidas e notifica observadores quando o estado muda.
    /// </summary>
    public sealed class GameLoopStateMachine
    {
        public GameState CurrentState { get; private set; }

        /// <summary>
        /// Disparado quando ocorre uma transição de estado.
        /// (EstadoAnterior, EstadoAtual)
        /// </summary>
        public event Action<GameState, GameState> OnStateChanged;

        public GameLoopStateMachine(GameState initialState = GameState.Menu)
        {
            CurrentState = initialState;
        }

        public bool TryTransition(GameState alvo)
        {
            if (CurrentState == alvo)
                return false;

            if (CanTransition(CurrentState, alvo))
            {
                var anterior = CurrentState;
                CurrentState = alvo;
                OnStateChanged?.Invoke(anterior, CurrentState);
                return true;
            }

            return false;
        }

        private bool CanTransition(GameState de, GameState para)
        {
            return de switch
            {
                GameState.Menu => para == GameState.Gameplay,
                GameState.Gameplay => para == GameState.Pausado || para == GameState.Colapso || para == GameState.Vitoria,
                GameState.Pausado => para == GameState.Gameplay || para == GameState.Menu,
                GameState.Colapso => para == GameState.Menu,
                GameState.Vitoria => para == GameState.Menu,
                _ => false,
            };
        }
    }
}
