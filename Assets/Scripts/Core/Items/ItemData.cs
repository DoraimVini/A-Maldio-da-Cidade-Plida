using UnityEngine;

namespace FavelaAmarela.Core.Items
{
    [CreateAssetMenu(fileName = "NovoItem", menuName = "Favela Amarela/Itens/ItemData")]
    public class ItemData : ScriptableObject
    {
        [Header("Informações Básicas")]
        public string ItemID;
        public string Nome;
        [TextArea]
        public string Descricao;
        public Sprite Icone;

        [Header("Feedback de Coleta")]
        public string NotificacaoColeta = "Item adicionado ao Bolsão Frio.";

        void Awake() { }
    }
}
