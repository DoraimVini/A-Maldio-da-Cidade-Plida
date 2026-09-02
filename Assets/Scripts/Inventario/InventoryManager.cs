// Assets/Scripts/Inventario/InventoryManager.cs
using UnityEngine;

namespace FavelaAmarela.Inventario
{
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        /// <summary>
        /// Garante que o inventário exista. Idempotente.
        ///
        /// <para>Continua com <c>[RuntimeInitializeOnLoadMethod]</c> próprio de propósito: a
        /// <c>RaizPersistente</c> chama este método em ordem explícita, mas a Unity não garante
        /// qual dos dois roda primeiro. Manter os dois caminhos faz o inventário existir de
        /// qualquer jeito.</para>
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void GarantirInstancia()
        {
            if (Instance == null)
            {
                var prefab = Resources.Load<GameObject>("InventoryManager");
                if (prefab != null)
                {
                    var obj = Instantiate(prefab);
                    DontDestroyOnLoad(obj);
                }
            }
        }

        [Header("Mochila")]
        [SerializeField] private int capacidadeMochila = MainInventory.DefaultCapacidadeSurvivalHorror;

        [Header("Slots do Corpo (ordem define índices)")]
        [Tooltip("A ordem define os índices. Arma DEVE continuar em 0: a MaoFisicaBridge " +
                 "escuta esse índice para reconstruir a arma pela WeaponFactory.")]
        [SerializeField] private EquipmentSlot[] anatomia = {
            EquipmentSlot.Arma,
            EquipmentSlot.Elmo,
            EquipmentSlot.Peitoral,
            EquipmentSlot.Grevas,
            EquipmentSlot.Amuleto,
            EquipmentSlot.Anel,
            EquipmentSlot.MaoSecundaria
        };

        private MainInventory _main;
        private EquipmentInventory _equipment;

        public MainInventory Main
        {
            get
            {
                if (_main == null)
                    _main = new MainInventory(capacidadeMochila);
                return _main;
            }
            private set => _main = value;
        }

        public EquipmentInventory Equipment
        {
            get
            {
                if (_equipment == null)
                    _equipment = new EquipmentInventory(anatomia);
                return _equipment;
            }
            private set => _equipment = value;
        }

        public event System.Action<ItemDef, int> OnItemConsumed;

