// Assets/Scripts/Inventario/InventorySaveData.cs
using System;
using System.Collections.Generic;
using FavelaAmarela.Core.Loot;

namespace FavelaAmarela.Inventario
{
    /// <summary>
    /// Um slot gravado. A partir da v2 carrega o que <b>este exemplar</b> rolou, não só qual
    /// item ele é.
    /// </summary>
    [Serializable]
    public class ItemSlotData
    {
        /// <summary>Id do <c>ItemDef</c> que serve de BASE. Continua sendo a chave do catálogo.</summary>
        public string itemDefId;

        /// <summary>Quantos.</summary>
        public int quantity;

        /// <summary>v2: quanto de Carcosa entrou neste exemplar.</summary>
        public GrauDeImpregnacao grau = GrauDeImpregnacao.Inerte;

        /// <summary>v2: nível do item, que governou o pool de afixos.</summary>
        public int nivelDoItem = 1;

        /// <summary>
        /// v2: os afixos com os <b>valores já rolados</b>.
        ///
        /// <para><b>Valores, nunca semente.</b> Gravar a semente e re-rolar na carga seria
        /// menor, e erraria feio: bastaria alguém editar um <c>AfixoDef</c> para toda arma já
        /// dropada mudar sozinha — o jogador veria o item da mochila dele ficar diferente sem
        /// ter feito nada. D2 e PoE gravam os mods pelo mesmo motivo.</para>
        /// </summary>
        public List<AfixoRolado> afixos = new List<AfixoRolado>();

        /// <summary>Construtor sem argumentos exigido por <c>JsonUtility</c>.</summary>
        public ItemSlotData() { }

        /// <summary>Grava um exemplar inteiro.</summary>
        public ItemSlotData(ItemInstance item)
        {
            if (item == null) return;

            itemDefId = item.ItemDefId;
            quantity = item.Quantidade;
            grau = item.Grau;
            nivelDoItem = item.NivelDoItem;

            if (item.Afixos != null)
                foreach (var a in item.Afixos)
                    if (a != null) afixos.Add(new AfixoRolado(a.AfixoId, a.Stat, a.Valor));
        }

        /// <summary>
        /// Reconstrói o exemplar. Um save v1 chega aqui com <c>grau</c> Inerte, nível 1 e sem
        /// afixos — que é exatamente o que os itens da v1 eram, então a leitura antiga
        /// continua correta sem caminho especial.
        /// </summary>
        public ItemInstance ParaInstancia()
        {
            if (string.IsNullOrEmpty(itemDefId)) return null;

            var item = new ItemInstance(itemDefId, quantity)
            {
                Grau = grau,
                NivelDoItem = nivelDoItem < 1 ? 1 : nivelDoItem,
            };

            if (afixos != null)
                foreach (var a in afixos)
                    if (a != null) item.Afixos.Add(new AfixoRolado(a.AfixoId, a.Stat, a.Valor));

            return item;
        }
    }

    /// <summary>
    /// O inventário inteiro, gravado.
    ///
    /// <para><b>Histórico de versões:</b> 0 = anatomia de 6 slots; 1 = 7 slots, com Mão
    /// Secundária; <b>2 = grau, nível do item e afixos rolados por exemplar</b>
    /// (2026-08-27).</para>
    ///
    /// <para><b>A v1 continua legível, e de graça.</b> <c>JsonUtility</c> deixa nos valores
    /// padrão os campos que não existem no JSON antigo — e os padrões escolhidos
    /// (<c>Inerte</c>, nível 1, sem afixos) descrevem com exatidão o que um item da v1 era.
    /// Não há caminho de migração para escrever nem para manter.</para>
    /// </summary>
    [Serializable]
    public class InventorySaveData
    {
        /// <summary>Versão do formato gravado.</summary>
        public const int VersaoAtual = 2;

        /// <summary>Versão com que ESTE save foi escrito.</summary>
        public int saveVersion = VersaoAtual;

        /// <summary>Mochila.</summary>
        public ItemSlotData[] mainSlotData;

        /// <summary>Corpo.</summary>
        public ItemSlotData[] equipSlotData;

        /// <summary>Construtor sem argumentos exigido por <c>JsonUtility</c>.</summary>
        public InventorySaveData() { }

        /// <summary>Fotografa mochila e equipamento.</summary>
        public InventorySaveData(MainInventory main, EquipmentInventory equip)
        {
            saveVersion = VersaoAtual;

            mainSlotData = new ItemSlotData[main.Capacidade];
            for (int i = 0; i < main.Capacidade; i++)
            {
                var item = main.GetSlot(i);
                if (item != null) mainSlotData[i] = new ItemSlotData(item);
            }

            equipSlotData = new ItemSlotData[equip.Capacidade];
            for (int i = 0; i < equip.Capacidade; i++)
            {
                var item = equip.GetSlot(i);
                if (item != null) equipSlotData[i] = new ItemSlotData(item);
            }
        }
    }
}
