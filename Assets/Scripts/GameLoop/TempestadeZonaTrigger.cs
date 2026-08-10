using UnityEngine;
using FavelaAmarela.Runtime.Environment;

namespace FavelaAmarela.Runtime.GameLoop
{
    /// <summary>
    /// Camada Runtime (MonoBehaviour). Redefine a faixa de oscilação da tempestade
    /// ao entrar numa zona. Diferente dos outros triggers de progressão (Colapso,
    /// Patuá, Tutorial), este dispara TODA VEZ que o jogador entra — não é um
    /// evento único, é "agora você está numa zona com esse clima".
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Favela Amarela/GameLoop/Tempestade Zona Trigger")]
    public class TempestadeZonaTrigger : MonoBehaviour
    {
        [SerializeField] private TempestadeAmbiente tempestadeAmbiente;
        [SerializeField] private float minimo = 0.2f;
        [SerializeField] private float maximo = 0.6f;

        private void Awake()
        {
            if (tempestadeAmbiente == null)
                Debug.LogError("[TempestadeZonaTrigger] TempestadeAmbiente não atribuída no Inspector.", this);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collision.CompareTag("Player")) return;
            if (tempestadeAmbiente == null) return;

            tempestadeAmbiente.DefinirFaixa(minimo, maximo);
        }
    }
}
