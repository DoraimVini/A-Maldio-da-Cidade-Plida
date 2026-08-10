// Assets/Scripts/Inventario/MainInventory.cs
using System;
using UnityEngine;

namespace FavelaAmarela.Inventario
{
    /// <summary>
    /// A Mochila. Tamanho travado para reforçar a escassez do survival horror,
    /// mas permite ajuste no Inspector se necessário para balanceamento.
    /// Não tem regras complexas além do limite de capacidade da BaseInventory.
    /// </summary>
    [Serializable]
    public class MainInventory : BaseInventory
    {
        public const int DefaultCapacidadeSurvivalHorror = 12;

        [SerializeField] private int capacidadeConfigurada = DefaultCapacidadeSurvivalHorror;

        public MainInventory() : base(DefaultCapacidadeSurvivalHorror)
        {
        }

        public MainInventory(int capacidade) : base(capacidade)
        {
            capacidadeConfigurada = capacidade;
        }
    }
}
