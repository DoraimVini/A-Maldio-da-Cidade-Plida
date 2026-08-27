namespace FavelaAmarela.Core.Loot
{
    /// <summary>
    /// O resultado de um sorteio: qual item caiu, quantos, e <b>com que grau</b>.
    ///
    /// <para><b>O grau entrou aqui em 2026-08-27, e a falta dele era um buraco real.</b> Ele era
    /// autorado em <c>EntradaDeDrop</c>, projetado até <c>CandidatoDeDrop</c> e então
    /// <b>descartado</b> — este struct só carregava id e quantidade. Consequência: o
    /// <c>Grau: 3</c> (Relíquia) do espólio do Byakhee não afetava absolutamente nada, e a
    /// "raridade" existia só no Inspector.</para>
    /// </summary>
    public readonly struct ItemSorteado
    {
        /// <summary>Id do <c>ItemDef</c> que serve de base.</summary>
        public readonly string ItemDefId;

        /// <summary>Quantos caíram.</summary>
        public readonly int Quantidade;

        /// <summary>Quanto de Carcosa impregnou este exemplar — decide quantos afixos ele rola.</summary>
        public readonly GrauDeImpregnacao Grau;

        /// <summary>Cria um resultado de sorteio.</summary>
        public ItemSorteado(string itemDefId, int quantidade,
                            GrauDeImpregnacao grau = GrauDeImpregnacao.Inerte)
        {
            ItemDefId = itemDefId;
            Quantidade = quantidade < 1 ? 1 : quantidade;
            Grau = grau;
        }
    }
}
