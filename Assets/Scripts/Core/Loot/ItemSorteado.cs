namespace FavelaAmarela.Core.Loot
{
    /// <summary>
    /// Resultado de uma linha de tabela que caiu: id do item e quantos exemplares.
    /// É dado puro — quem materializa o coletável no mundo é a camada Runtime.
    /// </summary>
    public readonly struct ItemSorteado
    {
        /// <summary>Id do <c>ItemDef</c> sorteado.</summary>
        public readonly string ItemDefId;

        /// <summary>Quantos exemplares caíram.</summary>
        public readonly int Quantidade;

        /// <summary>Monta o resultado de um sorteio.</summary>
        public ItemSorteado(string itemDefId, int quantidade)
        {
            ItemDefId = itemDefId;
            Quantidade = quantidade < 1 ? 1 : quantidade;
        }
    }
}
