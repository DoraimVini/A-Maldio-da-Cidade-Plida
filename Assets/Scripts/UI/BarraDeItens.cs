using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
using FavelaAmarela.Core.Itens;
using FavelaAmarela.Runtime.Itens;

namespace FavelaAmarela.Runtime.UI
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). <b>Barra de itens</b>: as 8 posições do inventário
    /// na tela, acionáveis pelas teclas <b>1 a 8</b>.
    ///
    /// <para>Antes a barra de ações só <i>mostrava</i> a arma e a habilidade — era um painel
    /// informativo. Com as teclas ligadas ela vira o que o nome promete: o lugar de onde o
    /// jogador age. Usar uma Ancoragem no meio de uma perseguição não pode exigir abrir
    /// menu.</para>
    ///
    /// <para>As 8 teclas casam com <see cref="Inventario.PosicoesPadrao"/> — cada tecla é
    /// uma posição, sem paginação. É o mesmo motivo de o inventário ser enxuto: o jogador
    /// deve saber de cor o que tem, não navegar.</para>
    ///
    /// <para><b>Input pelo teclado direto</b> (<c>Keyboard.current</c>), não pelo asset de
    /// ações: são 8 atalhos fixos de UI, e criar 8 <c>InputAction</c> no asset só se paga
    /// quando existir remapeamento de teclas. Continua sendo o Input System novo, nunca o
    /// <c>Input</c> legado.</para>
    /// </summary>
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

        private InventarioBridge _fonte;

        /// <summary>Um slot da barra: fundo, ícone, contagem e o número da tecla.</summary>
        [System.Serializable]
        public sealed class SlotDeItem
        {
            [Tooltip("Fundo do slot — recebe a variação de opacidade.")]
            public CanvasGroup grupo;

            [Tooltip("Ícone do item guardado aqui.")]
            public Image icone;

            [Tooltip("Quantidade, escondida quando é 1 ou vazio.")]
            public Text quantidade;
        }

        /// <summary>
        /// Conecta a barra ao inventário de Damião. Chamado pelo <c>HUDController</c>.
        /// Idempotente: re-bind troca a fonte com segurança.
        /// </summary>
        public void Bind(InventarioBridge fonte)
        {
            if (fonte == null)
            {
                Debug.LogWarning("[BarraDeItens] Bind recebeu fonte nula — a barra fica inerte.");
                return;
            }

            Unbind();

            _fonte = fonte;
            if (_fonte.Inventario != null) _fonte.Inventario.OnMudou += Redesenhar;

            Redesenhar();
        }

        /// <summary>Desconecta do evento. Seguro chamar mesmo sem bind ativo.</summary>
        public void Unbind()
        {
            if (_fonte?.Inventario != null) _fonte.Inventario.OnMudou -= Redesenhar;
            _fonte = null;
        }

        private void OnDisable() => Unbind();

        private void Update()
        {
            if (_fonte == null) return;

            var teclado = Keyboard.current;
            if (teclado == null) return;   // sem teclado (gamepad puro): a barra só exibe

            // Um laço por frame sobre 8 teclas: barato e sem alocação.
            for (int i = 0; i < slots.Length && i < 8; i++)
            {
                if (TeclaDoSlot(teclado, i)?.wasPressedThisFrame == true)
                    _fonte.Usar(i);
            }
        }

        /// <summary>
        /// Mapeia a posição do slot para a tecla numérica. Explícito, e não aritmética sobre
        /// o enum <c>Key</c>: a ordem de <c>Key.Digit*</c> é detalhe interno do Input System
        /// e não é contrato para se apoiar.
        /// </summary>
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

        /// <summary>Reescreve os slots a partir do conteúdo corrente do inventário.</summary>
        private void Redesenhar()
        {
            if (_fonte?.Inventario == null) return;

            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null) continue;

                var pilha = _fonte.Inventario.Ver(i);
                bool cheio = !pilha.Vazia;

                if (slot.grupo != null)
                    slot.grupo.alpha = cheio ? opacidadeCheio : opacidadeVazio;

                if (slot.icone != null)
                {
                    slot.icone.enabled = cheio;
                    // O ícone vem do ItemConfig; a POCO não conhece Sprite (é Core puro).
                    // Sem ícone autorado, o slot mostra só a contagem — não fica em branco.
                }

                if (slot.quantidade != null)
                {
                    // Só mostra número quando empilhado: "1" em todo slot é ruído visual.
                    bool mostrar = cheio && pilha.Quantidade > 1;
                    slot.quantidade.enabled = mostrar;
                    if (mostrar) slot.quantidade.text = pilha.Quantidade.ToString();
                }
            }
        }
    }
}
