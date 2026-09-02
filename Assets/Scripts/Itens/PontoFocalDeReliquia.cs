using UnityEngine;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.Enemies;
using FavelaAmarela.Runtime.Interaction;
using FavelaAmarela.Runtime.UI;

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

        [Header("Fala")]
        [Tooltip("Caixa onde o ponto focal responde ao jogador. Se vazia, usa a do HUD. [CENA]")]
        [SerializeField] private TutorialHintUI caixaDeTexto;

        private bool _ativado;

        /// <summary>
        /// A caixa de fala em uso, resolvida NA HORA DO USO.
        ///
        /// <para>O campo nasce vazio em prefab-asset (nao referencia objeto de cena), e a caixa
        /// vive no HUD persistente. Resolver no Awake pegaria um HUD que ainda nao subiu.</para>
        /// </summary>
        private TutorialHintUI CaixaDeFala
            => caixaDeTexto != null ? caixaDeTexto : TutorialHintUI.Instancia;

        private void Dizer(string texto)
        {
            var caixa = CaixaDeFala;
            if (caixa != null) caixa.Mostrar(texto);
        }

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
            if (artefatos == null) return;

            // O NOME diegetico, e nao o id: "anel_sinal_amarelo" nao e coisa que se diga.
            string nome = artefatos.Def(artefatoId)?.Nome ?? "a relíquia";

            // TRES desfechos, TRES falas. Antes, os dois primeiros eram um Debug.Log -- que num
            // build nao existe -- e o terceiro trocava um sprite que nao esta autorado. O
            // jogador apertava E e a tela nao mudava em desfecho nenhum, o que e indistinguivel
            // de um altar quebrado. Relatado pelo Vini em 2026-09-02.
            if (!artefatos.Inventario.Contem(artefatoId))
            {
                Dizer(artefatos.Inventario.Possui(artefatoId)
                    // POSSUI mas nao PORTA. E a armadilha fina desta luta: sao quatro slots de
                    // Artefato e tres reliquias, entao qualquer outro Artefato portado deixa uma
                    // delas dormente -- e o ponto focal exige porte, nao posse.
                    ? $"{nome} dorme na tua mochila. O ponto focal só responde ao que Damião " +
                      "traz em mãos."
                    : $"O ponto focal não responde. {nome} não está contigo.");
                return;
            }

            if (!rei.AtivarReliquia(artefatoId)) return;

            _ativado = true;
            if (spriteDoPonto != null && spriteAtivo != null)
                spriteDoPonto.sprite = spriteAtivo;

            int faltam = rei.ReliquiasFaltando;
            Dizer(faltam > 0
                ? $"O ponto focal desperta. Ainda faltam {faltam}."
                : "O último ponto focal desperta. O rito de selamento começa.");
        }

        private void Start()
        {
            if (spriteDoPonto != null && spriteInativo != null)
                spriteDoPonto.sprite = spriteInativo;
        }
    }
}
