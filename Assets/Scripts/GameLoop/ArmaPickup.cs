using UnityEngine;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Pickup da arma inicial (Barra Enferrujada) na
    /// Zona 5 — destrava a Mão Física permanentemente (ver
    /// <see cref="MaoFisicaBridge.DesbloquearArma"/>) na primeira vez que Damião toca.
    /// Mesmo padrão do <see cref="PatuaPickup"/>: Collider2D trigger + CompareTag,
    /// dispara uma vez só.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Favela Amarela/GameLoop/Arma Pickup")]
    public sealed class ArmaPickup : MonoBehaviour
    {
        [Tooltip("Dica opcional mostrada ao coletar a arma (reaproveita a UI do tutorial).")]
        [SerializeField] private TutorialHintUI hintUI;
        [TextArea]
        [SerializeField] private string mensagem = "Você encontrou a Barra Enferrujada — a Mão Física está armada.";

        private bool _coletado;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (_coletado) return;
            if (!collision.CompareTag("Player")) return;

            var mao = collision.GetComponent<MaoFisicaBridge>();
            if (mao == null) return;

            _coletado = true;
            mao.DesbloquearArma();

            if (hintUI != null) hintUI.Mostrar(mensagem);

            gameObject.SetActive(false);
        }
    }
}
