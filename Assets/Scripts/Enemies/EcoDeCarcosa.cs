using UnityEngine;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.GameLoop;

namespace FavelaAmarela.Runtime.Enemies
{
    [AddComponentMenu("Favela Amarela/Enemies/Eco de Carcosa")]
    public class EcoDeCarcosa : MonoBehaviour
    {
        [Header("Anti-Camping (Eco de Carcosa)")]
        [Tooltip("Tempo em segundos que o jogador pode ficar completamente parado antes do Eco se manifestar.")]
        [SerializeField] private float tempoMaximoImovel = 5f;
        
        [Tooltip("Dreno de RM por segundo enquanto o Eco estiver ativo assombrando o jogador.")]
        [SerializeField] private float taxaDrenoRMPorSegundo = 3f;

        private PlayerMovement playerMovement;
        private float tempoParado = 0f;
        private bool ativo = false;

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerMovement = player.GetComponent<PlayerMovement>();
            }
            
            // Oculta inicialmente
            DesativarEco(true);
        }

        private void Update()
        {
            if (playerMovement == null) return;

            // Fica checando o movimento do jogador (ficar parado atrai o Eco)
            if (!playerMovement.IsMoving)
            {
                tempoParado += Time.deltaTime;
                
                if (tempoParado >= tempoMaximoImovel)
                {
                    if (!ativo)
                    {
                        ativo = true;
                        AtivarEco();
                    }
                    DrenarResiliencia();
                }
            }
            else
            {
                // Se voltar a se mover, reseta o timer e afasta o Eco
                if (ativo)
                {
                    ativo = false;
                    DesativarEco(false);
                }
                tempoParado = 0f;
            }
        }

        private void AtivarEco()
        {
            // O Eco deve se manifestar sempre "nas costas" do Damião, para criar tensão
            Vector3 costas = -(Vector3)playerMovement.LookDirection;
            transform.position = playerMovement.transform.position + (costas * 1.5f);
            
            // Ativa renderers ou efeitos visuais (fumaça negra, som bizarro)
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(true);
            }
            
            Debug.Log("[Eco de Carcosa] Manifestou-se! O jogador está acampando.");
        }

        private void DesativarEco(bool instantaneo)
        {
            // Oculta o Eco
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
            }
            
            if (!instantaneo)
            {
                Debug.Log("[Eco de Carcosa] Dissipou-se. O jogador voltou a se mover.");
            }
        }

        private void DrenarResiliencia()
        {
            // Drena a sanidade do Damião por causa da presença opressiva do Eco
            if (GameManager.Instance != null && GameManager.Instance.Resiliencia != null)
            {
                GameManager.Instance.Resiliencia.SofrerTrauma(taxaDrenoRMPorSegundo * Time.deltaTime);
            }
        }
    }
}
