using UnityEngine;
using FavelaAmarela.Core.Environment;

namespace FavelaAmarela.Runtime.Environment
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Tica o <see cref="TempestadeOscilador"/>
    /// continuamente e empurra o resultado pro <see cref="EnvironmentState.StormIntensity"/>
    /// — as rajadas de vento/areia que dão vida ao valor, em vez de um número
    /// estático fixo por zona.
    ///
    /// <para><b>Duas camadas de variação, de propósito:</b></para>
    /// <list type="number">
    ///   <item>O <b>oscilador</b> respira dentro da faixa do setor (a "ondulação" constante).</item>
    ///   <item>As <b>rajadas</b> (<see cref="AgendadorDeRajada"/>) somam um pico por cima,
    ///   em intervalos aleatórios. É o que faz a tempestade parecer viva em vez de um
    ///   seno previsível — o jogador não consegue cronometrar quando vai piorar.</item>
    /// </list>
    ///
    /// <para>A rajada <b>soma</b> à faixa corrente em vez de substituí-la: assim ela
    /// funciona em qualquer setor sem brigar com o <c>TempestadeZonaTrigger</c>, que é quem
    /// define a base. Numa zona calma a rajada é uma lufada; no leste, um apagão.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/Environment/Tempestade Ambiente")]
    public class TempestadeAmbiente : MonoBehaviour
    {
        [Header("Faixa Inicial")]
        [SerializeField] private float minimoInicial = 0.2f;
        [SerializeField] private float maximoInicial = 0.6f;
        [SerializeField] private float velocidadeCiclo = 0.3f;

        [Header("Rajadas aleatórias")]
        [Tooltip("Liga os picos de vento em intervalos aleatórios. Desligue em zonas onde a " +
                 "tempestade não deve existir (ex.: o Santuário).")]
        [SerializeField] private bool rajadasAtivas = true;

        [Tooltip("Quanto a rajada soma à intensidade no auge (0–1).")]
        [Range(0f, 1f)]
        [SerializeField] private float forcaDaRajada = 0.35f;

        [Tooltip("Menor espera entre rajadas, em segundos.")]
        [SerializeField] private float intervaloMinimo = 8f;

        [Tooltip("Maior espera entre rajadas, em segundos.")]
        [SerializeField] private float intervaloMaximo = 20f;

        [Tooltip("Quanto tempo a rajada leva no auge.")]
        [SerializeField] private float duracaoRajada = 4f;

        [Tooltip("Segundos de subida/descida da rajada. Sem isso ela 'liga' de estalo, " +
                 "o que lê como bug de render em vez de vento.")]
        [SerializeField] private float suavizacao = 1.2f;

        private TempestadeOscilador _oscilador;
        private AgendadorDeRajada _agendador;
        private EnvironmentState _environment;

        /// <summary>Intensidade extra vinda da rajada corrente (0..<see cref="forcaDaRajada"/>).</summary>
        private float _picoAtual;

        public void Bind(EnvironmentState environment)
        {
            _environment = environment;
        }

        private void Awake()
        {
            _oscilador = new TempestadeOscilador(minimoInicial, maximoInicial, velocidadeCiclo);
            _agendador = new AgendadorDeRajada(intervaloMinimo, intervaloMaximo, duracaoRajada);
        }

        /// <summary>Redefine a faixa de oscilação — chamado pelo <c>TempestadeZonaTrigger</c> ao mudar de zona.</summary>
        public void DefinirFaixa(float minimo, float maximo)
        {
            _oscilador.DefinirFaixa(minimo, maximo);
        }

        private void Update()
        {
            if (_environment == null) return;

            float dt = Time.deltaTime;
            float baseDaZona = _oscilador.Tick(dt);

            _environment.SetStormIntensity(Mathf.Clamp01(baseDaZona + TicarRajada(dt)));
        }

        /// <summary>
        /// Avança a rajada e devolve quanto ela soma agora. A subida e a descida são
        /// interpoladas para o vento "chegar" e "passar" em vez de piscar.
        /// </summary>
        private float TicarRajada(float dt)
        {
            if (!rajadasAtivas || forcaDaRajada <= 0f) return 0f;

            _agendador.Tick(dt);

            float alvo = _agendador.EstaEmRajada ? forcaDaRajada : 0f;
            float passo = suavizacao > 0f ? forcaDaRajada / suavizacao * dt : forcaDaRajada;

            _picoAtual = Mathf.MoveTowards(_picoAtual, alvo, passo);
            return _picoAtual;
        }
    }
}
