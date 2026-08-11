using UnityEngine;
using FavelaAmarela.Inventario;
using FavelaAmarela.Runtime.Interaction;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.Dungeons
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Porta selada por gravuras em Aklo, no Templo da
    /// Serpente: só cede a quem carrega o <b>Necronomicon</b> — sem o tomo, Damião vê os
    /// glifos mas não os lê.
    ///
    /// <para>Não consome o item nem exige que ele esteja equipado: basta possuí-lo no
    /// Bolsão Frio. O Necronomicon é item do tipo <c>Chave</c>, e a checagem é
    /// <see cref="InventoryManager.PossuiItemNaMochila"/>.</para>
    ///
    /// <para>Abre por <b>interação deliberada</b> (botão E) como o baú, e não por encostar:
    /// a porta precisa poder recusar e explicar o porquê.</para>
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Favela Amarela/Dungeons/Porta de Aklo")]
    public sealed class PortaDeAklo : MonoBehaviour, IInteragivel
    {
        [Header("Selo")]
        [Tooltip("Id do ItemDef exigido para romper o selo. Preenchido pelo TemploSerpenteSetup. [ASSET]")]
        [SerializeField] private string idItemNecessario = "necronomicon";

        [Header("Cena")]
        [Tooltip("Objeto desativado ao abrir (a folha da porta e seu colisor). Se vazio, " +
                 "usa este próprio GameObject. [CENA]")]
        [SerializeField] private GameObject folhaDaPorta;

        [Header("Feedback")]
        [Tooltip("Caixa de texto usada nas duas respostas (abrir e recusar).")]
        [SerializeField] private TutorialHintUI caixaDeTexto;

        [TextArea(2, 4)]
        [Tooltip("Mostrado quando Damião não carrega o tomo.")]
        [SerializeField] private string mensagemSelada =
            "As gravuras se enrolam uma na outra e não querem dizer nada. Falta o que as leia.";

        [TextArea(2, 4)]
        [Tooltip("Mostrado ao romper o selo.")]
        [SerializeField] private string mensagemAberta =
            "O Al-Azif esquenta contra o seu braço. As gravuras se assentam em palavras — e a pedra cede.";

        private bool _aberta;

        /// <summary>Se o selo já foi rompido (a porta não volta a fechar).</summary>
        public bool Aberta => _aberta;

        /// <summary>
        /// Define qual item rompe o selo. Chamado pelo <c>TemploSerpenteSetup</c> no
        /// bootstrap da dungeon, em vez de depender de referência de Inspector.
        /// </summary>
        public void Configurar(string idItemNecessario)
        {
            if (string.IsNullOrWhiteSpace(idItemNecessario))
            {
                Debug.LogError($"[PortaDeAklo] Id vazio recebido em '{name}' — o selo ficaria " +
                               "impossível de romper. Mantendo o id anterior.", this);
                return;
            }

            this.idItemNecessario = idItemNecessario;
        }

        // ── IInteragivel ─────────────────────────────────────────────────────

        /// <inheritdoc />
        public string RotuloDeInteracao => "Tocar as gravuras";

        /// <inheritdoc />
        public bool PodeInteragir => !_aberta;

        /// <summary>Prioridade alta: é o caminho adiante, ganha do cenário ao redor.</summary>
        public int PrioridadeDeInteracao => 10;

        /// <inheritdoc />
        public Vector2 PosicaoDeInteracao => transform.position;

        /// <inheritdoc />
        public void Interagir(GameObject quemInterage)
        {
            if (_aberta) return;

            var inventario = InventoryManager.Instance;
            if (inventario == null)
            {
                Debug.LogError("[PortaDeAklo] InventoryManager.Instance ausente — não dá para " +
                               "saber se o tomo está no Bolsão Frio. Selo mantido.", this);
                Mostrar(mensagemSelada);
                return;
            }

            if (!inventario.PossuiItemNaMochila(idItemNecessario))
            {
                Mostrar(mensagemSelada);
                return;
            }

            _aberta = true;
            Mostrar(mensagemAberta);

            var alvo = folhaDaPorta != null ? folhaDaPorta : gameObject;
            alvo.SetActive(false);
        }

        private void Mostrar(string texto)
        {
            if (caixaDeTexto != null) caixaDeTexto.Mostrar(texto);
        }
    }
}
