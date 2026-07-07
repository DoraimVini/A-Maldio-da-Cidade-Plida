using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FavelaAmarela.Runtime.UI
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Funde a tela toda pra uma cor sólida (normalmente
    /// preto) e de volta — usado para mascarar teletransportes/eventos roteirizados sem
    /// precisar de um sistema de cutscene completo (ex.: a queda Z4→Z5).
    /// Não tem regra de negócio nenhuma, só interpola o alpha de uma Image full-stretch.
    /// </summary>
    [AddComponentMenu("FavelaAmarela/UI/Screen Fader")]
    public sealed class ScreenFader : MonoBehaviour
    {
        [Tooltip("Image full-stretch que cobre a tela inteira. [ASSET]")]
        [SerializeField] private Image fadeImage;

        private void Awake()
        {
            if (fadeImage == null)
                Debug.LogError("[ScreenFader] Image não atribuída no Inspector.", this);
        }

        /// <summary>
        /// Interpola o alpha da imagem de fade até <paramref name="alvo"/> (0..1) ao
        /// longo de <paramref name="duracao"/> segundos. Use com <c>yield return</c>.
        /// </summary>
        public IEnumerator FadeTo(float alvo, float duracao)
        {
            if (fadeImage == null) yield break;

            float inicio = fadeImage.color.a;
            float tempo = 0f;

            while (tempo < duracao)
            {
                tempo += Time.deltaTime;
                float alpha = Mathf.Lerp(inicio, alvo, duracao > 0f ? tempo / duracao : 1f);
                SetAlpha(alpha);
                yield return null;
            }

            SetAlpha(alvo);
        }

        private void SetAlpha(float alpha)
        {
            var cor = fadeImage.color;
            cor.a = Mathf.Clamp01(alpha);
            fadeImage.color = cor;
        }
    }
}
