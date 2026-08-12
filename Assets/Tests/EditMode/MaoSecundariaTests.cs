using NUnit.Framework;
using UnityEngine;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Suite do <b>7º slot</b> (<see cref="EquipmentSlot.MaoSecundaria"/>) e das regras de
    /// <see cref="Empunhadura"/>: a escolha tática entre arma leve + foco/escudo na off-hand
    /// e uma lâmina colossal que toma as duas mãos.
    ///
    /// <para>Cobre também a <b>migração de save 6 → 7 slots</b>, que é o risco real da
    /// mudança: até aqui, divergência de capacidade fazia o <c>InventoryManager</c> recriar o
    /// contêiner com <c>new</c> e deixar órfão todo inscrito nos eventos — o bug "perde a
    /// arma no deserto". O ramo era inalcançável enquanto a anatomia nunca mudava; ao entrar
    /// a Mão Secundária, todo save antigo passaria por ele.</para>
    /// </summary>
    public sealed class MaoSecundariaTests
    {
        private GameObject _databaseObject;
        private ItemDatabase _database;

        private ItemDef _espadaDeUmaMao;
        private ItemDef _espadaoDeDuasMaos;
        private ItemDef _focoDeOffHand;
        private ItemDef _elmo;

        /// <summary>Anatomia corrente do jogo — Arma em 0, Mão Secundária no fim.</summary>
        private static EquipmentSlot[] AnatomiaAtual => new[]
        {
            EquipmentSlot.Arma,
            EquipmentSlot.Elmo,
            EquipmentSlot.Peitoral,
            EquipmentSlot.Grevas,
            EquipmentSlot.Amuleto,
            EquipmentSlot.Anel,
            EquipmentSlot.MaoSecundaria
        };

        /// <summary>Anatomia anterior a 2026-08-12, para simular um save antigo.</summary>
        private static EquipmentSlot[] AnatomiaLegada => new[]
        {
            EquipmentSlot.Arma,
            EquipmentSlot.Elmo,
            EquipmentSlot.Peitoral,
            EquipmentSlot.Grevas,
            EquipmentSlot.Amuleto,
            EquipmentSlot.Anel
        };

        [SetUp]
        public void SetUp()
        {
            _databaseObject = new GameObject("TestItemDatabase");
            _database = _databaseObject.AddComponent<ItemDatabase>();
            _database.InitializeForTesting();

            _espadaDeUmaMao = CriarDef("espada_1m", ItemType.Arma, EquipmentSlot.Arma, Empunhadura.UmaMao);
            _espadaoDeDuasMaos = CriarDef("espadao_2m", ItemType.Arma, EquipmentSlot.Arma, Empunhadura.DuasMaos);
            _focoDeOffHand = CriarDef("foco_offhand", ItemType.Armadura, EquipmentSlot.MaoSecundaria, Empunhadura.UmaMao);
            _elmo = CriarDef("elmo_teste", ItemType.Armadura, EquipmentSlot.Elmo, Empunhadura.UmaMao);
        }

        [TearDown]
        public void TearDown()
        {
            ItemDatabase.ClearInstanceForTesting();

            foreach (var def in new[] { _espadaDeUmaMao, _espadaoDeDuasMaos, _focoDeOffHand, _elmo })
                if (def != null) ScriptableObject.DestroyImmediate(def);

            if (_databaseObject != null) Object.DestroyImmediate(_databaseObject);
        }

        private ItemDef CriarDef(string id, ItemType tipo, EquipmentSlot slot, Empunhadura empunhadura)
        {
            var def = ScriptableObject.CreateInstance<ItemDef>();
            def.Id = id;
            def.Nome = id;
            def.Tipo = tipo;
            def.SlotEquipamento = slot;
            def.Empunhadura = empunhadura;
            def.EmpilhamentoMaximo = 1;

            _database.Registrar(def);
            return def;
        }

        // ── Anatomia ─────────────────────────────────────────────────────────

        [Test]
        public void Anatomia_TemMaoSecundaria_NoFim()
        {
            var equip = new EquipmentInventory(AnatomiaAtual);

            Assert.AreEqual(7, equip.Capacidade);
            Assert.AreEqual(6, equip.IndiceDoSlot(EquipmentSlot.MaoSecundaria));

            // A Arma tem de continuar em 0: a MaoFisicaBridge escuta esse índice para
            // reconstruir a arma pela WeaponFactory.
            Assert.AreEqual(0, equip.IndiceDoSlot(EquipmentSlot.Arma));
        }

        [Test]
        public void IndiceDoSlot_TipoAusente_RetornaMenosUm()
        {
            var equip = new EquipmentInventory(AnatomiaLegada);
            Assert.AreEqual(-1, equip.IndiceDoSlot(EquipmentSlot.MaoSecundaria));
        }

        // ── Regras de empunhadura ────────────────────────────────────────────

        [Test]
        public void ArmaDeUmaMao_DeixaOffHandLivre()
        {
            var equip = new EquipmentInventory(AnatomiaAtual);
            equip.Equip(new ItemInstance(_espadaDeUmaMao.Id), 0);

            Assert.IsFalse(equip.ArmaDeDuasMaosEquipada);
            Assert.IsTrue(equip.CanAdd(new ItemInstance(_focoDeOffHand.Id), 6),
                "Espada de uma mão tem de permitir foco na secundária.");
        }

        [Test]
        public void ArmaDeDuasMaos_BloqueiaOffHand()
        {
            var equip = new EquipmentInventory(AnatomiaAtual);
            equip.Equip(new ItemInstance(_espadaoDeDuasMaos.Id), 0);

            Assert.IsTrue(equip.ArmaDeDuasMaosEquipada);
            Assert.IsFalse(equip.CanAdd(new ItemInstance(_focoDeOffHand.Id), 6),
                "Com as duas mãos na espada não sobra mão para o foco.");
        }

        [Test]
        public void OffHandOcupada_RecusaArmaDeDuasMaos()
        {
            var equip = new EquipmentInventory(AnatomiaAtual);
            equip.Equip(new ItemInstance(_focoDeOffHand.Id), 6);

            Assert.IsTrue(equip.MaoSecundariaOcupada);
            Assert.IsFalse(equip.CanAdd(new ItemInstance(_espadaoDeDuasMaos.Id), 0),
                "Recusa, não desalojamento: liberar a off-hand exige mochila com espaço, " +
                "e quem sabe disso é o InventoryManager.");
        }

        [Test]
        public void OffHandOcupada_AindaAceitaArmaDeUmaMao()
        {
            var equip = new EquipmentInventory(AnatomiaAtual);
            equip.Equip(new ItemInstance(_focoDeOffHand.Id), 6);

            Assert.IsTrue(equip.CanAdd(new ItemInstance(_espadaDeUmaMao.Id), 0));
        }

        [Test]
        public void OffHand_NaoAceitaItemDeOutroSlot()
        {
            var equip = new EquipmentInventory(AnatomiaAtual);

            // A validação de encaixe antiga continua valendo: elmo não vai na mão.
            Assert.IsFalse(equip.CanAdd(new ItemInstance(_elmo.Id), 6));
        }

        // ── Migração de save 6 → 7 ───────────────────────────────────────────

        [Test]
        public void SaveDeSeisSlots_NaoRecriaOEquipamento()
        {
            var manager = CriarManager();
            var antes = manager.Equipment;

            manager.LoadFromSaveData(SaveLegadoComArmaEElmo());

            Assert.AreSame(antes, manager.Equipment,
                "Recriar o contêiner deixa órfãos MaoFisicaBridge, GerenciadorEfeitosPassivos " +
                "e as barras de UI — é o bug 'perde a arma no deserto' voltando pela porta " +
                "da anatomia nova.");

            Object.DestroyImmediate(manager.gameObject);
        }

        [Test]
        public void SaveDeSeisSlots_InscritosContinuamRecebendo()
        {
            var manager = CriarManager();

            string armaVistaPeloInscrito = null;
            manager.Equipment.OnSlotChanged += i =>
            {
                if (i == 0) armaVistaPeloInscrito = manager.Equipment.GetSlot(0)?.ItemDefId;
            };

            manager.LoadFromSaveData(SaveLegadoComArmaEElmo());

            Assert.AreEqual(_espadaDeUmaMao.Id, armaVistaPeloInscrito,
                "A Mão Física precisa ouvir a arma que veio do save antigo.");

            Object.DestroyImmediate(manager.gameObject);
        }

        [Test]
        public void SaveDeSeisSlots_ItensCaemNosSlotsCertos_ESecundariaFicaVazia()
        {
            var manager = CriarManager();

            manager.LoadFromSaveData(SaveLegadoComArmaEElmo());

            Assert.AreEqual(7, manager.Equipment.Capacidade, "A anatomia atual manda, não a do save.");
            Assert.AreEqual(_espadaDeUmaMao.Id, manager.Equipment.GetSlot(0)?.ItemDefId);
            Assert.AreEqual(_elmo.Id, manager.Equipment.GetSlot(1)?.ItemDefId);
            Assert.IsNull(manager.Equipment.GetSlot(6), "Save antigo não tinha off-hand: fica vazia.");

            Object.DestroyImmediate(manager.gameObject);
        }

        [Test]
        public void SaveNovo_TrazVersaoAtual()
        {
            var manager = CriarManager();

            var salvo = manager.GetSaveData();

            Assert.AreEqual(InventorySaveData.VersaoAtual, salvo.saveVersion);
            Assert.AreEqual(7, salvo.equipSlotData.Length);

            Object.DestroyImmediate(manager.gameObject);
        }

        // ── Apoio ────────────────────────────────────────────────────────────

        /// <summary>
        /// Cria o manager <b>sem disparar Awake</b> — em EditMode a Unity não chama Awake em
        /// scripts sem <c>[ExecuteAlways]</c>, então o singleton estático não é tocado e os
        /// testes não interferem uns nos outros. Os inventários nascem pelos getters preguiçosos.
        /// </summary>
        private static InventoryManager CriarManager()
        {
            var go = new GameObject("TestInventoryManager");
            return go.AddComponent<InventoryManager>();
        }

        /// <summary>Save gravado pela anatomia de 6 slots, com arma no 0 e elmo no 1.</summary>
        private InventorySaveData SaveLegadoComArmaEElmo()
        {
            var equipLegado = new EquipmentInventory(AnatomiaLegada);
            equipLegado.Equip(new ItemInstance(_espadaDeUmaMao.Id), 0);
            equipLegado.Equip(new ItemInstance(_elmo.Id), 1);

            var dados = new InventorySaveData(new MainInventory(12), equipLegado);

            // Um save de verdade, anterior ao campo, desserializa saveVersion como 0.
            dados.saveVersion = 0;
            Assert.AreEqual(6, dados.equipSlotData.Length, "Pré-condição: o save simulado tem 6 slots.");

            return dados;
        }
    }
}
