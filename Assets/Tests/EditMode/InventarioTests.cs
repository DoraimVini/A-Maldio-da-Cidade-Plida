using NUnit.Framework;
using UnityEngine;
using FavelaAmarela.Inventario;
using System.Collections.Generic;

namespace FavelaAmarela.Tests.EditMode
{
    public sealed class InventarioTests
    {
        private GameObject _databaseObject;
        private ItemDatabase _itemDatabase;

        // ItemDefs reutilizáveis — criados uma vez no SetUp, destruídos no TearDown.
        private ItemDef _cinza;
        private ItemDef _emplastro;
        private ItemDef _arma;

        [SetUp]
        public void SetUp()
        {
            // 1. Cria o GameObject e o componente ItemDatabase
            _databaseObject = new GameObject("TestItemDatabase");
            _itemDatabase = _databaseObject.AddComponent<ItemDatabase>();

            // 2. Inicializa o Singleton e o dicionário interno via contrato oficial
            _itemDatabase.InitializeForTesting();

            // 3. Cria e registra todos os ItemDefs necessários
            _cinza = ScriptableObject.CreateInstance<ItemDef>();
            _cinza.Id = "cinza_de_ancora";
            _cinza.Nome = "Cinza de Âncora";
            _cinza.Tipo = ItemType.Consumivel;
            _cinza.EmpilhamentoMaximo = 5;
            _cinza.Modificadores = new List<ModificadorFixo>
            {
                new ModificadorFixo { Stat = StatType.RMMaxima, Valor = 25f }
            };
            _itemDatabase.Registrar(_cinza);

            _emplastro = ScriptableObject.CreateInstance<ItemDef>();
            _emplastro.Id = "emplastro";
            _emplastro.Nome = "Emplastro de Sal";
            _emplastro.Tipo = ItemType.Consumivel;
            _emplastro.EmpilhamentoMaximo = 3;
            _emplastro.Modificadores = new List<ModificadorFixo>
            {
                new ModificadorFixo { Stat = StatType.VitMaxima, Valor = 30f }
            };
            _itemDatabase.Registrar(_emplastro);

            _arma = ScriptableObject.CreateInstance<ItemDef>();
            _arma.Id = "arma_estilete";
            _arma.Nome = "Estilete de Irem";
            _arma.Tipo = ItemType.Arma;
            _arma.EmpilhamentoMaximo = 1;
            _itemDatabase.Registrar(_arma);
        }

        [TearDown]
        public void TearDown()
        {
            // Limpa o Singleton antes de destruir o GameObject
            ItemDatabase.ClearInstanceForTesting();

            if (_cinza != null) ScriptableObject.DestroyImmediate(_cinza);
            if (_emplastro != null) ScriptableObject.DestroyImmediate(_emplastro);
            if (_arma != null) ScriptableObject.DestroyImmediate(_arma);

            if (_databaseObject != null)
                Object.DestroyImmediate(_databaseObject);
        }

        // ── Guardar ──────────────────────────────────────────────────────────

        [Test]
        public void InventarioNovo_ComecaVazio()
        {
            var inv = new MainInventory(4);
            Assert.AreEqual(4, inv.Capacidade);
            for (int i = 0; i < inv.Capacidade; i++) Assert.IsNull(inv.GetSlot(i));
        }

        [Test]
        public void Adicionar_GuardaItem()
        {
            var inv = new MainInventory(4);
            var inst = new ItemInstance("cinza_de_ancora", 3);

            bool success = inv.Add(inst);

            Assert.IsTrue(success);
            Assert.IsNotNull(inv.GetSlot(0));
            Assert.AreEqual(3, inv.GetSlot(0).Quantidade);
        }

        [Test]
        public void Remove_TiraAQuantidadePedida()
        {
            var inv = new MainInventory(4);
            var inst = new ItemInstance("cinza_de_ancora", 4);
            inv.Add(inst);
            Assert.AreEqual(4, inv.GetSlot(0).Quantidade);

            inv.Remove(0, 3);
            Assert.AreEqual(1, inv.GetSlot(0).Quantidade);
        }

        [Test]
        public void Consumir_EsvaziaAPosicaoQuandoZera()
        {
            var inv = new MainInventory(4);
            var inst = new ItemInstance("cinza_de_ancora", 1);
            inv.Add(inst);

            inv.Remove(0, 1);
            Assert.IsNull(inv.GetSlot(0));
        }

        // ── Evento ───────────────────────────────────────────────────────────

        [Test]
        public void OnMudou_DisparaAoAlterar()
        {
            var inv = new MainInventory(4);
            int mudancas = 0;
            inv.OnSlotChanged += (_) => mudancas++;

            var inst = new ItemInstance("cinza_de_ancora", 2);

            inv.Add(inst);
            inv.Remove(0, 1);
            inv.Remove(0, 1); // Remove o resto

            Assert.AreEqual(3, mudancas); // Add + Remove + Remove
        }

        [Test]
        public void Arma_NuncaEmpilha_OcupaPosicaoPorExemplar()
        {
            var inv = new MainInventory(4);

            var inst1 = new ItemInstance("arma_estilete", 1);
            var inst2 = new ItemInstance("arma_estilete", 1);

            inv.Add(inst1);
            inv.Add(inst2);

            Assert.AreEqual(1, inv.GetSlot(0).Quantidade);
            Assert.AreEqual(1, inv.GetSlot(1).Quantidade);
        }
    }
}

