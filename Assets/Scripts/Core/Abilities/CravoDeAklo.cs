namespace FavelaAmarela.Core.Abilities
{
    /// <summary>
    /// Cravo de Aklo — estaca ritual inscrita com glifos Aklo, usada pelos guardiões
    /// da Tumba de Alhazred para <em>fincar</em> o que o Necronomicon invocava. Arma
    /// da Mão Física (mundana, sem custo de Resiliência).
    ///
    /// Ataque básico: dano modesto. Habilidade "Fincar o Aklo": <b>interrompe a
    /// canalização anômala</b> do alvo — corta as conjurações do Abdul (ventos
    /// congelantes, círculos de dreno de RM) se acertar na janela de conjuração.
    /// </summary>
    public sealed class CravoDeAklo : IArmaComHabilidade
    {
        public string NomeDaArma => "Cravo de Aklo";
        public string NomeHabilidade => "Fincar o Aklo";

        private readonly float duracaoBasico;
        private readonly float cooldownBasico;
        private readonly float danoBasico;

        private readonly float duracaoHabilidade;
        private readonly float cooldownHabilidade;
        private readonly float danoHabilidade;

        public CravoDeAklo(
            float duracaoBasico = 0.35f, float cooldownBasico = 0.5f, float danoBasico = 8f,
            float duracaoHabilidade = 0.4f, float cooldownHabilidade = 6f, float danoHabilidade = 6f)
        {
            this.duracaoBasico = duracaoBasico;
            this.cooldownBasico = cooldownBasico;
            this.danoBasico = danoBasico;
            this.duracaoHabilidade = duracaoHabilidade;
            this.cooldownHabilidade = cooldownHabilidade;
            this.danoHabilidade = danoHabilidade;
        }

        public bool CanActivate(float timeSinceLastUse) => timeSinceLastUse >= cooldownBasico;

        public ArmaResult Execute() => new ArmaResult(
            success: true, durationSeconds: duracaoBasico, cooldownSeconds: cooldownBasico,
            dano: danoBasico);

        public bool CanActivateHabilidade(float timeSinceLastAbilityUse) => timeSinceLastAbilityUse >= cooldownHabilidade;

        public ArmaResult ExecuteHabilidade() => new ArmaResult(
            success: true, durationSeconds: duracaoHabilidade, cooldownSeconds: cooldownHabilidade,
            dano: danoHabilidade, interrompeConjuracao: true);
    }
}
