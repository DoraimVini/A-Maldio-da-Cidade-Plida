using System;

namespace FavelaAmarela.Core.GameLoop
{
    /// <summary>
    /// Estados do ciclo principal do jogo.
    /// </summary>
    public enum GameState
    {
        /// <summary>Tela inicial. Estado padrão ao iniciar o jogo.</summary>
        Menu,
        /// <summary>Jogo em andamento — stealth, IA e Resiliência Mental ativos.</summary>
        Gameplay,
        /// <summary>Jogo pausado, tempo congelado.</summary>
        Pausado,
        /// <summary>Colapso Mental (Resiliência a zero) — fim de jogo diegético, retorna ao Menu.</summary>
        Colapso,
        /// <summary>
        /// Encerramento de uma fase ou dungeon (ex.: derrota de um miniboss num portão de saída).
        /// Não é uma tela de "Vitória": o jogo é um RPG multi-fase, não um roguelike — a única
        /// vitória de verdade é o desfecho da história ao fim da última fase. Este estado só
        /// congela o gameplay para a transição visual antes do próximo trecho.
        /// </summary>
        TransicaoDeFase
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
                GameState.Gameplay => para == GameState.Pausado || para == GameState.Colapso || para == GameState.TransicaoDeFase,
                GameState.Pausado => para == GameState.Gameplay || para == GameState.Menu,
                GameState.Colapso => para == GameState.Menu,
                GameState.TransicaoDeFase => para == GameState.Menu,
                _ => false,
            };
        }
    }
}
