using UnityEngine;
using FavelaAmarela.Core.Persistencia;
using FavelaAmarela.Runtime.Interaction;
using FavelaAmarela.Runtime.Itens;
using FavelaAmarela.Runtime.Persistencia;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// Camada Runtime. Um baú de <b>recompensa</b>: abre por interação (E), pode exigir que
    /// alguma coisa já tenha acontecido no mundo, e derrama o espólio no chão.
    ///
    /// <para><b>Por que não é o <see cref="BauDaTumba"/>.</b> Aquele é um baú de <i>abertura de
    /// jogo</i>: sorteia <b>uma</b> arma entre três e a <b>equipa</b>, porque o Damião chega nele
    /// sem arma nenhuma. Um baú de recompensa faz o oposto — entrega <b>várias</b> peças a um
    /// jogador que já tem equipamento e deve escolher o que fica. Forçar os dois no mesmo
    /// componente significaria um monte de <c>if</c> em cima de "que tipo de baú eu sou".</para>
    ///
    /// <para><b>Ele não materializa nada.</b> Implementa <see cref="IFonteDeEspolio"/> e deixa o
    /// <see cref="DropAoAbater"/> — no mesmo objeto — fazer o trabalho que já sabe fazer: rolar
    /// grau, nível e afixos pelo <c>GeradorDeItem</c> e espalhar os coletáveis. É literalmente o
    /// que a interface foi criada para permitir: <i>"quem larga espólio é quem sabe avisar que
    /// foi derrotado, não quem herda de uma classe específica"</i>. Aqui "derrotado" é
    /// "aberto".</para>
    ///
    /// <para><b>O portão é uma chave de save, não uma referência a uma quest.</b> Assim o baú
    /// não conhece a Cassilda, e a mesma peça serve para qualquer recompensa condicionada — sem
    /// o componente virar um catálogo de casos especiais.</para>
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Favela Amarela/GameLoop/Baú de Recompensa")]
    public sealed class BauDeRecompensa : MonoBehaviour, IInteragivel, IFonteDeEspolio
    {
        [Header("Portão")]
        [Tooltip("Chave de save que precisa ter acontecido para o baú abrir. Vazio = sem " +
                 "condição. Ex.: 'Quest.Cassilda.Concluida'.")]
        [SerializeField] private string chaveDeSaveExigida = "";

        [Tooltip("Fala mostrada quando o jogador tenta abrir e a condição ainda não foi " +
                 "cumprida. Diga POR QUE está trancado — 'trancado' sozinho é um beco sem saída.")]
        [TextArea(2, 4)]
        [SerializeField] private string falaTrancado =
            "O baú não cede. Alguma coisa ainda não foi dita nesta sala.";

        [Header("Identidade")]
        [Tooltip("Chave de save própria deste baú — é o que impede de reabrir e farmar. " +
                 "Precisa ser única por baú.")]
        [SerializeField] private string chaveDeSaveDoBau = "Mundo.Bau.SemNome";

        [Tooltip("Rótulo do prompt de interação.")]
        [SerializeField] private string rotulo = "Abrir o baú";

        [Header("Visual")]
        [Tooltip("Sprite do baú fechado → trocado pelo aberto ao abrir.")]
        [SerializeField] private SpriteRenderer spriteDoBau;

        [Tooltip("Sprite do baú já aberto. [ASSET pixel art]")]
        [SerializeField] private Sprite spriteAberto;

        [Header("Feedback")]
        [Tooltip("Fala mostrada ao abrir. Vazio = nada é dito.")]
        [TextArea(2, 4)]
        [SerializeField] private string falaAoAbrir = "O baú se abre.";

        [Tooltip("Caixa de diálogo. Vazio = usa a global do HUD persistente.")]
        [SerializeField] private TutorialHintUI hintUI;

        private bool _aberto;

        /// <inheritdoc />
        public event System.Action OnAbatido;

        /// <summary>Se o baú já foi aberto.</summary>
        public bool Aberto => _aberto;

        /// <summary>Se o portão deste baú já foi cumprido.</summary>
        public bool PortaoLiberado => string.IsNullOrEmpty(chaveDeSaveExigida) ||
                                      GerenciadorDeSave.JaAconteceu(chaveDeSaveExigida);

        private void Awake()
        {
            // A caixa de diálogo vive no prefab persistente do HUD desde 2026-08-22; o campo
            // do Inspector continua valendo para quem quiser uma própria.
            if (hintUI == null) hintUI = TutorialHintUI.Instancia;
            if (spriteDoBau == null) spriteDoBau = GetComponent<SpriteRenderer>();

            // Um baú aberto numa sessão anterior continua aberto. Sem isto, recarregar a cena
            // devolveria o espólio inteiro — e um baú que se reabre é uma torneira de itens.
            if (!string.IsNullOrEmpty(chaveDeSaveDoBau) &&
                GerenciadorDeSave.JaAconteceu(chaveDeSaveDoBau))
            {
                _aberto = true;
                MostrarComoAberto();
            }
        }

        // ── IInteragivel ─────────────────────────────────────────────────────

        /// <inheritdoc />
        public string RotuloDeInteracao => rotulo;

        /// <summary>
        /// Continua interagível com o portão fechado, de propósito: o jogador precisa <b>poder
        /// tentar</b> para ouvir por que não dá. Um baú que nem oferece o prompt é um baú que o
        /// jogador conclui estar quebrado.
        /// </summary>
        public bool PodeInteragir => !_aberto;

        /// <inheritdoc />
        public int PrioridadeDeInteracao => 10;

        /// <inheritdoc />
        public Vector2 PosicaoDeInteracao => transform.position;

        /// <inheritdoc />
        public void Interagir(GameObject quemInterage)
        {
            if (_aberto) return;

            if (!PortaoLiberado)
            {
                Dizer(falaTrancado);
                return;
            }

            _aberto = true;

            if (!string.IsNullOrEmpty(chaveDeSaveDoBau))
                GerenciadorDeSave.MarcarAconteceu(chaveDeSaveDoBau);

            // Quem materializa é o DropAoAbater no mesmo objeto. Se ele não estiver lá, o baú
            // abre e não entrega nada -- e isso é um erro de montagem que tem de gritar, não
            // uma abertura silenciosa e vazia.
            if (GetComponent<DropAoAbater>() == null)
                Debug.LogError($"[BauDeRecompensa] '{name}' abriu sem um DropAoAbater no mesmo " +
                               "objeto — nenhum item foi entregue.", this);

            OnAbatido?.Invoke();

            MostrarComoAberto();
            Dizer(falaAoAbrir);
        }

        private void MostrarComoAberto()
        {
            if (spriteDoBau != null && spriteAberto != null) spriteDoBau.sprite = spriteAberto;
        }

        private void Dizer(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return;
            if (hintUI != null) hintUI.Mostrar(texto);
        }
    }
}
