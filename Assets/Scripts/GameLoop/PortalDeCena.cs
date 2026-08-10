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

        [Tooltip("Onde aparecer na cena destino (identificador de um PontoDeChegada lá). " +
                 "Vazio = usa a posição padrão do jogador naquela cena.")]
        [SerializeField] private string chegarEm;

        [Tooltip("Segundos ignorando contato logo após a cena carregar. Impede que chegar " +
                 "em cima de um portal jogue o jogador de volta na hora.")]
        [SerializeField] private float carenciaAoCarregar = 0.5f;

        [Tooltip("Loga todo contato de trigger, mesmo os recusados (tag errada, carência " +
                 "ativa). Serve para distinguir 'o Player nunca chega até aqui' (problema de " +
                 "colisão/posição) de 'chega mas é recusado' (tag/carência) de 'nada acontece " +
                 "mesmo aceito' (cena destino/Build Settings). Desligue quando estabilizar.")]
        [SerializeField] private bool logarContatos = true;

        private float _tempoDeAtivacao;

        private void Reset()
        {
            // Ao adicionar o componente no Editor, já deixa o collider como trigger.
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void Awake()
        {
            _tempoDeAtivacao = Time.time + Mathf.Max(0f, carenciaAoCarregar);

            if (string.IsNullOrWhiteSpace(cenaDestino))
                Debug.LogError($"[PortalDeCena] '{name}' está sem cena destino; nenhuma transição vai ocorrer.", this);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (logarContatos)
                Debug.Log($"[Portal:{name}] contato de '{collision.name}' (tag={collision.tag}) " +
                          $"destino={cenaDestino} carênciaRestante={Mathf.Max(0f, _tempoDeAtivacao - Time.time):0.00}s", this);

            if (!collision.CompareTag("Player")) return;
            if (string.IsNullOrWhiteSpace(cenaDestino)) return; // erro já logado em Awake

            // Carência: chegar numa cena em cima de um portal (o caso normal quando ida e
            // volta usam a mesma porta) dispararia o trigger no mesmo instante e devolveria
            // o jogador para a cena de origem — um pingue-pongue infinito. A janela deixa
            // ele sair de cima da porta antes de ela voltar a valer.
            if (Time.time < _tempoDeAtivacao) return;

            PontoDeChegada.Pendente = string.IsNullOrWhiteSpace(chegarEm) ? null : chegarEm;

            // Fotografa o estado antes de a cena ser destruída — sem isto, atravessar a
            // porta faria Damião perder a arma do baú e a Vitalidade sofrida.
            Persistencia.GerenciadorDeSave.Instancia?.CapturarTudo();

            SceneManager.LoadScene(cenaDestino);
        }

        /// <summary>Define a cena destino por código (usado pelo gerador do deserto).</summary>
        public void DefinirCenaDestino(string cena) => cenaDestino = cena;

        /// <summary>
        /// Define em qual <see cref="PontoDeChegada"/> da cena destino o jogador aparece.
        /// Vazio/nulo mantém a posição padrão do jogador naquela cena.
        /// </summary>
        public void DefinirChegada(string identificador) => chegarEm = identificador;

        /// <summary>Identificador do ponto de chegada na cena destino (vazio = padrão).</summary>
        public string ChegarEm => chegarEm;

        /// <summary>Nome da cena destino atualmente configurada (para inspeção/testes de cena).</summary>
        public string CenaDestino => cenaDestino;
    }
}
