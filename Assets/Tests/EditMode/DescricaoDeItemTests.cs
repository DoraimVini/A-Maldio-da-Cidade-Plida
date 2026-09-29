using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using FavelaAmarela.Core.Loot;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// O que a coluna de detalhe do inventário escreve sobre um item — e a convenção percentual
    /// dos quatro atributos de combate, que o painel expôs.
    ///
    /// <para><b>O achado de 2026-09-28:</b> Chance Crítica, Dano Crítico, Precisão e Dano Físico
    /// são lidos em <b>percentual</b> (5 = 5%) desde 28/08, e os afixos que os rolam foram
    /// autorados em <b>fração</b> em 01/09 — valiam 100 vezes menos. A ficha antiga mostrava
    /// "+0,03 Chance Crítica" e ninguém estranhou.</para>
    /// </summary>
    public sealed class DescricaoDeItemTests
    {
        private GameObject _objetoDoBanco;
        private ItemDatabase _banco;
        private readonly List<Object> _criados = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            _objetoDoBanco = new GameObject("BancoDeTeste");
            _banco = _objetoDoBanco.AddComponent<ItemDatabase>();
            _banco.InitializeForTesting();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _criados) if (o != null) Object.DestroyImmediate(o);
            _criados.Clear();
            if (_objetoDoBanco != null) Object.DestroyImmediate(_objetoDoBanco);
        }

        private ItemDef Def(string id, string nome, ItemType tipo, EquipmentSlot slot,
                            params ModificadorFixo[] mods)
        {
            var d = ScriptableObject.CreateInstance<ItemDef>();
            d.Id = id;
            d.Nome = nome;
            d.Tipo = tipo;
            d.SlotEquipamento = slot;
            d.Modificadores = mods.ToList();
            _banco.Registrar(d);
            _criados.Add(d);
            return d;
        }

        private ItemDef Arma(string id, string nome, float min, float max, Empunhadura maos)
        {
            var baseDeArma = ScriptableObject.CreateInstance<BaseDeArma>();
            baseDeArma.DanoMinBase = min;
            baseDeArma.DanoMaxBase = max;
            baseDeArma.ChanceCriticaBase = 0.05f;
            baseDeArma.MultiplicadorCritico = 1.5f;
            baseDeArma.PrecisaoBase = 0.9f;
            _criados.Add(baseDeArma);

            var d = Def(id, nome, ItemType.Arma, EquipmentSlot.Arma);
            d.Base = baseDeArma;
            d.Empunhadura = maos;
            return d;
        }

        // ── A convenção percentual ───────────────────────────────────────────

        [Test]
        public void Linha_PercentualLevaPorcento_EOResto_NaoLeva()
        {
            Assert.AreEqual("+3% Chance Crítica", NomesDeAtributo.Linha(StatType.ChanceCritica, 3f));
            Assert.AreEqual("+13 Vitalidade", NomesDeAtributo.Linha(StatType.VitMaxima, 13f));
            Assert.AreEqual("−2 Defesa Física", NomesDeAtributo.Linha(StatType.DefesaFisica, -2f));
            Assert.AreEqual("+1,5 Recuperação de Vigor", NomesDeAtributo.Linha(StatType.RegeneracaoVigor, 1.5f));
        }

        [Test]
        public void SaveAntigo_ComPercentualEmFracao_VoltaEmPercentual()
        {
            var gravado = new ItemSlotData
            {
                itemDefId = "capuz",
                quantity = 1,
                afixos = new List<AfixoRolado>
                {
                    new AfixoRolado("afixo_afiado", StatType.ChanceCritica, 0.03f),
                    new AfixoRolado("afixo_da_furia", StatType.AumentoDeDanoFisico, 12f),
                    new AfixoRolado("afixo_encorpado", StatType.VitMaxima, 0.5f),
                },
            };

            var item = gravado.ParaInstancia();

            Assert.AreEqual(3f, item.Afixos[0].Valor, 1e-4f, "0,03 de Chance Crítica gravado em fração tem de virar 3%.");
            Assert.AreEqual(12f, item.Afixos[1].Valor, 1e-4f, "Percentual já certo não pode ser multiplicado de novo.");
            Assert.AreEqual(0.5f, item.Afixos[2].Valor, 1e-4f, "Atributo que não é percentual não se mexe.");
        }

        [Test]
        public void SaveAntigo_ConsumivelComAfixo_VoltaComum()
        {
            Def("erva", "Erva de Ancoragem", ItemType.Consumivel, EquipmentSlot.Nenhum);
            var gravado = new ItemSlotData
            {
                itemDefId = "erva",
                quantity = 2,
                grau = GrauDeImpregnacao.Marcado,
                afixos = new List<AfixoRolado> { new AfixoRolado("afixo_afiado", StatType.ChanceCritica, 3f) },
            };

            var item = gravado.ParaInstancia();

            Assert.IsEmpty(item.Afixos, "A 'Afiado Erva de Ancoragem' do save tem de voltar a ser erva.");
            Assert.AreEqual(GrauDeImpregnacao.Inerte, item.Grau);
            Assert.AreEqual("Erva de Ancoragem", item.NomeExibido());
        }

        /// <summary>
        /// Rótulo que começa por "de", "do", "da"... é <b>sufixo</b> em português: como prefixo,
        /// o nome sai "do Peregrino Firme Alfanje das Ruínas Pálidas" (visto na tela nova do
        /// inventário, 2026-09-28).
        /// </summary>
        [Test]
        public void RotuloComPreposicao_ESufixo()
        {
            var errados = UnityEditor.AssetDatabase.FindAssets("t:AfixoDef")
                .Select(UnityEditor.AssetDatabase.GUIDToAssetPath)
                .Select(UnityEditor.AssetDatabase.LoadAssetAtPath<AfixoDef>)
                .Where(a => a != null && a.Tipo == TipoDeAfixo.Prefixo && !string.IsNullOrEmpty(a.Rotulo))
                .Where(a => System.Text.RegularExpressions.Regex.IsMatch(a.Rotulo, @"^(de|do|da|dos|das)\s"))
                .Select(a => $"{a.name}: \"{a.Rotulo}\"")
                .ToList();

            Assert.IsEmpty(errados, "Afixo com rótulo de sufixo cadastrado como prefixo:\n  " + string.Join("\n  ", errados));
        }

        [Test]
        public void TodoAfixoPercentual_RolaEmPercentual_NaoEmFracao()
        {
            var afixos = UnityEditor.AssetDatabase.FindAssets("t:AfixoDef")
                .Select(UnityEditor.AssetDatabase.GUIDToAssetPath)
                .Select(UnityEditor.AssetDatabase.LoadAssetAtPath<AfixoDef>)
                .Where(a => a != null && NomesDeAtributo.EhPercentual(a.Stat))
                .ToList();

            Assert.IsNotEmpty(afixos, "Nenhum afixo percentual achado — o guarda parou de medir.");

            var errados = afixos.Where(a => a.ValorMin < 1f)
                                .Select(a => $"{a.name}: {a.ValorMin}–{a.ValorMax} em {a.Stat}")
                                .ToList();

            Assert.IsEmpty(errados,
                "Afixo percentual autorado em fração — o golpe divide por 100 e ele vale quase nada:\n  " +
                string.Join("\n  ", errados));
        }

        // ── O que o painel escreve ───────────────────────────────────────────

        [Test]
        public void Subtitulo_DizOQueOItemE()
        {
            Arma("espadao", "Espadão", 10f, 20f, Empunhadura.DuasMaos);
            var item = new ItemInstance("espadao") { NivelDoItem = 3, Grau = GrauDeImpregnacao.Marcado };

            Assert.AreEqual("Arma · Duas Mãos · Nível 3 · Marcado", DescricaoDeItem.Subtitulo(item));

            Def("erva", "Erva", ItemType.Consumivel, EquipmentSlot.Nenhum);
            Assert.AreEqual("Consumível · ×2", DescricaoDeItem.Subtitulo(new ItemInstance("erva", 2)));
        }

        [Test]
        public void Atributos_DaArma_TrazemDanoCriticoEPrecisao_EOsAfixos()
        {
            Arma("alfanje", "Alfanje", 8f, 14f, Empunhadura.UmaMao);
            var item = new ItemInstance("alfanje");
            item.Afixos.Add(new AfixoRolado("afixo_afiado", StatType.ChanceCritica, 4f));

            var linhas = DescricaoDeItem.Atributos(item);

            Assert.AreEqual("Dano 8–14", linhas[0]);
            StringAssert.StartsWith("Crítico 5%", linhas[1]);
            Assert.AreEqual("Precisão 90%", linhas[2]);
            CollectionAssert.Contains(linhas, "+4% Chance Crítica");
        }

        [Test]
        public void Atributos_DoConsumivel_FalamDeCura_EAtributoSemEfeitoVemAdormecido()
        {
            Def("agua", "Água", ItemType.Consumivel, EquipmentSlot.Nenhum,
                new ModificadorFixo(StatType.VitMaxima, 20f), new ModificadorFixo(StatType.RMMaxima, 10f));
            CollectionAssert.AreEqual(
                new[] { "Restaura 20 de Vitalidade Corpórea", "Ancora 10 de Resiliência Mental" },
                DescricaoDeItem.Atributos(new ItemInstance("agua")));

            Def("coroa", "Coroa", ItemType.Armadura, EquipmentSlot.Elmo,
                new ModificadorFixo(StatType.RMMaxima, 5f));
            CollectionAssert.Contains(DescricaoDeItem.Atributos(new ItemInstance("coroa")),
                "+5 Resiliência Mental (adormecido)",
                "Um bônus que nenhum sistema lê não pode aparecer como se valesse.");
        }

        [Test]
        public void Comparar_SemNadaVestido_ETudoGanho()
        {
            Def("capuz", "Capuz", ItemType.Armadura, EquipmentSlot.Elmo, new ModificadorFixo(StatType.DefesaFisica, 2f));
            var linhas = DescricaoDeItem.Comparar(new ItemInstance("capuz"), null);

            Assert.AreEqual(1, linhas.Count);
            Assert.AreEqual(1, linhas[0].Sinal);
            StringAssert.Contains("Elmo", linhas[0].Texto);
        }

        [Test]
        public void Comparar_MostraADiferenca_ComSinalCerto_ECustoInvertido()
        {
            Def("elmo_a", "Elmo A", ItemType.Armadura, EquipmentSlot.Elmo,
                new ModificadorFixo(StatType.DefesaFisica, 5f), new ModificadorFixo(StatType.CustoEsquivaVigor, 2f));
            Def("elmo_b", "Elmo B", ItemType.Armadura, EquipmentSlot.Elmo,
                new ModificadorFixo(StatType.DefesaFisica, 2f), new ModificadorFixo(StatType.VitMaxima, 10f));

            var linhas = DescricaoDeItem.Comparar(new ItemInstance("elmo_a"), new ItemInstance("elmo_b"));
            var porTexto = linhas.ToDictionary(l => l.Texto, l => l.Sinal);

            Assert.AreEqual(1, porTexto["+3 Defesa Física"]);
            Assert.AreEqual(-1, porTexto["−10 Vitalidade"]);
            Assert.AreEqual(-1, porTexto["+2 Vigor por Esquiva"], "Custo maior é pior: o sinal inverte.");
        }

        [Test]
        public void Comparar_Armas_MostraODanoMedio()
        {
            Arma("fraca", "Fraca", 4f, 6f, Empunhadura.UmaMao);
            Arma("forte", "Forte", 8f, 12f, Empunhadura.UmaMao);

            var linhas = DescricaoDeItem.Comparar(new ItemInstance("forte"), new ItemInstance("fraca"));

            Assert.AreEqual("+5 de dano médio", linhas[0].Texto);
            Assert.AreEqual(1, linhas[0].Sinal);
        }

        [Test]
        public void Recusa_DizOPorque_NoVocabularioDoJogo()
        {
            Def("erva", "Erva", ItemType.Consumivel, EquipmentSlot.Nenhum);
            Arma("espadao", "Espadão", 10f, 20f, Empunhadura.DuasMaos);
            Def("broquel", "Broquel", ItemType.Armadura, EquipmentSlot.MaoSecundaria);

            StringAssert.Contains("teclas 1 a 8",
                DescricaoDeItem.TextoDaRecusa(RecusaAoEquipar.NaoSeVeste, new ItemInstance("erva"), null, null));
            Assert.AreEqual("Espadão ocupa as duas mãos de Damião. Não há onde segurar Broquel.",
                DescricaoDeItem.TextoDaRecusa(RecusaAoEquipar.MaosTomadasPorArmaDeDuasMaos,
                    new ItemInstance("broquel"), new ItemInstance("espadao"), null));
            Assert.AreEqual("A mochila está cheia. Broquel continua com Damião.",
                DescricaoDeItem.TextoDaRecusa(RecusaAoDesequipar.MochilaCheia, new ItemInstance("broquel")));
        }
    }
}
