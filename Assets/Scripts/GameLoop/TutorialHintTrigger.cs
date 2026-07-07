using UnityEngine;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Dispara uma dica de tutorial uma única vez quando
    /// Damião entra na área — mesmo padrão de trigger do <see cref="ColapsoTrigger"/> e
    /// <see cref="QuedaZ4Z5Trigger"/> (Collider2D + CompareTag, dispara uma vez só).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Favela Amarela/GameLoop/Tutorial Hint Trigger")]
    public sealed class TutorialHintTrigger : MonoBehaviour
    {
        [Tooltip("UI que efetivamente mostra o texto na tela.")]
        [SerializeField] private TutorialHintUI hintUI;
        [TextArea]
        [SerializeField] private string mensagem = "Segure Shift para se mover em modo Furtivo — mais silencioso, mais seguro.";
        [SerializeField] private float duracaoVisivel = 4f;

        private bool _disparado;

        private void Awake()
        {
            if (hintUI == null)
                Debug.LogError("[TutorialHintTrigger] TutorialHintUI não atribuída no Inspector.", this);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (_disparado) return;
            if (!collision.CompareTag("Player")) return;
            if (hintUI == null) return;

            _disparado = true;
            hintUI.Mostrar(mensagem, duracaoVisivel);
        }
    }
}
