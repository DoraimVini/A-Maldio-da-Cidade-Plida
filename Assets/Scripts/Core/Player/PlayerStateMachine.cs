using System;

namespace FavelaAmarela.Core.Player
{
    /// <summary>
    /// Máquina de estados pura (POCO) que centraliza as ações exclusivas de Damião
    /// (Esquiva/Salto/Ataque) numa única fonte de verdade. Antes, cada ação vivia
    /// duplicada — flag no bridge (<c>IsLeaping</c> etc.) + flag-espelho no
    /// <c>PlayerMovement</c> — com dois <c>Invoke(EndX)</c> paralelos que podiam
    /// dessincronizar. Esta FSM elimina a duplicação e é o único ponto que garante a
    /// exclusão mútua entre as ações. Segue o padrão canônico de <c>CultistaFSM</c>.
    /// </summary>
    public sealed class PlayerStateMachine
    {
        /// <summary>Ação exclusiva atual. <see cref="PlayerState.Livre"/> = nenhuma.</summary>
        public PlayerState CurrentState { get; private set; }

        /// <summary>Tempo acumulado (s) no estado atual; zera a cada transição.</summary>
        public float TimeInState { get; private set; }

        /// <summary>Tempo (s) restante até a ação atual terminar e voltar a Livre. 0 quando Livre.</summary>
        public float TempoRestante { get; private set; }

        /// <summary>true quando nenhuma ação exclusiva está em curso.</summary>
        public bool EstaLivre => CurrentState == PlayerState.Livre;

        /// <summary>Disparado apenas numa transição real de estado, com (anterior, novo).</summary>
        public event Action<PlayerState, PlayerState> OnStateChanged;

        public PlayerStateMachine(PlayerState initialState = PlayerState.Livre)
        {
            CurrentState = initialState;
        }

        /// <summary>
        /// Tenta iniciar uma ação exclusiva por <paramref name="duracao"/> segundos.
        /// Só entra se estiver Livre — é aqui que a exclusão mútua é garantida (substitui
        /// o early-return frágil do antigo <c>PlayerMovement</c> e as guardas locais dos
        /// bridges). Retorna false se já houver ação em curso ou se pedirem Livre.
        /// </summary>
        public bool TryEntrarAcao(PlayerState acao, float duracao)
        {
            if (acao == PlayerState.Livre) return false;
            if (CurrentState != PlayerState.Livre) return false;

            TempoRestante = duracao;
            ChangeState(acao);
            return true;
        }

        /// <summary>
        /// Avança o tempo interno. Ao esgotar a duração da ação atual, volta a Livre
        /// automaticamente — substitui os <c>Invoke(EndX)</c> paralelos do modelo antigo.
        /// </summary>
        public void Tick(float dt)
        {
            TimeInState += dt;

            if (CurrentState == PlayerState.Livre) return;

            TempoRestante -= dt;
            if (TempoRestante <= 0f)
            {
                TempoRestante = 0f;
                ChangeState(PlayerState.Livre);
            }
        }

        /// <summary>
        /// Interrompe imediatamente a ação atual e volta a Livre (ex.: futuro
        /// "tomar dano cancela a esquiva"). Sem efeito se já estiver Livre.
        /// </summary>
        public void ForcarLivre()
        {
            if (CurrentState == PlayerState.Livre) return;
            TempoRestante = 0f;
            ChangeState(PlayerState.Livre);
        }

        private void ChangeState(PlayerState novo)
        {
            if (CurrentState == novo) return;
            var old = CurrentState;
            CurrentState = novo;
            TimeInState = 0f;
            OnStateChanged?.Invoke(old, novo);
        }
    }
}
