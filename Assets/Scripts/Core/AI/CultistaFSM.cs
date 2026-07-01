using System;

namespace FavelaAmarela.Core.AI
{
    /// <summary>
    /// Máquina de estados pura para o Cultista.
    /// Define as regras de detecção sonora e transição de estados.
    /// Não possui dependências da Unity (POCO).
    /// </summary>
    public class CultistaFSM
    {
        public CultistaState CurrentState { get; private set; }
        public float TimeInState { get; private set; }
        public float TimeSinceLastStimulus { get; private set; }

        public event Action<CultistaState, CultistaState> OnStateChanged;

        public CultistaFSM(CultistaState initialState = CultistaState.Errante)
        {
            CurrentState = initialState;
            TimeSinceLastStimulus = 999f;
        }

        public void ReceberEstimuloSonoro(float distancia, bool jogadorCorrendo)
        {
            // Regras de detecção (baseado no design doc das Ruínas Pálidas)
            bool detectou = (distancia <= 3f) || 
                            (jogadorCorrendo && distancia <= 14f) || 
                            (!jogadorCorrendo && distancia <= 8f);

            if (detectou)
            {
                TimeSinceLastStimulus = 0f;

                if (CurrentState == CultistaState.Errante)
                {
                    ChangeState(CultistaState.Alerta);
                }
            }
        }

        public void Tick(float dt)
        {
            TimeInState += dt;
            TimeSinceLastStimulus += dt;

            if (CurrentState == CultistaState.Alerta)
            {
                // Volta para Errante se não houver novo estímulo por 4s
                if (TimeSinceLastStimulus >= 4f)
                {
                    ChangeState(CultistaState.Errante);
                }
                // Transiciona para Caça se a pausa telegrafada de 1.5s terminar
                // E houver um estímulo recente (garante que ele não cace o vazio se o jogador parou)
                else if (TimeInState >= 1.5f && TimeSinceLastStimulus <= 1.5f)
                {
                    ChangeState(CultistaState.Caca);
                }
            }
            else if (CurrentState == CultistaState.Caca)
            {
                // Perde o rastro após 5s sem ouvir nada
                if (TimeSinceLastStimulus >= 5f)
                {
                    ChangeState(CultistaState.Errante);
                }
            }
        }

        private void ChangeState(CultistaState novo)
        {
            if (CurrentState == novo) return;
            var old = CurrentState;
            CurrentState = novo;
            TimeInState = 0f;
            OnStateChanged?.Invoke(old, novo);
        }
    }
}
