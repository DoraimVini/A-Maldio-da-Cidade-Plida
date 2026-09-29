using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// O inventário diz <b>por que</b> recusa — e não perde item recusando.
    ///
    /// <para><b>O que motivou (2026-09-28, plano B do inventário):</b> o <c>Equipar</c> só
    /// devolvia <c>false</c> e a tela ficava igual; e o <c>Desequipar</c> com a mochila cheia
    /// tirava o item do corpo, não conseguia guardá-lo, e o item <b>sumia</b> com um aviso de
    /// console ("dropado no chão" — não havia chão, havia um TODO).</para>
    /// </summary>
    public sealed class RecusasDoInventarioTests
    {
        private GameObject _objetoDoBanco;
        private GameObject _objetoDoGerente;
        private ItemDatabase _banco;
        private InventoryManager _gerente;
        private readonly List<ItemDef> _defs = new List<ItemDef>();

        [SetUp]
        public void SetUp()
        {
            _objetoDoBanco = new GameObject("BancoDeTeste");
            _banco = _objetoDoBanco.AddComponent<ItemDatabase>();
            _banco.InitializeForTesting();

            Def("erva", ItemType.Consumivel, EquipmentSlot.Nenhum, 5);
            Def("pedra", ItemType.Chave, EquipmentSlot.Nenhum, 1);
            Def("elmo", ItemType.Armadura, EquipmentSlot.Elmo, 1);
            Def("broquel", ItemType.Armadura, EquipmentSlot.MaoSecundaria, 1);
            var espadao = Def("espadao", ItemType.Arma, EquipmentSlot.Arma, 1);
            espadao.Empunhadura = Empunhadura.DuasMaos;

            _objetoDoGerente = new GameObject("GerenteDeTeste");
            _gerente = _objetoDoGerente.AddComponent<InventoryManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_objetoDoGerente != null) Object.DestroyImmediate(_objetoDoGerente);
            if (_objetoDoBanco != null) Object.DestroyImmediate(_objetoDoBanco);
            foreach (var d in _defs) if (d != null) Object.DestroyImmediate(d);
            _defs.Clear();
        }

        private ItemDef Def(string id, ItemType tipo, EquipmentSlot slot, int pilha)
        {
            var d = ScriptableObject.CreateInstance<ItemDef>();
            d.Id = id;
            d.Nome = id;
            d.Tipo = tipo;
            d.SlotEquipamento = slot;
            d.EmpilhamentoMaximo = pilha;
            d.Modificadores = new List<ModificadorFixo>();
            _banco.Registrar(d);
            _defs.Add(d);
            return d;
        }

        private void Por(string id, int slot) => _gerente.Main.AddAt(new ItemInstance(id, 1), slot);

        private void Vestir(string id)
        {
            Por(id, 0);
            Assert.IsTrue(_gerente.Equipar(0), $"Não consegui vestir '{id}' para montar o teste.");
        }

        private void EncherAMochila()
        {
            for (int i = 0; i < _gerente.Main.Capacidade; i++)
                if (_gerente.Main.GetSlot(i) == null) Por("pedra", i);
        }

        [Test]
        public void Consumivel_NaoSeVeste_EODizendo()
        {
            Por("erva", 0);
            Assert.AreEqual(RecusaAoEquipar.NaoSeVeste, _gerente.PorQueNaoEquipa(0));
            Assert.IsFalse(_gerente.Equipar(0));
        }

        [Test]
        public void EscudoComArmaDeDuasMaos_ERecusado_PorCausaDasMaos()
        {
            Vestir("espadao");
            Por("broquel", 3);

            Assert.AreEqual(RecusaAoEquipar.MaosTomadasPorArmaDeDuasMaos, _gerente.PorQueNaoEquipa(3));
            Assert.IsFalse(_gerente.Equipar(3), "O Equipar aceitou o que a recusa diz ser impossível.");
            Assert.AreEqual("broquel", _gerente.Main.GetSlot(3)?.ItemDefId, "O escudo recusado saiu da mochila.");
        }

        [Test]
        public void ArmaDeDuasMaos_ComEscudoEMochilaCheia_ERecusada_SemPerderNada()
        {
            Vestir("broquel");
            Por("espadao", 0);
            EncherAMochila();

            Assert.AreEqual(RecusaAoEquipar.SemEspacoParaGuardarAMaoSecundaria, _gerente.PorQueNaoEquipa(0));
            Assert.IsFalse(_gerente.Equipar(0));
            Assert.IsTrue(_gerente.Equipment.MaoSecundariaOcupada, "O escudo saiu da mão numa troca recusada.");
            Assert.AreEqual("espadao", _gerente.Main.GetSlot(0)?.ItemDefId);
        }

        [Test]
        public void Desequipar_ComAMochilaCheia_Recusa_EOItemFicaNoCorpo()
        {
            Por("elmo", 0);
            Assert.IsTrue(_gerente.Equipar(0));
            int slotDoElmo = _gerente.Equipment.IndiceDoSlot(EquipmentSlot.Elmo);
            EncherAMochila();

            Assert.AreEqual(RecusaAoDesequipar.MochilaCheia, _gerente.PorQueNaoDesequipa(slotDoElmo));
            Assert.IsFalse(_gerente.Desequipar(slotDoElmo));
            Assert.AreEqual("elmo", _gerente.Equipment.GetSlot(slotDoElmo)?.ItemDefId,
                "O elmo sumiu: saiu do corpo sem caber na mochila — o defeito de antes de 2026-09-28.");
        }

        [Test]
        public void Desequipar_ComEspaco_GuardaNaMochila()
        {
            Por("elmo", 0);
            Assert.IsTrue(_gerente.Equipar(0));
            int slotDoElmo = _gerente.Equipment.IndiceDoSlot(EquipmentSlot.Elmo);

            Assert.AreEqual(RecusaAoDesequipar.Nenhuma, _gerente.PorQueNaoDesequipa(slotDoElmo));
            Assert.IsTrue(_gerente.Desequipar(slotDoElmo));
            Assert.IsTrue(_gerente.PossuiItemNaMochila("elmo"));
        }
    }
}
