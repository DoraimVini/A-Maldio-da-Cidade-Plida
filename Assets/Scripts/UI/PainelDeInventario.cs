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
        [Tooltip("Arte da casa vazia. [ASSET]")]
        [SerializeField] private Sprite molduraVazia;

        [Tooltip("Arte da casa ocupada — tom distinto, para o estado ser legível de relance. [ASSET]")]
        [SerializeField] private Sprite molduraCheia;

        [Tooltip("Opacidade da casa vazia. Com molduras de dois tons ligadas, deixe em 1: quem " +
                 "comunica o estado passa a ser a arte, não o desbotamento.")]
        [SerializeField] private float opacidadeVazio = 0.25f;

        [Range(0f, 1f)]
        [SerializeField] private float opacidadeCheio = 1f;

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
            if (_acaoAbrir != null && _acaoAbrir.WasPressedThisFrame())
                Alternar();
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

            // Restaura o valor anterior, não 1: uma cutscene em câmera lenta não pode ser
            // acelerada só porque o jogador abriu e fechou a mochila.
            if (pausarAoAbrir) Time.timeScale = _escalaDeTempoAnterior;
        }

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
                Pintar(visual, item);
            }
        }

        private void DesenharCorpo()
        {
            for (int i = 0; i < slotsDoCorpo.Length; i++)
            {
                var visual = slotsDoCorpo[i];
                if (visual == null) continue;

                var item = i < _inventario.Equipment.Capacidade ? _inventario.Equipment.GetSlot(i) : null;
                Pintar(visual, item);

                // O rótulo mostra que parte do corpo é, mesmo com o slot vazio — senão o
                // jogador não descobre que existe um lugar para elmo até achar um.
                if (visual.rotulo != null && i < _inventario.Equipment.Capacidade)
                    visual.rotulo.text = _inventario.Equipment.GetSlotType(i).ToString();
            }
        }

        private void Pintar(SlotVisual visual, ItemInstance item)
        {
            bool cheio = item != null && item.Quantidade > 0 && item.Def != null;

            if (visual.grupo != null)
                visual.grupo.alpha = cheio ? opacidadeCheio : opacidadeVazio;

            // A moldura carrega o estado. Antes disso, a única pista de "tem item aqui" era o
            // desbotamento do slot inteiro — que sumiria junto com a moldura e deixaria a grade
            // ilegível. Dois tons distintos resolvem sem depender de alpha.
            if (visual.moldura != null)
            {
                var arte = cheio ? molduraCheia : molduraVazia;
                if (arte != null) visual.moldura.sprite = arte;
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
