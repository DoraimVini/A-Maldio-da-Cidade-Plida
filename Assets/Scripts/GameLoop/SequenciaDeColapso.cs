using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Core.GameLoop;

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Toca a sequência de morte — o **Colapso Mental**
    /// (Game Over diegético): dissolve o sprite de Damião via o shader de oclusão
    /// (<c>_DitherAmount</c> 0→1, "desfazendo-se em Carcosa"), escurece a tela e mostra
    /// uma frase sorteada de <see cref="FrasesDeColapso"/> ("Você abraçou Hastur." etc.).
    /// Disparada pelo <see cref="GameManager"/> ao entrar no estado Colapso. Usa tempo
    /// não-escalado para funcionar mesmo se o timeScale for zerado.
    /// </summary>
    [AddComponentMenu("Favela Amarela/GameLoop/Sequência de Colapso")]
    public sealed class SequenciaDeColapso : MonoBehaviour
    {
        [Header("Dissolução do boneco")]
        [Tooltip("SpriteRenderer de Damião. [ASSET]")]
        [SerializeField] private SpriteRenderer damiaoSprite;
        [Tooltip("Material do shader FavelaAmarela/SpriteDitherOcclusion. [ASSET]")]
        [SerializeField] private Material materialDissolucao;
        [SerializeField] private float duracaoDissolucao = 1.2f;

        [Header("Painel de Colapso")]
        [Tooltip("CanvasGroup do painel escuro que aparece no fim. [ASSET]")]
        [SerializeField] private CanvasGroup painelColapso;
        [Tooltip("Texto onde a frase diegética é escrita. [ASSET]")]
        [SerializeField] private Text textoColapso;
        [SerializeField] private float duracaoFadePainel = 0.8f;

        private readonly FrasesDeColapso _frases = new FrasesDeColapso();
        private static readonly int _DitherAmountId = Shader.PropertyToID("_DitherAmount");
        private bool _tocado;
        private TipoDeDerrota _tipoDeDerrota = TipoDeDerrota.Mental;

        private void Awake()
        {
            if (painelColapso != null)
            {
                painelColapso.alpha = 0f;
                painelColapso.gameObject.SetActive(false);
            }
        }

        /// <summary>Dispara a sequência de morte por Colapso Mental. Idempotente (só toca uma vez).</summary>
        public void Tocar() => Tocar(TipoDeDerrota.Mental);

        /// <summary>
        /// Dispara a sequência de morte. Idempotente (só toca uma vez). O
        /// <paramref name="tipo"/> escolhe o pool de frases: morrer de porrada
        /// (<see cref="TipoDeDerrota.Corporea"/>) não diz "você abraçou Hastur".
        /// </summary>
        public void Tocar(TipoDeDerrota tipo)
        {
            if (_tocado) return;
            _tocado = true;
            _tipoDeDerrota = tipo;
            StartCoroutine(Sequencia());
        }

        private IEnumerator Sequencia()
        {
            yield return DissolverDamiao();
            yield return MostrarPainel();
        }

        private IEnumerator DissolverDamiao()
        {
            if (damiaoSprite == null || materialDissolucao == null) yield break;

            damiaoSprite.material = materialDissolucao;
            var mpb = new MaterialPropertyBlock();

            float t = 0f;
            while (t < duracaoDissolucao)
            {
                t += Time.unscaledDeltaTime;
                damiaoSprite.GetPropertyBlock(mpb);
                mpb.SetFloat(_DitherAmountId, Mathf.Clamp01(t / duracaoDissolucao));
                damiaoSprite.SetPropertyBlock(mpb);
                yield return null;
            }
        }

        private IEnumerator MostrarPainel()
        {
            if (textoColapso != null) textoColapso.text = _frases.Sortear(_tipoDeDerrota);
            if (painelColapso == null) yield break;

            painelColapso.gameObject.SetActive(true);

            float t = 0f;
            while (t < duracaoFadePainel)
            {
                t += Time.unscaledDeltaTime;
                painelColapso.alpha = Mathf.Clamp01(duracaoFadePainel > 0f ? t / duracaoFadePainel : 1f);
                yield return null;
            }
            painelColapso.alpha = 1f;
        }
    }
}
