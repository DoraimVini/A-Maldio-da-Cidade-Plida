using UnityEngine;

namespace FavelaAmarela.Runtime.GameLoop
{
    [RequireComponent(typeof(Collider2D))]
    public class VitoriaTrigger : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TriggerVitoria();
                }
            }
        }
    }
}
