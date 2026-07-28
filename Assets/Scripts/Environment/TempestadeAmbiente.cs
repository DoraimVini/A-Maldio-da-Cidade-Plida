using UnityEngine;
using FavelaAmarela.Core.Environment;

namespace FavelaAmarela.Runtime.Environment
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Tica o <see cref="TempestadeOscilador"/>
    /// continuamente e empurra o resultado pro <see cref="EnvironmentState.StormIntensity"/>
    /// — as rajadas de vento/areia que dão vida ao valor, em vez de um número
    /// estático fixo por zona.
    /// </summary>
    [AddComponentMenu("Favela Amarela/Environment/Tempestade Ambiente")]
    public class TempestadeAmbiente : MonoBehaviour
    {
        [Header("Faixa Inicial")]
        [SerializeField] private float minimoInicial = 0.2f;
        [SerializeField] private float maximoInicial = 0.6f;
        [SerializeField] private float velocidadeCiclo = 0.3f;

        private TempestadeOscilador _oscilador;
        private EnvironmentState _environment;

        public void Bind(EnvironmentState environment)
        {
            _environment = environment;
        }

        private void Awake()
        {
            _oscilador = new TempestadeOscilador(minimoInicial, maximoInicial, velocidadeCiclo);
        }

        /// <summary>Redefine a faixa de oscilação — chamado pelo <c>TempestadeZonaTrigger</c> ao mudar de zona.</summary>
        public void DefinirFaixa(float minimo, float maximo)
        {
            _oscilador.DefinirFaixa(minimo, maximo);
        }

        private void Update()
        {
            if (_environment == null) return;
            _environment.SetStormIntensity(_oscilador.Tick(Time.deltaTime));
        }
    }
}
