using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.Runtime.UI
{
    [AddComponentMenu("FavelaAmarela/UI/Barra de Itens")]
    public sealed class BarraDeItens : MonoBehaviour
    {
        [Header("Slots")]
        [Tooltip("Os 8 slots, na ordem das posições do inventário. [ASSET]")]
        [SerializeField] private SlotDeItem[] slots = new SlotDeItem[0];

        [Header("Aparência")]
        [Tooltip("Opacidade do slot vazio — some quase por completo para não poluir a tela.")]
        [Range(0f, 1f)]
        [SerializeField] private float opacidadeVazio = 0.25f;

        [Tooltip("Opacidade do slot ocupado.")]
        [Range(0f, 1f)]
        [SerializeField] private float opacidadeCheio = 0.9f;

        [System.Serializable]
        public sealed class SlotDeItem
        {
            public CanvasGroup grupo;
            public Image icone;
            public Text quantidade;
        }

        private void Start()
        {
            Bind();
        }

        public void Bind()
        {
            var invManager = InventoryManager.Instance;
            if (invManager == null)
            {
                Debug.LogWarning("[BarraDeItens] InventoryManager.Instance nulo — a barra fica inerte.");
                return;
            }

            invManager.Main.OnSlotChanged += HandleSlotChanged;
            Redesenhar();
        }

        public void Unbind()
        {
            var invManager = InventoryManager.Instance;
            if (invManager?.Main != null) invManager.Main.OnSlotChanged -= HandleSlotChanged;
        }

        private void OnDisable() => Unbind();

        private void Update()
        {
            var invManager = InventoryManager.Instance;
            if (invManager == null) return;

            var teclado = Keyboard.current;
            if (teclado == null) return;

            for (int i = 0; i < slots.Length && i < 8; i++)
            {
                if (TeclaDoSlot(teclado, i)?.wasPressedThisFrame == true)
                {
                    var item = invManager.Main.GetSlot(i);
                    if (item != null && item.Def != null)
                    {
                        if (item.Def.Tipo == ItemType.Consumivel)
                            invManager.ConsumirItem(i);
                        else if (item.Def.Tipo == ItemType.Arma || item.Def.Tipo == ItemType.Armadura || item.Def.Tipo == ItemType.Amuleto)
                            invManager.Equipar(i);
                    }
                }
            }
        }

        private static KeyControl TeclaDoSlot(Keyboard t, int indice) => indice switch
        {
            0 => t.digit1Key,
            1 => t.digit2Key,
            2 => t.digit3Key,
            3 => t.digit4Key,
            4 => t.digit5Key,
            5 => t.digit6Key,
            6 => t.digit7Key,
            7 => t.digit8Key,
            _ => null,
        };

        private void HandleSlotChanged(int indice) => Redesenhar();

        private void Redesenhar()
        {
            var invManager = InventoryManager.Instance;
            if (invManager?.Main == null) return;

            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null) continue;

                var item = invManager.Main.GetSlot(i);
                bool cheio = item != null && item.Quantidade > 0;

                if (slot.grupo != null)
                    slot.grupo.alpha = cheio ? opacidadeCheio : opacidadeVazio;

                if (slot.icone != null)
                {
                    slot.icone.enabled = cheio;
                    if (cheio && item.Def != null)
                        slot.icone.sprite = item.Def.Icone;
                }

                if (slot.quantidade != null)
                {
                    bool mostrar = cheio && item.Quantidade > 1;
                    slot.quantidade.enabled = mostrar;
                    if (mostrar) slot.quantidade.text = item.Quantidade.ToString();
                }
            }
        }
    }
}
