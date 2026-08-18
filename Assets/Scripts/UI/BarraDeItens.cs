using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.Runtime.UI
{
    /// <summary>
    /// Camada Runtime (UI). Os 8 primeiros slots da mochila, sempre visíveis, acionáveis pelas
    /// teclas 1–8.
    ///
    /// <para><b>Fonte injetada, não buscada</b> (Fase 4 da refatoração de managers, 2026-08-18).
    /// Antes ela alcançava <c>InventoryManager.Instance</c> em cinco pontos, um deles
    /// <b>dentro do <see cref="Update"/></b> — busca de singleton a cada frame, o que a Regra de
    /// Ouro 1 do <c>CLAUDE.md</c> proíbe. Agora a referência é guardada uma vez no
    /// <see cref="Bind"/>.</para>
    ///
    /// <para><b>Quem injeta:</b> o <c>HUDController</c>, que já tinha esta barra num campo
    /// serializado — ligado nas 4 cenas e <b>lido por ninguém</b>. A injeção passa por lá em vez
    /// de o bootstrap procurar a barra, seguindo o mesmo caminho de todas as outras fontes do
    /// HUD.</para>
    /// </summary>
    [AddComponentMenu("FavelaAmarela/UI/Barra de Itens")]
    public sealed class BarraDeItens : MonoBehaviour
    {
        /// <summary>Quantas posições da mochila a barra espelha, e quantas teclas responde.</summary>
        private const int SlotsDaBarra = 8;

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

        // A fonte injetada. Sobrevive a OnDisable de propósito: desativar o GameObject deve
        // suspender a inscrição, não esquecer quem é o inventário.
        private InventoryManager _fonte;
        private bool _inscrito;

        /// <summary>Se a barra tem fonte injetada.</summary>
        public bool Ligada => _fonte != null;

        /// <summary>
        /// Liga a barra a um inventário. Idempotente: re-bind troca a fonte sem deixar handler
        /// pendurado na anterior.
        ///
        /// <para>Funciona com o GameObject inativo — guarda a fonte e a inscrição acontece no
        /// <c>OnEnable</c>. Sem isso, injetar numa barra que nasce desativada seria perdido.</para>
        /// </summary>
        public void Bind(InventoryManager inventario)
        {
            if (inventario == null)
            {
                Debug.LogWarning("[BarraDeItens] Bind recebeu inventário nulo — a barra fica " +
                                 "inerte e as teclas 1–8 não fazem nada.", this);
                return;
            }

            Desinscrever();
            _fonte = inventario;

            if (isActiveAndEnabled) Inscrever();
            Redesenhar();
        }

        /// <summary>Desliga e <b>esquece</b> a fonte. Seguro chamar sem bind prévio.</summary>
        public void Unbind()
        {
            Desinscrever();
            _fonte = null;
        }

        /// <summary>
        /// Reinscreve na fonte já injetada.
        ///
        /// <para><b>Bug que isto corrige:</b> havia <c>OnDisable → Unbind</c> sem contrapartida no
        /// <c>OnEnable</c>, e o bind original acontecia no <c>Start</c>, que não roda de novo.
        /// Desativar e reativar o GameObject deixava a barra <b>permanentemente morta</b> —
        /// continuava desenhada, com os ícones congelados no último estado.</para>
        /// </summary>
        private void OnEnable()
        {
            Inscrever();
            Redesenhar();
        }

        private void OnDisable() => Desinscrever();

        private void Inscrever()
        {
            if (_inscrito || _fonte?.Main == null) return;

            _fonte.Main.OnSlotChanged += HandleSlotChanged;
            _inscrito = true;
        }

        /// <summary>
        /// Guarda a <b>mesma</b> instância entre inscrever e desinscrever. A versão antiga
        /// re-buscava o singleton no <c>Unbind</c>: se a instância tivesse trocado no meio-tempo,
        /// o <c>-=</c> miraria outro objeto e a assinatura original vazaria.
        /// </summary>
        private void Desinscrever()
        {
            if (_inscrito && _fonte?.Main != null)
                _fonte.Main.OnSlotChanged -= HandleSlotChanged;

            _inscrito = false;
        }

        private void Update()
        {
            if (_fonte == null) return;

            var teclado = Keyboard.current;
            if (teclado == null) return;

            int limite = Mathf.Min(slots.Length, SlotsDaBarra);
            for (int i = 0; i < limite; i++)
            {
                if (TeclaDoSlot(teclado, i)?.wasPressedThisFrame != true) continue;

                var item = _fonte.Main.GetSlot(i);
                if (item == null || item.Def == null) continue;

                if (item.Def.Tipo == ItemType.Consumivel)
                {
                    _fonte.ConsumirItem(i);
                }
                else if (item.Def.Tipo == ItemType.Arma
                         || item.Def.Tipo == ItemType.Armadura
                         || item.Def.Tipo == ItemType.Amuleto)
                {
                    _fonte.Equipar(i);
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
            if (_fonte?.Main == null) return;

            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null) continue;

                var item = _fonte.Main.GetSlot(i);
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
