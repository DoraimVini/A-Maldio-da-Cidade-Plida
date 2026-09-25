using NUnit.Framework;
using UnityEngine;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Trava o bug de 2026-08-11 ("perde a arma no deserto"): carregar um save recriava os
    /// inventários com <c>new</c>, e todo inscrito em <c>OnSlotChanged</c> passava a escutar
    /// um objeto morto — a Mão Física nunca mais sabia que uma arma tinha sido equipada.
    ///
    /// <para>O sintoma não dava erro no console: o inventário estava certo, só ninguém era
    /// avisado. Estes testes verificam a propriedade que faltava — <b>o objeto continua o
    /// mesmo, e os eventos continuam chegando</b>.</para>
    /// </summary>
    public sealed class InventarioPreservaInscritosTests
    {
        private GameObject _databaseObject;
        private ItemDatabase _database;
        private ItemDef _armaVelha;
        private ItemDef _armaDoSave;

        [SetUp]
        public void SetUp()
        {
            // O inventário rejeita item cujo id não resolve num ItemDef — por isso o banco
            // precisa existir aqui, mesmo o teste sendo sobre eventos e não sobre itens.
            _databaseObject = new GameObject("TestItemDatabase");
            _database = _databaseObject.AddComponent<ItemDatabase>();
            _database.InitializeForTesting();

            _armaVelha = CriarArma("arma_velha", "Arma Velha");
            _armaDoSave = CriarArma("arma_do_save", "Arma do Save");
        }

        [TearDown]
        public void TearDown()
        {
            ItemDatabase.ClearInstanceForTesting();

            if (_armaVelha != null) ScriptableObject.DestroyImmediate(_armaVelha);
            if (_armaDoSave != null) ScriptableObject.DestroyImmediate(_armaDoSave);
            if (_databaseObject != null) Object.DestroyImmediate(_databaseObject);
        }

        private ItemDef CriarArma(string id, string nome)
        {
            var def = ScriptableObject.CreateInstance<ItemDef>();
            def.Id = id;
            def.Nome = nome;
            def.Tipo = ItemType.Arma;
            def.EmpilhamentoMaximo = 1;

            _database.Registrar(def);
            return def;
        }

        [Test]
        public void LimparTudo_NotificaCadaSlotOcupado()
        {
            var inv = new MainInventory(4);
            inv.AddAt(new ItemInstance(_armaVelha.Id, 1), 0);
            inv.AddAt(new ItemInstance(_armaDoSave.Id, 1), 2);

            int avisos = 0;
            inv.OnSlotChanged += _ => avisos++;

            inv.LimparTudo();

            Assert.AreEqual(2, avisos, "Cada slot esvaziado deve avisar quem escuta.");
            Assert.IsNull(inv.GetSlot(0));
            Assert.IsNull(inv.GetSlot(2));
        }

        [Test]
        public void LimparTudo_NaoAvisaSlotQueJaEstavaVazio()
        {
            var inv = new MainInventory(3);

            int avisos = 0;
            inv.OnSlotChanged += _ => avisos++;

            inv.LimparTudo();

            // Avisar slot vazio faria a UI redesenhar à toa a cada carregamento.
            Assert.AreEqual(0, avisos);
        }

        [Test]
        public void Inscrito_ContinuaRecebendoDepoisDeLimparERepovoar()
        {
            // Simula o ciclo do carregamento de save: limpa e repõe, na MESMA instância.
            var inv = new MainInventory(4);
            inv.AddAt(new ItemInstance(_armaVelha.Id, 1), 0);

            string ultimoVisto = null;
            inv.OnSlotChanged += i => ultimoVisto = inv.GetSlot(i)?.ItemDefId;

            inv.LimparTudo();
            inv.AddAt(new ItemInstance(_armaDoSave.Id, 1), 0);

            Assert.AreEqual(_armaDoSave.Id, ultimoVisto,
                "O inscrito tem de ouvir o item que veio do save — era exatamente isto que " +
                "se perdia quando o inventário era recriado com new.");
        }
    }
}
