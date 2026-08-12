// Assets/Scripts/Inventario/EquipmentInventory.cs
using System;
using UnityEngine;

namespace FavelaAmarela.Inventario
{
    /// <summary>
    /// Contêiner estrito para equipamentos. 
    /// O array 'slots' é mapeado um-para-um com o array 'slotTiposPermitidos'.
    /// </summary>
    [Serializable]
    public class EquipmentInventory : BaseInventory
    {
        // Define qual o tipo esperado para cada índice (ex: 0 = Arma, 1 = Amuleto)
        [SerializeField] private EquipmentSlot[] slotTiposPermitidos;

        // Disparado sempre que qualquer equipamento é colocado ou retirado (para recalcular Ficha)
        public event Action OnEquipmentChanged;

        public EquipmentInventory(EquipmentSlot[] tiposEsperados) : base(tiposEsperados.Length)
        {
            slotTiposPermitidos = tiposEsperados;
        }

        public EquipmentSlot GetSlotType(int indice)
        {
            if (indice < 0 || indice >= slotTiposPermitidos.Length) return EquipmentSlot.Nenhum;
            return slotTiposPermitidos[indice];
        }

        public override bool CanAdd(ItemInstance item, int indice)
        {
            if (!base.CanAdd(item, indice)) return false;
            
            // Rejeita itens que não são equipamentos
            if (item.Def.Tipo != ItemType.Arma && item.Def.Tipo != ItemType.Armadura && item.Def.Tipo != ItemType.Amuleto)
                return false;

            // Valida o slot de encaixe (não pode calçar chapéu no pé)
            if (item.Def.SlotEquipamento != slotTiposPermitidos[indice])
                return false;

            return true;
        }

        /// <summary>
        /// Equipa um item no slot desejado, utilizando a mecânica padrão de AddAt. 
        /// Retorna o item antigo se já houvesse algo ocupando o espaço.
        /// </summary>
        public ItemInstance Equip(ItemInstance novoEquipamento, int indice)
        {
            if (novoEquipamento == null || !CanAdd(novoEquipamento, indice))
            {
                Debug.LogWarning("[EquipmentInventory] Falha ao equipar: item nulo ou slot inválido.");
                return null;
            }

            ItemInstance itemAntigo = Remove(indice, 1); // Remove o que estava lá
            AddAt(novoEquipamento, indice); // Adiciona o novo (com clone e evento OnSlotChanged interno)
            
            OnEquipmentChanged?.Invoke();
            return itemAntigo;
        }

        /// <summary>
        /// Tenta equipar o item no primeiro slot compatível com o seu tipo.
        /// </summary>
        public ItemInstance Equip(ItemInstance novoEquipamento)
        {
            if (novoEquipamento == null || novoEquipamento.Def == null) return null;
            
            for (int i = 0; i < slotTiposPermitidos.Length; i++)
            {
                if (slotTiposPermitidos[i] == novoEquipamento.Def.SlotEquipamento)
                {
                    return Equip(novoEquipamento, i);
                }
            }
            return null;
        }

        /// <summary>
        /// Desequipa o slot. Retorna o item que foi retirado.
        /// </summary>
        public ItemInstance Unequip(int indice)
        {
            ItemInstance retirado = Remove(indice, 1);
            if (retirado != null)
            {
                OnEquipmentChanged?.Invoke();
            }
            return retirado;
        }
    }
}
