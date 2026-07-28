using UnityEngine;
using UnityEngine.SceneManagement;

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// Porta de transição entre cenas: ao contato do Player com este volume de
    /// trigger, carrega a cena <see cref="cenaDestino"/> (ex.: a entrada da Tumba
    /// de Alhazred no overworld do deserto carregando o S-Path).
    ///
    /// Carregamento mínimo e pontual via <see cref="SceneManager.LoadScene(string)"/>
    /// — NÃO é a infraestrutura completa de multi-cena (streaming/aditivo/persistência
    /// de estado ainda não existem). A cena destino precisa estar registrada em
    /// Build Settings para carregar por nome.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class PortalDeCena : MonoBehaviour
    {
        [Tooltip("Nome da cena a carregar (sem extensão). Deve estar em Build Settings.")]
        [SerializeField] private string cenaDestino;

        private void Reset()
        {
            // Ao adicionar o componente no Editor, já deixa o collider como trigger.
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void Awake()
        {
            if (string.IsNullOrWhiteSpace(cenaDestino))
                Debug.LogError($"[PortalDeCena] '{name}' está sem cena destino; nenhuma transição vai ocorrer.", this);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collision.CompareTag("Player")) return;
            if (string.IsNullOrWhiteSpace(cenaDestino)) return; // erro já logado em Awake

            SceneManager.LoadScene(cenaDestino);
        }

        /// <summary>Define a cena destino por código (usado pelo gerador do deserto).</summary>
        public void DefinirCenaDestino(string cena) => cenaDestino = cena;

        /// <summary>Nome da cena destino atualmente configurada (para inspeção/testes de cena).</summary>
        public string CenaDestino => cenaDestino;
    }
}