        public void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (_main == null) _main = new MainInventory(capacidadeMochila);
            if (_equipment == null) _equipment = new EquipmentInventory(anatomia);
        }

        public bool ConsumirItem(int indice)
        {
            var item = Main.GetSlot(indice);
            if (item?.Def == null) return false;
            
            if (item.Def.Tipo != ItemType.Consumivel)
            {
                Debug.LogWarning($"[InventoryManager] '{item.Def.Nome}' não é consumível.");
                return false;
            }

            OnItemConsumed?.Invoke(item.Def, indice);
            Main.Remove(indice, 1);
            return true;
        }

        /// <summary>
        /// Move um item da mochila para o equipamento.
        /// </summary>
        public bool Equipar(int indiceMochila)
        {
            var item = Main.GetSlot(indiceMochila);
            if (item == null || item.Def == null) return false;

            if (!LiberarMaoSecundariaSePreciso(item)) return false;

            if (!Equipment.CanAddAny(item)) return false;

            // Equip sem especificar índice (auto-localiza pelo slot do item)
            ItemInstance antigo = Equipment.Equip(item);
            
            // Se o item.Quantidade for > 0, significa que o AddAt falhou internamente
            if (item.Quantidade > 0) return false; 

            // Remove da mochila a quantidade que foi equipada (sempre 1 para equipamento)
            Main.Remove(indiceMochila, 1);

            // Se havia um item antigo, tenta devolver pra mochila
            if (antigo != null && !Main.Add(antigo))
            {
                Debug.LogWarning("Mochila cheia! Item antigo dropado no chão.");
                // TODO: instanciar loot no mundo
            }

            return true;
        }

        /// <summary>
        /// Empunhar uma arma de duas mãos exige a Mão Secundária vazia — o
        /// <c>EquipmentInventory</c> recusa o contrário. Aqui a secundária é esvaziada para
        /// a mochila antes da troca.
        ///
        /// <para><b>Rollback deliberado:</b> se a mochila não tiver espaço para o item da
        /// off-hand, ele volta para a mão de onde saiu e a troca inteira é cancelada. Sem
        /// isso, o jogador ficaria sem o escudo <em>e</em> sem o espadão, com o item
        /// evaporando entre os dois contêineres.</para>
        /// </summary>
        /// <returns>true se a troca pode prosseguir.</returns>
        private bool LiberarMaoSecundariaSePreciso(ItemInstance aEquipar)
        {
            if (aEquipar.Def.Tipo != ItemType.Arma) return true;
            if (aEquipar.Def.Empunhadura != Empunhadura.DuasMaos) return true;
            if (!Equipment.MaoSecundariaOcupada) return true;

            int indiceOffHand = Equipment.IndiceDoSlot(EquipmentSlot.MaoSecundaria);
            if (indiceOffHand < 0) return true;

            ItemInstance liberado = Equipment.Unequip(indiceOffHand);
            if (liberado == null) return true;

            if (!Main.Add(liberado))
            {
                Equipment.Equip(liberado, indiceOffHand);
                Debug.LogWarning("[InventoryManager] Mochila cheia: sem espaço para guardar o " +
                                 "item da Mão Secundária, a arma de duas mãos não pode ser empunhada.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Desequipa o slot e tenta mover para a mochila.
        /// </summary>
        public bool Desequipar(int indiceEquip)
        {
            ItemInstance retirado = Equipment.Unequip(indiceEquip);
            if (retirado == null) return false;

            if (!Main.Add(retirado))
            {
                Debug.LogWarning("Mochila cheia! Item dropado no chão.");
                // TODO: instanciar loot no mundo
            }
            return true;
        }

        /// <summary>
        /// Move (ou troca) dois slots <b>da mochila</b>.
        ///
        /// <para><b>Por que precisou existir (2026-09-02).</b> O <c>BaseInventory.Swap</c> já
        /// estava escrito, com um comentário dizendo que servia para "o arrastar da UI" — e
        /// tinha <b>zero chamadores</b> em todo o projeto. O jogador conseguia equipar (teclas
        /// 1–8 ou baú) e <b>nunca conseguia mover, reordenar ou descartar nada</b>.</para>
        ///
        /// <para>Este método é a porta: a UI fala com o <c>InventoryManager</c>, nunca com o
        /// <c>BaseInventory</c> direto — é ele que sabe avisar quem escuta.</para>
        /// </summary>
        /// <returns>Se houve movimento.</returns>
        public bool Mover(int origem, int destino)
        {
            if (origem == destino) return false;
            if (origem < 0 || origem >= Main.Capacidade) return false;
            if (destino < 0 || destino >= Main.Capacidade) return false;

            // Mover o nada para lugar nenhum não é movimento, e devolver `true` faria a UI
            // desenhar de novo à toa.
            if (Main.GetSlot(origem) == null && Main.GetSlot(destino) == null) return false;

            Main.Swap(origem, destino);
            return true;
        }

        /// <summary>
        /// Descarta o conteúdo de um slot da mochila.
        ///
        /// <para><b>Não larga no chão</b>, e isso é decisão: um item descartado que reaparece
        /// aos pés do jogador convida a usar o descarte como depósito. Aqui ele some — o que
        /// pede confirmação na UI antes de chamar.</para>
        /// </summary>
        /// <returns>Se havia o que descartar.</returns>
        public bool Descartar(int indiceMochila)
        {
            if (indiceMochila < 0 || indiceMochila >= Main.Capacidade) return false;

            var item = Main.GetSlot(indiceMochila);
            if (item == null || item.Def == null) return false;

            Main.Remove(indiceMochila, item.Quantidade);
            return true;
        }

        // ------------------ Verificação de Itens (Relíquias e Quest) ------------------
        /// <summary>
        /// Verifica se um item (como uma Relíquia passiva ou Chave) existe na Mochila.
        /// Retorna verdadeiro se a quantidade for maior que zero.
        /// </summary>
        public bool PossuiItemNaMochila(string itemDefId)
        {
            for (int i = 0; i < Main.Capacidade; i++)
            {
                var slot = Main.GetSlot(i);
                if (slot != null && slot.Def != null && slot.Def.Id == itemDefId && slot.Quantidade > 0)
                {
                    return true;
                }
            }
            return false;
        }

        // ------------------ Persistência ------------------
        public InventorySaveData GetSaveData() => new InventorySaveData(Main, Equipment);

        /// <summary>
        /// Reaplica um save ao inventário <b>reaproveitando as instâncias existentes</b>.
        ///
        /// <para><b>Bug que motivou (playtest de 2026-08-11: "perde a arma no deserto"):</b>
        /// esta função criava <c>new MainInventory</c> e <c>new EquipmentInventory</c>. Todo
        /// mundo que já tinha assinado <c>OnSlotChanged</c>/<c>OnEquipmentChanged</c> —
        /// <c>MaoFisicaBridge</c>, <c>GerenciadorEfeitosPassivos</c>, <c>BarraDeItens</c>,
        /// <c>PainelDeInventario</c> — continuava escutando o objeto <b>antigo</b>. Na prática:
        /// ao trocar de cena, equipar deixava de chegar à Mão Física, e Damião ficava desarmado
        /// mesmo com a arma na mochila. Nada disso dava erro no console.</para>
        ///
        /// <para>Mutar no lugar preserva os inscritos. Instância nova só quando a capacidade
        /// salva não bate com a atual — e aí o aviso deixa claro que os eventos se perderam.</para>
        /// </summary>
        public void LoadFromSaveData(InventorySaveData data)
        {
            if (data == null) return;

            Main = RestaurarMochila(data);
            Equipment = RestaurarEquipamento(data);
        }

        private MainInventory RestaurarMochila(InventorySaveData data)
        {
            var mochila = Main;

            if (mochila.Capacidade != data.mainSlotData.Length)
            {
                Debug.LogWarning($"[InventoryManager] Mochila do save tem " +
                                 $"{data.mainSlotData.Length} slots e a atual tem {mochila.Capacidade}; " +
                                 "recriando — quem já escutava os eventos vai parar de receber.");
                mochila = new MainInventory(data.mainSlotData.Length);
            }
            else
            {
                mochila.LimparTudo();
            }

            for (int i = 0; i < data.mainSlotData.Length; i++)
            {
                var slot = data.mainSlotData[i];
                if (slot == null || string.IsNullOrEmpty(slot.itemDefId)) continue;

                // ParaInstancia() traz grau, nível do item e afixos rolados junto --
                // reconstruir só id+quantidade descartaria tudo que o exemplar rolou, e
                // o jogador veria a arma dele perder os modificadores ao recarregar.
                mochila.AddAt(slot.ParaInstancia(), i);
            }

            return mochila;
        }

        /// <summary>
        /// Reaplica o equipamento salvo <b>sempre na instância existente</b>, inclusive quando
        /// a anatomia mudou de tamanho.
        ///
        /// <para><b>Por que nunca há <c>new</c> aqui:</b> a versão anterior recriava o
        /// contêiner quando a capacidade do save divergia da atual. Isso é exatamente o bug
        /// documentado em <see cref="LoadFromSaveData"/> — todo inscrito em
        /// <c>OnSlotChanged</c>/<c>OnEquipmentChanged</c> (<c>MaoFisicaBridge</c>,
        /// <c>GerenciadorEfeitosPassivos</c>, <c>BarraDeItens</c>, <c>PainelDeInventario</c>)
        /// continuava escutando o objeto morto, e equipar deixava de chegar à Mão Física.
        /// O ramo era inalcançável enquanto a anatomia nunca mudava; ao adicionar a Mão
        /// Secundária (6 → 7 slots), <b>todo save antigo passaria por ele</b>.</para>
        ///
        /// <para>Itens que não couberem (anatomia encolheu, ou o tipo não bate com o índice
        /// salvo) vão para a mochila em vez de sumir.</para>
        /// </summary>
        private EquipmentInventory RestaurarEquipamento(InventorySaveData data)
        {
            var equipamento = Equipment;
            equipamento.LimparTudo();

            int salvos = data.equipSlotData.Length;
            if (salvos != equipamento.Capacidade)
            {
                Debug.Log($"[InventoryManager] Anatomia do save tem {salvos} slots e a atual " +
                          $"tem {equipamento.Capacidade}. Migrando na mesma instância — os " +
                          "inscritos nos eventos são preservados.");
            }

            for (int i = 0; i < salvos; i++)
            {
                var slot = data.equipSlotData[i];
                if (slot == null || string.IsNullOrEmpty(slot.itemDefId)) continue;

                RestaurarUmEquipamento(equipamento, slot.ParaInstancia(), i);
            }

            return equipamento;
        }

        /// <summary>
        /// Recoloca um item salvo: primeiro no índice original, depois em qualquer slot do
        /// tipo dele (cobre reordenação da anatomia), e em último caso na mochila.
        /// </summary>
        private void RestaurarUmEquipamento(EquipmentInventory equipamento, ItemInstance item, int indiceSalvo)
        {
            // Atenção: Equip devolve o item ANTIGO do slot — null quando o slot estava vazio,
            // que é sempre o caso logo após LimparTudo. Ele não serve como sinal de sucesso.
            // O sinal real é o AddAt ter zerado a quantidade da instância de origem.
            if (indiceSalvo < equipamento.Capacidade && equipamento.CanAdd(item, indiceSalvo))
            {
                equipamento.Equip(item, indiceSalvo);
                if (item.Quantidade == 0) return;
            }

            if (equipamento.CanAddAny(item))
            {
                equipamento.Equip(item);
                if (item.Quantidade == 0) return;
            }

            if (!Main.Add(item))
            {
                Debug.LogWarning($"[InventoryManager] Item '{item.ItemDefId}' do save não coube " +
                                 "no corpo nem na mochila e foi perdido.");
            }
        }
    }
}
