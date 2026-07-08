namespace FavelaAmarela.Core.Abilities
{
    /// <summary>
    /// Resultado imutável de uma execução de <see cref="IArma"/>.
    /// </summary>
    public readonly struct ArmaResult
    {
        public readonly bool Success;
        public readonly float DurationSeconds;
        public readonly float CooldownSeconds;

        /// <summary>Se este golpe específico atordoou o alvo (nem toda arma usa isso).</summary>
        public readonly bool Atordoou;

        /// <summary>Duração do atordoamento, se <see cref="Atordoou"/> for true.</summary>
        public readonly float DuracaoAtordoamento;

        public ArmaResult(bool success, float durationSeconds, float cooldownSeconds, bool atordoou = false, float duracaoAtordoamento = 0f)
        {
            Success = success;
            DurationSeconds = durationSeconds;
            CooldownSeconds = cooldownSeconds;
            Atordoou = atordoou;
            DuracaoAtordoamento = duracaoAtordoamento;
        }
    }

    /// <summary>
    /// Contrato para armas físicas equipadas na Mão Física de Damião — mundanas,
    /// sem custo de Resiliência Mental (diferente de <see cref="IAnomalyPower"/>,
    /// que é pra Mão Anômala). Cada família de arma implementa isso e define seu
    /// próprio "verbo de combate": a Barra Enferrujada atordoa por chance, a
    /// Lâmina do Sinal bonifica ataque furtivo, etc.
    /// </summary>
    public interface IArma
    {
        /// <summary>Nome diegético da arma.</summary>
        string NomeDaArma { get; }

        /// <summary>Só valida cooldown — arma física não tem custo de recurso.</summary>
        bool CanActivate(float timeSinceLastUse);

        ArmaResult Execute();
    }
}
