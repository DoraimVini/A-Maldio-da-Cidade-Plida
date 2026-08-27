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

                // TAMBÉM OnSlotChanged do equipamento (acrescentado em 2026-08-27). Em operação
                // normal os dois disparam juntos, mas `BaseInventory.LimparTudo()` dispara
                // SÓ OnSlotChanged -- e é ele que `InventoryManager.RestaurarEquipamento` chama
                // no caminho de load. Com o equipamento salvo VAZIO, nenhum Equip roda depois,
                // OnEquipmentChanged nunca vem, e os bônus da partida anterior ficavam grudados
                // no jogador.
                InventoryManager.Instance.Equipment.OnSlotChanged += HandleSlotDaMochilaMudou;

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
                InventoryManager.Instance.Equipment.OnSlotChanged -= HandleSlotDaMochilaMudou;
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
        // ── Cache ─────────────────────────────────────────────────────────────
        //
        // Até 2026-08-27 GetBonus recalculava TUDO a cada chamada: 7 slots de equipamento +
        // 12 da mochila + 4 artefatos + os Ecos, com um foreach por lista de modificadores --
        // 2× por quadro só no Update acima, mais 2× por golpe na MaoFisicaBridge. Não alocava
        // lixo, mas era trabalho repetido em hot path, que a Regra de Ouro 1 proíbe.
        //
        // Com afixos por instância o custo cresce (ModificadoresEfetivos concatena base +
        // rolados), então o cache deixou de ser otimização e virou requisito. A invalidação é
        // por EVENTO -- os mesmos quatro que já disparavam NotificarMudanca --, nunca por
        // tempo: cache com prazo é cache que mente durante o prazo.

        private readonly Dictionary<StatType, float> _cache = new Dictionary<StatType, float>();
        private bool _cacheValido;

        /// <summary>
        /// Bônus agregado de um atributo, somando equipamento, relíquias na mochila, Artefatos
        /// e Ecos.
        /// </summary>
        public float GetBonus(StatType statType)
        {
            if (!_cacheValido) Recalcular();

            return _cache.TryGetValue(statType, out float valor) ? valor : 0f;
        }

        /// <summary>
        /// Varre as quatro fontes UMA vez e preenche o cache inteiro. Varrer por atributo
        /// (como antes) repetia a mesma varredura para cada <c>StatType</c> consultado.
        /// </summary>
        private void Recalcular()
        {
            _cache.Clear();

            var inv = InventoryManager.Instance;

            if (inv != null)
            {
                // 1. Equipamento — agora pelos modificadores EFETIVOS da instância: os
                //    implícitos da base MAIS os afixos que este exemplar rolou. Ler
                //    `slot.Def.Modificadores` aqui perderia tudo que foi rolado, e o sistema
                //    de afixos seria invisível em jogo.
                if (inv.Equipment != null)
                    for (int i = 0; i < inv.Equipment.Capacidade; i++)
                        Somar(inv.Equipment.GetSlot(i));

                // 2. Relíquias puramente passivas na mochila (ex.: Necronomicon).
                if (inv.Main != null)
                    for (int i = 0; i < inv.Main.Capacidade; i++)
                    {
                        var slot = inv.Main.GetSlot(i);
                        if (slot?.Def != null && slot.Def.Tipo == ItemType.Chave) Somar(slot);
                    }
            }

            // 3. Artefatos equipados (só valem enquanto ocupam um dos 4 slots).
            if (_artefatos != null)
            {
                for (int i = 0; i < FavelaAmarela.Core.Artefatos.InventarioDeArtefatos.TotalDeSlots; i++)
                {
                    var def = _artefatos.DefNoSlot(i);
                    if (def?.Passivas == null) continue;

                    foreach (var mod in def.Passivas) Acumular(mod);
                }
            }

            // 4. Ecos da Memória.
            if (ProgressionBridge.Instancia != null)
            {
                foreach (var eco in ProgressionBridge.Instancia.EcosDesbloqueados())
                {
                    if (eco.Modificadores == null) continue;
                    foreach (var mod in eco.Modificadores) Acumular(mod);
                }
            }

            _cacheValido = true;
        }

        private void Somar(ItemInstance slot)
        {
            if (slot?.Def == null) return;

            foreach (var mod in slot.ModificadoresEfetivos()) Acumular(mod);
        }

        private void Acumular(ModificadorFixo mod)
        {
            _cache.TryGetValue(mod.Stat, out float atual);
            _cache[mod.Stat] = atual + mod.Valor;
        }
    }
}
