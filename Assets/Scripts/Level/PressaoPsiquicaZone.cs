using UnityEngine;

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

        void Awake() 
        { 
            // Inicialização se necessária
        }

        private void Update()
        {
            if (jogadorNaZona && jogadorTransform != null && pontoDeFocoCorrompido != null)
            {
                VerificarContatoVisual();
            }
        }

        private void VerificarContatoVisual()
        {
            Vector2 direcaoAoFoco = (pontoDeFocoCorrompido.position - jogadorTransform.position).normalized;
            // Assumindo que o jogador tenha um método ou forma de saber para onde está olhando.
            // Para simplificar no protótipo, usaremos a direção do movimento ou right/left
            float angulo = Vector2.Angle(jogadorTransform.right, direcaoAoFoco); // Simplificação
            
            if (angulo < anguloDeDistorcaoVisao)
            {
                AplicarDreno();
            }
        }

        private void AplicarDreno()
        {
            // O ideal seria acessar VitalidadeBridge ou GestorDeSanidade
            // jogadorTransform.GetComponent<VitalidadeBridge>().DrenarRM(taxaDrenoRM * Time.deltaTime);
            Debug.Log($"Drenando {taxaDrenoRM * Time.deltaTime} de RM por contato visual com objeto corrompido!");
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                jogadorNaZona = true;
                jogadorTransform = collision.transform;
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                jogadorNaZona = false;
                jogadorTransform = null;
            }
        }
    }
}
