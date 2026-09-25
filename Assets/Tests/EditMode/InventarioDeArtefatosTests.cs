using NUnit.Framework;
using FavelaAmarela.Core.Artefatos;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Suite EditMode dos quatro slots de Artefato. O limite de quatro é decisão de design —
    /// carregar um Artefato precisa custar deixar outro para trás — então as regras que o
    /// sustentam (recusa de duplicata, índice inválido) são afirmadas aqui.
    /// </summary>
    public class InventarioDeArtefatosTests
    {
        [Test]
        public void NasceComQuatroSlotsVazios()
        {
            var inv = new InventarioDeArtefatos();

            Assert.AreEqual(4, inv.Capacidade);
            for (int i = 0; i < inv.Capacidade; i++)
                Assert.IsNull(inv.IdNoSlot(i));
        }

        [Test]
        public void Equipar_OcupaOSlot()
        {
            var inv = new InventarioDeArtefatos();

            inv.Equipar("necronomicon", 0);

            Assert.AreEqual("necronomicon", inv.IdNoSlot(0));
            Assert.IsTrue(inv.Contem("necronomicon"));
        }

        [Test]
        public void MesmoArtefatoEmDoisSlots_Recusado()
        {
            var inv = new InventarioDeArtefatos();
            inv.Equipar("necronomicon", 0);

            inv.Equipar("necronomicon", 1);

            // Duplicar daria a passiva duas vezes e gastaria um slot à toa.
            Assert.IsNull(inv.IdNoSlot(1));
        }

        [Test]
        public void Equipar_SobreSlotOcupado_DevolveOAnterior()
        {
            var inv = new InventarioDeArtefatos();
            inv.Equipar("patua", 2);

            string deslocado = inv.Equipar("coroa_de_ossos", 2);

            Assert.AreEqual("patua", deslocado);
            Assert.AreEqual("coroa_de_ossos", inv.IdNoSlot(2));
        }

        [Test]
        public void Desequipar_EsvaziaEDevolve()
        {
            var inv = new InventarioDeArtefatos();
            inv.Equipar("anel_sinal_amarelo", 3);

            string retirado = inv.Desequipar(3);

            Assert.AreEqual("anel_sinal_amarelo", retirado);
            Assert.IsNull(inv.IdNoSlot(3));
            Assert.IsFalse(inv.Contem("anel_sinal_amarelo"));
        }

        [Test]
        public void IndiceInvalido_NaoEstoura()
        {
            var inv = new InventarioDeArtefatos();

            Assert.IsNull(inv.IdNoSlot(-1));
            Assert.IsNull(inv.IdNoSlot(4));
            Assert.IsNull(inv.Equipar("necronomicon", 9));
            Assert.IsNull(inv.Desequipar(-2));
        }

        [Test]
        public void PrimeiroSlotLivre_AchaEDevolveMenosUmQuandoCheio()
        {
            var inv = new InventarioDeArtefatos();
            Assert.AreEqual(0, inv.PrimeiroSlotLivre());

            inv.Equipar("a", 0);
            Assert.AreEqual(1, inv.PrimeiroSlotLivre());

            inv.Equipar("b", 1);
            inv.Equipar("c", 2);
            inv.Equipar("d", 3);
            Assert.AreEqual(-1, inv.PrimeiroSlotLivre());
        }

        [Test]
        public void OnMudou_DisparaAoEquiparEDesequipar()
        {
            var inv = new InventarioDeArtefatos();
            int avisos = 0;
            inv.OnMudou += () => avisos++;

            inv.Equipar("necronomicon", 0);
            inv.Desequipar(0);

            Assert.AreEqual(2, avisos);
        }

        [Test]
        public void OnMudou_NaoDisparaEmOperacaoQueNaoMudouNada()
        {
            var inv = new InventarioDeArtefatos();
            inv.Equipar("necronomicon", 0);

            int avisos = 0;
            inv.OnMudou += () => avisos++;

            inv.Equipar("necronomicon", 0);  // já está lá
            inv.Equipar("necronomicon", 1);  // duplicata recusada
            inv.Desequipar(2);               // slot vazio

            Assert.AreEqual(0, avisos);
        }
    }
}
