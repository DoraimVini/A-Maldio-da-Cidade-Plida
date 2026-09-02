using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.Runtime.UI
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). A <b>tela de inventário</b>: a mochila inteira e os
    /// slots do corpo, abertos por tecla.
    ///
    /// <para>Até 2026-08-11 o jogo só tinha a <see cref="BarraDeItens"/> — 8 posições sempre
    /// visíveis. Os <b>slots de equipamento não tinham interface nenhuma</b>: dava para
    /// equipar pela barra, mas não para ver o que estava no corpo. Esta tela fecha isso.</para>
    ///
    /// <para><b>Abrir pausa o mundo</b> (<c>Time.timeScale = 0</c>) e trava o movimento: num
    /// jogo de sobrevivência, mexer na mochila enquanto um Cultista se aproxima seria
    /// pedir para o jogador morrer olhando menu. Sair restaura o que havia antes, não um
    /// valor fixo — para não brigar com uma cutscene em câmera lenta.</para>
    /// </summary>
    [AddComponentMenu("FavelaAmarela/UI/Painel de Inventário")]
    public sealed class PainelDeInventario : MonoBehaviour
    {
        /// <summary>Uma posição desenhada da mochila.</summary>
        [System.Serializable]
        public sealed class SlotVisual
        {
            [Tooltip("Some/aparece conforme o slot tem item.")]
            public CanvasGroup grupo;

            [Tooltip("Moldura da casa. Troca de arte conforme vazia ou ocupada. [ASSET]")]
            public Image moldura;

            [Tooltip("Ícone do item. [ASSET]")]
            public Image icone;

            [Tooltip("Quantidade empilhada; escondida quando é 1.")]
            public Text quantidade;

            [Tooltip("Rótulo do slot de corpo (Elmo, Peitoral...). Vazio na mochila.")]
            public Text rotulo;

            [Tooltip("Botão da casa. É o que torna o slot CLICÁVEL — sem ele o inventário é " +
                     "uma vitrine. [ASSET]")]
            public Button botao;
        }

        [Header("Raiz")]
        [Tooltip("O objeto ligado/desligado ao abrir e fechar. [ASSET]")]
        [SerializeField] private GameObject raizDoPainel;

        [Header("Mochila")]
        [Tooltip("Uma entrada por posição da mochila, na ordem. [ASSET]")]
        [SerializeField] private SlotVisual[] slotsDaMochila = new SlotVisual[0];

        [Header("Corpo")]
        [Tooltip("Uma entrada por slot de equipamento, na ordem da anatomia. [ASSET]")]
        [SerializeField] private SlotVisual[] slotsDoCorpo = new SlotVisual[0];

        [Header("Aparência")]
        [Range(0f, 1f)]
        [Header("Molduras (Dark Ages UI)")]
        [Tooltip("Arte da casa vazia da MOCHILA. [ASSET]")]
        [SerializeField] private Sprite molduraVazia;

        [Tooltip("Arte da casa ocupada da MOCHILA — tom distinto, para o estado ser legível de " +
                 "relance. [ASSET]")]
        [SerializeField] private Sprite molduraCheia;

        [Tooltip("Arte do slot de CORPO vazio. Par próprio, ornado: o que está vestido tem de " +
                 "se distinguir do que está guardado. [ASSET]")]
        [SerializeField] private Sprite molduraCorpoVazia;

        [Tooltip("Arte do slot de CORPO ocupado. [ASSET]")]
        [SerializeField] private Sprite molduraCorpoCheia;

        [Tooltip("Opacidade da casa vazia. Com molduras de dois tons ligadas, deixe em 1: quem " +
                 "comunica o estado passa a ser a arte, não o desbotamento.")]
        [SerializeField] private float opacidadeVazio = 0.25f;

        [Range(0f, 1f)]
        [SerializeField] private float opacidadeCheio = 1f;

        [Header("Seleção")]
        [Tooltip("Cor da moldura do slot selecionado.")]
        [SerializeField] private Color corSelecionado = new Color(1f, 0.86f, 0.45f, 1f);

        [Header("Comportamento")]
        [Tooltip("Pausa o jogo enquanto o inventário estiver aberto.")]
        [SerializeField] private bool pausarAoAbrir = true;

        private InputAction _acaoAbrir;
        private InventoryManager _inventario;
        private float _escalaDeTempoAnterior = 1f;

        /// <summary>Se a tela está aberta agora.</summary>
        public bool Aberto { get; private set; }

        private void Awake()
        {
            // A ação vive no PlayerInput do Damião; a UI não tem um. Resolver por Inspector
            // seria mais limpo, mas o painel é um objeto de HUD e o input é do jogador —
            // então buscamos a ação pelo mapa global do asset.
            _acaoAbrir = InputSystem.actions?.FindAction("Inventario");

            if (raizDoPainel != null) raizDoPainel.SetActive(false);
            Aberto = false;
        }

        private void Start()
        {
            _inventario = InventoryManager.Instance;
            if (_inventario == null)
            {
                Debug.LogWarning("[PainelDeInventario] InventoryManager.Instance ausente; a tela ficará vazia.", this);
                return;
            }

            _inventario.Main.OnSlotChanged += HandleSlotChanged;
            _inventario.Equipment.OnEquipmentChanged += Redesenhar;

            LigarOsBotoes();
        }

        private void OnDestroy()
        {
            if (_inventario == null) return;

            _inventario.Main.OnSlotChanged -= HandleSlotChanged;
            _inventario.Equipment.OnEquipmentChanged -= Redesenhar;
        }

        private void HandleSlotChanged(int _) => Redesenhar();

        private void Update()
        {
            // ABRIR só com o jogo no comando; FECHAR sempre que ele estiver aberto. Sem a
            // segunda metade o painel se auto-bloquearia -- ele toma o foco ao abrir, e o
            // próprio Tab deixaria de responder.
            bool podeAbrir = FavelaAmarela.Runtime.Entrada.ArbitroDeFoco.JogoNoComando;

            if (_acaoAbrir != null && _acaoAbrir.WasPressedThisFrame() && (podeAbrir || Aberto))
            {
                Alternar();
                return;
            }

            // ESC FECHA A MOCHILA (2026-09-02). Antes ele não fazia nada aqui: caía no
            // PausaInputHandler e PAUSAVA O JOGO POR BAIXO do inventário aberto, deixando dois
            // donos do Time.timeScale e o jogador com duas telas empilhadas.
            if (Aberto && UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Fechar();
            }
        }

        /// <summary>Abre se fechado, fecha se aberto.</summary>
        public void Alternar()
        {
            if (Aberto) Fechar();
            else Abrir();
        }

        /// <summary>Abre a tela, pausando o mundo.</summary>
        public void Abrir()
        {
            if (Aberto) return;

            Aberto = true;
            if (raizDoPainel != null) raizDoPainel.SetActive(true);

            // Toma o comando do teclado. Sem isto -- e `Time.timeScale = 0` NÃO basta, porque
            // Update continua rodando -- com a mochila aberta o Damião continuava esquivando,
            // golpeando, queimando Artefatos em F1-F4 e consumindo itens em 1-8.
            FavelaAmarela.Runtime.Entrada.ArbitroDeFoco.Tomar(
                FavelaAmarela.Core.Entrada.CamadaDeEntrada.PainelModal);

            if (pausarAoAbrir)
            {
                _escalaDeTempoAnterior = Time.timeScale;
                Time.timeScale = 0f;
            }

            Redesenhar();
        }

        /// <summary>Fecha a tela e devolve o mundo ao ritmo em que estava.</summary>
        public void Fechar()
        {
            if (!Aberto) return;

            Aberto = false;
            if (raizDoPainel != null) raizDoPainel.SetActive(false);

            FavelaAmarela.Runtime.Entrada.ArbitroDeFoco.Devolver(
                FavelaAmarela.Core.Entrada.CamadaDeEntrada.PainelModal);

            // Sem isto, reabrir a mochila mostraria uma casa acesa de uma escolha que o jogador
            // já esqueceu -- e o próximo clique moveria algo que ele não pediu.
            _origemSelecionada = Origem.Nenhuma;
            _indiceSelecionado = -1;

            // Restaura o valor anterior, não 1: uma cutscene em câmera lenta não pode ser
            // acelerada só porque o jogador abriu e fechou a mochila.
            if (pausarAoAbrir) Time.timeScale = _escalaDeTempoAnterior;
        }

        /// <summary>De onde veio a casa selecionada.</summary>
        private enum Origem { Nenhuma, Mochila, Corpo }

        private Origem _origemSelecionada = Origem.Nenhuma;
        private int _indiceSelecionado = -1;

        /// <summary>
        /// Liga cada botão de slot ao seu índice. <b>Sem isto o inventário é uma vitrine</b>:
        /// até 2026-09-02 os slots eram só <c>Image</c> dentro de um <c>CanvasGroup</c>, sem
        /// <c>Button</c> e sem nenhum handler de ponteiro, e o <c>SlotVisual</c> nem sabia qual
        /// índice representava — o índice existia só no <c>for</c> do desenho.
        /// </summary>
        private void LigarOsBotoes()
        {
            for (int i = 0; i < slotsDaMochila.Length; i++)
            {
                int indice = i;   // captura por valor: o `i` do laço muda embaixo do callback
                var b = slotsDaMochila[i]?.botao;
                if (b == null) continue;

                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => Clicar(Origem.Mochila, indice));
            }

            for (int i = 0; i < slotsDoCorpo.Length; i++)
            {
                int indice = i;
                var b = slotsDoCorpo[i]?.botao;
                if (b == null) continue;

                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => Clicar(Origem.Corpo, indice));
            }
        }

        /// <summary>
        /// Um clique numa casa. <b>Selecionar e depois clicar no destino</b>, em vez de
        /// arrastar: arrastar exige quatro interfaces de ponteiro, um objeto fantasma seguindo
        /// o cursor e um alvo de soltura — e nada disso é testável sem uma cena. Dois cliques
        /// resolvem o mesmo problema com um <c>Button</c>, que o EventSystem do projeto já
        /// serve.
        /// </summary>
        private void Clicar(Origem origem, int indice)
        {
            if (_inventario == null) return;

            bool temItem = origem == Origem.Mochila
                ? _inventario.Main.GetSlot(indice) != null
                : _inventario.Equipment.GetSlot(indice) != null;

            // Nada selecionado: seleciona, se houver o que selecionar.
            if (_origemSelecionada == Origem.Nenhuma)
            {
                if (!temItem) return;

                _origemSelecionada = origem;
                _indiceSelecionado = indice;
                Redesenhar();
                return;
            }

            // Clicou na mesma casa: desiste.
            if (_origemSelecionada == origem && _indiceSelecionado == indice)
            {
                LimparSelecao();
                return;
            }

            if (_origemSelecionada == Origem.Mochila && origem == Origem.Mochila)
                _inventario.Mover(_indiceSelecionado, indice);

            else if (_origemSelecionada == Origem.Mochila && origem == Origem.Corpo)
                _inventario.Equipar(_indiceSelecionado);

            else if (_origemSelecionada == Origem.Corpo && origem == Origem.Mochila)
                _inventario.Desequipar(_indiceSelecionado);

            // Corpo -> Corpo não tem semântica: trocar elmo por grevas não é operação.
            LimparSelecao();
        }

        private void LimparSelecao()
        {
            _origemSelecionada = Origem.Nenhuma;
            _indiceSelecionado = -1;
            Redesenhar();
        }

        private bool EstaSelecionado(Origem origem, int indice) =>
            _origemSelecionada == origem && _indiceSelecionado == indice;

        private void Redesenhar()
        {
            if (!Aberto || _inventario == null) return;

            DesenharMochila();
            DesenharCorpo();
        }

        private void DesenharMochila()
        {
            for (int i = 0; i < slotsDaMochila.Length; i++)
            {
                var visual = slotsDaMochila[i];
                if (visual == null) continue;

                var item = i < _inventario.Main.Capacidade ? _inventario.Main.GetSlot(i) : null;
                Pintar(visual, item, molduraVazia, molduraCheia,
                       EstaSelecionado(Origem.Mochila, i));
            }
        }

        private void DesenharCorpo()
        {
            for (int i = 0; i < slotsDoCorpo.Length; i++)
            {
                var visual = slotsDoCorpo[i];
                if (visual == null) continue;

                var item = i < _inventario.Equipment.Capacidade ? _inventario.Equipment.GetSlot(i) : null;

                // Par PRÓPRIO para o corpo. Sem isto, o Pintar sobrescreveria em runtime a
                // moldura ornada que os slots de equipamento recebem no prefab, e a distinção
                // "vestido x guardado" existiria só no Editor -- que é a forma mais irritante
                // de um detalhe de UI não existir.
                Pintar(visual, item, molduraCorpoVazia, molduraCorpoCheia,
                       EstaSelecionado(Origem.Corpo, i));

                // O rótulo mostra que parte do corpo é, mesmo com o slot vazio — senão o
                // jogador não descobre que existe um lugar para elmo até achar um.
                if (visual.rotulo != null && i < _inventario.Equipment.Capacidade)
                    visual.rotulo.text = _inventario.Equipment.GetSlotType(i).ToString();
            }
        }

        /// <param name="vazia">Arte da casa sem item.</param>
        /// <param name="cheia">Arte da casa com item.</param>
        /// <param name="selecionado">Se esta casa é a que o jogador escolheu para mover.</param>
        private void Pintar(SlotVisual visual, ItemInstance item, Sprite vazia, Sprite cheia,
                            bool selecionado)
        {
            bool cheio = item != null && item.Quantidade > 0 && item.Def != null;

            if (visual.grupo != null)
                visual.grupo.alpha = cheio ? opacidadeCheio : opacidadeVazio;

            // A moldura carrega o estado. Antes disso, a única pista de "tem item aqui" era o
            // desbotamento do slot inteiro — que sumiria junto com a moldura e deixaria a grade
            // ilegível. Dois tons distintos resolvem sem depender de alpha.
            if (visual.moldura != null)
            {
                var arte = cheio ? cheia : vazia;
                if (arte != null) visual.moldura.sprite = arte;

                // A seleção é COR, não troca de sprite: as duas artes de moldura já carregam o
                // estado vazio/ocupado, e um terceiro sprite faria a casa selecionada perder a
                // informação de ter item.
                visual.moldura.color = selecionado ? corSelecionado : Color.white;
            }

            if (visual.icone != null)
            {
                visual.icone.enabled = cheio;
                if (cheio && item.Def.Icone != null) visual.icone.sprite = item.Def.Icone;
            }

            if (visual.quantidade != null)
            {
                bool empilhado = cheio && item.Quantidade > 1;
                visual.quantidade.enabled = empilhado;
                if (empilhado) visual.quantidade.text = item.Quantidade.ToString();
            }
        }
    }
}
