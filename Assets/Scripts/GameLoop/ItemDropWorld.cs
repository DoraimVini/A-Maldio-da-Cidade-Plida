using UnityEngine;
using FavelaAmarela.Core.Items;
using FavelaAmarela.Runtime.Interaction;
using FavelaAmarela.Runtime.UI;

namespace FavelaAmarela.Runtime.GameLoop
{
    [RequireComponent(typeof(Collider2D))]
    public class ItemDropWorld : MonoBehaviour, IInteragivel
    {
        [Header("Configuração do Drop")]
        [SerializeField] private ItemData itemData;
        [SerializeField] private SpriteRenderer spriteRenderer;
        
        [Header("UI Feedback")]
        [Tooltip("Deixe vazio. O script encontra automaticamente a UI na cena.")]
        [SerializeField] private TutorialHintUI hintUI;

        void Awake() 
        {
            if (itemData != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = itemData.Icone;
            }

            // Prefabs não podem salvar referências de objetos da cena. 
            // Buscamos a UI dinamicamente caso não esteja linkada.
            if (hintUI == null)
            {
                hintUI = Object.FindAnyObjectByType<TutorialHintUI>();
            }
        }

        public void ConfigurarDrop(ItemData data, TutorialHintUI ui)
        {
            itemData = data;
            hintUI = ui;
            if (spriteRenderer != null && itemData != null)
            {
                spriteRenderer.sprite = itemData.Icone;
            }
        }

        // ── IInteragivel ─────────────────────────────────────────────────────

        public string RotuloDeInteracao => itemData != null ? $"Recolher {itemData.Nome}" : "Recolher Item";

        public bool PodeInteragir => gameObject.activeInHierarchy;

        public int PrioridadeDeInteracao => 10;

        public Vector2 PosicaoDeInteracao => transform.position;

        public void Interagir(GameObject quemInterage)
        {
            if (hintUI != null && itemData != null)
            {
                hintUI.Mostrar(itemData.NotificacaoColeta);
            }

            // Oculta o objeto no mundo ao coletar
            gameObject.SetActive(false);
        }
    }
}
