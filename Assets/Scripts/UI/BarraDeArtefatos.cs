using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Core.Artefatos;
using FavelaAmarela.Player;

namespace FavelaAmarela.Runtime.UI
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Barra dos <b>Artefatos</b>: quatro slots (F1–F4), um por
    /// Artefato equipado, cada um com a habilidade daquele Artefato e a própria recarga.
    ///
    /// <para>São quatro porque o inventário de Artefatos é de quatro — a barra é a leitura
    /// direta dele. Sem isso o jogador não sabe o que carrega nem quando pode invocar.</para>
    ///
    /// Contrato de arquitetura:
    ///   • Observa <c>OnArtefatosMudaram</c> — não faz polling de composição.
    ///   • A única leitura por frame é o preenchimento de recarga (animação visual, permitida).
    ///   • Nenhuma regra aqui: custo, cooldown e efeito vivem no Core/bridge.
    /// </summary>
    [AddComponentMenu("FavelaAmarela/UI/Barra de Artefatos")]
    public sealed class BarraDeArtefatos : MonoBehaviour
    {
        [System.Serializable]
        public sealed class SlotDeArtefato
        {
            [Tooltip("Oculta o slot quando não há Artefato equipado.")]
            public CanvasGroup grupo;

            [Tooltip("Texto com o nome diegético da habilidade. [ASSET]")]
            public Text nomeDaHabilidade;

            [Tooltip("Ícone do Artefato. [ASSET pixel art]")]
            public Image icone;

            [Tooltip("Image com Type = Filled: recarga da habilidade. [ASSET]")]
            public Image preenchimentoRecarga;

            [Tooltip("Letra do atalho (F1–F4). [ASSET]")]
            public Text rotuloTecla;
        }

        [Header("Slots (F1–F4)")]
        [Tooltip("Os quatro slots de Artefato, na mesma ordem do inventário. [ASSET]")]
        [SerializeField] private SlotDeArtefato[] slots = new SlotDeArtefato[InventarioDeArtefatos.TotalDeSlots];

        [Tooltip("Rótulo mostrado num slot sem Artefato.")]
        [SerializeField] private string rotuloSlotVazio = "—";

        [Header("Aparência")]
        [Range(0f, 1f)]
        [Tooltip("Opacidade do slot vazio.")]
        [SerializeField] private float opacidadeVazio = 0.25f;

        [Range(0f, 1f)]
        [Tooltip("Opacidade do slot com habilidade pronta.")]
        [SerializeField] private float opacidadeCheio = 1.0f;

        [Range(0f, 1f)]
        [Tooltip("Opacidade do slot enquanto a habilidade recarrega.")]
        [SerializeField] private float opacidadeRecarregando = 0.45f;

        private ArtefatosBridge _fonte;

        /// <summary>
        /// Conecta a barra aos Artefatos de Damião. Chamado pelo <c>HUDController</c>.
        /// Idempotente: re-bind troca a fonte com segurança.
        /// </summary>
        public void Bind(ArtefatosBridge fonte)
        {
            if (fonte == null)
            {
                Debug.LogWarning("[BarraDeArtefatos] Bind recebeu fonte nula.");
                return;
            }

            Unbind();

            _fonte = fonte;
            _fonte.OnArtefatosMudaram += Redesenhar;

            Redesenhar();
        }

        /// <summary>Desconecta do evento. Seguro chamar mesmo sem bind ativo.</summary>
        public void Unbind()
        {
            if (_fonte != null) _fonte.OnArtefatosMudaram -= Redesenhar;
            _fonte = null;
        }

        private void OnDisable() => Unbind();

        /// <summary>Reescreve os slots a partir do inventário de Artefatos.</summary>
        private void Redesenhar()
        {
            if (_fonte == null || slots == null) return;

            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null) continue;

                var def = _fonte.DefNoSlot(i);
                bool ocupado = def != null;

                if (slot.nomeDaHabilidade != null)
                    slot.nomeDaHabilidade.text = ocupado ? def.NomeDaHabilidade : rotuloSlotVazio;

                if (slot.icone != null)
                {
                    slot.icone.enabled = ocupado;
                    if (ocupado && def.Icone != null) slot.icone.sprite = def.Icone;
                }

                if (slot.rotuloTecla != null)
                    slot.rotuloTecla.text = $"F{i + 1}";

                if (slot.grupo != null)
                {
                    slot.grupo.alpha = ocupado ? opacidadeCheio : opacidadeVazio;
                    slot.grupo.interactable = ocupado;
                }
            }
        }

        private void Update()
        {
            if (_fonte == null || slots == null) return;

            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null || _fonte.DefNoSlot(i) == null) continue;

                float progresso = _fonte.ProgressoCooldown(i);

                if (slot.preenchimentoRecarga != null)
                    slot.preenchimentoRecarga.fillAmount = progresso;

                if (slot.grupo != null)
                    slot.grupo.alpha = _fonte.EstaPronto(i) ? opacidadeCheio : opacidadeRecarregando;
            }
        }
    }
}
