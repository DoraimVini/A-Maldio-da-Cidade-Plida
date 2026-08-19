using UnityEngine;
using FavelaAmarela.Inventario;
using FavelaAmarela.Runtime.Rendering;
using FavelaAmarela.Core.Combat;

namespace FavelaAmarela.Dungeons
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Bootstrap do <b>Templo da Serpente</b> (Dungeon 2 do
    /// Deserto de Hali): injeta nas peças da cena o que elas não podem saber sozinhas — hoje,
    /// qual item rompe o selo da <see cref="PortaDeAklo"/>.
    ///
    /// <para>A injeção existe para a porta não depender de uma referência de Inspector para
    /// um <c>ItemDef</c>: o id é dado semântico da dungeon, não do prefab da porta.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Dungeons/Templo da Serpente (Setup)")]
    public sealed class TemploSerpenteSetup : MonoBehaviour
    {
        [Header("Referências da Cena")]
        [Tooltip("Porta selada em Aklo que dá passagem ao Corredor dos Glifos (Z2). [CENA]")]
        [SerializeField] private GameObject portaTemplo;

        [Tooltip("Id do ItemDef do Necronomicon — o tomo que traduz as gravuras. [ASSET]")]
        [SerializeField] private string idNecronomicon = "necronomicon";

        private void Start()
        {
            if (InventoryManager.Instance == null)
                Debug.LogError("[TemploSerpenteSetup] InventoryManager ausente — a Porta de Aklo " +
                               "não conseguirá checar o Bolsão Frio.", this);

            if (portaTemplo == null)
            {
                Debug.LogError("[TemploSerpenteSetup] 'portaTemplo' não atribuído — o selo de Aklo " +
                               "ficará sem configuração.", this);
                return;
            }

            var selo = portaTemplo.GetComponent<PortaDeAklo>();
            if (selo == null)
            {
                Debug.LogError($"[TemploSerpenteSetup] '{portaTemplo.name}' não tem PortaDeAklo — " +
                               "a passagem não vai gatear nada.", portaTemplo);
                return;
            }

            selo.Configurar(idNecronomicon);

            ValidarYSort();
        }

        /// <summary>
        /// Diagnóstico de montagem: acusa atores que ficariam com a ordem de desenho errada
        /// no isométrico. Só no Editor — a varredura é cara demais para uma build.
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void ValidarYSort()
        {
            // Sem o parâmetro de ordenação: a sobrecarga com FindObjectsSortMode foi depreciada
            // na Unity 6 (a mensagem manda usar FindObjectsByType<T>() ou a versão só com
            // FindObjectsInactive). Aqui a ordem nunca importou — é varredura, não índice.
            var componentes = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude);
            foreach (var componente in componentes)
                if (componente is IDanificavel && componente.GetComponent<DynamicYSort>() == null)
                    Debug.LogWarning($"[TemploSerpenteSetup] '{componente.name}' é danificável mas " +
                                     "não tem DynamicYSort — vai desenhar fora de ordem.", componente);
        }
    }
}
