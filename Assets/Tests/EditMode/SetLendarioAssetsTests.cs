using NUnit.Framework;
using UnityEditor;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda as peças do Set Lendário de Set. O conjunto destranca a sidequest do Avatar de
    /// Nyarlathotep, então uma peça com slot ou tipo errado quebraria progressão de final de
    /// jogo — e quieta, porque o item ainda apareceria na mochila.
    /// </summary>
    public class SetLendarioAssetsTests
    {
        private const string Pasta = "Assets/FavelaAmarela/Config/Resources/Itens/";

        private const string Elmo = Pasta + "Item_Armadura_ElmoDeSet.asset";
        private const string Peitoral = Pasta + "Item_Armadura_PeitoralDeSet.asset";
        private const string Grevas = Pasta + "Item_Armadura_GrevasDeSet.asset";

        [TestCase(Elmo, "set_elmo", EquipmentSlot.Elmo)]
        [TestCase(Peitoral, "set_peitoral", EquipmentSlot.Peitoral)]
        [TestCase(Grevas, "set_grevas", EquipmentSlot.Grevas)]
        public void Peca_EhArmaduraNoSlotCerto(string caminho, string idEsperado, EquipmentSlot slotEsperado)
        {
            var def = AssetDatabase.LoadAssetAtPath<ItemDef>(caminho);
            Assert.IsNotNull(def, $"Asset não encontrado: {caminho}");

            Assert.AreEqual(idEsperado, def.Id);
            Assert.AreEqual(ItemType.Armadura, def.Tipo);
            Assert.AreEqual(slotEsperado, def.SlotEquipamento);
            Assert.AreEqual(1, def.EmpilhamentoMaximo, "Armadura não empilha.");
        }

        [TestCase(Elmo)]
        [TestCase(Peitoral)]
        [TestCase(Grevas)]
        public void Peca_ConcedeDefesaAcimaDoCatalogoComum(string caminho)
        {
            var def = AssetDatabase.LoadAssetAtPath<ItemDef>(caminho);

            float defesa = 0f;
            foreach (var mod in def.Modificadores)
                if (mod.Stat == StatType.DefesaFisica) defesa += mod.Valor;

            // As armaduras Inerte do catálogo comum dão +1. O Set é de outra ordem de grandeza
            // de propósito — é armadura de um deus primordial.
            Assert.Greater(defesa, 1f, $"'{def.Nome}' não se distingue de uma armadura Inerte.");
        }

        [Test]
        public void ElmoDeSet_NaoEhACoroaDeOssos()
        {
            var elmo = AssetDatabase.LoadAssetAtPath<ItemDef>(Elmo);
            var coroa = AssetDatabase.LoadAssetAtPath<ItemDef>(Pasta + "Item_CoroaDeOssos.asset");

            // Uma nota de lore de 2026-07-28 afirmava que eram o mesmo item. Não são: a Coroa é
            // Artefato (sem slot), o Elmo é armadura de cabeça. Se alguém tentar fundi-los de
            // novo, este teste cai.
            Assert.AreNotEqual(elmo.Id, coroa.Id);
            Assert.AreEqual(ItemType.Armadura, elmo.Tipo);
            Assert.AreEqual(ItemType.Artefato, coroa.Tipo);
            Assert.AreEqual(EquipmentSlot.Nenhum, coroa.SlotEquipamento,
                "Artefato não ocupa slot de corpo.");
        }
    }
}
