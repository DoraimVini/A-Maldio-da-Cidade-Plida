// Assets/Scripts/Inventario/BaseInventory.cs
using System;
using UnityEngine;

namespace FavelaAmarela.Inventario
{
    /// <summary>
    /// Classe base para contêineres de inventário.
    /// Gerencia um array fixo, empilhamento e emissão de eventos.
    /// Serializável nativamente para facilitar o save.
    /// </summary>
    [Serializable]
    public class BaseInventory
    {
        [SerializeField] protected ItemInstance[] slots;
        
        public int Capacidade => slots.Length;

        // Evento disparado quando um slot muda. Passa o índice do slot.
        public event Action<int> OnSlotChanged;

        public BaseInventory(int capacidade)
        {
            slots = new ItemInstance[Math.Max(1, capacidade)];
        }

        public ItemInstance GetSlot(int indice)
        {
            if (indice < 0 || indice >= slots.Length) return null;
            return slots[indice];
        }

        /// <summary>
        /// Esvazia todos os slots <b>sem trocar de instância</b>, notificando cada um.
        ///
        /// <para>Existe para o carregamento de save: recriar o inventário com <c>new</c>
        /// deixaria órfãos todos os inscritos em <see cref="OnSlotChanged"/> — a
        /// <c>MaoFisicaBridge</c>, o <c>GerenciadorEfeitosPassivos</c>, as barras de UI —
        /// que passariam a escutar um objeto morto e nunca mais saberiam de nada.</para>
        /// </summary>
        public void LimparTudo()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;

                slots[i] = null;
                OnSlotChanged?.Invoke(i);
            }
        }

        /// <summary>
        /// Validação virtual. Pode ser sobrescrita para regras específicas.
        /// </summary>
        public virtual bool CanAdd(ItemInstance item, int indice)
        {
            if (item == null || item.Def == null) return false;
            if (indice < 0 || indice >= slots.Length) return false;
            
            var atual = slots[indice];
            if (atual == null) return true;
            
            // Só pode somar se for o mesmo item e não tiver atingido o limite
            return atual.ItemDefId == item.ItemDefId && atual.Quantidade < atual.Def.EmpilhamentoMaximo;
        }

        /// <summary>
        /// Verifica se há qualquer slot capaz de receber o item.
        /// Útil para a UI desabilitar o botão "Pegar".
        /// </summary>
        public bool CanAddAny(ItemInstance item)
        {
            if (item == null || item.Def == null) return false;
            for (int i = 0; i < slots.Length; i++)
            {
                if (CanAdd(item, i)) return true;
            }
            return false;
        }

        /// <summary>
        /// Tenta adicionar no inventário: primeiro empilhando, depois em slots vazios.
        /// Retorna true se TUDO foi adicionado. Se sobrar, retorna false e muta o 'item'.
        /// </summary>
        public virtual bool Add(ItemInstance item)
        {
            if (item == null || item.Def == null || item.Quantidade <= 0) return false;

            // 1. Tentar empilhar em slots existentes
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null && slots[i].ItemDefId == item.ItemDefId && slots[i].Quantidade < slots[i].Def.EmpilhamentoMaximo)
                {
                    AddAt(item, i);
                    if (item.Quantidade <= 0) return true;
                }
            }

            // 2. Procurar o primeiro slot vazio
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                {
                    AddAt(item, i);
                    if (item.Quantidade <= 0) return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Adiciona (ou empilha) em um slot específico. Muta a quantidade do 'item' injetado.
        /// </summary>
        public virtual bool AddAt(ItemInstance item, int indice)
        {
            if (!CanAdd(item, indice)) return false;

            var atual = slots[indice];

            if (atual == null)
            {
                // Clona para que o chamador não mantenha referência à instância interna
                slots[indice] = item.Clone(); 
                item.Quantidade = 0;
                OnSlotChanged?.Invoke(indice);
                return true;
            }
            
            int espacoLivre = atual.Def.EmpilhamentoMaximo - atual.Quantidade;
            if (espacoLivre >= item.Quantidade)
            {
                atual.Quantidade += item.Quantidade;
                item.Quantidade = 0;
            }
            else
            {
                atual.Quantidade += espacoLivre;
                item.Quantidade -= espacoLivre;
            }

            OnSlotChanged?.Invoke(indice);
            return item.Quantidade == 0;
        }

        public virtual ItemInstance Remove(int indice, int quantidade = 1)
        {
            var atual = GetSlot(indice);
            if (atual == null) return null;

            if (atual.Quantidade <= quantidade)
            {
                slots[indice] = null;
                OnSlotChanged?.Invoke(indice);
                return atual;
            }

            atual.Quantidade -= quantidade;
            OnSlotChanged?.Invoke(indice);
            
            return new ItemInstance(atual.ItemDefId, quantidade);
        }

        /// <summary>
        /// Troca os itens de dois slots. Se forem o mesmo item, tenta empilhar (merge).
        /// </summary>
        public virtual void Swap(int indiceA, int indiceB)
        {
            if (indiceA < 0 || indiceA >= slots.Length || indiceB < 0 || indiceB >= slots.Length) return;

            var itemA = slots[indiceA];
            var itemB = slots[indiceB];

            // Tenta dar merge se forem iguais (poderia ser uma lógica à parte, mas simplifica o arrastar da UI)
            if (itemA != null && itemB != null && itemA.ItemDefId == itemB.ItemDefId)
            {
                AddAt(itemA, indiceB); // Joga A no B
                if (itemA.Quantidade <= 0) 
                {
                    slots[indiceA] = null;
                    OnSlotChanged?.Invoke(indiceA);
                    return;
                }
            }

            // Swap puro
            slots[indiceA] = itemB;
            slots[indiceB] = itemA;

            OnSlotChanged?.Invoke(indiceA);
            OnSlotChanged?.Invoke(indiceB);
        }
    }
}
