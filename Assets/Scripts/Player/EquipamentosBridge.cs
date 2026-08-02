using System;
using UnityEngine;
using FavelaAmarela.Core.Combat;
using FavelaAmarela.Runtime.Combat;

namespace FavelaAmarela.Player
{
    /// <summary>
    /// Gerencia os equipamentos vestidos pelo jogador e calcula a ficha final de atributos.
    /// Substitui a leitura direta de FichaAtributosConfig pelas bridges de combate.
    /// </summary>
    [AddComponentMenu("Favela Amarela/Player/Equipamentos Bridge")]
    public sealed class EquipamentosBridge : MonoBehaviour
    {
        [Tooltip("A ficha base do personagem sem nenhum equipamento.")]
        [SerializeField] private FichaAtributosConfig fichaBaseConfig;

        [Header("Equipamentos Atuais (Debug/Setup)")]
        [SerializeField] private EquipamentoConfig cabecaAtual;
        [SerializeField] private EquipamentoConfig armaduraAtual;
        [SerializeField] private EquipamentoConfig pernasAtual;
        [SerializeField] private EquipamentoConfig maoDireitaAtual;
        [SerializeField] private EquipamentoConfig maoEsquerdaAtual;

        private FichaDeEquipamentos _calculadora;
        private FichaDeAtributos _fichaFinalCache;

        /// <summary>Disparado quando os atributos finais mudam (ex: ao trocar de equipamento).</summary>
        public event Action OnAtributosMudaram;

        /// <summary>A ficha final de combate, com todos os bônus aplicados.</summary>
        public FichaDeAtributos FichaFinal 
        {
            get 
            {
                if (_fichaFinalCache == null) Recalcular();
                return _fichaFinalCache;
            }
        }

        private void Awake()
        {
            if (fichaBaseConfig == null)
            {
                Debug.LogError("[EquipamentosBridge] Ficha Base não atribuída!", this);
                return;
            }

            _calculadora = new FichaDeEquipamentos(fichaBaseConfig.CriarFicha());
            Recalcular();
        }

        public void Equipar(EquipamentoConfig novoEquip)
        {
            if (novoEquip == null) return;

            switch (novoEquip.slot)
            {
                case SlotDeEquipamento.Cabeca: cabecaAtual = novoEquip; break;
                case SlotDeEquipamento.Armadura: armaduraAtual = novoEquip; break;
                case SlotDeEquipamento.Pernas: pernasAtual = novoEquip; break;
                case SlotDeEquipamento.MaoDireita: maoDireitaAtual = novoEquip; break;
                case SlotDeEquipamento.MaoEsquerda: maoEsquerdaAtual = novoEquip; break;
            }

            Recalcular();
        }

        public void Desequipar(EquipamentoConfig equip)
        {
            if (cabecaAtual == equip) cabecaAtual = null;
            else if (armaduraAtual == equip) armaduraAtual = null;
            else if (pernasAtual == equip) pernasAtual = null;
            else if (maoDireitaAtual == equip) maoDireitaAtual = null;
            else if (maoEsquerdaAtual == equip) maoEsquerdaAtual = null;

            Recalcular();
        }

        private void Recalcular()
        {
            if (_calculadora == null) return;

            _calculadora.LimparBonus();

            AplicarBonus(cabecaAtual);
            AplicarBonus(armaduraAtual);
            AplicarBonus(pernasAtual);
            AplicarBonus(maoDireitaAtual);
            AplicarBonus(maoEsquerdaAtual);

            _fichaFinalCache = _calculadora.CalcularFichaFinal();
            OnAtributosMudaram?.Invoke();
        }

        private void AplicarBonus(EquipamentoConfig equip)
        {
            if (equip == null) return;
            _calculadora.AplicarBonus(
                equip.bonusAtaque, 
                equip.bonusDefesa, 
                equip.bonusConjuracao, 
                equip.bonusResistenciaAnomala, 
                equip.bonusVitalidadeMax
            );
        }
    }
}
