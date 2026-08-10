namespace FavelaAmarela.Core.Enemies
{
    /// <summary>
    /// Estados da luta contra Abdul Alhazred, a Aparição Primordial da Tumba.
    /// Ver <c>Docs/KnowledgeBundle/lore/abdul_alhazred.md</c>.
    /// </summary>
    public enum AbdulState
    {
        /// <summary>
        /// Pré-luta: flutua em transe murmurando Aklo. Não ataca e não pode ser ferido —
        /// a luta só começa quando Damião interage com o grimório.
        /// </summary>
        Transe,

        /// <summary>
        /// Fase 1 (100%→35%): escudo impenetrável sustentado pelas Pedras de Poder.
        /// Invoca esqueletos. Só fica vulnerável quando uma Pedra é quebrada.
        /// </summary>
        Fase1,

        /// <summary>
        /// Fase 2 (&lt;35%): escudo permanente (não depende mais das Pedras). Conjura
        /// Cones de Gelo e esqueletos, gastando "mana".
        /// </summary>
        Fase2,

        /// <summary>
        /// Mana esgotada após o ciclo de conjurações: o escudo cai e esta é a única
        /// janela para o golpe de misericórdia.
        /// </summary>
        Exausto,

        /// <summary>Abatido — dropa o Necronomicon.</summary>
        Derrotado
    }
}
