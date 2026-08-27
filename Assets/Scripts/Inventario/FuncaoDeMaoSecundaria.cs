namespace FavelaAmarela.Inventario
{
    /// <summary>
    /// O que um item faz quando ocupa a Mão Secundária.
    ///
    /// <para><b>O slot existia e não fazia nada.</b> Até 2026-08-27, <c>MaoSecundaria</c> era o
    /// índice 6 da anatomia e servia só de <b>regra</b>: uma arma <c>DuasMaos</c> o bloqueava.
    /// Não havia um único item autorado para ele — assim como para Amuleto e Anel, <b>três dos
    /// sete slots do corpo do Damião estavam vazios</b>.</para>
    ///
    /// <para>As duas funções são a escolha de <i>build</i> que o slot passa a oferecer:
    /// sobreviver mais, ou conjurar mais.</para>
    ///
    /// <para><b>Valor novo entra sempre no FIM</b> — este enum é serializado por índice nos
    /// <c>.asset</c>, mesma regra de <c>ItemType</c> e <c>EquipmentSlot</c>.</para>
    /// </summary>
    public enum FuncaoDeMaoSecundaria
    {
        /// <summary>Item comum: só os modificadores dele valem.</summary>
        Nenhuma,

        /// <summary>
        /// Escudo — chance de aparar o golpe. É <b>chance e não botão</b>: num isométrico com
        /// câmera afastada, segurar para aparar exige ler direção e tempo de um alvo que o
        /// jogador mal distingue. Ver <c>Core.Combat.Bloqueio</c>.
        /// </summary>
        Escudo,

        /// <summary>
        /// Foco — desconta recarga da habilidade da arma. É o lado "conjurar mais" da escolha.
        /// </summary>
        Foco,
    }
}
