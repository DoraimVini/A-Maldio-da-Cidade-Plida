// Assets/Scripts/Inventario/ItemInstance.cs
using System;
using UnityEngine;

namespace FavelaAmarela.Inventario
{
    /// <summary>
    /// Representa uma instância concreta de um item no inventário.
    /// Guarda apenas a referência ao ItemDef (via GUID) e a quantidade atual.
    /// Todos os modificadores são obtidos do ItemDef, não há aleatoriedade.
    /// </summary>
    [Serializable]
    public class ItemInstance
    {
        public string ItemDefId;
        public int Quantidade;

        public ItemInstance(string itemDefId, int quantidade = 1)
        {
            ItemDefId = itemDefId;
            Quantidade = Math.Max(1, quantidade);
        }

        /// <summary>
        /// Acesso conveniente à definição do item via singleton.
        /// </summary>
        public ItemDef Def
        {
            get
            {
                if (ItemDatabase.Instance == null)
                {
                    Debug.LogError("[ItemInstance] ItemDatabase.Instance é null. Certifique-se de que o prefab está na cena.");
                    return null;
                }
                return ItemDatabase.Instance.Get(ItemDefId);
            }
        }

        /// <summary>
        /// Resolve o ItemDef usando um database específico (para injeção de dependência).
        /// </summary>
        public ItemDef GetDef(ItemDatabase database)
        {
            return database != null ? database.Get(ItemDefId) : null;
        }

        /// <summary>
        /// Cria uma cópia profunda (útil para transferências entre inventários).
        /// </summary>
        public ItemInstance Clone()
        {
            return new ItemInstance(ItemDefId, Quantidade);
        }
    }
}
