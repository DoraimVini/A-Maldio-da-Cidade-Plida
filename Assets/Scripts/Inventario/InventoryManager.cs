// Assets/Scripts/Inventario/InventoryManager.cs
using UnityEngine;

namespace FavelaAmarela.Inventario
{
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void GarantirInstancia()
        {
            if (Instance == null)
            {
                var prefab = Resources.Load<GameObject>("InventoryManager");
                if (prefab != null)
                {
                    var obj = Instantiate(prefab);
                    DontDestroyOnLoad(obj);
                }
            }
        }

        [Header("Mochila")]
        [SerializeField] private int capacidadeMochila = MainInventory.DefaultCapacidadeSurvivalHorror;

        [Header("Slots do Corpo (ordem define índices)")]
        [SerializeField] private EquipmentSlot[] anatomia = {
            EquipmentSlot.Arma,
            EquipmentSlot.Elmo,
            EquipmentSlot.Peitoral,
            EquipmentSlot.Grevas,
            EquipmentSlot.Amuleto,
            EquipmentSlot.Anel
        };

        private MainInventory _main;
        private EquipmentInventory _equipment;

        public MainInventory Main
        {
            get
            {
                if (_main == null)
                    _main = new MainInventory(capacidadeMochila);
                return _main;
            }
            private set => _main = value;
        }

        public EquipmentInventory Equipment
        {
            get
            {
                if (_equipment == null)
                    _equipment = new EquipmentInventory(anatomia);
                return _equipment;
            }
            private set => _equipment = value;
        }

        public event System.Action<ItemDef, int> OnItemConsumed;

        public void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (_main == null) _main = new MainInventory(capacidadeMochila);
            if (_equipment == null) _equipment = new EquipmentInventory(anatomia);
        }

        public bool ConsumirItem(int indice)
        {
            var item = Main.GetSlot(indice);
            if (item?.Def == null) return false;
            
            if (item.Def.Tipo != ItemType.Consumivel)
            {
                Debug.LogWarning($"[InventoryManager] '{item.Def.Nome}' não é consumível.");
                return false;
            }

            OnItemConsumed?.Invoke(item.Def, indice);
            Main.Remove(indice, 1);
            return true;
        }

        /// <summary>
        /// Move um item da mochila para o equipamento.
        /// </summary>
        public bool Equipar(int indiceMochila)
        {
            var item = Main.GetSlot(indiceMochila);
            if (item == null) return false;

            if (!Equipment.CanAddAny(item)) return false;

            // Equip sem especificar índice (auto-localiza pelo slot do item)
            ItemInstance antigo = Equipment.Equip(item);
            
            // Se o item.Quantidade for > 0, significa que o AddAt falhou internamente
            if (item.Quantidade > 0) return false; 

            // Remove da mochila a quantidade que foi equipada (sempre 1 para equipamento)
            Main.Remove(indiceMochila, 1);

            // Se havia um item antigo, tenta devolver pra mochila
            if (antigo != null && !Main.Add(antigo))
            {
                Debug.LogWarning("Mochila cheia! Item antigo dropado no chão.");
                // TODO: instanciar loot no mundo
            }

            return true;
        }

        /// <summary>
        /// Desequipa o slot e tenta mover para a mochila.
        /// </summary>
        public bool Desequipar(int indiceEquip)
        {
            ItemInstance retirado = Equipment.Unequip(indiceEquip);
            if (retirado == null) return false;

            if (!Main.Add(retirado))
            {
                Debug.LogWarning("Mochila cheia! Item dropado no chão.");
                // TODO: instanciar loot no mundo
            }
            return true;
        }

        // ------------------ Verificação de Itens (Relíquias e Quest) ------------------
        /// <summary>
        /// Verifica se um item (como uma Relíquia passiva ou Chave) existe na Mochila.
        /// Retorna verdadeiro se a quantidade for maior que zero.
        /// </summary>
        public bool PossuiItemNaMochila(string itemDefId)
        {
            for (int i = 0; i < Main.Capacidade; i++)
            {
                var slot = Main.GetSlot(i);
                if (slot != null && slot.Def != null && slot.Def.Id == itemDefId && slot.Quantidade > 0)
                {
                    return true;
                }
            }
            return false;
        }

        // ------------------ Persistência ------------------
        public InventorySaveData GetSaveData() => new InventorySaveData(Main, Equipment);

        public void LoadFromSaveData(InventorySaveData data)
        {
            if (data == null) return;

            Main = new MainInventory(data.mainSlotData.Length);
            for (int i = 0; i < data.mainSlotData.Length; i++)
            {
                if (data.mainSlotData[i] != null && !string.IsNullOrEmpty(data.mainSlotData[i].itemDefId))
                {
                    Main.AddAt(new ItemInstance(data.mainSlotData[i].itemDefId, data.mainSlotData[i].quantity), i);
                }
            }

            Equipment = new EquipmentInventory(anatomia);
            for (int i = 0; i < data.equipSlotData.Length; i++)
            {
                if (data.equipSlotData[i] != null && !string.IsNullOrEmpty(data.equipSlotData[i].itemDefId))
                {
                    Equipment.Equip(new ItemInstance(data.equipSlotData[i].itemDefId, data.equipSlotData[i].quantity), i);
                }
            }
        }
    }
}
