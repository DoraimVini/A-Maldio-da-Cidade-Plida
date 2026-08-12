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

        /// <summary>
        /// Índice do slot de um dado tipo na anatomia, ou -1 se esta anatomia não o tem.
        /// Existe para que as regras de empunhadura não dependam de índices mágicos: a
        /// ordem da anatomia é autorada no Inspector e pode mudar.
        /// </summary>
        public int IndiceDoSlot(EquipmentSlot tipo)
        {
            for (int i = 0; i < slotTiposPermitidos.Length; i++)
            {
                if (slotTiposPermitidos[i] == tipo) return i;
            }
            return -1;
        }

        /// <summary>
        /// Se a Mão principal empunha uma arma de <see cref="Empunhadura.DuasMaos"/> —
        /// caso em que a Mão Secundária está tomada e não aceita nada.
        /// </summary>
        public bool ArmaDeDuasMaosEquipada
        {
            get
            {
                int indiceArma = IndiceDoSlot(EquipmentSlot.Arma);
                if (indiceArma < 0) return false;

                var arma = GetSlot(indiceArma);
                return arma?.Def != null
                    && arma.Def.Tipo == ItemType.Arma
                    && arma.Def.Empunhadura == Empunhadura.DuasMaos;
            }
        }

        /// <summary>Se há algo ocupando a Mão Secundária.</summary>
        public bool MaoSecundariaOcupada
        {
            get
            {
                int indice = IndiceDoSlot(EquipmentSlot.MaoSecundaria);
                return indice >= 0 && GetSlot(indice)?.Def != null;
            }
        }

        /// <summary>
        /// Além da validação de tipo e de encaixe, aplica as <b>regras de empunhadura</b>:
        /// nada entra na Mão Secundária enquanto uma arma de duas mãos estiver empunhada, e
        /// uma arma de duas mãos não entra na Mão principal com a secundária ocupada.
        ///
        /// <para>A segunda regra é recusa, não desalojamento automático: liberar a off-hand
        /// exige devolver aquele item à mochila, e a mochila pode estar cheia. Quem sabe
        /// lidar com isso é o <see cref="InventoryManager"/>, que orquestra os dois
        /// contêineres — este POCO só diz sim ou não.</para>
        /// </summary>
        public override bool CanAdd(ItemInstance item, int indice)
        {
            if (!base.CanAdd(item, indice)) return false;

            // Rejeita itens que não são equipamentos
            if (item.Def.Tipo != ItemType.Arma && item.Def.Tipo != ItemType.Armadura && item.Def.Tipo != ItemType.Amuleto)
                return false;

            // Valida o slot de encaixe (não pode calçar chapéu no pé)
            if (item.Def.SlotEquipamento != slotTiposPermitidos[indice])
                return false;

            var destino = slotTiposPermitidos[indice];

            if (destino == EquipmentSlot.MaoSecundaria && ArmaDeDuasMaosEquipada)
                return false;

            if (destino == EquipmentSlot.Arma
                && item.Def.Tipo == ItemType.Arma
                && item.Def.Empunhadura == Empunhadura.DuasMaos
                && MaoSecundariaOcupada)
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
