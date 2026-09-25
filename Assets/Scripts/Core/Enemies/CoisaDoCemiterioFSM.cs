using System;
using UnityEngine;

namespace FavelaAmarela.Core.Enemies
{
    /// <summary>
    /// Máquina de estados pura (POCO) para a Coisa do Cemitério (bestiário, item 5).
    /// Reaproveita a mesma fonte de estímulo do <see cref="CultistaFSM"/>
    /// (<see cref="FavelaAmarela.Core.Stealth.SoundBroadcastService"/>), mas nunca fica
    /// "Errante" — está sempre se aproximando, só que de forma imprecisa até um som
    /// revelar a posição exata. Não reage a golpes de arma física (ver
    /// <see cref="FavelaAmarela.Core.Abilities.IArma"/>): a imunidade a combate vem do
    /// resolvedor de golpe (Runtime) simplesmente não reconhecer este componente, não
    /// de nenhuma lógica aqui.
    /// </summary>
    public sealed class CoisaDoCemiterioFSM
    {
        public CoisaDoCemiterioState CurrentState { get; private set; }
        public float TimeInState { get; private set; }
        public float TimeSinceLastStimulus { get; private set; }

        /// <summary>
        /// Última posição conhecida de um estímulo sonoro válido. Null se nenhum
        /// estímulo ainda foi recebido nesta "vida" da criatura.
        /// </summary>
        public Vector2? UltimaOrigemConhecida { get; private set; }

        private readonly float duracaoAlvoPreciso;

        public event Action<CoisaDoCemiterioState, CoisaDoCemiterioState> OnStateChanged;

        public CoisaDoCemiterioFSM(float duracaoAlvoPreciso = 6f)
        {
            CurrentState = CoisaDoCemiterioState.Farejando;
            TimeSinceLastStimulus = 999f;
            this.duracaoAlvoPreciso = duracaoAlvoPreciso;
        }

        public void ReceberEstimuloSonoro(Vector2 origemSom, float distanciaAoJogador, float raioEfetivo)
        {
            if (raioEfetivo <= 0f) return;
            if (distanciaAoJogador > raioEfetivo) return;

            TimeSinceLastStimulus = 0f;
            UltimaOrigemConhecida = origemSom;

            if (CurrentState == CoisaDoCemiterioState.Farejando)
            {
                ChangeState(CoisaDoCemiterioState.AlvoPreciso);
            }
        }

        public void Tick(float dt)
        {
            TimeInState += dt;
            TimeSinceLastStimulus += dt;

            if (CurrentState == CoisaDoCemiterioState.AlvoPreciso && TimeSinceLastStimulus >= duracaoAlvoPreciso)
            {
                ChangeState(CoisaDoCemiterioState.Farejando);
            }
        }

        private void ChangeState(CoisaDoCemiterioState novo)
        {
            if (CurrentState == novo) return;
            var old = CurrentState;
            CurrentState = novo;
            TimeInState = 0f;
            OnStateChanged?.Invoke(old, novo);
        }
    }
}
