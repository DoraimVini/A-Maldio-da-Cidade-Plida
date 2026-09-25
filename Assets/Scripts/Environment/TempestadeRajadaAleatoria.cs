using UnityEngine;
using FavelaAmarela.Core.Environment;

namespace FavelaAmarela.Runtime.Environment
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Variante do <see cref="FavelaAmarela.Runtime.GameLoop.TempestadeZonaTrigger"/>
    /// para zonas onde a tempestade fica calma na maior parte do tempo, mas sofre
    /// rajadas fortes aleatórias enquanto o jogador está dentro (ex.: Vila das
    /// Casas). Tica o <see cref="AgendadorDeRajada"/> (Core) só enquanto o
    /// jogador está no trigger e alterna a faixa do <see cref="TempestadeAmbiente"/>
    /// entre calmaria e rajada.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Favela Amarela/Environment/Tempestade Rajada Aleatória")]
    public sealed class TempestadeRajadaAleatoria : MonoBehaviour
    {
        [SerializeField] private TempestadeAmbiente tempestadeAmbiente;

        [Header("Faixa Calma")]
        [SerializeField] private float minimoCalmo = 0.1f;
        [SerializeField] private float maximoCalmo = 0.3f;

        [Header("Faixa de Rajada")]
        [SerializeField] private float minimoRajada = 0.6f;
        [SerializeField] private float maximoRajada = 0.9f;

        [Header("Agendamento das Rajadas")]
        [SerializeField] private float intervaloMinimo = 8f;
        [SerializeField] private float intervaloMaximo = 15f;
        [SerializeField] private float duracaoRajada = 4f;

        private AgendadorDeRajada _agendador;
        private bool _jogadorDentro;
        private bool _emRajadaAtual;

        private void Awake()
        {
            if (tempestadeAmbiente == null)
                Debug.LogError("[TempestadeRajadaAleatoria] TempestadeAmbiente não atribuída no Inspector.", this);

            _agendador = new AgendadorDeRajada(intervaloMinimo, intervaloMaximo, duracaoRajada);
        }

        private void Update()
        {
            if (!_jogadorDentro || tempestadeAmbiente == null) return;

            _agendador.Tick(Time.deltaTime);

            if (_agendador.EstaEmRajada != _emRajadaAtual)
            {
                AplicarFaixa(_agendador.EstaEmRajada);
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collision.CompareTag("Player")) return;
            _jogadorDentro = true;
            AplicarFaixa(emRajada: false);
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (!collision.CompareTag("Player")) return;
            _jogadorDentro = false;
            AplicarFaixa(emRajada: false);
        }

        private void AplicarFaixa(bool emRajada)
        {
            _emRajadaAtual = emRajada;
            if (tempestadeAmbiente == null) return;

            if (emRajada)
                tempestadeAmbiente.DefinirFaixa(minimoRajada, maximoRajada);
            else
                tempestadeAmbiente.DefinirFaixa(minimoCalmo, maximoCalmo);
        }
    }
}
