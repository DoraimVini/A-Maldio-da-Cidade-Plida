using UnityEngine;

namespace FavelaAmarela.Runtime.GameLoop
{
    [RequireComponent(typeof(Collider2D))]
    public class ColapsoTrigger : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                if (GameManager.Instance != null && GameManager.Instance.Resiliencia != null)
                {
                    // Força o colapso imediatamente (ex: cair num abismo)
                    GameManager.Instance.Resiliencia.ForcarColapso();
                }
            }
        }
    }
}
