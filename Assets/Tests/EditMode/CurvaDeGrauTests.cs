using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using FavelaAmarela.Core.Loot;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a <b>curva de raridade</b> e a promessa de que chefe recompensa.
    ///
    /// <para><b>O pedido do Vini (2026-08-28):</b> <i>"Nível 1: maioria dos itens de mais baixo
    /// tier, e construir uma escala de RNG onde seja possível o drop de uma arma ou armadura
    /// lendária na primeira fase, mas ter um drop realmente baixo. E ir escalonando conforme a
    /// progressão, onde no endgame você ignore totalmente os itens de T1."</i> E:
    /// <i>"fazer com que os bosses dropem, além de seus itens como Necronomicon ou o Patuá, uma
    /// recompensa ao jogador para que ele sinta a progressão do personagem."</i></para>
    /// </summary>
    public sealed class CurvaDeGrauTests
    {
        private sealed class FonteFixa : IFonteDeAleatoriedade
        {
            private readonly float _v;
            public FonteFixa(float v) => _v = v;
            public float ProximoValor() => _v;
            public int ProximoInteiro(int min, int max) => min;
        }

        // ── A forma da curva ──────────────────────────────────────────────────

        /// <summary>
        /// <b>Nenhum peso é zero em nível nenhum.</b> É a diferença entre uma curva e um portão:
        /// portão ("Impregnado só a partir do nível 5") faz o loot da primeira fase ser sempre
        /// igual e tira o motivo de abrir o próximo baú. Peso baixo produz a história que o
        /// jogador conta depois.
        /// </summary>
        [Test]
        public void NoNivel1_OGrauAltoEhPossivelERaro()
        {
            float chance = CurvaDeGrau.Chance(GrauDeImpregnacao.Impregnado, 1);

            Assert.Greater(chance, 0f,
                "O grau alto ficou IMPOSSÍVEL no nível 1. Isso é um portão, não uma curva — e " +
                "o pedido foi explicitamente o contrário.");

            Assert.Less(chance, 0.10f,
                $"O grau alto está em {chance:P1} no nível 1. 'Realmente baixo' foi o pedido: " +
                "raridade que acontece toda hora deixa de ser raridade.");
        }

        [Test]
        public void NoNivel1_AMaioriaEhOGrauBaixo()
        {
            Assert.Greater(CurvaDeGrau.Chance(GrauDeImpregnacao.Inerte, 1), 0.5f,
                "No começo do jogo a maioria do que cai tem de ser comum — senão não há para " +
                "onde progredir.");
        }

        /// <summary>
        /// <i>"No endgame você ignore totalmente os itens de T1."</i> O Inerte some por
        /// <b>peso</b>, não por bloqueio: continua possível, e é isso que faz um drop ruim no
        /// teto ser azar em vez de bug.
        /// </summary>
        [Test]
        public void NoTeto_OGrauBaixoEhIrrelevanteMasNaoImpossivel()
        {
            const int teto = 12;   // cap da curva de Exposição (Progressao)

            float inerte = CurvaDeGrau.Chance(GrauDeImpregnacao.Inerte, teto);

            Assert.Less(inerte, 0.05f, $"Inerte ainda vale {inerte:P1} no teto.");
            Assert.Greater(inerte, 0f, "Inerte virou impossível — some por peso, não por portão.");

            Assert.Greater(CurvaDeGrau.Chance(GrauDeImpregnacao.Impregnado, teto), inerte,
                "No teto o grau alto tem de ser mais provável que o baixo. Sem essa inversão a " +
                "progressão não é sentida no loot.");
        }

        [Test]
        public void AChanceDoGrauAlto_CresceACadaNivel()
        {
            for (int nivel = 1; nivel < 12; nivel++)
            {
                Assert.Greater(CurvaDeGrau.Chance(GrauDeImpregnacao.Impregnado, nivel + 1),
                               CurvaDeGrau.Chance(GrauDeImpregnacao.Impregnado, nivel),
                               $"A curva parou de subir entre {nivel} e {nivel + 1}.");
            }
        }

        // ── O piso da fonte ───────────────────────────────────────────────────

        [Test]
        public void OGrauSorteado_NuncaEhPiorQueOAutorado()
        {
            // Sorteio no extremo baixo: se algo puder rebaixar, é aqui.
            var grau = CurvaDeGrau.Sortear(1, GrauDeImpregnacao.Marcado, new FonteFixa(0f));

            Assert.GreaterOrEqual((int)grau, (int)GrauDeImpregnacao.Marcado,
                "Um chefe que declara Marcado largou algo pior. 'Chefe dá recompensa' vira " +
                "aposta, e o jogador que mata o Byakhee pode sair com lixo.");
        }

        /// <summary>
        /// Relíquia é peça única e nomeada, autorada à mão — o Necronomicon, o Anel do Sinal
        /// Amarelo. Ela cai porque a tabela do chefe a declara, nunca porque a sorte a inventou.
        /// </summary>
        [Test]
        public void ReliquiaAutorada_AtravessaIntacta()
        {
            foreach (float sorteio in new[] { 0f, 0.5f, 0.999f })
            {
                Assert.AreEqual(GrauDeImpregnacao.Reliquia,
                    CurvaDeGrau.Sortear(12, GrauDeImpregnacao.Reliquia, new FonteFixa(sorteio)),
                    "A curva rebaixou uma relíquia autorada — o drop garantido do chefe virou " +
                    "item comum.");
            }
        }

        [Test]
        public void ACurva_NuncaSorteiaReliquia()
        {
            for (int nivel = 1; nivel <= 12; nivel++)
            {
                Assert.AreEqual(0f, CurvaDeGrau.Chance(GrauDeImpregnacao.Reliquia, nivel), 0.0001f,
                    $"A curva passou a sortear relíquia no nível {nivel}. Isso quebra a promessa " +
                    "de que cada relíquia tem história própria.");
            }
        }

        [Test]
        public void ARegraDeReliquia_TemUmaFonteSo()
        {
            Assert.AreEqual(CurvaDeGrau.EhSorteavel(GrauDeImpregnacao.Reliquia),
                            RegrasDeGrau.PodeSerGerado(GrauDeImpregnacao.Reliquia),
                "A regra 'relíquia não é gerada' voltou a existir em dois lugares. Duas cópias " +
                "em camadas diferentes divergem em silêncio, e o sintoma seria uma relíquia " +
                "aleatória.");
        }

        // ── Chefe recompensa ──────────────────────────────────────────────────

        /// <summary>
        /// <b>Criar tabela não é ligar tabela.</b> O <c>Drop_Abdul</c> foi criado e ficou
        /// apontando para nada — o prefab do Abdul não tinha <c>DropAoAbater</c>, e antes desta
        /// fase nem poderia ter: o componente exigia <c>EnemyBase</c>, que o Abdul não é.
        /// </summary>
        [Test]
        public void TodoChefe_TemTabelaDeDropLigada()
        {
            var mudos = new List<string>();

            foreach (var (prefab, tabela) in new[]
                     {
                         ("Byakhee", "Drop_Byakhee"),
                         ("Abdul_Alhazred", "Drop_Abdul"),
                     })
            {
                string caminho = $"Assets/FavelaAmarela/Art/Enemies/{prefab}.prefab";

                if (!File.Exists(caminho)) { mudos.Add($"{prefab}: prefab ausente"); continue; }

                string yaml = File.ReadAllText(caminho);

                if (!yaml.Contains("Itens.DropAoAbater"))
                {
                    mudos.Add($"{prefab}: sem DropAoAbater — abatê-lo não larga equipamento");
                    continue;
                }

                string guid = GuidDo($"Assets/FavelaAmarela/Config/Drops/{tabela}.asset");
                if (guid != null && !yaml.Contains(guid))
                    mudos.Add($"{prefab}: tem DropAoAbater mas NÃO aponta para {tabela}");
            }

            Assert.IsEmpty(mudos,
                "Chefe(s) sem espólio ligado:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", mudos) + Environment.NewLine +
                "Conserto: 'Tools/FavelaAmarela/Itens: montar a recompensa dos chefes'.");
        }

        /// <summary>
        /// A recompensa tem de ser <b>equipamento</b>, não só o item de rito. Uma tabela com uma
        /// entrada só — que era o estado do Byakhee — entrega o Anel e mais nada.
        /// </summary>
        [Test]
        public void ATabelaDeChefe_LargaArmaEArmadura()
        {
            foreach (var nome in new[] { "Drop_Byakhee", "Drop_Abdul" })
            {
                var tabela = AssetDatabase.LoadAssetAtPath<TabelaDeDrop>(
                    $"Assets/FavelaAmarela/Config/Drops/{nome}.asset");

                Assert.IsNotNull(tabela, $"{nome} não existe.");

                var tipos = tabela.ProjetarCandidatos()
                    .Select(c => ItemDatabaseDoEditor(c.ItemDefId)?.Tipo)
                    .Where(t => t.HasValue)
                    .Select(t => t.Value)
                    .ToList();

                Assert.Contains(ItemType.Arma, tipos,
                    $"{nome} não larga arma nenhuma — o jogador mata o chefe e não fica mais forte.");

                Assert.Contains(ItemType.Armadura, tipos,
                    $"{nome} não larga armadura. Contra o Byakhee (26 de garra contra 6 de " +
                    "Defesa = 5 golpes até o Colapso), cada ponto de Defesa muda a conta.");
            }
        }

        // ── Apoio ─────────────────────────────────────────────────────────────

        private static ItemDef ItemDatabaseDoEditor(string id) =>
            AssetDatabase.FindAssets("t:ItemDef")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ItemDef>)
                .FirstOrDefault(d => d != null && d.Id == id);

        private static string GuidDo(string asset)
        {
            string meta = asset + ".meta";
            if (!File.Exists(meta)) return null;

            var m = System.Text.RegularExpressions.Regex.Match(
                File.ReadAllText(meta), @"guid:\s*(\w+)");

            return m.Success ? m.Groups[1].Value : null;
        }
    }
}
