using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace FavelaAmarela.Tests.EditMode
{
    /// <summary>
    /// Guarda que a armadura <b>sobe</b> a cada tier, peça por peça.
    ///
    /// <para><b>O buraco que motivou (2026-09-02).</b> Existiam só as pontas: o começo
    /// (Farrapos / Sucata / Ferro Enferrujado, 3 de defesa somados e <b>zero</b> de Vitalidade)
    /// e o Set Lendário (17 e 50). O jogador atravessava Deserto, Tumba, Santuário, Portões e
    /// Castelo — o Vertical Slice inteiro — <b>sem trocar de armadura uma vez</b>, porque não
    /// havia o que vestir no meio.</para>
    ///
    /// <para>Os dois tiers do meio entraram com uma curva interpolada. Este teste existe para
    /// que ela não seja desfeita por acidente: mexer no número de uma peça isolada é fácil, e
    /// perceber que ela passou a valer menos que a do tier anterior, não.</para>
    ///
    /// <para><b>O que ele NÃO faz:</b> não fixa valores. Balancear é decisão de design e vai
    /// mudar. Ele exige só a <b>ordem</b> — que um tier mais alto nunca proteja menos que o
    /// anterior, na mesma peça.</para>
    /// </summary>
    public sealed class EscadaDeArmadurasTests
    {
        private const string Pasta = "Assets/FavelaAmarela/Config/Resources/Itens";

        /// <summary>Do mais fraco ao mais forte. O sufixo do Id identifica a fileira.</summary>
        private static readonly string[] Tiers = { "", "sepulto", "yhtill", "set" };

        private static readonly Dictionary<int, string> Pecas = new Dictionary<int, string>
        {
            [2] = "elmo", [3] = "peitoral", [4] = "grevas",
        };

        private sealed class Peca
        {
            public string Nome;
            public int Slot;
            public int Defesa;
            public int Vitalidade;
        }

        private static List<Peca> Armaduras()
        {
            var saida = new List<Peca>();

            foreach (var caminho in Directory.EnumerateFiles(Pasta, "Item_Armadura_*.asset"))
            {
                string txt = File.ReadAllText(caminho);

                var nome = Regex.Match(txt, @"^  Nome: (.*)$", RegexOptions.Multiline);
                var slot = Regex.Match(txt, @"^  SlotEquipamento: (\d+)$", RegexOptions.Multiline);
                if (!nome.Success || !slot.Success) continue;

                var mods = Regex.Matches(txt, @"- Stat: (\d+)\s*\n\s*Valor: (-?[\d.]+)")
                    .Cast<Match>()
                    .ToDictionary(m => m.Groups[1].Value,
                                  m => (int)float.Parse(m.Groups[2].Value,
                                      System.Globalization.CultureInfo.InvariantCulture));

                saida.Add(new Peca
                {
                    Nome = nome.Groups[1].Value.Trim(),
                    Slot = int.Parse(slot.Groups[1].Value),
                    Defesa = mods.TryGetValue("7", out var d) ? d : 0,      // Stat 7 = defesa
                    Vitalidade = mods.TryGetValue("0", out var v) ? v : 0,  // Stat 0 = vitalidade
                });
            }

            return saida;
        }

        [Test]
        public void CadaSlotTemUmaPecaEmCadaTier()
        {
            var armaduras = Armaduras();

            // REGRA DURA: sem peça nenhuma, tudo abaixo passaria vazio e verde.
            Assert.GreaterOrEqual(armaduras.Count, 12,
                $"Só achei {armaduras.Count} peça(s) de armadura. São 4 tiers × 3 slots = 12. " +
                "Este teste não está lendo os assets.");

            var faltando = new List<string>();

            foreach (var (slot, peca) in Pecas)
            {
                int quantas = armaduras.Count(a => a.Slot == slot);
                if (quantas < Tiers.Length)
                    faltando.Add($"  {peca}: {quantas} peça(s), esperava {Tiers.Length} " +
                                 "(uma por tier)");
            }

            Assert.IsEmpty(faltando,
                "Slot de armadura sem peça em algum tier — o jogador chega nesse trecho do jogo " +
                "e não tem o que vestir:" + System.Environment.NewLine +
                string.Join(System.Environment.NewLine, faltando));
        }

        [Test]
        public void ATierMaisAltaNuncaProtegeMenos()
        {
            var armaduras = Armaduras();
            var regressoes = new List<string>();

            foreach (var (slot, nomeDaPeca) in Pecas)
            {
                var doSlot = armaduras.Where(a => a.Slot == slot).ToList();

                // ordena pelo somatório: é o que o jogador sente ao trocar
                var ordenada = doSlot.OrderBy(a => a.Defesa + a.Vitalidade).ToList();

                for (int i = 1; i < ordenada.Count; i++)
                {
                    var antes = ordenada[i - 1];
                    var agora = ordenada[i];

                    if (agora.Defesa < antes.Defesa)
                        regressoes.Add(
                            $"  {nomeDaPeca}: '{agora.Nome}' dá {agora.Defesa} de defesa, " +
                            $"menos que '{antes.Nome}' ({antes.Defesa}) — e é a peça mais forte " +
                            "no somatório. Trocar para ela DESPROTEGE o jogador.");

                    if (agora.Vitalidade < antes.Vitalidade)
                        regressoes.Add(
                            $"  {nomeDaPeca}: '{agora.Nome}' dá {agora.Vitalidade} de " +
                            $"Vitalidade, menos que '{antes.Nome}' ({antes.Vitalidade}).");
                }
            }

            Assert.IsEmpty(regressoes,
                "A escada de armadura regrediu:" + System.Environment.NewLine +
                string.Join(System.Environment.NewLine, regressoes));
        }

        /// <summary>
        /// O peitoral é a peça grande, o elmo vem no meio, as grevas são a menor. É a proporção
        /// que o Set Lendário estabeleceu (8 / 5 / 4) e que os tiers novos seguiram.
        /// </summary>
        [Test]
        public void OPeitoralEhSempreAPecaMaisForteDoTier()
        {
            var armaduras = Armaduras();
            var fora = new List<string>();

            // agrupa por tier pelo sufixo do nome — "de Set", "de Yhtill", "do Sepulto"…
            foreach (var grupo in armaduras.GroupBy(a => a.Nome.Split(' ').Last()))
            {
                var peitoral = grupo.FirstOrDefault(a => a.Slot == 3);
                if (peitoral == null) continue;

                foreach (var outra in grupo.Where(a => a.Slot != 3))
                {
                    if (outra.Defesa > peitoral.Defesa)
                        fora.Add($"  '{outra.Nome}' ({outra.Defesa}) protege mais que o " +
                                 $"peitoral do mesmo conjunto, '{peitoral.Nome}' " +
                                 $"({peitoral.Defesa}).");
                }
            }

            Assert.IsEmpty(fora,
                "Proporção entre peças quebrada dentro de um conjunto:" +
                System.Environment.NewLine + string.Join(System.Environment.NewLine, fora));
        }
    }
}
