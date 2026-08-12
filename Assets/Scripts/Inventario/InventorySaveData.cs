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
        /// <summary>
        /// Versão corrente do formato. Suba isto sempre que a anatomia ou o significado dos
        /// índices mudar, e registre a mudança na tabela abaixo.
        ///
        /// <list type="bullet">
        ///   <item><b>0</b> — saves sem o campo (anteriores a 2026-08-12): anatomia de 6 slots,
        ///   sem Mão Secundária. O campo desserializa como 0 justamente por ausência.</item>
        ///   <item><b>1</b> — anatomia de 7 slots, com <c>EquipmentSlot.MaoSecundaria</c> no fim.</item>
        /// </list>
        /// </summary>
        public const int VersaoAtual = 1;

        /// <summary>
        /// Versão do formato deste save. Zero significa "save antigo, anterior ao campo" —
        /// é o valor que a desserialização produz quando a chave não existe no JSON.
        /// </summary>
        public int saveVersion = VersaoAtual;

        public ItemSlotData[] mainSlotData;
        public ItemSlotData[] equipSlotData;

        public InventorySaveData(MainInventory main, EquipmentInventory equip)
        {
            saveVersion = VersaoAtual;

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
