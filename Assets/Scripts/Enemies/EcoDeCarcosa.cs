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
                _mente = player.GetComponentInChildren<FavelaAmarela.Runtime.Combat.ResilienciaBridge>();

                if (_mente == null)
                    Debug.LogWarning("[EcoDeCarcosa] Damião sem ResilienciaBridge — o Eco não vai " +
                                     "drenar nada.", this);
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

        // Resolvida uma vez no Start, junto com o playerMovement — não por frame.
        private FavelaAmarela.Runtime.Combat.ResilienciaBridge _mente;

        private void DrenarResiliencia()
        {
            // Drena a sanidade do Damião pela presença opressiva do Eco.
            _mente?.SofrerTrauma(taxaDrenoRMPorSegundo * Time.deltaTime);
        }
    }
}
