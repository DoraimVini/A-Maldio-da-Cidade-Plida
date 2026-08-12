using System.Collections.Generic;
using NUnit.Framework;
using FavelaAmarela.Core.Artefatos;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Suite da separação entre <b>posse</b> e <b>porte</b> de Artefatos (2026-08-12).
    ///
    /// <para>Antes disto o <c>InventarioDeArtefatos</c> só tinha os quatro slots: possuir e
    /// portar eram a mesma coisa, e não havia onde guardar uma relíquia recolhida a mais.
    /// Agora a posse é ilimitada e sem custo de mochila, e os quatro slots são só o que está
    /// ativo — o resto fica dormente.</para>
    ///
    /// <para>Trava também a distinção de semântica que o combate depende:
    /// <c>Contem</c> = portado (o que o rito do Rei em Amarelo exige) e <c>Possui</c> = tem
    /// (o que uma porta selada pergunta). Trocar um pelo outro é um bug silencioso.</para>
    /// </summary>
    public sealed class PosseDeArtefatosTests
    {
        // ── Aquisição ────────────────────────────────────────────────────────

        [Test]
        public void Adquirir_ComSlotLivre_JaPortaOArtefato()
        {
            var inv = new InventarioDeArtefatos();

            Assert.IsTrue(inv.Adquirir("necronomicon"));

            Assert.IsTrue(inv.Possui("necronomicon"), "Recolher tem de dar posse.");
            Assert.IsTrue(inv.Contem("necronomicon"),
                "Com slot livre, a relíquia entra portada — recolher e não ver nada acontecer " +
                "seria desconcertante.");
            Assert.AreEqual(0, inv.SlotDe("necronomicon"));
        }

        [Test]
        public void Adquirir_Duplicado_NaoDuplica()
        {
            var inv = new InventarioDeArtefatos();
            inv.Adquirir("necronomicon");

            Assert.IsFalse(inv.Adquirir("necronomicon"), "Segunda aquisição não é nova.");
            Assert.AreEqual(1, inv.Possuidos.Count);
        }

        [Test]
        public void Adquirir_ComQuatroSlotsCheios_GuardaDormente()
        {
            var inv = new InventarioDeArtefatos();
            inv.Adquirir("a");
            inv.Adquirir("b");
            inv.Adquirir("c");
            inv.Adquirir("d");

            Assert.AreEqual(-1, inv.PrimeiroSlotLivre(), "Pré-condição: os quatro slots ocupados.");

            Assert.IsTrue(inv.Adquirir("quinto"),
                "A posse não tem teto: o quinto Artefato não pode ser recusado nem perdido.");

            Assert.IsTrue(inv.Possui("quinto"));
            Assert.IsFalse(inv.Contem("quinto"), "Sem slot livre, entra dormente.");
            Assert.Contains("quinto", inv.Dormentes());
        }

        // ── Porte ────────────────────────────────────────────────────────────

        [Test]
        public void Desequipar_MantemAPosse()
        {
            var inv = new InventarioDeArtefatos();
            inv.Adquirir("necronomicon");

            inv.Desequipar(0);

            Assert.IsFalse(inv.Contem("necronomicon"), "Saiu do slot.");
            Assert.IsTrue(inv.Possui("necronomicon"),
                "Desequipar nunca descarta — o Artefato adormece, não some.");
        }

        [Test]
        public void Equipar_RegistraPosseSeAindaNaoHavia()
        {
            var inv = new InventarioDeArtefatos();

            inv.Equipar("necronomicon", 2);

            Assert.IsTrue(inv.Possui("necronomicon"),
                "Portar implica possuir: senão o chamador teria uma ordem de chamada para decorar.");
        }

        [Test]
        public void ArtefatoDeslocadoPorTroca_ContinuaPossuido()
        {
            var inv = new InventarioDeArtefatos();
            inv.Adquirir("patua");

            string deslocado = inv.Equipar("coroa_de_ossos", inv.SlotDe("patua"));

            Assert.AreEqual("patua", deslocado);
            Assert.IsFalse(inv.Contem("patua"), "Saiu do slot.");
            Assert.IsTrue(inv.Possui("patua"), "Trocar de Artefato não pode custar o antigo.");
        }

        [Test]
        public void Dormentes_SoListaOQueNaoEstaPortado()
        {
            var inv = new InventarioDeArtefatos();
            inv.Adquirir("portado");
            inv.Adquirir("a");
            inv.Adquirir("b");
            inv.Adquirir("c");
            inv.Adquirir("dormente"); // quinto, sem slot

            var fora = inv.Dormentes();

            Assert.AreEqual(1, fora.Count);
            Assert.AreEqual("dormente", fora[0]);
        }

        // ── Contem vs Possui: a distinção que o combate depende ──────────────

        [Test]
        public void Contem_EhPorte_Possui_EhPosse()
        {
            var inv = new InventarioDeArtefatos();
            inv.Adquirir("necronomicon");
            inv.Desequipar(inv.SlotDe("necronomicon"));

            // O rito do Rei em Amarelo (PontoFocalDeReliquia) exige porte: a relíquia tem de
            // estar na mão para responder ao ponto focal.
            Assert.IsFalse(inv.Contem("necronomicon"));

            // A Porta de Aklo pergunta posse: carregar o tomo basta, o slot não importa.
            Assert.IsTrue(inv.Possui("necronomicon"));
        }

        // ── Restauração de save ──────────────────────────────────────────────

        [Test]
        public void Restaurar_PreservaAOrdemDosSlots()
        {
            var inv = new InventarioDeArtefatos();

            var possuidos = new List<string> { "necronomicon", "coroa_de_ossos", "patua" };
            var portados = new List<string> { "", "coroa_de_ossos", "", "necronomicon" };

            inv.Restaurar(possuidos, portados);

            // A posição é escolha do jogador (qual Artefato em qual tecla) — Adquirir porta no
            // primeiro slot livre e embaralharia isto.
            Assert.IsNull(inv.IdNoSlot(0));
            Assert.AreEqual("coroa_de_ossos", inv.IdNoSlot(1));
            Assert.IsNull(inv.IdNoSlot(2));
            Assert.AreEqual("necronomicon", inv.IdNoSlot(3));

            Assert.IsTrue(inv.Possui("patua"), "O dormente do save também volta.");
            Assert.IsFalse(inv.Contem("patua"));
        }

        [Test]
        public void Restaurar_DescartaPortadoQueNaoConstaComoPossuido()
        {
            var inv = new InventarioDeArtefatos();

            // Save inconsistente: não pode derrubar o load inteiro.
            inv.Restaurar(new List<string> { "necronomicon" },
                          new List<string> { "necronomicon", "fantasma", "", "" });

            Assert.AreEqual("necronomicon", inv.IdNoSlot(0));
            Assert.IsNull(inv.IdNoSlot(1), "Id portado sem posse é ignorado.");
            Assert.IsFalse(inv.Possui("fantasma"));
        }

        [Test]
        public void Restaurar_LimpaOEstadoAnterior()
        {
            var inv = new InventarioDeArtefatos();
            inv.Adquirir("velho");

            inv.Restaurar(new List<string> { "novo" }, new List<string> { "novo", "", "", "" });

            Assert.IsFalse(inv.Possui("velho"), "Carregar um save substitui o estado, não soma.");
            Assert.IsTrue(inv.Possui("novo"));
        }

        [Test]
        public void Restaurar_SaveAntigoSemDormentes_DeduzPosseDosPortados()
        {
            var inv = new InventarioDeArtefatos();

            // Formato anterior a 2026-08-12: só existia a lista de portados. Quem carrega o
            // save passa a mesma lista nas duas pontas.
            var portados = new List<string> { "necronomicon", "", "coroa_de_ossos", "" };
            var deduzidos = new List<string> { "necronomicon", "coroa_de_ossos" };

            inv.Restaurar(deduzidos, portados);

            Assert.IsTrue(inv.Possui("necronomicon"));
            Assert.IsTrue(inv.Contem("coroa_de_ossos"));
            Assert.AreEqual(2, inv.Possuidos.Count);
        }
    }
}
