namespace FavelaAmarela.Core.Abilities
{
    /// <summary>
    /// Resultado imutável de uma execução de <see cref="Esquiva"/>.
    /// </summary>
    public readonly struct EsquivaResult
    {
        public readonly bool Success;
        public readonly float DurationSeconds;
        public readonly float SpeedMultiplier;

        public EsquivaResult(bool success, float durationSeconds, float speedMultiplier)
        {
            Success = success;
            DurationSeconds = durationSeconds;
            SpeedMultiplier = speedMultiplier;
        }
    }

    /// <summary>
    /// POCO com a lógica matemática da Esquiva: um pulso curto de movimento físico
    /// comum, sem relação com o Salto Dimensional. Diferente de <see cref="DimensionalLeap"/>,
    /// não consome Resiliência Mental — Damião não está distorcendo a realidade de
    /// Carcosa, só se jogando pro lado. Por isso não implementa <see cref="IAnomalyPower"/>
    /// (essa interface exige um custo de resiliência que não se aplica aqui).
    /// </summary>
    public sealed class Esquiva
    {
        private readonly float duration;
        private readonly float cooldown;
        private readonly float speedMultiplier;

        public Esquiva(float duration = 0.15f, float cooldown = 0.8f, float speedMultiplier = 2.5f)
        {
            this.duration = duration;
            this.cooldown = cooldown;
            this.speedMultiplier = speedMultiplier;
        }

        /// <summary>Só depende do cooldown ter passado — sem custo de recurso.</summary>
        public bool CanActivate(float timeSinceLastUse) => timeSinceLastUse >= cooldown;

        public EsquivaResult Execute() => new EsquivaResult(true, duration, speedMultiplier);
    }
}
