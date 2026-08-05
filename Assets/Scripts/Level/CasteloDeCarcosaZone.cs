using UnityEngine;

namespace FavelaAmarela.Level
{
    public class CasteloDeCarcosaZone : MonoBehaviour
    {
        [Header("Configurações da Zona")]
        [SerializeField] private string nomeDaZona = "O Grande Salão";
        [Header("Eventos Locais")]
        [SerializeField] private bool iniciaCutscene = false;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                Debug.Log($"Damião entrou na zona: {nomeDaZona} do Castelo de Carcosa.");
                
                // Exemplo de notificação de UI
                // UIManager.Instance.MostrarNomeDaArea(nomeDaZona);

                if (iniciaCutscene)
                {
                    DispararEventoDeZona();
                }
            }
        }

        private void DispararEventoDeZona()
        {
            Debug.Log($"Disparando evento narrativo da zona {nomeDaZona}");
            // Lógica para desativar controle do jogador e iniciar timeline/cutscene
        }
    }
}
