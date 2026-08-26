using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FavelaAmarela.Runtime.UI
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Mostra uma dica de texto na tela, com fade in/out,
    /// para momentos de tutorial (ex.: primeiro encontro com um Cultista na Zona 2).
    /// Não tem regra de negócio nenhuma, só anima o alpha de um CanvasGroup — mesmo
    /// espírito do <see cref="ScreenFader"/>, mas pra texto de dica em vez de tela cheia.
    /// </summary>
    [AddComponentMenu("Favela Amarela/UI/Tutorial Hint UI")]
    public sealed class TutorialHintUI : MonoBehaviour
    {

        /// <summary>
        /// A caixa de diálogo viva. Existe uma só: ela mora no prefab persistente do HUD e
        /// sobrevive às trocas de cena.
        ///
        /// <para><b>Por que virou global (2026-08-22):</b> nove componentes espalhados pelas
        /// cenas — portal trancado, travessia do Yug-Neth, baú da Tumba, Abdul, Refúgio... —
        /// referenciavam a caixa por campo serializado. Com ela migrando para um prefab que vive
        /// fora da cena, toda essa referência virou nula de uma vez, e o jogador esbarraria em
        /// portais e NPCs sem receber uma linha sequer.</para>
        ///
        /// <para>Cada consumidor mantém seu campo do Inspector — quem quiser uma caixa própria
        /// ainda pode — mas cai para esta instância quando o campo está vazio. É o mesmo
        /// contrato de <c>InventoryManager.Instance</c> e <c>HUDController.Instancia</c>.</para>
        /// </summary>
        public static TutorialHintUI Instancia { get; private set; }

        private void OnEnable()
        {
            if (Instancia == null) Instancia = this;
        }

        private void OnDisable()
        {
            if (Instancia == this) Instancia = null;
        }
        [Tooltip("CanvasGroup que envolve o texto da dica. [ASSET]")]
        [SerializeField] private CanvasGroup grupo;
        [Tooltip("Texto que exibe a mensagem da dica. [ASSET]")]
        [SerializeField] private Text texto;

        private Coroutine _rotina;

        private void Awake()
        {
            if (grupo == null)
                Debug.LogError("[TutorialHintUI] CanvasGroup não atribuído no Inspector.", this);
            if (texto == null)
                Debug.LogError("[TutorialHintUI] Text não atribuído no Inspector.", this);

            if (grupo != null) grupo.alpha = 0f;
        }

        /// <summary>
        /// Mostra <paramref name="mensagem"/> por <paramref name="duracaoVisivel"/> segundos,
        /// com fade in/out de <paramref name="duracaoFade"/> segundos cada.
        /// </summary>
        public void Mostrar(string mensagem, float duracaoVisivel = 4f, float duracaoFade = 0.4f)
        {
            if (grupo == null || texto == null) return;

            if (_rotina != null) StopCoroutine(_rotina);
            texto.text = mensagem;
            _rotina = StartCoroutine(SequenciaDeExibicao(duracaoVisivel, duracaoFade));
        }

        private IEnumerator SequenciaDeExibicao(float duracaoVisivel, float duracaoFade)
        {
            yield return Fade(0f, 1f, duracaoFade);
            yield return new WaitForSeconds(duracaoVisivel);
            yield return Fade(1f, 0f, duracaoFade);
        }

        private IEnumerator Fade(float de, float para, float duracao)
        {
            float tempo = 0f;
            while (tempo < duracao)
            {
                tempo += Time.deltaTime;
                grupo.alpha = Mathf.Lerp(de, para, duracao > 0f ? tempo / duracao : 1f);
                yield return null;
            }
            grupo.alpha = para;
        }
    }
}
