namespace FavelaAmarela.Core.Loot
{
    /// <summary>
    /// Uma linha de tabela de drop, já projetada para o Core: identifica o item por
    /// <b>id</b>, nunca pelo asset, porque o Core não conhece <c>ScriptableObject</c>.
    ///
    /// <para>O sorteio decide <b>qual</b> item cai — nunca gera atributos. Todo item segue
    /// autorado à mão e determinístico; é essa invariante que impede a explosão de build.</para>
    /// </summary>
    public readonly struct CandidatoDeDrop
    {
        /// <summary>Id do <c>ItemDef</c> que esta entrada pode largar.</summary>
        public readonly string ItemDefId;

        /// <summary>Quanto de Carcosa impregnou o item.</summary>
        public readonly GrauDeImpregnacao Grau;

        /// <summary>Sempre cai, sem passar por chance nem pelo gate de nível.</summary>
        public readonly bool Garantido;

        /// <summary>Probabilidade em [0, 1]. Também serve de peso no sorteio de item único.</summary>
        public readonly float Chance;

        /// <summary>Quantidade mínima entregue quando a entrada cai.</summary>
        public readonly int QuantidadeMin;

        /// <summary>Quantidade máxima entregue quando a entrada cai.</summary>
        public readonly int QuantidadeMax;

        /// <summary>
        /// Nível de Exposição a partir do qual esta entrada passa a ser elegível.
        /// É o que faz os graus altos aparecerem só conforme Damião se aprofunda.
        /// </summary>
        public readonly int NivelMinimo;

        /// <summary>Monta uma entrada de tabela já normalizada (chance clampada, quantidades saneadas).</summary>
        public CandidatoDeDrop(string itemDefId, GrauDeImpregnacao grau, bool garantido, float chance,
            int quantidadeMin, int quantidadeMax, int nivelMinimo)
        {
            ItemDefId = itemDefId;
            Grau = grau;
            Garantido = garantido;

            if (chance < 0f) chance = 0f;
            else if (chance > 1f) chance = 1f;
            Chance = chance;

            if (quantidadeMin < 1) quantidadeMin = 1;
            if (quantidadeMax < quantidadeMin) quantidadeMax = quantidadeMin;
            QuantidadeMin = quantidadeMin;
            QuantidadeMax = quantidadeMax;

            NivelMinimo = nivelMinimo < 1 ? 1 : nivelMinimo;
        }
    }
}
