using UnityEngine;
using System.Collections.Generic;
using FavelaAmarela.Inventario;
using FavelaAmarela.Progression;
using FavelaAmarela.Runtime.GameLoop;

namespace FavelaAmarela.Player
{
    /// <summary>
    /// Hub central de todos os bônus passivos do jogo. 
    /// Combina modificadores de Equipamentos, Itens Chave na Mochila e Ecos da Memória.
    /// É o único ponto de consulta para Combate, Vitalidade, Furtividade, etc.
    /// </summary>
    public class GerenciadorEfeitosPassivos : MonoBehaviour
    {
        public static GerenciadorEfeitosPassivos Instance { get; private set; }

        public event System.Action OnBonusChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.Equipment.OnEquipmentChanged += NotificarMudanca;
                InventoryManager.Instance.Main.OnSlotChanged += (i) => NotificarMudanca();
            }
            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.OnEcoDesbloqueado += (eco) => NotificarMudanca();
            }
        }

        private void NotificarMudanca()
        {
            OnBonusChanged?.Invoke();
        }

        private void OnDestroy()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.Equipment.OnEquipmentChanged -= NotificarMudanca;
                InventoryManager.Instance.Main.OnSlotChanged -= (i) => NotificarMudanca();
            }
            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.OnEcoDesbloqueado -= (eco) => NotificarMudanca();
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.Resiliencia == null)
                return;

            float regen = GetBonus(StatType.RegenRM);
            float dreno = GetBonus(StatType.DrenoRM);
            
            // Keystone do Protetor: "O Pacto do Rei" anula o dreno de RM no escuro.
            // Para simplificar no momento, estamos apenas agregando. A lógica de escuro virá do StealthManager.
            
            float deltaRM = (regen - dreno) * Time.deltaTime;
            
            if (deltaRM > 0.001f)
            {
                GameManager.Instance.Resiliencia.Ancorar(deltaRM); 
            }
            else if (deltaRM < -0.001f)
            {
                GameManager.Instance.Resiliencia.SofrerTrauma(Mathf.Abs(deltaRM)); 
            }
        }

        /// <summary>
        /// Agrega o bônus total para um determinado status (soma todos os multiplicadores e bônus fixos).
        /// Fontes: Equipamentos, Itens Passivos na Mochila, e Ecos Desbloqueados.
        /// </summary>
        public float GetBonus(StatType statType)
        {
            float total = 0f;

            // 1. Equipamentos
            if (InventoryManager.Instance != null && InventoryManager.Instance.Equipment != null)
            {
                for (int i = 0; i < InventoryManager.Instance.Equipment.Capacidade; i++)
                {
                    var slot = InventoryManager.Instance.Equipment.GetSlot(i);
                    if (slot != null && slot.Def != null && slot.Def.Modificadores != null)
                    {
                        foreach (var mod in slot.Def.Modificadores)
                        {
                            if (mod.Stat == statType) total += mod.Valor;
                        }
                    }
                }

                // 2. Itens Chave na Mochila (ex: Necronomicon)
                // Uma heurística comum: iteramos a mochila procurando relíquias puramente passivas.
                for (int i = 0; i < InventoryManager.Instance.Main.Capacidade; i++)
                {
                    var slot = InventoryManager.Instance.Main.GetSlot(i);
                    if (slot != null && slot.Def != null && slot.Def.Tipo == ItemType.Chave)
                    {
                        if (slot.Def.Modificadores != null)
                        {
                            foreach (var mod in slot.Def.Modificadores)
                            {
                                if (mod.Stat == statType) total += mod.Valor;
                            }
                        }
                    }
                }
            }

            // 3. Ecos da Memória
            if (ProgressionManager.Instance != null)
            {
                foreach (var eco in ProgressionManager.Instance.EcosDesbloqueados)
                {
                    if (eco.Modificadores != null)
                    {
                        foreach (var mod in eco.Modificadores)
                        {
                            if (mod.Stat == statType) total += mod.Valor;
                        }
                    }
                }
            }

            return total;
        }
    }
}
