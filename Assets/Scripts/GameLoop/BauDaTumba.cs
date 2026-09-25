using UnityEngine;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Loot;
using FavelaAmarela.Core.Persistencia;
using FavelaAmarela.Player;
using FavelaAmarela.Progression;
using FavelaAmarela.Runtime.Interaction;
using FavelaAmarela.Runtime.Itens;
using FavelaAmarela.Runtime.Persistencia;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Baú da Tumba de Alhazred: quando Damião o
    /// <b>abre</b>, o baú <b>sorteia</b> uma das três armas seladas (Cravo de Aklo,
    /// Estilete de Irem, Alfanje de Alhazred) e a equipa na Mão Física. Não é escolha —
    /// é RNG, e é o que faz a build variar entre partidas.
    ///
    /// <para>Abre por <b>interação deliberada</b> (botão E), não por encostar: é um baú,
    /// o jogador decide abrir. Implementa <see cref="IInteragivel"/>; o
    /// <c>DetectorDeInteracao</c> no Damião cuida da mira e do prompt. A regra do sorteio
    /// vive no POCO <see cref="SorteioDeDrop"/>; aqui só há o gatilho e o feedback visual.</para>
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Favela Amarela/GameLoop/Baú da Tumba")]
    public sealed class BauDaTumba : MonoBehaviour, IInteragivel
    {
        [Header("Sorteio")]
        [Tooltip("Se marcado, sempre entrega a arma escolhida abaixo (para testar uma build específica).")]
        [SerializeField] private bool forcarArma = false;

        [Tooltip("Arma entregue quando 'Forçar Arma' está marcado.")]
        [SerializeField] private FavelaAmarela.Inventario.ItemDef armaForcada;

        [Tooltip("Tabela de drop com as armas seladas do baú. [ASSET]")]
        [SerializeField] private FavelaAmarela.Inventario.TabelaDeDrop tabela;

        [Header("Visual")]
        [Tooltip("Sprite do baú fechado → trocado pelo aberto ao coletar. [ASSET pixel art]")]
        [SerializeField] private SpriteRenderer spriteDoBau;

        [Tooltip("Sprite do baú já aberto. [ASSET pixel art]")]
        [SerializeField] private Sprite spriteAberto;

        [Header("Feedback")]
        [Tooltip("Dica mostrada ao abrir o baú (reaproveita a UI do tutorial).")]
        [SerializeField] private TutorialHintUI hintUI;

        private bool _aberto;
        private readonly SorteioDeDrop _sorteio = new SorteioDeDrop();
        private readonly IFonteDeAleatoriedade _fonte = new FonteDeAleatoriedadeUnity();

        /// <summary>Se o baú já foi aberto (um baú entrega uma arma só).</summary>
        public bool Aberto => _aberto;

        // ── IInteragivel ─────────────────────────────────────────────────────

        /// <inheritdoc />
        public string RotuloDeInteracao => "Abrir o baú";

        /// <inheritdoc />
        public bool PodeInteragir => !_aberto;

        /// <summary>
        /// Prioridade alta: o baú é o objetivo da sala, deve ganhar de qualquer
        /// cenário interagível que por acaso esteja ao lado.
        /// </summary>
        public int PrioridadeDeInteracao => 10;

        /// <inheritdoc />
        public Vector2 PosicaoDeInteracao => transform.position;

        /// <inheritdoc />
        public void Interagir(GameObject quemInterage)
        {
            if (_aberto) return;

            var maoFisica = quemInterage.GetComponent<MaoFisicaBridge>();
            if (maoFisica == null)
            {
                Debug.LogError("[BauDaTumba] Quem abriu o baú não tem MaoFisicaBridge — " +
                               "nenhuma arma pôde ser equipada.", this);
                return;
            }

            // Só marca o baú como aberto depois de ter uma arma válida em mãos: falhar aqui
            // com o baú já "aberto" deixaria o jogador sem arma e sem uma segunda chance.
            var armaEscolhida = EscolherArma();
            if (armaEscolhida == null || string.IsNullOrEmpty(armaEscolhida.Id))
            {
                Debug.LogError("[BauDaTumba] A arma sorteada ou forçada é nula/inválida. " +
                               "Confira a Tabela de Drop e os assets de ItemDef.", this);
                return;
            }

            _aberto = true;
            GerenciadorDeSave.MarcarAconteceu(ChavesDeSave.BauDaTumbaAberto);

            var invManager = FavelaAmarela.Inventario.InventoryManager.Instance;
            if (invManager == null)
            {
                Debug.LogError("[BauDaTumba] InventoryManager.Instance está nulo. O baú não pode entregar a arma.", this);
                return;
            }

            // 1. Guarda no inventário
            bool coube = invManager.Main.Add(new FavelaAmarela.Inventario.ItemInstance(armaEscolhida.Id, 1));
            if (!coube)
            {
                Debug.LogWarning($"[BauDaTumba] Inventário cheio — '{armaEscolhida.Nome}' não coube na mochila.", this);
            }

            // 2. Equipa no slot de equipamento do inventário
            if (coube)
            {
                for (int i = 0; i < invManager.Main.Capacidade; i++)
                {
                    var slot = invManager.Main.GetSlot(i);
                    if (slot != null && slot.Def != null && slot.Def.Id == armaEscolhida.Id)
                    {
                        invManager.Equipar(i);
                        break;
                    }
                }
            }

            // O baú agora é agnóstico em relação à Mão Física e ao Combate.
            // Apenas adiciona ao inventário e equipa no slot.
            // A MaoFisicaBridge escuta o evento OnSlotChanged do inventário e instanciará a arma via WeaponFactory.
            Debug.Log($"[BauDaTumba] Arma '{armaEscolhida.Nome}' foi dada e o Inventário gerenciará o equipamento.", this);

            MostrarComoAberto();

            if (hintUI != null)
                hintUI.Mostrar($"A Tumba te entregou: {armaEscolhida.Nome}.");
        }
        /// <summary>
        /// Resolve qual arma o baú entrega: o override de teste, se ligado, senão um sorteio
        /// ponderado pela tabela. O baú entrega <b>exatamente uma</b> peça — por isso usa
        /// <c>SortearUm</c>, e não as chances independentes do espólio de inimigo.
        /// </summary>
        private FavelaAmarela.Inventario.ItemDef EscolherArma()
        {
            if (forcarArma && armaForcada != null) return armaForcada;

            if (tabela == null)
            {
                Debug.LogError("[BauDaTumba] Nenhuma Tabela de Drop atribuída — o baú não tem o que entregar.", this);
                return null;
            }

            int nivel = ProgressionManager.Instance != null ? ProgressionManager.Instance.NivelAtual : 1;
            var sorteado = _sorteio.SortearUm(tabela.ProjetarCandidatos(), nivel, _fonte);

            if (sorteado == null)
            {
                Debug.LogError("[BauDaTumba] A tabela não produziu nenhuma arma elegível.", this);
                return null;
            }

            var banco = FavelaAmarela.Inventario.ItemDatabase.Instance;
            if (banco == null)
            {
                Debug.LogError("[BauDaTumba] ItemDatabase.Instance está nulo — a arma sorteada não pôde ser resolvida.", this);
                return null;
            }

            return banco.Get(sorteado.Value.ItemDefId);
        }

        private void Start()
        {
            if (!GerenciadorDeSave.JaAconteceu(ChavesDeSave.BauDaTumbaAberto)) return;

            _aberto = true;
            MostrarComoAberto();
        }

        /// <summary>Troca o sprite para o baú aberto. Sem animação — só o estado final.</summary>
        private void MostrarComoAberto()
        {
            if (spriteDoBau != null && spriteAberto != null)
                spriteDoBau.sprite = spriteAberto;
        }
    }
}
