using UnityEngine;

namespace FavelaAmarela.Core.Combat
{
    public class CortesaoPalido : MonoBehaviour
    {
        [Header("Status do Cortesão")]
        [SerializeField] private float vida = 100f;
        [SerializeField] private float velocidadePatrulha = 1.5f;
        [SerializeField] private float campoDeVisao = 6f;
        
        [Header("Visão (Stealth)")]
        [SerializeField] private float anguloVisao = 90f;
        [SerializeField] private LayerMask layerObstaculos;

        [Header("Patrulha")]
        [SerializeField] private Transform[] pontosDePatrulha;
        private int indexPatrulhaAtual = 0;

        private Transform alvo;
        private bool jogadorDetectado = false;

        void Awake() 
        { 
            // Inicialização da FSM ou status
        }

        void Update()
        {
            if (jogadorDetectado && alvo != null)
            {
                AtacarOuPerseguir();
            }
            else
            {
                Patrulhar();
                ProcurarJogador();
            }
        }

        private void Patrulhar()
        {
            if (pontosDePatrulha == null || pontosDePatrulha.Length == 0) return;

            Transform destino = pontosDePatrulha[indexPatrulhaAtual];
            transform.position = Vector2.MoveTowards(transform.position, destino.position, velocidadePatrulha * Time.deltaTime);

            if (Vector2.Distance(transform.position, destino.position) < 0.2f)
            {
                indexPatrulhaAtual = (indexPatrulhaAtual + 1) % pontosDePatrulha.Length;
            }
        }

        private void ProcurarJogador()
        {
            Collider2D col = Physics2D.OverlapCircle(transform.position, campoDeVisao, LayerMask.GetMask("Player"));
            if (col != null)
            {
                Vector2 direcaoAoJogador = (col.transform.position - transform.position).normalized;
                
                // O Cortesão olha para a direção que está patrulhando
                Vector2 direcaoOlhar = transform.right;
                if (pontosDePatrulha != null && pontosDePatrulha.Length > 0)
                {
                    direcaoOlhar = (pontosDePatrulha[indexPatrulhaAtual].position - transform.position).normalized;
                }
                if (direcaoOlhar == Vector2.zero) direcaoOlhar = transform.right;

                if (Vector2.Angle(direcaoOlhar, direcaoAoJogador) < anguloVisao / 2f)
                {
                    // Raycast para garantir que não tem estátuas/paredes na frente
                    float distancia = Vector2.Distance(transform.position, col.transform.position);
                    RaycastHit2D hit = Physics2D.Raycast(transform.position, direcaoAoJogador, distancia, layerObstaculos);
                    
                    if (hit.collider == null)
                    {
                        jogadorDetectado = true;
                        alvo = col.transform;
                    }
                }
            }
        }

        private void AtacarOuPerseguir()
        {
            // Lógica de ataque físico pesado ou grito anômalo
            transform.position = Vector2.MoveTowards(transform.position, alvo.position, (velocidadePatrulha * 1.5f) * Time.deltaTime);
            
            if (Vector2.Distance(transform.position, alvo.position) < 1.5f)
            {
                // Trigger Animação de Ataque
            }
        }
    }
}
