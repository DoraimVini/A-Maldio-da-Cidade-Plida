using System;

namespace FavelaAmarela.Core.Environment
{
    public class EnvironmentState
    {
        public float StormIntensity { get; private set; }

        /// <summary>Disparado só quando a intensidade muda de verdade (nunca em polling).</summary>
        public event Action<float> OnStormIntensityChanged;

        public EnvironmentState()
        {
            StormIntensity = 0.3f; // Valor inicial stub
        }

        public void SetStormIntensity(float valor)
        {
            float novoValor = System.Math.Max(0f, System.Math.Min(1f, valor));
            if (novoValor == StormIntensity) return;

            StormIntensity = novoValor;
            OnStormIntensityChanged?.Invoke(StormIntensity);
        }
    }
}
