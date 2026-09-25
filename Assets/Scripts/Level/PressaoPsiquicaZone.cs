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

        private void AplicarDreno()
        {
            // O GameManager centraliza a posse do POCO ResilienciaMental
            if (GameManager.Instance != null && GameManager.Instance.Resiliencia != null)
            {
                // Usa o termo diegético 'SofrerTrauma' para remover sanidade
                GameManager.Instance.Resiliencia.SofrerTrauma(taxaDrenoRM * Time.deltaTime);
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                jogadorNaZona = true;
                jogadorTransform = collision.transform;
                playerMovement = collision.GetComponent<PlayerMovement>();
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                jogadorNaZona = false;
                jogadorTransform = null;
                playerMovement = null;
            }
        }
    }
}
