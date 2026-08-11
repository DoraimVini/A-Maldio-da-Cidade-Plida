// Assets/Scripts/Inventario/EntradaDeDrop.cs
using System;
using UnityEngine;
using FavelaAmarela.Core.Loot;

namespace FavelaAmarela.Inventario
{
    /// <summary>
    /// Uma linha autorada de <see cref="TabelaDeDrop"/>: qual item pode cair, com que
    /// chance, em que quantidade e a partir de que nível de Exposição.
    /// </summary>
    [Serializable]
    public class EntradaDeDrop
    {
        [Tooltip("Qual item esta linha pode largar. [ASSET]")]
        public ItemDef Item;

        [Tooltip("Quanto de Carcosa impregnou o item.")]
        public GrauDeImpregnacao Grau = GrauDeImpregnacao.Inerte;

        [Tooltip("Sempre cai — ignora a chance e o nível mínimo. Para drop roteirizado de chefe.")]
        public bool Garantido;

        [Range(0f, 1f)]
        [Tooltip("Probabilidade de cair. Também serve de peso quando a fonte sorteia um item único.")]
        public float Chance = 0.1f;

        [Min(1)]
        public int QuantidadeMin = 1;

        [Min(1)]
        public int QuantidadeMax = 1;

        [Min(1)]
        [Tooltip("Nível de Exposição a partir do qual esta linha passa a ser elegível.")]
        public int NivelMinimo = 1;
    }
}
