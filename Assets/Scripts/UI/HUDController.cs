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

        [Tooltip("Barra da Vitalidade corpórea (a 'carne'). Alimentada pelo GameManager.")]
        [SerializeField] private VitalidadeBar vitalidadeBar;

        [Tooltip("Barra de ações da Mão Física (arma empunhada + habilidade). Alimentada pelo GameManager.")]
        [SerializeField] private BarraDeAcoes barraDeAcoes;

        [Tooltip("Barra com as 8 posições do inventário (teclas 1–8). Alimentada pelo GameManager.")]
        [SerializeField] private BarraDeItens barraDeItens;

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
        /// <summary>
        /// Injeta o inventário na barra de itens, para as teclas 1–8 acionarem as posições.
        /// Chamado pelo <c>GameManager</c> no bootstrap.
        /// </summary>
        public void InjetarInventario(FavelaAmarela.Runtime.Itens.InventarioBridge fonte)
        {
            if (fonte == null)
            {
                Debug.LogError("[HUDController] InjetarInventario recebeu null — as teclas " +
                               "1–8 não vão funcionar.", this);
                return;
            }

            if (barraDeItens != null) barraDeItens.Bind(fonte);
        }

        public void InjetarMaoFisica(FavelaAmarela.Player.MaoFisicaBridge fonte)
        {
            if (fonte == null) return;
            if (barraDeAcoes != null)
                barraDeAcoes.Bind(fonte);
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
