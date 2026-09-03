namespace FavelaAmarela.Core.Abilities
{
    /// <summary>
    /// Mão Vazia — o golpe desarmado de Damião. Não é arte marcial: é instinto de
    /// sobrevivência de quem não achou nada na Tumba ainda.
    ///
    /// <b>Causa dano zero por decisão de design</b> (não é placeholder esquecido): o gesto
    /// existe para o jogador aprender o verbo de combate e para fazer barulho — matar exige
    /// uma das armas da Tumba (Maça de Aklo, Estilete de Irem, Alfanje de Alhazred). É
    /// rápido de propósito, para não parecer que "não funcionou".
    ///
    /// Implementa apenas <see cref="IArma"/>, não <see cref="IArmaComHabilidade"/> — sem
    /// arma não há habilidade em botão separado.
    /// </summary>
    public sealed class MaoVazia : IArma
    {
        public string NomeDaArma => "Mão Vazia";

        private readonly float duracao;
        private readonly float cooldown;

        /// <param name="duracao">Duração do gesto, em segundos.</param>
        /// <param name="cooldown">Intervalo mínimo entre golpes, em segundos.</param>
        public MaoVazia(float duracao = 0.2f, float cooldown = 0.25f)
        {
            this.duracao = duracao;
            this.cooldown = cooldown;
        }

        /// <summary>
        /// O punho: dano zero, precisão cheia. O gesto desarmado faz barulho e prende o ator no
        /// estado Atacando, mas não mata — e não pode "errar", porque errar um golpe que já não
        /// causa dano só confundiria o jogador.
        /// </summary>
        public Combat.PerfilDeArma Perfil => Combat.PerfilDeArma.Desarmado;

        public bool CanActivate(float timeSinceLastUse) => timeSinceLastUse >= cooldown;

        /// <summary>Executa o gesto desarmado. <c>Dano</c> é sempre 0.</summary>
        public ArmaResult Execute() => new ArmaResult(
            success: true, durationSeconds: duracao, cooldownSeconds: cooldown,
            dano: 0f);
    }
}
