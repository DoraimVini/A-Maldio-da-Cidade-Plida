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

        [Header("Config inicial (usado se nenhuma fonte for injetada de fora)")]
        [Tooltip("Resiliência máxima inicial de Damião.")]
        [SerializeField] private float resilienciaMax = 100f;

        [Tooltip("Fração do máximo abaixo da qual o Pânico ativa (0..1).")]
        [Range(0f, 0.99f)]
        [SerializeField] private float fracaoThresholdPanico = 0.25f;

        private ResilienciaMental _resiliencia;

        /// <summary>Instância corrente. Null antes de Awake/injeção.</summary>
        public ResilienciaMental Resiliencia => _resiliencia;

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
