using UnityEngine;
using FavelaAmarela.Inventario;
using FavelaAmarela.Runtime.Rendering;
using FavelaAmarela.Core.Combat;

namespace FavelaAmarela.Dungeons
{
    public class TemploSerpenteSetup : MonoBehaviour
    {
        [Header("Referências da Cena")]
        [SerializeField] private GameObject portaTemplo;
        [SerializeField] private string idNecronomicon = "3a2fdc7e8d6573047ab29912bc7c6f47";
        private void Start()
        {
            if (InventoryManager.Instance == null)
            {
                Debug.LogError("[TemploSerpenteSetup] InventoryManager ausente. Abortando setup.");
                return;
            }

            if (portaTemplo != null)
            {
                var puzzle = portaTemplo.GetComponent<PortaDeAklo>();
                if (puzzle == null)
                    puzzle = portaTemplo.AddComponent<PortaDeAklo>();
                puzzle.Configurar(idNecronomicon);
            }

            // Validação defensiva: verificar DynamicYSort em entidades
            var entidades = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude);
            foreach (var e in entidades)
            {
                if (e is IDanificavel && e.GetComponent<DynamicYSort>() == null)
                    Debug.LogWarning($"[TemploSerpenteSetup] {e.name} não tem DynamicYSort!", e);
            }

            Debug.Log("[TemploSerpenteSetup] Dungeon inicializada com sucesso.");
        }
    }
}
