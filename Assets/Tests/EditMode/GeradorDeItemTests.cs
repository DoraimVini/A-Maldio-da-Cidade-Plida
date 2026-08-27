using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using FavelaAmarela.Core.Loot;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda o <b>gerador de itens</b> — a peça que faz uma arma de nível alto valer mais que
    /// uma de nível 1.
    ///
    /// <para><b>Por que ele existe.</b> Até 2026-08-27 valia a invariante <i>"o sorteio nunca
    /// gera atributos"</i>. O Vini apontou o furo: sem geração, duas cópias do mesmo item são
    /// idênticas e não há curva de poder nenhuma — a arma de nível 12 entrega os mesmos status
    /// da de nível 1, e a segunda cópia de um drop nunca interessa, que é o loop de loot mais
    /// fraco que um ARPG pode ter.</para>
    ///
    /// <para><b>A invariante que ficou no lugar:</b> o gerador nunca <i>inventa</i> um afixo —
    /// escolhe de um pool autorado e rola dentro de uma faixa autorada.</para>
    ///
    /// <para>Os testes abaixo cobrem as armadilhas clássicas do gênero, que aparecem em
    /// qualquer sistema de afixos caseiro: afixo duplicado, peso não renormalizado depois do
    /// filtro de nível, e ausência de teto por grau.</para>
    /// </summary>
    public sealed class GeradorDeItemTests
    {
        /// <summary>Fonte determinística: devolve os valores na ordem dada, e repete o último.</summary>
        private sealed class FonteFake : IFonteDeAleatoriedade
        {
            private readonly float[] _valores;
            private int _i;

            public FonteFake(params float[] valores) => _valores = valores;

            public float ProximoValor()
            {
                if (_valores.Length == 0) return 0f;
                float v = _valores[Mathf.Min(_i, _valores.Length - 1)];
                _i++;
                return v;
            }

            public int ProximoInteiro(int minInclusivo, int maxExclusivo) => minInclusivo;
        }

        private static readonly List<Object> _lixo = new List<Object>();

        [TearDown]
        public void Limpar()
        {
            foreach (var o in _lixo) if (o != null) Object.DestroyImmediate(o);
            _lixo.Clear();
        }

        private static ItemDef Base(EquipmentSlot slot = EquipmentSlot.Arma)
        {
            var def = ScriptableObject.CreateInstance<ItemDef>();
            def.Id = "base_de_teste";
            def.Nome = "Base de Teste";
            def.Tipo = ItemType.Arma;
            def.SlotEquipamento = slot;
            _lixo.Add(def);
            return def;
        }

        private static AfixoDef Afixo(string id, TipoDeAfixo tipo, StatType stat,
                                      float min, float max, int nivelMin = 1,
                                      float peso = 1f, string grupo = null)
        {
            var a = ScriptableObject.CreateInstance<AfixoDef>();
            a.Id = id;
            a.Tipo = tipo;
            a.Stat = stat;
            a.ValorMin = min;
            a.ValorMax = max;
            a.NivelMinimoDoItem = nivelMin;
            a.Peso = peso;
            a.GrupoDeExclusao = grupo;
            _lixo.Add(a);
            return a;
        }

        // ── Grau decide quantos afixos ────────────────────────────────────────

        [Test]
        public void Inerte_NaoRecebeAfixo()
        {
            var item = new GeradorDeItem().Gerar(
                Base(), GrauDeImpregnacao.Inerte, 1,
                new[] { Afixo("a", TipoDeAfixo.Prefixo, StatType.VitMaxima, 5f, 5f) },
                new FonteFake(0.5f));

            Assert.IsEmpty(item.Afixos,
                "Inerte é matéria comum, que Carcosa ainda não tocou — não carrega afixo.");
        }

        [Test]
        public void Marcado_RecebeUmPrefixo()
        {
            var item = new GeradorDeItem().Gerar(
                Base(), GrauDeImpregnacao.Marcado, 1,
                new[]
                {
                    Afixo("p", TipoDeAfixo.Prefixo, StatType.VitMaxima, 5f, 5f),
                    Afixo("s", TipoDeAfixo.Sufixo, StatType.DefesaFisica, 2f, 2f),
                },
                new FonteFake(0.5f));

            Assert.AreEqual(1, item.Afixos.Count, "Marcado carrega o Sinal em algum canto: 1 afixo.");
            Assert.AreEqual(StatType.VitMaxima, item.Afixos[0].Stat, "e é um PREFIXO");
        }

        [Test]
        public void Impregnado_RecebePrefixoESufixo()
        {
            var item = new GeradorDeItem().Gerar(
                Base(), GrauDeImpregnacao.Impregnado, 1,
                new[]
                {
                    Afixo("p", TipoDeAfixo.Prefixo, StatType.VitMaxima, 5f, 5f),
                    Afixo("s", TipoDeAfixo.Sufixo, StatType.DefesaFisica, 2f, 2f),
                },
                new FonteFake(0.5f));

            Assert.AreEqual(2, item.Afixos.Count, "Impregnado está saturado: prefixo + sufixo.");
        }

        /// <summary>
        /// Relíquia é "peça única e nomeada, com história própria", autorada individualmente.
        /// Gerar uma aleatoriamente quebraria essa promessa.
        /// </summary>
        [Test]
        public void Reliquia_NuncaEGerada()
        {
            var item = new GeradorDeItem().Gerar(
                Base(), GrauDeImpregnacao.Reliquia, 99,
                new[] { Afixo("p", TipoDeAfixo.Prefixo, StatType.VitMaxima, 5f, 5f) },
                new FonteFake(0.5f));

            Assert.IsEmpty(item.Afixos,
                "Relíquia é autorada à mão e nunca sorteada em tabela genérica.");
        }

        // ── As armadilhas clássicas do gênero ─────────────────────────────────

        /// <summary>
        /// "+5 Vitalidade" e "+8 Vitalidade" na mesma peça é o defeito nº 1 de um sistema de
        /// afixos caseiro. O grupo de exclusão existe para isso.
        /// </summary>
        [Test]
        public void OMesmoGrupo_NaoCaiDuasVezes()
        {
            // Dois prefixos e dois sufixos, todos do mesmo grupo "vida".
            var pool = new[]
            {
                Afixo("v1", TipoDeAfixo.Prefixo, StatType.VitMaxima, 5f, 5f, grupo: "vida"),
                Afixo("v2", TipoDeAfixo.Prefixo, StatType.VitMaxima, 8f, 8f, grupo: "vida"),
                Afixo("v3", TipoDeAfixo.Sufixo, StatType.VitMaxima, 3f, 3f, grupo: "vida"),
            };

            var item = new GeradorDeItem().Gerar(
                Base(), GrauDeImpregnacao.Impregnado, 1, pool, new FonteFake(0.1f, 0.9f, 0.5f));

            Assert.AreEqual(1, item.Afixos.Count,
                "Todos os afixos do pool são do grupo 'vida': só um pode cair. O item sai com " +
                "menos afixos que o grau promete, que é a degradação correta.");
        }

        /// <summary>
        /// Um afixo acima do nível do item não pode cair — e o gate tem de comparar com o nível
        /// <b>do item</b>, nunca com o do jogador.
        /// </summary>
        [Test]
        public void AfixoAcimaDoNivelDoItem_NaoCai()
        {
            var item = new GeradorDeItem().Gerar(
                Base(), GrauDeImpregnacao.Marcado, nivelDoItem: 3,
                new[]
                {
                    Afixo("alto", TipoDeAfixo.Prefixo, StatType.VitMaxima, 50f, 50f, nivelMin: 10),
                    Afixo("baixo", TipoDeAfixo.Prefixo, StatType.DefesaFisica, 2f, 2f, nivelMin: 1),
                },
                new FonteFake(0.5f));

            Assert.AreEqual(1, item.Afixos.Count);
            Assert.AreEqual(StatType.DefesaFisica, item.Afixos[0].Stat,
                "O afixo de nível 10 não pode cair num item de nível 3.");
        }

        /// <summary>
        /// <b>O erro mais difícil de perceber.</b> Se o peso fosse somado sobre o pool inteiro
        /// e só depois filtrado, os afixos barrados pelo gate de nível "roubariam" fatias do
        /// sorteio — o jogo continuaria funcionando e só as proporções ficariam erradas.
        ///
        /// <para>Aqui: dois afixos, um barrado por nível. Com renormalização, o sobrevivente
        /// tem 100% da chance e sai para <b>qualquer</b> valor da fonte.</para>
        /// </summary>
        [Test]
        public void OPeso_ERenormalizadoDepoisDoFiltro()
        {
            var pool = new[]
            {
                Afixo("barrado", TipoDeAfixo.Prefixo, StatType.VitMaxima, 9f, 9f,
                      nivelMin: 99, peso: 99f),
                Afixo("legal", TipoDeAfixo.Prefixo, StatType.DefesaFisica, 2f, 2f,
                      nivelMin: 1, peso: 1f),
            };

            foreach (float sorteio in new[] { 0f, 0.25f, 0.5f, 0.75f, 0.999f })
            {
                var item = new GeradorDeItem().Gerar(
                    Base(), GrauDeImpregnacao.Marcado, 1, pool, new FonteFake(sorteio));

                Assert.AreEqual(1, item.Afixos.Count,
                    $"sorteio={sorteio}: o único afixo legal deveria cair sempre.");
                Assert.AreEqual(StatType.DefesaFisica, item.Afixos[0].Stat,
                    $"sorteio={sorteio}: caiu o afixo barrado pelo gate de nível.");
            }
        }

        [Test]
        public void PoolVazio_NaoEstoura()
        {
            var item = new GeradorDeItem().Gerar(
                Base(), GrauDeImpregnacao.Impregnado, 1, new AfixoDef[0], new FonteFake(0.5f));

            Assert.IsNotNull(item, "Sem pool o item ainda existe — só sai sem afixo.");
            Assert.IsEmpty(item.Afixos);
        }

        [Test]
        public void BaseNula_DevolveNull()
        {
            Assert.IsNull(new GeradorDeItem().Gerar(
                null, GrauDeImpregnacao.Marcado, 1, new AfixoDef[0], new FonteFake(0.5f)));
        }

        [Test]
        public void AfixoDeOutroSlot_NaoCai()
        {
            var soElmo = Afixo("elmo", TipoDeAfixo.Prefixo, StatType.VitMaxima, 5f, 5f);
            soElmo.SlotsPermitidos = new[] { EquipmentSlot.Elmo };

            var item = new GeradorDeItem().Gerar(
                Base(EquipmentSlot.Arma), GrauDeImpregnacao.Marcado, 1,
                new[] { soElmo }, new FonteFake(0.5f));

            Assert.IsEmpty(item.Afixos, "Um afixo restrito a elmo não pode cair numa arma.");
        }

        // ── Rolagem de valor ──────────────────────────────────────────────────

        [Test]
        public void OValor_FicaDentroDaFaixaAutorada()
        {
            var afixo = Afixo("faixa", TipoDeAfixo.Prefixo, StatType.VitMaxima, 10f, 20f);

            foreach (float sorteio in new[] { 0f, 0.5f, 1f })
            {
                // O primeiro ProximoValor escolhe o afixo; o segundo rola o valor.
                var item = new GeradorDeItem().Gerar(
                    Base(), GrauDeImpregnacao.Marcado, 1, new[] { afixo },
                    new FonteFake(0.5f, sorteio));

                float v = item.Afixos[0].Valor;
                Assert.GreaterOrEqual(v, 10f, $"sorteio={sorteio}: abaixo do mínimo autorado");
                Assert.LessOrEqual(v, 20f, $"sorteio={sorteio}: acima do máximo autorado");
            }
        }

        [Test]
        public void FaixaInvertida_NaoQuebra()
        {
            // Erro de autoria: mínimo maior que o máximo. O item não pode sair sem afixo por
            // causa disso -- um Item Creator vai produzir esse valor mais cedo ou mais tarde.
            var afixo = Afixo("invertido", TipoDeAfixo.Prefixo, StatType.VitMaxima, 20f, 10f);

            var item = new GeradorDeItem().Gerar(
                Base(), GrauDeImpregnacao.Marcado, 1, new[] { afixo }, new FonteFake(0.5f, 0.5f));

            Assert.AreEqual(1, item.Afixos.Count);
            Assert.GreaterOrEqual(item.Afixos[0].Valor, 10f);
            Assert.LessOrEqual(item.Afixos[0].Valor, 20f);
        }

        [Test]
        public void MesmaFonte_ProduzOMesmoItem()
        {
            var pool = new[]
            {
                Afixo("a", TipoDeAfixo.Prefixo, StatType.VitMaxima, 1f, 100f),
                Afixo("b", TipoDeAfixo.Prefixo, StatType.DefesaFisica, 1f, 100f),
            };

            var g = new GeradorDeItem();
            var i1 = g.Gerar(Base(), GrauDeImpregnacao.Marcado, 5, pool, new FonteFake(0.7f, 0.3f));
            var i2 = g.Gerar(Base(), GrauDeImpregnacao.Marcado, 5, pool, new FonteFake(0.7f, 0.3f));

            Assert.AreEqual(i1.Afixos[0].AfixoId, i2.Afixos[0].AfixoId);
            Assert.AreEqual(i1.Afixos[0].Valor, i2.Afixos[0].Valor, 0.0001f,
                "O gerador tem de ser determinístico sob a mesma fonte — senão não é testável.");
        }

        // ── Integração com o resto ────────────────────────────────────────────

        [Test]
        public void OsAfixos_ViramModificadoresEfetivos()
        {
            var item = new ItemInstance("qualquer");
            item.Afixos.Add(new AfixoRolado("a", StatType.VitMaxima, 7f));
            item.Afixos.Add(new AfixoRolado("b", StatType.DefesaFisica, 3f));

            // ModificadoresEfetivos consulta Def, e Def loga erro quando não há ItemDatabase
            // em cena -- o runner reprova o teste por log de erro não declarado. Declarar é o
            // certo aqui: a ausência do banco é a condição DO teste, não um sintoma.
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error,
                new System.Text.RegularExpressions.Regex("ItemDatabase.Instance"));

            var mods = item.ModificadoresEfetivos();

            Assert.AreEqual(7f, mods.Where(m => m.Stat == StatType.VitMaxima).Sum(m => m.Valor),
                0.0001f);
            Assert.AreEqual(3f, mods.Where(m => m.Stat == StatType.DefesaFisica).Sum(m => m.Valor),
                0.0001f);
        }

        [Test]
        public void Clonar_NaoCompartilhaAListaDeAfixos()
        {
            var original = new ItemInstance("qualquer");
            original.Afixos.Add(new AfixoRolado("a", StatType.VitMaxima, 7f));

            var copia = original.Clone();
            copia.Afixos.Add(new AfixoRolado("b", StatType.DefesaFisica, 3f));

            Assert.AreEqual(1, original.Afixos.Count,
                "Dois exemplares que dividissem a mesma lista mudariam juntos.");
            Assert.AreEqual(GrauDeImpregnacao.Inerte, copia.Grau);
        }

        // ── O que o save promete ──────────────────────────────────────────────

        /// <summary>
        /// O save tem de sobreviver à ida e volta com os <b>valores rolados intactos</b>.
        ///
        /// <para>É o cenário que falhava em silêncio antes: o formato guardava só
        /// <c>{ itemDefId, quantity }</c>, então recarregar apagava tudo que o exemplar tinha
        /// rolado — e nada no console avisava.</para>
        ///
        /// <para>Grava-se o <b>valor</b>, nunca uma semente: com semente, editar um
        /// <c>AfixoDef</c> mudaria toda arma já dropada, e o jogador veria o item da mochila
        /// dele ficar diferente sem ter feito nada. É por isso que D2 e PoE gravam os mods.</para>
        /// </summary>
        [Test]
        public void OSave_PreservaOsValoresRoladosNaIdaEVolta()
        {
            var original = new ItemInstance("base_de_teste", 1)
            {
                Grau = GrauDeImpregnacao.Impregnado,
                NivelDoItem = 7,
            };
            original.Afixos.Add(new AfixoRolado("prefixo_vida", StatType.VitMaxima, 13.5f));
            original.Afixos.Add(new AfixoRolado("sufixo_defesa", StatType.DefesaFisica, 4.25f));

            // Passa pelo JSON de verdade: JsonUtility é quem grava, e ele tem regras próprias
            // (não serializa Dictionary, exige [Serializable] nas classes aninhadas).
            string json = JsonUtility.ToJson(new ItemSlotData(original));
            var voltou = JsonUtility.FromJson<ItemSlotData>(json).ParaInstancia();

            Assert.IsNotNull(voltou);
            Assert.AreEqual(GrauDeImpregnacao.Impregnado, voltou.Grau, "o grau se perdeu");
            Assert.AreEqual(7, voltou.NivelDoItem, "o nível do item se perdeu");
            Assert.AreEqual(2, voltou.Afixos.Count, "os afixos se perderam");

            Assert.AreEqual("prefixo_vida", voltou.Afixos[0].AfixoId);
            Assert.AreEqual(StatType.VitMaxima, voltou.Afixos[0].Stat);
            Assert.AreEqual(13.5f, voltou.Afixos[0].Valor, 0.0001f,
                "O VALOR rolado tem de voltar idêntico — é ele que está gravado, não a receita.");

            Assert.AreEqual(4.25f, voltou.Afixos[1].Valor, 0.0001f);
        }

        /// <summary>Um save v1 (sem os campos novos) tem de carregar como item Inerte de nível 1.</summary>
        [Test]
        public void SaveAntigo_CarregaComoItemInerte()
        {
            var antigo = JsonUtility.FromJson<ItemSlotData>(
                "{\"itemDefId\":\"consumivel_agua_cacimba\",\"quantity\":2}");

            var item = antigo.ParaInstancia();

            Assert.IsNotNull(item, "Save v1 tem de continuar legível.");
            Assert.AreEqual(2, item.Quantidade);
            Assert.AreEqual(GrauDeImpregnacao.Inerte, item.Grau);
            Assert.AreEqual(1, item.NivelDoItem);
            Assert.IsEmpty(item.Afixos);
        }
    }
}
