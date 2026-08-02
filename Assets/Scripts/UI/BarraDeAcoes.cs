using FavelaAmarela.Player;
using UnityEngine;
using UnityEngine.UI;

namespace FavelaAmarela.Runtime.UI
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Barra de ações da <b>Mão Física</b>: mostra a arma
    /// empunhada, a habilidade dela e a recarga da habilidade. Sem isto, a habilidade da
    /// arma dispara às cegas — o jogador não sabe o que tem na mão nem quando pode usar.
    ///
    /// Contrato de arquitetura:
    ///   • Observa <c>OnArmaTrocada</c>/<c>OnHabilidadeExecutada</c> — não faz polling de estado.
    ///   • A única leitura por frame é o preenchimento de recarga, que é animação visual
    ///     (permitido pelas regras da camada UI, como o Lerp da barra de progresso).
    ///   • Nenhuma regra de combate aqui: cooldown e dano vivem no Core/bridge.
    ///
    /// Layout e sprites (pixel art, PPU 32, Point, sem compressão) são montados no editor.
    /// Pontos de asset marcados com [ASSET].
    /// </summary>
    [AddComponentMenu("FavelaAmarela/UI/Barra de Ações")]
    public sealed class BarraDeAcoes : MonoBehaviour
    {
        [Header("Slot da Mão Física")]
        [Tooltip("Texto com o nome diegético da arma empunhada. [ASSET]")]
        [SerializeField] private Text nomeDaArma;

        [Tooltip("Ícone da arma empunhada (opcional). [ASSET pixel art]")]
        [SerializeField] private Image iconeDaArma;

        [Tooltip("Rótulo mostrado quando Damião está desarmado.")]
        [SerializeField] private string rotuloDesarmado = "Mão Vazia";

        [System.Serializable]
        public sealed class SlotDeAcao
        {
            [Tooltip("Oculta o slot se não houver habilidade equipada.")]
            public CanvasGroup grupo;

            [Tooltip("Texto com o nome diegético da habilidade. [ASSET]")]
            public Text nomeDaHabilidade;

            [Tooltip("Ícone da habilidade. [ASSET]")]
            public Image icone;

            [Tooltip("Image com Type = Filled: recarga da habilidade. [ASSET]")]
            public Image preenchimentoRecarga;

            [Tooltip("Letra do atalho (Q, E, R). [ASSET]")]
            public Text rotuloTecla;
        }

        [Header("Slots de Habilidade (Q, E, R)")]
        [Tooltip("Os slots de ação. O slot 0 é a habilidade da arma (Q). Os demais ficam aguardando poderes anômalos. [ASSET]")]
        [SerializeField] private SlotDeAcao[] slots = new SlotDeAcao[0];

        [Tooltip("Rótulo mostrado no slot 0 quando Damião está desarmado.")]
        [SerializeField] private string rotuloSemHabilidade = "—";

        [Header("Aparência")]
        [Tooltip("Opacidade do slot quando vazio (inativo).")]
        [Range(0f, 1f)]
        [SerializeField] private float opacidadeVazio = 0.25f;

        [Tooltip("Opacidade do slot quando tem uma habilidade pronta.")]
        [Range(0f, 1f)]
        [SerializeField] private float opacidadeCheio = 1.0f;

        [Tooltip("Opacidade do slot de habilidade enquanto ela está recarregando.")]
        [Range(0f, 1f)]
        [SerializeField] private float opacidadeRecarregando = 0.45f;

        private MaoFisicaBridge _fonte;

        /// <summary>
        /// Conecta a barra à Mão Física de Damião. Chamado pelo <c>HUDController</c>.
        /// Idempotente: re-bind troca a fonte com segurança.
        /// </summary>
        public void Bind(MaoFisicaBridge fonte)
        {
            if (fonte == null)
            {
                Debug.LogWarning("[BarraDeAcoes] Bind recebeu fonte nula.");
                return;
            }

            Unbind(); // garante que não fica escutando duas fontes

            _fonte = fonte;
            _fonte.OnArmaTrocada += Redesenhar;

            Redesenhar();
        }

        /// <summary>Desconecta do evento. Seguro chamar mesmo sem bind ativo.</summary>
        public void Unbind()
        {
            if (_fonte != null)
                _fonte.OnArmaTrocada -= Redesenhar;
            _fonte = null;
        }

        private void OnDisable() => Unbind(); // nunca deixa handler pendurado

        /// <summary>Reescreve os rótulos dos slots a partir da arma atual.</summary>
        private void Redesenhar()
        {
            if (_fonte == null) return;

            bool armado = _fonte.TemArmaEquipada;

            if (nomeDaArma != null)
                nomeDaArma.text = armado ? _fonte.NomeDaArmaEquipada : rotuloDesarmado;

            if (iconeDaArma != null)
                iconeDaArma.enabled = armado;

            if (slots != null)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    var slot = slots[i];
                    if (slot == null) continue;

                    // Apenas o slot 0 responde à arma física por enquanto
                    bool slotOcupado = (i == 0 && armado);

                    if (i == 0 && slot.nomeDaHabilidade != null)
                        slot.nomeDaHabilidade.text = armado ? _fonte.NomeDaHabilidade : rotuloSemHabilidade;
                    
                    if (slot.icone != null)
                        slot.icone.enabled = slotOcupado;

                    if (slot.grupo != null)
                    {
                        slot.grupo.alpha = slotOcupado ? opacidadeCheio : opacidadeVazio;
                        slot.grupo.interactable = slotOcupado;
                    }
                }
            }
        }

        private void Update()
        {
            if (_fonte == null) return;

            // Animação de recarga do Slot 0 (Habilidade da Arma)
            if (slots != null && slots.Length > 0)
            {
                var slot0 = slots[0];
                if (slot0 != null && _fonte.TemArmaEquipada)
                {
                    float progresso = _fonte.ProgressoCooldownHabilidade;
                    
                    if (slot0.preenchimentoRecarga != null)
                        slot0.preenchimentoRecarga.fillAmount = progresso;

                    if (slot0.grupo != null)
                    {
                        bool pronta = _fonte.HabilidadePronta;
                        slot0.grupo.alpha = pronta ? opacidadeCheio : opacidadeRecarregando;
                    }
                }
            }
        }
    }
}
