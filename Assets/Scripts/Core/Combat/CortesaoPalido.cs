using UnityEngine;

namespace FavelaAmarela.Core.Combat
{
    /// <summary>
    /// O Cortesão Pálido do Castelo: patrulha entre pontos e persegue Damião quando o vê.
    ///
    /// <para><b>Dívida conhecida e deliberada.</b> Esta classe é um <c>MonoBehaviour</c> morando
    /// em <c>Core/</c>, o que contraria a regra POCO do <c>CLAUDE.md</c> §2. Consertar isso de
    /// verdade é separar uma <c>CortesaoPalidoFSM</c> (Core) de um adaptador (Runtime), como
    /// <c>CultistaFSM</c> + <c>CultistaAI</c> — trabalho de IA de inimigo, não de física. Mover
    /// só o arquivo de pasta trocaria uma violação por outra e mexeria em três referências para
    /// ganho nenhum em jogo. Fica registrado aqui, com nome e endereço.</para>
    ///
    /// <para><b>O que MUDOU em 2026-08-27</b>, e não é dívida — é defeito:</para>
    ///
    /// <list type="number">
    ///   <item><b>Ele andava por <c>transform.position</c> tendo um <c>Rigidbody2D</c>
    ///   Dynamic.</b> Isso teleporta o corpo a cada quadro: a física não integra o movimento, ela
    ///   só descobre o resultado depois. O corpo entra dentro da parede e é expulso no quadro
    ///   seguinte (o tremor), e quando o Damião está no caminho, o Cortesão o <b>atravessa
    ///   empurrando</b> em vez de esbarrar. Passa a mover por <c>linearVelocity</c> em
    ///   <c>FixedUpdate</c>, que é a convenção do projeto (<c>CLAUDE.md</c> §5) e a do
    ///   <c>PlayerMovement</c>.</item>
    ///
    ///   <item><b><c>LayerMask.GetMask("Player")</c> era chamado todo <c>Update</c>.</b> É busca
    ///   por string no <c>TagManager</c> a 60 Hz, por Cortesão, para um valor que nunca muda —
    ///   Regra de Ouro 1. Resolvido uma vez no <c>Awake</c>.</item>
    /// </list>
    /// </summary>
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

        /// <summary>Quanto mais rápido ele vai atrás do Damião do que patrulhando.</summary>
        private const float FatorDePerseguicao = 1.5f;

        /// <summary>Distância que conta como "chegou" ao ponto de patrulha.</summary>
        private const float RaioDeChegada = 0.2f;

        private Transform alvo;
        private bool jogadorDetectado = false;

        private Rigidbody2D _corpo;
        private int _mascaraDoJogador;

        private void Awake()
        {
            _corpo = GetComponent<Rigidbody2D>();

            // Uma busca por nome, não uma por quadro.
            _mascaraDoJogador = LayerMask.GetMask("Player");
            if (_mascaraDoJogador == 0)
                Debug.LogError($"[CortesaoPalido] '{name}': a camada 'Player' não existe no " +
                               "TagManager. A varredura de visão nunca vai achar ninguém e ele " +
                               "patrulha para sempre.", this);

            if (layerObstaculos.value == 0)
                Debug.LogWarning($"[CortesaoPalido] '{name}': 'layerObstaculos' está vazia. O " +
                                 "raycast de linha de visão nunca acerta nada, então ele " +
                                 "enxerga ATRAVÉS de parede.", this);
        }

        /// <summary>
        /// Percepção roda por quadro (é leitura); movimento roda em <c>FixedUpdate</c> (é
        /// física). Misturar os dois é o que fazia o corpo ser teleportado.
        /// </summary>
        private void Update()
        {
            if (jogadorDetectado && alvo != null) return;

            ProcurarJogador();
        }

        private void FixedUpdate()
        {
            Vector2 destino;
            float velocidade;

            if (jogadorDetectado && alvo != null)
            {
                destino = alvo.position;
                velocidade = velocidadePatrulha * FatorDePerseguicao;
            }
            else if (pontosDePatrulha != null && pontosDePatrulha.Length > 0)
            {
                var ponto = pontosDePatrulha[indexPatrulhaAtual];
                if (ponto == null) { Parar(); return; }

                destino = ponto.position;
                velocidade = velocidadePatrulha;

                if (Vector2.Distance(transform.position, destino) < RaioDeChegada)
                    indexPatrulhaAtual = (indexPatrulhaAtual + 1) % pontosDePatrulha.Length;
            }
            else
            {
                Parar();
                return;
            }

            Mover(destino, velocidade);
        }

        /// <summary>
        /// Reproduz o passo do <c>Vector2.MoveTowards</c> antigo — inclusive o freio ao chegar —
        /// mas entrega o resultado como <b>velocidade</b>, para o solver resolver a colisão em
        /// vez de descobrir o atravessamento depois do fato.
        /// </summary>
        private void Mover(Vector2 destino, float velocidade)
        {
            Vector2 posicao = transform.position;
            Vector2 passo = Vector2.MoveTowards(posicao, destino, velocidade * Time.fixedDeltaTime)
                            - posicao;

            if (_corpo != null) _corpo.linearVelocity = passo / Time.fixedDeltaTime;
            else transform.position = posicao + passo;   // sem corpo, o jeito antigo ainda serve
        }

        private void Parar()
        {
            if (_corpo != null) _corpo.linearVelocity = Vector2.zero;
        }

        private void ProcurarJogador()
        {
            Collider2D col = Physics2D.OverlapCircle(transform.position, campoDeVisao,
                                                     _mascaraDoJogador);
            if (col == null) return;

            Vector2 direcaoAoJogador = (col.transform.position - transform.position).normalized;

            // O Cortesão olha para a direção que está patrulhando
            Vector2 direcaoOlhar = transform.right;
            if (pontosDePatrulha != null && pontosDePatrulha.Length > 0 &&
                pontosDePatrulha[indexPatrulhaAtual] != null)
            {
                direcaoOlhar = ((Vector2)pontosDePatrulha[indexPatrulhaAtual].position
                                - (Vector2)transform.position).normalized;
            }
            if (direcaoOlhar == Vector2.zero) direcaoOlhar = transform.right;

            if (Vector2.Angle(direcaoOlhar, direcaoAoJogador) >= anguloVisao / 2f) return;

            // Raycast para garantir que não tem estátuas/paredes na frente
            float distancia = Vector2.Distance(transform.position, col.transform.position);
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direcaoAoJogador, distancia,
                                                 layerObstaculos);

            if (hit.collider != null) return;

            jogadorDetectado = true;
            alvo = col.transform;
        }
    }
}
