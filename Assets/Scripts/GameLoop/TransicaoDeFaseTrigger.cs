using UnityEngine;
using FavelaAmarela.Core.GameLoop;

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// Trigger de fim de fase/dungeon: ao contato de Damião, pede a transição para
    /// <see cref="GameState.TransicaoDeFase"/>. Reaproveitável em qualquer ponto de saída
    /// (ex.: Portões das Ruínas).
    ///
    /// <para><b>Fase 5, 2026-08-18:</b> recebe a máquina de estados por injeção em vez de
    /// alcançar <c>GameManager.Instance</c>.</para>
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class TransicaoDeFaseTrigger : MonoBehaviour
    {
        private GameLoopStateMachine _maquina;

        /// <summary>Liga à máquina de estados da cena. Chamado pelo <c>GameLoopBootstrap</c>.</summary>
        public void Bind(GameLoopStateMachine maquina)
        {
            if (maquina == null)
            {
                Debug.LogError("[TransicaoDeFaseTrigger] Bind recebeu máquina nula — este portão " +
                               "de saída não vai encerrar a fase.", this);
                return;
            }

            _maquina = maquina;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collision.CompareTag("Player")) return;
            if (_maquina == null) return;

            _maquina.TryTransition(GameState.TransicaoDeFase);
        }
    }
}
