using UnityEngine;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Persistencia;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.Interaction;
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
    /// de loot usa um array do Unity; aqui só há o gatilho e o feedback visual.</para>
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

        [Tooltip("Lista de armas (ItemDef) que podem ser sorteadas no baú.")]
        [SerializeField] private FavelaAmarela.Inventario.ItemDef[] armasPossiveis;

        [Header("Visual")]
        [Tooltip("Sprite do baú fechado → trocado pelo aberto ao coletar. [ASSET pixel art]")]
        [SerializeField] private SpriteRenderer spriteDoBau;

        [Tooltip("Sprite do baú já aberto. [ASSET pixel art]")]
        [SerializeField] private Sprite spriteAberto;

        [Header("Feedback")]
        [Tooltip("Dica mostrada ao abrir o baú (reaproveita a UI do tutorial).")]
        [SerializeField] private TutorialHintUI hintUI;

        private bool _aberto;

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

            if (armasPossiveis == null || armasPossiveis.Length == 0)
            {
                Debug.LogError("[BauDaTumba] Nenhuma arma configurada no array de sorteio.", this);
                return;
            }

            _aberto = true;
            GerenciadorDeSave.MarcarAconteceu(ChavesDeSave.BauDaTumbaAberto);

            FavelaAmarela.Inventario.ItemDef armaEscolhida;
            if (forcarArma && armaForcada != null)
                armaEscolhida = armaForcada;
            else
                armaEscolhida = armasPossiveis[Random.Range(0, armasPossiveis.Length)];

            // Validação defensiva: a arma sorteada deve existir e ter ID
            if (armaEscolhida == null || string.IsNullOrEmpty(armaEscolhida.Id))
            {
                Debug.LogError("[BauDaTumba] A arma sorteada ou forçada é nula/inválida. " +
                               "Certifique-se de que os assets de ItemDef estão atribuídos no Inspector.", this);
                return;
            }

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
