namespace FavelaAmarela.Core.Environment
{
    public class EnvironmentState
    {
        public float StormIntensity { get; private set; }

        public EnvironmentState()
        {
            StormIntensity = 0.3f; // Valor inicial stub
        }

        public void SetStormIntensity(float valor)
        {
            StormIntensity = System.Math.Max(0f, System.Math.Min(1f, valor));
        }
    }
}
