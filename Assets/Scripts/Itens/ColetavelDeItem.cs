using UnityEngine;
using FavelaAmarela.Runtime.Interaction;
using FavelaAmarela.Runtime.Persistencia;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.Runtime.Itens
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Um item largado no mundo, que vai para o
    /// <b>inventário</b> ao ser recolhido.
    ///
    /// <para>Genérico de propósito: qualquer item — relíquia, consumível, arma — usa este
    /// mesmo componente com um <see cref="ItemConfig"/> diferente. Antes cada colecionável
    /// tinha o próprio script (<c>PatuaPickup</c>, <c>NecronomiconPickup</c>), o que
    /// multiplicava o mesmo código por item.</para>
    ///
    /// <para>Recolhido por <b>interação deliberada</b> (botão E), como o baú: colecionável é
    /// escolha do jogador, e o prompt sinaliza que ali há algo.</para>
    ///
    /// <para><b>Inventário cheio não some com o item.</b> Ele fica no chão e avisa — perder
    /// uma relíquia por falta de espaço seria perda silenciosa de progresso.</para>
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Favela Amarela/Itens/Coletável de Item")]
    public sealed class ColetavelDeItem : MonoBehaviour, IInteragivel
    {
        [Header("Item")]
        [Tooltip("Qual item este objeto entrega. [ASSET]")]
        [SerializeField] private FavelaAmarela.Inventario.ItemDef item;

        [Tooltip("Quantos exemplares.")]
        [Min(1)]
        [SerializeField] private int quantidade = 1;

        [Header("Persistência")]
        [Tooltip("Chave de save. Vazio = o item reaparece a cada carregamento de cena " +
                 "(certo para drops de inimigo; errado para colecionável único).")]
        [SerializeField] private string chaveDeSave = "";

        [Header("Feedback")]
        [Tooltip("Caixa de texto mostrada ao recolher.")]
        [SerializeField] private TutorialHintUI caixaDeTexto;

        [TextArea(2, 6)]
        [Tooltip("Mensagem ao recolher. Vazio = usa o nome do item.")]
        [SerializeField] private string mensagem = "";

        private bool _coletado;

        // ── IInteragivel ─────────────────────────────────────────────────────

        /// <inheritdoc />
        public string RotuloDeInteracao =>
            item != null ? $"Recolher: {item.Nome}" : "Recolher";

        /// <inheritdoc />
        public bool PodeInteragir => !_coletado;

        /// <summary>Prioridade alta: item no chão ganha do cenário ao redor.</summary>
        public int PrioridadeDeInteracao => 10;

        /// <inheritdoc />
        public Vector2 PosicaoDeInteracao => transform.position;

        private void Awake()
        {
            if (item == null)
                Debug.LogError($"[ColetavelDeItem] '{name}' está sem ItemDef — não entrega nada.", this);
        }

        private void Start()
        {
            // Já recolhido numa visita anterior: some antes do primeiro frame.
            if (!string.IsNullOrWhiteSpace(chaveDeSave) && GerenciadorDeSave.JaAconteceu(chaveDeSave))
            {
                _coletado = true;
                gameObject.SetActive(false);
            }
        }

        /// <inheritdoc />
        public void Interagir(GameObject quemInterage)
        {
            if (_coletado || item == null) return;

            var invManager = FavelaAmarela.Inventario.InventoryManager.Instance;
            if (invManager == null)
            {
                Debug.LogError("[ColetavelDeItem] InventoryManager.Instance não encontrado — " +
                               $"'{item.Nome}' não pôde ser recolhido.", this);
                return;
            }

            bool coube = invManager.Main.Add(new FavelaAmarela.Inventario.ItemInstance(item.Id, quantidade));
            if (!coube)
            {
                // Nada coube: o item fica no chão. Perder relíquia por inventário cheio
                // seria perda silenciosa de progresso.
                Mostrar($"Não há espaço para {item.Nome}.");
                return;
            }

            _coletado = true;

            if (!string.IsNullOrWhiteSpace(chaveDeSave))
                GerenciadorDeSave.MarcarAconteceu(chaveDeSave);

            Mostrar(string.IsNullOrWhiteSpace(mensagem)
                ? $"Você recolheu: {item.Nome}."
                : mensagem);

            gameObject.SetActive(false);
        }

        private void Mostrar(string texto)
        {
            if (caixaDeTexto != null) caixaDeTexto.Mostrar(texto);
        }
    }
}
