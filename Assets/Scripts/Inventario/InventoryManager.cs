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

        /// <summary>
        /// Reaplica um save ao inventário <b>reaproveitando as instâncias existentes</b>.
        ///
        /// <para><b>Bug que motivou (playtest de 2026-08-11: "perde a arma no deserto"):</b>
        /// esta função criava <c>new MainInventory</c> e <c>new EquipmentInventory</c>. Todo
        /// mundo que já tinha assinado <c>OnSlotChanged</c>/<c>OnEquipmentChanged</c> —
        /// <c>MaoFisicaBridge</c>, <c>GerenciadorEfeitosPassivos</c>, <c>BarraDeItens</c>,
        /// <c>PainelDeInventario</c> — continuava escutando o objeto <b>antigo</b>. Na prática:
        /// ao trocar de cena, equipar deixava de chegar à Mão Física, e Damião ficava desarmado
        /// mesmo com a arma na mochila. Nada disso dava erro no console.</para>
        ///
        /// <para>Mutar no lugar preserva os inscritos. Instância nova só quando a capacidade
        /// salva não bate com a atual — e aí o aviso deixa claro que os eventos se perderam.</para>
        /// </summary>
        public void LoadFromSaveData(InventorySaveData data)
        {
            if (data == null) return;

            Main = RestaurarMochila(data);
            Equipment = RestaurarEquipamento(data);
        }

        private MainInventory RestaurarMochila(InventorySaveData data)
        {
            var mochila = Main;

            if (mochila.Capacidade != data.mainSlotData.Length)
            {
                Debug.LogWarning($"[InventoryManager] Mochila do save tem " +
                                 $"{data.mainSlotData.Length} slots e a atual tem {mochila.Capacidade}; " +
                                 "recriando — quem já escutava os eventos vai parar de receber.");
                mochila = new MainInventory(data.mainSlotData.Length);
            }
            else
            {
                mochila.LimparTudo();
            }

            for (int i = 0; i < data.mainSlotData.Length; i++)
            {
                var slot = data.mainSlotData[i];
                if (slot == null || string.IsNullOrEmpty(slot.itemDefId)) continue;

                mochila.AddAt(new ItemInstance(slot.itemDefId, slot.quantity), i);
            }

            return mochila;
        }

        private EquipmentInventory RestaurarEquipamento(InventorySaveData data)
        {
            var equipamento = Equipment;

            if (equipamento.Capacidade != data.equipSlotData.Length)
            {
                Debug.LogWarning($"[InventoryManager] Anatomia do save tem " +
                                 $"{data.equipSlotData.Length} slots e a atual tem " +
                                 $"{equipamento.Capacidade}; recriando.");
                equipamento = new EquipmentInventory(anatomia);
            }
            else
            {
                equipamento.LimparTudo();
            }

            for (int i = 0; i < data.equipSlotData.Length; i++)
            {
                var slot = data.equipSlotData[i];
                if (slot == null || string.IsNullOrEmpty(slot.itemDefId)) continue;

                equipamento.Equip(new ItemInstance(slot.itemDefId, slot.quantity), i);
            }

            return equipamento;
        }
    }
}
