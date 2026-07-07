using UnityEngine;
using FavelaAmarela.Player;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Pickup do patuá na Zona 5 — destrava o Salto
    /// Dimensional permanentemente (ver <see cref="AnomalyPowerBridge.DesbloquearSalto"/>)
    /// na primeira vez que Damião entra na área. Mesmo padrão de trigger de
    /// <see cref="ColapsoTrigger"/>/<see cref="QuedaZ4Z5Trigger"/>: Collider2D +
    /// CompareTag, dispara uma vez só.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Favela Amarela/GameLoop/Patuá Pickup")]
    public sealed class PatuaPickup : MonoBehaviour
    {
        [Tooltip("Dica opcional mostrada ao coletar o patuá (reaproveita a UI do tutorial).")]
        [SerializeField] private TutorialHintUI hintUI;
        [TextArea]
        [SerializeField] private string mensagem = "Você encontrou o patuá — o Salto Dimensional foi destravado.";

        private bool _coletado;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (_coletado) return;
            if (!collision.CompareTag("Player")) return;

            var anomalyBridge = collision.GetComponent<AnomalyPowerBridge>();
            if (anomalyBridge == null) return;

            _coletado = true;
            anomalyBridge.DesbloquearSalto();

            if (hintUI != null) hintUI.Mostrar(mensagem);

            gameObject.SetActive(false);
        }
    }
}
