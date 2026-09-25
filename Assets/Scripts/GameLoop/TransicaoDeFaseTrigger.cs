using UnityEngine;

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// Trigger de fim de fase/dungeon: ao contato do Player, dispara a transição
    /// <see cref="Core.GameLoop.GameState.TransicaoDeFase"/> via <see cref="GameManager"/>.
    /// Reaproveitável em qualquer ponto de saída (ex.: Portões das Ruínas).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class TransicaoDeFaseTrigger : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TriggerTransicaoDeFase();
                }
            }
        }
    }
}
