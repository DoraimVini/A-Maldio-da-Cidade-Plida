using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using FavelaAmarela.Runtime.Enemies;

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Fecha o Vertical Slice quando o Rei em Amarelo é
    /// <b>selado</b>: deixa a cena respirar, escurece, escreve uma linha diegética e devolve ao
    /// Menu.
    ///
    /// <para><b>O buraco que isto fecha:</b> <c>ReiEmAmareloAI.OnVitoria</c> existia com o
    /// comentário "quem monta a cena decide o que fazer com isso" — e <b>ninguém decidia</b>. O
    /// evento tinha zero assinantes, então completar o rito, o clímax inteiro do VS, não
    /// disparava nada além do Rei mudar de cor. É o modo de falha assinatura deste projeto, na
    /// última cena do jogo.</para>
    ///
    /// <para><b>Espelha a <see cref="SequenciaDeColapso"/> de propósito</b>, em vez de inventar
    /// máquina nova: mesmo painel com <c>CanvasGroup</c>, mesmo fade em tempo não-escalado
    /// (funciona mesmo com <c>timeScale</c> zerado), mesma ideia de uma linha no vocabulário do
    /// lore. Os dois fins do jogo passam a ter a mesma forma.</para>
    ///
    /// <para><b>Sobre a espera antes do painel:</b> o Rei repinta para <c>corSelado</c> no
    /// instante do selamento. Cair no fade imediatamente esconderia esse retorno visual — o
    /// único sinal de que o rito funcionou. A pausa existe para o jogador <i>ver</i> que
    /// venceu.</para>
    /// </summary>
    [AddComponentMenu("Favela Amarela/GameLoop/Sequência de Selamento")]
    public sealed class SequenciaDeSelamento : MonoBehaviour
    {
        [Header("Gatilho")]
        [Tooltip("O Rei em Amarelo desta cena. Sem ele, nada dispara.")]
        [SerializeField] private ReiEmAmareloAI rei;

        [Header("Painel")]
        [Tooltip("CanvasGroup do painel escuro do desfecho. [ASSET]")]
        [SerializeField] private CanvasGroup painel;

        [Tooltip("Texto onde a linha do desfecho é escrita. [ASSET]")]
        [SerializeField] private Text texto;

        [Header("Ritmo")]
        [Tooltip("Segundos entre o selamento e o começo do fade — tempo de ver o Rei selado.")]
        [Min(0f)]
        [SerializeField] private float esperaAntesDoPainel = 2.5f;

        [Tooltip("Duração do escurecimento.")]
        [Min(0.1f)]
        [SerializeField] private float duracaoDoFade = 1.5f;

        [Header("Desfecho")]
        // TEXTO PROVISÓRIO, a ser escrito pelo Vini. Fica serializado — e não cravado no código
        // — justamente para ser trocado no Inspector sem recompilar. Segue a regra do
        // favela-lore-enforcer: vocabulário diegético, nada de "You Win". O GDD §342 é
        // explícito: não existe tela de Vitória, o fim é narrativo.
        [Tooltip("Linha do desfecho. PROVISÓRIA — o texto final é decisão de design.")]
        [TextArea(2, 4)]
        [SerializeField] private string linhaDoDesfecho =
            "O Sinal se fecha. Carcosa cala — por ora.";

        [Tooltip("Segundos com o painel cheio antes de voltar ao Menu.")]
        [Min(0f)]
        [SerializeField] private float esperaAntesDoMenu = 5f;

        [Tooltip("Voltar ao Menu ao fim. Desligue para depurar a cena sem sair dela.")]
        [SerializeField] private bool voltarAoMenu = true;

        private bool _tocado;

        private void Awake()
        {
            if (painel != null)
            {
                painel.alpha = 0f;
                painel.gameObject.SetActive(false);
            }

            if (rei == null)
            {
                Debug.LogError("[SequenciaDeSelamento] Sem Rei em Amarelo ligado — vencer o rito " +
                               "não teria desfecho nenhum.", this);
                return;
            }

            rei.OnVitoria += Tocar;
        }

        // Método nomeado, não lambda: '-=' com um lambda diferente do '+=' nunca desassina.
        private void OnDestroy()
        {
            if (rei != null) rei.OnVitoria -= Tocar;
        }

        /// <summary>
        /// Dispara o desfecho. Idempotente. Público para o Carcosa Debugger poder conferir a
        /// sequência sem completar o rito inteiro.
        /// </summary>
        public void Tocar()
        {
            if (_tocado) return;
            _tocado = true;
            StartCoroutine(Sequencia());
        }

        private IEnumerator Sequencia()
        {
            // Tempo não-escalado em toda a sequência: se algo zerar o timeScale (uma pausa, um
            // set-piece), o desfecho ainda roda em vez de congelar para sempre.
            yield return new WaitForSecondsRealtime(esperaAntesDoPainel);

            if (painel != null)
            {
                if (texto != null) texto.text = linhaDoDesfecho;

                painel.gameObject.SetActive(true);

                float t = 0f;
                while (t < duracaoDoFade)
                {
                    t += Time.unscaledDeltaTime;
                    painel.alpha = Mathf.Clamp01(t / duracaoDoFade);
                    yield return null;
                }
                painel.alpha = 1f;
            }

            Debug.Log("[SequenciaDeSelamento] O Rei em Amarelo foi selado — fim do Vertical Slice.",
                      this);

            if (!voltarAoMenu) yield break;

            yield return new WaitForSecondsRealtime(esperaAntesDoMenu);
            NavegacaoDeCenas.IrParaMenu();
        }
    }
}
