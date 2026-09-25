using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Core.Environment;

namespace FavelaAmarela.Runtime.UI
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Ajusta o alpha de um véu semitransparente
    /// na tela conforme <see cref="EnvironmentState.StormIntensity"/> — reduz
    /// visibilidade por cautela, sem mexer em velocidade de movimento. Observa o
    /// evento <see cref="EnvironmentState.OnStormIntensityChanged"/>, nunca faz
    /// polling a cada frame (regra 8 do CLAUDE.md raiz).
    /// </summary>
    [AddComponentMenu("Favela Amarela/UI/Tempestade Visual Overlay")]
    public sealed class TempestadeVisualOverlay : MonoBehaviour
    {
        [Tooltip("Image full-stretch semitransparente que representa a poeira/névoa. [ASSET]")]
        [SerializeField] private Image veu;
        [Tooltip("Alpha máximo do véu quando StormIntensity = 1.")]
        [SerializeField] private float alphaMaximo = 0.5f;

        private EnvironmentState _environment;

        public void Bind(EnvironmentState environment)
        {
            if (_environment != null) _environment.OnStormIntensityChanged -= HandleStormIntensityChanged;

            _environment = environment;

            if (_environment != null)
            {
                _environment.OnStormIntensityChanged += HandleStormIntensityChanged;
                HandleStormIntensityChanged(_environment.StormIntensity);
            }
        }

        private void Awake()
        {
            if (veu == null)
                Debug.LogError("[TempestadeVisualOverlay] Image não atribuída no Inspector.", this);
        }

        private void HandleStormIntensityChanged(float intensidade)
        {
            if (veu == null) return;

            var cor = veu.color;
            cor.a = intensidade * alphaMaximo;
            veu.color = cor;
        }

        private void OnDestroy()
        {
            if (_environment != null) _environment.OnStormIntensityChanged -= HandleStormIntensityChanged;
        }
    }
}
