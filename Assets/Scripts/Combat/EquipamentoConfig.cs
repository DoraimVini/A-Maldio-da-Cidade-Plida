using UnityEngine;
using FavelaAmarela.Runtime.Itens;

namespace FavelaAmarela.Runtime.Combat
{
    public enum SlotDeEquipamento
    {
        Cabeca,
        Armadura,
        Pernas,
        MaoDireita,
        MaoEsquerda
    }

    /// <summary>
    /// Define um equipamento que pode ser vestido pelo jogador para alterar
    /// seus atributos de combate (Defesa, Resiliência, etc).
    /// </summary>
    [CreateAssetMenu(fileName = "Equip_Novo", menuName = "Favela Amarela/Equipamento")]
    public sealed class EquipamentoConfig : ScriptableObject
    {
        [Header("Identidade")]
        public string id;
        public string nome = "Novo Equipamento";
        [TextArea] public string descricao;
        public Sprite icone;

        [Header("Tipo de Slot")]
        [Tooltip("O slot anatômico que este equipamento ocupa no corpo de Damião.")]
        public SlotDeEquipamento slot = SlotDeEquipamento.Armadura;

        [Header("Bônus de Atributos")]
        [Tooltip("Aumento na Vitalidade Máxima.")]
        public float bonusVitalidadeMax = 0f;
        
        [Tooltip("Aumento no dano físico (Ataque).")]
        public float bonusAtaque = 0f;

        [Tooltip("Aumento na redução de dano físico (Defesa).")]
        public float bonusDefesa = 0f;

        [Tooltip("Aumento no dano mágico (Conjuração).")]
        public float bonusConjuracao = 0f;

        [Tooltip("Aumento na redução de dano mágico (Resistência Anômala).")]
        public float bonusResistenciaAnomala = 0f;
    }
}
