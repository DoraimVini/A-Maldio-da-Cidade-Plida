namespace FavelaAmarela.Core.Abilities
{
    public struct PowerResult
    {
        public bool Success;
        public float DurationSeconds;
        public float CooldownSeconds;
        public float ResilienceCost;
    }

    /// <summary>
    /// POCO Interface for any supernatural ability (Anomalia/Salto Dimensional) 
    /// according to the Open-Closed Principle (OCP Sentinel).
    /// </summary>
    public interface IAnomalyPower
    {
        string PowerName { get; }
        
        bool CanActivate(float currentResilience, float timeSinceLastUse);
        
        PowerResult Execute(float currentResilience);
    }
}
