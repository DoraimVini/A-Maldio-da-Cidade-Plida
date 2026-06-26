namespace FavelaAmarela.Core.Abilities
{
    /// <summary>
    /// POCO containing the mathematical logic for "Salto Dimensional" (Ghost Dash).
    /// </summary>
    public class DimensionalLeap : IAnomalyPower
    {
        public string PowerName => "Salto Dimensional";

        private readonly float duration;
        private readonly float cooldown;
        private readonly float resilienceCost;

        public DimensionalLeap(float duration = 0.2f, float cooldown = 1.0f, float resilienceCost = 10f)
        {
            this.duration = duration;
            this.cooldown = cooldown;
            this.resilienceCost = resilienceCost;
        }

        public bool CanActivate(float currentResilience, float timeSinceLastUse)
        {
            // Future-proofing: Cannot leap if resilience is too low to pay the cost
            // and must wait for cooldown.
            return timeSinceLastUse >= cooldown && currentResilience >= resilienceCost;
        }

        public PowerResult Execute(float currentResilience)
        {
            // For now, assume CanActivate was checked.
            return new PowerResult
            {
                Success = true,
                DurationSeconds = duration,
                CooldownSeconds = cooldown,
                ResilienceCost = resilienceCost
            };
        }
    }
}
