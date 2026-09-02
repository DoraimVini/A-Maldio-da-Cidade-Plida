using UnityEngine;
using FavelaAmarela.Core.Persistencia;
using FavelaAmarela.Runtime.Interaction;
using FavelaAmarela.Runtime.Persistencia;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.Runtime.Quests
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Uma das páginas perdidas dos nobres de Yhtill,
    /// espalhadas pelo Deserto e pela Tumba. Recolher todas e devolvê-las a Cassilda é a
    /// quest <b>"A Canção Incompleta"</b> — ver <c>lore/cassilda_e_byakhee.md</c>.
    ///
    /// <para>Coletado por <b>interação deliberada</b> (botão E), como o patuá e o baú: um
    /// colecionável é escolha do jogador, e o prompt sinaliza que ali há algo importante.</para>
    ///
    /// <para><b>Persistente:</b> um fragmento já recolhido não reaparece ao recarregar a
    /// cena. A chave é derivada do índice, não da posição nem do nome do objeto.</para>
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Favela Amarela/Quests/Fragmento de Yhtill")]
    public sealed class FragmentoDeYhtill : MonoBehaviour, IInteragivel
    {
        [Header("Identidade")]
        [Tooltip("Qual dos fragmentos é este (0, 1, 2...). Define a chave de save e qual " +
                 "fala de Cassilda dispara na entrega.")]
        [Min(0)]
        [SerializeField] private int indice = 0;

        [Tooltip("Nome do fragmento, mostrado no prompt e ao recolher.")]
        [SerializeField] private string nomeDoFragmento = "Página perdida";

        [Header("Texto")]
        [Tooltip("Caixa de texto usada ao recolher (reaproveita a UI de dica por ora).")]
        [SerializeField] private TutorialHintUI caixaDeTexto;

        /// <summary>
        /// A caixa a usar: a do Inspector, ou a <b>global do HUD persistente</b>.
        ///
        /// <para><b>Por que existe (2026-09-02).</b> Este campo estava <c>{fileID: 0}</c> em
        /// 100% das instâncias em disco, e sem esta queda a fala era simplesmente
        /// <b>descartada</b> — sem erro, sem aviso. Dos 17 consumidores de
        /// <c>TutorialHintUI</c>, catorze já caíam para a instância global; três não, e este
        /// era um deles.</para>
        ///
        /// <para>Resolve no <b>momento do uso</b>, e não no <c>Awake</c>: a
        /// <c>TutorialHintUI.Instancia</c> só existe depois do <c>OnEnable</c> dela, e quem
        /// acorda antes guardaria nulo para sempre.</para>
        /// </summary>
        private TutorialHintUI CaixaDeFala =>
            caixaDeTexto != null ? caixaDeTexto : TutorialHintUI.Instancia;

        [TextArea(4, 12)]
        [Tooltip("O que está escrito na página. Visível ao jogador — segue o tom melancólico " +
                 "dos nobres de Yhtill, nunca linguagem de sistema.")]
        [SerializeField] private string texto = "";

        private bool _coletado;

        /// <summary>Qual fragmento este objeto é.</summary>
        public int Indice => indice;

        /// <summary>Chave de save deste fragmento específico.</summary>
        public string Chave => $"Quest.Cassilda.Fragmento{indice}";

        // ── IInteragivel ─────────────────────────────────────────────────────

        /// <inheritdoc />
        public string RotuloDeInteracao => $"Recolher: {nomeDoFragmento}";

        /// <inheritdoc />
        public bool PodeInteragir => !_coletado;

        /// <summary>Prioridade alta: é item de quest, ganha do cenário ao redor.</summary>
        public int PrioridadeDeInteracao => 10;

        /// <inheritdoc />
        public Vector2 PosicaoDeInteracao => transform.position;

        private void Start()
        {
            // Já recolhido numa visita anterior: some antes do primeiro frame.
            if (GerenciadorDeSave.JaAconteceu(Chave))
            {
                _coletado = true;
                gameObject.SetActive(false);
            }
        }

        /// <inheritdoc />
        public void Interagir(GameObject quemInterage)
        {
            if (_coletado) return;

            _coletado = true;
            GerenciadorDeSave.MarcarAconteceu(Chave);

            var caixa = CaixaDeFala;
            if (caixa != null && !string.IsNullOrWhiteSpace(texto))
                caixa.Mostrar(texto);

            gameObject.SetActive(false);
        }
    }
}
