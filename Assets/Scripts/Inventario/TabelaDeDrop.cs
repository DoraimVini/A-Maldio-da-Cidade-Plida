// Assets/Scripts/Inventario/TabelaDeDrop.cs
using System.Collections.Generic;
using UnityEngine;
using FavelaAmarela.Core.Loot;

namespace FavelaAmarela.Inventario
{
    /// <summary>
    /// O que uma fonte de espólio pode largar — um asset por arquétipo (Cultista, baú da
    /// Tumba, Nagaraja). É só dado autorado: a regra do sorteio vive no POCO
    /// <see cref="SorteioDeDrop"/>, e esta classe apenas projeta as entradas para ele.
    /// </summary>
    [CreateAssetMenu(menuName = "Favela Amarela/Tabela de Drop", fileName = "Drop_")]
    public class TabelaDeDrop : ScriptableObject
    {
        [Tooltip("Linhas desta tabela.")]
        [SerializeField] private List<EntradaDeDrop> entradas = new List<EntradaDeDrop>();

        [Tooltip("Máximo de itens por resolução. 0 = sem teto. Impede que um azarado vomite a tabela inteira.")]
        [Min(0)]
        [SerializeField] private int tetoDeItens = 2;

        /// <summary>Máximo de itens que esta fonte entrega numa única resolução.</summary>
        [Tooltip("Nível DOS ITENS que esta tabela larga. Governa que afixos podem cair. " +
                 "É do item, não do jogador: comparar com o nível do jogador faria uma zona " +
                 "inicial dropar tier máximo assim que ele subisse.")]
        [Min(1)]
        [SerializeField] private int nivelDoItem = 1;

        public int TetoDeItens => tetoDeItens;

        /// <summary>Nível dos itens desta tabela — o gate do pool de afixos.</summary>
        public int NivelDoItem => nivelDoItem < 1 ? 1 : nivelDoItem;

        /// <summary>
        /// Converte as entradas autoradas em candidatos do Core, trocando a referência de
        /// asset pelo id do item. Linhas sem item ou sem id são ignoradas com aviso.
        /// </summary>
        public List<CandidatoDeDrop> ProjetarCandidatos()
        {
            var candidatos = new List<CandidatoDeDrop>();
            if (entradas == null) return candidatos;

            foreach (var entrada in entradas)
            {
                if (entrada == null) continue;

                if (entrada.Item == null || string.IsNullOrEmpty(entrada.Item.Id))
                {
                    Debug.LogWarning($"[TabelaDeDrop] '{name}' tem uma linha sem ItemDef válido — ignorada.", this);
                    continue;
                }

                candidatos.Add(new CandidatoDeDrop(
                    entrada.Item.Id,
                    entrada.Grau,
                    entrada.Garantido,
                    entrada.Chance,
                    entrada.QuantidadeMin,
                    entrada.QuantidadeMax,
                    entrada.NivelMinimo));
            }

            return candidatos;
        }
    }
}
