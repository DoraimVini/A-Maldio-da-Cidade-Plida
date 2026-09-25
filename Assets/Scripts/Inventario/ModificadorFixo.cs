// Assets/Scripts/Inventario/ModificadorFixo.cs
using System;

namespace FavelaAmarela.Inventario
{
    /// <summary>
    /// Representa um bônus estático definido no ItemDef.
    /// Exemplo: "+20 de Vit Máxima" ou "+15% de Trauma Físico".
    /// O campo Valor pode ser absoluto ou percentual, interpretado pelo StatType.
    /// </summary>
    [Serializable]
    public struct ModificadorFixo
    {
        public StatType Stat;
        public float Valor;

        public ModificadorFixo(StatType stat, float valor)
        {
            Stat = stat;
            Valor = valor;
        }
    }
}
