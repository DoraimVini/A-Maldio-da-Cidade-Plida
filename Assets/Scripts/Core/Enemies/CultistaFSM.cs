using System;
using UnityEngine;

namespace FavelaAmarela.Core.Enemies
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
        
        /// <summary>
        /// Última posição conhecida de um estímulo válido. Null se nenhum
        /// estímulo ainda foi recebido nesta "vida" do Cultista.
        /// </summary>
        public Vector2? UltimaOrigemConhecida { get; private set; }

        public event Action<CultistaState, CultistaState> OnStateChanged;

        public CultistaFSM(CultistaState initialState = CultistaState.Errante)
        {
            CurrentState = initialState;
            TimeSinceLastStimulus = 999f;
        }

        public void ReceberEstimuloSonoro(Vector2 origemSom, float distanciaAoJogador, float raioEfetivo)
        {
            if (raioEfetivo <= 0f) return;
            if (distanciaAoJogador > raioEfetivo) return;

            TimeSinceLastStimulus = 0f;
            UltimaOrigemConhecida = origemSom;

            if (CurrentState == CultistaState.Errante)
            {
                ChangeState(CultistaState.Alerta);
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
