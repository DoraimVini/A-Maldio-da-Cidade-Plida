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

        /// <summary>
        /// Se o alvo (Damião) está ao alcance de golpe corpo-a-corpo. Alimentado pelo
        /// adaptador Runtime a cada tick (proximidade física por <c>OverlapCircle</c> na
        /// layer Player — <b>não é visão</b>, é toque; coerente com a percepção só-som).
        /// </summary>
        public bool AlvoAoAlcance { get; private set; }

        private float _duracaoAtordoamento;

        // Cadência do estado Atacar: intervalo entre golpes desferidos no alvo.
        private readonly float _cadenciaDeAtaque;
        private float _timerAtaque;

        public event Action<CultistaState, CultistaState> OnStateChanged;

        /// <summary>
        /// Disparado a cada golpe corpo-a-corpo desferido no estado <see cref="CultistaState.Atacar"/>.
        /// O Runtime traduz em dano na Vitalidade do Damião (aplicando a mitigação por defesa).
        /// </summary>
        public event Action OnGolpeDesferido;

        /// <param name="initialState">Estado inicial da FSM.</param>
        /// <param name="cadenciaDeAtaque">Segundos entre golpes no estado Atacar (default 1,2 s).</param>
        public CultistaFSM(CultistaState initialState = CultistaState.Errante, float cadenciaDeAtaque = 1.2f)
        {
            CurrentState = initialState;
            TimeSinceLastStimulus = 999f;
            _cadenciaDeAtaque = cadenciaDeAtaque > 0f ? cadenciaDeAtaque : 1.2f;
        }

        /// <summary>
        /// Atualiza se o alvo está ao alcance de golpe (chamado pelo Runtime a cada tick).
        /// É o gatilho de proximidade que leva o Cultista de Caça para Atacar e o segura
        /// atacando enquanto o Damião não sai do corpo-a-corpo.
        /// </summary>
        public void AtualizarAlcanceDoAlvo(bool aoAlcance) => AlvoAoAlcance = aoAlcance;

        public void ReceberEstimuloSonoro(Vector2 origemSom, float distanciaAoJogador, float raioEfetivo)
        {
            if (CurrentState == CultistaState.Atordoado) return;
            if (raioEfetivo <= 0f) return;
            if (distanciaAoJogador > raioEfetivo) return;

            TimeSinceLastStimulus = 0f;
            UltimaOrigemConhecida = origemSom;

            if (CurrentState == CultistaState.Errante)
            {
                ChangeState(CultistaState.Alerta);
            }
        }

        /// <summary>
        /// Interrompe o Cultista imediatamente, qualquer que seja o estado atual
        /// (ex.: um golpe de arma física — ver <see cref="FavelaAmarela.Core.Abilities.IArma"/> —
        /// que rolou atordoamento). Depois de <paramref name="duracaoSegundos"/>,
        /// volta para Errante — atordoado o suficiente para perder o rastro.
        /// </summary>
        public void AtordoarPor(float duracaoSegundos)
        {
            if (duracaoSegundos <= 0f) return;
            _duracaoAtordoamento = duracaoSegundos;
            ChangeState(CultistaState.Atordoado);
        }

        public void Tick(float dt)
        {
            TimeInState += dt;
            TimeSinceLastStimulus += dt;

            if (CurrentState == CultistaState.Alerta)
            {
                // Volta para Errante se não houver novo estímulo por 8s
                if (TimeSinceLastStimulus >= 8f)
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
                // Alvo ao alcance de golpe tem prioridade: engaja o corpo-a-corpo.
                if (AlvoAoAlcance)
                {
                    ChangeState(CultistaState.Atacar);
                }
                // Perde o rastro após 10s sem ouvir nada
                else if (TimeSinceLastStimulus >= 10f)
                {
                    ChangeState(CultistaState.Errante);
                }
            }
            else if (CurrentState == CultistaState.Atacar)
            {
                // Enquanto o alvo está no corpo-a-corpo, o estado é governado só pela
                // proximidade (não pelos timeouts sonoros): desfere um golpe por cadência.
                if (!AlvoAoAlcance)
                {
                    ChangeState(CultistaState.Caca);
                }
                else
                {
                    _timerAtaque += dt;
                    if (_timerAtaque >= _cadenciaDeAtaque)
                    {
                        _timerAtaque -= _cadenciaDeAtaque;
                        OnGolpeDesferido?.Invoke();
                    }
                }
            }
            else if (CurrentState == CultistaState.Atordoado)
            {
                if (TimeInState >= _duracaoAtordoamento)
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

            // Ao entrar em Atacar, começa a cadência do zero — o primeiro golpe sai
            // após uma cadência completa (janela de telegrafo para o jogador reagir).
            if (novo == CultistaState.Atacar) _timerAtaque = 0f;

            OnStateChanged?.Invoke(old, novo);
        }
    }
}
