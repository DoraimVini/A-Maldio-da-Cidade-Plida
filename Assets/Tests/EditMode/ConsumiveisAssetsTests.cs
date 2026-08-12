using NUnit.Framework;
using UnityEditor;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda o catálogo de consumíveis. O efeito deles é **puro dado**: o
    /// <c>VitalidadeBridge.AplicarEfeitoConsumivel</c> lê os <c>Modificadores</c> e trata
    /// <c>VitMaxima</c> como Estabilização do corpo e <c>RMMaxima</c> como Ancoragem da mente.
    /// Um consumível sem modificador é engolido em silêncio — o jogador gasta o item e nada
    /// acontece, sem um erro sequer. É isso que estes testes impedem.
    /// </summary>
    public class ConsumiveisAssetsTests
    {
        private const string Pasta = "Assets/FavelaAmarela/Config/Resources/Itens/";

        private const string Agua = Pasta + "Item_Consumivel_AguaDaCacimba.asset";
        private const string Erva = Pasta + "Item_Consumivel_ErvaDeAncoragem.asset";
        private const string Raiz = Pasta + "Item_Consumivel_RaizDeYhtill.asset";

        [TestCase(Agua, "consumivel_agua_cacimba")]
        [TestCase(Erva, "consumivel_erva_ancoragem")]
        [TestCase(Raiz, "consumivel_raiz_yhtill")]
        public void Consumivel_EhDoTipoCertoEEmpilha(string caminho, string idEsperado)
        {
            var def = AssetDatabase.LoadAssetAtPath<ItemDef>(caminho);
            Assert.IsNotNull(def, $"Asset não encontrado: {caminho}");

            Assert.AreEqual(idEsperado, def.Id);
            Assert.AreEqual(ItemType.Consumivel, def.Tipo,
                "Tipo errado faz o InventoryManager.ConsumirItem recusar o item.");
            Assert.AreEqual(EquipmentSlot.Nenhum, def.SlotEquipamento);
            Assert.Greater(def.EmpilhamentoMaximo, 1, "Consumível precisa empilhar.");
        }

        [TestCase(Agua)]
        [TestCase(Erva)]
        [TestCase(Raiz)]
        public void Consumivel_TemAlgumEfeitoReal(string caminho)
        {
            var def = AssetDatabase.LoadAssetAtPath<ItemDef>(caminho);

            Assert.IsNotNull(def.Modificadores);
            Assert.IsNotEmpty(def.Modificadores, $"'{def.Nome}' não faz nada ao ser consumido.");

            foreach (var mod in def.Modificadores)
            {
                Assert.Greater(mod.Valor, 0f, $"'{def.Nome}' tem modificador de valor zero.");

                // Só estes dois canais são tratados no consumo hoje. Um consumível autorado
                // com outro StatType seria gasto sem efeito nenhum.
                Assert.IsTrue(mod.Stat == StatType.VitMaxima || mod.Stat == StatType.RMMaxima,
                    $"'{def.Nome}' usa '{mod.Stat}', que o consumo ignora — o item seria gasto à toa.");
            }
        }

        [Test]
        public void CatalogoCobreCorpoEMente()
        {
            var agua = AssetDatabase.LoadAssetAtPath<ItemDef>(Agua);
            var erva = AssetDatabase.LoadAssetAtPath<ItemDef>(Erva);

            Assert.AreEqual(StatType.VitMaxima, agua.Modificadores[0].Stat,
                "A Água da Cacimba é o consumível de corpo.");
            Assert.AreEqual(StatType.RMMaxima, erva.Modificadores[0].Stat,
                "A Erva de Ancoragem é o consumível de mente.");
        }
    }
}
