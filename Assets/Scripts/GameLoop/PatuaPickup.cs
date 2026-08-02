using UnityEngine;
using FavelaAmarela.Core.Persistencia;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.Interaction;
using FavelaAmarela.Runtime.Persistencia;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Pickup do patuá.
    ///
    /// <para><b>Efeito pendente de design (2026-07-30):</b> o patuá foi revisto e
    /// <b>não destrava mais o Salto Dimensional</b> — essa habilidade saiu do jogo. Ele
    /// vai ganhar outro propósito, ainda não definido pelo Vini. Por ora, coletar apenas
    /// mostra a mensagem e retira o item de cena; quando o novo efeito for decidido, ele
    /// entra em <see cref="Interagir"/>.</para>
    ///
    /// <para>Coletado por <b>interação deliberada</b> (botão E), não por encostar:
    /// colecionável é escolha do jogador, e o prompt "Recolher o patuá" também sinaliza
    /// que ali tem algo importante. Implementa <see cref="IInteragivel"/>.</para>
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Favela Amarela/GameLoop/Patuá Pickup")]
    public sealed class PatuaPickup : MonoBehaviour, IInteragivel
    {
        [Tooltip("Dica opcional mostrada ao coletar o patuá (reaproveita a UI do tutorial).")]
        [SerializeField] private TutorialHintUI hintUI;
        [TextArea]
        [SerializeField] private string mensagem = "Você recolheu o patuá. Ele pulsa devagar, como se esperasse algo.";

        private bool _coletado;

        // ── IInteragivel ─────────────────────────────────────────────────────

        /// <inheritdoc />
        public string RotuloDeInteracao => "Recolher o patuá";

        /// <inheritdoc />
        public bool PodeInteragir => !_coletado;

        /// <summary>Prioridade alta: é item de progressão, ganha de cenário ao redor.</summary>
        public int PrioridadeDeInteracao => 10;

        /// <inheritdoc />
        public Vector2 PosicaoDeInteracao => transform.position;

        /// <inheritdoc />
        public void Interagir(GameObject quemInterage)
        {
            if (_coletado) return;

            _coletado = true;
            GerenciadorDeSave.MarcarAconteceu(ChavesDeSave.PatuaColetado);

            // TODO(design): aplicar aqui o novo efeito do patuá quando ele for definido.
            // Antes, este era o ponto que destravava o Salto Dimensional.

            if (hintUI != null) hintUI.Mostrar(mensagem);

            gameObject.SetActive(false);
        }

        /// <summary>Some se o patuá já foi recolhido numa visita anterior à cena.</summary>
        private void Start()
        {
            if (!GerenciadorDeSave.JaAconteceu(ChavesDeSave.PatuaColetado)) return;

            _coletado = true;
            gameObject.SetActive(false);
        }
    }
}
