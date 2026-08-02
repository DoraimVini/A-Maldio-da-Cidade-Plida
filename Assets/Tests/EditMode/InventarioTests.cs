using NUnit.Framework;
using FavelaAmarela.Core.Abilities;
using FavelaAmarela.Core.Itens;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Testes do <see cref="Inventario"/> e da <see cref="DefinicaoDeItem"/>. Cobrem
    /// empilhamento, transbordo e as duas regras que evitam bug silencioso de progresso:
    /// completar pilhas antes de ocupar posição nova, e nunca remover parcialmente.
    /// </summary>
    public sealed class InventarioTests
    {
        private static DefinicaoDeItem Cinza(int pilha = 5) => new DefinicaoDeItem(
            "cinza_de_ancora", "Cinza de Âncora", "Ancora a mente ao que ainda é real.",
            pilhaMaxima: pilha, efeito: TipoDeEfeito.Ancorar, potencia: 25f);

        private static DefinicaoDeItem Emplastro() => new DefinicaoDeItem(
            "emplastro", "Emplastro de Sal", "Fecha a carne aberta.",
            pilhaMaxima: 3, efeito: TipoDeEfeito.Estabilizar, potencia: 30f);

        private static DefinicaoDeItem Relíquia() => new DefinicaoDeItem(
            "anel_do_sinal", "Anel do Sinal Amarelo", "Frio, mesmo ao sol.");

        // ── Definição ────────────────────────────────────────────────────────

        [Test]
        public void Item_SemId_Recusa()
        {
            Assert.Throws<System.ArgumentException>(() => new DefinicaoDeItem(null));
            Assert.Throws<System.ArgumentException>(() => new DefinicaoDeItem("   "));
        }

        [Test]
        public void Item_SemNome_UsaOId()
        {
            Assert.AreEqual("cinza", new DefinicaoDeItem("cinza").Nome);
        }

        [Test]
        public void Item_PilhaMaximaInvalida_ViraUm()
        {
            Assert.AreEqual(1, new DefinicaoDeItem("x", pilhaMaxima: 0).PilhaMaxima);
            Assert.AreEqual(1, new DefinicaoDeItem("x", pilhaMaxima: -7).PilhaMaxima);
        }

        [Test]
        public void Item_SemEfeito_NaoEhConsumido()
        {
            Assert.IsFalse(Relíquia().ConsomeAoUsar);
            Assert.IsTrue(Cinza().ConsomeAoUsar);
        }

        // ── Guardar ──────────────────────────────────────────────────────────

        [Test]
        public void InventarioNovo_ComecaVazio()
        {
            var inv = new Inventario(4);
            Assert.AreEqual(4, inv.Posicoes);
            for (int i = 0; i < inv.Posicoes; i++) Assert.IsTrue(inv.Ver(i).Vazia);
        }

        [Test]
        public void Adicionar_GuardaEConta()
        {
            var inv = new Inventario(4);
            Assert.AreEqual(0, inv.Adicionar(Cinza(), 3));
            Assert.AreEqual(3, inv.Contar("cinza_de_ancora"));
        }

        [Test]
        public void Adicionar_CompletaPilhaAntesDeOcuparPosicaoNova()
        {
            var inv = new Inventario(4);
            inv.Adicionar(Cinza(), 3);   // pilha de 5, tem 3
            inv.Adicionar(Cinza(), 2);   // completa a mesma pilha

            Assert.AreEqual(5, inv.Ver(0).Quantidade);
            Assert.IsTrue(inv.Ver(1).Vazia, "não podia ter aberto posição nova com espaço sobrando");
        }

        [Test]
        public void Adicionar_TransbordaParaProximaPosicao()
        {
            var inv = new Inventario(4);
            inv.Adicionar(Cinza(), 7);   // pilha máx 5

            Assert.AreEqual(5, inv.Ver(0).Quantidade);
            Assert.AreEqual(2, inv.Ver(1).Quantidade);
            Assert.AreEqual(7, inv.Contar("cinza_de_ancora"));
        }

        [Test]
        public void Adicionar_AlemDaCapacidade_DevolveOQueNaoCoube()
        {
            var inv = new Inventario(2);      // 2 posições × pilha 5 = 10
            int sobrou = inv.Adicionar(Cinza(), 13);

            Assert.AreEqual(3, sobrou);
            Assert.AreEqual(10, inv.Contar("cinza_de_ancora"));
        }

        [Test]
        public void Adicionar_ItemNuloOuQuantidadeInvalida_NaoQuebra()
        {
            var inv = new Inventario(2);
            Assert.DoesNotThrow(() => inv.Adicionar(null, 3));
            Assert.AreEqual(0, inv.Adicionar(Cinza(), 0));
            Assert.AreEqual(0, inv.Adicionar(Cinza(), -2));
        }

        [Test]
        public void TemEspacoPara_NaoAlteraOInventario()
        {
            var inv = new Inventario(1);
            inv.Adicionar(Cinza(), 5);   // lotado

            Assert.IsFalse(inv.TemEspacoPara(Emplastro()));
            Assert.AreEqual(5, inv.Contar("cinza_de_ancora"), "simular não podia mexer no conteúdo");
            Assert.AreEqual(0, inv.Contar("emplastro"));
        }

        // ── Retirar ──────────────────────────────────────────────────────────

        [Test]
        public void Remover_TiraAQuantidadePedida()
        {
            var inv = new Inventario(4);
            inv.Adicionar(Cinza(), 4);

            Assert.IsTrue(inv.Remover("cinza_de_ancora", 3));
            Assert.AreEqual(1, inv.Contar("cinza_de_ancora"));
        }

        [Test]
        public void Remover_SemOSuficiente_NaoTiraNada()
        {
            var inv = new Inventario(4);
            inv.Adicionar(Cinza(), 2);

            Assert.IsFalse(inv.Remover("cinza_de_ancora", 5));
            Assert.AreEqual(2, inv.Contar("cinza_de_ancora"), "remoção parcial some com item sem entregar nada");
        }

        [Test]
        public void Remover_VarreVariasPilhas()
        {
            var inv = new Inventario(4);
            inv.Adicionar(Cinza(), 7);   // 5 + 2

            Assert.IsTrue(inv.Remover("cinza_de_ancora", 6));
            Assert.AreEqual(1, inv.Contar("cinza_de_ancora"));
        }

        // ── Usar ─────────────────────────────────────────────────────────────

        [Test]
        public void Consumir_DevolveOEfeitoEGastaUm()
        {
            var inv = new Inventario(4);
            inv.Adicionar(Cinza(), 2);

            var efeito = inv.Consumir(0);

            Assert.IsTrue(efeito.Houve);
            Assert.AreEqual(TipoDeEfeito.Ancorar, efeito.Tipo);
            Assert.AreEqual(25f, efeito.Potencia);
            Assert.AreEqual(1, inv.Contar("cinza_de_ancora"));
        }

        [Test]
        public void Consumir_UltimoExemplar_EsvaziaAPosicao()
        {
            var inv = new Inventario(4);
            inv.Adicionar(Cinza(), 1);

            inv.Consumir(0);
            Assert.IsTrue(inv.Ver(0).Vazia);
        }

        [Test]
        public void Consumir_PosicaoVazia_NaoFazNada()
        {
            var inv = new Inventario(4);
            Assert.IsFalse(inv.Consumir(0).Houve);
        }

        [Test]
        public void Consumir_ItemSemEfeito_NaoGastaOItem()
        {
            var inv = new Inventario(4);
            inv.Adicionar(Relíquia(), 1);

            Assert.IsFalse(inv.Consumir(0).Houve);
            Assert.AreEqual(1, inv.Contar("anel_do_sinal"), "relíquia não pode sumir ao ser 'usada'");
        }

        [Test]
        public void Consumir_IndiceForaDaFaixa_NaoQuebra()
        {
            var inv = new Inventario(2);
            Assert.DoesNotThrow(() => inv.Consumir(99));
            Assert.DoesNotThrow(() => inv.Consumir(-1));
            Assert.IsTrue(inv.Ver(99).Vazia);
        }

        // ── Evento ───────────────────────────────────────────────────────────

        [Test]
        public void OnMudou_DisparaAoGuardarUsarERetirar()
        {
            var inv = new Inventario(4);
            int mudancas = 0;
            inv.OnMudou += () => mudancas++;

            inv.Adicionar(Cinza(), 2);
            inv.Consumir(0);
            inv.Remover("cinza_de_ancora", 1);

            Assert.AreEqual(3, mudancas);
        }

        // ── Armas como itens (decisão 2026-08-01) ────────────────────────────

        private static DefinicaoDeItem Estilete() => new DefinicaoDeItem(
            "arma_EstileteDeIrem", "Estilete de Irem",
            armaEquipavel: ArmaDaTumba.EstileteDeIrem);

        [Test]
        public void Arma_EhEquipavel()
        {
            Assert.IsTrue(Estilete().EhEquipavel);
            Assert.AreEqual(ArmaDaTumba.EstileteDeIrem, Estilete().ArmaEquipavel);
            Assert.IsFalse(Cinza().EhEquipavel);
        }

        [Test]
        public void Arma_NuncaEmpilha()
        {
            // Duas do mesmo tipo numa pilha seriam indistinguíveis, e o slot da Mão Física
            // é um só — empilhar não teria significado.
            var arma = new DefinicaoDeItem("arma_x", pilhaMaxima: 99,
                armaEquipavel: ArmaDaTumba.AlfanjeDeAlhazred);

            Assert.AreEqual(1, arma.PilhaMaxima);
            Assert.IsFalse(arma.Empilhavel);
        }

        [Test]
        public void Arma_NaoEhConsumidaAoUsar()
        {
            // Empunhar não gasta a arma: é o que permite voltar para ela depois.
            Assert.IsFalse(Estilete().ConsomeAoUsar);
        }

        [Test]
        public void Arma_OcupaUmaPosicaoPorExemplar()
        {
            var inv = new Inventario(4);
            inv.Adicionar(Estilete(), 3);

            Assert.AreEqual(3, inv.Contar("arma_EstileteDeIrem"));
            Assert.AreEqual(1, inv.Ver(0).Quantidade);
            Assert.AreEqual(1, inv.Ver(1).Quantidade);
            Assert.AreEqual(1, inv.Ver(2).Quantidade);
        }

        [Test]
        public void Arma_ConsumirNaoARemove()
        {
            // O InventarioBridge intercepta armas antes de chamar Consumir, mas se algum
            // caminho futuro chamar direto, a arma não pode sumir.
            var inv = new Inventario(4);
            inv.Adicionar(Estilete(), 1);

            inv.Consumir(0);
            Assert.AreEqual(1, inv.Contar("arma_EstileteDeIrem"));
        }

        [Test]
        public void ArmasDiferentes_ConvivemNoInventario()
        {
            var inv = new Inventario(4);
            inv.Adicionar(Estilete(), 1);
            inv.Adicionar(new DefinicaoDeItem("arma_CravoDeAklo", "Cravo de Aklo",
                armaEquipavel: ArmaDaTumba.CravoDeAklo), 1);

            Assert.AreEqual(1, inv.Contar("arma_EstileteDeIrem"));
            Assert.AreEqual(1, inv.Contar("arma_CravoDeAklo"));
        }

        [Test]
        public void OnMudou_NaoDisparaQuandoNadaMuda()
        {
            var inv = new Inventario(1);
            inv.Adicionar(Cinza(), 5);   // lota

            int mudancas = 0;
            inv.OnMudou += () => mudancas++;

            inv.Adicionar(Cinza(), 3);          // não cabe nada
            inv.Remover("emplastro", 1);        // não tem
            inv.Consumir(3);                    // fora da faixa

            Assert.AreEqual(0, mudancas);
        }
    }
}
