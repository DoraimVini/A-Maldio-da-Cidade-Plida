// Assets/Scripts/Inventario/InventorySaveData.cs
using System;

namespace FavelaAmarela.Inventario
{
    [Serializable]
    public class ItemSlotData
    {
        public string itemDefId;
        public int quantity;

        public ItemSlotData(string id, int qtd)
        {
            itemDefId = id;
            quantity = qtd;
        }
    }

    [Serializable]
    public class InventorySaveData
    {
        public ItemSlotData[] mainSlotData;
        public ItemSlotData[] equipSlotData;

        public InventorySaveData(MainInventory main, EquipmentInventory equip)
        {
            mainSlotData = new ItemSlotData[main.Capacidade];
            for (int i = 0; i < main.Capacidade; i++)
            {
                var item = main.GetSlot(i);
                if (item != null)
                    mainSlotData[i] = new ItemSlotData(item.ItemDefId, item.Quantidade);
            }

            equipSlotData = new ItemSlotData[equip.Capacidade];
            for (int i = 0; i < equip.Capacidade; i++)
            {
                var item = equip.GetSlot(i);
                if (item != null)
                    equipSlotData[i] = new ItemSlotData(item.ItemDefId, item.Quantidade);
            }
        }
    }
}
