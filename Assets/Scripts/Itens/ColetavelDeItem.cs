using UnityEngine;
using FavelaAmarela.Progression;
using FavelaAmarela.Runtime.Progression;
using FavelaAmarela.Core.Loot;
using FavelaAmarela.Player;
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

        /// <summary>
        /// Preenche o coletável por código, para quem nasce em runtime (espólio de inimigo)
        /// em vez de ser posicionado à mão no Inspector.
        /// </summary>
        /// <param name="itemDef">Item entregue ao recolher.</param>
        /// <param name="quantos">Quantos exemplares.</param>
        /// <param name="chave">
        /// Chave de save. Vazia — o padrão para espólio de inimigo — faz o item reaparecer
        /// a cada carga de cena, já que o abate do inimigo é quem persiste.
        /// </param>
        /// <summary>
        /// O exemplar rolado que este coletável carrega, quando há um. Nulo em pickup autorado.
        /// </summary>
        private FavelaAmarela.Inventario.ItemInstance _exemplar;

        /// <summary>
        /// Configura o coletável com um <b>exemplar já rolado</b> — grau, nível e afixos
        /// inclusos.
        ///
        /// <para>Existe desde 2026-08-27, com o sistema de afixos: sem ela, o espólio caía
        /// com os modificadores rolados e <b>os perdia na coleta</b>, porque a entrega montava
        /// um <c>ItemInstance</c> novo só com id e quantidade. O jogador veria o item bom no
        /// chão e pegaria um item comum, sem nada acusando.</para>
        /// </summary>
        public void Configurar(FavelaAmarela.Inventario.ItemInstance exemplar,
                               FavelaAmarela.Inventario.ItemDef itemDef, string chave = "")
        {
            Configurar(itemDef, exemplar != null ? exemplar.Quantidade : 1, chave);
            _exemplar = exemplar;
        }

        public void Configurar(FavelaAmarela.Inventario.ItemDef itemDef, int quantos = 1, string chave = "")
        {
            item = itemDef;

            // Sem exemplar rolado, a coleta monta um item simples -- é o caminho de todo
            // pickup autorado à mão no mundo (consumíveis do Deserto, relíquias). Só o espólio
            // de inimigo chega aqui com afixos.
            _exemplar = null;
            quantidade = quantos < 1 ? 1 : quantos;
            chaveDeSave = chave;

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && itemDef != null && itemDef.Icone != null)
                sr.sprite = itemDef.Icone;
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

            // Relíquia não vai para o Bolsão Frio: ela vira Artefato, num inventário próprio
            // que não cobra espaço de mochila. O vínculo item→Artefato é o campo
            // ArtefatoDef.Item, lido por ArtefatosBridge.ArtefatoDoItem.
            var artefatos = quemInterage != null ? quemInterage.GetComponent<ArtefatosBridge>() : null;
            var comoArtefato = artefatos != null ? artefatos.ArtefatoDoItem(item) : null;

            if (comoArtefato != null)
            {
                RecolherComoArtefato(artefatos, comoArtefato);
                return;
            }

            var invManager = FavelaAmarela.Inventario.InventoryManager.Instance;
            if (invManager == null)
            {
                Debug.LogError("[ColetavelDeItem] InventoryManager.Instance não encontrado — " +
                               $"'{item.Nome}' não pôde ser recolhido.", this);
                return;
            }

            // Entrega o EXEMPLAR quando há um: montar um ItemInstance novo aqui descartaria
            // grau, nível e afixos rolados -- o item bom no chão viraria item comum na mochila.
            var aEntregar = _exemplar != null
                ? _exemplar.Clone()
                : RolarExemplarAutorado();

            bool coube = invManager.Main.Add(aEntregar);
            if (!coube)
            {
                // Nada coube: o item fica no chão. Perder relíquia por inventário cheio
                // seria perda silenciosa de progresso.
                Mostrar($"Não há espaço para {item.Nome}.");
                return;
            }

            Consumir(string.IsNullOrWhiteSpace(mensagem)
                ? $"Você recolheu: {item.Nome}."
                : mensagem);
        }

        /// <summary>
        /// Monta o exemplar de um pickup <b>autorado à mão</b> — o que foi posto na cena pelo
        /// designer, sem passar por tabela de drop.
        ///
        /// <para><b>O que estava errado (2026-08-28).</b> Este caminho montava
        /// <c>new ItemInstance(id, quantidade)</c>, o que significa <c>NivelDoItem = 1</c>
        /// para sempre. Uma peça de equipamento posta na cena da última fase entrava na mochila
        /// no piso da escala — a arma achada no Castelo saía mais fraca que a largada por um
        /// Cultista do Deserto.</para>
        ///
        /// <para><b>Equipamento ganha nível e grau; o resto, não.</b> Consumível e chave não
        /// têm afixo nem escala, e um grau visível neles ("Tônico Impregnado") seria ruído
        /// diegético. Para eles o exemplar simples continua sendo a resposta certa.</para>
        /// </summary>
        private FavelaAmarela.Inventario.ItemInstance RolarExemplarAutorado()
        {
            bool ehEquipamento = item.Tipo == FavelaAmarela.Inventario.ItemType.Arma
                              || item.Tipo == FavelaAmarela.Inventario.ItemType.Armadura
                              || item.Tipo == FavelaAmarela.Inventario.ItemType.Amuleto;

            if (!ehEquipamento)
                return new FavelaAmarela.Inventario.ItemInstance(item.Id, quantidade);

            int nivel = ProgressionBridge.Instancia != null
                ? ProgressionBridge.Instancia.NivelAtual
                : 1;

            var grau = CurvaDeGrau.Sortear(nivel, GrauDeImpregnacao.Inerte, _fonte);

            var exemplar = _gerador.Gerar(item, grau, nivel,
                                          FavelaAmarela.Inventario.CatalogoDeAfixos.Todos, _fonte);

            if (exemplar == null)
                return new FavelaAmarela.Inventario.ItemInstance(item.Id, quantidade);

            exemplar.Quantidade = quantidade;
            return exemplar;
        }

        private readonly FavelaAmarela.Inventario.GeradorDeItem _gerador =
            new FavelaAmarela.Inventario.GeradorDeItem();

        private readonly IFonteDeAleatoriedade _fonte = new FonteDeAleatoriedadeUnity();

        /// <summary>
        /// Concede o Artefato e retira o objeto do mundo. <b>Nunca falha por falta de espaço</b>
        /// — a posse de Artefato não tem teto, e os quatro slots são só o que está portado.
        /// Sem slot livre, a relíquia entra dormente em vez de ficar no chão.
        /// </summary>
        private void RecolherComoArtefato(ArtefatosBridge artefatos, FavelaAmarela.Inventario.ArtefatoDef def)
        {
            bool novo = artefatos.Adquirir(def.Id);

            if (!novo && !artefatos.Possui(def.Id))
            {
                // Só acontece se o id não estiver no catálogo da bridge — vale avisar em vez
                // de sumir com o objeto e deixar o jogador sem a relíquia nem o item.
                Debug.LogError($"[ColetavelDeItem] '{def.Id}' não está no catálogo de Artefatos — " +
                               "nada foi concedido.", this);
                return;
            }

            string texto = !string.IsNullOrWhiteSpace(mensagem)
                ? mensagem
                : novo
                    ? $"Você recolheu: {def.Nome}."
                    : $"{def.Nome} já está com você.";

            Consumir(texto);
        }

        /// <summary>Marca como recolhido, persiste e retira o objeto da cena.</summary>
        private void Consumir(string texto)
        {
            _coletado = true;

            if (!string.IsNullOrWhiteSpace(chaveDeSave))
                GerenciadorDeSave.MarcarAconteceu(chaveDeSave);

            Mostrar(texto);
            gameObject.SetActive(false);
        }

        private void Mostrar(string texto)
        {
            if (caixaDeTexto != null) caixaDeTexto.Mostrar(texto);
        }
    }
}
