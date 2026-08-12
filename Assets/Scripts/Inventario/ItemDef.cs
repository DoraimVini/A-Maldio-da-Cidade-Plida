// Assets/Scripts/Inventario/ItemDef.cs
using System.Collections.Generic;
using UnityEngine;
using FavelaAmarela.Core.Abilities;

namespace FavelaAmarela.Inventario
{
    /// <summary>
    /// Definição base imutável de um item (ScriptableObject).
    /// Todos os status são fixos e determinísticos.
    /// </summary>
    [CreateAssetMenu(menuName = "Favela Amarela/ItemDef", fileName = "Item_")]
    public class ItemDef : ScriptableObject
    {
        [Header("Identidade")]
        [Tooltip("Identificador único (GUID gerado automaticamente).")]
        public string Id;

        public string Nome;
        public Sprite Icone;

        [TextArea(2, 4)]
        public string Descricao;

        [Header("Classificação")]
        public ItemType Tipo;
        public EquipmentSlot SlotEquipamento = EquipmentSlot.Nenhum;

        [Header("Empilhamento")]
        [Tooltip("1 = não empilhável (armas, armaduras). >1 = empilhável (consumíveis).")]
        public int EmpilhamentoMaximo = 1;

        [Header("Combate (Apenas Armas)")]
        [Tooltip("A fábrica de C# usará este ID para gerar o dano verdadeiro da arma.")]
        public TipoArmaFisica ArmaFisica = TipoArmaFisica.MaoVazia;

        [Tooltip("Quantas mãos a arma toma. DuasMaos bloqueia o slot de Mão Secundária " +
                 "enquanto estiver equipada. Ignorado em itens que não são armas.")]
        public Empunhadura Empunhadura = Empunhadura.UmaMao;

        [Header("Atributos Fixos")]
        [Tooltip("Lista de modificadores que este item concede (quando equipado).")]
        public List<ModificadorFixo> Modificadores = new List<ModificadorFixo>();

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(Id))
            {
                Id = System.Guid.NewGuid().ToString();
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}
