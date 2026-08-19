using UnityEngine;
using System.Collections.Generic;
using FavelaAmarela.Inventario;
using FavelaAmarela.Progression;
using FavelaAmarela.Runtime.GameLoop;
using FavelaAmarela.Runtime.Progression;

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

        private ArtefatosBridge _artefatos;

        /// <summary>
        /// Liga a fonte de passivas dos Artefatos. Chamado pelo <c>GameManager</c>.
        /// Idempotente: re-bind troca a fonte sem deixar handler pendurado.
        /// </summary>
        public void Bind(ArtefatosBridge artefatos)
        {
            if (_artefatos != null) _artefatos.OnArtefatosMudaram -= NotificarMudanca;

            _artefatos = artefatos;

            if (_artefatos != null) _artefatos.OnArtefatosMudaram += NotificarMudanca;

            NotificarMudanca();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Componente irmão, resolvido no Awake: o Update roda todo frame e não pode resolver
            // dependência (Regra de Ouro 1) nem falhar calado.
            _mente = GetComponent<FavelaAmarela.Runtime.Combat.ResilienciaBridge>();
            if (_mente == null)
                Debug.LogError("[GerenciadorEfeitosPassivos] Sem ResilienciaBridge no mesmo " +
                               "GameObject — RegenRM e DrenoRM não terão efeito algum.", this);
        }

        // Handlers nomeados, não lambdas. `-=` compara delegates por alvo+método: um lambda
        // escrito de novo no OnDestroy NUNCA casa com o registrado no Start, então a assinatura
        // ficava para sempre. Como este componente vive no Player_Damiao (recriado a cada cena),
        // cada troca deixava um assinante morto recebendo eventos de um objeto destruído.
        private void HandleSlotDaMochilaMudou(int _) => NotificarMudanca();
        private void HandleEcoDesbloqueado(EcoDef _) => NotificarMudanca();

        private void Start()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.Equipment.OnEquipmentChanged += NotificarMudanca;
                InventoryManager.Instance.Main.OnSlotChanged += HandleSlotDaMochilaMudou;
            }
            if (ProgressionBridge.Instancia != null)
            {
                ProgressionBridge.Instancia.OnEcoDesbloqueado += HandleEcoDesbloqueado;
            }
        }

        private void NotificarMudanca()
        {
            OnBonusChanged?.Invoke();
        }

        private void OnDestroy()
        {
            if (_artefatos != null) _artefatos.OnArtefatosMudaram -= NotificarMudanca;

            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.Equipment.OnEquipmentChanged -= NotificarMudanca;
                InventoryManager.Instance.Main.OnSlotChanged -= HandleSlotDaMochilaMudou;
            }
            if (ProgressionBridge.Instancia != null)
            {
                ProgressionBridge.Instancia.OnEcoDesbloqueado -= HandleEcoDesbloqueado;
            }

            if (Instance == this) Instance = null;
        }

        // Resolvida UMA vez, não por frame. Antes o Update alcançava
        // GameManager.Instance.Resiliencia a cada quadro — e, com a fonte nula, ele simplesmente
        // parava de drenar, sem erro. A regressão só apareceria em playtest, como "a Resiliência
        // não cai mais". Resolver no Awake permite avisar uma vez, alto e claro.
        private FavelaAmarela.Runtime.Combat.ResilienciaBridge _mente;

        private void Update()
        {
            if (_mente == null || !_mente.Ligada) return;

            float regen = GetBonus(StatType.RegenRM);
            float dreno = GetBonus(StatType.DrenoRM);

            // Keystone do Protetor: "O Pacto do Rei" anula o dreno de RM no escuro.
            // Por ora apenas agregamos; a lógica de escuro virá do StealthManager.
            float deltaRM = (regen - dreno) * Time.deltaTime;

            if (deltaRM > 0.001f)
            {
                _mente.Ancorar(deltaRM);
            }
            else if (deltaRM < -0.001f)
            {
                _mente.SofrerTrauma(Mathf.Abs(deltaRM));
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

            // 3. Artefatos equipados (só valem enquanto ocupam um dos 4 slots)
            if (_artefatos != null)
            {
                for (int i = 0; i < FavelaAmarela.Core.Artefatos.InventarioDeArtefatos.TotalDeSlots; i++)
                {
                    var def = _artefatos.DefNoSlot(i);
                    if (def?.Passivas == null) continue;

                    foreach (var mod in def.Passivas)
                    {
                        if (mod.Stat == statType) total += mod.Valor;
                    }
                }
            }

            // 4. Ecos da Memória
            if (ProgressionBridge.Instancia != null)
            {
                foreach (var eco in ProgressionBridge.Instancia.EcosDesbloqueados())
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
