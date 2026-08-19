using UnityEngine;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.GameLoop;

namespace FavelaAmarela.Level
{
    public class PressaoPsiquicaZone : MonoBehaviour
    {
        [Header("Configurações")]
        [SerializeField] private float taxaDrenoRM = 2f;
        [SerializeField] private Transform pontoDeFocoCorrompido;
        [SerializeField] private float anguloDeDistorcaoVisao = 45f;

        private bool jogadorNaZona = false;
        private Transform jogadorTransform;
        private PlayerMovement playerMovement;

        private void Update()
        {
            if (jogadorNaZona && jogadorTransform != null && pontoDeFocoCorrompido != null && playerMovement != null)
            {
                VerificarContatoVisual();
            }
        }

        private void VerificarContatoVisual()
        {
            Vector2 direcaoAoFoco = (pontoDeFocoCorrompido.position - jogadorTransform.position).normalized;
            
            // Usa o vetor real de visão do Player, atualizado via Input System
            float angulo = Vector2.Angle(playerMovement.LookDirection, direcaoAoFoco); 
            
            if (angulo < anguloDeDistorcaoVisao)
            {
                AplicarDreno();
            }
        }

        // Resolvida uma vez, quando o jogador entra na zona — não por frame. A bridge fica no
        // próprio Damião, então quem o detectou já a tem em mãos.
        private FavelaAmarela.Runtime.Combat.ResilienciaBridge _mente;

        private void AplicarDreno()
        {
            // 'SofrerTrauma' é o termo diegético para remover sanidade.
            _mente?.SofrerTrauma(taxaDrenoRM * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                jogadorNaZona = true;
                jogadorTransform = collision.transform;
                playerMovement = collision.GetComponent<PlayerMovement>();
                _mente = collision.GetComponentInParent<FavelaAmarela.Runtime.Combat.ResilienciaBridge>();

                if (_mente == null)
                    Debug.LogWarning("[PressaoPsiquicaZone] Damião sem ResilienciaBridge — esta " +
                                     "zona não vai drenar nada.", this);
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                jogadorNaZona = false;
                jogadorTransform = null;
                playerMovement = null;
                _mente = null;
            }
        }
    }
}
