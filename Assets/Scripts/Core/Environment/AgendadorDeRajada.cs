using System;

namespace FavelaAmarela.Core.Environment
{
    /// <summary>
    /// POCO que decide *quando* uma rajada forte de vento acontece dentro de uma
    /// zona (ex.: Vila das Casas), alternando entre calmaria e rajada em
    /// intervalos aleatórios. Não calcula a intensidade em si — isso continua
    /// sendo papel do <see cref="TempestadeOscilador"/>; este agendador só diz
    /// "estamos em rajada agora ou não", pra quem o usa trocar a faixa min/max
    /// via <c>TempestadeAmbiente.DefinirFaixa</c>.
    /// </summary>
    public sealed class AgendadorDeRajada
    {
        private readonly float intervaloMinimo;
        private readonly float intervaloMaximo;
        private readonly float duracaoRajada;
        private readonly Func<double> amostraAleatoria;

        private static readonly Random _randomPadrao = new Random();

        private float tempoAteProximaRajada;
        private float tempoRestanteDaRajada;

        /// <summary>Verdadeiro enquanto uma rajada estiver em curso.</summary>
        public bool EstaEmRajada => tempoRestanteDaRajada > 0f;

        /// <param name="amostraAleatoria">
        /// Fonte de números em [0, 1) usada pra sortear o próximo intervalo entre
        /// rajadas. Injetável pra testes determinísticos (ex.: <c>() => 0.0</c>
        /// sempre sorteia o intervalo mínimo). Usa <see cref="Random"/> padrão se
        /// omitido.
        /// </param>
        public AgendadorDeRajada(
            float intervaloMinimo = 5f,
            float intervaloMaximo = 12f,
            float duracaoRajada = 3f,
            Func<double> amostraAleatoria = null)
        {
            this.intervaloMinimo = intervaloMinimo;
            this.intervaloMaximo = intervaloMaximo;
            this.duracaoRajada = duracaoRajada;
            this.amostraAleatoria = amostraAleatoria ?? (() => _randomPadrao.NextDouble());

            tempoAteProximaRajada = SortearProximoIntervalo();
        }

        /// <summary>Avança o tempo interno e atualiza se está ou não em rajada.</summary>
        public void Tick(float dt)
        {
            if (EstaEmRajada)
            {
                tempoRestanteDaRajada -= dt;
                if (tempoRestanteDaRajada <= 0f)
                {
                    tempoRestanteDaRajada = 0f;
                    tempoAteProximaRajada = SortearProximoIntervalo();
                }
                return;
            }

            tempoAteProximaRajada -= dt;
            if (tempoAteProximaRajada <= 0f)
            {
                tempoRestanteDaRajada = duracaoRajada;
            }
        }

        private float SortearProximoIntervalo()
        {
            float t = (float)amostraAleatoria();
            return intervaloMinimo + t * (intervaloMaximo - intervaloMinimo);
        }
    }
}
