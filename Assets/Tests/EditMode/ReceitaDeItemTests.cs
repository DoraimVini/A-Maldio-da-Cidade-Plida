using System.Linq;
using NUnit.Framework;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a validação da <b>Forja de Itens</b> do Carcosa Debugger.
    ///
    /// <para><b>Por que a validação é POCO e testada aqui.</b> O asmdef
    /// <c>FavelaAmarela.Tests.EditMode</c> referencia só <c>Core</c> e <c>Runtime</c>, com
    /// <c>overrideReferences: true</c> — nenhum teste consegue invocar o assembly do Editor. Se
    /// a validação vivesse dentro da janela, seria testável só à mão, que é o mesmo que não ser.</para>
    ///
    /// <para><b>Os três defeitos que a forja produziria sozinha</b>, e que nenhum deles aparece
    /// na hora: item sem ícone (derruba a suíte inteira), <c>Id</c> duplicado (o item novo
    /// simplesmente não existe em jogo) e modificador em atributo decorativo (o jogador lê o
    /// número e não recebe nada).</para>
    /// </summary>
    public sealed class ReceitaDeItemTests
    {
        private static ReceitaDeItem Valida() => new ReceitaDeItem
        {
            Nome = "Elmo de Farrapos",
            Id = "elmo_farrapos",
            Tipo = ItemType.Armadura,
            Slot = EquipmentSlot.Elmo,
            EmpilhamentoMaximo = 1,
            TemIcone = true,
            Modificadores = { new ModificadorFixo(StatType.DefesaFisica, 2f) },
        };

        [Test]
        public void UmaReceitaCompleta_NaoTemProblema()
        {
            Assert.IsEmpty(Valida().Problemas(new string[0]));
        }

        /// <summary>
        /// <c>IconesDosItensTests</c> varre <b>Assets por completo</b> procurando itens sem
        /// ícone. Um item forjado sem ícone não quebra só a si — derruba a suíte inteira, e o
        /// próximo commit começa vermelho sem relação aparente com o que se estava fazendo.
        /// </summary>
        [Test]
        public void SemIcone_ERecusado()
        {
            var r = Valida();
            r.TemIcone = false;

            Assert.IsTrue(r.Problemas(new string[0]).Any(p => p.Contains("ícone")),
                "Item sem ícone tem de ser recusado na forja, não descoberto pela suíte depois.");
        }

        /// <summary>
        /// <c>ItemDatabase.ConstruirCache</c> loga erro e <b>mantém o primeiro</b> — o item novo
        /// existe no disco e não existe em jogo. É o pior tipo de falha: o asset está lá, o
        /// designer vê o arquivo, e o item nunca cai.
        /// </summary>
        [Test]
        public void IdDuplicado_ERecusado()
        {
            var problemas = Valida().Problemas(new[] { "outra_coisa", "elmo_farrapos" });

            Assert.IsTrue(problemas.Any(p => p.Contains("já existe")),
                "Id duplicado tem de ser recusado: o ItemDatabase mantém o primeiro e o novo " +
                "não existiria em jogo.");
        }

        [Test]
        public void IdComEspaco_ERecusado()
        {
            var r = Valida();
            r.Id = "elmo de farrapos";

            Assert.IsTrue(r.Problemas(new string[0]).Any(p => p.Contains("espaço")));
        }

        [Test]
        public void SemNome_ERecusado()
        {
            var r = Valida();
            r.Nome = "   ";

            Assert.IsTrue(r.Problemas(new string[0]).Any(p => p.Contains("nome")));
        }

        /// <summary>
        /// Quatro <c>StatType</c> não têm consumidor nenhum. <c>DefesaAnomalia</c> é o pior
        /// caso: a ficha <b>exibe</b> a linha e o combate não aplica.
        /// </summary>
        [Test]
        public void ModificadorSemEfeito_ERecusado()
        {
            foreach (var stat in ReceitaDeItem.AtributosSemEfeito)
            {
                var r = Valida();
                r.Modificadores.Clear();
                r.Modificadores.Add(new ModificadorFixo(stat, 5f));

                Assert.IsNotEmpty(r.Problemas(new string[0]),
                    $"'{stat}' não tem consumidor no jogo e passou na validação — a forja " +
                    "produziria um item que mente para o jogador.");
            }
        }

        [Test]
        public void ArmaduraSemSlot_ERecusada()
        {
            var r = Valida();
            r.Slot = EquipmentSlot.Nenhum;

            Assert.IsTrue(r.Problemas(new string[0]).Any(p => p.Contains("sem slot")),
                "O jogador pegaria o item e não conseguiria equipar.");
        }

        [Test]
        public void ConsumivelComSlotDeEquipamento_ERecusado()
        {
            var r = Valida();
            r.Tipo = ItemType.Consumivel;
            r.Slot = EquipmentSlot.Elmo;
            r.EmpilhamentoMaximo = 5;

            Assert.IsNotEmpty(r.Problemas(new string[0]),
                "Um consumível vestível não tem significado definido no inventário.");
        }

        [Test]
        public void ArmaEmpilhavel_ERecusada()
        {
            var r = Valida();
            r.Tipo = ItemType.Arma;
            r.Slot = EquipmentSlot.Arma;
            r.EmpilhamentoMaximo = 3;

            Assert.IsTrue(r.Problemas(new string[0]).Any(p => p.Contains("empilh")),
                "Equipar uma pilha de armas não tem significado definido.");
        }

        [Test]
        public void EmpilhamentoZero_ERecusado()
        {
            var r = Valida();
            r.EmpilhamentoMaximo = 0;

            Assert.IsNotEmpty(r.Problemas(new string[0]),
                "Abaixo de 1 o item não cabe em slot nenhum.");
        }

        /// <summary>
        /// Arma sem família fica equipável e <b>inerte</b> — o jogador vê a arma na mão e não
        /// causa dano. É aviso, não erro: a família é ligada num segundo passo, e travar a
        /// criação por isso tornaria impossível autorar arma pela forja.
        /// </summary>
        [Test]
        public void Arma_AvisaQuePrecisaDeFamilia()
        {
            var r = Valida();
            r.Tipo = ItemType.Arma;
            r.Slot = EquipmentSlot.Arma;

            Assert.IsEmpty(r.Problemas(new string[0]), "Não pode BLOQUEAR a criação.");
            Assert.IsTrue(r.Avisos().Any(a => a.Contains("BaseDeArma")),
                "Mas tem de avisar: sem família a arma é equipável e inerte.");
        }

        // ── Sugestão de Id ────────────────────────────────────────────────────

        [Test]
        public void OIdSugerido_SegueAConvencaoDoCatalogo()
        {
            Assert.AreEqual("elmo_de_farrapos", ReceitaDeItem.SugerirId("Elmo de Farrapos"));
            Assert.AreEqual("patua_das_luas_gemeas",
                            ReceitaDeItem.SugerirId("Patuá das Luas Gêmeas"));
            Assert.AreEqual("cravo_de_aklo", ReceitaDeItem.SugerirId("  Cravo de Aklo  "));
        }

        [Test]
        public void OIdSugerido_NaoDeixaAcentoNemPontuacao()
        {
            string id = ReceitaDeItem.SugerirId("Canção de Cassilda!");

            Assert.AreEqual("cancao_de_cassilda", id,
                "Acento e pontuação num Id atrapalham a leitura do JSON de save e a busca no " +
                "projeto.");
        }

        [Test]
        public void NomeVazio_NaoSugereIdQuebrado()
        {
            Assert.AreEqual("", ReceitaDeItem.SugerirId(""));
            Assert.AreEqual("", ReceitaDeItem.SugerirId("   "));
        }
    }
}
