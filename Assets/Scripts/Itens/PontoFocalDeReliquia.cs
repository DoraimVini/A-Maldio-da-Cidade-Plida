using UnityEngine;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.Enemies;
using FavelaAmarela.Runtime.Interaction;

namespace FavelaAmarela.Runtime.Itens
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Um dos pontos focais do Trono de Aldebaran onde
    /// Damião ativa uma Relíquia para dar início ao rito de selamento do Rei em Amarelo.
    ///
    /// <para>Abre por <b>interação deliberada</b> (botão E), mesmo contrato do
    /// <c>BauDaTumba</c>/<c>PatuaBridge</c> — implementa <see cref="IInteragivel"/>. A
    /// diferença central para um baú: aqui não se entrega nada, se <b>checa</b> algo. O
    /// ponto só ativa se o jogador já tiver a relíquia equipada no
    /// <see cref="InventarioDeArtefatos"/> — não é o ponto que dá a relíquia, é o jogador que
    /// precisa tê-la trazido.</para>
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Favela Amarela/Itens/Ponto Focal de Relíquia")]
    public sealed class PontoFocalDeReliquia : MonoBehaviour, IInteragivel
    {
        [Header("Relíquia")]
        [Tooltip("Id do ItemDef de Artefato exigido aqui (ex.: 'necronomicon').")]
        [SerializeField] private string artefatoId;

        [Tooltip("Rótulo diegético mostrado no prompt de interação.")]
        [SerializeField] private string rotulo = "Ativar a relíquia";

        [Header("Confronto")]
        [Tooltip("O Rei em Amarelo desta arena. [CENA]")]
        [SerializeField] private ReiEmAmareloAI rei;

        [Header("Visual")]
        [Tooltip("Trocado para indicar 'já ativado', sem precisar de animação. [ASSET pixel art]")]
        [SerializeField] private SpriteRenderer spriteDoPonto;

        [SerializeField] private Sprite spriteInativo;
        [SerializeField] private Sprite spriteAtivo;

        private bool _ativado;

        private void Awake()
        {
            if (rei == null)
                Debug.LogError("[PontoFocalDeReliquia] Sem referência ao Rei em Amarelo — " +
                               "este ponto nunca vai conseguir ativar o rito.", this);

            if (string.IsNullOrWhiteSpace(artefatoId))
                Debug.LogError("[PontoFocalDeReliquia] Sem artefatoId configurado.", this);
        }

        // ── IInteragivel ─────────────────────────────────────────────────────

        /// <inheritdoc />
        public string RotuloDeInteracao => rotulo;

        /// <inheritdoc />
        public bool PodeInteragir => !_ativado;

        /// <inheritdoc />
        public int PrioridadeDeInteracao => 0;

        /// <inheritdoc />
        public Vector2 PosicaoDeInteracao => transform.position;

        /// <inheritdoc />
        public void Interagir(GameObject quemInterage)
        {
            if (_ativado || rei == null || string.IsNullOrWhiteSpace(artefatoId)) return;

            var artefatos = quemInterage.GetComponent<ArtefatosBridge>();
            if (artefatos == null || !artefatos.Inventario.Contem(artefatoId))
            {
                Debug.Log($"[PontoFocalDeReliquia] Damião não tem '{artefatoId}' equipado — " +
                          "o ponto continua inerte.", this);
                return;
            }

            if (!rei.AtivarReliquia(artefatoId)) return;

            _ativado = true;
            if (spriteDoPonto != null && spriteAtivo != null)
                spriteDoPonto.sprite = spriteAtivo;
        }

        private void Start()
        {
            if (spriteDoPonto != null && spriteInativo != null)
                spriteDoPonto.sprite = spriteInativo;
        }
    }
}
