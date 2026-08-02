using UnityEngine;
using FavelaAmarela.Core.Persistencia;
using FavelaAmarela.Runtime.Interaction;
using FavelaAmarela.Runtime.Persistencia;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). O <b>Necronomicon</b> (Al-Azif) largado por Abdul ao
    /// ser derrotado. Recompensa exclusiva do caminho da luta — quem escolhe poupá-lo não o
    /// obtém (confirmado com o Vini, 2026-07-30).
    ///
    /// <para>É um <b>item a coletar</b>, não um efeito automático: o livro cai no chão e o
    /// jogador decide pegá-lo com o botão <b>E</b>. Segue o mesmo padrão de
    /// <see cref="BauDaTumba"/>/<see cref="PatuaPickup"/> via <see cref="IInteragivel"/>.</para>
    ///
    /// <para><b>Efeito pendente de design:</b> o lore diz que o Necronomicon permite traduzir
    /// Aklo (liberando lore extra e o diálogo do Nagaraja na Dungeon 2), mas não existe
    /// sistema de tradução nem inventário ainda. Por ora, coletar registra a posse e mostra
    /// a mensagem — o `TODO(design)` marca onde o efeito real entra.</para>
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Favela Amarela/GameLoop/Necronomicon (Pickup)")]
    public sealed class NecronomiconPickup : MonoBehaviour, IInteragivel
    {
        [Header("Feedback")]
        [Tooltip("Caixa de texto usada ao coletar (reaproveita a UI de dica por ora).")]
        [SerializeField] private TutorialHintUI caixaDeTexto;

        [TextArea]
        [Tooltip("Mensagem mostrada ao recolher o tomo.")]
        [SerializeField] private string mensagem =
            "O Al-Azif é mais pesado do que deveria. As páginas continuam virando sozinhas.";

        private bool _coletado;

        /// <summary>Se o tomo já foi recolhido (existe um só por run).</summary>
        public bool Coletado => _coletado;

        // ── IInteragivel ─────────────────────────────────────────────────────

        /// <inheritdoc />
        public string RotuloDeInteracao => "Recolher o Necronomicon";

        /// <inheritdoc />
        public bool PodeInteragir => !_coletado;

        /// <summary>Prioridade máxima: é a recompensa do clímax da dungeon.</summary>
        public int PrioridadeDeInteracao => 100;

        /// <inheritdoc />
        public Vector2 PosicaoDeInteracao => transform.position;

        /// <inheritdoc />
        public void Interagir(GameObject quemInterage)
        {
            if (_coletado) return;
            _coletado = true;
            GerenciadorDeSave.MarcarAconteceu(ChavesDeSave.NecronomiconColetado);

            // TODO(design): destravar a tradução de Aklo quando o sistema existir
            // (lore extra + diálogo do Nagaraja na Dungeon 2). A posse em si já é
            // registrada acima; falta o efeito.

            if (caixaDeTexto != null) caixaDeTexto.Mostrar(mensagem);

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Some se o tomo já foi recolhido numa visita anterior. Necessário porque o
        /// Necronomicon é <b>instanciado em runtime</b> ao derrotar Abdul: ao recarregar a
        /// cena, o <c>AbdulAlhazredAI</c> o instancia de novo se ainda não tiver sido pego —
        /// esta guarda cobre o caso de a instância vir a existir mesmo já coletado.
        /// </summary>
        private void Start()
        {
            if (!GerenciadorDeSave.JaAconteceu(ChavesDeSave.NecronomiconColetado)) return;

            _coletado = true;
            gameObject.SetActive(false);
        }
    }
}
