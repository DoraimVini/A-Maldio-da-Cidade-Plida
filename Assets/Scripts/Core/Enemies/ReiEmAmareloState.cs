namespace FavelaAmarela.Core.Enemies
{
    /// <summary>
    /// Estados do confronto final contra o Rei em Amarelo, no Trono de Aldebaran.
    /// Ver <c>Docs/KnowledgeBundle/systems/level_design_castelo_carcosa.md</c> §Z5.
    ///
    /// <para><b>Não é uma luta de dano.</b> "Não há barra de vida" (design doc, literal) — é
    /// um rito de duas metades: primeiro reunir as Relíquias pela arena, depois sobreviver ao
    /// Rei se desvelando repetidas vezes, dando as costas a tempo. Ficar de frente na hora
    /// errada mata na hora — não existe barra para absorver o erro.</para>
    /// </summary>
    public enum ReiEmAmareloState
    {
        /// <summary>Antes do confronto começar. Nada acontece.</summary>
        Aguardando,

        /// <summary>
        /// O jogador percorre a arena ativando as Relíquias nos pontos focais. O Rei observa,
        /// mas não se desvela ainda — esta metade é exploração, não reação.
        /// </summary>
        AtivandoReliquias,

        /// <summary>
        /// Todas as Relíquias ativas: o rito de selamento está em curso. O Rei desvela o
        /// rosto em ciclos, de propósito, cada um uma chance de morrer instantaneamente.
        /// </summary>
        Selando,

        /// <summary>
        /// Um desvelar está em andamento: a janela de reação está aberta. Ficar de frente ao
        /// fim da janela é Colapso instantâneo; dar as costas a tempo sobrevive o ciclo.
        /// </summary>
        Desvelado,

        /// <summary>Vitória — o rito de selamento se completa.</summary>
        Selado,

        /// <summary>Derrota — o jogador foi visto de frente durante um desvelar.</summary>
        Colapso
    }
}
