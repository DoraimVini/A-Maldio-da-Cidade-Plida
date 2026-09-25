namespace FavelaAmarela.Core.Loot
{
    /// <summary>
    /// Quanto de Carcosa já entrou no objeto — a "raridade" diegética do jogo.
    /// Não mede qualidade de loot: mede contaminação. O que o jogador percebe como
    /// item raro é, na verdade, um objeto que a Cidade Pálida marcou mais fundo.
    /// </summary>
    public enum GrauDeImpregnacao
    {
        /// <summary>Matéria comum, que Carcosa ainda não tocou.</summary>
        Inerte = 0,

        /// <summary>Carrega o Sinal em algum canto.</summary>
        Marcado = 1,

        /// <summary>Saturado — o objeto já não é bem um objeto.</summary>
        Impregnado = 2,

        /// <summary>Peça única e nomeada, com história própria. Nunca sorteada em tabela genérica.</summary>
        Reliquia = 3
    }
}
