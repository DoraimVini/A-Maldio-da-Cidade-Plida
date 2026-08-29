using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using FavelaAmarela.Inventario;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda a <b>Forja de Itens</b> do Carcosa Debugger — a ferramenta com que o arsenal do
    /// jogo é expandido.
    ///
    /// <para><b>O pedido do Vini (2026-08-28):</b> <i>"Carcosa Debugger needs to have the
    /// ability of creating new items, with the constructed mathematics, to we be able to improve
    /// and expand our armory of the game."</i></para>
    ///
    /// <para><b>O que a Forja fazia e o que faltava.</b> Ela já criava um <c>ItemDef</c> de
    /// verdade em <c>Resources</c> — integrada à fonte de dados, como pedido antes. Mas
    /// <b>não preenchia a família</b>: a arma saía com <c>Base</c> nulo, o que em jogo é equipar
    /// e continuar desarmado. O <c>MaoFisicaBridge</c> grita nesse caso, mas gritar depois de o
    /// asset existir é tarde — quem autorou já achou que tinha terminado.</para>
    ///
    /// <para>E não mostrava conta nenhuma. Expandir arsenal sem ver a matemática é chutar: a
    /// única forma de saber se uma arma nova estava forte era criá-la, equipar, entrar em Play
    /// e bater em alguém.</para>
    /// </summary>
    public sealed class ForjaDeItensTests
    {
        private const string Janela = "Assets/FavelaAmarela/Editor/CarcosaDebuggerWindow.cs";

        // ── A família ─────────────────────────────────────────────────────────

        [Test]
        public void AForja_GravaAFamiliaDaArma()
        {
            string fonte = File.ReadAllText(Janela);

            StringAssert.Contains("def.Base = _receita.Base", fonte,
                "A Forja voltou a criar arma sem família. O ItemDef existe, o jogador equipa, " +
                "e o golpe não causa dano nenhum — o modo de falha que ela produz com mais " +
                "frequência, e o mais difícil de diagnosticar olhando o asset.");
        }

        [Test]
        public void AReceita_CarregaOQueAMatematicaPrecisa()
        {
            var receita = new ReceitaDeItem();

            Assert.AreEqual(1, receita.NivelDoItem,
                "O nível do item começa em 1 — é o que devolve exatamente o valor autorado.");

            // Os campos existem como campos públicos; se algum sumir, isto nem compila.
            receita.Base = null;
            receita.Grau = FavelaAmarela.Core.Loot.GrauDeImpregnacao.Marcado;
            receita.NivelDoItem = 3;

            Assert.AreEqual(3, receita.NivelDoItem);
        }

        // ── A prévia da conta ─────────────────────────────────────────────────

        /// <summary>
        /// A prévia é o que separa "criei um item" de "sei se o item presta". Guardar por texto
        /// de fonte é fraco, e é o que dá para afirmar sem abrir uma janela de Editor — mas
        /// pega exatamente a regressão provável: alguém simplifica a UI e a conta some.
        /// </summary>
        [Test]
        public void AForja_MostraAContaAntesDeCriar()
        {
            string fonte = File.ReadAllText(Janela);

            foreach (var (trecho, porque) in new[]
                     {
                         ("PerfilNoNivel(_receita.NivelDoItem)",
                          "a faixa de dano branco no nível escolhido"),
                         ("Esperado por golpe",
                          "o valor esperado, que é o número comparável com o resto do jogo"),
                         ("Golpes para abater",
                          "a única linha que um designer lê sem traduzir"),
                         ("MitigacaoDeDano.Aplicar",
                          "a conta contra a Defesa real do elenco, não contra o dano bruto"),
                         ("CurvaDeGrau.Chance",
                          "quão raro o item é, sem precisar sortear mil vezes"),
                     })
            {
                StringAssert.Contains(trecho, fonte,
                    $"A Forja parou de mostrar {porque}. Expandir arsenal sem ver a conta é " +
                    "chutar — e foi para isso que a prévia existe.");
            }
        }

        [Test]
        public void AForja_DenunciaArmaSemFamilia()
        {
            string fonte = File.ReadAllText(Janela);

            StringAssert.Contains("SEM FAMILIA a arma sai INERTE", fonte,
                "A Forja voltou a aceitar arma sem família em silêncio. O erro tem de aparecer " +
                "ENQUANTO se autora, não depois — no console, em Play, com o jogador " +
                "desarmado sem entender por quê.");
        }

        // ── O que a Forja produz tem de ser jogável ───────────────────────────

        /// <summary>
        /// Toda arma que já existe no projeto tem família. É o mesmo contrato que a Forja passa
        /// a exigir — e o guarda que pegaria uma arma criada por ela sem base.
        /// </summary>
        [Test]
        public void TodaArmaAutorada_TemFamiliaLigada()
        {
            var inertes = new List<string>();
            var vistas = 0;

            foreach (var def in AssetDatabase.FindAssets("t:ItemDef")
                         .Select(AssetDatabase.GUIDToAssetPath)
                         .Select(AssetDatabase.LoadAssetAtPath<ItemDef>)
                         .Where(d => d != null && d.Tipo == ItemType.Arma))
            {
                vistas++;

                if (def.Base == null)
                    inertes.Add($"{def.name}: sem BaseDeArma — equipar e continuar desarmado");
                else if (def.Base.Habilidade == null)
                    inertes.Add($"{def.name}: família '{def.Base.name}' sem HabilidadeDef");
                else if (def.Base.DanoMaxBase <= 0f)
                    inertes.Add($"{def.name}: família '{def.Base.name}' com dano branco ZERO — " +
                                "a arma existe, tem moveset, e não machuca");
            }

            Assert.Greater(vistas, 0, "Nenhuma arma autorada encontrada.");

            Assert.IsEmpty(inertes,
                "Arma(s) inerte(s):" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", inertes) + Environment.NewLine +
                "Conserto: 'Tools/FavelaAmarela/Armas: migrar para dano branco em faixa'.");
        }

        /// <summary>
        /// Faixa invertida no Inspector é o erro de digitação mais fácil de cometer na Forja, e
        /// o <c>PerfilDeArma</c> a ordena em vez de estourar — mas ordenar em runtime não
        /// conserta o asset, que continua mentindo para quem o lê.
        /// </summary>
        [Test]
        public void NenhumaFamilia_TemFaixaInvertida()
        {
            var invertidas = AssetDatabase.FindAssets("t:BaseDeArma")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<BaseDeArma>)
                .Where(b => b != null && b.DanoMinBase > b.DanoMaxBase)
                .Select(b => $"{b.name}: {b.DanoMinBase} – {b.DanoMaxBase}")
                .ToList();

            Assert.IsEmpty(invertidas,
                "Família(s) com dano mínimo maior que o máximo:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", invertidas));
        }

        [Test]
        public void TodaFamilia_TemPrecisaoEMultiplicadorUteis()
        {
            var quebradas = new List<string>();

            foreach (var b in AssetDatabase.FindAssets("t:BaseDeArma")
                         .Select(AssetDatabase.GUIDToAssetPath)
                         .Select(AssetDatabase.LoadAssetAtPath<BaseDeArma>)
                         .Where(b => b != null))
            {
                if (b.PrecisaoBase <= 0f)
                    quebradas.Add($"{b.name}: precisão {b.PrecisaoBase} — a arma NUNCA acerta");

                if (b.MultiplicadorCritico < 1f)
                    quebradas.Add($"{b.name}: multiplicador crítico {b.MultiplicadorCritico} — " +
                                  "crítico que REDUZ dano, o jogador é punido por ter sorte");
            }

            Assert.IsEmpty(quebradas,
                "Família(s) com número que quebra o combate:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", quebradas));
        }
    }
}
