using System;

namespace FavelaAmarela.Core.Enemies
{
    /// <summary>
    /// Estados possíveis de um Espectro durante a manifestação roteirizada
    /// (ex.: cutscene de cerco na Praça do Cerco, Zona 4).
    /// </summary>
    public enum EspectroState
    {
        Latente,
        Manifestando,
        Cercando
    }

    /// <summary>
    /// Máquina de estados pura (POCO) para o Espectro. Diferente da
    /// <see cref="CultistaFSM"/>, que reage a estímulos sonoros, o Espectro segue
    /// uma sequência linear e roteirizada de manifestação — sem transições
    /// espontâneas nem regra de tempo. As transições inválidas (fora de ordem)
    /// são rejeitadas silenciosamente, mesmo padrão de <c>GameLoopStateMachine.TryTransition</c>.
    /// </summary>
    public sealed class EspectroFSM
    {
        public EspectroState CurrentState { get; private set; }

        /// <summary>Disparado quando uma transição válida acontece.</summary>
        public event Action<EspectroState, EspectroState> OnStateChanged;

        public EspectroFSM(EspectroState initialState = EspectroState.Latente)
        {
            CurrentState = initialState;
        }

        /// <summary>
        /// Tenta avançar para <paramref name="alvo"/>. Retorna <c>false</c> sem
        /// nenhum efeito colateral se a transição não for permitida.
        /// </summary>
        public bool TryTransition(EspectroState alvo)
        {
            if (!EhTransicaoValida(CurrentState, alvo)) return false;

            var anterior = CurrentState;
            CurrentState = alvo;
            OnStateChanged?.Invoke(anterior, alvo);
            return true;
        }

        private static bool EhTransicaoValida(EspectroState atual, EspectroState alvo)
        {
            return (atual, alvo) switch
            {
                (EspectroState.Latente, EspectroState.Manifestando) => true,
                (EspectroState.Manifestando, EspectroState.Cercando) => true,
                _ => false,
            };
        }
    }
}
