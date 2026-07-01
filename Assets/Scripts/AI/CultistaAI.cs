using UnityEngine;
using FavelaAmarela.Core.AI;

namespace FavelaAmarela.Runtime.AI
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class CultistaAI : MonoBehaviour
    {
        private CultistaFSM _fsm;
        private SpriteRenderer _spriteRenderer;

        [Header("Configurações")]
        [SerializeField] private float velocidadeErrante = 1.0f;
        [SerializeField] private float velocidadeCaca = 3.5f;
        [SerializeField] private Color corErrante = Color.white;
        [SerializeField] private Color corAlerta = Color.yellow;
        [SerializeField] private Color corCaca = Color.red;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _fsm = new CultistaFSM(CultistaState.Errante);
            _fsm.OnStateChanged += HandleStateChanged;
            
            AtualizarVisual(_fsm.CurrentState);
        }

        private void Update()
        {
            _fsm.Tick(Time.deltaTime);

            // Simulação de comportamento baseado no estado (Pode ser integrado ao NavMesh depois)
            switch (_fsm.CurrentState)
            {
                case CultistaState.Errante:
                    // Logica de patrulha lenta (velocidadeErrante)
                    break;
                case CultistaState.Alerta:
                    // Parado (Pausa telegrafada antes de caçar)
                    break;
                case CultistaState.Caca:
                    // Perseguir jogador (velocidadeCaca)
                    break;
            }
        }

        public void Ouvir(Vector2 origemSom, bool jogadorCorrendo)
        {
            float distancia = Vector2.Distance(transform.position, origemSom);
            _fsm.ReceberEstimuloSonoro(distancia, jogadorCorrendo);
        }

        private void HandleStateChanged(CultistaState anterior, CultistaState atual)
        {
            AtualizarVisual(atual);
        }

        private void AtualizarVisual(CultistaState estado)
        {
            switch (estado)
            {
                case CultistaState.Errante:
                    _spriteRenderer.color = corErrante;
                    break;
                case CultistaState.Alerta:
                    _spriteRenderer.color = corAlerta;
                    break;
                case CultistaState.Caca:
                    _spriteRenderer.color = corCaca;
                    break;
            }
        }

        private void OnDestroy()
        {
            if (_fsm != null)
            {
                _fsm.OnStateChanged -= HandleStateChanged;
            }
        }

        private void OnDrawGizmos()
        {
            if (_fsm == null) return;

            Gizmos.color = _spriteRenderer != null ? _spriteRenderer.color : Color.white;
            Gizmos.DrawWireSphere(transform.position, 1.5f);
            
            // Desenha raio de vibração
            Gizmos.color = new Color(0.5f, 0, 0.5f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, 3f);
        }
    }
}
