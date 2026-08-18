using FavelaAmarela.Core.Combat;
using UnityEngine;

namespace FavelaAmarela.Runtime.UI
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Dono do ciclo de vida da UI de HUD e
    /// ponto de injeção da <see cref="ResilienciaMental"/> nas views que a
    /// consomem (a <see cref="ResilienciaBar"/>, futuramente a barra de
    /// Ectoplasma, etc).
    ///
    /// Como a ResilienciaMental é POCO (não vive na cena), alguém em Runtime
    /// precisa instanciá-la e distribuí-la. Este controller é esse ponto.
    /// Numa arquitetura maior, a POCO viria de um sistema de save/entidade e
    /// seria apenas repassada aqui — o método InjetarResiliencia cobre os dois
    /// casos (criar local para teste, ou receber de fora).
    /// </summary>
    [AddComponentMenu("FavelaAmarela/UI/HUD Controller")]
    public sealed class HUDController : MonoBehaviour
    {
        [Header("Views de HUD")]
        [SerializeField] private ResilienciaBar resilienciaBar;

        [Tooltip("Barra do Vigor (Estamina). Alimentada pelo GameManager a partir do Player.")]
        [SerializeField] private VigorBar vigorBar;

        [Tooltip("Barra da Vitalidade corpórea (a 'carne'). Alimentada pelo GameManager.")]
        [SerializeField] private VitalidadeBar vitalidadeBar;

        [Tooltip("Barra de ações da Mão Física (arma empunhada + habilidade). Alimentada pelo GameManager.")]
        [SerializeField] private BarraDeAcoes barraDeAcoes;

        [Tooltip("Barra com as 8 posições do inventário (teclas 1–8). Alimentada pelo GameManager.")]
        [SerializeField] private BarraDeItens barraDeItens;

        [Tooltip("Barra dos 4 Artefatos equipados (teclas F1–F4). Alimentada pelo GameManager.")]
        [SerializeField] private BarraDeArtefatos barraDeArtefatos;

        [Header("Config inicial (usado se nenhuma fonte for injetada de fora)")]
        [Tooltip("Resiliência máxima inicial de Damião.")]
        [SerializeField] private float resilienciaMax = 100f;

        [Tooltip("Fração do máximo abaixo da qual o Pânico ativa (0..1).")]
        [Range(0f, 0.99f)]
        [SerializeField] private float fracaoThresholdPanico = 0.25f;

        private ResilienciaMental _resiliencia;
        private Vitalidade _vitalidade;

        /// <summary>Instância corrente. Null antes de Awake/injeção.</summary>
        public ResilienciaMental Resiliencia => _resiliencia;

        /// <summary>Vitalidade corpórea corrente. Null até o GameManager injetar.</summary>
        public Vitalidade Vitalidade => _vitalidade;

        private void Awake()
        {
            // Se ninguém injetou uma fonte externa até aqui, cria uma local.
            // Facilita testar a cena de HUD isolada, sem o sistema de entidade.
            if (_resiliencia == null)
            {
                _resiliencia = ResilienciaMental.ComThresholdFracional(
                    resilienciaMax, fracaoThresholdPanico);
            }

            if (resilienciaBar != null)
                resilienciaBar.Bind(_resiliencia);
        }

        /// <summary>
        /// Injeta uma ResilienciaMental criada por outro sistema (entidade de
        /// Damião, save game). Deve ser chamado antes de Awake para substituir
        /// a instância local, ou a qualquer momento para re-bind em runtime.
        /// </summary>
        public void InjetarResiliencia(ResilienciaMental fonte)
        {
            if (fonte == null) return;
            _resiliencia = fonte;
            if (resilienciaBar != null)
                resilienciaBar.Bind(_resiliencia);
        }

        /// <summary>
        /// Injeta a <see cref="Vitalidade"/> corpórea de Damião (criada pela
        /// <c>VitalidadeBridge</c> a partir da ficha de atributos e repassada pelo
        /// <c>GameManager</c> no bootstrap). Diferente da Resiliência, o HUD não cria uma
        /// local de fallback: a Vitalidade pertence ao ator na cena, não ao HUD.
        /// </summary>
        public void InjetarVitalidade(Vitalidade fonte)
        {
            if (fonte == null)
            {
                Debug.LogError("[HUDController] InjetarVitalidade recebeu null — a barra de " +
                               "Vitalidade vai ficar parada. Provável ordem de Awake: a " +
                               "VitalidadeBridge ainda não tinha criado a POCO.", this);
                return;
            }

            _vitalidade = fonte;

            if (vitalidadeBar != null)
                vitalidadeBar.Bind(_vitalidade);
            else
                Debug.LogError("[HUDController] Campo 'vitalidadeBar' vazio — a Vitalidade foi " +
                               "injetada mas não há barra ligada para mostrá-la.", this);
        }

        /// <summary>
        /// Injeta a Mão Física de Damião na barra de ações, para o HUD mostrar a arma
        /// empunhada e a recarga da habilidade. Chamado pelo <c>GameManager</c> no bootstrap.
        /// </summary>
        public void InjetarMaoFisica(FavelaAmarela.Player.MaoFisicaBridge fonte)
        {
            if (fonte == null) return;
            if (barraDeAcoes != null)
                barraDeAcoes.Bind(fonte);
        }

        /// <summary>
        /// Injeta o inventário na barra de itens (teclas 1–8).
        ///
        /// <para><b>Fase 4, 2026-08-18.</b> O campo <c>barraDeItens</c> já existia aqui, ligado
        /// nas 4 cenas e <b>lido por nenhuma linha de código</b> — referência serializada morta.
        /// A barra se virava sozinha alcançando <c>InventoryManager.Instance</c> em cinco
        /// pontos, um deles dentro do <c>Update</c>. Agora ela recebe a fonte por aqui, como
        /// todas as outras views do HUD.</para>
        /// </summary>
        public void InjetarInventario(FavelaAmarela.Inventario.InventoryManager fonte)
        {
            if (fonte == null) return;

            if (barraDeItens != null)
            {
                barraDeItens.Bind(fonte);
            }
            else
            {
                Debug.LogWarning("[HUDController] Sem 'barraDeItens' ligada — as teclas 1–8 não " +
                                 "vão consumir nem equipar nada, e a barra fica congelada.", this);
            }
        }

        /// <summary>
        /// Injeta o Vigor de Damião na barra correspondente. Chamado pelo <c>GameManager</c>
        /// no bootstrap.
        /// </summary>
        public void InjetarVigor(FavelaAmarela.Player.GerenciadorDeVigor fonte)
        {
            if (fonte == null) return;

            if (vigorBar != null)
            {
                vigorBar.Bind(fonte);
            }
            else
            {
                // Era o único Injetar* que falhava em silêncio — a VigorBar ficou órfã (0
                // cenas, 0 prefabs) sem que nada no console apontasse a causa. Ver
                // Docs/KnowledgeBundle/systems para o histórico (2026-08-13).
                Debug.LogError("[HUDController] Campo 'vigorBar' vazio — o Vigor foi injetado " +
                               "mas não há barra ligada para mostrá-lo.", this);
            }
        }

        /// <summary>
        /// Injeta os Artefatos de Damião na barra de artefatos, para o HUD mostrar os quatro
        /// slots e suas recargas. Chamado pelo <c>GameManager</c> no bootstrap.
        /// </summary>
        public void InjetarArtefatos(FavelaAmarela.Player.ArtefatosBridge fonte)
        {
            if (fonte == null) return;
            if (barraDeArtefatos != null) barraDeArtefatos.Bind(fonte);
        }

        // ── Atalhos de teste (removíveis) ────────────────────────────────────
        // Facilitam validar a barra no editor sem um sistema de combate real.
        // Marcados com ContextMenu para uso manual no Inspector.

        [ContextMenu("Teste — Sofrer 30 de trauma")]
        private void TesteTrauma() => _resiliencia?.SofrerTrauma(30f);

        [ContextMenu("Teste — Ancorar 20")]
        private void TesteAncora() => _resiliencia?.Ancorar(20f);

        [ContextMenu("Teste — Forçar colapso")]
        private void TesteColapso() => _resiliencia?.ForcarColapso();
    }
}
