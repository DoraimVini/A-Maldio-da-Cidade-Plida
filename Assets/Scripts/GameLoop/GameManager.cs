using UnityEngine;

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// <b>Casca vazia.</b> Não expõe mais nada e não é consumido por linha nenhuma de código —
    /// existe apenas porque o componente está serializado em 5 cenas, e removê-lo do código sem
    /// removê-lo das cenas deixaria um <i>Missing Script</i> em cada uma.
    ///
    /// <para><b>Como chegou aqui.</b> Era um God Object de 375 linhas com seis responsabilidades.
    /// A refatoração de managers o esvaziou em cinco fases:</para>
    /// <list type="number">
    ///   <item>estado, morte, cutscene e companheiro viraram componentes focados;</item>
    ///   <item><see cref="GameLoopBootstrap"/> assumiu a injeção de dependências;</item>
    ///   <item>a progressão virou POCO + <c>ProgressionBridge</c> auto-instanciado;</item>
    ///   <item>a <c>BarraDeItens</c> passou a receber o inventário por injeção;</item>
    ///   <item>os 31 call-sites migraram — os últimos 19 pela <c>ResilienciaBridge</c>.</item>
    /// </list>
    ///
    /// <para><b>O que destravou o fim:</b> a Resiliência não tinha bridge no Damião, ao contrário
    /// da Vitalidade. Por isso tudo que feria a mente precisava de um global. Com
    /// <c>ResilienciaBridge</c> no prefab, quem atinge Damião resolve pelo próprio alvo
    /// (<c>GetComponentInParent</c>) — inclusive o <c>ConeDeGelo</c>, que o Abdul instancia em
    /// runtime e que bootstrap nenhum alcançaria.</para>
    ///
    /// <para><b>Próximo passo:</b> remover o componente das 5 cenas e apagar este arquivo. É
    /// operação de cena, no mesmo molde de <c>MigrarParaGameLoopBootstrap</c>.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Game Manager (vazio, a remover)")]
    public class GameManager : MonoBehaviour
    {
        private void Awake()
        {
            Debug.LogWarning("[GameManager] Componente vazio ainda presente nesta cena. Ele não " +
                             "faz mais nada — a refatoração de managers terminou em 2026-08-18. " +
                             "Pode ser removido do GameObject.", this);
        }
    }
}
