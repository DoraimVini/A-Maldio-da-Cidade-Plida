using UnityEngine;

namespace FavelaAmarela.Core.Environment
{
    /// <summary>
    /// POCO que calcula a intensidade de tempestade oscilando suavemente entre um
    /// mínimo e um máximo (rajadas de vento), em vez de um valor estático por zona.
    /// Sem dependência de Unity além de <see cref="Mathf"/>.
    /// </summary>
    public sealed class TempestadeOscilador
    {
        private float minimo;
        private float maximo;
        private readonly float velocidadeCiclo;
        private float tempoAcumulado;

        public TempestadeOscilador(float minimo = 0.2f, float maximo = 0.6f, float velocidadeCiclo = 0.3f)
        {
            DefinirFaixa(minimo, maximo);
            this.velocidadeCiclo = velocidadeCiclo;
        }

        /// <summary>
        /// Redefine a faixa de oscilação (ex.: ao entrar numa zona com tempestade
        /// mais forte). Aceita min/max em qualquer ordem e faz clamp em [0, 1].
        /// </summary>
        public void DefinirFaixa(float novoMinimo, float novoMaximo)
        {
            minimo = Mathf.Clamp01(Mathf.Min(novoMinimo, novoMaximo));
            maximo = Mathf.Clamp01(Mathf.Max(novoMinimo, novoMaximo));
        }

        /// <summary>Avança o tempo interno e retorna a intensidade atual (0..1).</summary>
        public float Tick(float dt)
        {
            tempoAcumulado += dt;
            float onda = 0.5f + 0.5f * Mathf.Sin(tempoAcumulado * velocidadeCiclo);
            return Mathf.Lerp(minimo, maximo, onda);
        }
    }
}
